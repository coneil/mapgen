using System;
using System.Collections.Generic;
using System.Linq;
using coneil.Math.Voronoi.Geometry;

namespace coneil.World.Map
{
    class TestGenerator
    {
        static void Main(string[] args)
        {
            const int NUM_POINTS = 1 << 14;
            const int SIZE = 1000;

            Rectangle bounds = new Rectangle(0, 0, SIZE, SIZE);

            Map m = new Map(NUM_POINTS, 2, bounds);
            System.Diagnostics.Debug.WriteLine(m.VoronoiGraph.Edges.Count);
        }
    }
}
