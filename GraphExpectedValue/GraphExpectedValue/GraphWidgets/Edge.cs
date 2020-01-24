using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GraphExpectedValue.GraphWidgets
{
    internal enum ChangeType
    {
        LineChange,
        TextChange
    }
    public class Edge
    {
        private const int offset = 5;

        private event Action<ChangeType> EdgeChangedEvent;

        private string text;
        private double X1, X2, Y1, Y2;

        public readonly Line edgeLine;
        public readonly TextBlock edgeText;

        public string Text
        {
            get => text;
            private set
            {
                text = value;
                edgeText.Text = text;
                EdgeChangedEvent?.Invoke(ChangeType.TextChange);
            }
        }

        public Point StartPoint
        {
            get => new Point(X1, Y1);
            private set
            {
                X1 = value.X;
                Y1 = value.Y;
                EdgeChangedEvent?.Invoke(ChangeType.LineChange);
            }
        }

        public Point EndPoint
        {
            get => new Point(X2, Y2);
            private set
            {
                X2 = value.X;
                Y2 = value.Y;
                EdgeChangedEvent?.Invoke(ChangeType.LineChange);
            }
        }

        private double Angle
        {
            get
            {
                var a = EndPoint - StartPoint;
                return Math.Atan2(a.Y, a.X) / Math.PI * 180;
            }
        }

        public Edge(Point from, Point to, double val):this()
        {
            Text = val.ToString(CultureInfo.CurrentCulture);
            StartPoint = from;
            EndPoint = to;
        }

        private void Update(ChangeType changeType)
        {
            if (changeType == ChangeType.TextChange)
            {
                edgeText.Text = Text;
                TransformText();
                return;
            }

            edgeLine.X1 = StartPoint.X;
            edgeLine.Y1 = StartPoint.Y;
            edgeLine.X2 = EndPoint.X;
            edgeLine.Y2 = EndPoint.Y;
              
            TransformText();
        }

        private void TransformText()
        {
            double xOffset, yOffset;

            var textWidth = edgeText.ActualWidth;
            var line = EndPoint - StartPoint;

            xOffset = line.X / 2;
            yOffset = -offset - edgeText.Height;

            Canvas.SetLeft(edgeText, StartPoint.X + xOffset);
            Canvas.SetTop(edgeText, StartPoint.Y + yOffset);

            edgeText.RenderTransform = new RotateTransform(Angle);
        }

        public void AddToCanvas(Canvas canvas)
        {
            canvas.Children.Add(edgeLine);
            canvas.Children.Add(edgeText);
        }

        public void RemoveFromCanvas(Canvas canvas)
        {
            canvas.Children.Remove(edgeLine);
            canvas.Children.Remove(edgeText);
        }

        private Edge()
        {
            EdgeChangedEvent += Update;
            edgeLine = new Line {Stroke = new SolidColorBrush(Colors.Black)};
            edgeText = new TextBlock();
            edgeText.Height = edgeText.FontSize + 3;
        }

        public Edge(Vertex from, Vertex to, double val) : this()
        {
            var firstCenter = from.Center;
            var secondCenter = to.Center;

            var lineBetweenCenters = secondCenter - firstCenter;
            lineBetweenCenters.Normalize();
            lineBetweenCenters *= Vertex.Size;
            lineBetweenCenters /= 2;

            firstCenter += lineBetweenCenters;
            secondCenter -= lineBetweenCenters;

            Text = val.ToString(CultureInfo.CurrentCulture);
            StartPoint = firstCenter;
            EndPoint = secondCenter;
        }
    }
}
