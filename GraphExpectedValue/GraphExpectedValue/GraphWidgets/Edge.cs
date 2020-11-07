using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GraphExpectedValue.GraphLogic;
using MathNet.Symbolics;

namespace GraphExpectedValue.GraphWidgets
{
    [Flags]
    public enum ChangeType
    {
        LineChange = 0,
        LengthTextChange = (1 << 0),
        BackLengthTextChange = (1 << 1),
        ProbaTextChange = (1 << 2),
        BackProbaTextChange = (1 << 3),
        CurvedChanged = (1 << 4),
        BackedChanged = (1 << 5)
    }
    
    public class Edge
    {
        private const double TOLERANCE = 1e-6;
        
        private const int OFFSET = 5;
        
        private event Action<ChangeType> EdgeChangedEvent;

        private string _lengthText;
        private string _backLengthText;
        private string _probaText;
        private string _backProbaText;
        
        private double X1, X2, Y1, Y2;
        
        private readonly Arrow _edgeLine;

        private readonly TextBlock _edgeLengthText;
        private readonly TextBlock _edgeBackLengthText;

        private readonly TextBlock _edgeProbaText;
        private readonly TextBlock _edgeBackProbaText;

        private SymbolicExpression _lengthExpr, _backLengthExpr, _probaExpr, _backProbaExpr;

        private bool _showProba;
        private bool _showBackProba;
        private bool _showBackLength;

        private bool _backed;
        private bool _curved;

        private Canvas _canvas;
        
        public EdgeMetadata Metadata { get; }
        
        public string LengthText
        {
            get => _lengthText;
            private set
            {
                _lengthText = value;
                _edgeLengthText.Text = _lengthText;

                EdgeChangedEvent?.Invoke(ChangeType.LengthTextChange);
            }
        }

        public string BackLengthText
        {
            get => _backLengthText;
            private set
            {
                _backLengthText = value;
                _edgeBackLengthText.Text = _backLengthText;

                if (!_showBackLength && _canvas != null)
                {
                    _showBackLength = true;
                    _canvas.Children.Add(_edgeBackLengthText);
                }

                EdgeChangedEvent?.Invoke(ChangeType.BackLengthTextChange);
            }
        }

        public string ProbaText
        {
            get => _probaText;
            private set
            {
                _probaText = value;
                _edgeProbaText.Text = _probaText;

                if(!_showProba && _canvas != null)
                {
                    _showProba = true;
                    _canvas.Children.Add(_edgeProbaText);
                }

                EdgeChangedEvent?.Invoke(ChangeType.ProbaTextChange);
            }
        }

        public string BackProbaText
        {
            get => _backProbaText;
            private set
            {
                _backProbaText = value;
                _edgeBackProbaText.Text = _backProbaText;

                if (value == null && _showBackProba && _canvas != null)
                {
                    _showBackProba = false;
                    _canvas.Children.Remove(_edgeBackProbaText);
                    return;
                }

                if(!_showBackProba && _canvas != null)
                {
                    _showBackProba = true;
                    _canvas.Children.Add(_edgeBackProbaText);
                }

                EdgeChangedEvent?.Invoke(ChangeType.BackProbaTextChange);
            }
        }
        public SymbolicExpression LengthExpression
        {
            get => _lengthExpr;
            set
            {
                _lengthExpr = value;
                LengthText = _lengthExpr.ToString();
                Metadata.Length = _lengthExpr.ToString();
            }
        }

        public SymbolicExpression BackLengthExpression
        {
            get => _backLengthExpr;
            set
            {
                _backLengthExpr = value;
                BackLengthText = _backLengthExpr?.ToString();
                Metadata.BackLength = BackLengthText;
            }
        }

        public SymbolicExpression ProbabilityExpression
        {
            get => _probaExpr;
            set
            {
                _probaExpr = value;
                ProbaText = _probaExpr.ToString();
                Metadata.Probability = _probaExpr.ToString();
            }
        }

