using System.Windows;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace GraphSharp.Algorithms.Layout.Compound
{
    public class TestingCompoundVertexInfo
    {
        public TestingCompoundVertexInfo(float2 springForce, float2 repulsionForce, float2 gravityForce, float2 applicationForce)
        {
            SpringForce = springForce;
            RepulsionForce = repulsionForce;
            GravityForce = gravityForce;
            ApplicationForce = applicationForce;
        }

        public float2 SpringForce { get; set; }
        public float2 RepulsionForce { get; set; }
        public float2 GravityForce { get; set; }
        public float2 ApplicationForce { get; set; }
    }
}
