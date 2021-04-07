/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RethinkDb.Driver;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Net;

public static class RethinkConnection
{
    public static RethinkDB R = RethinkDB.R;
    
    public static RethinkQueryStatus RethinkConnect(CultCache cache, string connectionString, string dbName, bool syncLocalChanges = true)
    {
        // Add Unity.Mathematics serialization support to RethinkDB Driver
        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            Converters = new List<JsonConverter>
            {
                new MathJsonConverter(),
                Converter.DateTimeConverter,
                Converter.BinaryConverter,
                Converter.GroupingConverter,
                Converter.PocoExprConverter
            }
        };

        var syncTables = typeof(DatabaseEntry).GetAllChildClasses()
            .Select(t => t.GetCustomAttribute<RethinkTableAttribute>()?.TableName ?? "Default").Distinct();

        var connectionStringDomainLength = connectionString.IndexOf(':');
        if (connectionStringDomainLength < 1)
            throw new ArgumentException("Illegal Connection String: must include port!");
        
        var portString = connectionString.Substring(connectionStringDomainLength + 1);
        if (!int.TryParse(portString, out var port))
            throw new ArgumentException($"Illegal connection string! \"{portString}\" is not a valid port number!");
        
        var connection = R.Connection()
            .Hostname(connectionString.Substring(0,connectionStringDomainLength))
            .Port(port).Timeout(60).Connect();

        var tables = R.Db(dbName).TableList().RunAtom<string[]>(connection);

        foreach (var st in syncTables.Where(st => !tables.Contains(st)))
            R.Db(dbName).TableCreate(st).RunNoReply(connection);

        if (syncLocalChanges)
        {
            // When entries are changed locally, push the changes to RethinkDB
            cache.OnDataUpdateLocal += async entry =>
            {
                var table = entry.GetType().GetCustomAttribute<RethinkTableAttribute>()?.TableName ?? "Other";
                var result = await R
                    .Db(dbName)
                    .Table(table)
                    .Get(entry.ID)
                    .Replace(entry)
                    .RunAsync(connection);
            };
        
            cache.OnDataInsertLocal += async entry =>
            {
                var table = entry.GetType().GetCustomAttribute<RethinkTableAttribute>()?.TableName ?? "Other";
                var result = await R
                    .Db(dbName)
                    .Table(table)
                    .Insert(entry)
                    .RunAsync(connection);
            };

            cache.OnDataRemoveLocal += async entry =>
            {
                var table = entry.GetType().GetCustomAttribute<RethinkTableAttribute>()?.TableName ?? "Other";
                var result = await R
                    .Db(dbName)
                    .Table(table)
                    .Get(entry.ID)
                    .Delete()
                    .RunAsync(connection);
            };
        }

        var status = new RethinkQueryStatus();

        foreach (var table in syncTables)
        {
            // Get entries from RethinkDB
            Task.Run(async () =>
            {
                status.TotalEntries += R
                    .Db(dbName)
                    .Table(table)
                    .Count().RunAtom<int>(connection);
            
                var result = await R
                    .Db(dbName)
                    .Table(table)
                    .RunCursorAsync<DatabaseEntry>(connection);
                while (await result.MoveNextAsync())
                {
                    var entry = result.Current;
                    cache.Add(entry, true);
                    status.RetrievedEntries++;
                }
            }).WrapAwait();
            
            // Subscribe to changes from RethinkDB
            Task.Run(async () =>
            {
                var result = await R
                    .Db(dbName)
                    .Table(table)
                    .Changes()
                    .RunChangesAsync<DatabaseEntry>(connection);
                while (await result.MoveNextAsync())
                {
                    var change = result.Current;
                    cache.Add(change.NewValue, true);
                }
            }).WrapAwait();
        }

        return status;
    }
}

public static class AsyncAwaitExtensions
{
    public static async void WrapAwait(this Task task)
    {
        await task;
    }
}

public class RethinkQueryStatus
{
    public int RetrievedEntries;
    public int TotalEntries;
}