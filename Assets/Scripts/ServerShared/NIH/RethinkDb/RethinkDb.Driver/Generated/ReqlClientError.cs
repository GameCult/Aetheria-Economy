



//AUTOGENERATED, DO NOTMODIFY.
//Do not edit this file directly.

#pragma warning disable 1591
// ReSharper disable CheckNamespace

using System;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver {
    public class ReqlClientError : ReqlError {


        public ReqlClientError () {
        }

        public ReqlClientError (Exception e) : this(e.Message, e) {
        }

        public ReqlClientError (string message) : base(message)
        {
        }

        public ReqlClientError (string message, Exception innerException) : base(message, innerException)
        {
        }
        
        
    }
}
