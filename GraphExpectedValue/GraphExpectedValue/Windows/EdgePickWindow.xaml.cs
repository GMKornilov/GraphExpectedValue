using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
        private StackPanel customProbaPanel = new StackPanel()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Orientation = Orientation.Vertical,
            VerticalAlignment = VerticalAlignment.Center
        };
        private TextBlock customProbaText = new TextBlock()
        {
            Text = "Transition probability",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        private TextBox customProbaInput = new TextBox()
        {
            Width = 200,
            HorizontalAlignment = HorizontalAlignment.Center
        };

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

        public string EdgeProbabilityExpr { get; set; }

        public SymbolicExpression LengthExpression { get; set; }

        public SymbolicExpression ProbabilityExpression { get; set; }

        private bool CustomProbabilities { get; }
        public EdgePickWindow()
        {
            InitializeComponent();
            StartVertexNumber = -1;
            EndVertexNumber = -1;
        }

        public EdgePickWindow(bool customProbas) : this()
        {
            CustomProbabilities = customProbas;
            if (CustomProbabilities)
            {
                LayoutGrid.RowDefinitions.Insert(
                    3,
                    new RowDefinition()
                    {
                        Height = new GridLength(1.0, GridUnitType.Star)
                    }
                );
                customProbaPanel.Children.Add(customProbaText);
                customProbaPanel.Children.Add(customProbaInput);

                var binding = new Binding()
                {
                    ElementName = Name,
                    Path = new PropertyPath("EdgeProbabilityExpr"),
                    Mode = BindingMode.OneWayToSource
                };

                customProbaInput.SetBinding(TextBox.TextProperty, binding);

                Grid.SetRow(customProbaPanel, 4);
                Grid.SetRow(EndButton, 5);

                LayoutGrid.Children.Add(customProbaPanel);
            }
        }

        public EdgePickWindow(Func<int, int, bool> checker, bool customProbas) : this(customProbas)
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
            double len, proba = 0;
            try
            {
                LengthExpression = Infix.ParseOrThrow(EdgeLengthExpr);
                len = LengthExpression.Evaluate(null).RealValue;
                if (CustomProbabilities)
                {
                    ProbabilityExpression = Infix.ParseOrThrow(EdgeProbabilityExpr);
                    proba = ProbabilityExpression.Evaluate(null).RealValue;
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
                var errorMessage = "input correct expression for length";
                if (CustomProbabilities) errorMessage += "/probability";
                MessageBox.Show(errorMessage);
                return;
            }

            if (len <= 0)
            {
                MessageBox.Show("Edge should have positive length", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CustomProbabilities && (proba < 0 || proba > 1))
            {
                MessageBox.Show(
                    "Probabilty should be in [0;1] segment.",
                    "",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
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
