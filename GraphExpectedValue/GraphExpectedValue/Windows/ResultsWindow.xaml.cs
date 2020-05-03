using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
    /// Interaction logic for ResultsWindow.xaml
    /// </summary>
    public partial class ResultsWindow : Window, INotifyPropertyChanged
    {
        private string _elapsedTime;


        public string ElapsedTime
        {
            get => _elapsedTime;
            set
            {
                _elapsedTime = value;
                OnPropertyChanged();
            }
        }
        public ResultsWindow()
        {
            InitializeComponent();
        }

        public ResultsWindow(List<Tuple<int, double>> calcResults, string elapsedTime) : this()
        {
            ProcessResults(calcResults);
            ElapsedTime = elapsedTime;
        }

        private void ProcessResults(List<Tuple<int, double>> results)
        {
            for(var i = 0; i < results.Count - 1; i++)
            {
                ProcessResult(results[i]);
            }
            ProcessResult(results[results.Count - 1], false);
        }

        private void ProcessResult(Tuple<int, double> result, bool addLineBreak = true)
        {
            resultTextBlock.Inlines.Add("T");
            var run = new Run(result.Item1.ToString());
            run.Typography.Variants = FontVariants.Subscript;
            //Debug.WriteLine(run.FontSize);
            run.FontSize = 18;
            resultTextBlock.Inlines.Add(run);
            resultTextBlock.Inlines.Add($" = {result.Item2}");
            if (addLineBreak)
            {
                resultTextBlock.Inlines.Add(new LineBreak());
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e) => DialogResult = true;
    }
}
