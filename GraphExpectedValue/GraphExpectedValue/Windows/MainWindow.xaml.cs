using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;
using GraphExpectedValue.GraphLogic;
using GraphExpectedValue.GraphWidgets;
using GraphExpectedValue.Utility;
using GraphExpectedValue.Utility.ConcreteStrategies;
using Microsoft.Win32;

namespace GraphExpectedValue.Windows
{
    public class ActionCommand : ICommand
    {
        private readonly Action _action;

        public ActionCommand(Action action)
        {
            _action = action;
        }

        public void Execute(object parameter)
        {
            _action();
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public readonly ActionCommand saveActionCommand;
        public readonly ActionCommand openActionCommand;
        private GraphMetadata graphMetadata = new GraphMetadata();
        private List<Vertex> vertexes = new List<Vertex>();
        private Dictionary<Tuple<Vertex, Vertex>, Edge> edges = new Dictionary<Tuple<Vertex, Vertex>, Edge>();
        private Vertex startVertex = null, endVertex = null;
        private bool Working = false;

        public MainWindow()
        {
            InitializeComponent();
            testCanvas.MouseLeftButtonDown += TestCanvasOnMouseLeftButtonDown;
            saveActionCommand = new ActionCommand(
                () => SafeGraphButton_OnClick(this, new RoutedEventArgs())
            );
            openActionCommand = new ActionCommand(
                () => OpenGraphButton_OnClick(this, new RoutedEventArgs())
            );
            var openKeyBinding = new KeyBinding(
                openActionCommand,
                Key.O,
                ModifierKeys.Control
            );
            var saveKeyBinding = new KeyBinding(
                saveActionCommand,
                Key.S,
                ModifierKeys.Control
            );
            InputBindings.Add(openKeyBinding);
            InputBindings.Add(saveKeyBinding);

            buttonPanel.Visibility = Visibility.Hidden;
            savePanel.Visibility = Visibility.Hidden;
            testCanvas.Visibility = Visibility.Hidden;
        }
        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            cmbSolution.ItemsSource = new SolutionStrategy[]
            {
                new GaussEliminationSolutionStrategy(),
                new InverseMatrixSolutionStrategy()
            };
            cmbSolution.SelectedIndex = 0;
            DropUpComboBox(cmbSolution);

            cmbInverse.ItemsSource = new InverseStrategy[]
            {
                new GaussEliminationInverseStrategy(),
                new BlockInverseStrategy()
            };
            cmbInverse.SelectedIndex = 0;
            DropUpComboBox(cmbInverse);

            cmbMult.ItemsSource = new MultiplyStrategy[]
            {
                new SimpleMultiplyStrategy(),
                new StrassenMultiplyStrategy()
            };
            cmbMult.SelectedIndex = 0;
            DropUpComboBox(cmbMult);
        }

        private void DropUpComboBox(ComboBox comboBox)
        {
            var ct = comboBox.Template;
            var popup = ct.FindName("PART_Popup", comboBox) as Popup;

            if (popup != null)
            {
                popup.Placement = PlacementMode.Top;
            }
        }

        private void TestCanvasOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var point = e.GetPosition(testCanvas);
            if (
                point.X - Vertex.Size / 2.0 < 0 ||
                point.X + Vertex.Size / 2.0 > testCanvas.ActualWidth ||
                point.Y - Vertex.Size / 2.0 < 0 ||
                point.Y + Vertex.Size / 2.0 > testCanvas.ActualHeight
            ) return;
            if (!vertexes.TrueForAll(v => v.CheckIntersection(point))) return;
            var vertex = new Vertex(point.X, point.Y, vertexes.Count + 1);
            vertexes.Add(vertex);
            graphMetadata.VertexMetadatas.Add(vertex.Metadata);
            testCanvas.Children.Add(vertex);
        }

        private void AddEdgeButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (vertexes.Count < 2) return;
            var edgePickWindow = new EdgePickWindow { TotalVertexes = vertexes.Count };
            if (edgePickWindow.ShowDialog() != true) return;

            var startVertexNumber = edgePickWindow.StartVertexNumber - 1;
            var endVertexNumber = edgePickWindow.EndVertexNumber - 1;

            var edgeStartVertex = vertexes[startVertexNumber];
            var edgeEndVertex = vertexes[endVertexNumber];
            var edgeLength = edgePickWindow.EdgeLength;

