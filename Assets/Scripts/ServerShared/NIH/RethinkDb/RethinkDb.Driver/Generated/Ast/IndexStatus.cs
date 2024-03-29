














//AUTOGENERATED, DO NOTMODIFY.
//Do not edit this file directly.

#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
// ReSharper disable CheckNamespace

using System;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Proto;
using System.Collections;
using System.Collections.Generic;


namespace RethinkDb.Driver.Ast {

    public partial class IndexStatus : ReqlExpr {

    
    
    
/// <summary>
/// <para>Get the status of the specified indexes on this table, or the status
/// of all indexes on this table if no indexes are specified.</para>
/// </summary>
/// <example><para>Example: Get the status of all the indexes on <code>test</code>:</para>
/// <code>r.table('test').indexStatus().run(conn, callback)
/// </code></example>
        public IndexStatus (object arg) : this(new Arguments(arg), null) {
        }
/// <summary>
/// <para>Get the status of the specified indexes on this table, or the status
/// of all indexes on this table if no indexes are specified.</para>
/// </summary>
/// <example><para>Example: Get the status of all the indexes on <code>test</code>:</para>
/// <code>r.table('test').indexStatus().run(conn, callback)
/// </code></example>
        public IndexStatus (Arguments args) : this(args, null) {
        }
/// <summary>
/// <para>Get the status of the specified indexes on this table, or the status
/// of all indexes on this table if no indexes are specified.</para>
/// </summary>
/// <example><para>Example: Get the status of all the indexes on <code>test</code>:</para>
/// <code>r.table('test').indexStatus().run(conn, callback)
/// </code></example>
        public IndexStatus (Arguments args, OptArgs optargs)
         : base(TermType.INDEX_STATUS, args, optargs) {
        }


    



    


    

    
        /// <summary>
        /// Get a single field from an object. If called on a sequence, gets that field from every object in the sequence, skipping objects that lack it.
        /// </summary>
        /// <param name="bracket"></param>
        public new Bracket this[string bracket] => base[bracket];
        
        /// <summary>
        /// Get the nth element of a sequence, counting from zero. If the argument is negative, count from the last element.
        /// </summary>
        /// <param name="bracket"></param>
        /// <returns></returns>
        public new Bracket this[int bracket] => base[bracket];


    

    


    
    }
}
