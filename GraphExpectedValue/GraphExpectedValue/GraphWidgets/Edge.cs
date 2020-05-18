using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GraphExpectedValue.GraphLogic;
using MathNet.Symbolics;

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
        
        private const int OFFSET = 5;
        
        private event Action<ChangeType> EdgeChangedEvent;
        
        private string _text;
        
        private double X1, X2, Y1, Y2;
        
        private readonly Arrow _edgeLine;
        
        private readonly TextBlock _edgeText;

        private SymbolicExpression _lengthExpr, _probaExpr;

        private bool _showProba;
        
        public EdgeMetadata Metadata { get; }
        
        private string Text
        {
            get => _text;
            set
            {
                _text = value;
                _edgeText.Text = _text;
                EdgeChangedEvent?.Invoke(ChangeType.TextChange);
            }
        }
        public SymbolicExpression LengthExpression
        {
            get => _lengthExpr;
            set
            {
                _lengthExpr = value;
                var text = _lengthExpr.ToString();
                if (_showProba)
                {
                    text += "  /  " + _probaExpr.ToString();
                }

                Text = text;
                Metadata.Length = _lengthExpr.ToString();
            }
        }

        public SymbolicExpression ProbabilityExpression
        {
            get => _probaExpr;
            set
            {
                _probaExpr = value;
                var text = _lengthExpr.ToString();
                if (_showProba)
                {
                    text += "  /  " + _probaExpr.ToString();
                }

                Text = text;
                Metadata.Probability = _probaExpr.ToString();
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
                    new Typeface(_edgeText.FontFamily, _edgeText.FontStyle, _edgeText.FontWeight, _edgeText.FontStretch),
                    _edgeText.FontSize,
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
                //return angle;
                return angle <= 90 ? angle : 180 - angle;
            }
        }
        
        public bool Curved { get; set; }
        
        public bool Backed { get; set; }

        private void Update(ChangeType changeType)
        {
            if (changeType == ChangeType.TextChange)
            {
                _edgeText.Text = Text;
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

            _edgeLine.X1 = StartPoint.X;
            _edgeLine.Y1 = StartPoint.Y;
            _edgeLine.X2 = EndPoint.X;
            _edgeLine.Y2 = EndPoint.Y;
            _edgeLine.IsCurved = Curved;
            _edgeLine.IsBacked = Backed;

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
            var heightOffset = OFFSET + TextSize.Height;
            heightOffsetVector *= heightOffset;

            var offsetVector = heightOffsetVector + widthOffsetVector;

            var textBlockPoint = offsetPoint + offsetVector;
            Canvas.SetLeft(_edgeText, textBlockPoint.X);
            Canvas.SetTop(_edgeText, textBlockPoint.Y);

            _edgeText.RenderTransform = new RotateTransform(Angle);
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
            

            var bezierPoint = _edgeLine.BezierPoint;
            var midPoint = pt1 + s * lineLength / 2;

            var bezierMidVec = new Vector(bezierPoint.X - midPoint.X, bezierPoint.Y - midPoint.Y);
            var bezierMidLength = bezierMidVec.Length;

            var textWidth = TextSize.Width;
            var widthOffset = (lineLength - textWidth) / 2;

            var offsetPoint = pt1;
            var widthOffsetVector = s * widthOffset;

            var heightOffsetVector = bezierMidVec;
            heightOffsetVector.Normalize();
            var heightOffset = OFFSET + TextSize.Height + bezierMidLength / 2.0;
            if (pt1 == StartPoint)
            {
                heightOffset -= TextSize.Height;
            }
            heightOffsetVector *= heightOffset;

            var offsetVector = heightOffsetVector + widthOffsetVector;

            var textBlockPoint = offsetPoint + offsetVector;
            Canvas.SetLeft(_edgeText, textBlockPoint.X);
            Canvas.SetTop(_edgeText, textBlockPoint.Y);

            _edgeText.RenderTransform = new RotateTransform(Angle);
        }
        
        public void AddToCanvas(Canvas canvas)
        {
            canvas.Children.Add(_edgeLine);
            canvas.Children.Add(_edgeText);
        }
        
        public void RemoveFromCanvas(Canvas canvas)
        {
            canvas.Children.Remove(_edgeLine);
            canvas.Children.Remove(_edgeText);
        }
        
        public void UpdateEdge() => EdgeChangedEvent?.Invoke(ChangeType.LineChange);

        private Edge()
        {
            EdgeChangedEvent += Update;
            _edgeLine = new Arrow
            {
                Stroke = new SolidColorBrush(Colors.Black),
                ArrowLength = 10,
                ArrowAngle = 30,
                IsCurved = Curved
            };
            _edgeText = new TextBlock();
            _edgeText.Height = _edgeText.FontSize + 3;
            _edgeText.Width = 100;
        }

        public Edge(Vertex from, Vertex to, SymbolicExpression lengthVal) : this()
        {
            Metadata = new EdgeMetadata(from, to, lengthVal.ToString());

            var firstCenter = from.Center;
            var secondCenter = to.Center;

            var lineBetweenCenters = secondCenter - firstCenter;
            lineBetweenCenters.Normalize();
            lineBetweenCenters *= Vertex.Size;
            lineBetweenCenters /= 2;

            firstCenter += lineBetweenCenters;
            secondCenter -= lineBetweenCenters;

            Text = lengthVal.ToString();

            StartPoint = firstCenter;
            EndPoint = secondCenter;
            LengthExpression = lengthVal;
        }

        public Edge(Vertex from, Vertex to, SymbolicExpression lengthVal, SymbolicExpression probaVal) : this(from, to, lengthVal)
        {
            _showProba = true;

            Metadata = new EdgeMetadata(from, to, lengthVal.ToString(), probaVal.ToString());
            
            Text = lengthVal.ToString() + "  /  " + probaVal.ToString();
            
            ProbabilityExpression = probaVal;
        }

        public Edge(Vertex from, Vertex to, EdgeMetadata metadata, bool customProba) : this(from, to, SymbolicExpression.Parse(metadata.Length))
        {
            this.Metadata = metadata;
            if (customProba)
            {
                _showProba = true;
                ProbabilityExpression = SymbolicExpression.Parse(Metadata.Probability);
            }
        }
    }
}
