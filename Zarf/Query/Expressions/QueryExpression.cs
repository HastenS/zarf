using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Zarf.Entities;
using Zarf.Mapping;

namespace Zarf.Query.Expressions
{
    public class QueryExpression : FromTableExpression
    {
        /// <summary>
        /// 查询投影
        /// </summary>
        public List<ColumnDescriptor> Projections { get; }

        /// <summary>
        /// 表连接
        /// </summary>
        public List<JoinExpression> Joins { get; }

        /// <summary>
        /// 代表其他查询集合
        /// Union Except etc...
        /// </summary>
        public List<SetsExpression> Sets { get; }

        /// <summary>
        /// 排序
        /// </summary>
        public List<OrderExpression> Orders { get; }

        /// <summary>
        /// 分组
        /// </summary>
        public List<GroupExpression> Groups { get; }

        /// <summary>
        /// 条件
        /// </summary>
        public WhereExperssion Where { get; set; }

        /// <summary>
        /// 去重
        /// </summary>
        public bool IsDistinct { get; set; }

        /// <summary>
        /// 限制条数
        /// </summary>
        public int Limit { get; set; }

        /// <summary>
        /// 查询结果偏移量
        /// </summary>
        public SkipExpression Offset { get; set; }

        /// <summary>
        /// 为空时返回默认值
        /// </summary>
        public bool DefaultIfEmpty { get; set; }

        /// <summary>
        /// 表示一个子查询
        /// </summary>
        public QueryExpression SubQuery { get; protected set; }

        public EntityResult Result { get; set; }

        public QueryExpression(Type entityType, string alias = "")
            : base(entityType, alias)
        {
            Sets = new List<SetsExpression>();
            Joins = new List<JoinExpression>();
            Orders = new List<OrderExpression>();
            Groups = new List<GroupExpression>();
            Projections = new List<ColumnDescriptor>();
        }

        public QueryExpression PushDownSubQuery(string fromTableAlias, Func<QueryExpression, QueryExpression> subQueryHandle = null)
        {
            var query = new QueryExpression(Type, fromTableAlias)
            {
                SubQuery = this,
                Table = null,
                DefaultIfEmpty = DefaultIfEmpty
            };

            DefaultIfEmpty = false;
            Parent = query;
            query.Result = query.SubQuery.Result;
            return subQueryHandle != null ? subQueryHandle(query) : query;
        }

        public void AddJoin(JoinExpression table)
        {
            Joins.Add(table);
        }

        public void AddProjections(IEnumerable<ColumnDescriptor> projections)
        {
            Projections.Clear();
            Projections.AddRange(projections);
        }

        public void AddWhere(Expression predicate)
        {
            if (predicate == null)
            {
                return;
            }

            if (Where == null)
            {
                Where = new WhereExperssion(predicate);
            }
            else
            {
                Where.Combine(predicate);
            }
        }

        /// <summary>
        /// 是否一个空查询
        /// 引用一个Table
        /// </summary>
        /// <returns></returns>
        public bool IsEmptyQuery()
        {
            return
                !IsDistinct &&
                //!DefaultIfEmpty &&
                Where == null &&
                Offset == null &&
                SubQuery == null &&
                Projections.Count == 0 &&
                Orders.Count == 0 &&
                Groups.Count == 0 &&
                Sets.Count == 0 &&
                Joins.Count == 0 &&
                Limit == 0;
        }
    }
}