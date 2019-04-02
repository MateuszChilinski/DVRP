using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using TspLibNet;
using TspLibNet.TSP;

namespace DVRP
{
    static class Program
    {
        private static bool testing = false;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (testing)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
            else
            {
                RunBenchmarks();
            }
        }

        private static void RunCalculation(string file, string timeStamp)
        {
            TspFile tspfile = TspFile.Load(file);
            CapacitatedVehicleRoutingProblem problem = CapacitatedVehicleRoutingProblem.FromFile(file);
            Graph Graph = new Graph(_maxDemand: tspfile.Capacity);
            var nodes = problem.NodeProvider.GetNodes();
            for (int i = 0; i < nodes.Count; i++)
            {
                TspLibNet.Graph.Nodes.Node2D node = (TspLibNet.Graph.Nodes.Node2D)nodes[i];
                double demand = problem.DemandProvider.GetDemand(nodes[i]);
                Vertice v = new Vertice();
                v.X = node.X;
                v.Y = node.Y;
                v.Demand = demand;
                Graph.Vertices.Add(v);
            }
            Graph.RecalculateVertices();
            var maxDistance = Graph.Edges.SelectMany(x => x).Max(x => x.Weigth)*200;
            Graph.maxDistance = maxDistance;
            var solution = Graph.FindDVRPSolution();
            double usage = Graph.CalculateUsage(solution);
            string paramsCSV = file + "," + Graph.GetParamsCSV() + ",\"" + tspfile.Comment + "\"," + usage + "\n";
            System.IO.File.AppendAllText(@"Benchmarks" + timeStamp + ".csv", paramsCSV);
        }
        private static void RunBenchmarks()
        {
            String timeStamp = GetTimestamp(DateTime.Now);
            string[] filePaths = Directory.GetFiles(@"Benchmarks");
            int each = 50;
            Parallel.For(0, each, (int current) => Parallel.ForEach(filePaths, (string file) => RunCalculation(file, timeStamp)));
        }
        private static String GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssffff");
        }
    }
}
