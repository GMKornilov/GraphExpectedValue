using System.ComponentModel;
using System.Runtime.CompilerServices;
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
    public partial class Vertex : UserControl, INotifyPropertyChanged
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
            set {
                SetValue(NumberProperty, value);
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
                    BlackColorBrush
                )
            );
        }
        public Vertex()
        {
            InitializeComponent();
        }
        public Vertex(int number) : this()
        {
            Number = number;
        }
        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
