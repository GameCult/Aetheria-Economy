using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MessagePack;
using UniRx;
using UnityEditor;
using UnityEngine;


#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public class Database
{
    public static event Action OnDataUpdate;

    private static Dictionary<Guid, IDatabaseEntry> Entries = new Dictionary<Guid, IDatabaseEntry>();

    private static Dictionary<Type, Dictionary<Guid, IDatabaseEntry>> Types = new Dictionary<Type, Dictionary<Guid, IDatabaseEntry>>();
	
    private static DirectoryInfo _dir;

    private static IDatabaseEntry Load(string path)
    {
        var bytes = File.ReadAllBytes(path);
//		Debug.Log("Deserializing item: \n" + MessagePackSerializer.ToJson(bytes));
        try
        {
            var entry = MessagePackSerializer.Deserialize<IDatabaseEntry>(bytes);
            if (entry != null)
            {
                Entries[entry.GetId()] = entry;
                var type = entry.GetType();
                var types = type.FindInterfaces((_, __) => true, null).Append(type);
                foreach (var t in types)
                {
                    if(!Types.ContainsKey(t))
                        Types[t] = new Dictionary<Guid, IDatabaseEntry>();
                    Types[t][entry.GetId()] = entry;
                }
                OnDataUpdate?.Invoke();
            }
			
            return entry;
        }
        catch (Exception e)
        {
            Debug.Log($"Encountered exception while reading item {path.Split('\\').Last()}:\n{MessagePackSerializer.SerializeToJson(bytes)}\n{e.StackTrace}");
            return null;
        }
    }

    public static IEnumerable<IDatabaseEntry> AllEntries => Entries.Values;

    public static IDatabaseEntry Get(Guid guid)
    {
        IDatabaseEntry entry;
        Entries.TryGetValue(guid, out entry);
        return entry;
    }
	
    public static T Get<T>(Guid guid) where T : IDatabaseEntry
    {
        IDatabaseEntry entry;
        Types[typeof(T)].TryGetValue(guid, out entry);
        return (T) entry;
    }

    public static IEnumerable<T> GetAll<T>()
    {
        return Types[typeof(T)].Values.Cast<T>();
    }

    public static void Save(IDatabaseEntry entry)
    {
//		Debug.Log("Serializing item: \n" + item.ToJson());
        var dir = new DirectoryInfo(Path.Combine(_dir.FullName, entry.GetType().Name));
        if(!dir.Exists)
            dir.Create();
        File.WriteAllBytes(Path.Combine(dir.FullName, entry.GetId().ToString()), entry.Serialize());
    }

    public static void Duplicate(IDatabaseEntry entry)
    {
        var oldID = entry.Entry.ID;
        entry.Entry.ID = Guid.NewGuid();
        Save(entry);
        var dir = new DirectoryInfo(Path.Combine(_dir.FullName, entry.GetType().Name));
        Load(Path.Combine(dir.FullName, oldID.ToString()));
    }

    public static void SaveAll()
    {
        foreach (var entry in GetAll<IDatabaseEntry>())
        {
            Save(entry);
        }
    }

    public static void Delete(IDatabaseEntry entry)
    {
        var dir = new DirectoryInfo(Path.Combine(_dir.FullName, entry.GetType().Name));
        File.Delete(Path.Combine(dir.FullName,entry.GetId().ToString()));
    }
	
    static Database()
    {
        _dir = new DirectoryInfo(Application.dataPath).Parent.CreateSubdirectory("GameData");
		
        Action<string> update = path => Load(path);

        foreach (var file in Directory.GetFiles(_dir.FullName, "*.*", SearchOption.AllDirectories))
            update(file);

        var watcher = new FileSystemWatcher(_dir.FullName);
        watcher.IncludeSubdirectories = true;
        watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
        watcher.Changed += (_, args) => MainThreadDispatcher.Post(__ => update(args.FullPath), null);
        watcher.Created += (_, args) => MainThreadDispatcher.Post(__ => update(args.FullPath), null);
        watcher.Deleted += (_, args) => MainThreadDispatcher.Post(__ =>
        {
            var entryGuid = Guid.Parse(args.Name.Split('\\').Last());
            foreach (var entries in Types.Values)
                entries.Remove(entryGuid);
            Entries.Remove(entryGuid);
            OnDataUpdate?.Invoke();
        }, null);
        watcher.EnableRaisingEvents = true;
    }
}

public interface IDatabaseEntry
{
    DatabaseEntry Entry { get; }
}

[Inspectable]
[MessagePackObject]
public class DatabaseEntry
{
    [Key("id")]          public Guid   ID = Guid.NewGuid();
    [Inspectable] [Key("name")]        public string Name;
    [Inspectable] [Key("description")] public string Description;
}