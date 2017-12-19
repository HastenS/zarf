﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Zarf.Extensions;
using Zarf.Query.Expressions;

namespace Zarf.Query.ExpressionTranslators.Methods
{
    public class ExceptTranslator : Translator<MethodCallExpression>
    {
        public static IEnumerable<MethodInfo> SupprotedMethods { get; }

        static ExceptTranslator()
        {
            SupprotedMethods = ReflectionUtil.QueryableMethods.Where(item => item.Name == "Except");
        }

        public ExceptTranslator(IQueryContext queryContext, IQueryCompiler queryCompiper) : base(queryContext, queryCompiper)
        {
        }

        public override Expression Translate(MethodCallExpression methodCall)
        {
            var query = GetCompiledExpression<QueryExpression>(methodCall.Arguments[0]);
            var setsQuery = GetCompiledExpression<QueryExpression>(methodCall.Arguments[1]);

            Utils.CheckNull(query, "Query Expression");
            Utils.CheckNull(setsQuery, "Except Query Expression");

            //if (setsQuery.Columns.Count == 0)
            //{
            //    //setsQuery.AddColumns(GetColumns(setsQuery));
            //}

            query.Sets.Add(new ExceptExpression(setsQuery));
            return query;
        }
    }
}
