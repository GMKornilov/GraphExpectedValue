using System;
using System.Diagnostics;
using System.Windows;

namespace GraphExpectedValue
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Vertex vertex;

        public MainWindow()
        {
            InitializeComponent();
            vertex = new Vertex(1);
            testCanvas.Children.Add(vertex);
        }


        private void AddEdgeButton_OnClick(object sender, RoutedEventArgs e)
        {
            vertex.VertexType = VertexType.StartVertex;
        }

        private void RemoveEdgeButton_OnClick(object sender, RoutedEventArgs e)
        {
            vertex.VertexType = VertexType.PathVertex;
            vertex.Number++;
        }

        private void RemoveVertexButton_OnClick(object sender, RoutedEventArgs e)
        {
            vertex.VertexType = VertexType.EndVertex;
            vertex.Number--;
        }
    }
}
