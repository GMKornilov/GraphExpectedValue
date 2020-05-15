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
                MessageBox.Show("input correct expression for length");
                return;
            }

            if (len <= 0)
            {
                MessageBox.Show("Length should be positive");
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
