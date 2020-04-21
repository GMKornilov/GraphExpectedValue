using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GraphExpectedValue.GraphLogic;

namespace GraphExpectedValue.GraphWidgets
{
    public enum ChangeType
    {
        LineChange,
        TextChange
    }
    public class Edge
    {
        private const double TOLERANCE = 1e-6;
        private const int offset = 5;

        private EdgeMetadata metadata;

        public event Action<ChangeType> EdgeChangedEvent;

        private string text;
        private double X1, X2, Y1, Y2;

        public readonly Arrow edgeLine;
        public readonly TextBlock edgeText;

        public EdgeMetadata Metadata => metadata;

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
            }
        }

        public bool Curved { get; set; }

        public bool Backed { get; set; }

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
                if (Curved)
                {
                    TransformBezier();
                }
                else
                {
                    TransformText();
                }
                return;
            }

            edgeLine.X1 = StartPoint.X;
            edgeLine.Y1 = StartPoint.Y;
            edgeLine.X2 = EndPoint.X;
            edgeLine.Y2 = EndPoint.Y;
            edgeLine.IsCurved = Curved;
            edgeLine.IsBacked = Backed;

            if (Curved)
            {
                TransformBezier();
            }
            else
            {
                TransformText();
            }
        }

        private void TransformText()
        {
            var s = new Vector()
            {
                X = EndPoint.X - StartPoint.X,
                Y = EndPoint.Y - StartPoint.Y
            };

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

        private void TransformBezier()
        {
            var s = new Vector()
            {
                X = EndPoint.X - StartPoint.X,
                Y = EndPoint.Y - StartPoint.Y
            };
            var lineLength = s.Length;
            var pt1 = s.X > 0 ? StartPoint : EndPoint;
            
            s *= Math.Sign(s.X);
            s.Normalize();
            var perpS = new Vector(
                -s.Y,
                s.X
            );
            perpS.Normalize();
            

            var bezierPoint = edgeLine.BezierPoint;
            var midPoint = pt1 + s * lineLength / 2;

            var bezierMidVec = new Vector(bezierPoint.X - midPoint.X, bezierPoint.Y - midPoint.Y);
            var bezierMidLength = bezierMidVec.Length;

            var textWidth = TextSize.Width;
            var widthOffset = (lineLength - textWidth) / 2;

            var offsetPoint = pt1;
            var widthOffsetVector = s * widthOffset;

            var heightOffsetVector = bezierMidVec;
            heightOffsetVector.Normalize();
            var heightOffset = offset + TextSize.Height + bezierMidLength / 2.0;
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

        public void UpdateEdge() => EdgeChangedEvent?.Invoke(ChangeType.LineChange);

        private Edge()
        {
            EdgeChangedEvent += Update;
            edgeLine = new Arrow
            {
                Stroke = new SolidColorBrush(Colors.Black),
                ArrowLength = 10,
                ArrowAngle = 30,
                IsCurved = Curved
            };
            edgeText = new TextBlock();
            edgeText.Height = edgeText.FontSize + 3;
            edgeText.Width = 100;
        }

        public Edge(Vertex from, Vertex to, double val) : this()
        {
            metadata = new EdgeMetadata(from, to, val);

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

        public Edge(Vertex from, Vertex to, EdgeMetadata metadata) : this(from, to, metadata.Length)
        {
            this.metadata = metadata;
        }
    }
}
