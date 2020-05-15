using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Mime;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GraphExpectedValue.GraphLogic;
using MathNet.Symbolics;

namespace GraphExpectedValue.GraphWidgets
{
    [Flags]
    public enum ChangeType
    {
        LineChange = 1,
        LengthTextChange = (1 << 1),
        ProbaTextChange = (1 << 2),
        BackProbaTextChange = (1 << 3),
        CurvedChanged = (1 << 4),
        BackedChanged = (1 << 5)
    }
    /// <summary>
    /// Графическое представление ребра графа
    /// </summary>
    public class Edge
    {
        /// <summary>
        /// Константа, представляющая погрешность при сравенении вещественных чисел.
        /// </summary>
        private const double TOLERANCE = 1e-6;
        /// <summary>
        /// Отступ текста от линии ребра
        /// </summary>
        private const int offset = 5;
        /// <summary>
        /// Событие, вызываемое при изменеии свойств ребра
        /// </summary>
        public event Action<ChangeType> EdgeChangedEvent;
        /// <summary>
        /// Текст, который должен быть написан на ребре
        /// </summary>
        private string _lengthText;

        private string _probaText;

        private string _backProbaText;
        /// <summary>
        /// X и Y координаты начальной и конечной точки ребра
        /// </summary>
        private double X1, X2, Y1, Y2;
        /// <summary>
        /// Стрелка, являющаяся графическим представлением ребра
        /// </summary>
        public readonly Arrow edgeLine;
        /// <summary>
        /// Виджет, являющийся графическим представлением текста, написанным на ребре
        /// </summary>
        private readonly TextBlock _edgeLengthText;

        private readonly TextBlock _edgeProbaText;

        private readonly TextBlock _edgeBackProbaText;

        private SymbolicExpression _lengthExpr, _probaExpr, _backProbaExpr;

        private bool _showProba = false;

        private bool _showBackProba = false;

        private bool _backed;

        private bool _curved;

        private Canvas _canvas;
        /// <summary>
        /// ПРедставление ребра для сериализации
        /// </summary>
        public EdgeMetadata Metadata { get; }
        /// <summary>
        /// Свойство текста, написанного на ребре
        /// </summary>
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

        public string ProbaText
        {
            get => _probaText;
            private set
            {
                _probaText = value;
                _edgeProbaText.Text = _probaText;
                if (!_showProba && _canvas != null)
                {
                    _showProba = true;
                    _canvas.Children.Add(_edgeProbaText);
                }
                EdgeChangedEvent?.Invoke(ChangeType.ProbaTextChange);
                //TODO: update edge
            }
        }

