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

namespace GraphExpectedValue.Windows
{ 
    /// <summary>
    /// Interaction logic for VertexChooseWindow.xaml
    /// </summary>
    public partial class VertexChooseWindow : Window, INotifyPropertyChanged
    {
        private int _totalVertexes;
        private string _confirmButtonText, _prompt;
        private Func<int, bool> _checker;

        public string Prompt
        {
            get => _prompt;
            set
            {
                _prompt = value;
                OnPropertyChanged(nameof(Prompt));
            }
        }

        public string ConfirmButtonText
        {
            get => _confirmButtonText;
            set
            {
                _confirmButtonText = value;
                OnPropertyChanged(nameof(ConfirmButtonText));
            }
        }
        public int TotalVertexes
        {
            get => _totalVertexes;
            set
            {
                _totalVertexes = value;
                OnPropertyChanged(nameof(TotalVertexes));
            }
        }

        public int ChosenVertex { get; set; }

        public void AddChecker(Func<int, bool> checker)
        {
            _checker += checker;
        }
        public VertexChooseWindow()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (ChosenVertex < 1 || ChosenVertex > TotalVertexes)
            {
                MessageBox.Show(
                    $"Vertex should be positive number between 0 and {TotalVertexes}",
                    "",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            if (_checker?.Invoke(ChosenVertex) == true || _checker == null)
            {
                DialogResult = true;
            }
        }
    }
}
