namespace GraphSharp.Algorithms.Layout.Compound.FDP
{
    public class CompoundFDPLayoutParameters : LayoutParametersBase
    {
        private float _idealEdgeLength = 25;
        private float _elasticConstant = 0.005f;
        private float _repulsionConstant = 150;
        private float _nestingFactor = 0.2f;
        private float _gravitationFactor = 8;

        private int _phase1Iterations = 50;
        private int _phase2Iterations = 70;
        private int _phase3Iterations = 30;

        private float _phase2TemperatureInitialMultiplier = 0.5f;
        private float _phase3TemperatureInitialMultiplier = 0.2f;

        private float _temperatureDecreasing = 0.5f;
        private float _temperatureFactor = 0.95f;
        private float _displacementLimitMultiplier = 0.5f;
        private float _separationMultiplier = 15;

        /// <summary>
        /// Gets or sets the ideal edge length.
        /// </summary>
        public float IdealEdgeLength
        {
            get { return _idealEdgeLength; }
            set
            {
                if (value == _idealEdgeLength)
                    return;

                _idealEdgeLength = value;
                NotifyPropertyChanged("IdealEdgeLength");
            }
        }

        /// <summary>
        /// Gets or sets the elastic constant for the edges.
        /// </summary>
        public float ElasticConstant
        {
            get { return _elasticConstant; }
            set
            {
                if (value == _elasticConstant)
                    return;

                _elasticConstant = value;
                NotifyPropertyChanged("ElasticConstant");
            }
        }

        /// <summary>
        /// Gets or sets the repulsion constant for the node-node 
        /// repulsion.
        /// </summary>
        public float RepulsionConstant
        {
            get { return _repulsionConstant; }
            set
            {
                if (value == _repulsionConstant)
                    return;

                _repulsionConstant = value;
                NotifyPropertyChanged("RepulsionConstant");
            }
        }

        /// <summary>
        /// Gets or sets the factor of the ideal edge length for the 
        /// inter-graph edges.
        /// </summary>
        public float NestingFactor
        {
            get { return _nestingFactor; }
            set
            {
                if (value == _nestingFactor)
                    return;

                _nestingFactor = value;
                NotifyPropertyChanged("NestingFactor");
            }
        }

        /// <summary>
        /// Gets or sets the factor of the gravitation.
        /// </summary>
        public float GravitationFactor
        {
            get { return _gravitationFactor; }
            set
            {
                if (value == _gravitationFactor)
                    return;

                _gravitationFactor = value;
                NotifyPropertyChanged("GravitationFactor");
            }
        }

        public int Phase1Iterations
        {
            get { return _phase1Iterations; }
            set
            {
                if (value == _phase1Iterations)
                    return;

                _phase1Iterations = value;
                NotifyPropertyChanged("Phase1Iterations");
            }
        }

        public int Phase2Iterations
        {
            get { return _phase2Iterations; }
            set
            {
                if (value == _phase2Iterations)
                    return;

                _phase2Iterations = value;
                NotifyPropertyChanged("Phase2Iterations");
            }
        }

        public int Phase3Iterations
        {
            get { return _phase3Iterations; }
            set
            {
                if (value == _phase3Iterations)
                    return;

                _phase3Iterations = value;
                NotifyPropertyChanged("Phase3Iterations");
            }
        }

        public float Phase2TemperatureInitialMultiplier
        {
            get { return _phase2TemperatureInitialMultiplier; }
            set
            {
                if (value == _phase2TemperatureInitialMultiplier)
                    return;

                _phase2TemperatureInitialMultiplier = value;
                NotifyPropertyChanged("Phase2TemperatureInitialMultiplier");
            }
        }

        public float Phase3TemperatureInitialMultiplier
        {
            get { return _phase3TemperatureInitialMultiplier; }
            set
            {
                if (value == _phase3TemperatureInitialMultiplier)
                    return;

                _phase3TemperatureInitialMultiplier = value;
                NotifyPropertyChanged("Phase3TemperatureInitialMultiplier");
            }
        }

        public float TemperatureDecreasing
        {
            get { return _temperatureDecreasing; }
            set
            {
                if (value == _temperatureDecreasing)
                    return;

                _temperatureDecreasing = value;
                NotifyPropertyChanged("TemperatureDecreasing");
            }
        }

        public float TemperatureFactor
        {
            get { return _temperatureFactor; }
            set
            {
                if (value == _temperatureFactor)
                    return;

                _temperatureFactor = value;
                NotifyPropertyChanged("TemperatureFactor");
            }
        }

        public float DisplacementLimitMultiplier
        {
            get { return _displacementLimitMultiplier; }
            set
            {
                if (value == _displacementLimitMultiplier)
                    return;

                _displacementLimitMultiplier = value;
                NotifyPropertyChanged("DisplacementLimitMultiplier");
            }
        }

        public float SeparationMultiplier
        {
            get { return _separationMultiplier; }
            set
            {
                if (value == _separationMultiplier)
                    return;

                _separationMultiplier = value;
                NotifyPropertyChanged("SeparationMultiplier");
            }
        }
    }
}