        public string BackProbaText
        {
            get => _backProbaText;
            private set
            {
                _backProbaText = value;
                _edgeBackProbaText.Text = _backProbaText;
                if (!_showBackProba && _canvas != null)
                {
                    _showBackProba = true;
                    _canvas.Children.Add(_edgeBackProbaText);
                }
                EdgeChangedEvent?.Invoke(ChangeType.BackProbaTextChange);
                //TODO: update edge
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
                BackProbaText = _backProbaExpr.ToString();
                // TODO: update metadata
            }
        }
        /// <summary>
        /// Стартовая точка ребра
        /// </summary>
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
        /// <summary>
        /// Конечная точка ребра
        /// </summary>
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
                textBlock.Foreground,
                new NumberSubstitution(),
                1
            );
            return new Size(formattedText.Width, formattedText.Height);
        }
        /// <summary>
        /// Угол, под которым наклонен текст, написанный на ребре
        /// </summary>
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

        /// <summary>
        /// Необходимо ли отрисовывать ребро при помощи кривых Безье
        /// </summary>
        public bool Curved
        {
            get => _curved;
            set
            {
                _curved = value;
                Update(ChangeType.CurvedChanged);
            }
        }

        /// <summary>
        /// Является ли ребро "неориентированным"
        /// </summary>
        public bool Backed
        {
            get => _backed;
            set
            {
                _backed = value;
                Update(ChangeType.BackedChanged);
            }
        }
        /// <summary>
        /// Обновляет координаты ребра и стрелки
        /// </summary>
        private void Update(ChangeType changeType)
        {
            switch (changeType)
            {
                case ChangeType.LineChange:
                    edgeLine.X1 = StartPoint.X;
                    edgeLine.Y1 = StartPoint.Y;
                    edgeLine.X2 = EndPoint.X;
                    edgeLine.Y2 = EndPoint.Y;
                    Tranform(changeType | ChangeType.LengthTextChange, _edgeLengthText);
                    Tranform(changeType | ChangeType.ProbaTextChange, _edgeProbaText);
                    Tranform(changeType | ChangeType.BackProbaTextChange, _edgeBackProbaText);
                    break;
                case ChangeType.BackedChanged:
                    edgeLine.IsBacked = Backed;
                    break;
                case ChangeType.CurvedChanged:
                    edgeLine.IsCurved = Curved;
                    Tranform(changeType | ChangeType.LengthTextChange, _edgeLengthText);
                    Tranform(changeType | ChangeType.ProbaTextChange, _edgeProbaText);
                    Tranform(changeType | ChangeType.BackProbaTextChange, _edgeBackProbaText);
                    break;
                case ChangeType.LengthTextChange:
                    Tranform(changeType | ChangeType.LengthTextChange, _edgeLengthText);
                    break;
                case ChangeType.ProbaTextChange:
                    Tranform(changeType | ChangeType.ProbaTextChange, _edgeProbaText);
                    break;
                case ChangeType.BackProbaTextChange:
                    Tranform(changeType | ChangeType.BackProbaTextChange, _edgeBackProbaText);
                    break;
            }
        }
        private void Tranform(ChangeType changeType, TextBlock textBlock)
        {
            var s = EndPoint - StartPoint;

            var oldS = s;
            oldS.Normalize();

            var textSize = TextSize(textBlock);
            var textWidth = textSize.Width;
            var textHeight = textSize.Height;

            var lineWidth = s.Length;

            var widthOffset = (lineWidth - textWidth) / 2;

            var offsetPoint = s.X > 0 ? StartPoint : EndPoint;

            double offsetSign = Math.Sign(s.X);
            if (Math.Sign(s.X) != 0)
            {
                s *= Math.Sign(s.X);
            }
            else
            {
                offsetSign = Math.Sign(s.Y);
                offsetPoint = StartPoint;
            }
            s.Normalize();

            var perpS = new Vector(
                s.Y,
                -s.X
            );
            perpS.Normalize();

            Vector widthOffsetVector, heightOffsetVector, offsetVector = new Vector();
            double heightOffset, angle;
            var matrix = new Matrix();
            if (changeType.HasFlag(ChangeType.ProbaTextChange))
            {
                offsetPoint = StartPoint;
                s = oldS;
                perpS = new Vector(
                    s.Y * offsetSign,
                    -s.X * offsetSign
                );
                perpS.Normalize();
                widthOffsetVector = 5 * s;
                if (s.X < 0)
                {
                    widthOffsetVector += textWidth * s;
                }
                heightOffset = offset + textHeight;
                angle = Angle;
                if (Curved)
                {
                    perpS = new Vector(
                        -s.Y,
                        s.X
                    );
                    perpS.Normalize();
                    if (s.X > 0 || Math.Sign(s.X) == 0)
                    {
                        heightOffset -= textHeight;
                    }

                    heightOffsetVector = heightOffset * perpS;
                    offsetVector = widthOffsetVector + heightOffsetVector;
                    angle += Arrow.BezierAngle;
                }
                else
                {
                    heightOffsetVector = perpS * heightOffset;
                    offsetVector = widthOffsetVector + heightOffsetVector;
                }
            }
            else if (changeType.HasFlag(ChangeType.BackProbaTextChange))
            {
                offsetPoint = EndPoint;
                s *= -1;
                widthOffsetVector = (5 + textWidth) * s;
                heightOffset = offset + textHeight;
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
                    var bezierPoint = edgeLine.BezierPoint;
                    var midPoint = offsetPoint + s * (lineWidth / 2);
                    var bezierMidVec = bezierPoint - midPoint;
                    var bezierMidLength = bezierMidVec.Length;
                    bezierMidVec.Normalize();
                    heightOffset = offset + textHeight + bezierMidLength / 2.0;
                    if (offsetPoint == StartPoint && Math.Abs(s.X) > TOLERANCE || Math.Sign(s.X) == 0)
                    {
                        heightOffset -= textHeight;
                    }
                    heightOffsetVector = bezierMidVec * heightOffset;
                    offsetVector = widthOffsetVector + heightOffsetVector;
                }
                else
                {
                    heightOffset = offset + textHeight;
                    heightOffsetVector = perpS * heightOffset;
                    offsetVector = widthOffsetVector + heightOffsetVector;
                }

                angle = Angle;
            }

            var textBlockPoint = offsetPoint + offsetVector;
            Canvas.SetLeft(textBlock, textBlockPoint.X);
            Canvas.SetTop(textBlock, textBlockPoint.Y);
            textBlock.RenderTransform = new RotateTransform(angle);
        }
        /// <summary>
        /// Добавляет ребро в заданный канвас
        /// </summary>
        public void AddToCanvas(Canvas canvas)
        {
            _canvas = canvas;
            canvas.Children.Add(edgeLine);
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
        /// <summary>
        /// Убирает ребро из заданного канваса
        /// </summary>
        public void RemoveFromCanvas()
        {
            _canvas.Children.Remove(edgeLine);
            _canvas.Children.Remove(_edgeLengthText);
            if (_showProba)
            {
                _canvas.Children.Remove(_edgeProbaText);
            }

            if (_showBackProba)
            {
                _canvas.Children.Remove(_edgeBackProbaText);
            }
        }
        /// <summary>
        /// Обновляет свойства ребра
        /// </summary>
        public void UpdateEdge(ChangeType changeType) => EdgeChangedEvent?.Invoke(changeType);

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
            edgeLine = new Arrow
            {
                Stroke = new SolidColorBrush(Colors.Black),
                ArrowLength = 10,
                ArrowAngle = 30,
                IsCurved = Curved
            };
            _edgeLengthText = new TextBlock();
            _edgeLengthText.Height = _edgeLengthText.FontSize + 3;

            _edgeProbaText = new TextBlock();
            _edgeProbaText.Height = _edgeProbaText.FontSize + 3;
            _edgeProbaText.Foreground = Brushes.Red;

            _edgeBackProbaText = new TextBlock();
            _edgeBackProbaText.Height = _edgeBackProbaText.FontSize + 3;
            _edgeBackProbaText.Foreground = Brushes.Red;
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
