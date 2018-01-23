﻿using Microsoft.Extensions.DependencyInjection;
using Zarf.Core;
using Zarf.Generators;
using Zarf.Sqlite.Generators;

namespace Zarf.Sqlite
{
    public static class SqliteExtensions
    {
        public static IDbService UseSqlite(this IServiceCollection serviceCollection, string connectionString)
        {
            return new SqliteDbServiceBuilder().BuildService(connectionString, serviceCollection);
        }

        public static IDbService UseSqlite(this IDbServiceBuilder serviceBuilder, string connectionString)
        {
            return new ServiceCollection().AddSqlite().UseSqlite(connectionString);
        }

        internal static IServiceCollection AddSqlite(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<ISQLGenerator, SqliteGenerator>();

            serviceCollection.AddScoped<IDbEntityCommandFacotry>(
                p => new SqliteDbEntityCommandFactory(p.GetService<IDbEntityConnectionFacotry>()));

            serviceCollection.AddSingleton<IDbServiceBuilder, SqliteDbServiceBuilder>();

            return serviceCollection.AddZarf();
        }
    }
}
