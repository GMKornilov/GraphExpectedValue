using System;
using System.ComponentModel;
using System.Linq;
using MathNet.Symbolics;

namespace GraphExpectedValue.Utility.ConcreteAlgorithms
{
    public class StrassenMultiplyAlgorithm : MultiplyAlgorithm
    {
        public static int NextPowerOfTwo(int v)
        {
            --v;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            ++v;
            return v;
        }

        private static SymbolicExpression StrassenGet(Matrix matrix, int i, int j)
        {
            if (i < 0 || i >= matrix.Rows || j < 0 || j >= matrix.Cols)
            {
                return SymbolicExpression.Zero;
            }

            return matrix[i, j];
        }
        
        public Matrix Multiply(Matrix lhs, Matrix rhs)
        {
            if (lhs.Cols != rhs.Rows)
            {
                throw new ArgumentException("Incorrect matrix sizes");
            }
            var res = MatrixStrassenMultiply(lhs, rhs);
            return GetSubMatrix(
                res,
                new Tuple<int, int>(0, 0),
                new Tuple<int, int>(lhs.Rows, rhs.Cols),
                StrassenGet
            );
        }
        
        private Matrix MatrixStrassenMultiply(Matrix lhs, Matrix rhs)
        {
            if (lhs.Rows == 1 && lhs.Cols == 1 && rhs.Rows == 1 && rhs.Cols == 1)
            {
                var el1 = lhs[0, 0];
                var el2 = rhs[0, 0];
                var content = new[]
                {
                    new[] {el1 * el2}
                };
                return new Matrix(content);
            }
            if (lhs.Rows == 2 && lhs.Cols == 2 && rhs.Rows == 2 && rhs.Cols == 2)
            {
                return SimpleMultiply(lhs, rhs);
            }
            var maxPow = new[]
            {
                NextPowerOfTwo(lhs.Rows),
                NextPowerOfTwo(lhs.Cols),
                NextPowerOfTwo(rhs.Rows),
                NextPowerOfTwo(rhs.Cols)
            }.Max();
            #region submatrices
            var a11 = GetSubMatrix(
                lhs,
                new Tuple<int, int>(0, 0),
                new Tuple<int, int>(maxPow / 2, maxPow / 2),
                StrassenGet
            );
            var a12 = GetSubMatrix(
                lhs,
                new Tuple<int, int>(0, maxPow / 2),
                new Tuple<int, int>(maxPow / 2, maxPow),
                StrassenGet
            );
            var a21 = GetSubMatrix(
                lhs,
                new Tuple<int, int>(maxPow / 2, 0),
                new Tuple<int, int>(maxPow, maxPow / 2),
                StrassenGet
            );
            var a22 = GetSubMatrix(
                lhs,
                new Tuple<int, int>(maxPow / 2, maxPow / 2),
                new Tuple<int, int>(maxPow, maxPow),
                StrassenGet
            );

            var b11 = GetSubMatrix(
                rhs,
                new Tuple<int, int>(0, 0),
                new Tuple<int, int>(maxPow / 2, maxPow / 2),
                StrassenGet
            );
            var b12 = GetSubMatrix(
                rhs,
                new Tuple<int, int>(0, maxPow / 2),
                new Tuple<int, int>(maxPow / 2, maxPow),
                StrassenGet
            );
            var b21 = GetSubMatrix(
                rhs,
                new Tuple<int, int>(maxPow / 2, 0),
                new Tuple<int, int>(maxPow, maxPow / 2),
                StrassenGet
            );
            var b22 = GetSubMatrix(
                rhs,
                new Tuple<int, int>(maxPow / 2, maxPow / 2),
                new Tuple<int, int>(maxPow, maxPow),
                StrassenGet
            );
            #endregion

            var P = new[]
            {
                MatrixStrassenMultiply(a11 + a22, b11 + b22),
                MatrixStrassenMultiply(a21 + a22, b11),
                MatrixStrassenMultiply(a11, b12 - b22),
                MatrixStrassenMultiply(a22, b21 - b11),
                MatrixStrassenMultiply(a11 + a12, b22),
                MatrixStrassenMultiply(a21 - a11, b11 + b12),
                MatrixStrassenMultiply(a12 - a22, b21 + b22)
            };
            var c11 = P[0] + P[3] - P[4] + P[6];
            var c12 = P[2] + P[4];
            var c21 = P[1] + P[3];
            var c22 = P[0] - P[1] + P[2] + P[5];
            return CombineSubMatrices(c11, c12, c21, c22);
        }
        
        private Matrix SimpleMultiply(Matrix lhs, Matrix rhs)
        {
            if (!(lhs.Rows == 2 && lhs.Cols == 2 && rhs.Rows == 2 && rhs.Cols == 2))
            {
                throw new ArgumentException("Matrices should be 2x2 size");
            }

            SymbolicExpression[] m =
            {
                (lhs[0, 0] + lhs[1, 1]) * (rhs[0, 0] + rhs[1, 1]),
                (lhs[1, 0] + lhs[1, 1]) * rhs[0, 0],
                lhs[0, 0] * (rhs[0, 1] - rhs[1, 1]),
                lhs[1, 1] * (rhs[1, 0] - rhs[0, 0]),
                (lhs[0, 0] + lhs[0, 1]) * rhs[1, 1],
                (lhs[1, 0] - lhs[0, 0]) * (rhs[0, 0] + rhs[0, 1]),
                (lhs[0, 1] - lhs[1, 1]) * (rhs[1, 0] + rhs[1, 1])
            };
            var res = new[]
            {
                new[]
                {
                    m[0] + m[3] - m[4] + m[6],
                    m[2] + m[4]
                },
                new []
                {
                    m[1] + m[3],
                    m[0] - m[1] + m[2] + m[5]
                }
            };
            return new Matrix(res);
        }
        
        public static Matrix GetSubMatrix(Matrix matrix, Tuple<int, int> leftBorder, Tuple<int, int> rightBorder, Func<Matrix, int, int, SymbolicExpression> getter) 
        {
            var (n1, m1) = leftBorder;
            var (n2, m2) = rightBorder;

            var result = new Matrix(n2 - n1, m2 - m1);
            for (var i = 0; i < n2 - n1; i++)
            {
                for (var j = 0; j < m2 - m1; j++)
                {
                    result[i, j] = getter(matrix, i + n1, j + m1);
                }
            }

            return result;
        }
        
        public static Matrix CombineSubMatrices(Matrix c11, Matrix c12, Matrix c21, Matrix c22)
        {
            if (c11.Rows != c12.Rows || c21.Rows != c22.Rows || c11.Cols != c21.Cols || c12.Cols != c22.Cols)
            {
                throw new ArgumentException("Matrices should have equal size");
            }
            var result = new Matrix(
                c11.Rows + c21.Rows,
                c11.Cols + c12.Cols
            );
            var matrices = new[]
            {
                new[] {c11, c12},
                new[] {c21, c22}
            };
            for (var i = 0; i < result.Rows; i++)
            {
                for (var j = 0; j < result.Cols; j++)
                {
                    var indI = i / c11.Rows;
                    var chooseI = i % c11.Rows;
                    var indJ = j / c11.Cols;
                    var chooseJ = j % c11.Cols;
                    result[i, j] = matrices[indI][indJ][chooseI, chooseJ];
                }
            }

            return result;
        }

        public override string ToString() => "Strassen multiply";
    }
}