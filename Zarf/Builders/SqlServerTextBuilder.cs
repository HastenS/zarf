﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Zarf.Query.Expressions;
using Zarf.Extensions;
using System.Linq;
using Zarf.Entities;

namespace Zarf.Builders
{
    public partial class SqlServerTextBuilder : SqlTextBuilder
    {
        protected StringBuilder _builder { get; set; } = new StringBuilder();

        public override string Build(Expression expression)
        {
            BuildExpression(expression);
            return _builder.ToString();
        }

        protected override Expression VisitAggregate(AggregateExpression aggregate)
        {
            if (Utils.AggregateFunctionMap.TryGetValue(aggregate.Method.Name, out string funcName))
            {
                Append(funcName, '(');
                if (aggregate.KeySelector == null)
                {
                    Append("1");
                }
                else
                {
                    BuildExpression(aggregate.KeySelector);
                }
                Append(')');
            }
            else
            {
                throw new NotImplementedException($"method {aggregate.Method.Name} is not supported!");
            }
            return aggregate;
        }

        protected override Expression VisitColumn(ColumnExpression column)
        {
            if (column.FromTable != null && !column.FromTable.Alias.IsNullOrEmpty())
            {
                _builder.Append(column.FromTable.Alias.Escape());
                _builder.Append('.');
            }

            if (column.Column == null)
            {
                Append(" NULL ");
            }
            else
            {
                _builder.Append(column.Column.Name.Escape());
            }

            if (!column.Alias.IsNullOrEmpty())
            {
                Append(" AS ");
                _builder.Append(column.Alias.Escape());
            }

            return column;
        }

        protected override Expression VisitExcept(ExceptExpression except)
        {
            Append(" Except ");
            BuildExpression(except.Query);
            return except;
        }

        protected override Expression VisitGroup(GroupExpression group)
        {
            BuildColumns(group.Columns);
            return group;
        }

        protected override Expression VisitIntersect(IntersectExpression intersec)
        {
            Append(" INTERSECT ");
            BuildExpression(intersec.Query);
            return intersec;
        }

        protected override Expression VisitUnion(UnionExpression union)
        {
            Append(" UNION ALL ");
            BuildExpression(union.Query);
            return union;
        }

        protected override Expression VisitJoin(JoinExpression join)
        {
            switch (join.JoinType)
            {
                case JoinType.Left:
                    Append(" Left JOIN ");
                    break;
                case JoinType.Right:
                    Append(" Right JOIN ");
                    break;
                case JoinType.Full:
                    Append(" Full JOIN ");
                    break;
                case JoinType.Inner:
                    Append(" Inner JOIN ");
                    break;
                case JoinType.Cross:
                    Append(" Cross JOIN ");
                    break;
            }

            var query = join.Table.Cast<QueryExpression>();

            BuildSubQuery(query);

            if (query.IsEmptyQuery())
            {
                BuildFromTable(query);
            }
            else
            {
                Append(" (");
                BuildExpression(query);
                Append(") AS " + query.Alias.Escape());
            }

            Append(" ON ");
            BuildExpression(join.Predicate);
            return join;
        }

        protected override Expression VisitOrder(OrderExpression order)
        {
            var direction = order.OrderType == OrderType.Desc
                ? " DESC "
                : " ASC ";

            BuildColumns(order.Columns);
            _builder.Append(direction);
            return order;
        }

        protected override Expression VisitQuery(QueryExpression query)
        {
            Append(" SELECT  ");

            BuildDistinct(query);
            BuildLimit(query);
            BuildProjections(query);

            Append(" FROM ");

            BuildSubQuery(query);
            BuildFromTable(query);
            BuildJoins(query);

            BuildWhere(query);

            BuildGroups(query);
            BuildOrders(query);

            BuildSets(query);

            return query;
        }

        protected override Expression VisitWhere(WhereExperssion where)
        {
            Append(" WHERE ");
            BuildExpression(where.Predicate);

            return where;
        }

        protected override Expression VisitAll(AllExpression all)
        {
            Append(" IF NOT EXISTS(");
            BuildExpression(all.Expression);
            Append(") SELECT CAST(1 AS BIT) ELSE SELECT CAST(0 AS BIT)");
            return all;
        }

        protected override Expression VisitAny(AnyExpression any)
        {
            Append(" IF EXISTS(");
            BuildExpression(any.Expression);
            Append(") SELECT CAST(1 AS BIT) ELSE SELECT CAST(0 AS BIT)");
            return any;
        }

        protected override Expression VisitSkip(SkipExpression skip)
        {
            Append(" ROW_NUMBER() OVER ( ");
            if (skip.Orders == null || skip.Orders.Count == 0)
            {
                Append("ORDER BY GETDATE()) AS __ROWINDEX__");
            }
            else
            {
                foreach (var order in skip.Orders)
                {
                    BuildExpression(order);
                    Append(',');
                }

                _builder.Length--;
                Append(")  AS __ROWINDEX__");
            }

            return skip;
        }

