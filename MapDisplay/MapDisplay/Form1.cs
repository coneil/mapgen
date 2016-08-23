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
                var rp = new Pen(Color.Red, 1f);
                var gp = new Pen(Color.Green, 1f);

                //PaintElevation(g);

                //PaintPolys(g);

                PaintBiomes(g);

                PaintCoastline(g);

                PaintRivers(g);

                //PaintWatershed(g);

                //PaintCorners(g);

                //PaintEdges(g);
                
                _draw = false;
            }
        }

        void PaintBiomes(Graphics g)
        {
            var colors = new Dictionary<Biome, Color>()
            {
                { Biome.NONE, Color.Black },
                { Biome.OCEAN, Color.Blue },
                { Biome.MARSH, Color.DarkSeaGreen },
                { Biome.ICE, Color.SteelBlue },
                { Biome.LAKE, Color.Blue },
                { Biome.SNOW, Color.White },
                { Biome.TUNDRA, Color.Wheat },
                { Biome.BARE, Color.LightGray },
                { Biome.SCORCHED, Color.Gray },
                { Biome.TAIGA, Color.Bisque },
                { Biome.SHRUBLAND, Color.Beige },
                { Biome.TEMPERATE_DESERT, Color.SaddleBrown},
                { Biome.TEMPERATE_RAIN_FOREST, Color.ForestGreen },
                { Biome.BEACH, Color.SandyBrown },
                { Biome.TEMPERATE_DECIDUOUS_FOREST, Color.DarkGreen },
                { Biome.GRASSLAND, Color.LawnGreen },
                { Biome.TROPICAL_RAIN_FOREST, Color.DarkOliveGreen },
                { Biome.TROPICAL_SEASONAL_FOREST, Color.GreenYellow },
                { Biome.SUBTROPICAL_DESERT, Color.PaleVioletRed }
            };

            foreach(var p in _map.Polys)
            {
                var points = new List<PointF>();
                foreach(var c in p.Corners)
                {
                    points.Add(new PointF(f(c.Point.X), f(c.Point.Y)));
                }
                
                Color color = colors[p.Biome];

                Brush b = new SolidBrush(color);
                g.FillPolygon(b, points.ToArray());
            }
        }

        void PaintRivers(Graphics g)
        {
            Pen p = new Pen(Color.Blue);
            foreach(var edge in _map.Edges)
            {
                if(edge.RiverVolume > 0)
                {
                    PaintEdge(edge, g, p);
                }
            }
        }

        void PaintWatershed(Graphics g)
        {
            Pen p = new Pen(Color.AliceBlue);
            foreach(var c in _map.Corners)
            {
                if(!c.IsOcean && !c.IsCoast)
                {
                    g.DrawEllipse(p, f(c.Watershed.Point.X), f(c.Watershed.Point.Y), 3f, 2f);
                }
            }
        }

        void PaintElevation(Graphics g)
        {
            var ordered = _map.Polys.OrderBy(x => x.GetElevation());
            float lowest = ordered.ElementAt(0).GetElevation();
            float highest = ordered.ElementAt(_map.Polys.Count - 1).GetElevation();

            foreach(var poly in _map.Polys)
            {
                var points = new List<PointF>();
                foreach(var c in poly.Corners)
                {
                    points.Add(new PointF(f(c.Point.X), f(c.Point.Y)));
                }

                float el = poly.GetElevation();
                el = (el - lowest) / (highest - lowest);

                Color color = LerpColor(Color.Blue, Color.Yellow, el);

                Brush b = new SolidBrush(color);
                g.FillPolygon(b, points.ToArray());
            }
        }

        void PaintPolys(Graphics g)
        {
            foreach(var poly in _map.Polys)
            {
                var points = new List<PointF>();
                foreach(var c in poly.Corners)
                {
                    points.Add(new PointF(f(c.Point.X), f(c.Point.Y)));
                }

                float el = poly.GetElevation();

                Color color;
                if(poly.IsOcean())
                    color = LerpColor(Color.DarkBlue, Color.Blue, el);
                else if(poly.IsWater(_map.Config.LakeThreshold))
                    color = Color.Blue;
                else
                    color = LerpColor(Color.Tan, Color.AntiqueWhite, el);

                Brush b = new SolidBrush(color);
                g.FillPolygon(b, points.ToArray());
            }
        }

        void PaintCoastline(Graphics g)
        {
            var p = new Pen(Color.Black, 1f);
            foreach(var edge in _map.Edges)
            {
                if(edge.C0.IsCoast && edge.C1.IsCoast)
                {
                    if(edge.P0 != null && edge.P1 != null)
                    {
                        if(edge.P0.IsOcean() != edge.P1.IsOcean())
                        {
                            PaintEdge(edge, g, p);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Null poly");
                    }
                }
            }
        }

        void PaintCorners(Graphics g)
        {
            var p = new Pen(Color.Black, 1f);
            foreach(var corner in _map.Corners)
            {
                g.DrawRectangle(p, new System.Drawing.Rectangle(Convert.ToInt32(corner.Point.X), Convert.ToInt32(corner.Point.Y), 2, 2));
            }
        }

        void PaintEdges(Graphics g)
        {
            var p = new Pen(Color.Black, 1f);
            foreach(var edge in _map.Edges)
            {
                PaintEdge(edge, g, p);
            }
        }

        void PaintEdge(coneil.World.Map.Graph.Edge edge, Graphics g, Pen p)
        {
            g.DrawLine(p, new PointF(f(edge.C0.Point.X), f(edge.C0.Point.Y)), new PointF(f(edge.C1.Point.X), f(edge.C1.Point.Y)));
        }

        Color LerpColor(Color a, Color b, float t)
        {
            float first = t;
            t = t < 0 ? 0 : t;
            t = t > 1f ? 1f : t;
            if(first != t) System.Diagnostics.Debug.WriteLine("Clamped t from " + first);
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
            _map.Normalize();
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
