using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GraphExpectedValue.GraphWidgets
{
    public class Arrow : Shape
    {
        public static readonly DependencyProperty X1Property;
        public static readonly DependencyProperty X2Property;
        public static readonly DependencyProperty Y1Property;
        public static readonly DependencyProperty Y2Property;
        public static readonly DependencyProperty ArrowLengthProperty;

        public double X1
        {
            get => (double)GetValue(X1Property);
            set => SetValue(X1Property, value);
        }

        public double X2
        {
            get => (double)GetValue(X2Property);
            set => SetValue(X2Property, value);
        }

        public double Y1
        {
            get => (double)GetValue(Y1Property);
            set => SetValue(Y1Property, value);
        }

        public double Y2
        {
            get => (double)GetValue(Y2Property);
            set => SetValue(Y2Property, value);
        }

        public double ArrowLength
        {
            get => (double)GetValue(ArrowLengthProperty);
            set => SetValue(ArrowLengthProperty, value);
        }

        static Arrow()
        {
            X1Property = DependencyProperty.Register(
                nameof(X1),
                typeof(double),
                typeof(Arrow)

            );
            X2Property = DependencyProperty.Register(
                nameof(X2),
                typeof(double),
                typeof(Arrow)
            );
            Y1Property = DependencyProperty.Register(
                nameof(Y1),
                typeof(double),
                typeof(Arrow)
            );
            Y2Property = DependencyProperty.Register(
                nameof(Y2),
                typeof(double),
                typeof(Arrow)
            );
            ArrowLengthProperty = DependencyProperty.Register(
                nameof(ArrowLength),
                typeof(double),
                typeof(Arrow)
            );
        }

        protected override Geometry DefiningGeometry
        {
            get
            {
                var geometry = new StreamGeometry { FillRule = FillRule.EvenOdd };

                using (var context = geometry.Open())
                {
                    DrawArrow(context);
                }

                // Freeze the geometry for performance benefits
                geometry.Freeze();

                return geometry;

            }
        }

        private void DrawArrow(StreamGeometryContext context)
        {
            var angle = Math.Atan2(Y2 - Y1, X2 - X1);
            var pt1 = new Point(X1, Y1);
            var pt2 = new Point(X2, Y2);

            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);

            var pt3 = new Point(
                X2 - (ArrowLength * cos - ArrowLength * sin),
                Y2 - (ArrowLength * sin + ArrowLength * cos)
            );

            var pt4 = new Point(
                X2 - (ArrowLength * cos + ArrowLength * sin),
                Y2 + (ArrowLength * cos - ArrowLength * sin)
            );

            context.BeginFigure(pt1, true, false);
            context.LineTo(pt2, true, true);
            context.LineTo(pt3, true, true);
            context.LineTo(pt2, true, true);
            context.LineTo(pt4, true, true);

        }
    }
}
