using System;
using MathNet.Symbolics;

namespace GraphExpectedValue.Utility.ConcreteAlgorithms
{
    public class BlockInverseAlgorithm : InverseAlgorithm
    {
        public Matrix Inverse(Matrix matrix)
        {
            if (matrix.Rows != matrix.Cols)
            {
                throw new ArgumentException("Cant invert non-square matrix");
            }
            var transposed = matrix.Transpose();
            var lhs = BlockInverse(transposed * matrix);
            lhs = StrassenMultiplyAlgorithm.GetSubMatrix(
                lhs,
                new Tuple<int, int>(0, 0),
                new Tuple<int, int>(matrix.Rows, matrix.Rows),
                BlockGet
            );
            var res = lhs * transposed;
            return res;
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
            var N = StrassenMultiplyAlgorithm.NextPowerOfTwo(matrix.Rows);
            var A = StrassenMultiplyAlgorithm.GetSubMatrix(
                matrix,
                new Tuple<int, int>(0, 0),
                new Tuple<int, int>(N / 2, N / 2),
                BlockGet
            );
            var B = StrassenMultiplyAlgorithm.GetSubMatrix(
                matrix,
                new Tuple<int, int>(0, N / 2),
                new Tuple<int, int>(N / 2, N),
                BlockGet
            );
            var C = StrassenMultiplyAlgorithm.GetSubMatrix(
                matrix,
                new Tuple<int, int>(N / 2, 0),
                new Tuple<int, int>(N, N / 2),
                BlockGet
            );
            var D = StrassenMultiplyAlgorithm.GetSubMatrix(
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
            return StrassenMultiplyAlgorithm.CombineSubMatrices(res11, res12, res21, res22);
        }

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
                return i == j ? SymbolicExpression.One : SymbolicExpression.Zero;
            }

            return matrix[i, j];
        }

        public override string ToString() => "Block inverse";
    }
}