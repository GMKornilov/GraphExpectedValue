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
    
    public partial class MainWindow : Window
    {
        private GraphMetadata _graphMetadata = new GraphMetadata();
        private List<Vertex> _vertexes = new List<Vertex>();
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
            var vertex = new Vertex(point.X, point.Y, _vertexes.Count + 1);
            _vertexes.Add(vertex);
            _graphMetadata.VertexMetadatas.Add(vertex.Metadata);
            mainCanvas.Children.Add(vertex);
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
            var edgeLengthExpr = edgePickWindow.EdgeLengthExpr;

            Edge edge;
            if (_graphMetadata.CustomProbabilities)
            {
                var edgeProbaExpr = edgePickWindow.EdgeProbabilityExpr;
                edge = new Edge(chosenEdgeStartVertex, chosenEdgeEndVertex, edgeLengthExpr, edgeProbaExpr)
                {
                    Backed = !_graphMetadata.IsOriented
                };
            }
            else
            {
                edge = new Edge(chosenEdgeStartVertex, chosenEdgeEndVertex, edgeLengthExpr)
                {
                    Backed = !_graphMetadata.IsOriented
                };
            }
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
            var edgeLengthExpr = edgePickWindow.EdgeLengthExpr;

            if (!_edges.TryGetValue(new Tuple<Vertex, Vertex>(chosenEdgeStartVertex, chosenEdgeEndVertex), out var edge))
            {
                edge = _edges[new Tuple<Vertex, Vertex>(chosenEdgeEndVertex, chosenEdgeStartVertex)];
            }

            edge.LengthExpression = edgeLengthExpr;
            if (_graphMetadata.CustomProbabilities)
            {
                var edgeProbaExpr = edgePickWindow.EdgeProbabilityExpr;
                edge.ProbabilityExpression = edgeProbaExpr;
            }
            edge.UpdateEdge();
        }

        private void RemoveEdgeButton_OnClick(object sender, RoutedEventArgs e)
        {
            Func<Tuple<int, int>, bool> checker = tuple =>
            {
                var (num1, num2) = tuple;
                var startEdgeVertex = _vertexes[num1 - 1];
                var endEdgeVertex = _vertexes[num2 - 1];
                return _edges.TryGetValue(new Tuple<Vertex, Vertex>(startEdgeVertex, endEdgeVertex), out _);
            };
            var edgeChooseWindow = new EdgeChooseWindow(checker) { TotalVertexes = _vertexes.Count };
            if (edgeChooseWindow.ShowDialog() != true) return;

            var chosenStartVertexNumber = edgeChooseWindow.ChosenStartVertex - 1;
            var chosenEndVertexNumber = edgeChooseWindow.ChosenEndVertex - 1;

            var chosenStartVertex = _vertexes[chosenStartVertexNumber];
            var chosenEndVertex = _vertexes[chosenEndVertexNumber];

            if (!_edges.TryGetValue(new Tuple<Vertex, Vertex>(chosenStartVertex, chosenEndVertex), out var edge))
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
            mainCanvas.Children.Remove(chosenVertex);

            foreach (var (fromVertex, toVertex) in _edges.Keys.Where(item => item.Item1.Number == chosenVertex.Number || item.Item2.Number == chosenVertex.Number).ToList())
            {
                RemoveEdge(fromVertex, toVertex);
            }
            
            _graphMetadata.VertexMetadatas.Remove(chosenVertex.Metadata);
            _vertexes.RemoveAt(chosenVertexNumber);

            for (var i = chosenVertexNumber; i < _vertexes.Count; i++)
            {
                _vertexes[i].Number--;
            }
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
                Prompt = "Add end vertex",
                TotalVertexes = _vertexes.Count,
                ConfirmButtonText = "Add end vertex"
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
            if (!_working)
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
                
                using (var stream = new FileStream(safeFileDialog.FileName, FileMode.Create))
                {
                    serializer.Serialize(stream, _graphMetadata);
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
            var openFileDialog = new OpenFileDialog()
            {
                Filter = "XML file (*.xml)|*.xml"
            };
            if (openFileDialog.ShowDialog() != true || string.IsNullOrEmpty(openFileDialog.FileName)) return;
            try
            {
                var serializer = new XmlSerializer(typeof(GraphMetadata));
                GraphMetadata metadata;
                using (var stream = new FileStream(openFileDialog.FileName, FileMode.OpenOrCreate))
                {
                    metadata = (GraphMetadata)serializer.Deserialize(stream);
                }

                if (!CheckMetadata(metadata))
                {
                    throw new XmlException();
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
        
        private void AddEdge(Edge edge, Vertex edgeStartVertex, Vertex edgeEndVertex, bool addToMetadata = true)
        {
            // if we have oriented graph and trying to add back edge,
            // we need to draw it as a back edge
            if (_edges.TryGetValue(new Tuple<Vertex, Vertex>(edgeEndVertex, edgeStartVertex), out var backEdge))
            {
                if (_graphMetadata.IsOriented)
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
                    return;
                }
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
                if (testEdgeDict.ContainsKey(new Tuple<int, int>(edgeData.StartVertexNumber, edgeData.EndVertexNumber)))
                {
                    // two same edges
                    return false;
                }
                testEdgeDict.Add(
                    new Tuple<int, int>(edgeData.StartVertexNumber, edgeData.EndVertexNumber),
                    edgeData
                );
                try
                {
                    var parseTest = SymbolicExpression.Parse(edgeData.Length);
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
            _graphMetadata = metadata;
            _vertexes = new List<Vertex>(metadata.VertexMetadatas.Count);
            _edges = new Dictionary<Tuple<Vertex, Vertex>, Edge>();
            foreach (var vertexData in metadata.VertexMetadatas)
            {
                var vertex = new Vertex(vertexData);
                _vertexes.Add(vertex);
                mainCanvas.Children.Add(vertex);
            }

            foreach (var edgeData in metadata.EdgeMetadatas)
            {
                var edgeStartVertex = _vertexes[edgeData.StartVertexNumber - 1];
                var edgeEndVertex = _vertexes[edgeData.EndVertexNumber - 1];
                var edge = new Edge(
                    edgeStartVertex,
                    edgeEndVertex,
                    edgeData,
                    _graphMetadata.CustomProbabilities
                );
                edge.Backed = !_graphMetadata.IsOriented;
                edge.UpdateEdge();
                AddEdge(edge, edgeStartVertex, edgeEndVertex, false);
            }

            GraphMetadata.solutionStrategy = cmbSolution.SelectedItem as SolutionAlgorithm;
            Matrix.inverseStrategy = cmbInverse.SelectedItem as InverseAlgorithm;
            Matrix.multiplyStrategy = cmbMult.SelectedItem as MultiplyAlgorithm;
        }

        private void ClearGraph()
        {
            foreach (var vertex in _vertexes)
            {
                mainCanvas.Children.Remove(vertex);
            }

            foreach (var edgePair in _edges)
            {
                var edge = edgePair.Value;
                edge.RemoveFromCanvas(mainCanvas);
            }
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var createGraphWindow = new GraphCreateWindow();
            if(createGraphWindow.ShowDialog() != true)return;
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
                    errMessage += "Sum of all probabilities of outcoming edges of one's vertex should be 1";
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
            var res = await Task<Tuple<int, SymbolicExpression>[]>.Factory.StartNew(() => _graphMetadata.Solve());
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
            var strategy = e.AddedItems[0] as SolutionAlgorithm;
            GraphMetadata.solutionStrategy = strategy;
        }

        private void CmbInverse_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var strategy = e.AddedItems[0] as InverseAlgorithm;
            Matrix.inverseStrategy = strategy;
        }

        private void CmbMult_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var strategy = e.AddedItems[0] as MultiplyAlgorithm;
            Matrix.multiplyStrategy = strategy;
        }
    }
}
