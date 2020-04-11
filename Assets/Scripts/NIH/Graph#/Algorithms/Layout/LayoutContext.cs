using System.Collections.Generic;
using System.Diagnostics.Contracts;
using QuickGraph;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace GraphSharp.Algorithms.Layout
{
    public class LayoutContext<TVertex, TEdge, TGraph> : ILayoutContext<TVertex, TEdge, TGraph>
        where TEdge : IEdge<TVertex>
        where TGraph : IVertexAndEdgeListGraph<TVertex, TEdge>
    {
        public IDictionary<TVertex, float2> Positions { get; private set; }

        public IDictionary<TVertex, float2> Sizes { get; private set; }

        public TGraph Graph { get; private set; }

        public LayoutMode Mode { get; private set; }

        public LayoutContext( TGraph graph, IDictionary<TVertex, float2> positions, IDictionary<TVertex, float2> sizes, LayoutMode mode )
        {
            Graph = graph;
            Positions = positions;
            Sizes = sizes;
            Mode = mode;
        }
    }
}