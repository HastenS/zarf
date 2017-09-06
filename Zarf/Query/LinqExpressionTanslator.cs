﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Zarf.Entities;
using Zarf.Extensions;
using Zarf.Mapping;
using Zarf.Query.Expressions;
using Zarf.Query.ExpressionTranslators;
using Zarf.Query.ExpressionVisitors;

namespace Zarf.Query
{
    public class LinqExpressionTanslator : ILinqExpressionTanslator
    {
        public Expression Build(Expression node, QueryContext context)
        {
            var translatedExpression = new SqlTranslatingExpressionVisitor(context, NodeTypeTranslatorProvider.Default).Visit(node);
            if (translatedExpression.Is<QueryExpression>())
            {
                var rootQuery = translatedExpression.As<QueryExpression>();
                if (rootQuery.Projections.Count == 0)
                {
                    rootQuery.Projections.AddRange(rootQuery.GenerateColumns());
                }

                BuildResult(rootQuery, context);
                OptimizingColumns(rootQuery);
                return rootQuery;
            }
            else
            {
                context.ProjectionMappingProvider.Map(translatedExpression, translatedExpression, 0);
                return translatedExpression;
            }
        }

        public void BuildResult(QueryExpression rootQuery, QueryContext context)
        {
            if (rootQuery.Result != null)
            {
                var entityNewExpression = new ExpressionMemberMapVisitor(
                    rootQuery,
                    ToEntityNewExpression,
                    context
                 ).Visit(rootQuery.Result.EntityNewExpression);

                if (entityNewExpression != rootQuery.Result.EntityNewExpression)
                {
                    rootQuery.Result = new EntityResult(entityNewExpression, rootQuery.Result.ElementType);
                }

                return;
            }

            var entityNew = ToEntityNewExpression(
                    rootQuery,
                    rootQuery,
                    context
                );

            if (entityNew == null)
            {
                throw new Exception("QueryExpressionBuilder.BuildResult MemberInit Is NULL!");
            }

            rootQuery.Result = new EntityResult(entityNew, rootQuery.Type);
        }

        public void OptimizingColumns(QueryExpression query)
        {
            if (query.SubQuery == null)
            {
                return;
            }

            var subQueryProjections = new List<Expression>();

            foreach (var p in query.Projections)
            {
                var column = p.As<ColumnExpression>();
                if (column == null)
                {
                    var aggreate = p.As<AggregateExpression>();
                    if (aggreate != null && aggreate.KeySelector.Is<ColumnExpression>())
                    {
                        column = aggreate.KeySelector.As<ColumnExpression>();
                    }
                }

                foreach (var sColumn in query.SubQuery.Projections.OfType<ColumnExpression>())
                {
                    if (sColumn.Alias == column.Column.Name)
                    {
                        subQueryProjections.Add(sColumn);
                        break;
                    }
                }
            }

            foreach (var projection in query.SubQuery.Projections.Where(item => !item.Is<ColumnExpression>()))
            {
                subQueryProjections.Add(projection);
            }

            query.SubQuery.Projections.Clear();
            query.SubQuery.Projections.AddRange(subQueryProjections);

            OptimizingColumns(query.SubQuery);
        }

        /// <summary>
        /// 清醒后重构
        /// </summary>
        /// <param name="rootQuery"></param>
        /// <param name="fromTable"></param>
        /// <param name="mappingProvider"></param>
        /// <returns></returns>
        public Expression ToEntityNewExpression(
            QueryExpression rootQuery,
            FromTableExpression fromTable,
            IQueryContext context
        )
        {
            var entityType = fromTable.Type.GetCollectionElementType();
            var entityTypeDescriptor = EntityTypeDescriptorFactory.Factory.Create(entityType);
            var entityBindings = new List<MemberBinding>();
            var entityNew = Expression.New(entityTypeDescriptor.Constructor);

            foreach (var memberInfo in entityTypeDescriptor.GetWriteableMembers())
            {
                var column = new ColumnExpression(fromTable, memberInfo);
                if (context.EntityMemberMappingProvider.IsMapped(memberInfo))
                {
                    column = context.EntityMemberMappingProvider.GetExpression(memberInfo).As<ColumnExpression>();
                }

                var ordinal = QueryUtils.FindProjectionOrdinal(rootQuery, column);
                if (ordinal == -1)
                {
                    continue;
                }

                context.ProjectionMappingProvider.Map(column, rootQuery, ordinal);
                entityBindings.Add(Expression.Bind(memberInfo, column));
            }

            foreach (var member in entityTypeDescriptor.Type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!context.PropertyNavigationContext.IsPropertyNavigation(member))
                {
                    continue;
                }

                var navigation = context.PropertyNavigationContext.GetNavigation(member);
                foreach (var item in navigation.RefrenceColumns)
                {
                    if (!item.Is<ColumnExpression>())
                    {
                        continue;
                    }

                    var column = item.As<ColumnExpression>();
                    var index = -1;
                    if (column.FromTable == rootQuery)
                    {
                        index = QueryUtils.FindProjectionOrdinal(rootQuery, item);
                        context.ProjectionMappingProvider.Map(item, rootQuery, index);
                    }

                    if (index == -1)
                    {
                        throw new Exception("");
                    }
                }

                var binding = CreateIncludePropertyBinding(member, context as QueryContext, context.ProjectionMappingProvider);
                entityBindings.Add(binding);
            }

