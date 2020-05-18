using System;
using System.Windows.Documents;
using MathNet.Symbolics;

namespace GraphExpectedValue.Utility.ConcreteStrategies
{
    /// <summary>
    /// "Стратегия" по нахождению обратной матрицы при помощи разибения на блока
    /// </summary>
    public class BlockInverseStrategy : InverseStrategy
    {
        /// <summary>
        /// Нахожденре обратной матрицы
        /// </summary>
        public Matrix Inverse(Matrix matrix)
        {
            if (matrix.Rows != matrix.Cols)
            {
                throw new ArgumentException("Cant invert non-square matrix");
            }
            var transposed = matrix.Transpose();
            var lhs = BlockInverse(transposed * matrix);
            var res = lhs * transposed;
            return StrassenMultiplyStrategy.GetSubMatrix(
                res,
                new Tuple<int, int>(0, 0),
                new Tuple<int, int>(matrix.Rows, matrix.Rows),
                BlockGet
            );
        }
        /// <summary>
        /// Нахождение обратной матрицы при условии, что все галвные миноры матрицы обратимы
        /// </summary>
        private Matrix BlockInverse(Matrix matrix)
        {
            if (matrix.Rows != matrix.Cols)
            {
                throw new ArgumentException("Cant invert non-square matrix");
            }

            if (matrix.Rows == 2)
            {
                return InverseSquare(matrix);
            }
            var N = StrassenMultiplyStrategy.NextPowerOfTwo(matrix.Rows);
            var A = StrassenMultiplyStrategy.GetSubMatrix(
                matrix,
                new Tuple<int, int>(0, 0),
                new Tuple<int, int>(N / 2, N / 2),
                BlockGet
            );
            var B = StrassenMultiplyStrategy.GetSubMatrix(
                matrix,
                new Tuple<int, int>(0, N / 2),
                new Tuple<int, int>(N / 2, N),
                BlockGet
            );
            var C = StrassenMultiplyStrategy.GetSubMatrix(
                matrix,
                new Tuple<int, int>(N / 2, 0),
                new Tuple<int, int>(N, N / 2),
                BlockGet
            );
            var D = StrassenMultiplyStrategy.GetSubMatrix(
                matrix,
                new Tuple<int, int>(N / 2, N / 2),
                new Tuple<int, int>(N, N),
                BlockGet
            );

            // A^-1
            var inverseA = Inverse(A);
            // C * A^-1
            var CAInverse = C * inverseA;
            // inverse of Schur complement
            // S_A^-1 = (D - C * A^-1 * B)^-1
            var schurInverse = Inverse(D - CAInverse * B);
            // S_A^-1 * C * A^-1
            var schurCA = schurInverse * CAInverse;
            // A^-1 * B
            var AInverseB = inverseA * B;

            var res11 = inverseA + AInverseB * schurCA;
            var res12 = -(AInverseB * schurInverse);
            var res21 = -(schurCA);
            var res22 = schurInverse;
            return StrassenMultiplyStrategy.CombineSubMatrices(res11, res12, res21, res22);
        }

        /// <summary>
        /// Нахождение обратной матрицы для мматрицы размером 2 на 2
        /// </summary>
        private Matrix InverseSquare(Matrix matrix)
        {
            if (matrix.Rows != 2 || matrix.Cols != 2)
            {
                throw new ArgumentException("Matrix should be 2x2 size");
            }

            var a = matrix[0, 0];
            var b = matrix[0, 1];
            var c = matrix[1, 0];
            var d = matrix[1, 1];
            var det = a * d - b * c;
            var content = new[]
            {
                new[] {d / det, -b / det},
                new[] {-c / det, a / det},
            };
            return new Matrix(content);
        }

        private static SymbolicExpression BlockGet(Matrix matrix, int i, int j)
        {
            if (i < 0 || i >= matrix.Rows || j < 0 || j >= matrix.Cols)
            {
                return i == j ? 1 : 0;
            }

            return matrix[i, j];
        }

        public override string ToString() => "Block inverse";
    }
}