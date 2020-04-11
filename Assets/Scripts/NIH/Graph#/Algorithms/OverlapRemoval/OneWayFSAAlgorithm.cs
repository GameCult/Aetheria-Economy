using System;
using System.Collections.Generic;
using System.Windows;
using System.Diagnostics.Contracts;
using Unity.Mathematics;
using Unity.Tiny;
using static Unity.Mathematics.math;

namespace GraphSharp.Algorithms.OverlapRemoval
{
    public class OneWayFSAAlgorithm<TObject> : FSAAlgorithm<TObject, OneWayFSAParameters>
        where TObject : class
    {
        public OneWayFSAAlgorithm( IDictionary<TObject, Rect> rectangles, OneWayFSAParameters parameters )
            : base( rectangles, parameters )
        {
        }

        protected override void RemoveOverlap()
        {
            switch ( Parameters.Way )
            {
                case OneWayFSAWayEnum.Horizontal:
                    HorizontalImproved();
                    break;
                case OneWayFSAWayEnum.Vertical:
                    VerticalImproved();
                    break;
                default:
                    break;
            }
        }

        protected new float HorizontalImproved()
        {
            wrappedRectangles.Sort( XComparison );
            int i = 0, n = wrappedRectangles.Count;

            //bal szelso
            var lmin = wrappedRectangles[0];
            float sigma = 0, x0 = lmin.CenterX;
            var gamma = new float[wrappedRectangles.Count];
            var x = new float[wrappedRectangles.Count];
            while ( i < n )
            {
                var u = wrappedRectangles[i];

                //i-vel azonos középponttal rendelkező téglalapok meghatározása
                int k = i;
                for ( int j = i + 1; j < n; j++ )
                {
                    var v = wrappedRectangles[j];
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

                //ne legyenek ugyanabban a pontban
                for ( int z = i + 1; z <= k; z++ )
                {
                    var v = wrappedRectangles[z];
                    v.Rectangle.x += ( z - i ) * 0.0001f;
                }

                //i-k intervallumban lévő téglalapokra erőszámítás a tőlük balra lévőkkel
                if ( u.CenterX > x0 )
                {
                    for ( int m = i; m <= k; m++ )
                    {
                        float ggg = 0;
                        for ( int j = 0; j < i; j++ )
                        {
                            var f = force( wrappedRectangles[j].Rectangle, wrappedRectangles[m].Rectangle );
                            ggg = Math.Max( f.x + gamma[j], ggg );
                        }
                        var v = wrappedRectangles[m];
                        float gg = v.Rectangle.x + ggg < lmin.Rectangle.x ? sigma : ggg;
                        g = Math.Max( g, gg );
                    }
                }
                //megjegyezzük az elemek eltolásást x tömbbe
                //bal szélő elemet újra meghatározzuk
                for ( int m = i; m <= k; m++ )
                {
                    gamma[m] = g;
                    var r = wrappedRectangles[m];
                    x[m] = r.Rectangle.x + g;
                    if ( r.Rectangle.x < lmin.Rectangle.x )
                    {
                        lmin = r;
                    }
                }

                //az i-k intervallum négyzeteitől jobbra lévőkkel erőszámítás, legnagyobb erő tárolása
                // delta = max(0, max{f.x(m,j)|i<=m<=k<j<n})
                float delta = 0;
                for ( int m = i; m <= k; m++ )
                {
                    for ( int j = k + 1; j < n; j++ )
                    {
                        var f = force( wrappedRectangles[m].Rectangle, wrappedRectangles[j].Rectangle );
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
                var r = wrappedRectangles[i];
                float oldPos = r.Rectangle.x;
                float newPos = x[i];

                r.Rectangle.x = newPos;

                float diff = oldPos - newPos;
                cost += diff * diff;
            }
            return cost;
        }
    }
}