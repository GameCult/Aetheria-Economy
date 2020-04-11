namespace GraphSharp.Algorithms.Layout.Simple.FDP
{
	public class ISOMLayoutParameters : LayoutParametersBase
	{
		private float _width = 300;
		/// <summary>
		/// Width of the bounding box. Default value is 300.
		/// </summary>
		public float Width
		{
			get { return _width; }
			set
			{
				_width = value;
				NotifyPropertyChanged("Width");
			}
		}

		private float _height = 300;
		/// <summary>
		/// Height of the bounding box. Default value is 300.
		/// </summary>
		public float Height
		{
			get { return _height; }
			set
			{
				_height = value;
				NotifyPropertyChanged("Height");
			}
		}

		private int maxEpoch = 2000;
		/// <summary>
		/// Maximum iteration number. Default value is 2000.
		/// </summary>
		public int MaxEpoch
		{
			get { return maxEpoch; }
			set
			{
				maxEpoch = value;
				NotifyPropertyChanged("MaxEpoch");
			}
		}

		private int _radiusConstantTime = 100;
		/// <summary>
		/// Radius constant time. Default value is 100.
		/// </summary>
		public int RadiusConstantTime
		{
			get { return _radiusConstantTime; }
			set
			{
				_radiusConstantTime = value;
				NotifyPropertyChanged("RadiusConstantTime");
			}
		}

		private int _initialRadius = 5;
		/// <summary>
		/// Default value is 5.
		/// </summary>
		public int InitialRadius
		{
			get { return _initialRadius; }
			set
			{
				_initialRadius = value;
				NotifyPropertyChanged("InitialRadius");
			}
		}

		private int _minRadius = 1;
		/// <summary>
		/// Minimal radius. Default value is 1.
		/// </summary>
		public int MinRadius
		{
			get { return _minRadius; }
			set
			{
				_minRadius = value;
				NotifyPropertyChanged("MinRadius");
			}
		}

		private float _initialAdaption = 0.9f;
		/// <summary>
		/// Default value is 0.9.
		/// </summary>
		public float InitialAdaption
		{
			get { return _initialAdaption; }
			set
			{
				_initialAdaption = value;
				NotifyPropertyChanged("InitialAdaption");
			}
		}

		private float _minAdaption;
		/// <summary>
		/// Default value is 0.
		/// </summary>
		public float MinAdaption
		{
			get { return _minAdaption; }
			set
			{
				_minAdaption = value;
				NotifyPropertyChanged("MinAdaption");
			}
		}

		private float _coolingFactor = 2;
		/// <summary>
		/// Default value is 2.
		/// </summary>
		public float CoolingFactor
		{
			get { return _coolingFactor; }
			set
			{
				_coolingFactor = value;
				NotifyPropertyChanged("CoolingFactor");
			}
		}
	}
}