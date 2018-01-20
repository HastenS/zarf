﻿using Zarf.Generators;
using Zarf.Core;
using Zarf.SqlServer.Builders;

namespace Zarf.SqlServer.Core
{
    public class SqlServerDbContextParts : IDbContextParts
    {
        public ISQLGenerator CommandTextBuilder { get; }

        public IDbEntityCommandFacotry EntityCommandFacotry { get; }

        public IDbEntityConnectionFacotry EntityConnectionFacotry { get; }

        public IDbEntityConnection EntityConnection { get; }

        public string ConnectionString { get; }

        public SqlServerDbContextParts(string connectionString)
        {
            CommandTextBuilder = new SqlServerGenerator();
            EntityConnectionFacotry = new SqlServerDbEntityConnectionFacotry(connectionString);
            EntityCommandFacotry = new SqlServerDbEntityCommandFactory(EntityConnectionFacotry);
            EntityConnection = EntityConnectionFacotry.Create();
            ConnectionString = connectionString;
        }
    }
}
