﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Zarf.Entities;
using Zarf.Extensions;
using Zarf.Mapping;
using Zarf.Query.Expressions;
using Zarf.SqlServer.Builders;

namespace Zarf
{
    public class SqlServerDbContext : DbContext
    {
        public override void Add<TEntity>(TEntity entity)
            => Add(entity, null);

        public override int Update<TEntity>(TEntity entity)
        {
            var eType = typeof(TEntity);
            var typeDescriptor = EntityTypeDescriptorFactory.Factory.Create(eType);
            var dbParams = new List<DbParameter>();
            var dbColumns = new List<string>();
            var byKey = "";
            DbParameter byKeyValue = null;

            foreach (var item in typeDescriptor.GetExpandMembers())
            {
                if (item.Name.ToLower() == "id")
                {
                    byKey = item.Name;
                    byKeyValue = new DbParameter("@" + item.Name, GetMemberValue(entity, item));
                    continue;
                }

                dbColumns.Add(item.Name);
                dbParams.Add(new DbParameter("@" + item.Name, GetMemberValue(entity, item)));
            }

            var update = new UpdateExpression(eType.ToTable(), dbParams, dbColumns, byKey, byKeyValue);
            var sql = new SqlServerTextBuilder().Build(update);
            var dbCommand = new DbCommand(string.Empty);
            return (int)dbCommand.ExecuteScalar(sql, dbParams.ToArray());
        }

        public override int Delete<TEntity>(TEntity entity)
        {
            var eType = typeof(TEntity);
            var typeDescriptor = EntityTypeDescriptorFactory.Factory.Create(eType);
            var byKey = "";
            DbParameter byKeyValue = null;

            foreach (var item in typeDescriptor.GetExpandMembers())
            {
                if (item.Name.ToLower() == "id")
                {
                    byKey = item.Name;
                    byKeyValue = new DbParameter("@" + item.Name, GetMemberValue(entity, item));
                    break;
                }
            }

            var delete = new DeleteExpression(eType.ToTable(), byKey, byKeyValue);
            var sql = new SqlServerTextBuilder().Build(delete);
            var dbCommand = new DbCommand(string.Empty);
            return (int)dbCommand.ExecuteScalar(sql, byKeyValue);
        }

        public override void AddRange<TEntity>(IEnumerable<TEntity> entities)
        {
            throw new NotImplementedException();
        }

        public override int AddRange<TEntity>(IEnumerable<TEntity> entities, Expression<Func<TEntity, bool>> predicate)
        {
            throw new NotImplementedException();
        }

        public override int Add<TEntity>(TEntity entity, Expression<Func<TEntity, bool>> predicate)
        {
            var eType = typeof(TEntity);
            var typeDescriptor = EntityTypeDescriptorFactory.Factory.Create(eType);
            var table = eType.ToTable();
            var dbParams = new List<DbParameter>();
            var dbColumns = new List<string>();
            MemberInfo autoIncrement = null;

            foreach (var item in typeDescriptor.GetExpandMembers())
            {
                if (item.GetCustomAttribute<AutoIncrementAttribute>() != null)
                {
                    autoIncrement = item;
                    continue;
                }

                dbColumns.Add(item.Name);
                dbParams.Add(new DbParameter("@" + item.Name, GetMemberValue(entity, item)));
            }

            var insert = new InsertExpression(table, dbParams, dbColumns, autoIncrement != null);
            var sql = new SqlServerTextBuilder().Build(insert);
            var dbCommand = new DbCommand(string.Empty);
            if (autoIncrement == null)
            {
                dbCommand.ExecuteNonQuery(sql, dbParams.ToArray());
            }
            else
            {
                var id = dbCommand.ExecuteScalar(sql, dbParams.ToArray());
                autoIncrement.As<PropertyInfo>().SetValue(entity, int.Parse(id.ToString()));
            }

            return 0;
        }

        public override int Update<TEntity>(TEntity entity, Expression<Func<TEntity, bool>> predicate)
        {
            throw new NotImplementedException();
        }

        public override int Delete<TEntity>(Expression<Func<TEntity, bool>> predicate)
        {
            throw new NotImplementedException();
        }

        public override int Delete<TEntity>(TEntity entity, Expression<Func<TEntity, bool>> predicate)
        {
            throw new NotImplementedException();
        }
    }
}
