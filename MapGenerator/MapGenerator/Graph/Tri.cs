using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace coneil.World.Map.Graph
{
    public enum Biome { NONE, OCEAN, MARSH, ICE, LAKE, BEACH, SNOW, TUNDRA, BARE, SCORCHED, TAIGA, SHRUBLAND, TEMPERATE_DESERT, TEMPERATE_RAIN_FOREST,
                        TEMPERATE_DECIDUOUS_FOREST, TROPICAL_RAIN_FOREST, TROPICAL_SEASONAL_FOREST, GRASSLAND, SUBTROPICAL_DESERT }
    // The smallest poly tracked in our custom graph.
    // Represented by the edges of the tri, it is constructed from a voronoi site and one of its edges.
    public class Tri
    {
        public Edge E0;
        public Edge E1;
        public Edge E2;

        public Edge[] Edges;
        public Corner[] Corners;

        public Corner VoronoiSite { get; private set; }
        public Corner EdgeCorner0 { get; private set; }
        public Corner EdgeCorner1 { get; private set; }

        public Biome Biome;

        public Tri(Edge e0, Edge e1, Edge e2)
        {
            E0 = e0;
            E1 = e1;
            E2 = e2;

            Edges = new Edge[3] { E0, E1, E2 };

            foreach(var edge in Edges)
            {
                if(edge.P0 == null)
                {
                    edge.P0 = this;
                }
                else if(edge.P1 == null)
                {
                    edge.P1 = this;
                }

                foreach(var corner in edge.Corners)
                {
                    corner.AddPoly(this);
                }
            }

            List<Corner> corners = new List<Corner>();
            foreach(var e in Edges)
            {
                foreach(var c in e.Corners)
                {
                    if(c.IsCenter)
                        VoronoiSite = c;
                    else if(EdgeCorner0 == null)
                        EdgeCorner0 = c;
                    else if(EdgeCorner1 == null && EdgeCorner0 != c)
                        EdgeCorner1 = c;

                    if(!corners.Contains(c)) corners.Add(c);
                }
            }

            Corners = corners.ToArray();
            
            if(VoronoiSite == null)
                throw new Exception("Voronoi site not found for tri! All tris should be created from a voronoi site and one of its edges.");
        }

        public float GetElevation()
        {
            return Corners.Average(x => x.Elevation);
        }

        public bool IsWater(float lakeThreshold)
        {
            if(IsOcean()) return true;

            if(Corners.Count(x => x.IsWater) == Corners.Length) return true;

            if(Corners.Count(x => x.IsCoast) > 0) return false;

            List<Corner> corners = new List<Corner>();
            foreach(var c in Corners)
            {
                corners.Add(c);

                foreach(var n in c.NeighboringCorners)
                {
                    if(corners.Contains(n)) continue; 

                    corners.Add(n);
                }
            }

            return (corners.Count(x => x.IsWater) / (float) corners.Count) > lakeThreshold;
        }

        public bool IsOcean()
        {
            int numWater = Corners.Count(x => x.IsOcean);
            return numWater > 2 || (numWater > 0 && Corners.Count(x => x.IsCoast) > 0);
        }

        public float GetMoisture()
        {
            return Corners.Average(x => x.Moisture);
        }

        public bool IsCoast()
        {
            return Corners.Any(x => x.IsCoast) && !IsOcean();
        }
    }
}
