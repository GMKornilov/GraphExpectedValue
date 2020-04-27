using System;
using System.Runtime.InteropServices;

namespace GraphExpectedValue.Utility.ConcreteStrategies
{
    public class GaussEliminationInverseStrategy : InverseStrategy
    {
        private const double EPS = 1e-6;
        public Matrix Inverse(Matrix matrix)
        {
            var matrixCopy = new Matrix(matrix.Rows, matrix.Cols * 2);
            for (var i = 0; i < matrix.Rows; i++)
            {
                matrixCopy[i, i + matrix.Cols] = 1;
                for (var j = 0; j < matrix.Cols; j++)
                {
                    matrixCopy[i, j] = matrix[i, j];
                }
            }
            matrixCopy.GaussElimination();
            for (var i = 0; i < matrixCopy.Rows; i++)
            {
                if (Math.Abs(matrixCopy[i, i] - 1) > EPS)
                {
                    throw new ArgumentException("Given matrix is uninvertible");
                }
            }
            var result = new Matrix(matrix.Rows, matrix.Cols);
            for (var i = 0; i < matrix.Rows; i++)
            {
                for (var j = 0; j < matrix.Cols; j++)
                {
                    result[i, j] = matrixCopy[i, j + matrix.Cols];
                }
            }

            return result;
        }

        public override string ToString() => "Gauss elimination";
    }
}