using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MessagePack;

public class DatabaseCache
{
    public event Action<DatabaseEntry> OnDataUpdateLocal;
    public event Action<DatabaseEntry> OnDataUpdateRemote;

    public Action<string> Logger = Console.WriteLine;

    private readonly Dictionary<Guid, DatabaseEntry> _entries = new Dictionary<Guid, DatabaseEntry>();

    private readonly Dictionary<Type, Dictionary<Guid, DatabaseEntry>> _types = new Dictionary<Type, Dictionary<Guid, DatabaseEntry>>();
	
    private DirectoryInfo _dir;

    public IEnumerable<DatabaseEntry> AllEntries => _entries.Values;

    public void Add(DatabaseEntry entry, bool remote = false)
    {
        if (entry != null)
        {
            _entries[entry.ID] = entry;
            var type = entry.GetType();
            var types = type.FindInterfaces((_, __) => true, null).Append(type);
            foreach (var t in types)
            {
                if(!_types.ContainsKey(t))
                    _types[t] = new Dictionary<Guid, DatabaseEntry>();
                _types[t][entry.ID] = entry;
            }
            if(remote)
                OnDataUpdateRemote?.Invoke(entry);
            else
                OnDataUpdateLocal?.Invoke(entry);
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
        DatabaseEntry entry;
        _types[typeof(T)].TryGetValue(guid, out entry);
        return (T) entry;
    }

    public IEnumerable<T> GetAll<T>()
    {
        return _types[typeof(T)].Values.Cast<T>();
    }
}