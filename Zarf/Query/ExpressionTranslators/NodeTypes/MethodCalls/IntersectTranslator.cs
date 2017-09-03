﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Zarf.Extensions;
using Zarf.Query.Expressions;

namespace Zarf.Query.ExpressionTranslators.Methods
{
    public class IntersectTranslator : Translator<MethodCallExpression>
    {
        public static IEnumerable<MethodInfo> SupprotedMethods { get; }

        static IntersectTranslator()
        {
            SupprotedMethods = ReflectionUtil.AllQueryableMethods.Where(item => item.Name == "Intersect");
        }

        public override Expression Translate(QueryContext context, MethodCallExpression methodCall, ExpressionVisitor transformVisitor)
        {
            if (methodCall.Arguments.Count != 2)
            {
                throw new NotImplementedException("not supproted!");
            }

            var query = transformVisitor.Visit(methodCall.Arguments[0]).As<QueryExpression>();
            var setsQuery = transformVisitor.Visit(methodCall.Arguments[1]).As<QueryExpression>();

            Utils.CheckNull(query, "Query Expression");
            Utils.CheckNull(setsQuery, "Intersect Query Expression");

            query.Sets.Add(new IntersectExpression(setsQuery));

            if (setsQuery.Projections.Count == 0)
            {
                var entityType = new Mapping.EntityTypeDescriptorFactory().Create(setsQuery.Type);
                foreach (var item in entityType.GetWriteableMembers())
                {
                    setsQuery.Projections.Add(new ColumnExpression(setsQuery, item, item.Name));
                }
            }

            return query;
        }
    }
}