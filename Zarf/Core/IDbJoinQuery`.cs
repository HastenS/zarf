﻿using System;
using System.Linq.Expressions;
using Zarf.Entities;

namespace Zarf.Core
{
    public interface IDbJoinQuery<T1, T2>
    {
        IDbJoinQuery<T1, T2, T3> Join<T3>(IDbQuery<T3> query, Expression<Func<T1, T2, T3, bool>> predicate, JoinType joinType = JoinType.Inner);

        IDbQuery<TResult> Select<TResult>(Expression<Func<T1, T2, TResult>> selector);
    }

    public interface IDbJoinQuery<T1, T2, T3> : IDbJoinQuery<T1, T2>
    {
        IDbJoinQuery<T1, T2, T3, T4> Join<T4>(IDbQuery<T4> query, Expression<Func<T1, T2, T3, T4, bool>> predicate, JoinType joinType = JoinType.Inner);

        IDbQuery<TResult> Select<TResult>(Expression<Func<T1, T2, T3, TResult>> selector);
    }

    public interface IDbJoinQuery<T1, T2, T3, T4> : IDbJoinQuery<T1, T2, T3>
    {
        IDbJoinQuery<T1, T2, T3, T4, T5> Join<T5>(IDbQuery<T5> query, Expression<Func<T1, T2, T3, T4, T5, bool>> predicate, JoinType joinType = JoinType.Inner);

        IDbQuery<TResult> Select<TResult>(Expression<Func<T1, T2, T3, T4, TResult>> selector);
    }

    public interface IDbJoinQuery<T1, T2, T3, T4, T5> : IDbJoinQuery<T1, T2, T3, T4>
    {
        IDbJoinQuery<T1, T2, T3, T4, T5, T6> Join<T6>(IDbQuery<T6> query, Expression<Func<T1, T2, T3, T4, T5, T6, bool>> predicate, JoinType joinType = JoinType.Inner);

        IDbQuery<TResult> Select<TResult>(Expression<Func<T1, T2, T3, T4, T5, TResult>> selector);
    }

    public interface IDbJoinQuery<T1, T2, T3, T4, T5, T6> : IDbJoinQuery<T1, T2, T3, T4, T5>
    {
        IDbJoinQuery<T1, T2, T3, T4, T5, T6, T7> Join<T7>(IDbQuery<T7> query, Expression<Func<T1, T2, T3, T4, T5, T6, T7, bool>> predicate, JoinType joinType = JoinType.Inner);

        IDbQuery<TResult> Select<TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, TResult>> selector);
    }

    public interface IDbJoinQuery<T1, T2, T3, T4, T5, T6, T7> : IDbJoinQuery<T1, T2, T3, T4, T5, T6>
    {
        IDbJoinQuery<T1, T2, T3, T4, T5, T6, T7, T8> Join<T8>(IDbQuery<T8> query, Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, bool>> predicate, JoinType joinType = JoinType.Inner);

        IDbQuery<TResult> Select<TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, TResult>> selector);
    }

    public interface IDbJoinQuery<T1, T2, T3, T4, T5, T6, T7, T8> : IDbJoinQuery<T1, T2, T3, T4, T5, T6, T7>
    {
        IDbJoinQuery<T1, T2, T3, T4, T5, T6, T7, T8, T9> Join<T9>(IDbQuery<T9> query, Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, bool>> predicate, JoinType joinType = JoinType.Inner);

        IDbQuery<TResult> Select<TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>> selector);
    }

    public interface IDbJoinQuery<T1, T2, T3, T4, T5, T6, T7, T8, T9> : IDbJoinQuery<T1, T2, T3, T4, T5, T6, T7, T8>
    {
        IDbQuery<TResult> Select<TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>> selector);
    }
}
