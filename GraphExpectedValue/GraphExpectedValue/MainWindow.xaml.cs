using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GraphExpectedValue
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Vertex vertex;
        private int total_vertexes = 0;

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
            var vertex = new Vertex(point.X, point.Y, ++total_vertexes);
            testCanvas.Children.Add(vertex);

        }


        private void AddEdgeButton_OnClick(object sender, RoutedEventArgs e)
        {
            //vertex.VertexType = VertexType.StartVertex;
        }

        private void RemoveEdgeButton_OnClick(object sender, RoutedEventArgs e)
        {
            //vertex.VertexType = VertexType.PathVertex;
            //vertex.Number++;
        }

        private void RemoveVertexButton_OnClick(object sender, RoutedEventArgs e)
        {
            //vertex.VertexType = VertexType.EndVertex;
            //vertex.Number--;
        }
    }
}
