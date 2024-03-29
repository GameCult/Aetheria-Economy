














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

    public partial class Distance : ReqlExpr {

    
    
    
/// <summary>
/// <para>Compute the distance between a point and another geometry object. At least one of the geometry objects specified must be a point.</para>
/// </summary>
/// <example><para>Example: Compute the distance between two points on the Earth in kilometers.</para>
/// <code>var point1 = r.point(-122.423246,37.779388);
/// var point2 = r.point(-117.220406,32.719464);
/// r.distance(point1, point2, {unit: 'km'}).run(conn, callback);
/// // result returned to callback
/// 734.1252496021841
/// </code></example>
        public Distance (object arg) : this(new Arguments(arg), null) {
        }
/// <summary>
/// <para>Compute the distance between a point and another geometry object. At least one of the geometry objects specified must be a point.</para>
/// </summary>
/// <example><para>Example: Compute the distance between two points on the Earth in kilometers.</para>
/// <code>var point1 = r.point(-122.423246,37.779388);
/// var point2 = r.point(-117.220406,32.719464);
/// r.distance(point1, point2, {unit: 'km'}).run(conn, callback);
/// // result returned to callback
/// 734.1252496021841
/// </code></example>
        public Distance (Arguments args) : this(args, null) {
        }
/// <summary>
/// <para>Compute the distance between a point and another geometry object. At least one of the geometry objects specified must be a point.</para>
/// </summary>
/// <example><para>Example: Compute the distance between two points on the Earth in kilometers.</para>
/// <code>var point1 = r.point(-122.423246,37.779388);
/// var point2 = r.point(-117.220406,32.719464);
/// r.distance(point1, point2, {unit: 'km'}).run(conn, callback);
/// // result returned to callback
/// 734.1252496021841
/// </code></example>
        public Distance (Arguments args, OptArgs optargs)
         : base(TermType.DISTANCE, args, optargs) {
        }


    



    
///<summary>
/// "geo_system": "E_GEO_SYSTEM",
///  "unit": "E_UNIT"
///</summary>
        public Distance this[object optArgs] {
            get
            {
                var newOptArgs = OptArgs.FromMap(this.OptArgs).With(optArgs);
        
                return new Distance (this.Args, newOptArgs);
            }
        }
        
///<summary>
/// "geo_system": "E_GEO_SYSTEM",
///  "unit": "E_UNIT"
///</summary>
    public Distance this[OptArgs optArgs] {
        get
        {
            var newOptArgs = OptArgs.FromMap(this.OptArgs).With(optArgs);
    
            return new Distance (this.Args, newOptArgs);
        }
    }
    
///<summary>
/// "geo_system": "E_GEO_SYSTEM",
///  "unit": "E_UNIT"
///</summary>
        public Distance OptArg(string key, object val){
            
            var newOptArgs = OptArgs.FromMap(this.OptArgs).With(key, val);
        
            return new Distance (this.Args, newOptArgs);
        }
        internal Distance optArg(string key, object val){
        
            return this.OptArg(key, val);
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
