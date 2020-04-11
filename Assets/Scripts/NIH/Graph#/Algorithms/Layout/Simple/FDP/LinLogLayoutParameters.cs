namespace GraphSharp.Algorithms.Layout.Simple.FDP
{
	public class LinLogLayoutParameters : LayoutParametersBase
	{
		internal float attractionExponent = 1.0f;

		public float AttractionExponent
		{
			get { return attractionExponent; }
			set
			{
				attractionExponent = value;
				NotifyPropertyChanged("AttractionExponent");
			}
		}

		internal float repulsiveExponent;

		public float RepulsiveExponent
		{
			get { return repulsiveExponent; }
			set
			{
				repulsiveExponent = value;
				NotifyPropertyChanged("RepulsiveExponent");
			}
		}

		internal float gravitationMultiplier = 0.1f;

		public float GravitationMultiplier
		{
			get { return gravitationMultiplier; }
			set
			{
				gravitationMultiplier = value;
				NotifyPropertyChanged("GravitationMultiplier");
			}
		}

		internal int iterationCount = 100;

		public int IterationCount
		{
			get { return iterationCount; }
			set
			{
				iterationCount = value;
				NotifyPropertyChanged("IterationCount");
			}
		}
	}
}