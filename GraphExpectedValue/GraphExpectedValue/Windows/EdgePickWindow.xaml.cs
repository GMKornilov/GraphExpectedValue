using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using GraphExpectedValue.Annotations;
using MathNet.Symbolics;

namespace GraphExpectedValue.Windows
{
    /// <summary>
    /// Interaction logic for EdgePickWindow.xaml
    /// </summary>
    public partial class EdgePickWindow : Window, INotifyPropertyChanged
    {
        private int _totalVertexes;
        private Func<int, int, bool> _checker;

        public int TotalVertexes
        {
            get => _totalVertexes;
            set
            {
                _totalVertexes = value;
                OnPropertyChanged(nameof(TotalVertexes));
            }
        }

        public int StartVertexNumber { get; set; }
        public int EndVertexNumber { get; set; }
        public string EdgeLengthExpr { get; set; }
        public SymbolicExpression Expression { get; set; }
        public EdgePickWindow()
        {
            InitializeComponent();
            StartVertexNumber = -1;
            EndVertexNumber = -1;
        }

        public EdgePickWindow(Func<int, int, bool> checker) : this()
        {
            _checker = checker;
        }

        private void CreateEdgeButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (StartVertexNumber == -1 || EndVertexNumber == -1)
            {
                MessageBox.Show(
                    "One of vertexes isn't choosen",
                    "",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }
            double len;
            try
            {
                Expression = Infix.ParseOrThrow(EdgeLengthExpr);
                len = Expression.Evaluate(null).RealValue;
            }
            catch
            {
                MessageBox.Show("input correct expression");
                return;
            }

            if (len <= 0)
            {
                MessageBox.Show("Edge should have positive length", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_checker?.Invoke(StartVertexNumber, EndVertexNumber) == true)
            {
                DialogResult = true;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
