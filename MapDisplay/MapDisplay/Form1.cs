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

                foreach(coneil.Math.Voronoi.Edge edge in _map.VoronoiGraph.Edges)
                {
                    if(!edge.Visible) continue;
                    var ve = edge.VoronoiEdge;
                    g.DrawLine(p, new PointF(f(ve.P0.X), f(ve.P0.Y)), new PointF(f(ve.P1.X), f(ve.P1.Y)));
                } 
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            const int SIZE = 512;
            const int NUM_POINTS = 1 << 12;
            coneil.Math.Voronoi.Geometry.Rectangle bounds = new coneil.Math.Voronoi.Geometry.Rectangle(0, 0, SIZE, SIZE);
            _map = new coneil.World.Map.Map(NUM_POINTS, 2, bounds);
            _draw = true;
        }

        float f(double d)
        {
            return Convert.ToSingle(d);
        }
    }
}
