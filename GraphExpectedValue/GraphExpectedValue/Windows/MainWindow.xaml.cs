using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
using GraphExpectedValue.Utility.ConcreteStrategies;
using Microsoft.Win32;
using MathNet.Numerics;
using MathNet.Symbolics;
using Expression = MathNet.Symbolics.Expression;

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
    public partial class MainWindow
    {
        public readonly ActionCommand saveActionCommand;
        public readonly ActionCommand openActionCommand;
        private GraphMetadata graphMetadata = new GraphMetadata();
        private List<Vertex> vertexes = new List<Vertex>();
        private List<int> degrees = new List<int>();
        private Dictionary<Tuple<Vertex, Vertex>, Edge> edges = new Dictionary<Tuple<Vertex, Vertex>, Edge>();
        private Vertex startVertex = null, endVertex = null;
        private Vertex clickedVertex = null;
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
            canvasBorder.Visibility = Visibility.Hidden;
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
            var vertexMetadata = new VertexMetadata(vertexes.Count + 1, VertexType.PathVertex, point);
            AddVertex(vertexMetadata, true);
        }

        private void AddEdgeButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (vertexes.Count < 2) return;
            Func<int, int, bool> checker = (startVertexNumber, endVertexNumber) =>
            {
                if (startVertexNumber == endVertexNumber)
                {
                    MessageBox.Show("Can\'t create loop edges", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                startVertexNumber--;
                endVertexNumber--;
                var edgeStartVertex = vertexes[startVertexNumber];
                var edgeEndVertex = vertexes[endVertexNumber];
                if (edges.TryGetValue(new Tuple<Vertex, Vertex>(edgeStartVertex, edgeEndVertex), out _)
                || (!graphMetadata.IsOriented && edges.TryGetValue(new Tuple<Vertex, Vertex>(edgeEndVertex, edgeStartVertex), out _)))
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
            var edgePickWindow = new EdgePickWindow(checker, graphMetadata.CustomProbabilities) { TotalVertexes = vertexes.Count };
            if (edgePickWindow.ShowDialog() != true) return;

            var chosenStartVertexNumber = edgePickWindow.StartVertexNumber - 1;
            var chosenEndVertexNumber = edgePickWindow.EndVertexNumber - 1;

            var chosenEdgeStartVertex = vertexes[chosenStartVertexNumber];
            var chosenEdgeEndVertex = vertexes[chosenEndVertexNumber];
            var edgeLengthExpr = edgePickWindow.EdgeLengthExpr;

            Edge edge;
            if (graphMetadata.CustomProbabilities)
            {
                var edgeProbaExpr = edgePickWindow.EdgeProbabilityExpr;
                edge = new Edge(chosenEdgeStartVertex, chosenEdgeEndVertex, edgeLengthExpr, edgeProbaExpr)
                {
                    Backed = !graphMetadata.IsOriented
                };
            }
            else
            {
                edge = new Edge(chosenEdgeStartVertex, chosenEdgeEndVertex, edgeLengthExpr)
                {
                    Backed = !graphMetadata.IsOriented
                };
            }
            edge.UpdateEdge();
            AddEdge(edge, chosenEdgeStartVertex, chosenEdgeEndVertex);
        }

        private void EditEdgeButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (edges.Count == 0) return;
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

                var edgeStartVertex = vertexes[startVertexNumber];
                var edgeEndVertex = vertexes[endVertexNumber];

                var edgeTuple = new Tuple<Vertex, Vertex>(edgeStartVertex, edgeEndVertex);
                var backEdgeTuple = new Tuple<Vertex, Vertex>(edgeEndVertex, edgeStartVertex);
                if (!edges.TryGetValue(edgeTuple, out _))
                {
                    if (!graphMetadata.IsOriented && !edges.TryGetValue(backEdgeTuple, out _))
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
            var edgePickWindow = new EdgePickWindow(checker, graphMetadata.CustomProbabilities)
            {
                TotalVertexes = vertexes.Count,
                Title = "Edit edge",
                EndButton = { Content = "Edit edge" }
            };
            if (edgePickWindow.ShowDialog() != true) return;

            var chosenStartVertexNumber = edgePickWindow.StartVertexNumber - 1;
            var chosenEndVertexNumber = edgePickWindow.EndVertexNumber - 1;

            var chosenEdgeStartVertex = vertexes[chosenStartVertexNumber];
            var chosenEdgeEndVertex = vertexes[chosenEndVertexNumber];
            var edgeLengthExpr = edgePickWindow.EdgeLengthExpr;

            if (!edges.TryGetValue(new Tuple<Vertex, Vertex>(chosenEdgeStartVertex, chosenEdgeEndVertex), out var edge))
            {
                edge = edges[new Tuple<Vertex, Vertex>(chosenEdgeEndVertex, chosenEdgeStartVertex)];
            }

            edge.LengthExpression = edgeLengthExpr;
            if (graphMetadata.CustomProbabilities)
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

            //if (chosenVertex == startVertex)
            //{
            //    startVertex = null;
            //    graphMetadata.StartVertexNumber = -1;
            //}
            if (chosenVertex == endVertex)
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

        //private void StartVertexButton_OnClick(object sender, RoutedEventArgs e)
        //{
        //    if (vertexes.Count == 0) return;
        //    var vertexPickWindow = new VertexChooseWindow()
        //    {
        //        Prompt = "Choose start vertex",
        //        TotalVertexes = vertexes.Count,
        //        ConfirmButtonText = "Choose start vertex"
        //    };
        //    if (vertexPickWindow.ShowDialog() != true) return;
        //    var chosenVertexNumber = vertexPickWindow.ChosenVertex - 1;
        //    var chosenVertex = vertexes[chosenVertexNumber];
        //    //SetStartVertex(chosenVertex);
        //}

        private void AddEndVertexButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (vertexes.Count == 0) return;
            var vertexPickWindow = new VertexChooseWindow()
            {
                Prompt = "Add end vertex",
                TotalVertexes = vertexes.Count,
                ConfirmButtonText = "Add end vertex"
            };
            Func<int, bool> checker = vertexNumber =>
            {
                var vertex = vertexes[vertexNumber - 1];
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
                var chosenVertex = vertexes[chosenVertexNumber];
                chosenVertex.VertexType = VertexType.EndVertex;
            }
        }

        private void RemoveEndVertexButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (vertexes.Count == 0) return;
            var vertexPickWindow = new VertexChooseWindow()
            {
                Prompt = "Remove end vertex",
                TotalVertexes = vertexes.Count,
                ConfirmButtonText = "Remove end vertex"
            };
            Func<int, bool> checker = vertexNumber =>
            {
                var vertex = vertexes[vertexNumber - 1];
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
                var chosenVertex = vertexes[chosenVertexNumber];
                chosenVertex.VertexType = VertexType.PathVertex;
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
                    //Debug.WriteLine("WRONG");
                    throw new XmlException();
                }

                Working = true;
                savePanel.Visibility = Visibility.Visible;
                buttonPanel.Visibility = Visibility.Visible;
                testCanvas.Visibility = Visibility.Visible;
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

        //private void SetStartVertex(Vertex vertex)
        //{
        //    if (startVertex != null && !startVertex.Equals(vertex))
        //    {
        //        startVertex.PropertyChanged -= UpdateStartVertexNumber;
        //        startVertex.VertexType = VertexType.PathVertex;
        //        startVertex = null;
        //    }

        //    if (vertex == endVertex)
        //    {
        //        endVertex.PropertyChanged -= UpdateEndVertexNumber;
        //        endVertex.VertexType = VertexType.PathVertex;
        //        endVertex = null;
        //        graphMetadata.EndVertexNumber = -1;
        //    }

        //    vertex.PropertyChanged += UpdateStartVertexNumber;
        //    vertex.VertexType = VertexType.StartVertex;
        //    startVertex = vertex;
        //}

        private void AddVertex(VertexMetadata vertexMetadata, bool addToMetadata = false)
        {
            var vertex = new Vertex(vertexMetadata);
            vertexes.Add(vertex);
            degrees.Add(0);
            testCanvas.Children.Add(vertex);
            // TODO: add update and click handler
            vertex.MouseLeftButtonDown += VertexClicked;
            if (addToMetadata)
            {
                graphMetadata.VertexMetadatas.Add(vertexMetadata);
            }
        }

        private void VertexClicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is Vertex vertex)
            {
                if (clickedVertex == null)
                {
                    clickedVertex = vertex;
                    return;
                }

                Edge edge;
                EdgeParametersWindow edgeParametersWindow;
                if (edges.TryGetValue(new Tuple<Vertex, Vertex>(clickedVertex, vertex), out edge) ||
                    !graphMetadata.IsOriented &&
                    edges.TryGetValue(new Tuple<Vertex, Vertex>(vertex, clickedVertex), out edge))
                {
                    //TODO: edit
                    edgeParametersWindow = new EdgeParametersWindow(graphMetadata.CustomProbabilities);
                    if(edgeParametersWindow.ShowDialog() != true)return;
                    var edgeLength = edgeParametersWindow.EdgeLength;
                    var edgeProba = edgeParametersWindow.EdgeProba;
                    edge.LengthExpression = edgeLength;
                    if (graphMetadata.CustomProbabilities)
                    {
                        edge.ProbabilityExpression = edgeProba;
                    }
                    edge.UpdateEdge();
                }
                else
                {
                    edgeParametersWindow = new EdgeParametersWindow(graphMetadata.CustomProbabilities);
                    if(edgeParametersWindow.ShowDialog() != true) return;
                    var edgeLength = edgeParametersWindow.EdgeLength;
                    if (graphMetadata.CustomProbabilities)
                    {
                        var edgeProba = edgeParametersWindow.EdgeProba;
                        edge = new Edge(clickedVertex, vertex, edgeLength, edgeProba)
                        {
                            Backed = !graphMetadata.IsOriented
                        };
                    }
                    else
                    {
                        edge = new Edge(clickedVertex, vertex, edgeLength)
                        {
                            Backed = !graphMetadata.IsOriented
                        };
                    }
                    edge.UpdateEdge();
                    AddEdge(edge, clickedVertex, vertex);
                }
            }
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
                    return;
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
            edge.RemoveFromCanvas();
        }

        //private void UpdateStartVertexNumber(object sender, PropertyChangedEventArgs e)
        //{
        //    if (sender is Vertex vertex)
        //    {
        //        graphMetadata.StartVertexNumber = vertex.Number;
        //    }
        //}

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
            //if (metadata.StartVertexNumber != -1 && (metadata.StartVertexNumber < 1 ||
            //                                         metadata.StartVertexNumber > metadata.VertexMetadatas.Count))
            //{
            //    return false;
            //}
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
                AddVertex(vertexData);
            }

            foreach (var edgeData in metadata.EdgeMetadatas)
            {
                var edgeStartVertex = vertexes[edgeData.StartVertexNumber - 1];
                var edgeEndVertex = vertexes[edgeData.EndVertexNumber - 1];
                var edge = new Edge(
                    edgeStartVertex,
                    edgeEndVertex,
                    edgeData,
                    graphMetadata.CustomProbabilities
                );
                edge.Backed = !graphMetadata.IsOriented;
                edge.UpdateEdge();
                AddEdge(edge, edgeStartVertex, edgeEndVertex, false);
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
                edge.RemoveFromCanvas();
            }

            //startVertex = null;
            endVertex = null;
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
            graphMetadata = new GraphMetadata()
            {
                IsOriented = isOriented,
                CustomProbabilities = customProbas
            };
            vertexes = new List<Vertex>();
            edges = new Dictionary<Tuple<Vertex, Vertex>, Edge>();
            //startVertex = null;
            endVertex = null;

            Working = true;
            savePanel.Visibility = Visibility.Visible;
            buttonPanel.Visibility = Visibility.Visible;
            testCanvas.Visibility = Visibility.Visible;
            canvasBorder.Visibility = Visibility.Visible;
        }

        private async void CalculateButton_OnClick(object sender, RoutedEventArgs e)
        {
            var algo = new GraphAlgorithms(graphMetadata);
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
            //var res = graphMetadata.Solve();
            var res = await Task<Tuple<int, SymbolicExpression>[]>.Factory.StartNew(() =>
            {
                //Task.Delay(1000).Wait();
                return graphMetadata.Solve();
            });
            watcher.Stop();
            var calcResults = new List<Tuple<int, SymbolicExpression, double>>();
            for (var i = 0; i < res.Length; i++)
            {
                var expanded = MathNet.Symbolics.Algebraic.Expand(res[i].Item2.Expression);
                calcResults.Add(new Tuple<int, SymbolicExpression, double>(res[i].Item1, expanded, res[i].Item2.Evaluate(null).RealValue));
            }
            //for (var i = 0; i < 6; i++)
            //{
            //    calcResults.AddRange(calcResults);
            //}

            var resWindow = new ResultsWindow(calcResults, watcher.Elapsed.TotalMilliseconds.ToString());
            resWindow.ShowDialog();
        }

        private void CmbSolution_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var strategy = e.AddedItems[0] as SolutionStrategy;
            //Debug.WriteLine(strategy.ToString());
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
