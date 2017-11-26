﻿using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Zarf.Extensions;
using Zarf.Mapping;
using Zarf.Query.Expressions;

namespace Zarf.Query.ExpressionTranslators
{
    public abstract class Translator<TExpression> : ITranslator<TExpression>, ITranslaor
    {
        public IQueryContext Context { get; }

        public IQueryCompiler Compiler { get; }

        public Translator(IQueryContext queryContext, IQueryCompiler queryCompiper)
        {
            Context = queryContext;
            Compiler = queryCompiper;
        }

        public abstract Expression Translate(TExpression query);

        public Expression Translate(Expression query)
            => Translate(query.Cast<TExpression>());

        protected void MapQuerySource(ParameterExpression parameter, QueryExpression query)
        {
            Context.QuerySourceProvider.AddSource(parameter, query);
        }

        protected TNodeType GetCompiledExpression<TNodeType>(Expression exp)
            where TNodeType : Expression
        {
            return Compiler.Compile(exp) as TNodeType;
        }

        protected Expression GetCompiledExpression(Expression exp)
        {
            return GetCompiledExpression<Expression>(exp);
        }

        protected List<ParameterExpression> GetParameters(Expression lambda)
        {
            return lambda.UnWrap().As<LambdaExpression>().Parameters.ToList();
        }

        protected List<ColumnDescriptor> GetColumns(Expression exp)
        {
            return Context.ProjectionScanner.Scan(exp);
        }
    }
}
