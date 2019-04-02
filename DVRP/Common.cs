using System;
using System.Collections.Generic;
using System.Linq;

namespace DVRP
{
    public class CityProbability
    {
        public CityProbability(int city, double probability)
        {
            City = city;
            Probability = probability;
        }

        public int City { get; }
        public double Probability { get; set; }
    }

    public class Graph
    {
        private readonly Random rnd = new Random();

        int kMax = 20; // number of trucks
        public double maxDistance { get; set; } // max distance by car
        double maxDemand = 100; // sum of demand car can do
        int iterations = 500;
        double ro = 0.8; // wspolczynnik odparowywania fermonu
        double Q = 100;
        double alpha = 2;
        double beta = 5;
        public Graph(int _kMax = 20, double _maxDistance = 500, double _maxDemand = 100, int _iterations = 500, double _ro = 0.5, double _Q = 100, double _alpha = 1, double _beta = 5)
        {
            Vertices = new List<Vertice>();
            Edges = new List<List<Edge>>();

            kMax = _kMax; // number of trucks
            maxDistance = _maxDistance; // max distance by car
            maxDemand = _maxDemand; // sum of demand car can do
            iterations = _iterations;
            ro = _ro; // wspolczynnik odparowywania fermonu
            Q = _Q;
            alpha = _alpha;
            beta = _beta;
        }

        public List<Vertice> Vertices { get; set; }
        public List<List<Edge>> Edges { get; set; }

        public string GetParamsCSV()
        {
            // alpha, beta, ro, Q, maxDemand, maxDistance, iterations
            return alpha + "," + beta + "," + ro + "," + Q + "," + maxDemand + "," + maxDistance + "," + iterations;
        }
        public void RecalculateVertices()
        {
            for (var i = 0; i < Vertices.Count; i++)
            {
                Edges.Add(new List<Edge>());
                for (var j = 0; j < Vertices.Count; j++)
                {
                    var e = new Edge(GetWeight(i, j));
                    e.Fermone = 10;
                    Edges[i].Add(e);
                }
            }
        }

        private void UpdateAvailability(HashSet<int> UnvisitedVertices, HashSet<int> availableCities, int currentCity,
            double usedDemand, double maxDemand,
            double antDistanceTotal, double maxDistance)
        {
            foreach (var otherCity in UnvisitedVertices)
            {
                var distance1 = Edges[currentCity][otherCity].Weigth;
                var distance2 = Edges[otherCity][0].Weigth;
                var demand = Vertices[otherCity].Demand;
                if (maxDistance - (antDistanceTotal + distance1 + distance2) < 0 || usedDemand + demand > maxDemand)
                    availableCities.Remove(otherCity);
            }
        }

        public List<List<int>> FindDVRPSolution()
        {
            //constants


            var BestPath = new List<int>(); // best found path
            var bestSolution = double.MaxValue; // best found solution value

            for (var i = 0; i < iterations; i++) // iteration
            {
                var VisitedByAnts =
                    new List<List<int>>(); // list of solutions found in the iteration by all of ants, used to update fermone
                var AntDistancesTotal = new List<double>(); // distance travelled by all ants

                for (var k = 0; k < kMax; k++) // ant
                {
                    var visitedByAnt = new List<int>(); // list of vertices visited already by the ant
                    double antDistanceTotal = 0; // total used distance
                    double usedDemand = 0; // total used demand
                    var currentCity = 0; // city in which ant currently is

                    var UnvisitedVertices = new HashSet<int>(); // all cities to visit
                    var availableCities = new HashSet<int>(); // cities that are available for the ant to visit
                    visitedByAnt.Add(0); // we atart in depot

                    for (var j = 1; j < Vertices.Count; j++)
                        UnvisitedVertices.Add(j); // all cities are needed to be visited
                    foreach (var otherCity in UnvisitedVertices)
                        availableCities.Add(otherCity); // all cities are available to be visited
                    var first = true; // used so first city will be always random

                    while (true)
                    {
                        UpdateAvailability(UnvisitedVertices, availableCities, currentCity, usedDemand, maxDemand,
                            antDistanceTotal, maxDistance); // updating availability of cities


                        if (availableCities.Count == 0)
                        {
                            // ant goes to depot
                            if (antDistanceTotal == 0 && visitedByAnt.Last() == 0)
                                break;
                            usedDemand = antDistanceTotal = currentCity = 0; // ant is in depot, new "vehicle" starts
                            AntDistancesTotal.Add(antDistanceTotal + Edges[visitedByAnt.Last()][0].Weigth);
                            visitedByAnt.Add(0);
                            // ant starts again in depot
                            visitedByAnt.Add(0);
                            foreach (var otherCity in UnvisitedVertices) // all unvisited cities are available again
                                availableCities
                                    .Add(otherCity);
                            if (UnvisitedVertices.Count == 0) // if we visited all vertices, we end
                                break;
                            continue;
                        }

                        var CityProbabilities =
                            CalculateCityProbabilities(availableCities, currentCity, alpha, beta,
                                ref first); // calculating probability of going to a city

                        var chosenCity = ChooseCityWithProbability(CityProbabilities); // choosing city with probability

                        // ant goes to the chosen city
                        GoToCity(visitedByAnt, ref antDistanceTotal, ref usedDemand, ref currentCity, UnvisitedVertices,
                            availableCities, chosenCity);
                    }

                    VisitedByAnts.Add(visitedByAnt); // adding found path
                    if (CalculateUsage(visitedByAnt) < bestSolution) // checking if it's the best
                    {
                        bestSolution = CalculateUsage(visitedByAnt);
                        BestPath = visitedByAnt;
                    }
                }

                RecalculateFermone(kMax, VisitedByAnts, ro, Q, AntDistancesTotal); // recalculate fermone
            }



            var BestPathFull = GenerateVaildBestPath(BestPath); // separates one ant's work to show it as multiple vehicles

            return BestPathFull;
        }

