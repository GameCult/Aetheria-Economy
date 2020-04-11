using System.Collections.Generic;
using System.Windows;
using QuickGraph;
using Unity.Mathematics;
using static Unity.Mathematics.math;


namespace GraphSharp.Algorithms.Layout.Contextual
{
	public class RadialTreeLayoutAlgorithm<TVertex, TEdge, TGraph> : DefaultParameterizedLayoutAlgorithmBase<TVertex, TEdge, TGraph, RadialTreeLayoutParameters>
		where TVertex : class
		where TEdge : IEdge<TVertex>
		where TGraph : IBidirectionalGraph<TVertex, TEdge>
	{
		private readonly TVertex root;

		public RadialTreeLayoutAlgorithm(
			TGraph visitedGraph,
			IDictionary<TVertex, float2> vertexPositions,
			RadialTreeLayoutParameters oldParameters,
			TVertex selectedVertex )
			: base( visitedGraph, vertexPositions, oldParameters )
		{
			this.root = selectedVertex;
		}

		protected override void InternalCompute()
		{
			
		}
	}
}
