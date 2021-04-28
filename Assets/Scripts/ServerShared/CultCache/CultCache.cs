/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MessagePack;

public class CultCache
{
    public event Action<DatabaseEntry> OnDataInsertLocal;
    public event Action<DatabaseEntry> OnDataUpdateLocal;
    public event Action<DatabaseEntry> OnDataRemoveLocal;
    public event Action<DatabaseEntry> OnDataInsertRemote;
    public event Action<DatabaseEntry> OnDataUpdateRemote;
    public event Action<DatabaseEntry> OnDataRemoveRemote;

    private readonly object addLock = new object();

    //public Action<string> Logger = Console.WriteLine;
    private string _filePath;

    private readonly Dictionary<Guid, DatabaseEntry> _entries = new Dictionary<Guid, DatabaseEntry>();

    private readonly Dictionary<Type, DatabaseEntry> _globals = new Dictionary<Type, DatabaseEntry>();
    private readonly Dictionary<Type, HashSet<DatabaseEntry>> _types = new Dictionary<Type, HashSet<DatabaseEntry>>();

    private readonly Dictionary<Type, (DirectoryInfo directory, List<Guid> entries)> _externalTypes = new Dictionary<Type, (DirectoryInfo directory, List<Guid> entries)>();

    private class ExternalEntry
    {
        public string FilePath;
        public DatabaseEntry Entry;

        public ExternalEntry(string filePath, DatabaseEntry entry)
        {
            FilePath = filePath;
            Entry = entry;
        }
    }
    private readonly Dictionary<Guid, ExternalEntry> _externalEntries = new Dictionary<Guid, ExternalEntry>();
    private readonly Dictionary<Type, FieldInfo[]> _linkFields = new Dictionary<Type, FieldInfo[]>();

    public IEnumerable<DatabaseEntry> AllEntries => _entries.Values;

    public CultCache(string filePath)
    {
        DatabaseLinkBase.Cache = this;
        RegisterResolver.Register();
        _filePath = filePath;
        var fileInfo = new FileInfo(filePath);
        var dataDirectory = fileInfo.Directory;
        
        // Entry types can be marked external
        // External entries are stored individually and loaded on demand
        // This is mainly to accomodate large files like datasets
        foreach (var type in typeof(DatabaseEntry).GetAllChildClasses())
        {
            var linkFields = type.GetFields().Where(f => f.FieldType.IsAssignableToGenericType(typeof(DatabaseLink<>))).ToArray();
            if(linkFields.Length > 0)
                _linkFields[type] = linkFields;
            
            if (type.GetCustomAttribute<ExternalEntryAttribute>() != null)
            {
                var typeDirectory = dataDirectory.CreateSubdirectory(type.Name);
                _externalTypes[type] = (typeDirectory, new List<Guid>());

                // Cache the GUIDs for all available external entries for querying
                // Initially set values to null to indicate they haven't been loaded yet
                foreach (var file in typeDirectory.EnumerateFiles("*.msgpack"))
                {
                    Guid id;
                    if (Guid.TryParse(file.Name.Substring(0, file.Name.IndexOf('.')), out id))
                    {
                        _externalEntries.Add(id, new ExternalEntry(file.FullName, null));
                        _externalTypes[type].entries.Add(id);
                    }
                }
            }
            else _types[type] = new HashSet<DatabaseEntry>();
            
            if (type.GetCustomAttribute<GlobalSettingsAttribute>() != null)
            {
                _globals[type] = null;
                Add(Activator.CreateInstance(type) as DatabaseEntry);
            }
        }
    }

