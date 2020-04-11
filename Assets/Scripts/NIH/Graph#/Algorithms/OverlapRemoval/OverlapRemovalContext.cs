using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Windows;
using Unity.Mathematics;
using Unity.Tiny;
using static Unity.Mathematics.math;

namespace GraphSharp.Algorithms.OverlapRemoval
{
    public class OverlapRemovalContext<TVertex> : IOverlapRemovalContext<TVertex>
    {
        public IDictionary<TVertex, Rect> Rectangles { get; private set; }

        public OverlapRemovalContext( IDictionary<TVertex, Rect> rectangles )
        {
            Rectangles = rectangles;
        }
    }
}