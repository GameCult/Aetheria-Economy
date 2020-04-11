using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using QuickGraph;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace GraphSharp.Algorithms.Layout.Compound
{
    public class TestingCompoundLayoutIterationEventArgs<TVertex, TEdge, TVertexInfo, TEdgeInfo>
        : CompoundLayoutIterationEventArgs<TVertex, TEdge>, ILayoutInfoIterationEventArgs<TVertex, TEdge, TVertexInfo, TEdgeInfo>
        where TVertex : class 
        where TEdge : IEdge<TVertex>
    {
        private IDictionary<TVertex, TVertexInfo> vertexInfos;

        public float2 GravitationCenter { get; private set; }

        public TestingCompoundLayoutIterationEventArgs(
            int iteration, 
            double statusInPercent, 
            string message, 
            IDictionary<TVertex, float2> vertexPositions, 
            IDictionary<TVertex, float2> innerCanvasSizes,
            IDictionary<TVertex, TVertexInfo> vertexInfos,
            float2 gravitationCenter) 
            : base(iteration, statusInPercent, message, vertexPositions, innerCanvasSizes)
        {
            this.vertexInfos = vertexInfos;
            this.GravitationCenter = gravitationCenter;
        }

        public override object GetVertexInfo(TVertex vertex)
        {
            TVertexInfo info = default(TVertexInfo);
            if (vertexInfos.TryGetValue(vertex, out info))
                return info;

            return null;
        }

        public IDictionary<TVertex, TVertexInfo> VertexInfos
        {
            get { return this.vertexInfos; }
        }

        public IDictionary<TEdge, TEdgeInfo> EdgeInfos
        {
            get { return null; }
        }
    }
}
