using System;
using System.Windows.Documents;

namespace GraphExpectedValue.Utility.ConcreteStrategies
{
    public class BlockInverseStrategy : InverseStrategy
    {
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
                new Tuple<int, int>(matrix.Rows, matrix.Rows)
            );
        }
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
                new Tuple<int, int>(N / 2, N / 2)
            );
            var B = StrassenMultiplyStrategy.GetSubMatrix(
                matrix,
                new Tuple<int, int>(0, N / 2),
                new Tuple<int, int>(N / 2, N)
            );
            var C = StrassenMultiplyStrategy.GetSubMatrix(
                matrix,
                new Tuple<int, int>(N / 2, 0),
                new Tuple<int, int>(N, N / 2)
            );
            var D = StrassenMultiplyStrategy.GetSubMatrix(
                matrix,
                new Tuple<int, int>(N / 2, N / 2),
                new Tuple<int, int>(N, N)
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
        /// Inverse 2x2 matrix
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
            var content = new double[][]
            {
                new[] {d / det, -b / det},
                new[] {-c / det, a / det},
            };
            return new Matrix(content);
        }

        public override string ToString() => "Block inverse";
    }
}