            if (edges.TryGetValue(new Tuple<Vertex, Vertex>(edgeStartVertex, edgeEndVertex), out _))
            {
                MessageBox.Show(
                    "Such edge already exists",
                    "",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            var edge = new Edge(edgeStartVertex, edgeEndVertex, edgeLength)
            {
                Backed = !graphMetadata.IsOriented
            };
            edge.UpdateEdge();
            AddEdge(edge, edgeStartVertex, edgeEndVertex);
        }

        private void RemoveEdgeButton_OnClick(object sender, RoutedEventArgs e)
        {
            Func<Tuple<int, int>, bool> checker = tuple =>
            {
                var (num1, num2) = tuple;
                var startEdgeVertex = vertexes[num1 - 1];
                var endEdgeVertex = vertexes[num2 - 1];
                return edges.TryGetValue(new Tuple<Vertex, Vertex>(startEdgeVertex, endEdgeVertex), out _);
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
            RemoveEdge(chosenStartVertex, chosenEndVertex);
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
            if (vertexPickWindow.ShowDialog() != true) return;
            var chosenVertexNumber = vertexPickWindow.ChosenVertex - 1;
            var chosenVertex = vertexes[chosenVertexNumber];
            testCanvas.Children.Remove(chosenVertex);

            foreach (var (fromVertex, toVertex) in edges.Keys.Where(item => item.Item1.Number == chosenVertex.Number || item.Item2.Number == chosenVertex.Number).ToList())
            {
                RemoveEdge(fromVertex, toVertex);
            }

            if (chosenVertex == startVertex)
            {
                startVertex = null;
                graphMetadata.StartVertexNumber = -1;
            }
            else if (chosenVertex == endVertex)
            {
                endVertex = null;
                graphMetadata.EndVertexNumber = -1;
            }

            graphMetadata.VertexMetadatas.Remove(chosenVertex.Metadata);
            vertexes.RemoveAt(chosenVertexNumber);

            for (var i = chosenVertexNumber; i < vertexes.Count; i++)
            {
                vertexes[i].Number--;
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
            if (vertexPickWindow.ShowDialog() != true) return;
            var chosenVertexNumber = vertexPickWindow.ChosenVertex - 1;
            var chosenVertex = vertexes[chosenVertexNumber];
            SetStartVertex(chosenVertex);
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
                SetEndVertex(chosenVertex);
            }
        }

        private void SafeGraphButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!Working)
            {
                return;
            }
            var safeFileDialog = new SaveFileDialog()
            {
                Filter = "XML file (*.xml)|*.xml"
            };
            if (safeFileDialog.ShowDialog() != true || string.IsNullOrEmpty(safeFileDialog.FileName)) return;
            try
            {
                var serializer = new XmlSerializer(typeof(GraphMetadata));
                // if (File.Exists(safeFileDialog.FileName))
                // {
                //     File.Delete(safeFileDialog.FileName);
                // }
                using (var stream = new FileStream(safeFileDialog.FileName, FileMode.Create))
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
            GraphMetadata metadata;
            var openFileDialog = new OpenFileDialog()
            {
                Filter = "XML file (*.xml)|*.xml"
            };
            if (openFileDialog.ShowDialog() != true || string.IsNullOrEmpty(openFileDialog.FileName)) return;
            try
            {
                var serializer = new XmlSerializer(typeof(GraphMetadata));
                using (var stream = new FileStream(openFileDialog.FileName, FileMode.OpenOrCreate))
                {
                    metadata = (GraphMetadata)serializer.Deserialize(stream);
                }

                if (!CheckMetadata(metadata))
                {
                    Debug.WriteLine("WRONG");
                    throw new XmlException();
                }

                Working = true;
                savePanel.Visibility = Visibility.Visible;
                buttonPanel.Visibility = Visibility.Visible;
                testCanvas.Visibility = Visibility.Visible;

                LoadGraph(metadata);
            }
            catch (IOException)
            {
                MessageBox.Show(
                    "Error while reading file",
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
            catch (XmlException)
            {
                MessageBox.Show(
                    "Incorrect xml was written in file",
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

        private void SetStartVertex(Vertex vertex)
        {
            if (startVertex != null && !startVertex.Equals(vertex))
            {
                startVertex.PropertyChanged -= UpdateStartVertexNumber;
                startVertex.VertexType = VertexType.PathVertex;
                startVertex = null;
            }

            if (vertex == endVertex)
            {
                endVertex.PropertyChanged -= UpdateEndVertexNumber;
                endVertex.VertexType = VertexType.PathVertex;
                endVertex = null;
                graphMetadata.EndVertexNumber = -1;
            }

            vertex.PropertyChanged += UpdateStartVertexNumber;
            vertex.VertexType = VertexType.StartVertex;
            startVertex = vertex;
        }

        private void SetEndVertex(Vertex vertex)
        {
            if (endVertex != null && !endVertex.Equals(vertex))
            {
                endVertex.PropertyChanged -= UpdateEndVertexNumber;
                endVertex.VertexType = VertexType.PathVertex;
                endVertex = null;
            }

            if (vertex == startVertex)
            {
                startVertex.PropertyChanged -= UpdateStartVertexNumber;
                startVertex.VertexType = VertexType.PathVertex;
                startVertex = null;
                graphMetadata.StartVertexNumber = -1;
            }

            vertex.PropertyChanged += UpdateEndVertexNumber;
            vertex.VertexType = VertexType.EndVertex;
            endVertex = vertex;
        }

        private void AddEdge(Edge edge, Vertex edgeStartVertex, Vertex edgeEndVertex, bool addToMetadata = true)
        {
            // if we have oriented graph and trying to add back edge,
            // we need to draw it as a back edge
            if (edges.TryGetValue(new Tuple<Vertex, Vertex>(edgeEndVertex, edgeStartVertex), out var backEdge))
            {
                if (graphMetadata.IsOriented)
                {
                    backEdge.Curved = true;
                    backEdge.UpdateEdge();
                    edge.Curved = true;
                    edge.UpdateEdge();
                }
                else
                {
                    MessageBox.Show(
                        "Such edge already exists",
                        "",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }
            }

            edge.AddToCanvas(testCanvas);
            edges.Add(new Tuple<Vertex, Vertex>(edgeStartVertex, edgeEndVertex), edge);
            if (addToMetadata)
            {
                graphMetadata.EdgeMetadatas.Add(edge.Metadata);
            }
        }

        private void RemoveEdge(Vertex chosenStartVertex, Vertex chosenEndVertex)
        {
            // if we are trying to remove unexisting edge,
            // show error message
            if (!edges.TryGetValue(new Tuple<Vertex, Vertex>(chosenStartVertex, chosenEndVertex), out var edge))
            {
                if (graphMetadata.IsOriented || !edges.TryGetValue(new Tuple<Vertex, Vertex>(chosenEndVertex, chosenStartVertex), out _))
                {
                    MessageBox.Show(
                        "There is no such edge in graph",
                        "",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }
            }
            // if we have oriented graph and trying to delete one of back edges,
            // set another back edge as normal
            if (graphMetadata.IsOriented && edges.TryGetValue(new Tuple<Vertex, Vertex>(chosenEndVertex, chosenStartVertex), out var backEdge))
            {
                backEdge.Curved = false;
                backEdge.UpdateEdge();
            }
            edges.Remove(new Tuple<Vertex, Vertex>(chosenStartVertex, chosenEndVertex));
            graphMetadata.EdgeMetadatas.Remove(edge.Metadata);
            edge.RemoveFromCanvas(testCanvas);
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

        private static bool CheckMetadata(GraphMetadata metadata)
        {
            metadata.VertexMetadatas.Sort(((metadata1, metadata2) => metadata1.Number.CompareTo(metadata2.Number)));
            if (metadata.VertexMetadatas.Where((t, i) => t.Number != i + 1).Any())
            {
                // vertexes numbers should be from 1 to n
                return false;
            }

            var testEdgeDict = new Dictionary<Tuple<int, int>, EdgeMetadata>();
            foreach (var edgeData in metadata.EdgeMetadatas)
            {
                if (testEdgeDict.ContainsKey(new Tuple<int, int>(edgeData.StartVertexNumber, edgeData.EndVertexNumber)))
                {
                    // two same edges
                    return false;
                }
                testEdgeDict.Add(
                    new Tuple<int, int>(edgeData.StartVertexNumber, edgeData.EndVertexNumber),
                    edgeData
                );
            }

            if (metadata.StartVertexNumber != -1 && (metadata.StartVertexNumber < 1 ||
                                                     metadata.StartVertexNumber > metadata.VertexMetadatas.Count))
            {
                return false;
            }

            if (metadata.EndVertexNumber != -1 &&
                (metadata.EndVertexNumber < 1 || metadata.EndVertexNumber > metadata.VertexMetadatas.Count))
            {
                return false;
            }

            return true;
        }
        private void LoadGraph(GraphMetadata metadata)
        {
            ClearGraph();

            metadata.VertexMetadatas.Sort(((metadata1, metadata2) => metadata1.Number.CompareTo(metadata2.Number)));
            graphMetadata = metadata;
            vertexes = new List<Vertex>(metadata.VertexMetadatas.Count);
            edges = new Dictionary<Tuple<Vertex, Vertex>, Edge>();
            foreach (var vertexData in metadata.VertexMetadatas)
            {
                var vertex = new Vertex(vertexData);
                vertexes.Add(vertex);
                testCanvas.Children.Add(vertex);
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
                edge.Backed = !graphMetadata.IsOriented;
                edge.UpdateEdge();
                AddEdge(edge, edgeStartVertex, edgeEndVertex, false);
            }

            if (metadata.StartVertexNumber != -1)
            {
                SetStartVertex(vertexes[metadata.StartVertexNumber - 1]);
            }

            if (metadata.EndVertexNumber != -1)
            {
                SetEndVertex(vertexes[metadata.EndVertexNumber - 1]);
            }
            GraphMetadata.solutionStrategy = cmbSolution.SelectedItem as SolutionStrategy;
            Matrix.inverseStrategy = cmbInverse.SelectedItem as InverseStrategy;
            Matrix.multiplyStrategy = cmbMult.SelectedItem as MultiplyStrategy;
        }

        private void ClearGraph()
        {
            foreach (var vertex in vertexes)
            {
                testCanvas.Children.Remove(vertex);
            }

            foreach (var edgePair in edges)
            {
                var edge = edgePair.Value;
                edge.RemoveFromCanvas(testCanvas);
            }

            startVertex = null;
            endVertex = null;
        }

        private void ItemOriented_OnClick(object sender, RoutedEventArgs e)
        {
            CreateGraph(true);
        }

        private void ItemUnoriented_OnClick(object sender, RoutedEventArgs e)
        {
            CreateGraph(false);
        }

        private void CreateGraph(bool isOriented)
        {
            ClearGraph();
            graphMetadata = new GraphMetadata()
            {
                IsOriented = isOriented
            };
            vertexes = new List<Vertex>();
            edges = new Dictionary<Tuple<Vertex, Vertex>, Edge>();
            startVertex = null;
            endVertex = null;

            Working = true;
            savePanel.Visibility = Visibility.Visible;
            buttonPanel.Visibility = Visibility.Visible;
            testCanvas.Visibility = Visibility.Visible;
        }

        private void CalculateButton_OnClick(object sender, RoutedEventArgs e)
        {
            var watcher = Stopwatch.StartNew();
            var res = graphMetadata.Solve();
            watcher.Stop();
            var builder = new StringBuilder();
            for (var i = 0; i < graphMetadata.EndVertexNumber - 1; i++)
            {
                builder.Append($"T_{i + 1}:{res[i]}\n");
            }

            for (var i = graphMetadata.EndVertexNumber; i <= res.Length; i++)
            {
                builder.Append($"T_{i + 1}:{res[i - 1]}\n");
            }
            builder.Append($"Done in {watcher.Elapsed.ToString()}");
            MessageBox.Show(
                builder.ToString(),
                "",
                MessageBoxButton.OK,
                MessageBoxImage.None
            );
        }

        private void CmbSolution_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var strategy = e.AddedItems[0] as SolutionStrategy;
            Debug.WriteLine(strategy.ToString());
            GraphMetadata.solutionStrategy = strategy;
        }

        private void CmbInverse_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var strategy = e.AddedItems[0] as InverseStrategy;
            Matrix.inverseStrategy = strategy;
        }

        private void CmbMult_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var strategy = e.AddedItems[0] as MultiplyStrategy;
            Matrix.multiplyStrategy = strategy;
        }
    }
}
