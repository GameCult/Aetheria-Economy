/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MessagePack;

public class DatabaseCache
{
    public event Action<DatabaseEntry> OnDataInsertLocal;
    public event Action<DatabaseEntry> OnDataUpdateLocal;
    public event Action<DatabaseEntry> OnDataDeleteLocal;
    public event Action<DatabaseEntry> OnDataInsertRemote;
    public event Action<DatabaseEntry> OnDataUpdateRemote;
    public event Action<DatabaseEntry> OnDataDeleteRemote;

    private readonly object addLock = new object();

    public Action<string> Logger = Console.WriteLine;

    private readonly Dictionary<Guid, DatabaseEntry> _entries = new Dictionary<Guid, DatabaseEntry>();

    private readonly Dictionary<Type, Dictionary<Guid, DatabaseEntry>> _types = new Dictionary<Type, Dictionary<Guid, DatabaseEntry>>();

    public IEnumerable<DatabaseEntry> AllEntries => _entries.Values;

    public void Add(DatabaseEntry entry, bool remote = false)
    {
        lock(addLock)
        {
            if (entry != null)
            {
                var exists = _entries.ContainsKey(entry.ID);
                _entries[entry.ID] = entry;
                var type = entry.GetType();
                var types = type.GetParentTypes();
                foreach (var t in types)
                {
                    if (!_types.ContainsKey(t))
                        _types[t] = new Dictionary<Guid, DatabaseEntry>();
                    _types[t][entry.ID] = entry;
                }

                if (remote)
                {
                    if (exists)
                        OnDataUpdateRemote?.Invoke(entry);
                    else
                        OnDataInsertRemote?.Invoke(entry);
                }
                else
                {
                    if (exists)
                        OnDataUpdateLocal?.Invoke(entry);
                    else
                        OnDataInsertLocal?.Invoke(entry);
                }
            }
        }
    }

    public void AddAll(IEnumerable<DatabaseEntry> entries, bool remote = false)
    {
        foreach (var entry in entries)
        {
            Add(entry, remote);
        }
    }

    public DatabaseEntry Get(Guid guid)
    {
        DatabaseEntry entry;
        _entries.TryGetValue(guid, out entry);
        return entry;
    }
	
    public T Get<T>(Guid guid) where T : DatabaseEntry
    {
        if (!_types.ContainsKey(typeof(T)))
            return null;
        DatabaseEntry entry;
        _types[typeof(T)].TryGetValue(guid, out entry);
        return (T) entry;
    }

    public IEnumerable<T> GetAll<T>()
    {
        return !_types.ContainsKey(typeof(T)) ? Enumerable.Empty<T>() : _types[typeof(T)].Values.Cast<T>();
    }

    public void Delete(DatabaseEntry entry, bool remote = false)
    {
        _entries.Remove(entry.ID);
        foreach (var type in _types.Values)
        {
            type.Remove(entry.ID);
        }

        if (remote)
            OnDataDeleteRemote?.Invoke(entry);
        else
            OnDataDeleteLocal?.Invoke(entry);
    }

    public void Load(string path)
    {
        RegisterResolver.Register();
        var bytes = File.ReadAllBytes(Path.Combine(path, "AetherDB.msgpack"));
        var entries = MessagePackSerializer.Deserialize<DatabaseEntry[]>(bytes);
        AddAll(entries);
    }

    public void Save(string path)
    {
        RegisterResolver.Register();
        var entries = AllEntries.ToArray();
        File.WriteAllBytes(Path.Combine(path, "AetherDB.msgpack"), MessagePackSerializer.Serialize(entries));
    }
}