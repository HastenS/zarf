﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Zarf.Extensions;
using Zarf.Query.Expressions;
using Zarf.Mapping;

namespace Zarf.Query.ExpressionVisitors
{
    //Projection 不能包含Member
    //一个Member中包含一个简单类型,一个简单类型后续方法调用包含一个复杂类型
    public class ProjectionExpressionVisitor : ExpressionVisitor, IProjectionScanner
    {
        private List<ColumnDescriptor> _list;

        public List<ColumnDescriptor> Scan(Expression node)
        {
            _list = new List<ColumnDescriptor>();
            Visit(node);
            return _list.ToList();
        }

        public List<ColumnDescriptor> Scan(Func<Expression, Expression> preHandle, Expression node)
        {
            return Scan(preHandle(node));
        }

        protected override Expression VisitExtension(Expression node)
        {
            if (node.Is<FromTableExpression>())
            {
                foreach (var item in node
                 .As<FromTableExpression>()
                 .GenerateColumns()
                 .OfType<ColumnExpression>())
                {
                    AddProjection(item.Member, item);
                }
            }
            else if (node.Is<ColumnExpression>())
            {
                AddProjection(null, node);
            }

            return node;
        }

        protected override Expression VisitMemberInit(MemberInitExpression memberInit)
        {
            Visit(memberInit.NewExpression);

            foreach (var binding in memberInit.Bindings.OfType<MemberAssignment>())
            {
                AddProjection(binding.Member, binding.Expression);
            }

            return memberInit;
        }

        protected override Expression VisitNew(NewExpression newExp)
        {
            if (newExp.Arguments == null || newExp.Arguments.Count == 0)
            {
                return newExp;
            }

            for (var i = 0; i < newExp.Arguments.Count; i++)
            {
                if (ReflectionUtil.SimpleTypes.Contains(newExp.Members[i].GetPropertyType()))
                {
                    AddProjection(newExp.Members[i], newExp.Arguments[i]);
                }
            }

            return newExp;
        }

        private void AddProjection(MemberInfo member, Expression node)
        {
            if (node.NodeType != ExpressionType.Extension)
            {
                Visit(node);
                return;
            }

            if (node.Is<FromTableExpression>())
            {
                foreach (var item in node
                    .As<FromTableExpression>()
                    .GenerateColumns()
                    .OfType<ColumnExpression>())
                {
                    AddProjection(item.Member, item);
                }

                return;
            }

            if (node.Is<ColumnExpression>())
            {
                _list.Add(new ColumnDescriptor()
                {
                    Member = member,
                    Expression = node,
                    Ordinal = _list.Count
                });
            }
        }
    }
}
