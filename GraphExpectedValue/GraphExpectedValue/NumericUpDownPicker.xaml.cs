using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GraphExpectedValue
{
    public partial class NumericUpDownPicker : UserControl
    {
        public static readonly DependencyProperty MaximumProperty;
        public static readonly DependencyProperty MinimumProperty;
        public static readonly DependencyProperty ValueProperty;
        public static readonly DependencyProperty DoubleAllowedProperty;

        public delegate bool Parser(string input, out double result);

        private Parser _parser = (string s, out double res) =>
        {
            var success = int.TryParse(s, out var r);
            res = r;
            return success;
        };

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set
            {
                SetValue(MaximumProperty, value);
                if (Value > Maximum)
                {
                    Value = Maximum;
                }
            }
        }

        public double Minimum
        {
            get => (double)GetValue(MinimumProperty);
            set
            {
                SetValue(MinimumProperty, value);
                if (Value < Minimum)
                {
                    Value = Minimum;
                }
            }
        }

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set
            {
                if (Minimum > value || Maximum < value) return;
                TextBoxValue.Text = value.ToString(CultureInfo.CurrentCulture);
                SetValue(ValueProperty, value);
            }
        }

        public bool DoubleAllowed
        {
            get => (bool) GetValue(DoubleAllowedProperty);
            set => SetValue(DoubleAllowedProperty, value);
        }

        static NumericUpDownPicker()
        {
            MaximumProperty = DependencyProperty.Register(
                "Maximum",
                typeof(double),
                typeof(NumericUpDownPicker),
                new UIPropertyMetadata(double.PositiveInfinity)
            );
            MinimumProperty = DependencyProperty.Register(
                "Minimum",
                typeof(double),
                typeof(NumericUpDownPicker),
                new UIPropertyMetadata(default(double))
            );
            ValueProperty = DependencyProperty.Register(
                "Value",
                typeof(double),
                typeof(NumericUpDownPicker),
                new PropertyMetadata(
                    default(double),
                    OnValueChanged
                )
            );
            DoubleAllowedProperty = DependencyProperty.Register(
                "DoubleAllowed",
                typeof(bool),
                typeof(NumericUpDownPicker),
                new UIPropertyMetadata(
                    false,
                    OnDoubleAllowedChanged
                )
            );
        }
        public NumericUpDownPicker()
        {
            InitializeComponent();
        }

        public NumericUpDownPicker(int minimum, int maximum) : this()
        {
            Minimum = minimum;
            Maximum = maximum;

            Value = 0 < Minimum ? Minimum : 0;
        }

        private void ResetText(TextBox textBox)
        {
            textBox.Text = 0 < Minimum ? Minimum.ToString() : "0";

            textBox.SelectAll();

            Value = 0 < Minimum ? Minimum : 0;
        }

        private void TextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;

            double newValue;

            if (DoubleAllowed && (textBox.Text.EndsWith(",") || textBox.Text.EndsWith(".")))
            {
                var copy = textBox.Text.Substring(0, textBox.Text.Length - 1);
                if (!_parser(copy, out newValue))
                {
                    ResetText(textBox);
                }
                return;
            }

            if (!_parser(textBox.Text, out newValue) && !textBox.Text.Equals("-"))
            {
                ResetText(textBox);
                return;
            }
            if (newValue < Minimum) Value = Minimum;
            else if (newValue > Maximum) Value = Maximum;
            else Value = newValue;
        }

        private void TextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(e.IsDown) return;

            switch (e.Key)
            {
                case Key.Up:
                    Value++;
                    break;
                case Key.Down:
                    Value--;
                    break;
            }
        }

        private void IncreaseButtonClick(object sender, RoutedEventArgs e) => Value++;

        private void DecreaseButtonClick(object sender, RoutedEventArgs e) => Value--;

        private static void OnValueChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            if (target is NumericUpDownPicker numController)
            {
                numController.TextBoxValue.Text = e.NewValue.ToString();
            }
        }

        private static void OnDoubleAllowedChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            var numController = target as NumericUpDownPicker;
            if ((bool) e.NewValue)
            {
                numController._parser = double.TryParse;
            }
            else
            {
                numController._parser = (string s, out double res) =>
                {
                    var success = int.TryParse(s, out var r);
                    res = r;
                    return success;
                };
            }
        }
    }
}
