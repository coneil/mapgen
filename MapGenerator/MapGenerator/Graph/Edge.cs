using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace coneil.World.Map.Graph
{
    public class Edge
    {
        public Corner C0;
        public Corner C1;
        public Tri P0;
        public Tri P1;

        public Corner[] Corners { get; private set; }

        public int RiverVolume;

        public Corner OtherCorner(Corner c)
        {
            return C0 == c ? C1 : C0;
        }

        public Edge(Corner c0, Corner c1)
        {
            C0 = c0;
            C1 = c1;

            Corners = new Corner[2] { C0, C1 };

            RiverVolume = 0;
        }
    }
}
