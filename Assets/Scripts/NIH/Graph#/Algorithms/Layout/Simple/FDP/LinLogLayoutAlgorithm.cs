using System;
using System.Collections.Generic;
using QuickGraph;
using System.Linq;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace GraphSharp.Algorithms.Layout.Simple.FDP
{
	public partial class LinLogLayoutAlgorithm<TVertex, TEdge, TGraph> : DefaultParameterizedLayoutAlgorithmBase<TVertex, TEdge, TGraph, LinLogLayoutParameters>
		where TVertex : class
		where TEdge : IEdge<TVertex>
		where TGraph : IBidirectionalGraph<TVertex, TEdge>
	{
		#region Constructors
		public LinLogLayoutAlgorithm( TGraph visitedGraph )
			: base( visitedGraph ) { }

		public LinLogLayoutAlgorithm( TGraph visitedGraph, IDictionary<TVertex, float2> positions,
		                              LinLogLayoutParameters parameters )
			: base( visitedGraph, positions, parameters ) { }
		#endregion

		#region Member variables - privates
		class LinLogVertex
		{
			public int Index;
			public TVertex OriginalVertex;
			public LinLogEdge[] Attractions;
			public float RepulsionWeight;
			public float2 Position;
		}

		class LinLogEdge
		{
			public LinLogVertex Target;
			public float AttractionWeight;
		}

		private LinLogVertex[] vertices;
		private float2 baryCenter;
		private float repulsionMultiplier;

		#endregion


		protected override void InternalCompute()
		{
			if ( VisitedGraph.VertexCount <= 1 ) return;

			InitializeWithRandomPositions( 1, 1, -0.5f, -0.5f );

			InitAlgorithm();
			QuadTree quadTree;

			float finalRepuExponent = Parameters.repulsiveExponent;
			float finalAttrExponent = Parameters.attractionExponent;

			for ( int step = 1; step <= Parameters.iterationCount; step++ )
			{
				ComputeBaryCenter();
				quadTree = BuildQuadTree();

				#region h�l�si f�ggv�ny meghat�roz�sa
				if ( Parameters.iterationCount >= 50 && finalRepuExponent < 1.0 )
				{
					Parameters.attractionExponent = finalAttrExponent;
					Parameters.repulsiveExponent = finalRepuExponent;
					if ( step <= 0.6 * Parameters.iterationCount )
					{
						// use energy model with few local minima 
						Parameters.attractionExponent += 1.1f * ( 1.0f - finalRepuExponent );
						Parameters.repulsiveExponent += 0.9f * ( 1.0f - finalRepuExponent );
					}
					else if ( step <= 0.9 * Parameters.iterationCount )
					{
						// gradually move to final energy model
						Parameters.attractionExponent +=
							1.1f * ( 1.0f - finalRepuExponent ) * ( 0.9f - step / (float)Parameters.iterationCount ) / 0.3f;
						Parameters.repulsiveExponent +=
							0.9f * ( 1.0f - finalRepuExponent ) * ( 0.9f - step / (float)Parameters.iterationCount ) / 0.3f;
					}
				}
				#endregion

				#region Move each node
				for ( int i = 0; i < vertices.Length; i++ )
				{
					var v = vertices[i];
					float oldEnergy = GetEnergy( i, quadTree );

					// compute direction of the move of the node
					float2 bestDir;
					GetDirection( i, quadTree, out bestDir );

					// line search: compute length of the move
					float2 oldPos = v.Position;

					float bestEnergy = oldEnergy;
					int bestMultiple = 0;
					bestDir /= 32;
					//kisebb mozgat�sok eset�n a legjobb eset meghat�roz�sa
					for ( int multiple = 32;
					      multiple >= 1 && ( bestMultiple == 0 || bestMultiple / 2 == multiple );
					      multiple /= 2 )
					{
						v.Position = oldPos + bestDir * multiple;
						float curEnergy = GetEnergy( i, quadTree );
						if ( curEnergy < bestEnergy )
						{
							bestEnergy = curEnergy;
							bestMultiple = multiple;
						}
					}

					//nagyobb mozgat�s eset�n van-e jobb megold�s?
					for ( int multiple = 64;
					      multiple <= 128 && bestMultiple == multiple / 2;
					      multiple *= 2 )
					{
						v.Position = oldPos + bestDir * multiple;
						float curEnergy = GetEnergy( i, quadTree );
						if ( curEnergy < bestEnergy )
						{
							bestEnergy = curEnergy;
							bestMultiple = multiple;
						}
					}

					//legjobb megold�ssal mozgat�s
					v.Position = oldPos + bestDir * bestMultiple;
					if ( bestMultiple > 0 )
					{
						quadTree.MoveNode( oldPos, v.Position, v.RepulsionWeight );
					}
				}
				#endregion

				if ( ReportOnIterationEndNeeded )
					Report( step );
			}
			CopyPositions();
			NormalizePositions();
		}

		protected void CopyPositions()
		{
			// Copy positions
			foreach ( var v in vertices )
				VertexPositions[v.OriginalVertex] = v.Position;
		}

		protected void Report( int step )
		{
			CopyPositions();
			OnIterationEnded( step, step / Parameters.iterationCount * 100, "Iteration " + step + " finished.", true );
		}

		private void GetDirection( int index, QuadTree quadTree, out float2 dir )
		{
			dir = new float2( 0, 0 );

			float dir2 = AddRepulsionDirection( index, quadTree, ref dir );
			dir2 += AddAttractionDirection( index, ref dir );
			dir2 += AddGravitationDirection( index, ref dir );

			if ( dir2 != 0.0 )
			{
				dir /= dir2;

				float length = math.length(dir);
				if ( length > quadTree.Width / 8 )
				{
					length /= quadTree.Width / 8;
					dir /= length;
				}
			}
			else { dir = new float2( 0, 0 ); }
		}

		private float AddGravitationDirection( int index, ref float2 dir )
		{
			var v = vertices[index];
			float2 gravitationVector = ( baryCenter - v.Position );
			float dist = math.length(gravitationVector);
			float tmp = Parameters.gravitationMultiplier * repulsionMultiplier * max( v.RepulsionWeight, 1 ) * pow( dist, Parameters.attractionExponent - 2 );
			dir += gravitationVector * tmp;

			return tmp * Math.Abs( Parameters.attractionExponent - 1 );
		}

		private float AddAttractionDirection( int index, ref float2 dir )
		{
			float dir2 = 0.0f;
			var v = vertices[index];
			foreach ( var e in v.Attractions )
			{
				//onhurkok elhagyasa
				if ( e.Target == v )
					continue;

				float2 attractionVector = ( e.Target.Position - v.Position );
				float dist = math.length(attractionVector);
				if ( dist <= 0 )
					continue;

				float tmp = e.AttractionWeight * pow( dist, Parameters.attractionExponent - 2 );
				dir2 += tmp * Math.Abs( Parameters.attractionExponent - 1 );

				dir += ( e.Target.Position - v.Position ) * tmp;
			}
			return dir2;
		}

		/// <summary>
		/// Kisz�m�tja az <code>index</code> sorsz�m� pontra hat� er�t a 
		/// quadTree seg�ts�g�vel.
		/// </summary>
		/// <param name="index">A node sorsz�ma, melyre a repulz�v er�t sz�m�tani akarjuk.</param>
		/// <param name="quadTree"></param>
		/// <param name="dir">A repulz�v er�t hozz�adja ehhez a Vectorhoz.</param>
		/// <returns>Becs�lt m�sodik deriv�ltja a repulz�v energi�nak.</returns>
		private float AddRepulsionDirection( int index, QuadTree quadTree, ref float2 dir )
		{
			var v = vertices[index];

			if ( quadTree == null || quadTree.Index == index || v.RepulsionWeight <= 0 )
				return 0.0f;

			float2 repulsionVector = ( quadTree.Position - v.Position );
			float dist = math.length(repulsionVector);
			if ( quadTree.Index < 0 && dist < 2.0 * quadTree.Width )
			{
				float dir2 = 0.0f;
				for ( int i = 0; i < quadTree.Children.Length; i++ )
					dir2 += AddRepulsionDirection( index, quadTree.Children[i], ref dir );
				return dir2;
			}

			if ( dist != 0.0 )
			{
				float tmp = repulsionMultiplier * v.RepulsionWeight * quadTree.Weight
				             * pow( dist, Parameters.repulsiveExponent - 2 );
				dir -= repulsionVector * tmp;
				return tmp * abs( Parameters.repulsiveExponent - 1 );
			}

			return 0.0f;
		}

		/*
				private float GetEnergySum( QuadTree q )
				{
					float sum = 0;
					for ( int i = 0; i < vertices.Length; i++ )
						sum += GetEnergy( i, q );
					return sum;
				}
		*/

		private float GetEnergy( int index, QuadTree q )
		{
			return GetRepulsionEnergy( index, q )
			       + GetAttractionEnergy( index ) + GetGravitationEnergy( index );
		}

		private float GetGravitationEnergy( int index )
		{
			var v = vertices[index];

			float dist = length( v.Position - baryCenter );
			return Parameters.gravitationMultiplier * repulsionMultiplier * max( v.RepulsionWeight, 1 )
			       * pow( dist, Parameters.attractionExponent ) / Parameters.attractionExponent;
		}

		private float GetAttractionEnergy( int index )
		{
			float energy = 0.0f;
			var v = vertices[index];
			foreach ( var e in v.Attractions )
			{
				if ( e.Target == v )
					continue;

				float dist = length( e.Target.Position - v.Position );
				energy += e.AttractionWeight * pow( dist, Parameters.attractionExponent ) / Parameters.attractionExponent;
			}
			return energy;
		}

		private float GetRepulsionEnergy( int index, QuadTree tree )
		{
			if ( tree == null || tree.Index == index || index >= vertices.Length )
				return 0.0f;

			var v = vertices[index];

			float dist = length( v.Position - tree.Position );
			if ( tree.Index < 0 && dist < ( 2 * tree.Width ) )
			{
				float energy = 0.0f;
				for ( int i = 0; i < tree.Children.Length; i++ )
					energy += GetRepulsionEnergy( index, tree.Children[i] );

				return energy;
			}

			if ( Parameters.repulsiveExponent == 0.0 )
				return -repulsionMultiplier * v.RepulsionWeight * tree.Weight * log( dist );

			return -repulsionMultiplier * v.RepulsionWeight * tree.Weight
			       * pow( dist, Parameters.repulsiveExponent ) / Parameters.repulsiveExponent;
		}

		private void InitAlgorithm()
		{
			vertices = new LinLogVertex[VisitedGraph.VertexCount];

			var vertexMap = new Dictionary<TVertex, LinLogVertex>();

			//vertexek indexel�se
			int i = 0;
			foreach ( TVertex v in VisitedGraph.Vertices )
			{
				vertices[i] = new LinLogVertex
				              	{
				              		Index = i,
				              		OriginalVertex = v,
				              		Attractions = new LinLogEdge[VisitedGraph.Degree( v )],
				              		RepulsionWeight = 0,
				              		Position = VertexPositions[v]
				              	};
				vertexMap[v] = vertices[i];
				i++;
			}

			//minden vertex-hez fel�p�ti az attractionWeights, attractionIndexes,
			//�s a repulsionWeights strukt�r�t, valamint �tm�solja a poz�ci�j�t a VertexPositions-b�l
			foreach ( var v in vertices )
			{
				int attrIndex = 0;
				foreach ( var e in VisitedGraph.InEdges( v.OriginalVertex ) )
				{
					float weight = e is WeightedEdge<TVertex> ? ( ( e as WeightedEdge<TVertex> ).Weight ) : 1;
					v.Attractions[attrIndex] = new LinLogEdge
					                           	{
					                           		Target = vertexMap[e.Source],
					                           		AttractionWeight = weight
					                           	};
					//TODO look at this line below
					//v.RepulsionWeight += weight;
					v.RepulsionWeight += 1;
					attrIndex++;
				}

				foreach ( var e in VisitedGraph.OutEdges( v.OriginalVertex ) )
				{
					float weight = e is WeightedEdge<TVertex> ? ( ( e as WeightedEdge<TVertex> ).Weight ) : 1;
					v.Attractions[attrIndex] = new LinLogEdge
					                           	{
					                           		Target = vertexMap[e.Target],
					                           		AttractionWeight = weight
					                           	};
					//v.RepulsionWeight += weight;
					v.RepulsionWeight += 1;
					attrIndex++;
				}
				v.RepulsionWeight = Math.Max( v.RepulsionWeight, Parameters.gravitationMultiplier );
			}

			repulsionMultiplier = ComputeRepulsionMultiplier();
		}

		private void ComputeBaryCenter()
		{
			baryCenter = new float2( 0, 0 );
			float repWeightSum = 0.0f;
			foreach ( var v in vertices )
			{
				repWeightSum += v.RepulsionWeight;
				baryCenter.x += v.Position.x * v.RepulsionWeight;
				baryCenter.y += v.Position.y * v.RepulsionWeight;
			}
			if ( repWeightSum > 0.0 )
			{
				baryCenter.x /= repWeightSum;
				baryCenter.y /= repWeightSum;
			}
		}

		private float ComputeRepulsionMultiplier()
		{
			float attractionSum = vertices.Sum( v => v.Attractions.Sum( e => e.AttractionWeight ) );
			float repulsionSum = vertices.Sum( v => v.RepulsionWeight );

			if ( repulsionSum > 0 && attractionSum > 0 )
				return attractionSum / pow( repulsionSum, 2 ) * pow( repulsionSum, 0.5f * ( Parameters.attractionExponent - Parameters.repulsiveExponent ) );

			return 1;
		}

		/// <summary>
		/// Fel�p�t egy QuadTree-t (olyan mint az OctTree, csak 2D-ben).
		/// </summary>
		private QuadTree BuildQuadTree()
		{
			//a minim�lis �s maxim�lis poz�ci� sz�m�t�sa
			var minPos = new float2( float.MaxValue, float.MaxValue );
			var maxPos = new float2( -float.MaxValue, -float.MaxValue );

			foreach ( var v in vertices )
			{
				if ( v.RepulsionWeight <= 0 )
					continue;

				minPos.x = Math.Min( minPos.x, v.Position.x );
				minPos.y = Math.Min( minPos.y, v.Position.y );
				maxPos.x = Math.Max( maxPos.x, v.Position.x );
				maxPos.y = Math.Max( maxPos.y, v.Position.y );
			}

			//a nemnulla repulsionWeight-el rendelkez� node-ok hozz�ad�sa a QuadTree-hez.
			QuadTree result = null;
			foreach ( var v in vertices )
			{
				if ( v.RepulsionWeight <= 0 )
					continue;

				if ( result == null )
					result = new QuadTree( v.Index, v.Position, v.RepulsionWeight, minPos, maxPos );
				else
					result.AddNode( v.Index, v.Position, v.RepulsionWeight, 0 );
			}
			return result;
		}
	}
}