using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GraphExpectedValue
{
    public enum VertexType
    {
        StartVertex,
        EndVertex,
        PathVertex
    }
    /// <summary>
    /// Interaction logic for Vertex.xaml
    /// </summary>
    public partial class Vertex : UserControl
    {
        private static readonly SolidColorBrush BlackColorBrush = new SolidColorBrush(Colors.Black);
        private static readonly SolidColorBrush RedColorBrush = new SolidColorBrush(Colors.Red);
        private static readonly SolidColorBrush BlueColorBrush = new SolidColorBrush(Colors.Blue);

        public static DependencyProperty NumberProperty;
        public static DependencyProperty VertexTypeProperty;
        public static DependencyProperty ColorProperty;

        public int Number
        {
            get => (int)GetValue(NumberProperty);
            set => SetValue(NumberProperty, value);
        }

        public Brush CircleColor
        {
            get => (Brush)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        public VertexType VertexType
        {
            get => (VertexType)GetValue(VertexTypeProperty);
            set
            {
                switch (value)
                {
                    case VertexType.EndVertex:
                        CircleColor = BlueColorBrush;
                        break;
                    case VertexType.PathVertex:
                        CircleColor = BlackColorBrush;
                        break;
                    case VertexType.StartVertex:
                        CircleColor = RedColorBrush;
                        break;
                }
                SetValue(VertexTypeProperty, value);
            }
        }

        static Vertex()
        {
            NumberProperty = DependencyProperty.Register(
                "Number",
                typeof(int),
                typeof(Vertex)
            );
            VertexTypeProperty = DependencyProperty.Register(
                "VertexType",
                typeof(VertexType),
                typeof(Vertex),
                new FrameworkPropertyMetadata(
                    VertexType.PathVertex
                )
            );
            ColorProperty = DependencyProperty.Register(
                "Color",
                typeof(SolidColorBrush),
                typeof(Vertex),
                new FrameworkPropertyMetadata(
                    BlackColorBrush,
                    OnColorChanged
                )
            );
        }

        private static void OnColorChanged(DependencyObject sender,
            DependencyPropertyChangedEventArgs e)
        {
            var vertex = sender as Vertex;
            var newColorBrush = e.NewValue as SolidColorBrush;
            vertex?.SetBorderBackground(newColorBrush);
        }

        private void SetBorderBackground(Brush brush)
        {
            if (Border != null)
            {
                Border.Background = brush;
            }
        }
        public Vertex()
        {
            InitializeComponent();
        }
        public Vertex(int number) : this()
        {
            Number = number;
        }
    }
}
