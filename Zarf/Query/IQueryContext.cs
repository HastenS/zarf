﻿using Zarf.Mapping;
using System.Linq.Expressions;
using Zarf.Query.Expressions;
using System;
using System.Reflection;
using System.Collections.Generic;
using Zarf.Core;

namespace Zarf.Query
{
    public interface IQueryContext
    {
        IEntityMemberSourceMappingProvider EntityMemberMappingProvider { get; }

        IPropertyNavigationContext PropertyNavigationContext { get; }

        IQuerySourceProvider QuerySourceProvider { get; }

        IProjectionScanner ProjectionScanner { get; }

        IEntityProjectionMappingProvider ProjectionMappingProvider { get; }

        IAliasGenerator Alias { get; }

        QueryExpression UpdateRefrenceSource(QueryExpression query);

        IMemberValueCache MemberValueCache { get; }

        IDbContextParts DbContextParts { get; }
    }
}
