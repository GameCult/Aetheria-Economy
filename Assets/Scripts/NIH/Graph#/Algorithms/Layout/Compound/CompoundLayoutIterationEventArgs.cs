using System.Collections.Generic;
using QuickGraph;
using System.Windows;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace GraphSharp.Algorithms.Layout.Compound
{
    public class CompoundLayoutIterationEventArgs<TVertex, TEdge>
        : LayoutIterationEventArgs<TVertex, TEdge>, ICompoundLayoutIterationEventArgs<TVertex>
        where TVertex : class
        where TEdge : IEdge<TVertex>
    {
        public CompoundLayoutIterationEventArgs(
            int iteration, 
            double statusInPercent, 
            string message,
            IDictionary<TVertex, float2> vertexPositions,
            IDictionary<TVertex, float2> innerCanvasSizes)
            : base(iteration, statusInPercent, message, vertexPositions)
        {
            InnerCanvasSizes = innerCanvasSizes;
        }

        #region ICompoundLayoutIterationEventArgs<TVertex> Members

        public IDictionary<TVertex, float2> InnerCanvasSizes
        {
            get; private set;
        }

        #endregion
    }
}