        private List<List<int>> GenerateVaildBestPath(List<int> BestPath)
        {
            var BestPathFull = new List<List<int>>();
            var CurrentList = new List<int>();
            var firstList = true;
            foreach (var v in BestPath)
            {
                if (v == 0)
                {
                    if (firstList)
                    {
                        firstList = false;
                        CurrentList = new List<int>();
                    }
                    else
                    {
                        firstList = true;
                        CurrentList.Add(v);
                        BestPathFull.Add(CurrentList);
                        continue;
                    }
                }

                CurrentList.Add(v);
            }

            return BestPathFull;
        }

        private void GoToCity(List<int> visitedByAnt, ref double antDistanceTotal, ref double usedDemand, ref int currentCity, HashSet<int> UnvisitedVertices, HashSet<int> availableCities, int chosenCity)
        {
            antDistanceTotal += Edges[currentCity][chosenCity].Weigth;
            usedDemand += Vertices[chosenCity].Demand;
            UnvisitedVertices.Remove(chosenCity);
            availableCities.Remove(chosenCity);
            visitedByAnt.Add(chosenCity);
            currentCity = chosenCity;
        }

        private void RecalculateFermone(int kMax, List<List<int>> VisitedByAnts, double ro, double Q, List<double> AntDistancesTotal)
        {
            for (var k = 0; k < kMax; k++) // vaporise
            for (var j = 0; j < VisitedByAnts[k].Count - 1; j++)
            {
                var firstCity = VisitedByAnts[k][j];
                var secondCity = VisitedByAnts[k][j + 1];
                Edges[firstCity][secondCity].Fermone = Edges[secondCity][firstCity].Fermone *= 1 - ro;
            }

            for (var k = 0; k < kMax; k++) // add news
            for (var j = 0; j < VisitedByAnts[k].Count - 1; j++)
            {
                var firstCity = VisitedByAnts[k][j];
                var secondCity = VisitedByAnts[k][j + 1];
                Edges[firstCity][secondCity].Fermone =
                    Edges[secondCity][firstCity].Fermone += Q / AntDistancesTotal[k];
            }
        }

        private List<CityProbability> CalculateCityProbabilities(HashSet<int> availableCities, int currentCity,
            double alpha, double beta, ref bool first)
        {
            var CityProbabilities = new List<CityProbability>();
            foreach (var cityToGo in availableCities)
            {
                var e = Edges[currentCity][cityToGo];
                double probability;
                probability = Math.Pow(e.Fermone, alpha) * Math.Pow(1.0 / e.Weigth, beta);
                if (first) probability = 1.0 / availableCities.Count;
                CityProbabilities.Add(new CityProbability(cityToGo, probability));
            }

            var sumprob = CityProbabilities.Sum(x => x.Probability);
            foreach (var cp in CityProbabilities)
                cp.Probability /= sumprob;
            if (first) first = false;
            return CityProbabilities;
        }

        public double CalculateUsages(List<List<int>> list)
        {
            var fullPath = list[0];
            var usage = 0.0;
            for (var i = 0; i < fullPath.Count - 1; i++) usage += Edges[fullPath[i]][fullPath[i + 1]].Weigth;

            return usage;
        }

        public double CalculateUsage(List<int> list)
        {
            var usage = 0.0;
            for (var i = 0; i < list.Count - 1; i++) usage += Edges[list[i]][list[i + 1]].Weigth;

            return usage;
        }

        public double CalculateUsage(List<List<int>> list)
        {
            var fullPath = list.SelectMany(x => x).ToList();
            if (fullPath.Distinct().Count() != Vertices.Count)
                return -1;
            if (fullPath.Count(x => x != 0) != fullPath.Where(x => x != 0).Distinct().Count())
                return -1;
            if (fullPath.Count(x => x == 0) % 2 != 0)
                return -1;
            var usage = 0.0;
            for (var i = 0; i < fullPath.Count - 1; i++) usage += Edges[fullPath[i]][fullPath[i + 1]].Weigth;

            return usage;
        }

        private int ChooseCityWithProbability(List<CityProbability> cityProbabilities)
        {
            var converted = new List<CityProbability>(cityProbabilities.Count);
            var sum = 0.0;
            foreach (var item in cityProbabilities.Take(cityProbabilities.Count - 1))
            {
                sum += item.Probability;
                converted.Add(new CityProbability(item.City, sum));
            }

            converted.Add(new CityProbability(cityProbabilities.Last().City, 1.0));

            var probability = rnd.NextDouble();
            var selected = converted.SkipWhile(i => i.Probability < probability).First();
            return selected.City;
        }

        private double GetWeight(int i, int j)
        {
            var X = Vertices[i].X - Vertices[j].X;
            var Y = Vertices[i].Y - Vertices[j].Y;
            return Math.Sqrt(X * X + Y * Y);
        }
    }

    public class Vertice
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Demand { get; set; }
    }

    public class Edge
    {
        public Edge(double W)
        {
            Weigth = W;
        }

        public double Fermone { get; set; }

        public double Weigth { get; }
    }
}