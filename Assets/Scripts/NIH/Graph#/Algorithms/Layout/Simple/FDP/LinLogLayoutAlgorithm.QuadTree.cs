using System;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using QuickGraph;

namespace GraphSharp.Algorithms.Layout.Simple.FDP
{
	public partial class LinLogLayoutAlgorithm<TVertex, TEdge, TGraph> 
		where TVertex : class
		where TEdge : IEdge<TVertex>
		where TGraph : IBidirectionalGraph<TVertex, TEdge>
	{
		class QuadTree
		{
			#region Properties
			private readonly QuadTree[] children = new QuadTree[4];
			public QuadTree[] Children
			{
				get { return children; }
			}

			private int index;
			public int Index
			{
				get { return index; }
			}

			private float2 position;

			public float2 Position
			{
				get { return position; }
			}

			private float weight;

			public float Weight
			{
				get { return weight; }
			}

			private float2 minPos;
			private float2 maxPos;

			#endregion

			public float Width
			{
				get
				{
					return Math.Max( maxPos.x - minPos.x, maxPos.y - minPos.y );
				}
			}

			protected const int maxDepth = 20;

			public QuadTree( int index, float2 position, float weight, float2 minPos, float2 maxPos )
			{
				this.index = index;
				this.position = position;
				this.weight = weight;
				this.minPos = minPos;
				this.maxPos = maxPos;
			}

			public void AddNode( int nodeIndex, float2 nodePos, float nodeWeight, int depth )
			{
				if ( depth > maxDepth )
					return;

				if ( index >= 0 )
				{
					AddNode2( index, position, weight, depth );
					index = -1;
				}

				position.x = ( position.x * weight + nodePos.x * nodeWeight ) / ( weight + nodeWeight );
				position.y = ( position.y * weight + nodePos.y * nodeWeight ) / ( weight + nodeWeight );
				weight += nodeWeight;

				AddNode2( nodeIndex, nodePos, nodeWeight, depth );
			}

			protected void AddNode2( int nodeIndex, float2 nodePos, float nodeWeight, int depth )
			{
				//Debug.WriteLine( string.Format( "AddNode2 {0} {1} {2} {3}", nodeIndex, nodePos, nodeWeight, depth ) );
				int childIndex = 0;
				float middleX = ( minPos.x + maxPos.x ) / 2;
				float middleY = ( minPos.y + maxPos.y ) / 2;

				if ( nodePos.x > middleX )
					childIndex += 1;

				if ( nodePos.y > middleY )
					childIndex += 2;

				//Debug.WriteLine( string.Format( "childIndex: {0}", childIndex ) );               


				if ( children[childIndex] == null )
				{
					var newMin = new float2();
					var newMax = new float2();
					if ( nodePos.x <= middleX )
					{
						newMin.x = minPos.x;
						newMax.x = middleX;
					}
					else
					{
						newMin.x = middleX;
						newMax.x = maxPos.x;
					}
					if ( nodePos.y <= middleY )
					{
						newMin.y = minPos.y;
						newMax.y = middleY;
					}
					else
					{
						newMin.y = middleY;
						newMax.y = maxPos.y;
					}
					children[childIndex] = new QuadTree( nodeIndex, nodePos, nodeWeight, newMin, newMax );
				}
				else
				{
					children[childIndex].AddNode( nodeIndex, nodePos, nodeWeight, depth + 1 );
				}
			}

			/// <summary>
			/// Az adott rész pozícióját újraszámítja, levonva belőle a mozgatott node részét.
			/// </summary>
			/// <param name="oldPos"></param>
			/// <param name="newPos"></param>
			/// <param name="nodeWeight"></param>
			public void MoveNode( float2 oldPos, float2 newPos, float nodeWeight )
			{
				position += ( ( newPos - oldPos ) * ( nodeWeight / weight ) );

				int childIndex = 0;
				float middleX = ( minPos.x + maxPos.x ) / 2;
				float middleY = ( minPos.y + maxPos.y ) / 2;

				if ( oldPos.x > middleX )
					childIndex += 1;
				if ( oldPos.y > middleY )
					childIndex += 1 << 1;

				if ( children[childIndex] != null )
					children[childIndex].MoveNode( oldPos, newPos, nodeWeight );
			}
		}
	}
}