        protected override Expression VisitConstant(ConstantExpression constant)
        {
            if (constant.Type == typeof(bool))
            {
                _builder.Append(constant.Value.Cast<bool>() ? 1 : 0);
            }
            else if (NumbericTypes.Contains(constant.Type))
            {
                _builder.Append(constant.Value);
            }
            else if (constant.Value.Is<DateTime>())
            {
                //998毫秒Sql Server 999毫秒报错
                var date = constant.Value.Cast<DateTime>();
                Append(
                    '\'',
                    date.Year,
                    date.ToString("-MM-dd HH:mm:ss."),
                    date.Millisecond > 998 ? 998 : date.Millisecond,
                    '\'');
            }
            else
            {
                Append('\'', constant.Value.ToString(), '\'');
            }

            return constant;
        }

        protected override Expression VisitUnary(UnaryExpression unary)
        {
            switch (unary.NodeType)
            {
                case ExpressionType.Not:
                    Append(" NOT ( ");
                    BuildExpression(unary.Operand);
                    Append(" )");
                    break;
                case ExpressionType.ArrayLength:
                    break;
            }
            return unary;
        }

        protected override Expression VisitBinary(BinaryExpression binary)
        {
            var leftIsNull = binary.Left.IsNullValueConstant();
            var righIsNull = binary.Right.IsNullValueConstant();
            var left = binary.Left;
            var right = binary.Right;

            if (Utils.OperatorMap.TryGetValue(binary.NodeType, out string op))
            {
                if (leftIsNull && righIsNull)
                {
                    left = right = Expression.Constant(1);
                }
                else if (leftIsNull || righIsNull)
                {
                    BuildExpression(leftIsNull ? right : left);
                    if (binary.NodeType == ExpressionType.Equal)
                    {
                        Append(" IS NULL ");
                        return binary;
                    }

                    if (binary.NodeType == ExpressionType.NotEqual)
                    {
                        Append(" IS NOT NULL ");
                        return binary;
                    }

                    throw new ArgumentNullException(" NULL ");
                }

                BuildExpression(left);
                _builder.Append(op);
                BuildExpression(right);

                return binary;
            }

            throw new NotImplementedException("ViistBinary");
        }

        protected virtual void BuildColumns(IEnumerable<ColumnExpression> columns)
        {
            foreach (var column in columns)
            {
                VisitColumn(column);
                Append(',');
            }

            _builder.Length--;
        }

        protected virtual void BuildLimit(QueryExpression query)
        {
            if (query.Limit != 0)
            {
                Append(" TOP ", query.Limit);
            }
        }

        protected virtual void BuildDistinct(QueryExpression query)
        {
            if (query.IsDistinct)
            {
                Append(" DISTINCT ");
            }
        }

        protected virtual void BuildProjections(QueryExpression query)
        {
            if (query.Projections == null)
            {
                Append('*');
            }
            else
            {
                query.Projections.ToList().ForEach(item =>
                {
                    BuildExpression(item);
                    Append(',');
                });
                _builder.Length--;
            }
        }

        protected virtual void BuildSubQuery(QueryExpression query)
        {
            if (query.SubQuery != null)
            {
                Append('(');
                BuildExpression(query.SubQuery);
                Append(')');
            }
        }

        protected virtual void BuildFromTable(QueryExpression query)
        {
            if (query.SubQuery == null)
            {
                Utils.CheckNull(query.Table, "query.Table is null");
                Append(query.Table.Schema.Escape(), '.', query.Table.Name.Escape());
            }

            if (!query.Alias.IsNullOrEmpty())
            {
                Append(" AS ", query.Alias.Escape());
            }
        }

        protected virtual void BuildJoins(QueryExpression query)
        {
            if (query.Joins == null || query.Joins.Count == 0)
            {
                return;
            }

            foreach (var join in query.Joins)
            {
                BuildExpression(join);
            }
        }

        protected virtual void BuildSets(QueryExpression query)
        {
            if (query.Sets == null || query.Sets.Count == 0)
            {
                return;
            }

            foreach (var set in query.Sets)
            {
                BuildExpression(set);
            }
        }

        protected virtual void BuildOrders(QueryExpression query)
        {
            if (query.Orders == null || query.Orders.Count == 0)
            {
                return;
            }

            if (query.Parent != null && query.Limit == 0)
            {
                Append(" TOP (100) Percent ");
            }
            Append(" ORDER BY ");

            foreach (var order in query.Orders)
            {
                BuildExpression(order);
                Append(',');
            }

            _builder.Length--;
        }

        protected virtual void BuildGroups(QueryExpression query)
        {
            if (query.Groups == null || query.Groups.Count == 0)
            {
                return;
            }

            if (query.Parent != null && query.Limit == 0)
            {
                Append(" TOP (100) Percent ");
            }

            Append(" GROUP BY ");

            foreach (var group in query.Groups)
            {
                BuildExpression(group);
                Append(',');
            }

            _builder.Length--;
        }

        protected virtual void BuildWhere(QueryExpression query)
        {
            if (query.Where != null)
            {
                VisitWhere(query.Where);
            }
        }

        internal virtual void Append(params object[] args)
        {
            foreach (var arg in args)
            {
                _builder.Append(arg);
            }
        }

        internal virtual void BuildExpression(Expression expression)
        {
            Visit(expression);
        }
    }
}