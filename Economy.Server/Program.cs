using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RethinkDb.Driver;

namespace ChatApp.Server
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var connection = RethinkDb.Driver.RethinkDB.R.Connection().Hostname("asgard.gamecult.games")
                .Port(RethinkDBConstants.DefaultPort).Timeout(60).Connect();
            
        }
    }
}
