














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

    public partial class Reduce : ReqlExpr {

    
    
    
/// <summary>
/// <para>Produce a single value from a sequence through repeated application of a reduction function.</para>
/// </summary>
/// <example><para>Example: Return the number of documents in the table <code>posts</code>.</para>
/// <code>r.table("posts").map(function(doc) {
///     return 1;
/// }).reduce(function(left, right) {
///     return left.add(right);
/// }).default(0).run(conn, callback);
/// </code>
/// <para>A shorter way to execute this query is to use <a href="/api/javascript/count">count</a>.</para></example>
        public Reduce (object arg) : this(new Arguments(arg), null) {
        }
/// <summary>
/// <para>Produce a single value from a sequence through repeated application of a reduction function.</para>
/// </summary>
/// <example><para>Example: Return the number of documents in the table <code>posts</code>.</para>
/// <code>r.table("posts").map(function(doc) {
///     return 1;
/// }).reduce(function(left, right) {
///     return left.add(right);
/// }).default(0).run(conn, callback);
/// </code>
/// <para>A shorter way to execute this query is to use <a href="/api/javascript/count">count</a>.</para></example>
        public Reduce (Arguments args) : this(args, null) {
        }
/// <summary>
/// <para>Produce a single value from a sequence through repeated application of a reduction function.</para>
/// </summary>
/// <example><para>Example: Return the number of documents in the table <code>posts</code>.</para>
/// <code>r.table("posts").map(function(doc) {
///     return 1;
/// }).reduce(function(left, right) {
///     return left.add(right);
/// }).default(0).run(conn, callback);
/// </code>
/// <para>A shorter way to execute this query is to use <a href="/api/javascript/count">count</a>.</para></example>
        public Reduce (Arguments args, OptArgs optargs)
         : base(TermType.REDUCE, args, optargs) {
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
