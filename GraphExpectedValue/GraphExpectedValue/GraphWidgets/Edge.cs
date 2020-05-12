using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GraphExpectedValue.GraphLogic;
using MathNet.Symbolics;

namespace GraphExpectedValue.GraphWidgets
{
    public enum ChangeType
    {
        LineChange,
        TextChange
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
        private string text;
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
        public readonly TextBlock edgeText;

        private SymbolicExpression lengthExpr, probaExpr;

        private bool _showProba = false;
        /// <summary>
        /// ПРедставление ребра для сериализации
        /// </summary>
        public EdgeMetadata Metadata { get; }
        /// <summary>
        /// Свойство текста, написанного на ребре
        /// </summary>
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
        public SymbolicExpression LengthExpression
        {
            get => lengthExpr;
            set
            {
                lengthExpr = value;
                var text = lengthExpr.ToString();
                if (_showProba)
                {
                    text += "  /  " + probaExpr.ToString();
                }

                Text = text;
                Metadata.Length = lengthExpr.ToString();
            }
        }

        public SymbolicExpression ProbabilityExpression
        {
            get => probaExpr;
            set
            {
                probaExpr = value;
                var text = lengthExpr.ToString();
                if (_showProba)
                {
                    text += "  /  " + probaExpr.ToString();
                }

                Text = text;
                Metadata.Probability = probaExpr.ToString();
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
        /// <summary>
        /// Размер текста, написанного на ребре
        /// </summary>
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
                return angle <= 90 ? angle : 180 - angle;
            }
        }
        /// <summary>
        /// Необходимо ли отрисовывать ребро при помощи кривых Безье
        /// </summary>
        public bool Curved { get; set; }
        /// <summary>
        /// Является ли ребро "неориентированным"
        /// </summary>
        public bool Backed { get; set; }
        public Edge(Point from, Point to, double val):this()
        {
            Text = val.ToString(CultureInfo.CurrentCulture);
            StartPoint = from;
            EndPoint = to;
        }
        /// <summary>
        /// Обновляет координаты ребра и стрелки
        /// </summary>
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
        /// <summary>
        /// Обновляет координаты текстового виджета, если ребро отрисовывается как пряма
        /// </summary>
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
        /// <summary>
        /// Обновляет координаты текстового виджета, если ребро отрисовываетс как кривая Безье
        /// </summary>
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
        /// <summary>
        /// Добавляет ребро в заданный канвас
        /// </summary>
        public void AddToCanvas(Canvas canvas)
        {
            canvas.Children.Add(edgeLine);
            canvas.Children.Add(edgeText);
        }
        /// <summary>
        /// Убирает ребро из заданного канваса
        /// </summary>
        public void RemoveFromCanvas(Canvas canvas)
        {
            canvas.Children.Remove(edgeLine);
            canvas.Children.Remove(edgeText);
        }
        /// <summary>
        /// Обновляет свойства ребра
        /// </summary>
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

        public Edge(Vertex from, Vertex to, EdgeMetadata metadata) : this(from, to, SymbolicExpression.Parse(metadata.Length))
        {
            this.Metadata = metadata;
        }
    }
}
