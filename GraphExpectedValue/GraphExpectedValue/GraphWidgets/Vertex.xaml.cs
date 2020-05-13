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
    /// <summary>
    /// Графическое представление вершины графа
    /// </summary>
    public partial class Vertex : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// Координаты центра вершины
        /// </summary>
        private double x, y;

        public static DependencyProperty NumberProperty;
        public static DependencyProperty VertexTypeProperty;
        public static DependencyProperty ColorProperty;
        /// <summary>
        /// Diameter of vertex
        /// </summary>
        public static int Size { get; set; } = 30;
        /// <summary>
        /// Представление вершины для сериализации
        /// </summary>
        public VertexMetadata Metadata { get; }
        /// <summary>
        /// Центр вершины
        /// </summary>
        public Point Center => new Point(x, y);
        /// <summary>
        /// Номер вершины
        /// </summary>
        public int Number
        {
            get => (int)GetValue(NumberProperty);
            set {
                SetValue(NumberProperty, value);
                Metadata.Number = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Цвет, которым отрисовывается вершина
        /// </summary>
        public Brush CircleColor
        {
            get => (Brush)GetValue(ColorProperty);
            set
            {
                SetValue(ColorProperty, value);
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Проверяет, пересекаются ли наша вершина с данной
        /// </summary>
        public bool CheckIntersection(Point vertexCenter)
        {
            var distance = Math.Sqrt(
                Math.Pow(x - vertexCenter.X, 2) +
                Math.Pow(y - vertexCenter.Y, 2)
            );
            return distance > Size;
        }
        /// <summary>
        /// Тип вершины
        /// </summary>
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
        /// <summary>
        /// Статический конструктор, инициализирующий все DependencyProperty
        /// </summary>
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
        public Vertex()
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
            this.x = x;
            this.y = y;
            VertexType = type;
            Metadata.Position = Center;
            Canvas.SetLeft(this, x - Size / 2);
            Canvas.SetTop(this, y - Size / 2);
        }

        public Vertex(double x, double y, int number) : this(x, y, number, VertexType.PathVertex)
        {
        }

        public Vertex(VertexMetadata metadata):this(metadata.Position.X, metadata.Position.Y, metadata.Number, metadata.Type)
        {
            this.Metadata = metadata;
        }
        /// <summary>
        /// Событие, вызываемое при изменении свойств вершины
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Стандартная реализация интерфейса INotifyPropertyChanged
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
