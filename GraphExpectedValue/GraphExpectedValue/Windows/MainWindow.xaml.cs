using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;
using GraphExpectedValue.GraphLogic;
using GraphExpectedValue.GraphWidgets;
using Microsoft.Win32;

namespace GraphExpectedValue.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Random random = new Random();
        private GraphMetadata graphMetadata = new GraphMetadata();
        private List<Vertex> vertexes = new List<Vertex>();
        private Dictionary<Tuple<Vertex, Vertex>, Edge> edges = new Dictionary<Tuple<Vertex, Vertex>, Edge>();
        private Vertex startVertex = null, endVertex = null;

        public MainWindow()
        {
            InitializeComponent();
            testCanvas.MouseLeftButtonDown += TestCanvasOnMouseLeftButtonDown;
        }

        private void TestCanvasOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var point = e.GetPosition(testCanvas);
            if (vertexes.TrueForAll((Vertex v) => v.CheckIntersection(point)))
            {
                var vertex = new Vertex(point.X, point.Y, vertexes.Count + 1);
                vertexes.Add(vertex);
                graphMetadata.VertexMetadatas.Add(vertex.Metadata);
                testCanvas.Children.Add(vertex);
            }
        }


        private void AddEdgeButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (vertexes.Count < 2) return;
            var edgePickWindow = new EdgePickWindow { TotalVertexes = vertexes.Count };
            if (edgePickWindow.ShowDialog() != true) return;

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
            edges.Add(new Tuple<Vertex, Vertex>(startVertex, endVertex), edge);
            graphMetadata.EdgeMetadatas.Add(edge.Metadata);
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
            var edgeChooseWindow = new EdgeChooseWindow(checker) { TotalVertexes = vertexes.Count };
            if (edgeChooseWindow.ShowDialog() != true) return;

            var chosenStartVertexNumber = edgeChooseWindow.ChosenStartVertex - 1;
            var chosenEndVertexNumber = edgeChooseWindow.ChosenEndVertex - 1;

            var chosenStartVertex = vertexes[chosenStartVertexNumber];
            var chosenEndVertex = vertexes[chosenEndVertexNumber];

            if (!edges.TryGetValue(new Tuple<Vertex, Vertex>(chosenStartVertex, chosenEndVertex), out var edge))
            {
                MessageBox.Show(
                    "There is no such edge in graph",
                    "",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
            edges.Remove(new Tuple<Vertex, Vertex>(chosenStartVertex, chosenEndVertex));
            graphMetadata.EdgeMetadatas.Remove(edge.Metadata);
            edge.RemoveFromCanvas(testCanvas);
        }

        private void RemoveVertexButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (vertexes.Count == 0) return;
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
                testCanvas.Children.Remove(chosenVertex);

                foreach (var key in edges.Keys.Where(item => item.Item1.Number == chosenVertex.Number || item.Item2.Number == chosenVertex.Number).ToList())
                {
                    var edge = edges[key];
                    edges.Remove(key);
                    graphMetadata.EdgeMetadatas.Remove(edge.Metadata);
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

                graphMetadata.VertexMetadatas.Remove(chosenVertex.Metadata);
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
                    startVertex.PropertyChanged -= UpdateStartVertexNumber;
                    startVertex.VertexType = VertexType.PathVertex;
                    startVertex = null;
                }

                if (chosenVertex == endVertex)
                {
                    endVertex.PropertyChanged -= UpdateEndVertexNumber;
                    endVertex.VertexType = VertexType.PathVertex;
                    endVertex = null;
                }

                chosenVertex.PropertyChanged += UpdateStartVertexNumber;
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
                    endVertex.PropertyChanged -= UpdateEndVertexNumber;
                    endVertex.VertexType = VertexType.PathVertex;
                    endVertex = null;
                }

                if (chosenVertex == startVertex)
                {
                    startVertex.PropertyChanged -= UpdateStartVertexNumber;
                    startVertex.VertexType = VertexType.PathVertex;
                    startVertex = null;
                }

                chosenVertex.PropertyChanged += UpdateEndVertexNumber;
                chosenVertex.VertexType = VertexType.EndVertex;
                endVertex = chosenVertex;
            }
        }

        private void SafeGraphButton_OnClick(object sender, RoutedEventArgs e)
        {
            var safeFileDialog = new SaveFileDialog()
            {
                Filter = "XML file (*.xml)|*.xml"
            };
            if (safeFileDialog.ShowDialog() != true || string.IsNullOrEmpty(safeFileDialog.FileName)) return;
            try
            {
                var serializer = new XmlSerializer(typeof(GraphMetadata));
                using (var stream = new FileStream(safeFileDialog.FileName, FileMode.OpenOrCreate))
                {
                    serializer.Serialize(stream, graphMetadata);
                }
            }
            catch (IOException)
            {
                MessageBox.Show(
                    "Error while writing to file",
                    "",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    "Not enough rights to write in this path. Try running program with admin rights.",
                    "",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            catch (Exception err)
            {
                MessageBox.Show(
                    $"Unknown error happened:{err.Message}",
                    "",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void OpenGraphButton_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void UpdateStartVertexNumber(object sender, PropertyChangedEventArgs e)
        {
            if (sender is Vertex vertex)
            {
                graphMetadata.StartVertexNumber = vertex.Number;
            }
        }

        private void UpdateEndVertexNumber(object sender, PropertyChangedEventArgs e)
        {
            if (sender is Vertex vertex)
            {
                graphMetadata.EndVertexNumber = vertex.Number;
            }
        }

        private void LoadGraph(GraphMetadata metadata)
        {
            // TODO: remove all edges and vertexes
            metadata.VertexMetadatas.Sort(((metadata1, metadata2) => metadata1.Number.CompareTo(metadata2.Number)));
            graphMetadata = metadata;
            vertexes = new List<Vertex>(metadata.VertexMetadatas.Count);
            edges = new Dictionary<Tuple<Vertex, Vertex>, Edge>();
            foreach (var vertexData in metadata.VertexMetadatas)
            {
                var vertex = new Vertex(vertexData);
                vertexes.Add(vertex);
                // TODO: code for adding vertex to canvas
            }

            foreach (var edgeData in metadata.EdgeMetadatas)
            {
                var edgeStartVertex = vertexes[edgeData.StartVertexNumber - 1];
                var edgeEndVertex = vertexes[edgeData.EndVertexNumber - 1];
                var edge = new Edge(
                    edgeStartVertex,
                    edgeEndVertex,
                    edgeData
                );
                edges.Add(new Tuple<Vertex, Vertex>(edgeStartVertex, edgeEndVertex), edge);
                //TODO: code for adding edge to canvas
            }
        }

    }
}