            var memInit = Expression.MemberInit(entityNew, entityBindings);
            if (!fromTable.Type.IsCollection())
            {
                return memInit;
            }
            else
            {
                //属性为集合 简单处理为List<T>
                var constructor = typeof(List<>).MakeGenericType(entityType).GetConstructor(Type.EmptyTypes);
                return Expression.ListInit(Expression.New(constructor), memInit);
            }
        }

        public MemberBinding CreateIncludePropertyBinding(MemberInfo memberInfo, QueryContext queryContext, IEntityProjectionMappingProvider mappingProvider)
        {
            var innerQuery = queryContext.PropertyNavigationContext.GetNavigation(memberInfo).RefrenceQuery;
            BuildResult(innerQuery, queryContext);

            var propertyElementType = memberInfo.GetMemberInfoType().GetCollectionElementType();
            var propertyEnumerableType = typeof(EntityEnumerable<>).MakeGenericType(propertyElementType);

            var newPropertyEnumearbles = Expression.Convert(
                    Expression.New(propertyEnumerableType.GetConstructor(new Type[] { typeof(Expression), typeof(EntityProjectionMappingProvider), typeof(QueryContext) }),
                    Expression.Constant(innerQuery), Expression.Constant(mappingProvider), Expression.Constant(queryContext)),
                    propertyEnumerableType
                );

            var contextInstance = Expression.Constant(queryContext);
            var propertyInstnance = Expression.Constant(memberInfo);
            var getEnumerables = Expression.Call(null, GetIncludeMemberInstanceMethodInfo, contextInstance, propertyInstnance);
            var setEnumerables = Expression.Call(null, SetIncludeMemberInstanceMethodInfo, contextInstance, propertyInstnance, newPropertyEnumearbles);

            var target = Expression.Label(propertyEnumerableType);
            var variable = Expression.Variable(propertyEnumerableType);

            var isNull = Expression.Equal(getEnumerables, Expression.Constant(null));

            var condtion = Expression.IfThen(isNull, setEnumerables);
            var setLocal = Expression.Assign(variable, Expression.Convert(getEnumerables, newPropertyEnumearbles.Type));

            var ret = Expression.Return(target, variable, propertyEnumerableType);
            var label = Expression.Label(target, variable);

            var block = Expression.Block(new[] { variable }, condtion, setLocal, ret, label);

            return Expression.Bind(memberInfo, block);
        }

        public static object GetIncludeMemberInstance(QueryContext queryContext, MemberInfo member)
        {
            if (queryContext.SubQueryInstance.TryGetValue(member, out object instance))
            {
                return instance;
            }

            return null;
        }

        public static void SetIncludeMemberInstance(QueryContext queryContext, MemberInfo member, object instance)
        {
            queryContext.SubQueryInstance[member] = instance;
        }


        public static MethodInfo GetIncludeMemberInstanceMethodInfo = typeof(LinqExpressionTanslator).GetMethod(nameof(GetIncludeMemberInstance));

        public static MethodInfo SetIncludeMemberInstanceMethodInfo = typeof(LinqExpressionTanslator).GetMethod(nameof(SetIncludeMemberInstance));

        public Expression Translate(Expression linqExpression)
        {
            return Build(linqExpression, Context as QueryContext);
        }

        public IQueryContext Context { get; }

        public LinqExpressionTanslator(IQueryContext context)
        {
            Context = context;
        }

        public LinqExpressionTanslator()
        {

        }
    }
}