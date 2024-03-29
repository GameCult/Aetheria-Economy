














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

    public partial class Error : ReqlExpr {

    
    
    
/// <summary>
/// <para>Throw a runtime error. If called with no arguments inside the second argument to <code>default</code>, re-throw the current error.</para>
/// </summary>
/// <example><para>Example: Iron Man can't possibly have lost a battle:</para>
/// <code>r.table('marvel').get('IronMan').do(function(ironman) {
///     return r.branch(ironman('victories').lt(ironman('battles')),
///         r.error('impossible code path'),
///         ironman)
/// }).run(conn, callback)
/// </code></example>
        public Error (object arg) : this(new Arguments(arg), null) {
        }
/// <summary>
/// <para>Throw a runtime error. If called with no arguments inside the second argument to <code>default</code>, re-throw the current error.</para>
/// </summary>
/// <example><para>Example: Iron Man can't possibly have lost a battle:</para>
/// <code>r.table('marvel').get('IronMan').do(function(ironman) {
///     return r.branch(ironman('victories').lt(ironman('battles')),
///         r.error('impossible code path'),
///         ironman)
/// }).run(conn, callback)
/// </code></example>
        public Error (Arguments args) : this(args, null) {
        }
/// <summary>
/// <para>Throw a runtime error. If called with no arguments inside the second argument to <code>default</code>, re-throw the current error.</para>
/// </summary>
/// <example><para>Example: Iron Man can't possibly have lost a battle:</para>
/// <code>r.table('marvel').get('IronMan').do(function(ironman) {
///     return r.branch(ironman('victories').lt(ironman('battles')),
///         r.error('impossible code path'),
///         ironman)
/// }).run(conn, callback)
/// </code></example>
        public Error (Arguments args, OptArgs optargs)
         : base(TermType.ERROR, args, optargs) {
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
