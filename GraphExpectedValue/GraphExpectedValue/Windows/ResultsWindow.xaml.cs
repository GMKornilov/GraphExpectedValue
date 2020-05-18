using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using GraphExpectedValue.Annotations;
using MathNet.Symbolics;

namespace GraphExpectedValue.Windows
{
    public partial class ResultsWindow : Window, INotifyPropertyChanged
    {
        private string _elapsedTime;

        private List<Tuple<int, SymbolicExpression>> _calcResults;

        public string ElapsedTime
        {
            get => _elapsedTime;
            set
            {
                _elapsedTime = value;
                OnPropertyChanged();
            }
        }

        public List<Tuple<int, SymbolicExpression>> CalcResults
        {
            get => _calcResults;
            set
            {
                _calcResults = value;
                OnPropertyChanged();
            }
        }

        private ResultsWindow()
        {
            InitializeComponent();
        }

        public ResultsWindow(List<Tuple<int, SymbolicExpression>> calcResults, string elapsedTime) : this()
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
