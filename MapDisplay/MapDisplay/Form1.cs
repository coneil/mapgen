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

                foreach(var poly in _map.Polys)
                {
                    // fill
                    var points = new List<PointF>();
                    foreach(var c in poly.Corners)
                    {
                        points.Add(new PointF(f(c.Point.X), f(c.Point.Y)));
                    }

                    float el = poly.GetElevation();
                    Color color;
                    if(el >= 0)
                        color = Color.Black;// LerpColor(Color.Blue, Color.Yellow, el);
                    else
                        color = Color.Red;// LerpColor(Color.Blue, Color.Red, -el);

                    Brush b = new SolidBrush(color);
                    g.FillPolygon(b, points.ToArray());
                }
                /*
                foreach(var edge in _map.Edges)
                {
                    g.DrawLine(p, new PointF(f(edge.C0.Point.X), f(edge.C0.Point.Y)), new PointF(f(edge.C1.Point.X), f(edge.C1.Point.Y)));
                }
                */
                _draw = false;
            }
        }

        Color LerpColor(Color a, Color b, float t)
        {
            t = t < 0 ? 0 : t;
            t = t > 1f ? 1f : t;
            return Color.FromArgb(
                Convert.ToInt32(a.A + (b.A - a.A) * t),
                Convert.ToInt32(a.R + (b.R - a.R) * t),
                Convert.ToInt32(a.G + (b.G - a.G) * t),
                Convert.ToInt32(a.B + (b.B - a.B) * t)
                );
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var config = new MapConfig();
            _map = new coneil.World.Map.Map(config);
            _draw = true;
        }

        float f(double d)
        {
            return Convert.ToSingle(d);
        }

        void Redraw()
        {
            _draw = _map != null;
            Invalidate();
        }
    }
}
