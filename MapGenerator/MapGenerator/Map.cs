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

            // Generate proto landscape
            DefineRandomSlope();

            for(int i = 0; i < Config.NumberOfBlobs; i++) { AddBlob(); }

            PlanchonDarbouxCorrection();
            SetSeaLevel();
            AssignOceanAndLand(); // Assumes all ocean is below elevation 0 when it's run (done by SetSeaLevel())
            IdentifyCoastline();
            AssignDownslopesAndWatersheds();
            Normalize(); // Normalize so we can more easily find spots for river sources. Assume elevation values of 0-1 from here on.
            CreateRivers();

            // Erosion sinks a lot of geometry, and requires reidentifying everything afterwards.
            Erode();
            SetSeaLevel();
            
            foreach(var c in Corners)
            {
                c.IsOcean = c.IsWater = c.IsCoast = false;
            }

            AssignOceanAndLand(); // Assumes all ocean is below elevation 0 when it's run (done by SetSeaLevel())
            IdentifyCoastline();
            Normalize();
            RedistributeElevations();
            
            // Remove river paths now in the ocean due to erosion
            foreach(var c in Corners)
            {
                if(c.RiverVolume > 0 && c.IsOcean) c.RiverVolume = 0;
            }

            foreach(var e in Edges)
            {
                if(e.RiverVolume > 0)
                {
                    if(e.P0 != null && e.P0.IsOcean())
                    {
                        e.RiverVolume = 0;
                    }
                    else if(e.P1 != null && e.P1.IsOcean())
                    {
                        e.RiverVolume = 0;
                    }
                }
            }

            CreateMoisture();
            AssignBiomes();
        }

        #region GRAPH GENERATION
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
            var libedges = voronoi.Edges;

            // Create corners for voronoi sites
            var centers = new List<Corner>();
            foreach(var p in points)
            {
                Corner c = new Corner();
                c.Point = p;
                c.IsCenter = true;
                Corners.Add(c);
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

                var edge = GetOrCreateEdge(c0, c1);
                
                // Create tris for the sites it's bordering
                if(!dEdge.IsUnassigned())
                {
                    if(!dEdge.P0.IsUnassigned() && !dEdge.P0.AtInfinity())
                    {
                        Corner site = centers.FirstOrDefault(x => x.Point.Equals(dEdge.P0));
                        if(site != null)
                        {
                            var e1 = GetOrCreateEdge(c0, site);
                            var e2 = GetOrCreateEdge(c1, site);
                            var tri = new Tri(edge, e1, e2);
                            Polys.Add(tri);
                        }
                    }
                    if(!dEdge.P1.IsUnassigned() && !dEdge.P1.AtInfinity())
                    {
                        Corner site = centers.FirstOrDefault(x => x.Point.Equals(dEdge.P1));
                        if(site != null)
                        {
                            var e1 = GetOrCreateEdge(c0, site);
                            var e2 = GetOrCreateEdge(c1, site);
                            var tri = new Tri(edge, e1, e2);
                            Polys.Add(tri);
                        }
                    }
                }
            }

            cornerMap = null;
        }

        Graph.Edge GetOrCreateEdge(Corner a, Corner b)
        {
            foreach(var e in Edges)
            {
                if((e.C0 == a && e.C1 == b) || (e.C0 == b && e.C1 == a))
                {
                    return e;
                }
            }

            var edge = new Graph.Edge(a, b);
            a.AddEdge(edge);
            b.AddEdge(edge);
            Edges.Add(edge);
            return edge;
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
        #endregion

        #region GRAPH OPERATIONS
        public void DefineRandomSlope()
        {
            // Create random line segment to act as our highest point/slope
            // Elevation is set as distance to segment
            Point v = new Point();
            v.X = _rnd.NextDouble() * Bounds.Width;
            v.Y = _rnd.NextDouble() * Bounds.Height;
            Point w = new Point();
            w.X = _rnd.NextDouble() * Bounds.Width;
            w.Y = _rnd.NextDouble() * Bounds.Height;

            Func<Point, Point, double> dist2 = (a, b) => { return (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y); };

            double l2 = dist2(v, w);
            double maxDist = Bounds.Width > Bounds.Height ? Bounds.Width * Bounds.Width : Bounds.Height * Bounds.Height;

            foreach(var c in Corners)
            {
                var p = c.Point;
                double dist = 0;
                if(l2 == 0)
                {
                    dist = dist2(p, v);
                }
                else
                {
                    double t = ((p.X - v.X) * (w.X - v.X) + (p.Y - v.Y) * (w.Y - v.Y)) / l2;
                    t = t < 0 ? 0 : t;
                    t = t > 1 ? 1 : t;
                    Point d = new Point(v.X + t * (w.X - v.X), v.Y + t * (w.Y - v.Y));
                    dist = dist2(p, d);
                }

                c.Elevation = Convert.ToSingle(1D - (dist / maxDist));
                c.Elevation = c.Elevation < 0 ? 0 : c.Elevation;
                c.Elevation = c.Elevation > 1 ? 1 : c.Elevation;
            }
        }

        // Scale elevation of all corners so that they lie w/in a range of 0-1
        public void Normalize()
        {
            var ordered = Corners.OrderBy(x => x.Elevation);
            var lowest = ordered.ElementAt(0).Elevation;
            var highest = ordered.ElementAt(ordered.Count() - 1).Elevation;
            var range = highest - lowest;
            foreach(var c in Corners)
            {
                c.Elevation = (c.Elevation - lowest) / range;
            }
        }

        // Curve out elevations so that higher peaks occur with less frequency
        void RedistributeElevations()
        {
            var sorted = Corners.OrderBy(x => x.Elevation).ToArray();
            for(int i = 0; i < sorted.Length; i++)
            {
                float y = i / (float) (sorted.Length - 1);
                double x = System.Math.Sqrt(1.1f) - System.Math.Sqrt(1.1f * (1f - y));
                if(x > 1) x = 1;
                sorted[i].Elevation = Convert.ToSingle(x);
            }
        }

        // Assign sqrt of elevation to elevation, blunting higher elevations
        public void Round()
        {
            foreach(var c in Corners)
            {
                c.Elevation = Convert.ToSingle(System.Math.Sqrt(c.Elevation));
            }
        }

        public void Relax()
        {
            List<float> elevations = new List<float>();
            for(int i = 0; i < Corners.Count; i++)
            {
                float avg = 0f;
                foreach(var n in Corners[i].NeighboringCorners)
                {
                    avg += n.Elevation;
                }
                avg /= (float)Corners[i].NeighboringCorners.Count;
                elevations.Add(avg);
            }
            for(int i = 0; i < Corners.Count; i++)
            {
                Corners[i].Elevation = elevations[i];
            }
        }

        // Adjust elevations of all corners to move graph up/down wherein elevation 0 is at median
        public void SetSeaLevel()
        {
            float median = Convert.ToSingle(Corners.Median(x => x.Elevation));
            foreach(var c in Corners)
            {
                c.Elevation -= median;
            }
        }

        public void AddBlob()
        {
            int index = _rnd.Next(Corners.Count);
            List<Corner> touched = new List<Corner>();
            List<Corner> queue = new List<Corner>() { Corners[index] };
            float steps = Convert.ToSingle((_rnd.NextDouble() * 6) + 10);
            float step = 1f;
            while(step < steps)
            {
                var q = queue.ToList();
                queue.Clear();

                foreach(var c in q)
                {
                    touched.Add(c);

                    var e = (1f - step /(float) steps) + Convert.ToSingle((_rnd.NextDouble() * 0.1f));
                    c.Elevation = e < c.Elevation ? c.Elevation : e;

                    foreach(var neighbor in c.NeighboringCorners)
                    {
                        if(!touched.Contains(neighbor) && !queue.Contains(neighbor))
                            queue.Add(neighbor);
                    }
                }

                step++;
            }
        }
        #endregion

        #region LAND AND OCEAN
        public void PlanchonDarbouxCorrection()
        {
            List<Corner> queue = new List<Corner>();
            // Hijacking Moisture to store current elevation
            foreach(var c in Corners)
            {
                c.Moisture = c.Elevation;
                if(!c.IsBorder)
                    c.Elevation = float.PositiveInfinity;
            }

            while(true)
            {
                bool touched = false;
                foreach(var c in Corners)
                {
                    float lowest = float.PositiveInfinity;
                    foreach(var n in c.NeighboringCorners)
                    {
                        if(n.Elevation < c.Elevation)
                        {
                            lowest = n.Elevation;
                        }
                    }
                    if(lowest < float.PositiveInfinity)
                    {
                        float desiredElevation = lowest + 0.001f;
                        desiredElevation = desiredElevation < c.Moisture ? c.Moisture : desiredElevation;
                        if(c.Elevation == float.PositiveInfinity)
                        {
                            c.Elevation = desiredElevation;
                            touched = true;
                        }
                        else if(desiredElevation > c.Elevation)
                        {
                            c.Elevation = desiredElevation;
                            touched = true;
                        }
                    }
                }

                if(!touched) break;
            }

            // Reset moisture
            foreach(var c in Corners) c.Moisture = 0f;
        }

        public void AssignOceanAndLand()
        {
            // Mark all water points, with border points being ocean
            List<Corner> oceanQueue = new List<Corner>();
            foreach(var c in Corners)
            {
                if(c.Elevation <= 0f)
                {
                    c.IsWater = true;

                    if(c.IsBorder)
                    {
                        c.IsOcean = true;
                        oceanQueue.Add(c);
                    }
                }
            }

            // Assign ocean to water points neighboring ocean points
            while(oceanQueue.Count > 0)
            {
                Corner c = oceanQueue[0];
                oceanQueue.RemoveAt(0);
                foreach(var n in c.NeighboringCorners)
                {
                    if(!n.IsOcean && n.IsWater)
                    {
                        n.IsOcean = true;
                        oceanQueue.Add(n);
                    }
                }
            }
        }

        void IdentifyCoastline()
        {
            // Identify all coastal points
            foreach(var c in Corners)
            {
                if(c.IsWater) continue;

                c.IsCoast = c.NeighboringCorners.Count(x => x.IsOcean) > 0;
            }

            // A sweep is made on edges to identify corners that should not be identify as coastal
            // due to neighboring polygons identifying as ocean based on their rules.
            foreach(var edge in Edges)
            {
                if(edge.C0.IsCoast && edge.C1.IsCoast)
                {
                    if(edge.P0 != null && edge.P1 != null)
                    {
                        if(edge.P0.IsOcean() == edge.P1.IsOcean())
                        {
                            if(edge.P0.IsOcean())
                            {
                                // This edge is sticking out from land. One of the corners is a legitimate part of the coast.
                                // The other should be reset.
                                foreach(var corner in edge.Corners)
                                {
                                    if(corner.Polys.Count(x => !x.IsOcean()) == 0)
                                    {
                                        corner.IsCoast = false;
                                        corner.IsOcean = true;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                // This edge is made up of two, valid coastal points, but cuts across a penninsula.
                                // It can be left alone.
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Null poly");
                    }
                }
            }
        }
        #endregion

        #region RIVERS AND EROSION
        void AssignDownslopesAndWatersheds()
        {
            foreach(var c in Corners)
            {
                Corner d = c;
                foreach(var n in c.NeighboringCorners)
                {
                    if(n.Elevation <= d.Elevation)
                    {
                        d = n;
                    }
                }
                c.Downslope = d;
                c.Watershed = c;
                if(!c.IsOcean && !c.IsCoast) c.Watershed = c.Downslope;
            }

            var carriers = Corners.Where(x => !x.IsOcean && !x.IsCoast && !x.Watershed.IsCoast).ToArray();
            int step = 0;
            bool changed;
            do
            {
                step++;
                changed = false;
                foreach(var c in carriers)
                {
                    if(!c.Watershed.IsCoast)
                    {
                        Corner w = c.Watershed.Watershed;
                        if(!w.IsOcean && w != c.Watershed)
                        {
                            c.Watershed = w;
                            changed = true;
                        }
                    }
                }
            }
            while(step <= Config.WatershedMaxSteps && changed);
        }

        void CreateRivers()
        {
            int attempts = Config.MinimumNumberOfRivers + _rnd.Next(Config.MaximumNumberOfRivers - Config.MinimumNumberOfRivers);
            var candidates = Corners.Where(x => x.Elevation >= Config.MinimumRiverSourceElevation && x.Elevation <= Config.MaximumRiverSourceElevation && !x.IsOcean).ToList();
            for(int i = 0; i < attempts && candidates.Count > 0; i++)
            {
                var c = candidates[_rnd.Next(candidates.Count)];
                candidates.Remove(c);

                while(!c.IsCoast)
                {
                    c.RiverVolume += 1;
                    var next = c.Downslope;
                    var edge = c.Edges.FirstOrDefault(x => x.OtherCorner(c) == next);
                    if(edge != null)
                    {
                        edge.RiverVolume += 1;
                        
                        if(next.IsCoast)
                        {
                            next.RiverVolume += 1;
                            break;
                        }
                        else
                        {
                            c = next;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No corner found for connnecte edge. How'd that happen?");
                        break;
                    }
                }
            }
        }

        public void Erode()
        {
            // Hijacking Moisture to record waterflow
            foreach(var c in Corners)
            {
                if(c.IsOcean) continue;

                c.Moisture += 0.001f;
                var n = c;
                while(n.Downslope != null && n.Downslope != n)
                {
                    n.Downslope.Moisture += 0.001f;
                    n = n.Downslope;
                }
            }

            var sorted = Corners.OrderByDescending(x => x.Moisture).ToArray();
            for(int i = 0; i < Config.ErosionSteps; i++)
            {
                for(int j = 0; j < sorted.Length; j++)
                {
                    Corner s = sorted[j];
                    double slopes = 0f;
                    foreach(var e in s.Edges)
                    {
                        slopes += System.Math.Abs(e.OtherCorner(s).Elevation - s.Elevation);
                    }
                    slopes /= (float) s.Edges.Count;

                    //s.Elevation 
                    double mod = System.Math.Sqrt(s.Moisture);
                    float el = Convert.ToSingle(mod * slopes);
                    s.Elevation -= el;
                    //System.Diagnostics.Debug.WriteLine(s.Elevation + " => " + el + " from " + slopes + " and " + mod);
                }
            }

            foreach(var c in Corners)
            {
                c.Moisture = 0f;
            }
        }

        void CreateMoisture()
        {
            var queue = new List<Corner>();
            foreach(var c in Corners)
            {
                if(c.IsOcean || c.IsCoast)
                {
                    c.Moisture = 1f;
                }
                else if(c.IsWater || c.RiverVolume > 0)
                {
                    c.Moisture = c.RiverVolume > 0 ? System.Math.Min(3f, (0.2f * c.RiverVolume)) : 1f;
                    queue.Add(c);
                }
            }

            while(queue.Count > 0)
            {
                var next = queue[0];
                queue.RemoveAt(0);

                foreach(var n in next.NeighboringCorners)
                {
                    if(n.IsOcean || n.IsCoast) continue;

                    float moisture = next.Moisture * 0.9f;
                    if(moisture > n.Moisture)
                    {
                        n.Moisture = moisture;
                        queue.Add(n);
                    }
                }
            }
        }

        void AssignBiomes()
        {
            foreach(var p in Polys)
            {
                p.Biome = AssignBiome(p);
            }
        }

        Biome AssignBiome(Tri p)
        {
            float elevation = p.GetElevation();
            float moisture = p.GetMoisture();
            bool ocean = p.IsOcean();
            bool water = p.IsWater(Config.LakeThreshold);
            bool coast = p.IsCoast();

            if(ocean)
            {
                return Biome.OCEAN;
            }
            else if(water)
            {
                if(elevation < 0.1f) return Biome.MARSH;
                if(elevation > 0.8f) return Biome.ICE;
                return Biome.LAKE;
            }
            else if(coast)
            {
                return Biome.BEACH;
            }
            else if(elevation > 0.8f)
            {
                if(moisture > 0.5f) return Biome.SNOW;
                if(moisture > 0.33f) return Biome.TUNDRA;
                if(moisture > 0.16f) return Biome.BARE;
                return Biome.SCORCHED;
            }
            else if(elevation > 0.6f)
            {
                if(moisture > 0.66f) return Biome.TAIGA;
                if(moisture > 0.33f) return Biome.SHRUBLAND;
                return Biome.TEMPERATE_DESERT;
            }
            else if(elevation > 0.3f)
            {
                if(moisture > 0.83f) return Biome.TEMPERATE_RAIN_FOREST;
                if(moisture > 0.5f) return Biome.TEMPERATE_DECIDUOUS_FOREST;
                if(moisture > 0.16f) return Biome.GRASSLAND;
                return Biome.TEMPERATE_DESERT;
            }
            else
            {
                if(moisture > 0.66f) return Biome.TROPICAL_RAIN_FOREST;
                if(moisture > 0.33f) return Biome.TROPICAL_SEASONAL_FOREST;
                if(moisture > 0.16f) return Biome.GRASSLAND;
                return Biome.SUBTROPICAL_DESERT;
            }
        }
        #endregion
    }
}
