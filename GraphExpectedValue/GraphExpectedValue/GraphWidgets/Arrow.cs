using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GraphExpectedValue.Annotations;

namespace GraphExpectedValue.GraphWidgets
{
    public class Arrow : Shape, INotifyPropertyChanged
    {
        private const int angle = 30;
        public static readonly DependencyProperty X1Property;
        public static readonly DependencyProperty X2Property;
        public static readonly DependencyProperty Y1Property;
        public static readonly DependencyProperty Y2Property;
        public static readonly DependencyProperty ArrowLengthProperty;
        public static readonly DependencyProperty ArrowAngleProperty;
        public static readonly DependencyProperty IsCurvedProperty;

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

        public double ArrowAngle
        {
            get => (double)GetValue(ArrowAngleProperty);
            set => SetValue(ArrowAngleProperty, value);

        }

        public bool IsCurved
        {
            get => (bool)GetValue(IsCurvedProperty);
            set
            {
                SetValue(IsCurvedProperty, value);
                OnPropertyChanged();
            }
        }

        public Point BezierPoint
        {
            get
            {
                var lineVecHalved = new Vector(X2 - X1, Y2 - Y1) / 2;
                var lineVecHalvedLength = lineVecHalved.Length;
                var perpVec = new Vector(
                    -(Y2 - Y1),
                    (X2 - X1)
                );
                perpVec.Normalize();
                var perpVecLen = Math.Tan(angle * Math.PI / 180) * lineVecHalvedLength;
                perpVec *= perpVecLen;

                var midPointVec = perpVec + lineVecHalved;
                var ptMid = new Point(X1, Y1) + midPointVec;
                return ptMid;
            }
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
            ArrowAngleProperty = DependencyProperty.Register(
                nameof(ArrowAngle),
                typeof(double),
                typeof(Arrow)
            );
            IsCurvedProperty = DependencyProperty.Register(
                nameof(IsCurved),
                typeof(bool),
                typeof(Arrow),
                new FrameworkPropertyMetadata(
                    defaultValue:false,
                    FrameworkPropertyMetadataOptions.AffectsRender
                )
            );
        }

        protected override Geometry DefiningGeometry
        {
            get
            {
                var geometry = new StreamGeometry { FillRule = FillRule.EvenOdd };

                using (var context = geometry.Open())
                {
                    if (IsCurved)
                    {
                        DrawArrowWithBezier(context);
                    }
                    else
                    {
                        DrawArrow(context);
                    }
                }

                // Freeze the geometry for performance benefits
                geometry.Freeze();

                return geometry;

            }
        }

        private void DrawArrow(StreamGeometryContext context)
        {
            var pt1 = new Point(X1, Y1);
            var pt2 = new Point(X2, Y2);

            var endStartVector = new Vector(pt1.X - pt2.X, pt1.Y - pt2.Y);
            endStartVector.Normalize();
            endStartVector *= ArrowLength;

            var rotateMatrix = new Matrix();
            rotateMatrix.Rotate(ArrowAngle);
            var firstArrowVector = Vector.Multiply(endStartVector, rotateMatrix);
            rotateMatrix.Rotate(-2 * ArrowAngle);
            var secondArrowVector = Vector.Multiply(endStartVector, rotateMatrix);

            var pt3 = pt2 + firstArrowVector;
            var pt4 = pt2 + secondArrowVector;

            context.BeginFigure(pt1, true, false);
            context.LineTo(pt2, true, true);
            context.LineTo(pt3, true, true);
            context.LineTo(pt2, true, true);
            context.LineTo(pt4, true, true);
        }

        private void DrawArrowWithBezier(StreamGeometryContext context)
        {
            var pt1 = new Point(X1, Y1);
            var pt2 = new Point(X2, Y2);

            var endStartVector = new Vector(X1 - X2, Y1 - Y2);
            endStartVector.Normalize();
            endStartVector *= ArrowLength;

            var ptMid = BezierPoint;

            var rotateMatrix = new Matrix();
            rotateMatrix.Rotate(-angle);
            rotateMatrix.Rotate(ArrowAngle);
            var firstArrowVector = Vector.Multiply(endStartVector, rotateMatrix);
            rotateMatrix.Rotate(-2 * ArrowAngle);
            var secondArrowVector = Vector.Multiply(endStartVector, rotateMatrix);

            var pt3 = pt2 + firstArrowVector;
            var pt4 = pt2 + secondArrowVector;

            context.BeginFigure(pt1, false, false);
            context.QuadraticBezierTo(ptMid, pt2, true, false);
            context.LineTo(pt3, true, true);
            context.LineTo(pt2, true, true);
            context.LineTo(pt4, true, true);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
