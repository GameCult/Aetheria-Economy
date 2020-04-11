
using Unity.Mathematics;
using Unity.Tiny;
using static Unity.Mathematics.math;

namespace GraphSharp.Algorithms.OverlapRemoval
{
	/// <summary>
	/// A System.Windows.Rect egy strukt�ra, ez�rt a heap-en t�rol�dik. Bizonyos esetekben ez nem
	/// szerencs�s, �gy sz�ks�g van erre a wrapper oszt�lyra. Mivel ez class, ez�rt nem
	/// �rt�k szerinti �tad�s van.
	/// </summary>
	public class RectangleWrapper<TObject>
		where TObject : class
	{
		private readonly TObject id;
		public TObject Id
		{
			get { return id; }
		}

		public Rect Rectangle;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="rectangle"></param>
		/// <param name="id">Az adott t�glalap azonos�t�ja (az overlap-removal v�g�n tudnunk kell, hogy 
		/// melyik t�glalap melyik objektumhoz tartozik. Az azonos�t�s megoldhat� lesz id alapj�n.</param>
		public RectangleWrapper( Rect rectangle, TObject id )
		{
			Rectangle = rectangle;
			this.id = id;
		}

		public float CenterX
		{
			get { return Rectangle.x + Rectangle.width / 2; }
		}

		public float CenterY
		{
			get { return Rectangle.y + Rectangle.height / 2; }
		}

		public float2 Center
		{
			get { return new float2( CenterX, CenterY ); }
		}
	}
}