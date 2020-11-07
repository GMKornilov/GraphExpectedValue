using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using GraphExpectedValue.Utility;
using MathNet.Symbolics;

namespace GraphExpectedValue.Windows
{
    public partial class ProbaInputWindow : Window
    {
        public ObservableCollection<MutableTuple<int, string, bool>> probasExpressions = new ObservableCollection<MutableTuple<int, string, bool>>();
        
        public List<Tuple<int, SymbolicExpression, bool>> probas = new List<Tuple<int, SymbolicExpression, bool>>();
        
        public ProbaInputWindow()
        {
            InitializeComponent();
        }

        public ProbaInputWindow(List<Tuple<int, bool>> vertexNumbers) : this()
        {
            foreach (var vertexNumber in vertexNumbers)
            {
                probasExpressions.Add(new MutableTuple<int, string, bool>(vertexNumber.Item1, "", vertexNumber.Item2));
            }

            dgProbas.ItemsSource = probasExpressions;
        }

        private void SubmitButton_OnClick(object sender, RoutedEventArgs e)
        {
            probas = new List<Tuple<int, SymbolicExpression, bool>>();
            var probasSum = SymbolicExpression.Zero;
            foreach (var (vertexNumber, probasStr, isBacked) in probasExpressions)
            {
                if (probasStr == "")
                {
                    probas.Add(new Tuple<int, SymbolicExpression, bool>(vertexNumber, SymbolicExpression.Zero, isBacked));
                    continue;
                }
                try
                {
                    SymbolicExpression proba = Infix.ParseOrThrow(probasStr);
                    var realVal = proba.Evaluate(null).RealValue;
                    if (realVal < 0 || realVal > 1)
                    {
                        MessageBox.Show(
                            "Probability should be in [0;1] segment"
                        );
                        return;
                    }

                    probasSum += proba;
                    probas.Add(new Tuple<int, SymbolicExpression, bool>(vertexNumber, proba, isBacked));
                }
                catch
                {
                    MessageBox.Show(
                        "Input valid probability"
                    );
                    return;
                }
            }

            var probasSumRealValue = probasSum.Evaluate(null).RealValue; 

            if (Math.Abs(probasSumRealValue - 1.0) > 1e7)
            {
                MessageBox.Show(
                    "Sum of probabilities should be equal to one"
                );
                return;
            }

            DialogResult = true;
        }
    }
}