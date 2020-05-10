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
        public double EdgeLength { get; set; }
        public string EdgeLengthExpr { get; set; }
        public SymbolicExpression Expression { get; set; }
        public EdgePickWindow()
        {
            InitializeComponent();
        }

        private void CreateEdgeButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (StartVertexNumber == EndVertexNumber)
            {
                MessageBox.Show("Can\'t create loop edges", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            //if (EdgeLength <= 0)
            //{
            //    MessageBox.Show("Edge should have positive length", "", MessageBoxButton.OK, MessageBoxImage.Warning);
            //    return;
            //}
            try
            {
                Expression = SymbolicExpression.Parse(EdgeLengthExpr);
            }
            catch
            {
                MessageBox.Show("input correct expression");
                return;
            }

            if (Expression.Evaluate(null).RealValue <= 0)
            {
                MessageBox.Show("Edge should have positive length", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
