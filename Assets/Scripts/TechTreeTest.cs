using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GraphSharp.Algorithms.Layout;
using GraphSharp.Algorithms.Layout.Simple.FDP;
using GraphSharp.Algorithms.Layout.Simple.Hierarchical;
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
    Sugiyama
}

public class TechTreeTest : MonoBehaviour
{
    public string IconsPath;
    public Prototype Ring;
    public Prototype Tech;
    public Prototype Link;
    public Prototype RadialLink;
    public LayoutAlgorithm Algorithm;
    public int TechCount = 50;
    public int SeedTechs = 5;
    
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
    
    [Section("Radial")]
    public int Sections;
    public float PaddingRadians;
    public int StartTier = 3;
    public float Scale = 1;
    public int LinkPoints = 16;
    public int LinkDepth = 5;
    public int RingDepth = 6;

    private List<Prototype> _ringInstances = new List<Prototype>();
    private List<Prototype> _techInstances = new List<Prototype>();
    private List<Prototype> _linkInstances = new List<Prototype>();
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

        var vertices = new List<IntVertex>();
        var edges = new List<Edge<IntVertex>>();
        var loose = SeedTechs;
        var count = TechCount;
        for (int i = 0; i < count; i++)
        {
            var vertex = new IntVertex(i);
            vertices.Add(vertex);
            if(i>loose) edges.Add(new Edge<IntVertex>(vertices[Random.Range(0,i-1)], vertex));
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
        if(islandCount>1)
            for (int i = 1; i < islandCount; i++)
            {
                var i1 = islands.Where(x => x.Value == i - 1).Select(x=>x.Key).ToArray();
                var i2 = islands.Where(x => x.Value == i).Select(x=>x.Key).ToArray();
                edges.Add(new Edge<IntVertex>(i1[Random.Range(0,i1.Length-1)],i2[Random.Range(0,i2.Length-1)]));
            }
        graph = edges.ToBidirectionalGraph<IntVertex,Edge<IntVertex>>();

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
                    MinimizeEdgeLength = true,
                    OptimizeWidth = true,
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

        Rect bounds = Rect.MinMaxRect(
            layout.VertexPositions.Values.Min(v=>v.x),
            layout.VertexPositions.Values.Min(v=>v.y),
            layout.VertexPositions.Values.Max(v=>v.x),
            layout.VertexPositions.Values.Max(v=>v.y));
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
            tech.Icon.material.SetTexture("_MainTex", _icons[Random.Range(0,_icons.Length-1)]);
            _techInstances.Add(tech.GetComponent<Prototype>());
        }

        if (Algorithm == LayoutAlgorithm.Sugiyama && Radial)
        {
            foreach (var edge in edges)
            {
                var link = RadialLink.Instantiate<LineRenderer>();
                link.positionCount = LinkPoints;
                var p1r = length(positions[edge.Source]);
                var p2r = length(positions[edge.Target]);
                var p1a = (atan2(positions[edge.Source].y, positions[edge.Source].x) + PI * 2.5f) % (PI * 2);
                var p2a = (atan2(positions[edge.Target].y, positions[edge.Target].x) + PI * 2.5f) % (PI * 2);
                link.SetPositions(Enumerable.Range(0,LinkPoints).Select(i=>
                {
                    var lerp = (float) i / (LinkPoints-1);
                    var angle = math.lerp(p1a, p2a, ease_out_quad(lerp));
                    var radius = math.lerp(p1r, p2r, lerp);
                    return new Vector3(sin(angle) * radius, -cos(angle) * radius, LinkDepth);
                }).ToArray());
                _linkInstances.Add(link.GetComponent<Prototype>());
            }
        }
        else
        {
            foreach (var edge in edges)
            {
                var link = Link.Instantiate<Transform>();
                link.position = (Vector2) positions[edge.Source];
                var diff = positions[edge.Target] - positions[edge.Source];
                link.rotation = Quaternion.Euler(0,0,atan2(diff.y, diff.x) * Mathf.Rad2Deg);
                link.localScale = new Vector3(length(diff), 1, 1);
                _linkInstances.Add(link.GetComponent<Prototype>());
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
