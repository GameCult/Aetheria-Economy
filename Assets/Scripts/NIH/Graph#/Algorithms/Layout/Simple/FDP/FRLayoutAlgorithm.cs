using System;
using System.Collections.Generic;
using QuickGraph;
using QuickGraph.Algorithms;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace GraphSharp.Algorithms.Layout.Simple.FDP
{
    public class FRLayoutAlgorithm<Vertex, Edge, Graph> : ParameterizedLayoutAlgorithmBase<Vertex, Edge, Graph, FRLayoutParametersBase>
        where Vertex : class
        where Edge : IEdge<Vertex>
        where Graph : IVertexAndEdgeListGraph<Vertex, Edge>
    {
        /// <summary>
        /// Actual temperature of the 'mass'.
        /// </summary>
        private float _temperature;

        private float _maxWidth = float.PositiveInfinity;
        private float _maxHeight = float.PositiveInfinity;

        protected override FRLayoutParametersBase DefaultParameters
        {
            get { return new FreeFRLayoutParameters(); }
        }

        #region Constructors
        public FRLayoutAlgorithm(Graph visitedGraph)
            : base(visitedGraph) { }

        public FRLayoutAlgorithm(Graph visitedGraph, IDictionary<Vertex, float2> vertexPositions, FRLayoutParametersBase parameters)
            : base(visitedGraph, vertexPositions, parameters) { }
        #endregion

        /// <summary>
        /// It computes the layout of the vertices.
        /// </summary>
        protected override void InternalCompute()
        {
            //initializing the positions
            if (Parameters is BoundedFRLayoutParameters)
            {
                var param = Parameters as BoundedFRLayoutParameters;
                InitializeWithRandomPositions(param.Width, param.Height);
                _maxWidth = param.Width;
                _maxHeight = param.Height;
            }
            else
            {
                InitializeWithRandomPositions(10.0f, 10.0f);
            }
            Parameters.VertexCount = VisitedGraph.VertexCount;

            // Actual temperature of the 'mass'. Used for cooling.
            var minimalTemperature = Parameters.InitialTemperature*0.01;
            _temperature = Parameters.InitialTemperature;
            for (int i = 0;
                  i < Parameters._iterationLimit
                  && _temperature > minimalTemperature
                  && State != ComputationState.PendingAbortion;
                  i++)
            {
                IterateOne();

                //make some cooling
                switch (Parameters._coolingFunction)
                {
                    case FRCoolingFunction.Linear:
                        _temperature *= (1.0f - i / (float)Parameters._iterationLimit);
                        break;
                    case FRCoolingFunction.Exponential:
                        _temperature *= Parameters._lambda;
                        break;
                }

                //iteration ended, do some report
                if (ReportOnIterationEndNeeded)
                {
                    float statusInPercent = i / (float)Parameters._iterationLimit;
                    OnIterationEnded(i, statusInPercent, string.Empty, true);
                }
            }
        }


        protected void IterateOne()
        {
            //create the forces (zero forces)
            var forces = new Dictionary<Vertex, float2>();

            #region Repulsive forces
            var force = new float2(0, 0);
            foreach (Vertex v in VisitedGraph.Vertices)
            {
                force.x = 0; force.y = 0;
                float2 posV = VertexPositions[v];
                foreach (Vertex u in VisitedGraph.Vertices)
                {
                    //doesn't repulse itself
                    if (u.Equals(v))
                        continue;

                    //calculating repulsive force
                    float2 delta = posV - VertexPositions[u];
                    float length = max(math.length(delta), float.Epsilon);
                    delta = delta / length * Parameters.ConstantOfRepulsion / length;

                    force += delta;
                }
                forces[v] = force;
            }
            #endregion

            #region Attractive forces
            foreach (Edge e in VisitedGraph.Edges)
            {
                Vertex source = e.Source;
                Vertex target = e.Target;

                //vonzóerõ számítása a két pont közt
                float2 delta = VertexPositions[source] - VertexPositions[target];
                float length = Math.Max(math.length(delta), float.Epsilon);
                delta = delta / length * pow(length, 2) / Parameters.ConstantOfAttraction;

                forces[source] -= delta;
                forces[target] += delta;
            }
            #endregion

            #region Limit displacement
            foreach (Vertex v in VisitedGraph.Vertices)
            {
                float2 pos = VertexPositions[v];

                //erõ limitálása a temperature-el
                float2 delta = forces[v];
                float length = Math.Max(math.length(delta), float.Epsilon);
                delta = delta / length * Math.Min(math.length(delta), _temperature);

                //erõhatás a pontra
                pos += delta;

                //falon ne menjünk ki
                pos.x = Math.Min(_maxWidth, Math.Max(0, pos.x));
                pos.y = Math.Min(_maxHeight, Math.Max(0, pos.y));
                VertexPositions[v] = pos;
            }
            #endregion
        }
    }
}