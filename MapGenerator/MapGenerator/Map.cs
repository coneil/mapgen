using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using coneil.Math.Voronoi;
using coneil.Math.Voronoi.Geometry;

namespace coneil.World.Map
{
    public class Map
    {
        // TODO CJO consider getting this and all other magic numbers out to a config file
        public const float LAKE_THRESHOLD = 0.3f; // % of neighboring points on Delaunay edges (that are water) needed

        public Rectangle Bounds { get; private set; }

        public Voronoi VoronoiGraph { get; private set; }

        Random _rnd;
        bool _needsMoreRandomness; // TODO CJO config
        int _numPoints;

        // Graph
        List<Point> _points;
        
        public Map(int numPoints, int numRelaxSteps, Rectangle plotBounds)
        {
            _rnd = new Random();
            Bounds = plotBounds;
            // Hey! When you go to test this, keep in mind that using Point instead of Vertex in the voronoi assembly
            // may have led to issues where it was assumed the vertex was being passed by reference.
            // Be prepared to change that!

            List<Point> points = GeneratePoints(numPoints, Bounds, numRelaxSteps);
            VoronoiGraph = new Voronoi(points, new List<uint>(), Bounds);
            
            // Generate random points
            // Relax those points
            // Generate voronoi
            // Translate diagram to custom graph
                // Use and record corners of voronoi polys
                // Use and record sites of all voronoi polys (centers)
                // Create edges using voronoi poly corners and ALSO all centers to their corners
                // Terminology:
                    // Center - voronoi site
                    // Border - side to a voronoi poly, a line segment
                    // Corner - where borders meet
                    // Spoke - line segment from center to corner
                    // Edge - border or spoke
                // All edges become the geometry for deformations
                // Biomes are ultimately assigned for each center using data about surrounding corners
        }

        List<Point> GeneratePoints(int numPoints, Rectangle bounds, int numRelaxSteps)
        {
            List<Point> points = new List<Point>();

            for(int i = 0; i < numPoints; i++)
            {
                Point p = new Point(_rnd.NextDouble() * bounds.Width, _rnd.NextDouble() * bounds.Height);
                points.Add(p);
            }

            var colors = new List<uint>();
            for(int i = 0; i < numRelaxSteps; i++)
            {
                var v = new Voronoi(points, colors, bounds);
                List<Point> relaxed = new List<Point>();
                foreach(var p in points)
                {
                    var region = v.GetRegion(p);
                    var n = new Point(0, 0);
                    foreach(var q in region)
                    {
                        n.X += q.X;
                        n.Y += q.Y;
                    }
                    if(region.Count > 0)
                    {
                        n.X /= region.Count;
                        n.Y /= region.Count;
                    }
                    relaxed.Add(n);
                }
                points = relaxed;
                v.Dispose();
                v = null;
            }

            return points;
        }
    }
}
