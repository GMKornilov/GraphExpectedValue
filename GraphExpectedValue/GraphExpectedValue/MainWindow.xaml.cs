using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            //Point? dragStart = null;

            //MouseButtonEventHandler mouseDown = (sender, args) => {
            //    var element = (UIElement)sender;
            //    dragStart = args.GetPosition(element);
            //    element.CaptureMouse();
            //};
            //MouseButtonEventHandler mouseUp = (sender, args) => {
            //    var element = (UIElement)sender;
            //    dragStart = null;
            //    element.ReleaseMouseCapture();
            //};
            //MouseEventHandler mouseMove = (sender, args) => {
            //    if (dragStart != null && args.LeftButton == MouseButtonState.Pressed)
            //    {
            //        var element = (UIElement)sender;
            //        var p2 = args.GetPosition(testCanvas);
            //        if (dragStart != null && p2.X > 0 && p2.Y > 0 && p2.X < testCanvas.ActualWidth && p2.Y < testCanvas.ActualHeight)
            //        {
            //            Canvas.SetLeft(element, p2.X - dragStart.Value.X);
            //            Canvas.SetTop(element, p2.Y - dragStart.Value.Y);
            //        }
            //    }
            //};
            //Action<UIElement> enableDrag = (element) => {
            //    element.MouseDown += mouseDown;
            //    element.MouseMove += mouseMove;
            //    element.MouseUp += mouseUp;
            //};
            //var shapes = new UIElement[] {
            //    new Ellipse() { Fill = Brushes.DarkKhaki, Width = 100, Height = 100 },
            //    new Rectangle() { Fill = Brushes.LawnGreen, Width = 200, Height = 100 },
            //};


            //foreach (var shape in shapes)
            //{
            //    testCanvas.Children.Add(shape);
            //}

            //foreach (UIElement child in testCanvas.Children)
            //{
            //    enableDrag(child);
            //}
        }

        private void AddEdgeButton_OnClick(object sender, RoutedEventArgs e)
        {
            vertex.Number++;
        }

        private void RemoveEdgeButton_OnClick(object sender, RoutedEventArgs e)
        {
            vertex.Number--;
        }
    }
}
