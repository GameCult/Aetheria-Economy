using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using QuickGraph;
using System.Diagnostics;
using QuickGraph.Algorithms.Search;
using QuickGraph.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace GraphSharp.Algorithms.Layout.Simple.Tree
{
    public partial class SimpleTreeLayoutAlgorithm<TVertex, TEdge, TGraph> : DefaultParameterizedLayoutAlgorithmBase<TVertex, TEdge, TGraph, SimpleTreeLayoutParameters>
        where TVertex : class
        where TEdge : IEdge<TVertex>
        where TGraph : IBidirectionalGraph<TVertex, TEdge>
    {
        private BidirectionalGraph<TVertex, Edge<TVertex>> spanningTree;
        readonly IDictionary<TVertex, float2> sizes;
        readonly IDictionary<TVertex, VertexData> data = new Dictionary<TVertex, VertexData>();
        readonly IList<Layer> layers = new List<Layer>();
        int direction;

        public SimpleTreeLayoutAlgorithm( TGraph visitedGraph, IDictionary<TVertex, float2> vertexPositions, IDictionary<TVertex, float2> vertexSizes, SimpleTreeLayoutParameters parameters )
            : base( visitedGraph, vertexPositions, parameters )
        {
            //Contract.Requires( vertexSizes != null );
            //Contract.Requires( visitedGraph.Vertices.All( v => vertexSizes.ContainsKey( v ) ) );

            sizes = new Dictionary<TVertex, float2>( vertexSizes );
        }

        protected override void InternalCompute()
        {
            if ( Parameters.Direction == LayoutDirection.LeftToRight || Parameters.Direction == LayoutDirection.RightToLeft )
            {
                //change the sizes
                foreach ( var sizePair in sizes.ToArray() )
                    sizes[sizePair.Key] = new float2( sizePair.Value.y, sizePair.Value.x );
            }

            if ( Parameters.Direction == LayoutDirection.RightToLeft || Parameters.Direction == LayoutDirection.BottomToTop )
                direction = -1;
            else
                direction = 1;

            GenerateSpanningTree();
            //DoWidthAndHeightOptimization();

            //first layout the vertices with 0 in-edge
            foreach ( var source in spanningTree.Vertices.Where( v => spanningTree.InDegree( v ) == 0 ) )
                CalculatePosition( source, null, 0 );

            //then the others
            foreach ( var source in spanningTree.Vertices )
                CalculatePosition( source, null, 0 );

            AssignPositions();
        }

        private void GenerateSpanningTree()
        {
            spanningTree = new BidirectionalGraph<TVertex, Edge<TVertex>>( false );
            spanningTree.AddVertexRange( VisitedGraph.Vertices );
            IQueue<TVertex> vb = new QuickGraph.Collections.Queue<TVertex>();
            vb.Enqueue( VisitedGraph.Vertices.OrderBy( v => VisitedGraph.InDegree( v ) ).First() );
            switch ( Parameters.SpanningTreeGeneration )
            {
                case SpanningTreeGeneration.BFS:
                    var bfsAlgo = new BreadthFirstSearchAlgorithm<TVertex, TEdge>( VisitedGraph, vb, new Dictionary<TVertex, GraphColor>() );
                    bfsAlgo.TreeEdge += e => spanningTree.AddEdge( new Edge<TVertex>( e.Source, e.Target ) );
                    bfsAlgo.Compute();
                    break;
                case SpanningTreeGeneration.DFS:
                    var dfsAlgo = new DepthFirstSearchAlgorithm<TVertex, TEdge>( VisitedGraph );
                    dfsAlgo.TreeEdge += e => spanningTree.AddEdge( new Edge<TVertex>( e.Source, e.Target ) );
                    dfsAlgo.Compute();
                    break;
            }
        }

        protected float CalculatePosition( TVertex v, TVertex parent, int l )
        {
            if ( data.ContainsKey( v ) )
                return -1; //this vertex is already layed out

            while ( l >= layers.Count )
                layers.Add( new Layer() );

            var layer = layers[l];
            var size = sizes[v];
            var d = new VertexData { parent = parent };
            data[v] = d;

            layer.NextPosition += size.x / 2.0f;
            if ( l > 0 )
            {
                layer.NextPosition += layers[l - 1].LastTranslate;
                layers[l - 1].LastTranslate = 0;
            }
            layer.Size = max( layer.Size, size.y + Parameters.LayerGap );
            layer.Vertices.Add( v );
            if ( spanningTree.OutDegree( v ) == 0 )
            {
                d.position = layer.NextPosition;
            }
            else
            {
                float minPos = float.MaxValue;
                float maxPos = -float.MaxValue;
                //first put the children
                foreach ( var child in spanningTree.OutEdges( v ).Select( e => e.Target ) )
                {
                    float childPos = CalculatePosition( child, v, l + 1 );
                    if ( childPos >= 0 )
                    {
                        minPos = min( minPos, childPos );
                        maxPos = max( maxPos, childPos );
                    }
                }
                if ( minPos != float.MaxValue )
                    d.position = ( minPos + maxPos ) / 2.0f;
                else
                    d.position = layer.NextPosition;
                d.translate = Math.Max( layer.NextPosition - d.position, 0 );

                layer.LastTranslate = d.translate;
                d.position += d.translate;
                layer.NextPosition = d.position;
            }
            layer.NextPosition += size.x / 2.0f + Parameters.VertexGap;

            return d.position;
        }

        protected void AssignPositions()
        {
            float layerSize = 0;
            bool changeCoordinates = ( Parameters.Direction == LayoutDirection.LeftToRight || Parameters.Direction == LayoutDirection.RightToLeft );

            foreach ( var layer in layers )
            {
                foreach ( var v in layer.Vertices )
                {
                    float2 size = sizes[v];
                    var d = data[v];
                    if ( d.parent != null )
                    {
                        d.position += data[d.parent].translate;
                        d.translate += data[d.parent].translate;
                    }

                    VertexPositions[v] =
                        changeCoordinates
                            ? new float2( direction * ( layerSize + size.y / 2.0f ), d.position )
                            : new float2( d.position, direction * ( layerSize + size.y / 2.0f ) );
                }
                layerSize += layer.Size;
            }

            if ( direction < 0 )
                NormalizePositions();
        }
    }
}