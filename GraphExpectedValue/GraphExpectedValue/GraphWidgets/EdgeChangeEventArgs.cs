using System;
using System.Windows;

namespace GraphExpectedValue.GraphWidgets
{
    public class EdgeChangeEventArgs : EventArgs
    {
        public string Text { get; private set; }
        public Point StartPoint { get; private set; }
        public Point EndPoint { get; private set; }

        public EdgeChangeEventArgs(Point startPoint, Point endPoint, string text)
        {
            Text = text;
            StartPoint = startPoint;
            EndPoint = endPoint;
        }
    }
}