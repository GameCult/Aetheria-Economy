using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace GraphSharp.Algorithms.Layout.Simple.FDP
{
    public class FreeFRLayoutParameters : FRLayoutParametersBase
    {
        private float _idealEdgeLength = 10;

        public override float K
        {
            get { return _idealEdgeLength; }
        }

        public override float InitialTemperature
        {
            get { return sqrt(pow(_idealEdgeLength, 2) * VertexCount); }
        }

        /// <summary>
        /// Constant. Represents the ideal length of the edges.
        /// </summary>
        public float IdealEdgeLength
        {
            get { return _idealEdgeLength; }
            set
            {
                _idealEdgeLength = value;
                UpdateParameters();
            }
        }
    }
}
