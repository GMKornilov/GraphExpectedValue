using System;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using GraphExpectedValue.Annotations;

namespace GraphExpectedValue.GraphWidgets
{
    [Flags]
    public enum DrawType
    {
        NormalDrawing = 0,
        BezierDrawing = (1 << 0),
        BackedDrawing = (1 << 1)
    }

    public class Arrow : Shape, INotifyPropertyChanged
    {
        private const int BezierAngle = 15;
        public static readonly DependencyProperty X1Property;
        public static readonly DependencyProperty X2Property;
        public static readonly DependencyProperty Y1Property;
        public static readonly DependencyProperty Y2Property;
        public static readonly DependencyProperty ArrowLengthProperty;
        public static readonly DependencyProperty ArrowAngleProperty;
        public static readonly DependencyProperty IsCurvedProperty;
        public static readonly DependencyProperty IsBackedProperty;

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

        public bool IsBacked
        {
            get => (bool) GetValue(IsBackedProperty);
            set
            {
                SetValue(IsBackedProperty, value);
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
                var perpVecLen = Math.Tan(BezierAngle * Math.PI / 180) * lineVecHalvedLength;
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
            IsBackedProperty = DependencyProperty.Register(
                nameof(IsBacked),
                typeof(bool),
                typeof(Arrow),
                new FrameworkPropertyMetadata(
                    true,
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
                    var drawType = DrawType.NormalDrawing;
                    if (IsCurved)
                    {
                        drawType |= DrawType.BezierDrawing;
                    }

                    if (IsBacked)
                    {
                        drawType |= DrawType.BackedDrawing;
                    }
                    Draw(context, drawType);
                }

                
                // Freeze the geometry for performance benefits
                geometry.Freeze();

                return geometry;

            }
        }

        private void Draw(StreamGeometryContext context, DrawType drawType)
        {
            var pt1 = new Point(X1, Y1);
            var pt2 = new Point(X2, Y2);

            var endStartVector = pt1 - pt2;
            endStartVector.Normalize();
            endStartVector *= ArrowLength;

            var rotateMatrix = new Matrix();
            if (drawType.HasFlag(DrawType.BezierDrawing))
            {
                rotateMatrix.Rotate(-BezierAngle);
            }
            rotateMatrix.Rotate(ArrowAngle);
            var firstArrowVector = Vector.Multiply(endStartVector, rotateMatrix);
            rotateMatrix.Rotate(-2 * ArrowAngle);
            var secondArrowVector = Vector.Multiply(endStartVector, rotateMatrix);

            var pt3 = pt2 + firstArrowVector;
            var pt4 = pt2 + secondArrowVector;

            context.BeginFigure(pt1, true, false);
            if (drawType.HasFlag(DrawType.BezierDrawing))
            {
                context.QuadraticBezierTo(BezierPoint, pt2, true, true);
            }
            else
            {
                context.LineTo(pt2, true, true);
            }
            context.LineTo(pt3, true, true);
            context.LineTo(pt2, true, true);
            context.LineTo(pt4, true, true);
            if (!drawType.HasFlag(DrawType.BackedDrawing))
            {
                return;
            }

            endStartVector *= -1;
            rotateMatrix = new Matrix();
            if (drawType.HasFlag(DrawType.BezierDrawing))
            {
                rotateMatrix.Rotate(-BezierAngle);
            }
            rotateMatrix.Rotate(ArrowAngle);
            firstArrowVector = Vector.Multiply(endStartVector, rotateMatrix);
            pt3 = pt1 + firstArrowVector;

            rotateMatrix.Rotate(- 2 * ArrowAngle);
            secondArrowVector = Vector.Multiply(endStartVector, rotateMatrix);
            pt4 = pt1 + secondArrowVector;

            context.LineTo(pt2, true, true);
            if (drawType.HasFlag(DrawType.BezierDrawing))
            {
                context.QuadraticBezierTo(BezierPoint, pt1, true, true);
            }
            else
            {
                context.LineTo(pt1, true, true);
            }
            context.LineTo(pt3, true, true);
            context.LineTo(pt1, true, true);
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
