using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using QuickGraph;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using System.Diagnostics;

namespace GraphSharp.Algorithms.Layout.Simple.FDP
{
    public class KKLayoutAlgorithm<Vertex, Edge, Graph> : DefaultParameterizedLayoutAlgorithmBase<Vertex, Edge, Graph, KKLayoutParameters>
        where Vertex : class
        where Edge : IEdge<Vertex>
        where Graph : IBidirectionalGraph<Vertex, Edge>
    {

        #region Variables needed for the layout
        /// <summary>
        /// Minimal distances between the vertices.
        /// </summary>
        private float[,] distances;
        private float[,] edgeLengths;
        private float[,] springConstants;

        //cache for speed-up
        private Vertex[] vertices;
        /// <summary>
        /// Positions of the vertices, stored by indices.
        /// </summary>
        private float2[] positions;

        private float diameter;
        private float idealEdgeLength;
        #endregion

        #region Contructors
        public KKLayoutAlgorithm( Graph visitedGraph, KKLayoutParameters oldParameters )
            : this( visitedGraph, null, oldParameters ) { }

        public KKLayoutAlgorithm( Graph visitedGraph, IDictionary<Vertex, float2> vertexPositions,
                                  KKLayoutParameters oldParameters )
            : base( visitedGraph, vertexPositions, oldParameters ) { }
        #endregion

        protected override void InternalCompute()
        {
            #region Initialization
            distances = new float[VisitedGraph.VertexCount, VisitedGraph.VertexCount];
            edgeLengths = new float[VisitedGraph.VertexCount, VisitedGraph.VertexCount];
            springConstants = new float[VisitedGraph.VertexCount, VisitedGraph.VertexCount];
            vertices = new Vertex[VisitedGraph.VertexCount];
            positions = new float2[VisitedGraph.VertexCount];

            //initializing with random positions
            InitializeWithRandomPositions( Parameters.Width, Parameters.Height );

            //copy positions into array (speed-up)
            int index = 0;
            foreach ( var v in VisitedGraph.Vertices )
            {
                vertices[index] = v;
                positions[index] = VertexPositions[v];
                index++;
            }

            //calculating the diameter of the graph
            //TODO check the diameter algorithm
            diameter = VisitedGraph.GetDiameter<Vertex, Edge, Graph>( out distances );

            //L0 is the length of a side of the display area
            float L0 = Math.Min( Parameters.Width, Parameters.Height );

            //ideal length = L0 / max d_i,j
            idealEdgeLength = ( L0 / diameter ) * Parameters.LengthFactor;

            //calculating the ideal distance between the nodes
            for ( int i = 0; i < VisitedGraph.VertexCount - 1; i++ )
            {
                for ( int j = i + 1; j < VisitedGraph.VertexCount; j++ )
                {
                    //distance between non-adjacent vertices
                    float dist = diameter * Parameters.DisconnectedMultiplier;

                    //calculating the minimal distance between the vertices
                    if ( distances[i, j] != float.MaxValue )
                        dist = Math.Min( distances[i, j], dist );
                    if ( distances[j, i] != float.MaxValue )
                        dist = Math.Min( distances[j, i], dist );
                    distances[i, j] = distances[j, i] = dist;
                    edgeLengths[i, j] = edgeLengths[j, i] = idealEdgeLength * dist;
                    springConstants[i, j] = springConstants[j, i] = Parameters.K / pow( dist, 2 );
                }
            }
            #endregion

            int n = VisitedGraph.VertexCount;
            if ( n == 0 )
                return;

            //TODO check this condition
            for ( int currentIteration = 0; currentIteration < Parameters.MaxIterations; currentIteration++ )
            {
                #region An iteration
                float maxDeltaM = float.NegativeInfinity;
                int pm = -1;

                //get the 'p' with the max delta_m
                for ( int i = 0; i < n; i++ )
                {
                    float deltaM = CalculateEnergyGradient( i );
                    if ( maxDeltaM < deltaM )
                    {
                        maxDeltaM = deltaM;
                        pm = i;
                    }
                }
                //TODO is needed?
                if ( pm == -1 )
                    return;

                //calculating the delta_x & delta_y with the Newton-Raphson method
                //there is an upper-bound for the while (deltaM > epsilon) {...} cycle (100)
                for ( int i = 0; i < 100; i++ )
                {
                    positions[pm] += CalcDeltaXY( pm );

                    float deltaM = CalculateEnergyGradient( pm );
                    //real stop condition
                    if ( deltaM < float.Epsilon )
                        break;
                }

                //what if some of the vertices would be exchanged?
                if ( Parameters.ExchangeVertices && maxDeltaM < float.Epsilon )
                {
                    float energy = CalcEnergy();
                    for ( int i = 0; i < n - 1; i++ )
                    {
                        for ( int j = i + 1; j < n; j++ )
                        {
                            float xenergy = CalcEnergyIfExchanged( i, j );
                            if ( energy > xenergy )
                            {
                                float2 p = positions[i];
                                positions[i] = positions[j];
                                positions[j] = p;
                                return;
                            }
                        }
                    }
                }
                #endregion

                if ( ReportOnIterationEndNeeded )
                    Report( currentIteration );
            }
            Report( Parameters.MaxIterations );
        }

        protected void Report( int currentIteration )
        {
            #region Copy the calculated positions
            //poz�ci�k �tm�sol�sa a VertexPositions-ba
            for ( int i = 0; i < vertices.Length; i++ )
                VertexPositions[vertices[i]] = positions[i];
            #endregion

            OnIterationEnded( currentIteration, (float)currentIteration / (float)Parameters.MaxIterations, "Iteration " + currentIteration + " finished.", true );
        }

        /// <returns>
        /// Calculates the energy of the state where 
        /// the positions of the vertex 'p' & 'q' are exchanged.
        /// </returns>
        private float CalcEnergyIfExchanged( int p, int q )
        {
            float energy = 0;
            for ( int i = 0; i < vertices.Length - 1; i++ )
            {
                for ( int j = i + 1; j < vertices.Length; j++ )
                {
                    int ii = ( i == p ) ? q : i;
                    int jj = ( j == q ) ? p : j;

                    float l_ij = edgeLengths[i, j];
                    float k_ij = springConstants[i, j];
                    float dx = positions[ii].x - positions[jj].x;
                    float dy = positions[ii].y - positions[jj].y;

                    energy += k_ij / 2 * ( dx * dx + dy * dy + l_ij * l_ij -
                                           2 * l_ij * sqrt( dx * dx + dy * dy ) );
                }
            }
            return energy;
        }

        /// <summary>
        /// Calculates the energy of the spring system.
        /// </summary>
        /// <returns>Returns with the energy of the spring system.</returns>
        private float CalcEnergy()
        {
            float energy = 0, dist, l_ij, k_ij, dx, dy;
            for ( int i = 0; i < vertices.Length - 1; i++ )
            {
                for ( int j = i + 1; j < vertices.Length; j++ )
                {
                    dist = distances[i, j];
                    l_ij = edgeLengths[i, j];
                    k_ij = springConstants[i, j];

                    dx = positions[i].x - positions[j].x;
                    dy = positions[i].y - positions[j].y;

                    energy += k_ij / 2 * ( dx * dx + dy * dy + l_ij * l_ij -
                                           2 * l_ij * sqrt( dx * dx + dy * dy ) );
                }
            }
            return energy;
        }

        /// <summary>
        /// Determines a step to new position of the vertex m.
        /// </summary>
        /// <returns></returns>
        private float2 CalcDeltaXY( int m )
        {
            float dxm = 0, dym = 0, d2xm = 0, dxmdym = 0, dymdxm = 0, d2ym = 0;
            float l, k, dx, dy, d, ddd;

            for ( int i = 0; i < vertices.Length; i++ )
            {
                if ( i != m )
                {
                    //common things
                    l = edgeLengths[m, i];
                    k = springConstants[m, i];
                    dx = positions[m].x - positions[i].x;
                    dy = positions[m].y - positions[i].y;

                    //distance between the points
                    d = sqrt( dx * dx + dy * dy );
                    ddd = pow( d, 3 );

                    dxm += k * ( 1 - l / d ) * dx;
                    dym += k * ( 1 - l / d ) * dy;
                    //TODO isn't it wrong?
                    d2xm += k * ( 1 - l * pow( dy, 2 ) / ddd );
                    //d2E_d2xm += k_mi * ( 1 - l_mi / d + l_mi * dx * dx / ddd );
                    dxmdym += k * l * dx * dy / ddd;
                    //d2E_d2ym += k_mi * ( 1 - l_mi / d + l_mi * dy * dy / ddd );
                    //TODO isn't it wrong?
                    d2ym += k * ( 1 - l * pow( dx, 2 ) / ddd );
                }
            }
            // d2E_dymdxm equals to d2E_dxmdym
            dymdxm = dxmdym;

            float denomi = d2xm * d2ym - dxmdym * dymdxm;
            float deltaX = ( dxmdym * dym - d2ym * dxm ) / denomi;
            float deltaY = ( dymdxm * dxm - d2xm * dym ) / denomi;
            return new float2( deltaX, deltaY );
        }

        /// <summary>
        /// Calculates the gradient energy of a vertex.
        /// </summary>
        /// <param name="m">The index of the vertex.</param>
        /// <returns>Calculates the gradient energy of the vertex <code>m</code>.</returns>
        private float CalculateEnergyGradient( int m )
        {
            float dxm = 0, dym = 0, dx, dy, d, common;
            //        {  1, if m < i
            // sign = { 
            //        { -1, if m > i
            for ( int i = 0; i < vertices.Length; i++ )
            {
                if ( i == m )
                    continue;

                //differences of the positions
                dx = ( positions[m].x - positions[i].x );
                dy = ( positions[m].y - positions[i].y );

                //distances of the two vertex (by positions)
                d = sqrt( dx * dx + dy * dy );

                common = springConstants[m, i] * ( 1 - edgeLengths[m, i] / d );
                dxm += common * dx;
                dym += common * dy;
            }
            // delta_m = sqrt((dE/dx)^2 + (dE/dy)^2)
            return sqrt( dxm * dxm + dym * dym );
        }
    }
}