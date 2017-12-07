using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Zarf.Entities;
using Zarf.Extensions;
using Zarf.Mapping;
using Zarf.Query.Internals;

namespace Zarf.Query.Expressions
{
    public class QueryExpression : Expression
    {
        protected Type TypeOfExpression { get; set; }

        public Table Table { get; set; }

        public string Alias { get; }

        public override Type Type => TypeOfExpression;

        public override ExpressionType NodeType => ExpressionType.Extension;

        public List<ColumnDescriptor> Columns { get; }

        public List<JoinExpression> Joins { get; }

        public List<SetsExpression> Sets { get; }

        public List<OrderExpression> Orders { get; }

        public List<GroupExpression> Groups { get; }

        public WhereExperssion Where { get; set; }

        public bool IsDistinct { get; set; }

        public int Limit { get; set; }

        public SkipExpression Offset { get; set; }

        public bool DefaultIfEmpty { get; set; }

        public QueryExpression SubQuery { get; protected set; }

        public QueryExpression Container { get; protected set; }

        public EntityResult Result { get; set; }

        public IQueryColumnCaching ColumnCaching { get; }

        protected HashSet<string> ColumnAliases { get; }

        public QueryExpression(Type typeOfEntity, IQueryColumnCaching columnCaching, string alias = "")
        {
            Sets = new List<SetsExpression>();
            Joins = new List<JoinExpression>();
            Orders = new List<OrderExpression>();
            Groups = new List<GroupExpression>();
            Columns = new List<ColumnDescriptor>();
            ColumnAliases = new HashSet<string>();
            TypeOfExpression = typeOfEntity;
            Table = typeOfEntity.ToTable();
            ColumnCaching = columnCaching;
            Alias = alias;
        }

        public QueryExpression PushDownSubQuery(string alias, Func<QueryExpression, QueryExpression> subQueryHandle = null)
        {
            var query = new QueryExpression(Type, ColumnCaching, alias)
            {
                SubQuery = this,
                Table = null,
                DefaultIfEmpty = DefaultIfEmpty,
            };

            DefaultIfEmpty = false;
            Container = query;
            query.Result = query.SubQuery.Result;
            return subQueryHandle != null ? subQueryHandle(query) : query;
        }

        public void AddJoin(JoinExpression joinQuery)
        {
            joinQuery.Query.Container = this;
            Joins.Add(joinQuery);
        }

        public void AddColumns(IEnumerable<ColumnDescriptor> columns)
        {
            foreach (var item in columns)
            {
                var col = item.Expression.As<ColumnExpression>()?.Clone();
                if (col == null)
                {
                    continue;
                }
     
                while (ColumnAliases.Contains(col.Alias))
                {
                    col.Alias = col.Alias + "_1";
                }
                item.Expression = col;
                ColumnAliases.Add(col.Alias);
                ColumnCaching.AddColumn(col);
            }
          
            Columns.AddRange(columns);
        }

        public void CombineCondtion(Expression predicate)
        {
            if (predicate == null)
            {
                return;
            }

            if (Where == null)
            {
                Where = new WhereExperssion(predicate);
                return;
            }

            Where.Combine(predicate);
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
                Columns.Count == 0 &&
                Orders.Count == 0 &&
                Groups.Count == 0 &&
                Sets.Count == 0 &&
                Joins.Count == 0 &&
                Limit == 0;
        }

        public IEnumerable<ColumnExpression> GenerateTableColumns()
        {
            var typeOfEntity = TypeDescriptorCacheFactory.Factory.Create(Type);
            foreach (var memberDescriptor in typeOfEntity.MemberDescriptors)
            {
                yield return new ColumnExpression(
                    this,
                    memberDescriptor.Member,
                    memberDescriptor.Name);
            }
        }

        public void ChangeTypeOfExpression(Type typeOfExpression)
        {
            TypeOfExpression = typeOfExpression;
        }
    }
}