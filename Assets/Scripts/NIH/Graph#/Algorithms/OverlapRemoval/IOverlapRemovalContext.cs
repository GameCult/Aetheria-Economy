using System.Collections.Generic;
using System.Windows;
using Unity.Mathematics;
using Unity.Tiny;
using static Unity.Mathematics.math;

namespace GraphSharp.Algorithms.OverlapRemoval
{
	public interface IOverlapRemovalContext<TVertex>
	{
		IDictionary<TVertex, Rect> Rectangles { get; }
	}
}