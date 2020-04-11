using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Windows;
using QuickGraph;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace GraphSharp.Algorithms.Layout.Compound
{
    public class CompoundLayoutContext<TVertex, TEdge, TGraph> 
        : LayoutContext<TVertex, TEdge, TGraph>, ICompoundLayoutContext<TVertex, TEdge, TGraph>
        where TEdge : IEdge<TVertex>
        where TGraph : class, IBidirectionalGraph<TVertex, TEdge>
    {
        public CompoundLayoutContext(
            TGraph graph,
            IDictionary<TVertex, float2> positions,
            IDictionary<TVertex, float2> sizes,
            LayoutMode mode,
            // IDictionary<TVertex, Thickness> vertexBorders,
            IDictionary<TVertex, CompoundVertexInnerLayoutType> layoutTypes)
            : base( graph, positions, sizes, mode )
        {
            // VertexBorders = vertexBorders;
            LayoutTypes = layoutTypes;
        }

        // public IDictionary<TVertex, Thickness> VertexBorders { get; private set; }
        public IDictionary<TVertex, CompoundVertexInnerLayoutType> LayoutTypes { get; private set; }
    }
}
