using System;
using System.Collections.Generic;
using QuickGraph;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Random = System.Random;

namespace GraphSharp.Algorithms.Layout.Simple.FDP
{
	public class ISOMLayoutAlgorithm<TVertex, TEdge, TGraph> : DefaultParameterizedLayoutAlgorithmBase<TVertex, TEdge, TGraph, ISOMLayoutParameters>
		where TVertex : class
		where TEdge : IEdge<TVertex>
		where TGraph : IBidirectionalGraph<TVertex, TEdge>
	{
		#region Private fields
		private Queue<TVertex> _queue;
		private Dictionary<TVertex, ISOMData> _isomDataDict;
		private readonly Random _rnd = new Random( DateTime.Now.Millisecond );
		private float2 _tempPos;
		private float adaptation;
		private int radius;
		#endregion

		#region Constructors

		public ISOMLayoutAlgorithm( TGraph visitedGraph, ISOMLayoutParameters oldParameters )
			: base( visitedGraph )
		{
			Init( oldParameters );
		}

		public ISOMLayoutAlgorithm( TGraph visitedGraph, IDictionary<TVertex, float2> vertexPositions,
		                            ISOMLayoutParameters oldParameters )
			: base( visitedGraph, vertexPositions, oldParameters )
		{
			Init( oldParameters );
		}

		protected void Init( ISOMLayoutParameters oldParameters )
		{
			//init _parameters
			base.InitParameters( oldParameters );

			_queue = new Queue<TVertex>();
			_isomDataDict = new Dictionary<TVertex, ISOMData>();
			adaptation = Parameters.InitialAdaption;
		}
		#endregion

		protected override void InternalCompute()
		{
			//initialize vertex positions
			InitializeWithRandomPositions( Parameters.Width, Parameters.Height );

			//initialize ISOM data
			foreach ( var vertex in VisitedGraph.Vertices )
			{
				ISOMData isomData;
				if ( !_isomDataDict.TryGetValue( vertex, out isomData ) )
				{
					isomData = new ISOMData();
					_isomDataDict[vertex] = isomData;
				}
			}

			radius = Parameters.InitialRadius;
			for ( int epoch = 0; epoch < Parameters.MaxEpoch; epoch++ )
			{
				Adjust();

				//Update Parameters
				float factor = exp( -1 * Parameters.CoolingFactor * ( 1.0f * epoch / Parameters.MaxEpoch ) );
				adaptation = Math.Max( Parameters.MinAdaption, factor * Parameters.InitialAdaption );
				if ( radius > Parameters.MinRadius && epoch % Parameters.RadiusConstantTime == 0 )
				{
					radius--;
				}

				//report
				if ( ReportOnIterationEndNeeded )
					OnIterationEnded( epoch, (float)epoch / (float)Parameters.MaxEpoch, "Iteration " + epoch + " finished.", true );
                else if (ReportOnProgressChangedNeeded)
                    OnProgressChanged( (float)epoch / (float)Parameters.MaxEpoch * 100 );
			}
		}

		/// <summary>
		/// R�ntsunk egyet az �sszes ponton.
		/// </summary>
		protected void Adjust()
		{
			_tempPos = new float2();

			//get a random point in the container
			_tempPos.x = 0.1f * Parameters.Width + ( (float) _rnd.NextDouble() * 0.8f * Parameters.Width );
			_tempPos.y = 0.1f * Parameters.Height + ( (float) _rnd.NextDouble() * 0.8f * Parameters.Height );

			//find the closest vertex to this random point
			TVertex closest = GetClosest( _tempPos );

			//adjust the vertices to the selected vertex
			foreach ( TVertex v in VisitedGraph.Vertices )
			{
				ISOMData vid = _isomDataDict[v];
				vid.Distance = 0;
				vid.Visited = false;
			}
			AdjustVertex( closest );
		}

		private void AdjustVertex( TVertex closest )
		{
			_queue.Clear();
			ISOMData vid = _isomDataDict[closest];
			vid.Distance = 0;
			vid.Visited = true;
			_queue.Enqueue( closest );

			while ( _queue.Count > 0 )
			{
				TVertex current = _queue.Dequeue();
				ISOMData currentVid = _isomDataDict[current];
				float2 pos = VertexPositions[current];

				float2 force = _tempPos - pos;
				float factor = adaptation / pow( 2, currentVid.Distance );

				pos += factor * force;
				VertexPositions[current] = pos;

				//ha m�g a hat�k�r�n bel�l van
				if ( currentVid.Distance < radius )
				{
					//akkor a szomszedokra is hatassal vagyunk
					foreach ( TVertex neighbour in VisitedGraph.GetNeighbours<TVertex, TEdge>( current ) )
					{
						ISOMData nvid = _isomDataDict[neighbour];
						if ( !nvid.Visited )
						{
							nvid.Visited = true;
							nvid.Distance = currentVid.Distance + 1;
							_queue.Enqueue( neighbour );
						}
					}
				}
			}
		}

		/// <summary>
		/// Finds the the closest vertex to the given position.
		/// </summary>
		/// <param name="tempPos">The position.</param>
		/// <returns>Returns with the reference of the closest vertex.</returns>
		private TVertex GetClosest( float2 tempPos )
		{
			TVertex vertex = default( TVertex );
			float distance = float.MaxValue;

			//find the closest vertex
			foreach ( TVertex v in VisitedGraph.Vertices )
			{
				float d = length( tempPos - VertexPositions[v] );
				if ( d < distance )
				{
					vertex = v;
					distance = d;
				}
			}
			return vertex;
		}

		private class ISOMData
		{
			public float2 Force = new float2();
			public bool Visited = false;
			public float Distance = 0.0f;
		}
	}
}