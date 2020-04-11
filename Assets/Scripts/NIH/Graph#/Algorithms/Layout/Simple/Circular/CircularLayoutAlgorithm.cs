using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Windows;
using System.Diagnostics;
using QuickGraph;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace GraphSharp.Algorithms.Layout.Simple.Circular
{
    public class CircularLayoutAlgorithm<TVertex, TEdge, TGraph> : DefaultParameterizedLayoutAlgorithmBase<TVertex, TEdge, TGraph, CircularLayoutParameters>
        where TVertex : class
        where TEdge : IEdge<TVertex>
        where TGraph : IBidirectionalGraph<TVertex, TEdge>
    {
        readonly IDictionary<TVertex, float2> sizes;

        public CircularLayoutAlgorithm( TGraph visitedGraph, IDictionary<TVertex, float2> vertexPositions, IDictionary<TVertex, float2> vertexSizes, CircularLayoutParameters parameters )
            : base( visitedGraph, vertexPositions, parameters )
        {
            //Contract.Requires( vertexSizes != null );
            //Contract.Requires( visitedGraph.Vertices.All( v => vertexSizes.ContainsKey( v ) ) );

            sizes = vertexSizes;
        }

        protected override void InternalCompute()
        {
            //calculate the size of the circle
            float perimeter = 0;
            float[] halfSize = new float[VisitedGraph.VertexCount];
            int i = 0;
            foreach ( var v in VisitedGraph.Vertices )
            {
                float2 s = sizes[v];
                halfSize[i] = sqrt( s.x * s.x + s.y * s.y ) * 0.5f;
                perimeter += halfSize[i] * 2;
                i++;
            }

            float radius = perimeter / ( 2 * PI );

            //
            //precalculation
            //
            float angle = 0, a;
            i = 0;
            foreach ( var v in VisitedGraph.Vertices )
            {
                a = sin( halfSize[i] * 0.5f / radius ) * 2;
                angle += a;
                if ( ReportOnIterationEndNeeded )
                    VertexPositions[v] = new float2( cos( angle ) * radius + radius, sin( angle ) * radius + radius );
                angle += a;
            }

            if ( ReportOnIterationEndNeeded )
                OnIterationEnded( 0, 50, "Precalculation done.", false );

            //recalculate radius
            radius = angle / ( 2 * PI ) * radius;

            //calculation
            angle = 0;
            i = 0;
            foreach ( var v in VisitedGraph.Vertices )
            {
                a = sin( halfSize[i] * 0.5f / radius ) * 2;
                angle += a;
                VertexPositions[v] = new float2( cos( angle ) * radius + radius, sin( angle ) * radius + radius );
                angle += a;
            }
        }
    }
}