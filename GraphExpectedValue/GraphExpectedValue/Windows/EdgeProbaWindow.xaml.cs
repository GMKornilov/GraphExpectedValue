using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
using GraphExpectedValue.GraphWidgets;
using GraphExpectedValue.Utility;
using MathNet.Symbolics;

namespace GraphExpectedValue.Windows
{
    /// <summary>
    /// Interaction logic for EdgeProbaWindow.xaml
    /// </summary>
    public partial class EdgeProbaWindow : Window, INotifyPropertyChanged
    {
        private readonly ObservableCollection<Pair<int, string, Edge, bool>> _content = new ObservableCollection<Pair<int, string, Edge, bool>>();

        public ObservableCollection<Pair<int, string, Edge, bool>> Probas => _content;

        public EdgeProbaWindow()
        {
            InitializeComponent();
        }

        public EdgeProbaWindow(List<Tuple<int, Edge, bool>> neighbors) : this()
        {
            _content.CollectionChanged += ContentOnCollectionChanged; 
            foreach (var (vertexNumber, edge, backed) in neighbors)
            {
                _content.Add(new Pair<int, string, Edge, bool>(vertexNumber, "", edge, backed));
                OnPropertyChanged();
            }
        }

        private void ContentOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var expressions = new List<SymbolicExpression>();
            var realValues = new List<double>();
            try
            {
                foreach (var (_, unparsedExpression, _, _) in Probas)
                {
                    var expression = SymbolicExpression.Parse(unparsedExpression);
                    var len = expression.Evaluate(null).RealValue;
                    if (len < 0 || len > 1)
                    {
                        MessageBox.Show(
                            "All probas should be in [0;1] segment",
                            "",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        return;
                    }
                    expressions.Add(expression);
                    realValues.Add(len);
                }
            }
            catch
            {
                MessageBox.Show(
                    "All probabilities should be correct expressions",
                    "",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            var sum = realValues.Sum();
            if (Math.Abs(sum - 1) > 1e-6)
            {
                MessageBox.Show(
                    "Sum of all probabilities should be equal to 1",
                    "",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            for (var i = 0; i < Probas.Count; i++)
            {
                var edge = Probas[i].Item3;
                var isBacked = Probas[i].Item4;
                var expression = expressions[i];
                if (isBacked)
                {
                    edge.BackProbabilityExpression = expression;
                }
                else
                {
                    edge.ProbabilityExpression = expression;
                }
            }

            DialogResult = true;
        }
    }
}
