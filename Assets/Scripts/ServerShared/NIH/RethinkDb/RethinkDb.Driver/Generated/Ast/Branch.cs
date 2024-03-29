














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

    public partial class Branch : ReqlExpr {

    
    
    
/// <summary>
/// <para>Perform a branching conditional equivalent to <code>if-then-else</code>.</para>
/// <para>The <code>branch</code> command takes 2n+1 arguments: pairs of conditional expressions and commands to be executed if the conditionals return any value but <code>false</code> or <code>null</code> (i.e., "truthy" values), with a final "else" command to be evaluated if all of the conditionals are <code>false</code> or <code>null</code>.</para>
/// </summary>
/// <example><para>Example: Test the value of x.</para>
/// <code>var x = 10;
/// r.branch(r.expr(x).gt(5), 'big', 'small').run(conn, callback);
/// // Result passed to callback
/// "big"
/// </code></example>
        public Branch (object arg) : this(new Arguments(arg), null) {
        }
/// <summary>
/// <para>Perform a branching conditional equivalent to <code>if-then-else</code>.</para>
/// <para>The <code>branch</code> command takes 2n+1 arguments: pairs of conditional expressions and commands to be executed if the conditionals return any value but <code>false</code> or <code>null</code> (i.e., "truthy" values), with a final "else" command to be evaluated if all of the conditionals are <code>false</code> or <code>null</code>.</para>
/// </summary>
/// <example><para>Example: Test the value of x.</para>
/// <code>var x = 10;
/// r.branch(r.expr(x).gt(5), 'big', 'small').run(conn, callback);
/// // Result passed to callback
/// "big"
/// </code></example>
        public Branch (Arguments args) : this(args, null) {
        }
/// <summary>
/// <para>Perform a branching conditional equivalent to <code>if-then-else</code>.</para>
/// <para>The <code>branch</code> command takes 2n+1 arguments: pairs of conditional expressions and commands to be executed if the conditionals return any value but <code>false</code> or <code>null</code> (i.e., "truthy" values), with a final "else" command to be evaluated if all of the conditionals are <code>false</code> or <code>null</code>.</para>
/// </summary>
/// <example><para>Example: Test the value of x.</para>
/// <code>var x = 10;
/// r.branch(r.expr(x).gt(5), 'big', 'small').run(conn, callback);
/// // Result passed to callback
/// "big"
/// </code></example>
        public Branch (Arguments args, OptArgs optargs)
         : base(TermType.BRANCH, args, optargs) {
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
