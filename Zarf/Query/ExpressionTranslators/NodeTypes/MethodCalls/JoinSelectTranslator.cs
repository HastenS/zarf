﻿using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Zarf.Entities;
using Zarf.Extensions;
using Zarf.Query.Expressions;
using Zarf.Query.ExpressionVisitors;

namespace Zarf.Query.ExpressionTranslators.Methods
{
    /// <summary>
    /// Select Query
    /// </summary>
    public class JoinSelectTranslator : Translator<MethodCallExpression>
    {
        public static IEnumerable<MethodInfo> SupprotedMethods { get; }

        static JoinSelectTranslator()
        {
            SupprotedMethods = new[] { ReflectionUtil.JoinSelect };
        }

        public JoinSelectTranslator(IQueryContext queryContext, IQueryCompiler queryCompiper) : base(queryContext, queryCompiper)
        {

        }

        public override Expression Translate(MethodCallExpression methodCall)
        {
            var query = GetCompiledExpression<QueryExpression>(methodCall.Arguments[0]);
            var modelType = methodCall.Method.ReturnType;
            var parameters = methodCall.Arguments[1].GetParameters().ToList();
            var modelExpression = new ModelRefrenceExpressionVisitor(Context, query, parameters[0]).Visit(methodCall.Arguments[1]);

            query.QueryModel = new QueryEntityModel(query, modelExpression, modelType, query.QueryModel);

            for (var i = 0; i < parameters.Count; i++)
            {
                if (i != 0)
                {
                    Context.QueryMapper.MapQuery(parameters[i], query.Joins[i - 1].Query);
                    Context.QueryModelMapper.MapQueryModel(parameters[i], query.Joins[i - 1].Query.QueryModel);
                    continue;
                }

                Context.QueryMapper.MapQuery(parameters[i], query);
                Context.QueryModelMapper.MapQueryModel(parameters[i], query.QueryModel);
            }

            Utils.CheckNull(query, "query");

            CreateProjection(query, modelExpression);

            if (query.QueryModel.Model.Is<ConstantExpression>())
            {
                query.AddProjection(new AliasExpression(Context.Alias.GetNewColumn(), query.QueryModel.Model, methodCall.Arguments[1]));
            }

            return query;
        }

        protected void CreateProjection(QueryExpression query, Expression modelExpression)
        {
            modelExpression = new ProjectionExpressionVisitor(query, Context).Visit(modelExpression);

            new ResultExpressionVisitor(Context, query).Visit(modelExpression);
        }
    }
}