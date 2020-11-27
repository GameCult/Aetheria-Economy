using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RethinkDb.Driver;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Net;
using UniRx;
using UnityEngine;

public static class RethinkConnection
{
    public static RethinkDB R = RethinkDB.R;
    
    public static RethinkQueryStatus RethinkConnect(DatabaseCache cache, string connectionString, bool syncLocalChanges = true, bool filterGalaxyData = true)
    {
        // Add Unity.Mathematics serialization support to RethinkDB Driver
        //Converter.Serializer.Converters.Add(new MathJsonConverter());
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
        
        var connection = R.Connection().Hostname(connectionString).Port(RethinkDBConstants.DefaultPort+1).Timeout(60).Connect();
        Debug.Log("Connected to RethinkDB");

        if (syncLocalChanges)
        {
            // When entries are changed locally, push the changes to RethinkDB
            cache.OnDataUpdateLocal += async entry =>
            {
                var table = entry.GetType().GetCustomAttribute<RethinkTableAttribute>()?.TableName ?? "Other";
                var result = await R
                    .Db("Aetheria")
                    .Table(table)
                    .Get(entry.ID)
                    .Replace(entry)
                    .RunAsync(connection);
                Debug.Log($"Uploaded entry to RethinkDB: {entry.ID} result: {result}");
            };
        
            cache.OnDataInsertLocal += async entry =>
            {
                var table = entry.GetType().GetCustomAttribute<RethinkTableAttribute>()?.TableName ?? "Other";
                var result = await R
                    .Db("Aetheria")
                    .Table(table)
                    .Insert(entry)
                    .RunAsync(connection);
                Debug.Log($"Inserted entry to RethinkDB: {entry.ID} result: {result}");
            };

            cache.OnDataDeleteLocal += async entry =>
            {
                var table = entry.GetType().GetCustomAttribute<RethinkTableAttribute>()?.TableName ?? "Other";
                var result = await R
                    .Db("Aetheria")
                    .Table(table)
                    .Get(entry.ID)
                    .Delete()
                    .RunAsync(connection);
                Debug.Log($"Deleted entry from RethinkDB: {entry.ID} result: {result}");
            };
        }

        var status = new RethinkQueryStatus();

        // Get all item data from RethinkDB
        Task.Run(async () =>
        {
            status.ItemsEntries = R
                .Db("Aetheria")
                .Table("Items")
                .Count().RunAtom<int>(connection);
            
            var result = await R
                .Db("Aetheria")
                .Table("Items")
                .RunCursorAsync<DatabaseEntry>(connection);
            while (await result.MoveNextAsync())
            {
                var entry = result.Current;
                //Debug.Log($"Received Items entry from RethinkDB: {entry.GetType()} {(entry as INamedEntry)?.EntryName ?? ""}:{entry.ID}");
                cache.Add(entry, true);
                status.RetrievedItems++;
            }
        }).WrapErrors();

        // Get globaldata and all galaxy map layer data from RethinkDB
        Task.Run(async () =>
        {
            ReqlAst operation = R
                .Db("Aetheria")
                .Table("Galaxy");
            if (filterGalaxyData)
            {
                var filter = ((Table) operation).Filter(o =>
                    o["$type"] == typeof(GalaxyMapLayerData).Name || 
                    o["$type"] == typeof(GlobalData).Name ||
                    o["$type"] == typeof(MegaCorporation).Name);
                status.GalaxyEntries = filter.Count().RunAtom<int>(connection);
                operation = filter;
            }
            else status.GalaxyEntries = ((Table) operation).Count().RunAtom<int>(connection);
            
            var result = await operation
                .RunCursorAsync<DatabaseEntry>(connection);
            while (await result.MoveNextAsync())
            {
                var entry = result.Current;
                //Debug.Log($"Received Galaxy entry from RethinkDB: {entry.GetType()} {(entry as INamedEntry)?.EntryName ?? ""}:{entry.ID}");
                cache.Add(entry, true);
                status.RetrievedItems++;
            }
        }).WrapErrors();

        // Subscribe to changes from RethinkDB
        Task.Run(async () =>
        {
            var result = await R
                .Db("Aetheria")
                .Table("Items")
                .Changes()
                .RunChangesAsync<DatabaseEntry>(connection);
            while (await result.MoveNextAsync())
            {
                var change = result.Current;
                if(change.OldValue == null)
                    Debug.Log($"Received change from RethinkDB (Entry Created): {change.NewValue.ID}");
                else if(change.NewValue == null)
                    Debug.Log($"Received change from RethinkDB (Entry Deleted): {change.OldValue.ID}");
                else
                    Debug.Log($"Received change from RethinkDB: {change.NewValue.ID}");
                cache.Add(change.NewValue, true);
            }
        }).WrapErrors();

        Observable.Timer(DateTimeOffset.Now, TimeSpan.FromSeconds(60)).Subscribe(_ =>
        {
            Debug.Log(R.Now().Run<DateTime>(connection).ToString() as string);
        });
        
        return status;
    }
}

public class RethinkQueryStatus
{
    public int RetrievedItems;
    public int GalaxyEntries;
    public int ItemsEntries;
}