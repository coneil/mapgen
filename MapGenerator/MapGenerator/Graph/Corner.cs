using System;
using System.Collections.Generic;
using coneil.Math.Voronoi.Geometry;

namespace coneil.World.Map.Graph
{
    // Represents any point in our custom graph where two line segments meet.
    // This could be the edges of a voronoi poly meeting, or the line segment of one of those corners meeting with a voronoi site.
    public class Corner
    {
        public Point Point;
        public bool IsCenter; // does this corner represent a voronoi site?
        public Corner Downslope; // the corner this corner passes water to
        public Corner Watershed; // the coastal corner reached by moving downslope, if any

        // General flags and values to describe the topographical nature of the corner
        public bool IsWater;
        public bool IsOcean;
        public bool IsCoast;
        public bool IsBorder;
        public float Elevation; // arbitrary value, relative to all other points. typically normalized
        public float Moisture; // like elevation, normalized
        public int RiverVolume; // non-zero value if river, representing relative volume of river at this point

        public List<Edge> Edges { get; private set; }

        private List<Corner> _neighbors;
        public List<Corner> NeighboringCorners
        {
            get
            {
                if(_neighbors.Count != Edges.Count)
                {
                    _neighbors.Clear();
                    foreach(var e in Edges)
                    {
                        _neighbors.Add(e.Other(this));
                    }
                }
                return _neighbors;
            }
        }

        public void AddEdge(Edge edge)
        {
            if(Edges.Contains(edge)) return;
            Edges.Add(edge);
        }

        public Corner()
        {
            Edges = new List<Edge>();
            _neighbors = new List<Corner>();
            Elevation = 0f;
            Moisture = 0f;
            RiverVolume = 0;
        }
    }
}
