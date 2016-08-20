using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using coneil.Math.Voronoi;
using coneil.Math.Voronoi.Geometry;
using coneil.World.Map.Graph;

namespace coneil.World.Map
{
    public class Map
    {
        // TODO CJO consider getting this and all other magic numbers out to a config file

        public Rectangle Bounds { get; private set; }
        public MapConfig Config { get; private set; }
        public List<Corner> Corners { get; private set; }
        public List<Graph.Edge> Edges { get; private set; }
        public List<Tri> Polys { get; private set; }
        
        Random _rnd;
        
        public Map(MapConfig config)
        {
            _rnd = new Random(config.Seed);
            Config = config;
            Bounds = new Rectangle(0, 0, config.Width, config.Height);
            Corners = new List<Corner>();
            Edges = new List<Graph.Edge>();
            Polys = new List<Tri>();

            List<Point> points = GeneratePoints(config.NumberOfVoronoiSites, Bounds, config.LloydRelaxationIterations);
            // TODO CJO - remove colors parameter - not going to use it in the scope of the voronoi library
            Voronoi voronoi = new Voronoi(points, new List<uint>(), Bounds);

            BuildGraph(points, voronoi);
            
            // Generate random points √
            // Relax those points √
            // Generate voronoi √
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

        void BuildGraph(List<Point> points, Voronoi voronoi)
        {
            var corners = new List<Corner>();
            var libedges = voronoi.Edges;

            // Create corners for voronoi sites
            var centers = new List<Corner>();
            foreach(var p in points)
            {
                Corner c = new Corner();
                c.Point = p;
                c.IsCenter = true;
                corners.Add(c);
                centers.Add(c);
            }

            // TODO CJO - adjust voronoi library so this isn't required to generate edges and neighboring sites
            foreach(var c in centers)
            {
                voronoi.GetRegion(c.Point);
            }

            List<Corner>[] cornerMap = new List<Corner>[Config.Width + 2];
            foreach(var libedge in libedges)
            {
                var vEdge = libedge.VoronoiEdge;
                var dEdge = libedge.DelaunayLine;

                // Create edge for graph
                Corner c0 = GetOrCreateCornerAtPoint(cornerMap, vEdge.P0);
                Corner c1 = GetOrCreateCornerAtPoint(cornerMap, vEdge.P1);

                if(c0 == null || c1 == null) continue;

                var edge = new Graph.Edge(c0, c1);
                c0.AddEdge(edge);
                c1.AddEdge(edge);
                Edges.Add(edge);

                // Create tris for the sites it's bordering
                if(!dEdge.IsUnassigned())
                {
                    if(!dEdge.P0.IsUnassigned() && !dEdge.P0.AtInfinity())
                    {
                        Corner site = centers.FirstOrDefault(x => x.Point.Equals(dEdge.P0));
                        if(site != null)
                        {
                            var e1 = new Graph.Edge(c0, site);
                            c0.AddEdge(e1);
                            site.AddEdge(e1);
                            var e2 = new Graph.Edge(c1, site);
                            c1.AddEdge(e2);
                            site.AddEdge(e2);
                            var tri = new Tri(edge, e1, e2);
                            Edges.Add(e1);
                            Edges.Add(e2);
                            Polys.Add(tri);
                        }
                    }
                    if(!dEdge.P1.IsUnassigned() && !dEdge.P1.AtInfinity())
                    {
                        Corner site = centers.FirstOrDefault(x => x.Point.Equals(dEdge.P1));
                        if(site != null)
                        {
                            var e1 = new Graph.Edge(c0, site);
                            c0.AddEdge(e1);
                            site.AddEdge(e1);
                            var e2 = new Graph.Edge(c1, site);
                            c1.AddEdge(e2);
                            site.AddEdge(e2);
                            var tri = new Tri(edge, e1, e2);
                            Edges.Add(e1);
                            Edges.Add(e2);
                            Polys.Add(tri);
                        }
                    }
                }
            }

            cornerMap = null;
        }

        // The voronoi library has unique, overlapping points for all of the edges in its graph.
        // We want to consolidate these overlapping points into single Corners.
        Corner GetOrCreateCornerAtPoint(List<Corner>[] map, Point p)
        {
            Corner result = null;

            if(p.IsUnassigned() || p.AtInfinity()) return result;

            int bucket = Convert.ToInt32(p.X);
            for(int i = System.Math.Max(0, bucket - 1); i <= bucket + 1; i++)
            {
                if(map[i] != null)
                {
                    foreach(var c in map[i])
                    {
                        var dx = c.Point.X - p.X;
                        var dy = c.Point.Y - p.Y;
                        if(dx * dx + dy * dy < 1e-6) return c;
                    }
                }
            }

            if(map[bucket] == null)
                map[bucket] = new List<Corner>();

            result = new Corner();
            result.Point = p;
            result.IsBorder = p.X <= 0 || p.X >= Config.Width || p.Y <= 0 || p.Y >= Config.Height;

            map[bucket].Add(result);
            Corners.Add(result);

            return result;
        }
    }
}
