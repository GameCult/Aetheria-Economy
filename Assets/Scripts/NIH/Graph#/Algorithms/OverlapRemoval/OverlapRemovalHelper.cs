using Unity.Mathematics;
using Unity.Tiny;
using static Unity.Mathematics.math;

namespace GraphSharp.Algorithms.OverlapRemoval
{
	public static class OverlapRemovalHelper
	{
		public static float2 GetCenter( this Rect r )
		{
			return new float2( r.x + r.width / 2, r.y + r.height / 2 );
		}
	}
}