using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;
using GraphExpectedValue.GraphLogic;
using GraphExpectedValue.GraphWidgets;
using GraphExpectedValue.Utility;
using GraphExpectedValue.Utility.ConcreteAlgorithms;
using Microsoft.Win32;
using MathNet.Symbolics;
using GraphExpectedValue.Utility.ConcreteGraphIO;

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
    //TODO: change counting of graph data
    //TODO: change reading\writing graph
    public partial class MainWindow : Window
    {
        private GraphMetadata _graphMetadata = new GraphMetadata();

        private List<Vertex> _vertexes = new List<Vertex>();
        private List<int> _degrees = new List<int>();

        private Vertex clickedVertex = null;

        private Dictionary<Tuple<Vertex, Vertex>, Edge> _edges = new Dictionary<Tuple<Vertex, Vertex>, Edge>();

        private bool _working;

        public MainWindow()
        {
            InitializeComponent();
            var saveActionCommand = new ActionCommand(
                () => SafeGraphButton_OnClick(this, new RoutedEventArgs())
            );
            var openActionCommand = new ActionCommand(
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
            mainCanvas.Visibility = Visibility.Hidden;
            canvasBorder.Visibility = Visibility.Hidden;
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            cmbSolution.ItemsSource = new SolutionAlgorithm[]
            {
                new GaussEliminationSolutionAlgorithm(),
                new InverseMatrixSolutionAlgorithm()
            };
            cmbSolution.SelectedIndex = 0;
            DropUpComboBox(cmbSolution);

            cmbInverse.ItemsSource = new InverseAlgorithm[]
            {
                new GaussEliminationInverseAlgorithm(),
                new BlockInverseAlgorithm()
            };
            cmbInverse.SelectedIndex = 0;
            DropUpComboBox(cmbInverse);

            cmbMult.ItemsSource = new MultiplyAlgorithm[]
            {
                new SimpleMultiplyAlgorithm(),
                new StrassenMultiplyAlgorithm()
            };
            cmbMult.SelectedIndex = 0;
            DropUpComboBox(cmbMult);
        }

        private void DropUpComboBox(ComboBox comboBox)
        {
            var ct = comboBox.Template;

            if (ct.FindName("PART_Popup", comboBox) is Popup popup)
            {
                popup.Placement = PlacementMode.Top;
            }
        }

        private void MainCanvasOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var point = e.GetPosition(mainCanvas);
            if (
                point.X - Vertex.Size / 2.0 < 0 ||
                point.X + Vertex.Size / 2.0 > mainCanvas.ActualWidth ||
                point.Y - Vertex.Size / 2.0 < 0 ||
                point.Y + Vertex.Size / 2.0 > mainCanvas.ActualHeight
            ) return;
            if (!_vertexes.TrueForAll(v => v.CheckIntersection(point))) return;
            var vertexMetadata = new VertexMetadata(_vertexes.Count + 1, VertexType.PathVertex, point);
            AddVertex(vertexMetadata, true);
        }

        private void AddEdgeButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_vertexes.Count < 2) return;
            Func<int, int, bool> checker = (startVertexNumber, endVertexNumber) =>
            {
                if (startVertexNumber == endVertexNumber)
                {
                    MessageBox.Show("Can\'t create loop edges", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                startVertexNumber--;
                endVertexNumber--;
                var edgeStartVertex = _vertexes[startVertexNumber];
                var edgeEndVertex = _vertexes[endVertexNumber];
                if (_edges.TryGetValue(new Tuple<Vertex, Vertex>(edgeStartVertex, edgeEndVertex), out _)
                || (!_graphMetadata.IsOriented && _edges.TryGetValue(new Tuple<Vertex, Vertex>(edgeEndVertex, edgeStartVertex), out _)))
                {
                    MessageBox.Show(
                        "Such edge already exists",
                        "",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return false;
                }

                return true;
            };
            var edgePickWindow = new EdgePickWindow(checker, _graphMetadata.CustomProbabilities) { TotalVertexes = _vertexes.Count };
            if (edgePickWindow.ShowDialog() != true) return;

            var chosenStartVertexNumber = edgePickWindow.StartVertexNumber - 1;
            var chosenEndVertexNumber = edgePickWindow.EndVertexNumber - 1;

            var chosenEdgeStartVertex = _vertexes[chosenStartVertexNumber];
            var chosenEdgeEndVertex = _vertexes[chosenEndVertexNumber];
            var edgeLengthExpr = edgePickWindow.LengthExpression;

            var edge = new Edge(chosenEdgeStartVertex, chosenEdgeEndVertex, edgeLengthExpr)
            {
                Backed = !_graphMetadata.IsOriented
            };
            edge.UpdateEdge();
            AddEdge(edge, chosenEdgeStartVertex, chosenEdgeEndVertex);
        }

        private void EditEdgeButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_edges.Count == 0) return;
            Func<int, int, bool> checker = (startVertexNumber, endVertexNumber) =>
            {
                if (startVertexNumber == endVertexNumber)
                {
                    MessageBox.Show(
                        "There are no loop edges in graph",
                        "",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return false;
                }
                startVertexNumber--;
                endVertexNumber--;

                var edgeStartVertex = _vertexes[startVertexNumber];
                var edgeEndVertex = _vertexes[endVertexNumber];

                var edgeTuple = new Tuple<Vertex, Vertex>(edgeStartVertex, edgeEndVertex);
                var backEdgeTuple = new Tuple<Vertex, Vertex>(edgeEndVertex, edgeStartVertex);
                if (!_edges.TryGetValue(edgeTuple, out _))
                {
                    if (!_graphMetadata.IsOriented && !_edges.TryGetValue(backEdgeTuple, out _))
                    {
                        MessageBox.Show(
                            "There is no such edge in graph",
                            "",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        return false;
                    }
                }

                return true;
            };
            var edgePickWindow = new EdgePickWindow(checker, _graphMetadata.CustomProbabilities)
            {
                TotalVertexes = _vertexes.Count,
                Title = "Edit edge",
                EndButton = { Content = "Edit edge" }
            };
            if (edgePickWindow.ShowDialog() != true) return;

            var chosenStartVertexNumber = edgePickWindow.StartVertexNumber - 1;
            var chosenEndVertexNumber = edgePickWindow.EndVertexNumber - 1;

            var chosenEdgeStartVertex = _vertexes[chosenStartVertexNumber];
            var chosenEdgeEndVertex = _vertexes[chosenEndVertexNumber];
            var edgeLengthExpr = edgePickWindow.LengthExpression;

            if (!_edges.TryGetValue(new Tuple<Vertex, Vertex>(chosenEdgeStartVertex, chosenEdgeEndVertex), out var edge))
            {
                edge = _edges[new Tuple<Vertex, Vertex>(chosenEdgeEndVertex, chosenEdgeStartVertex)];
            }

            edge.LengthExpression = edgeLengthExpr;
            if (_graphMetadata.CustomProbabilities)
            {
                var edgeProbaExpr = edgePickWindow.ProbabilityExpression;
                edge.ProbabilityExpression = edgeProbaExpr;
            }
            edge.UpdateEdge();
        }

        private void RemoveEdgeButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_edges.Count == 0) return;
            Func<Tuple<int, int>, bool> checker = tuple =>
            {
                var (num1, num2) = tuple;
                var startEdgeVertex = _vertexes[num1 - 1];
                var endEdgeVertex = _vertexes[num2 - 1];
                var orientedCheck = _edges.TryGetValue(new Tuple<Vertex, Vertex>(startEdgeVertex, endEdgeVertex), out _);
                var unorientedCheck = !_graphMetadata.IsOriented &&
                                      _edges.TryGetValue(
                                          new Tuple<Vertex, Vertex>(startEdgeVertex, endEdgeVertex),
                                          out _
                                        );
                return orientedCheck || unorientedCheck;
            };
            var edgeChooseWindow = new EdgeChooseWindow(checker) { TotalVertexes = _vertexes.Count };
            if (edgeChooseWindow.ShowDialog() != true) return;

            var chosenStartVertexNumber = edgeChooseWindow.ChosenStartVertex - 1;
            var chosenEndVertexNumber = edgeChooseWindow.ChosenEndVertex - 1;

            var chosenStartVertex = _vertexes[chosenStartVertexNumber];
            var chosenEndVertex = _vertexes[chosenEndVertexNumber];

            if (!_edges.TryGetValue(new Tuple<Vertex, Vertex>(chosenStartVertex, chosenEndVertex), out var edge) && _graphMetadata.IsOriented)
            {
                MessageBox.Show(
                    "There is no such edge in graph",
                    "",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            if (!_edges.TryGetValue(new Tuple<Vertex, Vertex>(chosenEndVertex, chosenStartVertex), out _) &&
                !_graphMetadata.IsOriented)
            {
                MessageBox.Show(
                    "There is no such edge in graph",
                    "",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }
            RemoveEdge(chosenStartVertex, chosenEndVertex);
        }

        private void RemoveVertexButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_vertexes.Count == 0) return;
            var vertexPickWindow = new VertexChooseWindow()
            {
                Prompt = "Choose vertex to remove",
                TotalVertexes = _vertexes.Count,
                ConfirmButtonText = "Remove vertex"
            };
            if (vertexPickWindow.ShowDialog() != true) return;
            var chosenVertexNumber = vertexPickWindow.ChosenVertex - 1;
            var chosenVertex = _vertexes[chosenVertexNumber];
            RemoveVertex(chosenVertex);
        }

        private void AddEndVertexButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_vertexes.Count == 0) return;
            var vertexPickWindow = new VertexChooseWindow()
            {
                Prompt = "Add end vertex",
                TotalVertexes = _vertexes.Count,
                ConfirmButtonText = "Add end vertex"
            };
            Func<int, bool> checker = vertexNumber =>
            {
                var vertex = _vertexes[vertexNumber - 1];
                if (vertex.VertexType == VertexType.EndVertex)
                {
                    MessageBox.Show(
                        "This vertex is already ending",
                        "",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return false;
                }

                return true;
            };
            vertexPickWindow.AddChecker(checker);
            if (vertexPickWindow.ShowDialog() == true)
            {
                var chosenVertexNumber = vertexPickWindow.ChosenVertex - 1;
                var chosenVertex = _vertexes[chosenVertexNumber];
                chosenVertex.VertexType = VertexType.EndVertex;
            }
        }

        private void RemoveEndVertexButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_vertexes.Count == 0) return;
            var vertexPickWindow = new VertexChooseWindow()
            {
                Prompt = "Remove end vertex",
                TotalVertexes = _vertexes.Count,
                ConfirmButtonText = "Remove end vertex"
            };
            Func<int, bool> checker = vertexNumber =>
            {
                var vertex = _vertexes[vertexNumber - 1];
                if (vertex.VertexType != VertexType.EndVertex)
                {
                    MessageBox.Show(
                        "This vertex isn't ending",
                        "",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return false;
                }

                return true;
            };
            vertexPickWindow.AddChecker(checker);
            if (vertexPickWindow.ShowDialog() == true)
            {
                var chosenVertexNumber = vertexPickWindow.ChosenVertex - 1;
                var chosenVertex = _vertexes[chosenVertexNumber];
                chosenVertex.VertexType = VertexType.PathVertex;
            }
        }

        private void SafeGraphButton_OnClick(object sender, RoutedEventArgs e)
        {
            SaveGraph(new GraphBinaryIO(), "Gr file (*.gr)|*.gr");
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            double width, height;
            if (mainCanvas.Visibility == Visibility.Hidden)
            {
                width = mainCanvas.RenderSize.Width;
                height = mainCanvas.RenderSize.Height;
            }
            else
            {
                width = mainCanvas.Width;
                height = mainCanvas.Height;
            }
            OpenGraph(
                new GraphMatrixIO(width, height),
                "All files|*"
            );
        }

        private void OpenGraphButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenGraph(new GraphBinaryIO(), "Gr file (*.gr)|*.gr");
        }

        private void OpenGraph(GraphReader reader, string filter)
        {
            var openFileDialog = new OpenFileDialog()
            {
                Filter = filter
            };
            if (openFileDialog.ShowDialog() != true || string.IsNullOrEmpty(openFileDialog.FileName)) return;
            try
            {
                var metadata = reader.ReadGraph(File.Open(openFileDialog.FileName, FileMode.OpenOrCreate));

                if (!CheckMetadata(metadata))
                {
                    throw new ArgumentException();
                }

                _working = true;
                savePanel.Visibility = Visibility.Visible;
                buttonPanel.Visibility = Visibility.Visible;
                mainCanvas.Visibility = Visibility.Visible;
                canvasBorder.Visibility = Visibility.Visible;

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
                    "Not enough rights to read in this path. Try running program with admin rights.",
                    "",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(
                    "Incorrect graph was written in file",
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

        private void SaveGraph(GraphWriter writer, string filter)
        {
            if (!_working)
            {
                return;
            }
            var safeFileDialog = new SaveFileDialog()
            {
                Filter = filter
            };
            if (safeFileDialog.ShowDialog() != true || string.IsNullOrEmpty(safeFileDialog.FileName)) return;
            try
            {
                writer.WriteGraph(_graphMetadata, File.OpenWrite(safeFileDialog.FileName));
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

        private void VertexDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is Vertex vertex))
            {
                return;
            }

            var connectedVertexes = (
                from edgeTuple in _edges
                where edgeTuple.Value.Metadata.StartVertexNumber == vertex.Number
                select new Tuple<int, bool>(edgeTuple.Value.Metadata.EndVertexNumber, false)
            ).ToList();
            if (!_graphMetadata.IsOriented)
            {
                var backConnectedVertexes = (
                    from edgeTuple in _edges
                    where edgeTuple.Value.Metadata.EndVertexNumber == vertex.Number
                    select new Tuple<int, bool>(edgeTuple.Value.Metadata.StartVertexNumber, true)
                );
                connectedVertexes.AddRange(backConnectedVertexes);
            }

            var probaInputWindow = new ProbaInputWindow(connectedVertexes);
            if (probaInputWindow.ShowDialog() != true) return;
            foreach (var (vertexNumber, proba, isBacked) in probaInputWindow.probas)
            {
                var edgeVertex = _vertexes.Find(x => x.Number == vertexNumber);
                Edge edge;
                if (isBacked)
                {
                    edge = _edges[new Tuple<Vertex, Vertex>(edgeVertex, vertex)];
                    edge.BackProbabilityExpression = proba;
                }
                else
                {
                    edge = _edges[new Tuple<Vertex, Vertex>(vertex, edgeVertex)];
                    edge.ProbabilityExpression = proba;
                }

            }
        }

        private void VertexClicked(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is Vertex vertex))
            {
                return;
            }
            if (clickedVertex == null)
            {
                clickedVertex = vertex;
                return;
            }
            if (clickedVertex == vertex)
            {
                // MessageBox.Show("Cant create/edit loop edges");
                clickedVertex = null;
                return;
            }
            Edge edge;
            if (_edges.TryGetValue(new Tuple<Vertex, Vertex>(clickedVertex, vertex), out edge) ||
                !_graphMetadata.IsOriented && _edges.TryGetValue(new Tuple<Vertex, Vertex>(vertex, clickedVertex), out edge))
            {
                var edgeParametersWindow = new EdgeParametersWindow()
                {
                    InputTitle = $"Edit edge between vertexes {clickedVertex.Number} and {vertex.Number}"
                };
                if (edgeParametersWindow.ShowDialog() != true)
                {
                    clickedVertex = null;
                    return;
                };
                var edgeLength = edgeParametersWindow.EdgeLength;
                edge.LengthExpression = edgeLength;
            }
            else
            {
                var edgeParametersWindow = new EdgeParametersWindow()
                {
                    InputTitle = $"Add edge between vertexes {clickedVertex.Number} and {vertex.Number}"
                };
                if (edgeParametersWindow.ShowDialog() != true)
                {
                    clickedVertex = null;
                    return;
                }
                var edgeLength = edgeParametersWindow.EdgeLength;
                edge = new Edge(clickedVertex, vertex, edgeLength)
                {
                    Backed = !_graphMetadata.IsOriented
                };
                AddEdge(edge, clickedVertex, vertex);
            }
            clickedVertex = null;
        }

        private void AddVertex(VertexMetadata vertexMetadata, bool addToMetadata = false)
        {
            var vertex = new Vertex(vertexMetadata);
            _vertexes.Add(vertex);
            _degrees.Add(0);
            mainCanvas.Children.Add(vertex);
            vertex.MouseLeftButtonDown += VertexClicked;

            if (_graphMetadata.CustomProbabilities)
            {
                vertex.MouseDoubleClick += VertexDoubleClicked;
            }

            if (addToMetadata)
            {
                _graphMetadata.VertexMetadatas.Add(vertexMetadata);
            }
        }

        private void RemoveVertex(Vertex vertex)
        {
            var vertexNumber = vertex.Number;
            var index = _vertexes.FindIndex(x => x == vertex);
            _vertexes.RemoveAt(index);
            _degrees.RemoveAt(index);
            mainCanvas.Children.Remove(vertex);

            foreach (var (fromVertex, toVertex) in _edges.Keys.Where(item => item.Item1.Number == vertex.Number || item.Item2.Number == vertex.Number).ToList())
            {
                RemoveEdge(fromVertex, toVertex);
            }

            _graphMetadata.VertexMetadatas.Remove(vertex.Metadata);

            for (var i = vertexNumber; i < _vertexes.Count; i++)
            {
                _vertexes[i].Number--;
            }
        }

        private void AddEdge(Edge edge, Vertex edgeStartVertex, Vertex edgeEndVertex, bool addToMetadata = true)
        {
            // if we have oriented graph and trying to add back edge,
            // we need to draw it as a back edge
            var needToAdd = true;
            if (_edges.TryGetValue(new Tuple<Vertex, Vertex>(edgeEndVertex, edgeStartVertex), out var backEdge))
            {
                if (_graphMetadata.IsOriented)
                {
                    needToAdd = false;
                    backEdge.BackLengthExpression = edge.LengthExpression;
                    backEdge.Backed = true;
                    edge = backEdge;
                    //backEdge.Curved = true;
                    //backEdge.UpdateEdge();
                    //edge.Curved = true;
                    //edge.UpdateEdge();
                }
                else
                {
                    MessageBox.Show(
                        "Such edge already exists",
                        "",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }
            }

            if (!_graphMetadata.CustomProbabilities)
            {
                var startVertexNumber = edgeStartVertex.Number - 1;
                var endVertexNumber = edgeEndVertex.Number - 1;
                _degrees[startVertexNumber]++;
                edgeStartVertex.DegreeChangedEvent += degree => edge.UpdatedDegree(degree, !needToAdd);
                edgeStartVertex.UpdateDegree(_degrees[startVertexNumber]);
                if (!_graphMetadata.IsOriented)
                {
                    _degrees[endVertexNumber]++;
                    edgeEndVertex.DegreeChangedEvent += degree => edge.UpdatedDegree(degree, true);
                    edgeEndVertex.UpdateDegree(_degrees[endVertexNumber]);
                }
            }

            if (!needToAdd)
            {
                return;
            }

            edge.AddToCanvas(mainCanvas);
            _edges.Add(new Tuple<Vertex, Vertex>(edgeStartVertex, edgeEndVertex), edge);
            if (addToMetadata)
            {
                _graphMetadata.EdgeMetadatas.Add(edge.Metadata);
            }
        }

        private void RemoveEdge(Vertex chosenStartVertex, Vertex chosenEndVertex)
        {
            // if we are trying to remove unexisting edge,
            // show error message
            if (!_edges.TryGetValue(new Tuple<Vertex, Vertex>(chosenStartVertex, chosenEndVertex), out var edge))
            {
                if (_graphMetadata.IsOriented || !_edges.TryGetValue(new Tuple<Vertex, Vertex>(chosenEndVertex, chosenStartVertex), out _))
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
            if (_graphMetadata.IsOriented && _edges.TryGetValue(new Tuple<Vertex, Vertex>(chosenEndVertex, chosenStartVertex), out var backEdge))
            {
                backEdge.Curved = false;
                backEdge.UpdateEdge();
            }
            _edges.Remove(new Tuple<Vertex, Vertex>(chosenStartVertex, chosenEndVertex));
            _graphMetadata.EdgeMetadatas.Remove(edge.Metadata);
            edge.RemoveFromCanvas(mainCanvas);
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
                var start = edgeData.StartVertexNumber;
                var end = edgeData.EndVertexNumber;
                if (start <= 0 || start > metadata.VertexMetadatas.Count || end <= 0 ||
                    end > metadata.VertexMetadatas.Count)
                {
                    return false;
                }

                if (start == end)
                {
                    //dont allow loop edges
                    return false;
                }

                if (testEdgeDict.ContainsKey(new Tuple<int, int>(start, end)))
                {
                    // two same edges
                    return false;
                }

                if (!metadata.IsOriented && testEdgeDict.ContainsKey(new Tuple<int, int>(end, start)))
                {
                    // unoriented graph with 2 same edges
                    return false;
                }
                testEdgeDict.Add(
                    new Tuple<int, int>(start, end),
                    edgeData
                );
                try
                {
                    var _ = SymbolicExpression.Parse(edgeData.Length);
                    if (metadata.CustomProbabilities)
                    {
                        _ = SymbolicExpression.Parse(edgeData.Probability);
                    }
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }

        private void LoadGraph(GraphMetadata metadata)
        {
            ClearGraph();

            metadata.VertexMetadatas.Sort(((metadata1, metadata2) => metadata1.Number.CompareTo(metadata2.Number)));

            _graphMetadata = new GraphMetadata
            {
                IsOriented = metadata.IsOriented,
                CustomProbabilities = metadata.CustomProbabilities
            };

            _vertexes = new List<Vertex>(metadata.VertexMetadatas.Count);
            _edges = new Dictionary<Tuple<Vertex, Vertex>, Edge>();
            foreach (var vertexData in metadata.VertexMetadatas)
            {
                AddVertex(vertexData, true);
            }

            foreach (var edgeData in metadata.EdgeMetadatas)
            {
                var edgeStartVertex = _vertexes[edgeData.StartVertexNumber - 1];
                var edgeEndVertex = _vertexes[edgeData.EndVertexNumber - 1];
                var edge = new Edge(
                    edgeStartVertex,
                    edgeEndVertex,
                    edgeData
                );
                edge.Backed = !_graphMetadata.IsOriented;
                edge.UpdateEdge();

                AddEdge(edge, edgeStartVertex, edgeEndVertex);
            }

            GraphMetadata.SolutionAlgorithm = cmbSolution.SelectedItem as SolutionAlgorithm;
            Matrix.InverseAlgorithm = cmbInverse.SelectedItem as InverseAlgorithm;
            Matrix.MultiplyAlgorithm = cmbMult.SelectedItem as MultiplyAlgorithm;
        }

        private void ClearGraph()
        {
            foreach (var vertex in _vertexes.ToArray())
            {
                RemoveVertex(vertex);
            }

            _degrees = new List<int>();

            foreach (var edgePair in _edges)
            {
                var edge = edgePair.Value;
                edge.RemoveFromCanvas(mainCanvas);
            }

            _graphMetadata = new GraphMetadata();
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var createGraphWindow = new GraphCreateWindow();
            if (createGraphWindow.ShowDialog() != true) return;
            var isOrientedString = (createGraphWindow.GraphTypeComboBox.SelectedItem as TextBlock)?.Text;
            var isOriented = isOrientedString.Equals("Digraph");
            var customProbas = createGraphWindow.CustomProbasCheckBox.IsChecked == true;
            CreateGraph(isOriented, customProbas);
        }

        private void CreateGraph(bool isOriented, bool customProbas)
        {
            ClearGraph();
            _graphMetadata = new GraphMetadata()
            {
                IsOriented = isOriented,
                CustomProbabilities = customProbas
            };
            _vertexes = new List<Vertex>();
            _edges = new Dictionary<Tuple<Vertex, Vertex>, Edge>();

            _working = true;
            savePanel.Visibility = Visibility.Visible;
            buttonPanel.Visibility = Visibility.Visible;
            mainCanvas.Visibility = Visibility.Visible;
            canvasBorder.Visibility = Visibility.Visible;
        }

        private async void CalculateButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_vertexes.Count == 0) return;
            var algo = new GraphAlgorithms(_graphMetadata);
            var status = algo.Check();
            if (status != CheckStatus.Ok)
            {
                var errMessage = "Following erros were found:\n";
                if (status.HasFlag(CheckStatus.EndVertexNotSelected))
                {
                    errMessage += "End vertex wasn't selected\n";
                }

                if (status.HasFlag(CheckStatus.AllVertexesAreEnding))
                {
                    errMessage += "There should be at least one path vertex.\n";
                }

                if (status.HasFlag(CheckStatus.WrongProbabilities))
                {
                    errMessage += "Sum of all probabilities of outcoming edges of one's vertex should be 1\n";
                }

                if (status.HasFlag(CheckStatus.WrongConnectionComponents))
                {
                    errMessage += "All vertexes should be in one strong component";
                }

                MessageBox.Show(
                    errMessage,
                    "Incorrect grpah",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            var watcher = Stopwatch.StartNew();
            var res = await Task.Run(() => _graphMetadata.Solve());
            watcher.Stop();
            var calcResults = new List<Tuple<int, SymbolicExpression>>();
            foreach (var calcResult in res)
            {
                var expanded = Algebraic.Expand(calcResult.Item2.Expression);
                calcResults.Add(new Tuple<int, SymbolicExpression>(calcResult.Item1, expanded));
            }

            var resWindow = new ResultsWindow(calcResults, watcher.Elapsed.TotalMilliseconds.ToString());
            resWindow.ShowDialog();
        }

        private void CmbSolution_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var algorithm = e.AddedItems[0] as SolutionAlgorithm;
            GraphMetadata.SolutionAlgorithm = algorithm;
        }

        private void CmbInverse_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var algorithm = e.AddedItems[0] as InverseAlgorithm;
            Matrix.InverseAlgorithm = algorithm;
        }

        private void CmbMult_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var algorithm = e.AddedItems[0] as MultiplyAlgorithm;
            Matrix.MultiplyAlgorithm = algorithm;
        }
    }
}
