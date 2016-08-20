using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace coneil.World.Map.Graph
{
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

        public Tri(Edge e0, Edge e1, Edge e2)
        {
            E0 = e0;
            E1 = e1;
            E2 = e2;

            Edges = new Edge[3] { E0, E1, E2 };

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
                }
            }
            
            if(VoronoiSite == null)
                throw new Exception("Voronoi site not found for tri! All tris should be created from a voronoi site and one of its edges.");

            Corners = new Corner[3] { VoronoiSite, EdgeCorner0, EdgeCorner1 };
        }
    }
}
