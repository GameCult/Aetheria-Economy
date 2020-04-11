using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace GraphSharp.Algorithms.Layout
{
	public class LayoutState<TVertex, TEdge>
	{
		/// <summary>
		/// Gets the position of every vertex in this state of the layout process.
		/// </summary>
		public IDictionary<TVertex, float2> Positions { get; protected set; }

		public IDictionary<TVertex, float2> OverlapRemovedPositions { get; set; }

		public IDictionary<TEdge, float2[]> RouteInfos { get; set; }

		/// <summary>
		/// Gets how much time did it take to compute the position of the vertices (till the end of this iteration).
		/// </summary>
		public TimeSpan ComputationTime { get; protected set; }

		/// <summary>
		/// Gets the index of the iteration.
		/// </summary>
		public int Iteration { get; protected set; }

		/// <summary>
		/// Get the status message of this layout state.
		/// </summary>
		public string Message { get; protected set; }

		public LayoutState(
			IDictionary<TVertex, float2> positions,
			IDictionary<TVertex, float2> overlapRemovedPositions,
			IDictionary<TEdge, float2[]> routeInfos,
			TimeSpan computationTime,
			int iteration,
			string message )
		{
			Debug.Assert( computationTime != null );

			Positions = positions;
			OverlapRemovedPositions = overlapRemovedPositions != null ? overlapRemovedPositions : positions;

			if ( routeInfos != null )
				RouteInfos = routeInfos;
			else
				RouteInfos = new Dictionary<TEdge, float2[]>( 0 );

			ComputationTime = computationTime;
			Iteration = iteration;
			Message = message;
		}
	}
}