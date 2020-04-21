using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GraphExpectedValue.Annotations;

namespace GraphExpectedValue.Windows
{
    /// <summary>
    /// Interaction logic for EdgeChooseWindow.xaml
    /// </summary>
    public partial class EdgeChooseWindow : Window, INotifyPropertyChanged
    {
        private int _totalVertexes;
        private Func<Tuple<int, int>, bool> checker;

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
            this.checker = checker;
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
            ;
            if (!checker(new Tuple<int, int>(ChosenStartVertex, ChosenEndVertex)))
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
