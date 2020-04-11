using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using QuickGraph;
using System.Diagnostics.Contracts;
using System.Diagnostics;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Random = System.Random;

namespace GraphSharp.Algorithms.Layout.Compound.FDP
{
    /// <typeparam name="TVertex">The type of the vertices.</typeparam>
    /// <typeparam name="TEdge">The type of the edges.</typeparam>
    /// <typeparam name="TGraph">The type of the graph.</typeparam>
    public partial class CompoundFDPLayoutAlgorithm<TVertex, TEdge, TGraph> :
        DefaultParameterizedLayoutAlgorithmBase<TVertex, TEdge, TGraph, CompoundFDPLayoutParameters>,
        ICompoundLayoutAlgorithm<TVertex, TEdge, TGraph>
        where TVertex : class
        where TEdge : IEdge<TVertex>
        where TGraph : IBidirectionalGraph<TVertex, TEdge>
    {
        /*[ContractInvariantMethod]
        private void InvariantContracts()
        {
            Contract.Invariant(1 <= _phase && _phase <= 3);
            Contract.Invariant(_treeGrowingStep > 0);
        }*/

        private float _temperature = 0;
        private float _temperatureDelta; //need to be initialized
        private readonly float _temperatureLambda = 0.99f;
        private readonly Random rnd = new Random(DateTime.Now.Millisecond);

        /// <summary>
        /// <para>Phase of the layout process.</para>
        /// <para>Values: 1,2,3</para>
        /// </summary>
        private int _phase = 1;

        /// <summary>
        /// The steps in the actual phase.
        /// </summary>
        private int _step;

        /// <summary>
        /// The maximal iteration count in the phases.
        /// </summary>
        private /*readonly*/ int[] _maxIterationCounts = new int[3] { 30, 70, 50 };

        /// <summary>
        /// The error thresholds for the phases (calculated inside the Init method).
        /// </summary>
        private readonly float[] _errorThresholds = new float[3];

        /// <summary>
        /// Indicates whether the removed tree-node 
        /// has been grown back or not.
        /// </summary>
        private bool _allTreesGrown
        {
            get { return _removedRootTreeNodeLevels.Count == 0; }
        }

        /// <summary>
        /// Grows back a tree-node level in every 'treeGrowingStep'th step.
        /// </summary>
        private int _treeGrowingStep = 5;

        /// <summary>
        /// The magnitude of the gravity force calculated in the init phased.
        /// </summary>
        private float _gravityForceMagnitude;

        /// <summary>
        /// Has been the gravity center initiated or not.
        /// </summary>
        private bool _gravityCenterCalculated;

        private float _phaseDependentRepulsionMultiplier = 1.0f;

        /// <summary>
        /// This method is the skeleton of the layout algorithm.
        /// </summary>
        protected override void InternalCompute()
        {
            //call initialize
            Init(_vertexSizes, /*_vertexBorders,*/ _layoutTypes);

            //Phases:
            //1: layout the skeleton graph without app. specific and gravitation forces.
            //2: add the removed tree nodes and apply app. specific and gravitation forces.
            //3: stabilization

            /* ********* FOR OPTIMIZATION PURPOSES ********** */
            _maxIterationCounts = new int[3];
            _maxIterationCounts[0] = Parameters.Phase1Iterations;
            _maxIterationCounts[1] = Parameters.Phase2Iterations;
            _maxIterationCounts[2] = Parameters.Phase3Iterations;

            var _temperatureMultipliers = new float[3]
                                              {
                                                  1.0f, 
                                                  Parameters.Phase2TemperatureInitialMultiplier,
                                                  Parameters.Phase3TemperatureInitialMultiplier
                                              };

            float initialTemperature = sqrt(_compoundGraph.VertexCount) * Parameters.IdealEdgeLength;
            float minimalTemperature = initialTemperature * 0.1f;
            _temperature = initialTemperature;

            _gravityCenterCalculated = false;

            for (_phase = 1; _phase <= 3; _phase++)
            {
                _temperature = initialTemperature * _temperatureMultipliers[_phase - 1];
                _phaseDependentRepulsionMultiplier = _phase < 2 ? 0.5f : 1.0f;
                //TODO put back the error and its threshold
                /*float error = _errorThresholds[_phase] + 1;*/
                for (_step = _maxIterationCounts[_phase - 1];
                     (_step > 0 && true/*error > _errorThresholds[_phase - 1] */) || (_phase == 2 && !_allTreesGrown);
                     _step--)
                {
                    /*error = 0;*/

                    ApplySpringForces();
                    ApplyRepulsionForces();

                    if (_phase > 1)
                    {
                        ApplyGravitationForces();
                        ApplyApplicationSpecificForces();
                    }

                    if (ReportOnIterationEndNeeded)
                        SavePositions();

                    CalcNodePositionsAndSizes();

                    if (_phase == 2 && !_allTreesGrown && _step % _treeGrowingStep == 0)
                        GrowTreesOneLevel();

                    _temperature *= _temperatureLambda;
                    _temperature = Math.Max(_temperature, minimalTemperature);
                }
                if (!_gravityCenterCalculated)
                {
                    _rootCompoundVertex.RecalculateBounds();
                    _gravityCenterCalculated = true;
                }
                //if (_phase == 1)
                _temperature *= Parameters.TemperatureDecreasing;
            }
            SavePositions();
        }

        private void SavePositions()
        {
            foreach (var vertex in _vertexDatas.Keys)
            {
                var v = _vertexDatas[vertex];
                VertexPositions[vertex] = v.Position;
            }

            /*var iterationEndedArgs = 
                new CompoundLayoutIterationEventArgs<TVertex, TEdge>(
                    0, 0, string.Empty, 
                    VertexPositions,
                    InnerCanvasSizes);*/

            //build the test vertex infos
            var vertexInfos = _vertexDatas.ToDictionary(
                kvp => kvp.Key,
                kvp => new TestingCompoundVertexInfo(
                    kvp.Value.SpringForce,
                    kvp.Value.RepulsionForce,
                    kvp.Value.GravitationForce,
                    kvp.Value.ApplicationForce));

            var iterationEndedArgs =
                new TestingCompoundLayoutIterationEventArgs<TVertex, TEdge, TestingCompoundVertexInfo, object>(
                    0, 0, string.Format("Phase: {0}, Steps: {1}", _phase, _step),
                    VertexPositions,
                    InnerCanvasSizes,
                    vertexInfos,
                    _rootCompoundVertex.InnerCanvasCenter);

            //raise the event
            OnIterationEnded(iterationEndedArgs);
        }

        private void GrowTreesOneLevel()
        {
            if (_removedRootTreeNodeLevels.Count <= 0)
                return;

            var treeNodeDatas = _removedRootTreeNodeLevels.Pop();
            foreach (var tnd in treeNodeDatas)
            {
                _removedRootTreeNodes.Remove(tnd.Vertex);
                _removedRootTreeEdges.Remove(tnd.Edge);
                _levels[0].Add(tnd.Vertex);
                _compoundGraph.AddVertex(tnd.Vertex);
                _compoundGraph.AddEdge(tnd.Edge);

                var otherVertex = tnd.Edge.GetOtherVertex(tnd.Vertex);
                _vertexDatas[tnd.Vertex].Position = _vertexDatas[otherVertex].Position;
            }
        }

        private float2 GetSpringForce(float idealLength, float2 uPos, float2 vPos, float2 uSize, float2 vSize)
        {
            var positionVector = (uPos - vPos);
            if (abs(math.length(positionVector)) < .001f)
            {
                var compensationVector = new float2((float) rnd.NextDouble(), (float) rnd.NextDouble());
                positionVector = compensationVector * 2;
                uPos += compensationVector;
                vPos -= compensationVector;
            }
            positionVector = normalize(positionVector);

            //get the clipping points
            var c_u = LayoutUtil.GetClippingPoint(uSize, uPos, vPos);
            var c_v = LayoutUtil.GetClippingPoint(vSize, vPos, uPos);

            float2 F = (c_u - c_v);
            bool isSameDirection = LayoutUtil.IsSameDirection(positionVector, F);
            float length = 0;
            if (isSameDirection)
                length = math.length(F) - idealLength;
            else
                length = math.length(F) + idealLength;

            if (abs(math.length(F)) < .001f)
                F = -positionVector;
            F = normalize(F);
            if (length > 0)
                F *= -1;

            var Fs = pow(length / (idealLength), 2) / Parameters.ElasticConstant * F;
            return Fs;
        }

        private float2 GetRepulsionForce(float2 uPos, float2 vPos, float2 uSize, float2 vSize, float repulsionRange)
        {
            var positionVector = (uPos - vPos);
            if (math.length(positionVector) == 0)
            {
                var compensationVector = new float2((float) rnd.NextDouble(), (float) rnd.NextDouble());
                positionVector = compensationVector * 2;
                uPos += compensationVector;
                vPos -= compensationVector;
            }
            positionVector = normalize(positionVector);

            var c_u = LayoutUtil.GetClippingPoint(uSize, uPos, vPos);
            var c_v = LayoutUtil.GetClippingPoint(vSize, vPos, uPos);

            var F = c_u - c_v;
            var isSameDirection = LayoutUtil.IsSameDirection(positionVector, F);
            var Fr = new float2();

            if (isSameDirection && math.length(F) > repulsionRange)
                return new float2();
            float length = Math.Max(1, math.length(F));
            //float length = F.LengthSquared;
            length = pow(isSameDirection ? length / (Parameters.IdealEdgeLength * 2.0f) : 1 / length, 2);
            Fr = Parameters.RepulsionConstant / length * positionVector * _phaseDependentRepulsionMultiplier;
            return Fr;
        }

        /// <summary>
        /// Applies the attraction forces (between the end nodes
        /// of the edges).
        /// </summary>
        private void ApplySpringForces()
        {
            foreach (var edge in VisitedGraph.Edges)
            {
                if (!_allTreesGrown && (_removedRootTreeNodes.Contains(edge.Source) || _removedRootTreeNodes.Contains(edge.Target)))
                    continue;
                //get the ideal edge length
                float idealLength = Parameters.IdealEdgeLength;
                var u = _vertexDatas[edge.Source];
                var v = _vertexDatas[edge.Target];
                var multiplier = (u.Level + v.Level) / 2.0f + 1;
                if (IsInterGraphEdge(edge))
                {
                    //idealLength *= (u.Level + v.Level + 1) * Parameters.NestingFactor;
                    idealLength *= 1 + (u.Level + v.Level + 1) * Parameters.NestingFactor;
                    //multiplier = 1;
                }

                var Fs = GetSpringForce(idealLength, u.Position, v.Position, u.Size, v.Size) * multiplier;

                //aggregate the forces
                if ((u.IsFixedToParent && u.MovableParent == null) ^ (v.IsFixedToParent && v.MovableParent == null))
                    Fs *= 2;
                if (!u.IsFixedToParent)
                    u.SpringForce += Fs /* * u.Mass / (u.Mass + v.Mass)*/;
                else if (u.MovableParent != null)
                {
                    u.MovableParent.SpringForce += Fs;
                }
                if (!v.IsFixedToParent)
                    v.SpringForce -= Fs /* * v.Mass / (u.Mass + v.Mass)*/;
                else if (v.MovableParent != null)
                {
                    v.MovableParent.SpringForce -= Fs;
                }
            }
        }

        /// <summary>
        /// Applies the repulsion forces between every node-pair.
        /// </summary>
        private void ApplyRepulsionForces()
        {
            var repulsionRange = Parameters.IdealEdgeLength * Parameters.SeparationMultiplier;
            for (int i = _levels.Count - 1; i >= 0; i--)
            {
                var checkedVertices = new HashSet<TVertex>();
                foreach (var uVertex in _levels[i])
                {
                    checkedVertices.Add(uVertex);
                    var u = _vertexDatas[uVertex];
                    foreach (var vVertex in _levels[i])
                    {
                        if (checkedVertices.Contains(vVertex))
                            continue;
                        var v = _vertexDatas[vVertex];

                        if (u.Parent != v.Parent)
                            continue; //the two vertex not in the same graph

                        var Fr = GetRepulsionForce(u.Position, v.Position, u.Size, v.Size, repulsionRange) * pow(u.Level + 1,2);

                        if (u.IsFixedToParent ^ v.IsFixedToParent)
                            Fr *= 2;
                        if (!u.IsFixedToParent)
                            u.RepulsionForce += Fr /** u.Mass / (u.Mass + v.Mass)*/;
                        if (!v.IsFixedToParent)
                            v.RepulsionForce -= Fr /** v.Mass / (u.Mass + v.Mass)*/;
                    }
                }
            }
        }

        /// <summary>
        /// Applies the gravitation forces.
        /// </summary>
        private void ApplyGravitationForces()
        {
            for (int i = _levels.Count - 1; i >= 0; i--)
            {
                foreach (var uVertex in _levels[i])
                {
                    var u = _vertexDatas[uVertex];
                    var center = u.Parent.InnerCanvasCenter;

                    var Fg = center - u.Position;
                    if (math.length(Fg) < .001f)
                        continue;

                    float length = Math.Max(1, math.length(Fg) / (Parameters.IdealEdgeLength * 2.0f));
                    Fg = normalize(Fg);
                    Fg *= Parameters.GravitationFactor * _gravityForceMagnitude * pow(u.Level + 1,2) / pow(length, 0.25f);
                    u.GravitationForce += Fg;
                }
            }
        }

        /// <summary>
        /// Applies the application specific forces to the vertices.
        /// </summary>
        protected virtual void ApplyApplicationSpecificForces()
        {
        }

        private void CalcNodePositionsAndSizes()
        {
            for (int i = _levels.Count - 1; i >= 0; i--)
            {
                foreach (var uVertex in _levels[i])
                {
                    var u = _vertexDatas[uVertex];
                    var force = u.ApplyForce(_temperature * max(1, _step) / 100.0f * Parameters.DisplacementLimitMultiplier);
                }
            }
        }

        private bool IsInterGraphEdge(TEdge e)
        {
            return _vertexDatas[e.Source].Parent != _vertexDatas[e.Target].Parent;
        }
    }
}