    public void Add(DatabaseEntry entry, bool remote = false)
    {
        lock(addLock)
        {
            if (entry != null)
            {
                var exists = _entries.ContainsKey(entry.ID);
                
                var type = entry.GetType();

                // If an external entry is added, store it separately
                if (_externalTypes.ContainsKey(type))
                {
                    if (_externalEntries.ContainsKey(entry.ID))
                    {
                        _externalEntries[entry.ID].Entry = entry;
                    }
                    else
                    {
                        _externalTypes[type].entries.Add(entry.ID);
                        _externalEntries[entry.ID] = new ExternalEntry(
                            Path.Combine(_externalTypes[type].directory.FullName, $"{entry.ID.ToString()}.msgpack"),
                            entry);
                    }
                }
                else
                {
                    if (_globals.ContainsKey(type))
                    {
                        if(_globals[type]!=null)
                        {
                            Remove(_globals[type], remote);
                            exists = true;
                        }
                        _globals[type] = entry;
                    }
                    
                    _entries[entry.ID] = entry;
                    _types[type].Add(entry);
                    foreach (var parentType in type.GetParentTypes())
                    {
                        if(_types.ContainsKey(parentType))
                            _types[parentType].Add(entry);
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
    }

    public bool IsExternal(DatabaseEntry entry) => _externalTypes.ContainsKey(entry.GetType());
    public bool IsGlobal(DatabaseEntry entry) => _globals.ContainsKey(entry.GetType());

    public void AddAll(IEnumerable<DatabaseEntry> entries, bool remote = false)
    {
        foreach (var entry in entries)
        {
            Add(entry, remote);
        }
    }

    public DatabaseEntry Get(Guid guid)
    {
        if (_externalEntries.ContainsKey(guid))
        {
            if(_externalEntries[guid].Entry == null)
            {
                var bytes = File.ReadAllBytes(_externalEntries[guid].FilePath);
                var e = MessagePackSerializer.Deserialize<DatabaseEntry>(bytes);
                _externalEntries[guid].Entry = e;
            }

            return _externalEntries[guid].Entry;
        }

        DatabaseEntry entry;
        _entries.TryGetValue(guid, out entry);
        return entry;
    }
	
    public T Get<T>(Guid guid) where T : DatabaseEntry
    {
        return Get(guid) as T;
    }

    public DatabaseEntry GetGlobal(Type type)
    {
        if (_globals.ContainsKey(type)) return _globals[type];
        return null;
    }

    public T GetGlobal<T>() where T : DatabaseEntry
    {
        return GetGlobal(typeof(T)) as T;
    }

    public IEnumerable<DatabaseEntry> GetAll(Type type)
    {
        if (_externalTypes.ContainsKey(type))
        {
            return _externalTypes[type].entries.Select(Get);
        }
        return !_types.ContainsKey(type) ? Enumerable.Empty<DatabaseEntry>() : _types[type];
    }

    public IEnumerable<T> GetAll<T>() where T : DatabaseEntry
    {
        var type = typeof(T);
        if (_externalTypes.ContainsKey(type))
        {
            return _externalTypes[type].entries.Select(Get<T>);
        }
        return !_types.ContainsKey(type) ? Enumerable.Empty<T>() : _types[type].Cast<T>();
    }

    public void Remove(DatabaseEntry entry, bool remote = false)
    {
        _entries.Remove(entry.ID);
        foreach (var parentType in entry.GetType().GetParentTypes())
        {
            if(_types.ContainsKey(parentType))
                _types[parentType].Remove(entry);
        }

        if (remote)
            OnDataRemoveRemote?.Invoke(entry);
        else
            OnDataRemoveLocal?.Invoke(entry);
    }

    public void Load()
    {
        var bytes = File.ReadAllBytes(_filePath);
        var entries = MessagePackSerializer.Deserialize<DatabaseEntry[]>(bytes);
        AddAll(entries);
    }

    public void Save()
    {
        var entries = AllEntries.ToArray();
        File.WriteAllBytes(_filePath, MessagePackSerializer.Serialize(entries));
        
        foreach (var external in _externalEntries.Values)
        {
            if (external.Entry != null)
            {
                File.WriteAllBytes(external.FilePath, MessagePackSerializer.Serialize(external.Entry));
            }
        }
    }
}