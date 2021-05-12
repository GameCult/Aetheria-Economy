using System;
using System.Collections.Generic;
using NUnit.Framework;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;



class BresenhamTest
{
   

    
    [Test]
    public void ShapeTest()
    {
        Shape shape = new Shape(14, 6);
        Console.WriteLine("test");
        shape.SetLine(new float2(0.6f, 0.6f), new float2(10.6f, 4.6f));

        var coords = new[] { (1, 1), (2, 1), (3, 2), (4, 2), (5, 3), (6, 3), (7, 3), (8, 4), (9, 4), (10, 5), (11, 5)};
        foreach(var coord in coords)
        {
            Assert.True(shape.Cells[coord.Item1, coord.Item2], $"{coord.Item1}, {coord.Item2}");
        }
    }
}


