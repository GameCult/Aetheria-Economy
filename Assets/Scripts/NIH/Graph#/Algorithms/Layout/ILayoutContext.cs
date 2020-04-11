using System.Collections.Generic;
using System.Diagnostics.Contracts;
using QuickGraph;
using System.Windows;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace GraphSharp.Algorithms.Layout
{
    public interface ILayoutContext<TVertex, TEdge, TGraph>
        where TEdge : IEdge<TVertex>
        where TGraph : IVertexAndEdgeListGraph<TVertex, TEdge>
    {
        IDictionary<TVertex, float2> Positions { get; }
        IDictionary<TVertex, float2> Sizes { get; }

        TGraph Graph { get; }

        LayoutMode Mode { get; }
    }
}