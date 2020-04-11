using QuickGraph;

namespace GraphSharp
{
	public class WeightedEdge<Vertex> : Edge<Vertex>
	{
		public float Weight { get; private set; }

		public WeightedEdge(Vertex source, Vertex target)
			: this(source, target, 1) {}

		public WeightedEdge(Vertex source, Vertex target, float weight)
			: base(source, target)
		{
			this.Weight = weight;
		}
	}
}