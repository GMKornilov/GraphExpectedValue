using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public MainWindow()
        {
            InitializeComponent();

            testCanvas.MouseLeftButtonDown += TestCanvasOnMouseLeftButtonDown;
        }

        private void TestCanvasOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("Click!!!");
            Debug.WriteLine(e.GetPosition(testCanvas).X + " " + e.GetPosition(testCanvas).Y);
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
            Debug.WriteLine($"{edgePickWindow.StartVertexNumber} {edgePickWindow.EndVertexNumber} {edgePickWindow.EdgeLength}");
            var startVertexNumber = edgePickWindow.StartVertexNumber - 1;
            var endVertexNumber = edgePickWindow.EndVertexNumber - 1;
            var edgeLength = edgePickWindow.EdgeLength;
            var edge = new Edge(vertexes[startVertexNumber], vertexes[endVertexNumber], edgeLength);
            edge.AddToCanvas(testCanvas);
        }

        private void RemoveEdgeButton_OnClick(object sender, RoutedEventArgs e)
        {
            
        }

        private void RemoveVertexButton_OnClick(object sender, RoutedEventArgs e)
        {
            //vertex.VertexType = VertexType.EndVertex;
            //vertex.Number--;
        }
    }
}
