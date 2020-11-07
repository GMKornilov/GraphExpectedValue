using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GraphExpectedValue.GraphLogic;

namespace GraphExpectedValue.GraphWidgets
{
    public enum VertexType
    {
        StartVertex,
        EndVertex,
        PathVertex
    }
    
    public partial class Vertex : UserControl, INotifyPropertyChanged
    {
        
        private double _x, _y;

        public static DependencyProperty NumberProperty;
        public static DependencyProperty VertexTypeProperty;
        public static DependencyProperty ColorProperty;

        public event Action<int> DegreeChangedEvent;
        
        public static int Size { get; set; } = 30;
        
        public VertexMetadata Metadata { get; }
        
        public Point Center => new Point(_x, _y);
        
        public int Number
        {
            get => (int)GetValue(NumberProperty);
            set {
                SetValue(NumberProperty, value);
                Metadata.Number = value;
                OnPropertyChanged();
            }
        }
        
        public Brush CircleColor
        {
            get => (Brush)GetValue(ColorProperty);
            set
            {
                SetValue(ColorProperty, value);
                OnPropertyChanged();
            }
        }
        
        public bool CheckIntersection(Point vertexCenter)
        {
            var distance = Math.Sqrt(
                Math.Pow(_x - vertexCenter.X, 2) +
                Math.Pow(_y - vertexCenter.Y, 2)
            );
            return distance > Size;
        }
        
        public VertexType VertexType
        {
            get => (VertexType)GetValue(VertexTypeProperty);
            set
            {
                switch (value)
                {
                    case VertexType.EndVertex:
                        CircleColor = Brushes.Blue;
                        break;
                    case VertexType.PathVertex:
                        CircleColor = Brushes.Black;
                        break;
                    case VertexType.StartVertex:
                        CircleColor = Brushes.Red;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, "Incorrect vertex type");
                }

                Metadata.Type = value;
                SetValue(VertexTypeProperty, value);
                OnPropertyChanged();
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
                "CircleColor",
                typeof(SolidColorBrush),
                typeof(Vertex),
                new FrameworkPropertyMetadata(
                    Brushes.Black
                )
            );
        }
        private Vertex()
        {
            InitializeComponent();
            Metadata = new VertexMetadata(this);
        }
        public Vertex(int number) : this()
        {
            Number = number;
        }

        public Vertex(double x, double y, int number, VertexType type) : this(number)
        {
            _x = x;
            _y = y;
            VertexType = type;
            Metadata.Position = Center;
            Canvas.SetLeft(this, x - Size / 2.0);
            Canvas.SetTop(this, y - Size / 2.0);
        }

        public Vertex(double x, double y, int number) : this(x, y, number, VertexType.PathVertex)
        {
        }

        public Vertex(VertexMetadata metadata):this(metadata.Position.X, metadata.Position.Y, metadata.Number, metadata.Type)
        {
            Metadata = metadata;
        }

        public void UpdateDegree(int degree)
        {
            DegreeChangedEvent?.Invoke(degree);
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
