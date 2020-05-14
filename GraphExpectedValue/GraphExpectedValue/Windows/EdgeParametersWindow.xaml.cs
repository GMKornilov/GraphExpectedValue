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
using MathNet.Symbolics;

namespace GraphExpectedValue.Windows
{
    /// <summary>
    /// Interaction logic for EdgeParametersWindow.xaml
    /// </summary>
    public partial class EdgeParametersWindow : Window, INotifyPropertyChanged
    {
        private string _inputTitle;
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

        public EdgeParametersWindow()
        {
            InitializeComponent();
        }

        public EdgeParametersWindow(bool customProbas) : this()
        {
            CustomProbabilites = customProbas;
            if (customProbas)
            {
                LayoutGrid.RowDefinitions.Insert(
                    2,
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

                Grid.SetRow(customProbaPanel, 2);
                Grid.SetRow(EndButton, 3);

                LayoutGrid.Children.Add(customProbaPanel);
            }
        }

        public string InputTitle
        {
            get => _inputTitle;
            set
            {
                _inputTitle = value;
                OnPropertyChanged();
            }
        }
        
        private bool CustomProbabilites { get; }

        public string EdgeLengthExpr { get; set; }
        public SymbolicExpression EdgeLength { get; set; }
        public string EdgeProbaExpr { get; set; }
        public SymbolicExpression EdgeProba { get; set; }

        private void EndButton_OnClick(object sender, RoutedEventArgs e)
        {
            double len, proba = 0;
            try
            {
                EdgeLength = Infix.ParseOrThrow(EdgeLengthExpr);
                len = EdgeLength.Evaluate(null).RealValue;
                if (CustomProbabilites)
                {
                    EdgeProba = Infix.ParseOrThrow(EdgeProbaExpr);
                    proba = EdgeProba.Evaluate(null).RealValue;
                }
            }
            catch
            {
                var errorMessage = "input correct expression for length";
                if (CustomProbabilites) errorMessage += "/probability";
                MessageBox.Show(errorMessage);
                return;
            }

            if (len <= 0)
            {
                MessageBox.Show("Length should be positive");
                return;
            }

            if (CustomProbabilites && (proba < 0 || proba > 1))
            {
                MessageBox.Show("Probability should be in [0;1] segment");
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
