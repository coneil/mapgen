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
        public float MinimumElevation;
        public float MaximumElevation;
        public int NumberOfBlobs;
        
        public int WatershedMaxSteps;
        public int MinimumNumberOfRivers;
        public int MaximumNumberOfRivers;
        public float MinimumRiverSourceElevation;
        public float MaximumRiverSourceElevation;

        public MapConfig()
        {
            Width = Height = 512;
            NumberOfVoronoiSites = 1 << 12;
            LloydRelaxationIterations = 3;
            Seed = new Random().Next(int.MaxValue);
            LakeThreshold = 0.3f;
            MinimumElevation = 0f;
            MaximumElevation = 1f;
            NumberOfBlobs = 20;
            WatershedMaxSteps = 20;
            MinimumNumberOfRivers = 10;
            MaximumNumberOfRivers = 20;
            MinimumRiverSourceElevation = 0.5f;
            MaximumRiverSourceElevation = 0.9f;
        }
    }
}
