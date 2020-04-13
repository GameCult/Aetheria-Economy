using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GraphSharp.Algorithms.Layout;
using GraphSharp.Algorithms.Layout.Simple.FDP;
using GraphSharp.Algorithms.Layout.Simple.Hierarchical;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Layout.Incremental;
using Microsoft.Msagl.Layout.Layered;
using NaughtyAttributes;
using QuickGraph;
using QuickGraph.Algorithms;
using UnityEngine;
using Random = UnityEngine.Random;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public enum LayoutAlgorithm
{
    KamadaKawai,
    Sugiyama,
    MSAGL
}

public enum EliminateIslandMode
{
    None,
    Random,
    JoinLeaves,
    RandomLeaf
}

public class TechTreeTest : MonoBehaviour
{
    public string IconsPath;
    public Prototype Ring;
    public Prototype Tech;
    public Prototype Link;
    public Prototype RadialLink;
    public Prototype Arrow;
    public LayoutAlgorithm Algorithm;
    public int TechCount = 50;
    public int SeedTechs = 5;
    public int RollingWindowSize = 50;
    public EliminateIslandMode EliminateIslands = EliminateIslandMode.None;
    public bool MultipleDependencies;
    public float MultipleDependencyProbability = .25f;
    public int MaxDependencyDistance = 4;
    public int MaxDependencyTierDifference = 2;
    
    [Section("Kamada-Kawai")]
    public float DisconnectedMultiplier;
    public float K;
    public float LengthFactor;
    public int Iterations;
    
    [Section("Sugiyama")]
    public float LayerDistance;
    public float VertexDistance;
    public float WidthPerHeight;
    public int PositionMode;
    public bool Radial;
    public bool MinimizeEdgeLength;
    public bool OptimizeWidth;
    
    //[Section("MSAGL")]
    
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

    private List<Prototype> _ringInstances = new List<Prototype>();
    private List<Prototype> _techInstances = new List<Prototype>();
    private List<Prototype> _linkInstances = new List<Prototype>();
    private List<Prototype> _arrowInstances = new List<Prototype>();
    private Texture2D[] _icons;
    
    void Start()
    {
        _icons = Resources.LoadAll<Texture2D>(IconsPath);
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
            GenerateTechs();
    }

