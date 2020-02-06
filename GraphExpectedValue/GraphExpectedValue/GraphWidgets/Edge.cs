using System;
using System.Globalization;
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
        private const double TOLERANCE = 1e-6;
        private const int offset = 5;

        private event Action<ChangeType> EdgeChangedEvent;

        private string text;
        private double X1, X2, Y1, Y2;

        //public readonly Line edgeLine;
        public readonly Arrow edgeLine;
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

        private Size TextSize
        {
            get
            {
                var formattedText = new FormattedText(
                    Text,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(edgeText.FontFamily, edgeText.FontStyle, edgeText.FontWeight, edgeText.FontStretch),
                    edgeText.FontSize,
                    Brushes.Black,
                    new NumberSubstitution(),
                    1
                );
                return new Size(formattedText.Width, formattedText.Height);
            }
        }

        private double Angle
        {
            get
            {
                var a = EndPoint - StartPoint;
                if (Math.Abs(a.X) > TOLERANCE)
                {
                    a *= Math.Sign(a.X);
                }
                var angle = Math.Atan2(a.Y, a.X) / Math.PI * 180;
                return angle <= 90 ? angle : 180 - angle;
                //return angle;
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
            var s = new Vector(){X = EndPoint.X - StartPoint.X, Y = EndPoint.Y - StartPoint.Y};

            var textWidth = TextSize.Width;
            var lineLength = s.Length;
            var widthOffset = (lineLength - textWidth) / 2;

            var offsetPoint = s.X > 0 ? StartPoint : EndPoint;

            if (Math.Abs(s.X) > TOLERANCE)
            {
                s *= Math.Sign(s.X);
            }
            else
            {
                offsetPoint = StartPoint;
            }

            s.Normalize();

            var widthOffsetVector = s * widthOffset;

            var heightOffsetVector = new Vector {X = s.Y, Y = -s.X};
            heightOffsetVector.Normalize();
            var heightOffset = offset + TextSize.Height;
            heightOffsetVector *= heightOffset;

            var offsetVector = heightOffsetVector + widthOffsetVector;

            var textBlockPoint = offsetPoint + offsetVector;
            Canvas.SetLeft(edgeText, textBlockPoint.X);
            Canvas.SetTop(edgeText, textBlockPoint.Y);

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
            edgeLine = new Arrow {Stroke = new SolidColorBrush(Colors.Black), ArrowLength=10};
            edgeText = new TextBlock();
            edgeText.Height = edgeText.FontSize + 3;
            edgeText.Width = 100;
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
