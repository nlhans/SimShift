using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SimShift.Dialogs
{
    public partial class ucPlotter : UserControl
    {
        private int channels = 0;

        public List<List<double>> values = new List<List<double>>();
        private List<float> valueScale = new List<float>();
        private int gridsHorizontal = 20;
        private int gridsVertical = 20;

        private float samplesPerDiv = 10;

        private Pen gridPen = new Pen(Color.DarkSeaGreen, 1.0f);
        private Pen[] pens = new Pen[]
                                       {
                                           new Pen(Color.Yellow, 1.0f),
                                           new Pen(Color.Red, 1.0f),
                                           new Pen(Color.DeepSkyBlue, 1.0f),
                                           new Pen(Color.GreenYellow, 1.0f),
                                           new Pen(Color.Magenta, 3.0f)
                                       };

        public ucPlotter(int ch, float[] scale)
        {
            channels = ch;
            for (int k = 0; k < ch; k++)
            {
                values.Add(new List<double>());
            valueScale.Add(scale[k]/gridsVertical*2);
            }
            var emptyList = new List<double>();
            for (int k = 0; k < ch; k++) emptyList.Add(0);
            for (int i = 0; i < 1000;i++)
            {
                    Add(emptyList);
            }
                InitializeComponent();
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        }

        public void Add(List<double> v )
        {
            for (int k = 0; k < channels; k++)
            {
                values[k].Add(v[k]);
                while (values[k].Count > samplesPerDiv*gridsHorizontal)
                    values[k].RemoveAt(0);
            }
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            samplesPerDiv = 100;
            base.OnPaint(e);

            var g = e.Graphics;

            g.FillRectangle(Brushes.Black, e.ClipRectangle);

            var w = e.ClipRectangle.Width;
            var h = e.ClipRectangle.Height;

            
            var pxHor = w/gridsHorizontal;
            var pxVer = h/gridsVertical;
            
            for(int i = 0; i <= gridsHorizontal; i++)
            {
                g.DrawLine(gridPen, pxHor*i, 0, pxHor*i, h);
            }
            for(int i = 0; i <= gridsVertical; i++)
            {
                g.DrawLine(gridPen, 0, pxVer*i, w, pxVer*i);
            }

            // Display all values
            for (int chIndex = 0; chIndex < values.Count; chIndex++)
            {
                try
                {
                    if (pens.Length >= chIndex && values.Count >= chIndex && valueScale.Count >= chIndex)
                    {

                    }
                    else break;

                    var chPen = pens[chIndex];
                    var ch = values[chIndex];
                    var scale = valueScale[chIndex];

                    var lastX = 0.0f;
                    var lastY = (float) h/2.0f;

                    for (int sampleIndex = 0; sampleIndex < ch.Count; sampleIndex++)
                    {
                        var v = ch[sampleIndex];
                        var curX = (float) (sampleIndex*w/ch.Count);
                        var curY = (float)(gridsVertical / 2 - v / scale) * pxVer;

                        g.DrawLine(chPen, lastX, lastY, curX, curY);

                        lastX = curX;
                        lastY = curY;
                    }
                }catch(Exception ex)
                {
                }
            }
        }
    }
}
