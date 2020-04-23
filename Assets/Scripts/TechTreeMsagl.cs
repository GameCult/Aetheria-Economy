using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.Layered;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class TechTreeMsagl : MonoBehaviour
{
    [Section("Assets")]
    public string IconsPath;
    public Prototype Ring;
    public Prototype Tech;
    public Prototype RadialLink;
    public Prototype Arrow;

    [Section("UI Links")]
    public RectTransform PropertiesPanel;
    public TextMeshProUGUI TechName;
    public TextMeshProUGUI Quality;
    public TextMeshProUGUI ProductionTime;
    public TextMeshProUGUI Produces;
    public TextMeshProUGUI ResearchTime;
    public Prototype RequirementPrototype;
    public Prototype DependencyPrototype;
    public Prototype DescendantPrototype;
    public ClickRaycaster ClickRaycaster;

    [Section("Graph Generation")]
    public bool TestMode = false;
    public int TechCount = 50;
    public int SeedTechs = 5;
    public int RollingWindowSize = 50;
    public bool Radial;
    public float MultipleDependencyProbability;
    public int DependencyDepth;
    public int DependencyAncestorDepth;

    [Section("Layout")]
    public PackingMethod PackingMethod;
    
    [Section("Radial")]
    public int Sections;
    public float PaddingRadians;
    public int StartTier = 3;
    public float Scale = 1;
    public int LinkPoints = 16;
    public int LinkDepth = 5;
    public float LinkTargetDistance = .75f;
    public int RingDepth = 6;
    public float ArrowDepth = 3;
    public float LayerDistance;
    
    [Section("Colors")]
    public float Saturation = .95f;
    public float HueMin = 0;
    public float HueMax = 1;
    public float Brightness = 1;
    public float RingInnerSaturation;
    public float RingOuterSaturation;
    public float RingInnerBrightness;
    public float RingOuterBrightness;
    public int RingColorKeys;
    public float FillOpacity = .25f;
    public float GlowOpacity = .5f;
    public Material TechFillMaterial;
    public Material TechGlowMaterial;
    public Material TechArrowMaterial;
    public Material TechLinkMaterial;

    public GameContext Context;

    private List<Prototype> _ringInstances = new List<Prototype>();
    private List<Prototype> _techInstances = new List<Prototype>();
    private List<Prototype> _linkInstances = new List<Prototype>();
    private List<Prototype> _arrowInstances = new List<Prototype>();
    private Texture2D[] _icons;
    private List<Prototype> _propertyInstances = new List<Prototype>();
    
    public BlueprintData[] Blueprints { get; set; }
    
    void Start()
    {
        if(TestMode)
            Initialize();
        ClickRaycaster.OnClickMiss += () => PropertiesPanel.gameObject.SetActive(false);
    }

    public void Initialize()
    {
        _icons = Resources.LoadAll<Texture2D>(IconsPath);
    }

    void Update()
    {
        if(TestMode && Input.GetKeyDown(KeyCode.Space))
            GenerateTechs();
    }
    
    Node NewNode(object data) => new Node(new Ellipse(1,1,new Point()), data);

    List<Node> GetRegion(GeometryGraph graph, Node node, int depth, int maxAncestorDistance)
    {
        Node ancestor = node;
        int actualDepth = 0;
        while (actualDepth < depth)
        {
            var edge = graph.Edges.FirstOrDefault(e => e.Target == ancestor);
            if (edge == null) break;
            var next = edge.Source;
            if (next == null)
                break;
            ancestor = next;
            actualDepth++;
        }

        var skipGenerations = actualDepth - maxAncestorDistance;

        var region = new List<Node>();
        Node[] children = {ancestor};
        for (int i = 0; i < actualDepth - 1; i++)
        {
            children = children.SelectMany(n => graph.Edges.Where(e => e.Source == n).Select(e => e.Target)).ToArray();
            if(i >= skipGenerations)
                region.AddRange(children);
        }

        return region;
    }

    public void GenerateTechs()
    {
        foreach (var instance in _ringInstances) instance.ReturnToPool();
        _ringInstances.Clear();
        
        foreach (var instance in _techInstances) instance.ReturnToPool();
        _techInstances.Clear();
        
        foreach (var instance in _linkInstances) instance.ReturnToPool();
        _linkInstances.Clear();
        
        foreach (var instance in _arrowInstances) instance.ReturnToPool();
        _arrowInstances.Clear();
        
        var graph = new GeometryGraph();
        
        var settings = new SugiyamaLayoutSettings();

        if (TestMode)
        {
            for (int i = 0; i < TechCount; i++)
            {
                var node = NewNode(i);
                graph.Nodes.Add(node);
                if(i>SeedTechs) graph.Edges.Add(new Edge(graph.Nodes[Random.Range(max(0,i-RollingWindowSize),i-1)], node));
            }
            foreach (var vertex in graph.Nodes.Where(v=>!graph.Edges.Any(e=>e.Source==v||e.Target==v)))
            {
                graph.Edges.Add(new Edge(vertex, graph.Nodes[Random.Range(0,TechCount-1)]));
            }
        }
        else
        {
            var nodeMap = new Dictionary<BlueprintData, Node>();
            foreach (var blueprint in Blueprints)
            {
                var node = NewNode(blueprint);
                nodeMap[blueprint] = node;
                graph.Nodes.Add(node);
            }

            foreach (var targetBlueprint in Blueprints)
            {
                foreach (var sourceBlueprint in Blueprints.Where(sb =>
                    targetBlueprint.Dependencies.Any(dep => sb.ID == dep)))
                {
                    var edge = new Edge(nodeMap[sourceBlueprint], nodeMap[targetBlueprint]);
                    graph.Edges.Add(edge);
                    var splitName = sourceBlueprint.Name.Split(' ');
                    if (splitName.Length > 1)
                    {
                        var nameStart = string.Join(" ", splitName.Take(splitName.Length - 1));
                        if(targetBlueprint.Name.StartsWith(nameStart))
                            edge.Weight *= 10;
                    }
                }
            }
        }

        settings.BrandesThreshold = 9999;

        var islands = graph.GetClusteredConnectedComponents();
        var islandMap =
            graph.Nodes.ToDictionary(n => n, n => islands.Find(c => c.Nodes.Any(cn => cn.UserData == n)));

        if (TestMode)
        {
            foreach (var island in islands)
            {
                foreach (var node in island.Nodes)
                {
                    var region = GetRegion(island, node, DependencyAncestorDepth, DependencyDepth);
                    while (Random.value < MultipleDependencyProbability && region.Count > 0)
                    {
                        var dependency = region[Random.Range(0,region.Count)];
                        graph.Edges.Add(new Edge(dependency.UserData as Node, node.UserData as Node));
                        region.Remove(dependency);
                    }
                }
            }
        }
        
        var islandsBySize = islands.OrderByDescending(i => i.Nodes.Count);
        var largestIsland = islandsBySize.First();
        foreach (var island in islandsBySize.Skip(1))
            settings.VerticalConstraints.SameLayerConstraints.Insert(new Tuple<Node, Node>(
                largestIsland.Nodes[Random.Range(0,largestIsland.Nodes.Count)].UserData as Node, island.Nodes[Random.Range(0,island.Nodes.Count)].UserData as Node));
        
        settings.PackingMethod = PackingMethod;
        settings.Transformation = PlaneTransformation.Rotation(PI);
        var layout = new LayeredLayout(graph, settings);
        layout.Run();

        var positions = graph.Nodes.ToDictionary(n => n, n => (float2) n.Center);
        var islandCenters = islands.ToDictionary(i => i,
            i => i.Nodes.Aggregate(float2(0, 0), (total, v) => total + positions[v.UserData as Node]) / i.Nodes.Count);

        Rect bounds = Rect.MinMaxRect(
            positions.Values.Min(v=>v.x),
            positions.Values.Min(v=>v.y),
            positions.Values.Max(v=>v.x),
            positions.Values.Max(v=>v.y));

        var tiers =
            Mathf.RoundToInt(graph.Nodes.Max(n => Rect.PointToNormalized(bounds, n.Center).y) /
                             graph.Nodes.Min(n =>
                             {
                                 var normalized = Rect.PointToNormalized(bounds, n.Center);
                                 return normalized.y > .001f ? normalized.y : 1;
                             }));

        // var islandsBySize = islands.OrderByDescending(i => i.Nodes.Count);
        // var largestIsland = islandsBySize.First();
        // foreach(var island in islandsBySize.Skip(1))
        //     settings.AddSameLayerNeighbors(largestIsland.Nodes.RandomElement().UserData as Node, island.Nodes.RandomElement().UserData as Node);
        //
        // layout = new LayeredLayout(graph, settings);
        // layout.Run();

        positions = graph.Nodes.ToDictionary(n => n, n => (float2) n.Center);
        islandCenters = islands.ToDictionary(i => i,
            i => i.Nodes.Aggregate(float2(0, 0), (total, v) => total + positions[v.UserData as Node]) / i.Nodes.Count);

        bounds = Rect.MinMaxRect(
            positions.Values.Min(v=>v.x),
            positions.Values.Min(v=>v.y),
            positions.Values.Max(v=>v.x),
            positions.Values.Max(v=>v.y));
        
        Debug.Log($"Generated {tiers} tiers of techs!");
        
        var islandColors = islandCenters.ToDictionary(
            i=>i.Key,
            i=>Color.HSVToRGB(lerp(HueMin, HueMax, Rect.PointToNormalized(bounds, i.Value).x), Saturation, Brightness));

        var islandFillMaterials = islandColors.ToDictionary(i => i.Key, i =>
        {
            var mat = new Material(TechFillMaterial);
            var col = i.Value;
            col.a = FillOpacity;
            mat.SetColor("_TintColor", col);
            return mat;
        });

        var islandGlowMaterials = islandColors.ToDictionary(i => i.Key, i =>
        {
            var mat = new Material(TechGlowMaterial);
            var col = i.Value;
            col.a = GlowOpacity;
            mat.SetColor("_TintColor", col);
            return mat;
        });

        var islandArrowMaterials = islandColors.ToDictionary(i => i.Key, i =>
        {
            var mat = new Material(TechArrowMaterial);
            var col = i.Value;
            col.a = 1;
            mat.SetColor("_TintColor", col);
            return mat;
        });

        var islandLinkMaterials = islandColors.ToDictionary(i => i.Key, i =>
        {
            var mat = new Material(TechLinkMaterial);
            var col = i.Value;
            col.a = 1;
            mat.SetColor("_TintColor", col);
            return mat;
        });
        
        if(Radial)
            positions = positions.ToDictionary(
                kvp => kvp.Key, 
                kvp =>
                {
                    var normalized = Rect.PointToNormalized(bounds, kvp.Value);
                    var rads = normalized.x * (PI * 2 - PaddingRadians) + PaddingRadians / 2;
                    return float2(sin(rads), -cos(rads)) * ((normalized.y * tiers + StartTier + .5f) * LayerDistance) * Scale;
                });
        else 
            positions = positions.ToDictionary(
                kvp => kvp.Key, 
                kvp =>
                {
                    var normalized = Rect.PointToNormalized(bounds, kvp.Value);
                    return float2(
                        normalized.x * LayerDistance * tiers * (bounds.width / bounds.height),
                        (normalized.y * tiers / 2 + StartTier + .5f) * LayerDistance);
                });
        
        foreach (var vertex in graph.Nodes.Where(positions.ContainsKey))
        {
            var tech = Tech.Instantiate<TechNode>();
            tech.transform.position = (Vector2) positions[vertex];
            tech.Label.text = $"{(TestMode ? ((int) vertex.UserData).ToString() : ((BlueprintData) vertex.UserData).ID.ToString().Substring(0,2))}";
            var gradient = tech.Label.colorGradient;
            gradient.bottomLeft = gradient.bottomRight = islandColors[islandMap[vertex]];
            tech.Label.colorGradient = gradient;
            tech.Icon.material.SetTexture("_MainTex", _icons[Random.Range(0,_icons.Length-1)]);
            tech.Fill.material = islandFillMaterials[islandMap[vertex]];
            tech.Glow.material = islandGlowMaterials[islandMap[vertex]];
            if (!TestMode)
                tech.Fill.GetComponent<ClickableCollider>().OnClick += (collider, data) =>
                {
                    foreach (var instance in _propertyInstances) instance.ReturnToPool();
                    var blueprint = (BlueprintData) vertex.UserData;
                    PropertiesPanel.gameObject.SetActive(true);
                    TechName.text = blueprint.Name;
                    Quality.text = $"{Mathf.RoundToInt(blueprint.Quality * 100)}%";
                    ProductionTime.text = $"{blueprint.ProductionTime:0.##} MH";
                    Produces.text = $"{blueprint.Quantity} {Context.Cache.Get<ItemData>(blueprint.Item).Name}";
                    ResearchTime.text = $"{blueprint.ResearchTime:0.##} MH";
                    foreach (var ingredient in blueprint.Ingredients)
                    {
                        var ingredientData = Context.Cache.Get<ItemData>(ingredient.Key);
                        var ingredientInstance = RequirementPrototype.Instantiate<Prototype>();
                        ingredientInstance.GetComponentInChildren<TextMeshProUGUI>().text = $"{ingredient.Value} {ingredientData.Name}";
                        _propertyInstances.Add(ingredientInstance);
                    }
                    foreach (var dependency in blueprint.Dependencies)
                    {
                        var dependencyBlueprint = Context.Cache.Get<BlueprintData>(dependency);
                        var dependencyInstance = DependencyPrototype.Instantiate<Prototype>();
                        dependencyInstance.GetComponentInChildren<TextMeshProUGUI>().text = dependencyBlueprint.Name;
                        _propertyInstances.Add(dependencyInstance);
                    }
                    foreach (var descendant in graph.Edges.Where(e=>e.Source==vertex).Select(e=>(BlueprintData) e.Target.UserData))
                    {
                        var descendantInstance = DescendantPrototype.Instantiate<Prototype>();
                        descendantInstance.GetComponentInChildren<TextMeshProUGUI>().text = descendant.Name;
                        _propertyInstances.Add(descendantInstance);
                    }
                };
            _techInstances.Add(tech.GetComponent<Prototype>());
        }
        
        foreach (var edge in graph.Edges)
        {
            var link = RadialLink.Instantiate<LineRenderer>();
            link.material = islandLinkMaterials[islandMap[edge.Source]];
            link.positionCount = LinkPoints;
            var p1r = length(positions[edge.Source]);
            var p2r = length(positions[edge.Target]);
            var p1a = (atan2(positions[edge.Source].y, positions[edge.Source].x) + PI * 2.5f) % (PI * 2);
            var p2a = (atan2(positions[edge.Target].y, positions[edge.Target].x) + PI * 2.5f) % (PI * 2);
            var p1 = float2(sin(p1a) * p1r, -cos(p1a) * p1r);
            var p2 = float2(sin(p2a) * p2r, -cos(p2a) * p2r);
            var dist = length(p2 - p1);
            var distProp = (dist - LinkTargetDistance) / dist;
            var dir = new float2();
            var lastPos = new float2();
            link.SetPositions(Enumerable.Range(0,LinkPoints).Select(i=>
            {
                var lerp = (float) i / (LinkPoints-1) * distProp;
                var angle = math.lerp(p1a, p2a, (lerp));
                var radius = math.lerp(p1r, p2r, lerp);
                var pos = Radial ? float2(sin(angle) * radius, -cos(angle) * radius) : math.lerp(positions[edge.Source], positions[edge.Target], lerp);
                dir = normalize(pos - lastPos);
                lastPos = pos;
                return (Vector3) float3(lastPos, LinkDepth);
            }).ToArray());
            _linkInstances.Add(link.GetComponent<Prototype>());

            var arrow = Arrow.Instantiate<TechArrow>();
            arrow.transform.position = new Vector3(lastPos.x, lastPos.y, ArrowDepth);
            arrow.transform.rotation = Quaternion.Euler(0,0,atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            arrow.Icon.material = islandArrowMaterials[islandMap[edge.Source]];
            _arrowInstances.Add(arrow.GetComponent<Prototype>());
        }

        if (Radial)
        {
            for (int i = 1; i <= tiers; i++)
            {
                var ring = Ring.Instantiate<LineRenderer>();
                var ringPositions = new Vector3[Sections];
                for (int s = 0; s < Sections; s++)
                {
                    var rads = ((float) s / (Sections-1)) * (PI * 2 - PaddingRadians) + PaddingRadians / 2;
                    ringPositions[s] = new Vector3(sin(rads), -cos(rads), 0) * ((i + StartTier) * (LayerDistance) * Scale);
                    ringPositions[s].z = RingDepth;
                }
        
                ring.positionCount = Sections;
                ring.SetPositions(ringPositions);
                var tierLerp = (float) i / (tiers - 1);
                ring.colorGradient = new Gradient
                {
                    alphaKeys = new []{new GradientAlphaKey(1,0), new GradientAlphaKey(1,1)},
                    colorKeys = Enumerable.Range(0,RingColorKeys).Select(x=>
                    {
                        var ringLerp = (float) x / (RingColorKeys - 1);
                        return new GradientColorKey(
                            Color.HSVToRGB(HueMin + (HueMax - HueMin) * ringLerp,
                                lerp(RingInnerSaturation, RingOuterSaturation, tierLerp),
                                lerp(RingInnerBrightness, RingOuterBrightness, tierLerp)), ringLerp);
                    }).ToArray()
                };
                
                _ringInstances.Add(ring.GetComponent<Prototype>());
            }
        }
    }
}
