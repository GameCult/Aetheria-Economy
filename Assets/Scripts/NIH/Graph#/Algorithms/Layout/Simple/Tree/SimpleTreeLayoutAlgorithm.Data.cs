using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuickGraph;

namespace GraphSharp.Algorithms.Layout.Simple.Tree
{
    public partial class SimpleTreeLayoutAlgorithm<TVertex, TEdge, TGraph> : DefaultParameterizedLayoutAlgorithmBase<TVertex, TEdge, TGraph, SimpleTreeLayoutParameters>
        where TVertex : class
        where TEdge : IEdge<TVertex>
        where TGraph : IBidirectionalGraph<TVertex, TEdge>
    {
        class Layer
        {
            public float Size;
            public float NextPosition;
            public readonly IList<TVertex> Vertices = new List<TVertex>();
            public float LastTranslate;

            public Layer()
            {
                LastTranslate = 0;
            }

            /* Width and Height Optimization */

        }

        class VertexData
        {
            public TVertex parent;
            public float translate;
            public float position;

            /* Width and Height Optimization */

        }
    }
}
