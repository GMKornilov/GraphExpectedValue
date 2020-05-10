using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using MathNet.Symbolics;

namespace GraphExpectedValue.Windows
{
    /// <summary>
    /// Interaction logic for ResultsWindow.xaml
    /// </summary>
    public partial class ResultsWindow : Window, INotifyPropertyChanged
    {
        private string _elapsedTime;
        private List<Tuple<int, SymbolicExpression, double>> _calcResults;

        public string ElapsedTime
        {
            get => _elapsedTime;
            set
            {
                _elapsedTime = value;
                OnPropertyChanged();
            }
        }

        public List<Tuple<int, SymbolicExpression, double>> CalcResults
        {
            get => _calcResults;
            set
            {
                _calcResults = value;
                OnPropertyChanged();
            }
        }

        public ResultsWindow()
        {
            InitializeComponent();
        }

        public ResultsWindow(List<Tuple<int, SymbolicExpression, double>> calcResults, string elapsedTime) : this()
        {
            CalcResults = calcResults;
            ElapsedTime = elapsedTime;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e) => DialogResult = true;
    }
}
