﻿using System;
using System.Collections.Generic;
using System.Linq;
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

namespace GraphExpectedValue.Windows
{
    /// <summary>
    /// Interaction logic for GraphCreateWindow.xaml
    /// </summary>
    public partial class GraphCreateWindow : Window
    {
        public GraphCreateWindow()
        {
            InitializeComponent();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if (CustomProbasCheckBox.IsChecked == null)
            {
                return;
            }

            var selectedItem = (GraphTypeComboBox.SelectedItem as TextBlock)?.Text;
            if (selectedItem != "Digraph" && selectedItem != "Unoriented graph")
            {
                return;
            }

            DialogResult = true;
        }
    }
}