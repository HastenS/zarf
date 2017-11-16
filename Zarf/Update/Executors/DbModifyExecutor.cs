﻿using System.Collections.Generic;
using System.Linq;
using Zarf.Builders;
using Zarf.Core;
using Zarf.Update.Expressions;

namespace Zarf.Update.Executors
{
    public class DbModifyExecutor : IDbModifyExecutor
    {
        public IDbCommandFacotry CommandFacotry { get; }

        public ISqlTextBuilder SqlBuilder { get; }

        public DbModificationCommandGroupBuilder CommandGroupBuilder { get; }

        public DbModifyExecutor(IDbCommandFacotry commandFacotry, ISqlTextBuilder sqlBuilder, IEntityTracker tracker)
        {
            CommandFacotry = commandFacotry;
            SqlBuilder = sqlBuilder;
            CommandGroupBuilder = new DbModificationCommandGroupBuilder(tracker);
        }

        public virtual int Execute(IEnumerable<EntityEntry> entries)
        {
            var commandGroups = CommandGroupBuilder.Build(entries);
            var dbCommand = CommandFacotry.Create();

            foreach (var commandGroup in commandGroups)
            {
                if (commandGroup.Commands.Count == 1)
                {
                    var modifyCommand = commandGroup.Commands.FirstOrDefault();
                    if (modifyCommand.Entry.State == EntityState.Insert && modifyCommand.Entry.AutoIncrementProperty != null)
                    {
                        var id = dbCommand.ExecuteScalar<int>(BuildCommandText(commandGroup), commandGroup.Parameters.ToArray());
                        modifyCommand.Entry.AutoIncrementProperty.SetValue(modifyCommand.Entry.Entity, id);
                        continue;
                    }
                }

                dbCommand.ExecuteNonQuery(BuildCommandText(commandGroup), commandGroup.Parameters.ToArray());
            }

            return 0;
        }

        public string BuildCommandText(DbModificationCommandGroup commandGroup)
        {
            return SqlBuilder.Build(DbStoreExpressionFacotry.Default.Create(commandGroup));
        }
    }
}