    void GenerateTechs()
    {
        foreach (var instance in _ringInstances) instance.ReturnToPool();
        _ringInstances.Clear();
        
        foreach (var instance in _techInstances) instance.ReturnToPool();
        _techInstances.Clear();
        
        foreach (var instance in _linkInstances) instance.ReturnToPool();
        _linkInstances.Clear();
        
        foreach (var instance in _arrowInstances) instance.ReturnToPool();
        _arrowInstances.Clear();

        var vertices = new List<IntVertex>();
        var edges = new List<Edge<IntVertex>>();
        var loose = SeedTechs;
        var count = TechCount;
        for (int i = 0; i < count; i++)
        {
            var vertex = new IntVertex(i);
            vertices.Add(vertex);
            if(i>loose) edges.Add(new Edge<IntVertex>(vertices[Random.Range(max(0,i-RollingWindowSize),i-1)], vertex));
        }
        foreach (var vertex in vertices.Where(v=>!edges.Any(e=>e.Source!=v&&e.Target!=v)))
        {
            edges.Add(new Edge<IntVertex>(vertices[Random.Range(0,count-1)], vertex));
        }
        var graph = edges.ToBidirectionalGraph<IntVertex,Edge<IntVertex>>();
        // var roots = graph.Roots().ToArray();
        // var rootChildren = graph.
        // foreach (var vertex in roots.Skip(1))
        // {
        //     graph.AddEdge(new Edge<IntVertex>(vertex,))
        // }
        var islands = new Dictionary<IntVertex, int>();
        var islandCount = graph.WeaklyConnectedComponents(islands);
        var islandsInverse = islands
            .GroupBy(i => i.Value)
            .ToDictionary(
                i => i.Key, 
                i => i.Select(g => g.Key).ToArray());

        if (MultipleDependencies)
        {
            var vertexTiers = new Dictionary<IntVertex, int>();
            foreach (var island in islandsInverse.Values)
            {
                vertexTiers[island[0]] = 0;
                foreach (var vertex in island.Skip(1))
                {
                    vertexTiers[vertex] = edges.FindPath(vertex, island[0], (v1, v2) => 1).Count();
                    // var tryGetPath = graph.ShortestPathsDijkstra(edge => 1, vertex);
                    // if (tryGetPath(island[0], out var path))
                    // {
                    //     vertexTiers[vertex] = path.Count();
                    // }
                    // else Debug.Log("Path to root not found?");
                }
            }
            
            foreach (var island in islandsInverse.Values)
            {
                foreach (var vertex in island.Skip(1))
                {
                    //var tryGetPath = graph.ShortestPathsDijkstra(edge => 1, vertex);
                    //var ancestors = island.Take(Array.IndexOf(island, vertex) - 1);
                    var possibleDependencies = island.Where(v =>
                    {
                        var dist = edges.FindPath(vertex, v, (v1, v2) => 1).Count();
                        return v != vertex &&
                               vertexTiers[v] < vertexTiers[vertex] &&
                               abs(vertexTiers[v] - vertexTiers[vertex]) < MaxDependencyTierDifference &&
                               dist < MaxDependencyDistance && dist > 2;
                        // if (v != vertex && tryGetPath(v, out var path))
                        // {
                        //     return path.Count() > 1 && path.Count() < MaxDependencyDistance;
                        // }
                        //
                        // return false;
                    }).ToArray();
                    var dependencies = new List<int>();
                    while (dependencies.Count < possibleDependencies.Length && Random.value < MultipleDependencyProbability)
                    {
                        var depIndex = Random.Range(0, possibleDependencies.Length);
                        dependencies.Add(depIndex);
                        edges.Add(new Edge<IntVertex>(possibleDependencies[depIndex], vertex));
                    }
                    // while (Random.value < MultipleDependencyProbability)
                    // {
                    //     var index = Array.IndexOf(island, vertex);
                    //     int depIndex;
                    //     int tries = 0;
                    //     do
                    //     {
                    //         depIndex = Random.Range(max(0, index - MultipleDependencyRollingWindow), index);
                    //     } while (tries < 10 && dependencies.Contains(depIndex));
                    //
                    //     if (tries < 10)
                    //         edges.Add(new Edge<IntVertex>(island[depIndex], vertex));
                    //     else break;
                    // }
                }
            }
        }

        if (EliminateIslands!=EliminateIslandMode.None)
        {
            if(islandCount>1)
                for (int i = 1; i < islandCount; i++)
                {
                    var i1 = islands.Where(x => x.Value == i - 1).Select(x=>x.Key).ToArray();
                    var i2 = islands.Where(x => x.Value == i).Select(x=>x.Key).ToArray();
                    if(EliminateIslands==EliminateIslandMode.JoinLeaves)
                        edges.Add(new Edge<IntVertex>(i1.Last(), i2.Last()));
                    else if(EliminateIslands==EliminateIslandMode.Random)
                        edges.Add(new Edge<IntVertex>(i1[Random.Range(0,i1.Length-1)],i2[Random.Range(0,i2.Length-1)]));
                    else if(EliminateIslands==EliminateIslandMode.RandomLeaf)
                        edges.Add(new Edge<IntVertex>(i1.Last(),i2[Random.Range(0,i2.Length-1)]));
                }
            graph = edges.ToBidirectionalGraph<IntVertex,Edge<IntVertex>>();
        }
        

        LayoutAlgorithmBase<IntVertex, Edge<IntVertex>, BidirectionalGraph<IntVertex, Edge<IntVertex>>> layout = 
            (Algorithm == LayoutAlgorithm.Sugiyama) ?
            (LayoutAlgorithmBase<IntVertex, Edge<IntVertex>, BidirectionalGraph<IntVertex, Edge<IntVertex>>>) new EfficientSugiyamaLayoutAlgorithm<
                IntVertex, 
                Edge<IntVertex>,
                BidirectionalGraph<IntVertex, Edge<IntVertex>>>(
                graph,
                new EfficientSugiyamaLayoutParameters()
                {
                    EdgeRouting = SugiyamaEdgeRoutings.Traditional,
                    LayerDistance = LayerDistance,
                    MinimizeEdgeLength = MinimizeEdgeLength,
                    OptimizeWidth = OptimizeWidth,
                    VertexDistance = VertexDistance,
                    PositionMode = PositionMode,
                    WidthPerHeight = WidthPerHeight
                },
                vertices.ToDictionary(x => x, x => float2(1, 1))
            ) : 
            (LayoutAlgorithmBase<IntVertex, Edge<IntVertex>, BidirectionalGraph<IntVertex, Edge<IntVertex>>>) new KKLayoutAlgorithm<
            IntVertex, 
            Edge<IntVertex>,
            BidirectionalGraph<IntVertex, Edge<IntVertex>>>(
            graph,
            new KKLayoutParameters
            {
                AdjustForGravity = true,
                DisconnectedMultiplier = DisconnectedMultiplier,
                ExchangeVertices = false,
                Height = 50,
                Width = 50,
                K = K,
                LengthFactor = LengthFactor,
                MaxIterations = Iterations
            });
        layout.Compute();
        IDictionary<IntVertex, float2> positions = layout.VertexPositions;
        var islandCenters = islandsInverse.ToDictionary(i => i.Key,
            i => i.Value.Aggregate(float2(0, 0), (total, v) => total + positions[v]) / i.Value.Length);

        Rect bounds = Rect.MinMaxRect(
            layout.VertexPositions.Values.Min(v=>v.x),
            layout.VertexPositions.Values.Min(v=>v.y),
            layout.VertexPositions.Values.Max(v=>v.x),
            layout.VertexPositions.Values.Max(v=>v.y));
        
        var islandColors = islandCenters.ToDictionary(
            i=>i.Key,
            i=>Color.HSVToRGB(lerp(HueMin, HueMax, Algorithm == LayoutAlgorithm.Sugiyama ? Rect.PointToNormalized(bounds, i.Value).x : Random.value), Saturation, Brightness));

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
        
        if (Algorithm == LayoutAlgorithm.Sugiyama && Radial)
        {
            int tiers = Mathf.RoundToInt(bounds.height / LayerDistance);
            positions = layout.VertexPositions.ToDictionary(
                kvp => kvp.Key, 
                kvp =>
                {
                    var normalized = Rect.PointToNormalized(bounds, kvp.Value);
                    var rads = normalized.x * (PI * 2 - PaddingRadians) + PaddingRadians / 2;
                    return float2(sin(rads), -cos(rads)) * ((normalized.y * tiers / 2 + StartTier + .5f) * LayerDistance) * Scale;
                });
            for (int i = 1; i <= tiers / 2; i++)
            {
                var ring = Ring.Instantiate<LineRenderer>();
                var ringPositions = new Vector3[Sections];
                for (int s = 0; s < Sections; s++)
                {
                    var rads = ((float) s / (Sections-1)) * (PI * 2 - PaddingRadians) + PaddingRadians / 2;
                    ringPositions[s] = new Vector3(sin(rads), -cos(rads), 0) * ((i + StartTier) * LayerDistance * Scale);
                    ringPositions[s].z = RingDepth;
                }

                ring.positionCount = Sections;
                ring.SetPositions(ringPositions);
                var tierLerp = (float) i / (tiers / 2 - 1);
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
        else if (Algorithm == LayoutAlgorithm.KamadaKawai)
        {
            positions = layout.VertexPositions.ToDictionary(
                kvp => kvp.Key, 
                kvp => kvp.Value - (float2) bounds.center);
        }
        
        foreach (var vertex in vertices.Where(positions.ContainsKey))
        {
            var tech = Tech.Instantiate<TechNode>();
            tech.transform.position = (Vector2) positions[vertex];
            tech.Label.text = $"{vertex.N}";
            var gradient = tech.Label.colorGradient;
            gradient.bottomLeft = gradient.bottomRight = islandColors[islands[vertex]];
            tech.Label.colorGradient = gradient;
            tech.Icon.material.SetTexture("_MainTex", _icons[Random.Range(0,_icons.Length-1)]);
            tech.Fill.material = islandFillMaterials[islands[vertex]];
            tech.Glow.material = islandGlowMaterials[islands[vertex]];
            _techInstances.Add(tech.GetComponent<Prototype>());
        }

        if (Algorithm == LayoutAlgorithm.Sugiyama && Radial)
        {
            foreach (var edge in edges)
            {
                var link = RadialLink.Instantiate<LineRenderer>();
                link.material = islandLinkMaterials[islands[edge.Source]];
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
                    var pos = float2(sin(angle) * radius, -cos(angle) * radius);
                    dir = normalize(pos - lastPos);
                    lastPos = pos;
                    return (Vector3) float3(lastPos, LinkDepth);
                }).ToArray());
                _linkInstances.Add(link.GetComponent<Prototype>());

                var arrow = Arrow.Instantiate<TechArrow>();
                arrow.transform.position = new Vector3(lastPos.x, lastPos.y, ArrowDepth);
                arrow.transform.rotation = Quaternion.Euler(0,0,atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                arrow.Icon.material = islandArrowMaterials[islands[edge.Source]];
                _arrowInstances.Add(arrow.GetComponent<Prototype>());
            }
        }
        else
        {
            foreach (var edge in edges)
            {
                var link = Link.Instantiate<TechArrow>();
                link.Icon.material = islandLinkMaterials[islands[edge.Source]];
                link.transform.position = float3(positions[edge.Source], LinkDepth);
                var diff = positions[edge.Target] - positions[edge.Source];
                link.transform.rotation = Quaternion.Euler(0,0,atan2(diff.y, diff.x) * Mathf.Rad2Deg);
                link.transform.localScale = new Vector3(length(diff) - LinkTargetDistance, 1, 1);
                _linkInstances.Add(link.GetComponent<Prototype>());

                var arrow = Arrow.Instantiate<TechArrow>();
                arrow.transform.position = float3(positions[edge.Source] + normalize(positions[edge.Target] - positions[edge.Source]) * (length(diff) - LinkTargetDistance), ArrowDepth);
                arrow.transform.rotation = Quaternion.Euler(0,0,atan2(diff.y, diff.x) * Mathf.Rad2Deg);
                arrow.Icon.material = islandArrowMaterials[islands[edge.Source]];
                _arrowInstances.Add(arrow.GetComponent<Prototype>());
            }
        }

    }
    
    float ease_in_quad(float x) {
        float t = x; float b = 0; float c = 1; float d = 1;
        return c*(t/=d)*t + b;
    }

    float ease_out_quad(float x) {
        float t = x; float b = 0; float c = 1; float d = 1;
        return -c *(t/=d)*(t-2) + b;
    }

    class IntVertex
    {
        public int N;

        public IntVertex(int n)
        {
            N = n;
        }
    }
}
