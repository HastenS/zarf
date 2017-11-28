﻿using System.Linq.Expressions;
using Zarf.Query.Expressions;
using System;
using System.Collections.Generic;
using Zarf.Update.Expressions;

namespace Zarf.Builders
{
    public abstract class SqlTextBuilder : ExpressionVisitor, ISqlTextBuilder
    {
        protected static HashSet<Type> NumbericTypes = new HashSet<Type>()
        {
            typeof(Int32),
            typeof(Int16),
            typeof(Int64),
            typeof(UInt16),
            typeof(UInt32),
            typeof(UInt64),
            typeof(Single),
            typeof(Double),
            typeof(Decimal)
        };

        protected override Expression VisitExtension(Expression node)
        {
            switch (node)
            {
                case QueryExpression query:
                    VisitQuery(query);
                    break;
                case WhereExperssion where:
                    VisitWhere(where);
                    break;
                case ColumnExpression column:
                    VisitColumn(column);
                    break;
                case JoinExpression join:
                    VisitJoin(join);
                    break;
                case OrderExpression order:
                    VisitOrder(order);
                    break;
                case GroupExpression group:
                    VisitGroup(group);
                    break;
                case UnionExpression union:
                    VisitUnion(union);
                    break;
                case ExceptExpression except:
                    VisitExcept(except);
                    break;
                case IntersectExpression intersect:
                    VisitIntersect(intersect);
                    break;
                case SqlFunctionExpression function:
                    VisitSqlFunction(function);
                    break;
                case AggregateExpression aggregate:
                    VisitAggregate(aggregate);
                    break;
                case SkipExpression skip:
                    VisitSkip(skip);
                    break;
                case AllExpression all:
                    VisitAll(all);
                    break;
                case AnyExpression any:
                    VisitAny(any);
                    break;
                case DbStoreExpression store:
                    VisitStore(store);
                    break;
            }

            return node;
        }

        protected abstract Expression VisitQuery(QueryExpression query);

        protected abstract Expression VisitWhere(WhereExperssion where);

        protected abstract Expression VisitColumn(ColumnExpression column);

        protected abstract Expression VisitJoin(JoinExpression join);

        protected abstract Expression VisitOrder(OrderExpression order);

        protected abstract Expression VisitGroup(GroupExpression group);

        protected abstract Expression VisitUnion(UnionExpression union);

        protected abstract Expression VisitExcept(ExceptExpression except);

        protected abstract Expression VisitIntersect(IntersectExpression intersec);

        protected abstract Expression VisitSqlFunction(SqlFunctionExpression function);

        protected abstract Expression VisitAggregate(AggregateExpression aggregate);

        protected abstract Expression VisitSkip(SkipExpression skip);

        protected abstract Expression VisitAll(AllExpression all);

        protected abstract Expression VisitAny(AnyExpression any);

        protected abstract Expression VisitStore(DbStoreExpression store);

        public abstract string Build(Expression expression);
    }
}
