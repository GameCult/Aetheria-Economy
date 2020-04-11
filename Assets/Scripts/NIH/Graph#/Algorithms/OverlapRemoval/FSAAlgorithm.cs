using System;
using System.Collections.Generic;
using System.Windows;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using Unity.Mathematics;
using Unity.Tiny;
using static Unity.Mathematics.math;

namespace GraphSharp.Algorithms.OverlapRemoval
{
    public class FSAAlgorithm<TObject> : FSAAlgorithm<TObject, IOverlapRemovalParameters>
        where TObject : class
    {
        public FSAAlgorithm( IDictionary<TObject, Rect> rectangles, IOverlapRemovalParameters parameters )
            : base( rectangles, parameters )
        {
        }
    }

    /// <summary>
    /// Tim Dwyer �ltal JAVA-ban implement�lt FSA algoritmus portol�sa .NET al�.
    /// 
    /// http://adaptagrams.svn.sourceforge.net/viewvc/adaptagrams/trunk/RectangleOverlapSolver/placement/FSA.java?view=markup
    /// </summary>
    public class FSAAlgorithm<TObject, TParam> : OverlapRemovalAlgorithmBase<TObject, TParam>
        where TObject : class
        where TParam : IOverlapRemovalParameters
    {
        public FSAAlgorithm( IDictionary<TObject, Rect> rectangles, TParam parameters )
            : base( rectangles, parameters )
        {
        }

        protected override void RemoveOverlap()
        {
            DateTime t0 = DateTime.Now;
            float cost = HorizontalImproved();
            DateTime t1 = DateTime.Now;

            Debug.WriteLine( "PFS horizontal: cost=" + cost + " time=" + ( t1 - t0 ) );

            t1 = DateTime.Now;
            cost = VerticalImproved();
            DateTime t2 = DateTime.Now;
            Debug.WriteLine( "PFS vertical: cost=" + cost + " time=" + ( t2 - t1 ) );
            Debug.WriteLine( "PFS total: time=" + ( t2 - t0 ) );
        }

        /// <summary>
        /// Megadja a k�t t�glalap k�t�tt fell�p� er�t.
        /// </summary>
        /// <param name="vi">Egyik t�glalap.</param>
        /// <param name="vj">M�sik t�glalap.</param>
        /// <returns></returns>
        protected float2 force( Rect vi, Rect vj )
        {
            var f = new float2( 0, 0 );
            float2 d = vj.GetCenter() - vi.GetCenter();
            float adx = Math.Abs( d.x );
            float ady = Math.Abs( d.y );
            float gij = d.y / d.x;
            float Gij = ( vi.y + vj.y ) / ( vi.x + vj.x );
            if ( Gij >= gij && gij > 0 || -Gij <= gij && gij < 0 || gij == 0 )
            {
                // vi and vj touch with y-direction boundaries
                f.x = d.x / adx * ( ( vi.x + vj.x ) / 2.0f - adx );
                f.y = f.x * gij;
            }
            if ( Gij < gij && gij > 0 || -Gij > gij && gij < 0 )
            {
                // vi and vj touch with x-direction boundaries
                f.y = d.y / ady * ( ( vi.y + vj.y ) / 2.0f - ady );
                f.x = f.y / gij;
            }
            return f;
        }

        protected float2 force2( Rect vi, Rect vj )
        {
            var f = new float2( 0, 0 );
            float2 d = vj.GetCenter() - vi.GetCenter();
            float gij = d.y / d.x;
            if ( vi.IntersectsWith( vj ) )
            {
                f.x = ( vi.x + vj.x ) / 2.0f - d.x;
                f.y = ( vi.y + vj.y ) / 2.0f - d.y;
                // in the x dimension
                if ( f.x > f.y && gij != 0 )
                {
                    f.x = f.y / gij;
                }
                f.x = Math.Max( f.x, 0 );
                f.y = Math.Max( f.y, 0 );
            }
            return f;
        }

        protected int XComparison( RectangleWrapper<TObject> r1, RectangleWrapper<TObject> r2 )
        {
            float r1CenterX = r1.CenterX;
            float r2CenterX = r2.CenterX;

            if ( r1CenterX < r2CenterX )
            {
                return -1;
            }
            if ( r1CenterX > r2CenterX )
            {
                return 1;
            }
            return 0;
        }

        protected void Horizontal()
        {
            wrappedRectangles.Sort( XComparison );
            int i = 0, n = wrappedRectangles.Count;
            while ( i < n )
            {
                // x_i = x_{i+1} = ... = x_k
                int k = i;
                RectangleWrapper<TObject> u = wrappedRectangles[i];
                //TODO plus 1 check
                for ( int j = i + 1; j < n; j++ )
                {
                    RectangleWrapper<TObject> v = wrappedRectangles[j];
                    if ( u.CenterX == v.CenterX )
                    {
                        u = v;
                        k = j;
                    }
                    else
                    {
                        break;
                    }
                }
                // delta = max(0, max{f.x(m,j)|i<=m<=k<j<n})
                float delta = 0;
                for ( int m = i; m <= k; m++ )
                {
                    for ( int j = k + 1; j < n; j++ )
                    {
                        float2 f = force( wrappedRectangles[m].Rectangle, wrappedRectangles[j].Rectangle );
                        if ( f.x > delta )
                        {
                            delta = f.x;
                        }
                    }
                }
                for ( int j = k + 1; j < n; j++ )
                {
                    RectangleWrapper<TObject> r = wrappedRectangles[j];
                    r.Rectangle.x += delta;
                }
                i = k + 1;
            }

        }

        protected float HorizontalImproved()
        {
            wrappedRectangles.Sort( XComparison );
            int i = 0, n = wrappedRectangles.Count;

            //bal szelso
            RectangleWrapper<TObject> lmin = wrappedRectangles[0];
            float sigma = 0, x0 = lmin.CenterX;
            var gamma = new float[wrappedRectangles.Count];
            var x = new float[wrappedRectangles.Count];
            while ( i < n )
            {
                RectangleWrapper<TObject> u = wrappedRectangles[i];

                //i-vel azonos k�z�pponttal rendelkez� t�glalapok meghat�roz�sa
                int k = i;
                for ( int j = i + 1; j < n; j++ )
                {
                    RectangleWrapper<TObject> v = wrappedRectangles[j];
                    if ( u.CenterX == v.CenterX )
                    {
                        u = v;
                        k = j;
                    }
                    else
                    {
                        break;
                    }
                }
                float g = 0;

                //i-k intervallumban l�v� t�glalapokra er�sz�m�t�s a t�l�k balra l�v�kkel
                if ( u.CenterX > x0 )
                {
                    for ( int m = i; m <= k; m++ )
                    {
                        float ggg = 0;
                        for ( int j = 0; j < i; j++ )
                        {
                            float2 f = force( wrappedRectangles[j].Rectangle, wrappedRectangles[m].Rectangle );
                            ggg = Math.Max( f.x + gamma[j], ggg );
                        }
                        RectangleWrapper<TObject> v = wrappedRectangles[m];
                        float gg =
                            v.Rectangle.x + ggg < lmin.Rectangle.x
                                ? sigma
                                : ggg;
                        g = Math.Max( g, gg );
                    }
                }
                //megjegyezz�k az elemek eltol�s�st x t�mbbe
                //bal sz�l� elemet �jra meghat�rozzuk
                for ( int m = i; m <= k; m++ )
                {
                    gamma[m] = g;
                    RectangleWrapper<TObject> r = wrappedRectangles[m];
                    x[m] = r.Rectangle.x + g;
                    if ( r.Rectangle.x < lmin.Rectangle.x )
                    {
                        lmin = r;
                    }
                }

                //az i-k intervallum n�gyzeteit�l jobbra l�v�kkel er�sz�m�t�s, legnagyobb er� t�rol�sa
                // delta = max(0, max{f.x(m,j)|i<=m<=k<j<n})
                float delta = 0;
                for ( int m = i; m <= k; m++ )
                {
                    for ( int j = k + 1; j < n; j++ )
                    {
                        float2 f = force( wrappedRectangles[m].Rectangle, wrappedRectangles[j].Rectangle );
                        if ( f.x > delta )
                        {
                            delta = f.x;
                        }
                    }
                }
                sigma += delta;
                i = k + 1;
            }
            float cost = 0;
            for ( i = 0; i < n; i++ )
            {
                RectangleWrapper<TObject> r = wrappedRectangles[i];
                float oldPos = r.Rectangle.x;
                float newPos = x[i];

                r.Rectangle.x = newPos;

                float diff = oldPos - newPos;
                cost += diff * diff;
            }
            return cost;
        }

        protected int YComparison( RectangleWrapper<TObject> r1, RectangleWrapper<TObject> r2 )
        {
            float r1CenterY = r1.CenterY;
            float r2CenterY = r2.CenterY;

            if ( r1CenterY < r2CenterY )
            {
                return -1;
            }
            if ( r1CenterY > r2CenterY )
            {
                return 1;
            }
            return 0;
        }

        protected void Vertical()
        {
            wrappedRectangles.Sort( YComparison );
            int i = 0, n = wrappedRectangles.Count;
            while ( i < n )
            {
                // y_i = y_{i+1} = ... = y_k
                int k = i;
                RectangleWrapper<TObject> u = wrappedRectangles[i];
                for ( int j = i; j < n; j++ )
                {
                    RectangleWrapper<TObject> v = wrappedRectangles[j];
                    if ( u.CenterY == v.CenterY )
                    {
                        u = v;
                        k = j;
                    }
                    else
                    {
                        break;
                    }
                }
                // delta = max(0, max{f.y(m,j)|i<=m<=k<j<n})
                float delta = 0;
                for ( int m = i; m <= k; m++ )
                {
                    for ( int j = k + 1; j < n; j++ )
                    {
                        float2 f = force2( wrappedRectangles[m].Rectangle, wrappedRectangles[j].Rectangle );
                        if ( f.y > delta )
                        {
                            delta = f.y;
                        }
                    }
                }
                for ( int j = k + 1; j < n; j++ )
                {
                    RectangleWrapper<TObject> r = wrappedRectangles[j];
                    r.Rectangle.y += delta;
                }
                i = k + 1;
            }

        }

        protected float VerticalImproved()
        {
            wrappedRectangles.Sort( YComparison );
            int i = 0, n = wrappedRectangles.Count;
            RectangleWrapper<TObject> lmin = wrappedRectangles[0];
            float sigma = 0, y0 = lmin.CenterY;
            var gamma = new float[wrappedRectangles.Count];
            var y = new float[wrappedRectangles.Count];
            while ( i < n )
            {
                RectangleWrapper<TObject> u = wrappedRectangles[i];
                int k = i;
                for ( int j = i + 1; j < n; j++ )
                {
                    RectangleWrapper<TObject> v = wrappedRectangles[j];
                    if ( u.CenterY == v.CenterY )
                    {
                        u = v;
                        k = j;
                    }
                    else
                    {
                        break;
                    }
                }
                float g = 0;
                if ( u.CenterY > y0 )
                {
                    for ( int m = i; m <= k; m++ )
                    {
                        float ggg = 0;
                        for ( int j = 0; j < i; j++ )
                        {
                            float2 f = force2( wrappedRectangles[j].Rectangle, wrappedRectangles[m].Rectangle );
                            ggg = Math.Max( f.y + gamma[j], ggg );
                        }
                        RectangleWrapper<TObject> v = wrappedRectangles[m];
                        float gg =
                            v.Rectangle.y + ggg < lmin.Rectangle.y
                                ? sigma
                                : ggg;
                        g = Math.Max( g, gg );
                    }
                }
                for ( int m = i; m <= k; m++ )
                {
                    gamma[m] = g;
                    RectangleWrapper<TObject> r = wrappedRectangles[m];
                    y[m] = r.Rectangle.y + g;
                    if ( r.Rectangle.y < lmin.Rectangle.y )
                    {
                        lmin = r;
                    }
                }
                // delta = max(0, max{f.x(m,j)|i<=m<=k<j<n})
                float delta = 0;
                for ( int m = i; m <= k; m++ )
                {
                    for ( int j = k + 1; j < n; j++ )
                    {
                        float2 f = force( wrappedRectangles[m].Rectangle, wrappedRectangles[j].Rectangle );
                        if ( f.y > delta )
                        {
                            delta = f.y;
                        }
                    }
                }
                sigma += delta;
                i = k + 1;
            }

            float cost = 0;
            for ( i = 0; i < n; i++ )
            {
                RectangleWrapper<TObject> r = wrappedRectangles[i];
                float oldPos = r.Rectangle.y;
                float newPos = y[i];

                r.Rectangle.y = newPos;

                float diff = oldPos - newPos;
                cost += diff * diff;
            }
            return cost;
        }
    }
}