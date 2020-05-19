using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using GraphExpectedValue.Annotations;

namespace GraphExpectedValue.Windows
{
    public partial class EdgeChooseWindow : Window, INotifyPropertyChanged
    {
        private int _totalVertexes;

        private Func<Tuple<int, int>, bool> _checker;

        public int TotalVertexes
        {
            get => _totalVertexes;
            set
            {
                _totalVertexes = value;
                OnPropertyChanged(nameof(TotalVertexes));
            }
        }

        public int ChosenStartVertex { get; set; }

        public int ChosenEndVertex { get; set; }

        private EdgeChooseWindow()
        {
            InitializeComponent();
        }

        public EdgeChooseWindow(Func<Tuple<int, int>, bool> checker) : this()
        {
            _checker = checker;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
        {
            if(ChosenStartVertex == 0 || ChosenEndVertex == 0)return;
            if (!_checker(new Tuple<int, int>(ChosenStartVertex, ChosenEndVertex)))
            {
                MessageBox.Show(
                    "Such edge doesnt exist",
                    "",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            DialogResult = true;
        }
    }
}
