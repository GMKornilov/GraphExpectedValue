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
    public partial class EdgePickWindow : Window, INotifyPropertyChanged
    {
        private int _totalVertexes;

        private Func<int, int, bool> _checker;

        private readonly StackPanel _customProbaPanel = new StackPanel()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Orientation = Orientation.Vertical,
            VerticalAlignment = VerticalAlignment.Center
        };

        private readonly TextBlock _customProbaText = new TextBlock()
        {
            Text = "Transition probability",
            HorizontalAlignment = HorizontalAlignment.Center
        };

        private readonly TextBox _customProbaInput = new TextBox()
        {
            Width = 200,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment =  TextAlignment.Right
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

        private EdgePickWindow()
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
                var inputTooltip = FindResource("inputTooltip") as ToolTip;

                LayoutGrid.RowDefinitions.Insert(
                    3,
                    new RowDefinition()
                    {
                        Height = new GridLength(1.0, GridUnitType.Star)
                    }
                );
                _customProbaPanel.ToolTip = inputTooltip;
                _customProbaPanel.Children.Add(_customProbaText);
                _customProbaPanel.Children.Add(_customProbaInput);

                var binding = new Binding()
                {
                    ElementName = Name,
                    Path = new PropertyPath("EdgeProbabilityExpr"),
                    Mode = BindingMode.OneWayToSource
                };

                _customProbaInput.SetBinding(TextBox.TextProperty, binding);

                Grid.SetRow(_customProbaPanel, 4);
                Grid.SetRow(EndButton, 5);

                LayoutGrid.Children.Add(_customProbaPanel);
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

            if (Math.Abs(len) > 1E10)
            {
                MessageBox.Show("Value too big", "", MessageBoxButton.OK, MessageBoxImage.Warning);
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
