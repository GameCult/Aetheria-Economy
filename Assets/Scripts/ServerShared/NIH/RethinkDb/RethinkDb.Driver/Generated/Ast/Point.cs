














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

    public partial class Point : ReqlExpr {

    
    
    
/// <summary>
/// <para>Construct a geometry object of type Point. The point is specified by two floating point numbers, the longitude (-180 to 180) and latitude (-90 to 90) of the point on a perfect sphere. See <a href="/docs/geo-support/">Geospatial support</a> for more information on ReQL's coordinate system.</para>
/// </summary>
/// <example><para>Example: Define a point.</para>
/// <code>r.table('geo').insert({
///     id: 1,
///     name: 'San Francisco',
///     location: r.point(-122.423246,37.779388)
/// }).run(conn, callback);
/// </code></example>
        public Point (object arg) : this(new Arguments(arg), null) {
        }
/// <summary>
/// <para>Construct a geometry object of type Point. The point is specified by two floating point numbers, the longitude (-180 to 180) and latitude (-90 to 90) of the point on a perfect sphere. See <a href="/docs/geo-support/">Geospatial support</a> for more information on ReQL's coordinate system.</para>
/// </summary>
/// <example><para>Example: Define a point.</para>
/// <code>r.table('geo').insert({
///     id: 1,
///     name: 'San Francisco',
///     location: r.point(-122.423246,37.779388)
/// }).run(conn, callback);
/// </code></example>
        public Point (Arguments args) : this(args, null) {
        }
/// <summary>
/// <para>Construct a geometry object of type Point. The point is specified by two floating point numbers, the longitude (-180 to 180) and latitude (-90 to 90) of the point on a perfect sphere. See <a href="/docs/geo-support/">Geospatial support</a> for more information on ReQL's coordinate system.</para>
/// </summary>
/// <example><para>Example: Define a point.</para>
/// <code>r.table('geo').insert({
///     id: 1,
///     name: 'San Francisco',
///     location: r.point(-122.423246,37.779388)
/// }).run(conn, callback);
/// </code></example>
        public Point (Arguments args, OptArgs optargs)
         : base(TermType.POINT, args, optargs) {
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