        public SymbolicExpression BackProbabilityExpression
        {
            get => _backProbaExpr;
            set
            {
                _backProbaExpr = value;
                BackProbaText = _backProbaExpr?.ToString();
                Metadata.BackProbability = BackProbaText;
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
        
        private Size TextSize(TextBlock textBlock)
        {
            var formattedText = new FormattedText(
                    textBlock.Text,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
                    textBlock.FontSize,
                    Brushes.Black,
                    new NumberSubstitution(),
                    1
                );
            return new Size(formattedText.Width, formattedText.Height);
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
        
        public bool Curved 
        {
            get => _curved;
            set
            {
                _curved = value;
                EdgeChangedEvent?.Invoke(ChangeType.CurvedChanged);
            }
        }
       
        public bool Backed 
        {
            get => _backed;
            set
            {
                _backed = value;
                EdgeChangedEvent?.Invoke(ChangeType.BackedChanged);
            }
        }

        private void Update(ChangeType changeType)
        {
            _edgeLine.X1 = StartPoint.X;
            _edgeLine.Y1 = StartPoint.Y;
            _edgeLine.X2 = EndPoint.X;
            _edgeLine.Y2 = EndPoint.Y;
            _edgeLine.IsCurved = Curved;
            _edgeLine.IsBacked = Backed;

            switch(changeType)
            {
                case ChangeType.LineChange:
                    _edgeLine.X1 = StartPoint.X;
                    _edgeLine.Y1 = StartPoint.Y;
                    _edgeLine.X2 = EndPoint.X;
                    _edgeLine.Y2 = EndPoint.Y;
                    Transform(changeType | ChangeType.LengthTextChange, _edgeLengthText);
                    Transform(changeType | ChangeType.ProbaTextChange, _edgeProbaText);
                    Transform(changeType | ChangeType.BackProbaTextChange, _edgeBackProbaText);
                    break;
                case ChangeType.BackedChanged:
                    _edgeLine.IsBacked = Backed;
                    Transform(changeType | ChangeType.LengthTextChange, _edgeLengthText);
                    break;
                case ChangeType.CurvedChanged:
                    _edgeLine.IsCurved = Curved;
                    Transform(changeType | ChangeType.LengthTextChange, _edgeLengthText);
                    Transform(changeType | ChangeType.ProbaTextChange, _edgeProbaText);
                    Transform(changeType | ChangeType.BackProbaTextChange, _edgeBackProbaText);
                    break;
                case ChangeType.LengthTextChange:
                    //Transform(changeType | ChangeType.ProbaTextChange, _edgeProbaText);
                    Transform(changeType, _edgeLengthText);
                    break;
                case ChangeType.BackLengthTextChange:
                    Transform(changeType, _edgeBackLengthText);
                    break;
                case ChangeType.ProbaTextChange:
                    Transform(changeType, _edgeProbaText);
                    if (Backed)
                    {
                        Transform(ChangeType.LengthTextChange, _edgeLengthText);
                    }
                    break;
                case ChangeType.BackProbaTextChange:
                    Transform(changeType, _edgeBackProbaText);
                    Transform(ChangeType.BackLengthTextChange, _edgeBackLengthText);
                    break;
            }
        }

        private void Transform(ChangeType changeType, TextBlock textBlock)
        {
            var s = EndPoint - StartPoint;

            var textSize = TextSize(textBlock);
            var textWidth = textSize.Width;
            var textHeight = textSize.Height;

            var lineWidth = s.Length;
            
            s.Normalize();
            var widthOffset = (lineWidth - textWidth) / 2;

            var offsetPoint = s.X > 0 ? StartPoint : EndPoint;
            if (Math.Abs(s.X) > TOLERANCE)
            {
                s *= Math.Sign(s.X);
            }
            else
            {
                offsetPoint = StartPoint;
            }


            var perpS = new Vector(
                s.Y,
                -s.X
            );

            Vector widthOffsetVector, heightOffsetVector, offsetVector;
            double heightOffset, angle;
            var matrix = new Matrix();
            if (changeType.HasFlag(ChangeType.ProbaTextChange))
            {
                offsetPoint = StartPoint;
                s = EndPoint - StartPoint;
                s.Normalize();
                perpS = new Vector(
                    -s.Y,
                    s.X
                );

                widthOffset = 3;
                if (s.X < 0)
                {
                    widthOffset += textWidth;
                }
                widthOffsetVector = widthOffset * s;
                perpS.Normalize();
                
                heightOffset = OFFSET + textHeight;
                if (perpS.Y > 0)
                {
                    heightOffset -= textHeight;
                }
                heightOffsetVector = perpS * heightOffset;

                offsetVector = widthOffsetVector + heightOffsetVector;
                angle = Angle;
                if(Curved)
                {
                    //offsetVector = widthOffsetVector - heightOffsetVector;
                    angle += Arrow.BezierAngle;
                    matrix.Rotate(Arrow.BezierAngle);
                    offsetVector = Vector.Multiply(offsetVector, matrix);
                }
            }
            else if (changeType.HasFlag(ChangeType.BackProbaTextChange))
            {
                offsetPoint = EndPoint;
                s = StartPoint - EndPoint;
                s.Normalize();
                //offsetPoint = (offsetPoint == StartPoint) ? EndPoint : StartPoint;

                //s *= -1;
                widthOffset = 3 + textWidth;
                if (s.X >= 0)
                {
                    widthOffset -= textWidth;
                }

                widthOffsetVector = widthOffset * s;

                heightOffset = OFFSET + textHeight;
                if (perpS.Y > 0)
                {
                    heightOffset -= textHeight;
                }
                heightOffsetVector = perpS * heightOffset;

                offsetVector = widthOffsetVector + heightOffsetVector;

                angle = Angle;
                if (Curved)
                {
                    angle -= Angle;
                    matrix.Rotate(-Arrow.BezierAngle);
                    offsetVector = Vector.Multiply(offsetVector, matrix);
                }
            }
            else if (changeType.HasFlag(ChangeType.LengthTextChange) && Backed)
            {
                offsetPoint = StartPoint;
                s = EndPoint - StartPoint;
                s.Normalize();
                perpS = new Vector(
                    -s.Y,
                    s.X
                );

                widthOffset = 3 + OFFSET + TextSize(_edgeProbaText).Width;
                if (s.X < 0)
                {
                    widthOffset += textWidth;
                }
                widthOffsetVector = widthOffset * s;
                perpS.Normalize();

                heightOffset = OFFSET + textHeight;
                if (perpS.Y > 0)
                {
                    heightOffset -= textHeight;
                }
                heightOffsetVector = perpS * heightOffset;

                offsetVector = widthOffsetVector + heightOffsetVector;
                angle = Angle;
                if (Curved)
                {
                    //offsetVector = widthOffsetVector - heightOffsetVector;
                    angle += Arrow.BezierAngle;
                    matrix.Rotate(Arrow.BezierAngle);
                    offsetVector = Vector.Multiply(offsetVector, matrix);
                }
            }
            else if (changeType.HasFlag(ChangeType.BackLengthTextChange))
            {
                offsetPoint = EndPoint;
                s = StartPoint - EndPoint;
                s.Normalize();
                //s *= -1;

                var backProbaWidth = TextSize(_edgeBackProbaText).Width;
                widthOffset = 3 + textWidth + OFFSET + backProbaWidth;
                if (s.X >= 0)
                {
                    widthOffset -= textWidth;
                }
                widthOffsetVector = widthOffset * s;

                heightOffset = OFFSET + textHeight;
                if (perpS.Y > 0)
                {
                    heightOffset -= textHeight;
                }
                heightOffsetVector = perpS * heightOffset;

                offsetVector = widthOffsetVector + heightOffsetVector;

                angle = Angle;
                if (Curved)
                {
                    angle -= Angle;
                    matrix.Rotate(-Arrow.BezierAngle);
                    offsetVector = Vector.Multiply(offsetVector, matrix);
                }
            }
            else
            {
                widthOffsetVector = widthOffset * s;
                if (Curved)
                {
                    var bezierPoint = _edgeLine.BezierPoint;
                    var midPoint = offsetPoint + s * (lineWidth / 2);
                    var bezierMidVec = bezierPoint - midPoint;
                    var bezierMidLength = bezierMidVec.Length;
                    bezierMidVec.Normalize();
                    heightOffset = OFFSET + textHeight + bezierMidLength / 2.0;
                    if (offsetPoint == StartPoint && Math.Abs(s.X) > TOLERANCE)
                    {
                        heightOffset -= textHeight;
                    }
                    heightOffsetVector = bezierMidVec * heightOffset;
                }
                else
                {
                    heightOffset = OFFSET + textHeight;
                    heightOffsetVector = perpS * heightOffset;
                }
                offsetVector = widthOffsetVector + heightOffsetVector;

                angle = Angle;
            }

            var textBlockPoint = offsetPoint + offsetVector;
            Canvas.SetLeft(textBlock, textBlockPoint.X);
            Canvas.SetTop(textBlock, textBlockPoint.Y);
            textBlock.RenderTransform = new RotateTransform(angle);
        }
                
        public void AddToCanvas(Canvas canvas)
        {
            _canvas = canvas;

            canvas.Children.Add(_edgeLine);
            canvas.Children.Add(_edgeLengthText);

            if (!_showProba)
            {
                _showProba = true;
                canvas.Children.Add(_edgeProbaText);
            }

            if (!_showBackProba)
            {
                _showBackProba = true;
                canvas.Children.Add(_edgeBackProbaText);
            }
        }
        
        public void RemoveFromCanvas(Canvas canvas)
        {
            _canvas.Children.Remove(_edgeLine);
            _canvas.Children.Remove(_edgeLengthText);
            if (_showProba)
            {
                _canvas.Children.Remove(_edgeProbaText);
            }

            if (_showBackProba)
            {
                _canvas.Children.Remove(_edgeBackProbaText);
            }

            if (_showBackLength)
            {
                _canvas.Children.Remove(_edgeBackLengthText);
            }
        }
        
        public void UpdateEdge() => EdgeChangedEvent?.Invoke(ChangeType.LineChange);


        public void UpdatedDegree(int degree, bool isBack)
        {
            var proba = SymbolicExpression.Parse("1 / " + degree);
            if (isBack)
            {
                BackProbabilityExpression = proba;
            }
            else
            {
                ProbabilityExpression = proba;
            }
        }

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
            _edgeLengthText = new TextBlock();
            _edgeLengthText.Height = _edgeLengthText.FontSize + 3;

            _edgeBackLengthText = new TextBlock();
            _edgeBackLengthText.Height = _edgeLengthText.FontSize + 3;

            _edgeProbaText = new TextBlock();
            _edgeProbaText.Height = _edgeProbaText.FontSize + 3;
            _edgeProbaText.Foreground = Brushes.Red;

            _edgeBackProbaText = new TextBlock();
            _edgeBackProbaText.Height = _edgeBackProbaText.FontSize + 3;
            _edgeBackProbaText.Foreground = Brushes.Red;
        }

        public Edge(Vertex from, Vertex to, SymbolicExpression lengthVal) : this()
        {
            from.PropertyChanged += (sender, e) =>
            {
                Metadata.StartVertexNumber = from.Number;
            };
            to.PropertyChanged += (sender, e) =>
            {
                Metadata.EndVertexNumber = to.Number;
            };

            Metadata = new EdgeMetadata(from, to, lengthVal.ToString());

            var firstCenter = from.Center;
            var secondCenter = to.Center;

            var lineBetweenCenters = secondCenter - firstCenter;
            lineBetweenCenters.Normalize();
            lineBetweenCenters *= Vertex.Size;
            lineBetweenCenters /= 2;

            firstCenter += lineBetweenCenters;
            secondCenter -= lineBetweenCenters;

            LengthExpression = lengthVal;

            StartPoint = firstCenter;
            EndPoint = secondCenter;
        }


        public Edge(Vertex from, Vertex to, EdgeMetadata metadata) : this(from, to, SymbolicExpression.Parse(metadata.Length))
        {
            Metadata = metadata;
        }
    }
}
