using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using GraphExpectedValue.Annotations;
using MathNet.Symbolics;

namespace GraphExpectedValue.Windows
{
    public partial class EdgeParametersWindow : Window, INotifyPropertyChanged
    {
        private string _inputTitle;
        
        public EdgeParametersWindow()
        {
            InitializeComponent();
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
        
        public string EdgeLengthExpr { get; set; }
        public SymbolicExpression EdgeLength { get; set; }
        
        private void EndButton_OnClick(object sender, RoutedEventArgs e)
        {
            double len;
            try
            {
                EdgeLength = Infix.ParseOrThrow(EdgeLengthExpr);
                len = EdgeLength.Evaluate(null).RealValue;
            }
            catch
            {
                var errorMessage = "input correct expression for length";
                MessageBox.Show(errorMessage);
                return;
            }

            if (len <= 0)
            {
                MessageBox.Show("Length of edge should be positive");
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