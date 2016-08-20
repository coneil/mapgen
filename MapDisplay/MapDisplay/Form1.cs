using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using coneil.Math.Voronoi;
using coneil.Math.Voronoi.Geometry;
using coneil.World;
using coneil.World.Map;
using coneil.World.Map.Graph;

namespace MapDisplay
{
    public partial class MapDisplay : Form
    {
        private coneil.World.Map.Map _map;
        private bool _draw;

        public MapDisplay()
        {
            InitializeComponent();
            this.Paint += MapDisplay_Paint;
            _draw = false;
        }

        void MapDisplay_Paint(object sender, PaintEventArgs e)
        {
            if(_draw)
            {
                var g = this.CreateGraphics();
                var p = new Pen(Color.Black, 1f);

                foreach(coneil.World.Map.Graph.Edge edge in _map.Edges)
                {
                    g.DrawLine(p, new PointF(f(edge.C0.Point.X), f(edge.C0.Point.Y)), new PointF(f(edge.C1.Point.X), f(edge.C1.Point.Y)));
                } 
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var config = new MapConfig();
            config.NumberOfVoronoiSites = 1 << 12;
            config.LloydRelaxationIterations = 3;

            _map = new coneil.World.Map.Map(config);
            _draw = true;
        }

        float f(double d)
        {
            return Convert.ToSingle(d);
        }
    }
}
