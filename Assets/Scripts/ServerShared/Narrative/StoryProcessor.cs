
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ink;
using Ink.Runtime;
using MessagePack;
using Path = System.IO.Path;
using Random = Unity.Mathematics.Random;

public class StoryProcessor : IZoneResolver, IFactionResolver
{
    private class AetheriaInkFileHandler : IFileHandler {
        private DirectoryInfo NarrativeRoot { get; }
        
        public AetheriaInkFileHandler(DirectoryInfo narrativeRoot)
        {
            NarrativeRoot = narrativeRoot;
        }

        public string ResolveInkFilename (string includeName) => Path.Combine (NarrativeRoot.FullName, includeName);

        public string LoadInkFileContents (string fullFilename) => File.ReadAllText (fullFilename);
    }

    private Dictionary<Story, SectorZone> _placedStories = new Dictionary<Story, SectorZone>();
    private Dictionary<string, Story> _processedStories = new Dictionary<string, Story>();
    private DirectoryInfo _locationsPath;
    private IFileHandler _inkFileHandler;

    public DirectoryInfo NarrativeDirectory { get; }
    private Random _random;
    public Sector Sector { get; }
    public PlayerSettings Settings { get; }
    
    public StoryProcessor(PlayerSettings settings, DirectoryInfo narrativeDirectory, Sector sector, ref Random random)
    {
        NarrativeDirectory = narrativeDirectory;
        Sector = sector;
        _random = random;
        Settings = settings;
        
        _inkFileHandler = new AetheriaInkFileHandler(narrativeDirectory);
        
        _locationsPath = narrativeDirectory.CreateSubdirectory("Locations");
    }

    public Dictionary<Story, SectorZone> PlaceStories()
    {
        var locationFiles = _locationsPath.EnumerateFiles("*.ink");
        foreach(var inkFile in locationFiles) PlaceStory(GetStory(inkFile));
        return _placedStories;
    }
    
    public SectorZone ResolveZone(string path)
    {
        // Handle special cases: "start" and "end"
        if (path.Equals("start", StringComparison.InvariantCultureIgnoreCase))
            return Sector.Entrance;
        if (path.Equals("end", StringComparison.InvariantCultureIgnoreCase))
            return Sector.Exit;
        
        // Handle faction home reference: format = "home.<faction>"
        if(path.StartsWith("home"))
        {
            var factionString = path.Substring(5);
            var faction = ResolveFaction(factionString);
            return !Sector.HomeZones.ContainsKey(faction) ? null : Sector.HomeZones[faction];
        }
        
        var locationInkFile = new FileInfo(Path.Combine(_locationsPath.FullName, $"{path}.ink"));
        if (!locationInkFile.Exists) return null;

        var story = GetStory(locationInkFile);
        PlaceStory(story);
        return _placedStories.ContainsKey(story) ? _placedStories[story] : null;
    }

    public Faction ResolveFaction(string name)
    {
        return Sector.Factions.FirstOrDefault(f => f.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase));
    }

    Story GetStory(FileInfo inkFile)
    {
        var fileName = Path.GetFileNameWithoutExtension(inkFile.FullName);
        
        // If the story has already been read / compiled, return it directly
        if (_processedStories.ContainsKey(fileName)) return _processedStories[fileName];
        
        var compiledFileName = Path.Combine(inkFile.Directory.FullName, $"{fileName}.json");
        
        // Calculate a hash of the ink file
        var hash = File.ReadAllBytes(inkFile.FullName).GetHashSHA1();
        
        if (File.Exists(compiledFileName) && // If a compiled version of the ink file exists
            Settings.HashedStoryFiles.ContainsKey(fileName) && // And its hash has been saved
            hash == Settings.HashedStoryFiles[fileName]) // And the saved hash matches the current hash
        {
            // Load the story from the existing compiled JSON representation
            return new Story(File.ReadAllText(compiledFileName));
        }

        Settings.HashedStoryFiles[fileName] = hash;
        var compiler = new Compiler(File.ReadAllText(inkFile.FullName), new Compiler.Options
        {
            countAllVisits = true,
            fileHandler = _inkFileHandler
        });
        var story = compiler.Compile();
        File.WriteAllText(compiledFileName, story.ToJson());
        return story;
    }
    
    public void PlaceStory(Story story)
    {
        if (_placedStories.ContainsKey(story)) return; // Don't place already placed stories, idiot!
        _placedStories[story] = null; // Avoids potential for infinite loops when evaluating constraints

        var contentTags = GetContentTags(story);

        var constraints = new List<ZoneConstraint>();
        if(contentTags.ContainsKey("constraint"))
            foreach (var constraintString in contentTags["constraint"])
            {
                var args = constraintString.Split(' ');
                var flip = args[0] == "not";
                if (flip) args = args.Skip(1).ToArray();
                var constraintName = args[0];
                args = args.Skip(1).ToArray();
                ZoneConstraint constraint = constraintName switch
                {
                    "DistanceFrom" => new DistanceConstraint(args, this) { Flip = flip },
                    "FactionPresent" => new FactionPresenceConstraint(args, this) { Flip = flip },
                    "FactionOwner" => new FactionOwnerConstraint(args, this) { Flip = flip },
                    _ => null
                };
                if(constraint!=null) constraints.Add(constraint);
            }

        ZoneSelector selector;
        if (contentTags.ContainsKey("select"))
        {
            var selectorString = contentTags["select"].First();
            var args = selectorString.Split(' ');
            var flip = args[0] == "not";
            if (flip) args = args.Skip(1).ToArray();
            var selectorName = args[0];
            args = args.Skip(1).ToArray();
            selector = selectorName switch
            {
                "DistanceFrom" => new DistanceSelector(args, this) { Flip = flip },
                _ => new RandomSelector(ref _random)
            };
        }
        else
            selector = new RandomSelector(ref _random);
        
        var zoneCandidates = new List<SectorZone>();
        zoneCandidates.AddRange(Sector.Zones.Where(z=>constraints.All(c=>c.Test(z))));
        _placedStories[story] = selector.SelectZone(zoneCandidates);
    }

    // Parse all tags of the format X:Y into a dictionary with a list of every Y for each X
    public static Dictionary<string, List<string>> GetContentTags(Story story)
    {
        var contentTags = new Dictionary<string, List<string>>();
        foreach (var tag in story.globalTags)
        {
            if (tag.IndexOf(':') == -1) continue;
            
            var tokens = tag.Split(':');
            var leftSide = tokens[0].Trim();
            var rightSide = tokens[1].Trim();
            if (!contentTags.ContainsKey(leftSide)) contentTags[leftSide] = new List<string>();
            contentTags[leftSide].Add(rightSide);
        }

        return contentTags;
    }
}