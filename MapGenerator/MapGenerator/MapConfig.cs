using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace coneil.World.Map
{
    public class MapConfig
    {
        public int Width;
        public int Height;
        public int NumberOfVoronoiSites;
        public int LloydRelaxationIterations;
        public int Seed;
        public float LakeThreshold;

        public MapConfig()
        {
            Width = Height = 512;
            NumberOfVoronoiSites = 1 << 14;
            LloydRelaxationIterations = 2;
            Seed = new Random().Next(int.MaxValue);
            LakeThreshold = 0.3f;
        }
    }
}
