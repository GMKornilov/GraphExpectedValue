using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using GraphExpectedValue.GraphWidgets;

namespace GraphExpectedValue.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Random random = new Random();
        private readonly List<Vertex> vertexes = new List<Vertex>();
        private readonly Dictionary<Tuple<Vertex, Vertex>, Edge> edges = new Dictionary<Tuple<Vertex, Vertex>, Edge>(); 
        private Vertex startVertex = null, endVertex = null;

        public MainWindow()
        {
            InitializeComponent();
            testCanvas.MouseLeftButtonDown += TestCanvasOnMouseLeftButtonDown;
        }

        private void TestCanvasOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Debug.WriteLine("Click!!!");
            // Debug.WriteLine(e.GetPosition(testCanvas).X + " " + e.GetPosition(testCanvas).Y);
            var point = e.GetPosition(testCanvas);
            if (vertexes.TrueForAll((Vertex v) => v.CheckIntersection(point)))
            {
                var vertex = new Vertex(point.X, point.Y, vertexes.Count + 1);
                vertexes.Add(vertex);
                testCanvas.Children.Add(vertex);
            }
        }


        private void AddEdgeButton_OnClick(object sender, RoutedEventArgs e)
        {
            /*
            if(vertexes.Count < 2)return;
            var from = vertexes[vertexes.Count - 1];
            var to = vertexes[vertexes.Count - 2];
            var edge = new Edge(from, to, random.NextDouble());
            edge.AddToCanvas(testCanvas);
            */
            if(vertexes.Count < 2)return;
            var edgePickWindow = new EdgePickWindow {TotalVertexes = vertexes.Count};
            if (edgePickWindow.ShowDialog() != true) return;
            // Debug.WriteLine($"{edgePickWindow.StartVertexNumber} {edgePickWindow.EndVertexNumber} {edgePickWindow.EdgeLength}");
            
            var startVertexNumber = edgePickWindow.StartVertexNumber - 1;
            var endVertexNumber = edgePickWindow.EndVertexNumber - 1;

            var startVertex = vertexes[startVertexNumber];
            var endVertex = vertexes[endVertexNumber];
            var edgeLength = edgePickWindow.EdgeLength;

            if (edges.TryGetValue(new Tuple<Vertex, Vertex>(startVertex, endVertex), out _))
            {
                MessageBox.Show(
                    "Such edge already exists",
                    "",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            var edge = new Edge(startVertex, endVertex, edgeLength);
            edge.AddToCanvas(testCanvas);

            // TODO: check if edge between this two vertexes already exists
            

            edges.Add(new Tuple<Vertex, Vertex>(startVertex, endVertex), edge);
        }

        private void RemoveEdgeButton_OnClick(object sender, RoutedEventArgs e)
        {
            Func<Tuple<int, int>, bool> checker = tuple =>
            {
                var (num1, num2) = tuple;
                var startVertex = vertexes[num1 - 1];
                var endVertex = vertexes[num2 - 1];
                return edges.TryGetValue(new Tuple<Vertex, Vertex>(startVertex, endVertex), out _);
            };
            var edgeChooseWindow = new EdgeChooseWindow(checker){TotalVertexes = vertexes.Count};
            if (edgeChooseWindow.ShowDialog() == true)
            {
                var chosenStartVertexNumber = edgeChooseWindow.ChosenStartVertex - 1;
                var chosenEndVertexNumber = edgeChooseWindow.ChosenEndVertex - 1;

                var chosenStartVertex = vertexes[chosenStartVertexNumber];
                var chosenEndVertex = vertexes[chosenEndVertexNumber];

                if (!edges.TryGetValue(new Tuple<Vertex, Vertex>(chosenStartVertex, chosenEndVertex), out var edge))
                {
                    Debug.WriteLine("??????????????");
                }

                edges.Remove(new Tuple<Vertex, Vertex>(chosenStartVertex, chosenEndVertex));
                edge.RemoveFromCanvas(testCanvas);
            }
        }

        private void RemoveVertexButton_OnClick(object sender, RoutedEventArgs e)
        {
            if(vertexes.Count == 0) return;
            var vertexPickWindow = new VertexChooseWindow()
            {
                Prompt = "Choose vertex to remove",
                TotalVertexes = vertexes.Count,
                ConfirmButtonText = "Remove vertex"
            };
            if (vertexPickWindow.ShowDialog() == true)
            {
                var chosenVertexNumber = vertexPickWindow.ChosenVertex - 1;
                var chosenVertex = vertexes[chosenVertexNumber];
                // Debug.WriteLine(chosenVertexNumber);
                testCanvas.Children.Remove(chosenVertex);
                
                // TODO: remove all edges connected with this vertex
                foreach (var key in edges.Keys.Where(item => item.Item1.Number == chosenVertex.Number || item.Item2.Number == chosenVertex.Number).ToList())
                {
                    var edge = edges[key];
                    edges.Remove(key);
                    edge.RemoveFromCanvas(testCanvas);
                }

                if (chosenVertex == startVertex)
                {
                    startVertex = null;
                }
                else if (chosenVertex == endVertex)
                {
                    endVertex = null;
                }

                vertexes.RemoveAt(chosenVertexNumber);
                for (var i = chosenVertexNumber; i < vertexes.Count; i++)
                {
                    vertexes[i].Number--;
                }
            }
        }

        private void StartVertexButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (vertexes.Count == 0) return;
            var vertexPickWindow = new VertexChooseWindow()
            {
                Prompt = "Choose start vertex",
                TotalVertexes = vertexes.Count,
                ConfirmButtonText = "Choose start vertex"
            };
            if (vertexPickWindow.ShowDialog() == true)
            {
                var chosenVertexNumber = vertexPickWindow.ChosenVertex - 1;
                var chosenVertex = vertexes[chosenVertexNumber];
                if (startVertex != null && startVertex.Number != chosenVertexNumber + 1)
                {
                    startVertex.VertexType = VertexType.PathVertex;
                    startVertex = null;
                }

                if (chosenVertex == endVertex)
                {
                    endVertex = null;
                }
                chosenVertex.VertexType = VertexType.StartVertex;
                startVertex = chosenVertex;
            }
        }

        private void EndVertexButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (vertexes.Count == 0) return;
            var vertexPickWindow = new VertexChooseWindow()
            {
                Prompt = "Choose end vertex",
                TotalVertexes = vertexes.Count,
                ConfirmButtonText = "Choose end vertex"
            };
            if (vertexPickWindow.ShowDialog() == true)
            {
                var chosenVertexNumber = vertexPickWindow.ChosenVertex - 1;
                var chosenVertex = vertexes[chosenVertexNumber];
                if (endVertex != null && endVertex.Number != chosenVertexNumber + 1)
                {
                    endVertex.VertexType = VertexType.PathVertex;
                    endVertex = null;
                }

                if (chosenVertex == startVertex)
                {
                    startVertex = null;
                }
                chosenVertex.VertexType = VertexType.EndVertex;
                endVertex = chosenVertex;
            }
        }
    }
}
