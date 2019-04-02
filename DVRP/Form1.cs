using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using TspLibNet;
using TspLibNet.TSP;

namespace DVRP
{

    public partial class Form1 : Form
    {
        private bool testing = true;
        private int scale = 10;
        private Graph G;
        private List<List<int>> solution;
        public Form1()
        {
            InitializeComponent();
            Recalculate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            PaintGraph(G);
        }

        private void PaintGraph(Graph Graph)
        {
            if (!testing) return;
            Graphics g = this.CreateGraphics();
            g.Clear(Color.White);
            Pen pen = new Pen(Color.Black);
            System.Drawing.Font drawFont = new System.Drawing.Font("Arial", 10);
            System.Drawing.SolidBrush drawBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Black);
            System.Drawing.StringFormat drawFormat = new System.Drawing.StringFormat();
            int i = 0;
            foreach (var v in Graph.Vertices)
            {
                g.DrawEllipse(pen, (float) v.X*scale, (float) v.Y*scale, 5, 5);
                g.DrawString(i.ToString() + "(" + Graph.Vertices[i].Demand + ")", drawFont, drawBrush, (float)v.X*scale, (float)v.Y*scale, drawFormat);
                i++;
            }

            List<Color> colours = new List<Color>() { Color.Aqua, Color.Black, Color.Red, Color.Orange, Color.Yellow, Color.Blue, Color.Orchid, Color.Fuchsia };
            for (int k = 0; k < solution.Count; k++)
            {
                pen = new Pen(colours[k % 8]);
                for (int j = 0; j < solution[k].Count - 1; j++)
                {
                    var v1 = Graph.Vertices[solution[k][j]];
                    var v2 = Graph.Vertices[solution[k][j + 1]];
                    g.DrawLine(pen, (float) v1.X *scale, (float) v1.Y *scale, (float) v2.X *scale, (float) v2.Y *scale);
                }
            }
        }

        private void Recalculate()
        {
            if (!testing) return;
            G = new Graph();
            CapacitatedVehicleRoutingProblem problem = CapacitatedVehicleRoutingProblem.FromFile("Benchmarks/A-n53-k7.vrp");
            var nodes = problem.NodeProvider.GetNodes();
            for (int i = 0; i < nodes.Count; i++)
            {
                TspLibNet.Graph.Nodes.Node2D node = (TspLibNet.Graph.Nodes.Node2D)nodes[i];
                double demand = problem.DemandProvider.GetDemand(nodes[i]);
                Vertice v = new Vertice();
                v.X = node.X;
                v.Y = node.Y;
                v.Demand = demand;
                G.Vertices.Add(v);
            }
            G.RecalculateVertices();
            solution = G.FindDVRPSolution();
            double usage = G.CalculateUsage(solution);
            textBox1.Text = usage.ToString();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            Recalculate();
        }
    }
}
