using System;
using System.Collections.Generic;
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
            get => (int) GetValue(NumberProperty);
            set => SetValue(NumberProperty, value);
        }

        public SolidColorBrush CircleColor
        {
            get =>(SolidColorBrush) GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        public VertexType VertexType
        {
            get => (VertexType) GetValue(VertexTypeProperty);
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
                typeof(Vertex)
            );
            ColorProperty = DependencyProperty.Register(
                "Color",
                typeof(SolidColorBrush),
                typeof(Vertex)
            );
        }
        public Vertex()
        {
            VertexType = VertexType.PathVertex;

            InitializeComponent();
        }
        public Vertex(int number):this()
        {
            Number = number;
        }
    }
}
