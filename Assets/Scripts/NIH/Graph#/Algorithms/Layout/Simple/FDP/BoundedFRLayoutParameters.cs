using System;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace GraphSharp.Algorithms.Layout.Simple.FDP
{
    /// <summary>
    /// Parameters of the Fruchterman-Reingold Algorithm (FDP), bounded version.
    /// </summary>
    public class BoundedFRLayoutParameters : FRLayoutParametersBase
    {
        #region Properties, Parameters
        //some of the parameters declared with 'internal' modifier to 'speed up'

        private float _width = 100;
        private float _height = 100;
        private float _k;

        /// <summary>
        /// Width of the bounding box.
        /// </summary>
        public float Width
        {
            get { return _width; }
            set
            {
                _width = value;
                UpdateParameters();
                NotifyPropertyChanged("Width");
            }
        }

        /// <summary>
        /// Height of the bounding box.
        /// </summary>
        public float Height
        {
            get { return _height; }
            set
            {
                _height = value;
                UpdateParameters();
                NotifyPropertyChanged("Height");
            }
        }

        /// <summary>
        /// Constant. <code>IdealEdgeLength = sqrt(height * width / vertexCount)</code>
        /// </summary>
        public override float K
        {
            get { return _k; }
        }

        /// <summary>
        /// Gets the initial temperature of the mass.
        /// </summary>
        public override float InitialTemperature
        {
            get { return min(Width, Height) / 10; }
        }

        protected override void UpdateParameters()
        {
            _k = sqrt(_width * Height / VertexCount);
            NotifyPropertyChanged("K");
            base.UpdateParameters();
        }

        #endregion
    }
}