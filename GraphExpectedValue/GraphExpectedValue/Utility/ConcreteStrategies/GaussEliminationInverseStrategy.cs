using System;
using System.Runtime.InteropServices;

namespace GraphExpectedValue.Utility.ConcreteStrategies
{
    /// <summary>
    /// "Стратегия" нахождения обратной матрицы при помощи метода Гаусса
    /// </summary>
    public class GaussEliminationInverseStrategy : InverseStrategy
    {
        /// <summary>
        /// Константа для сравнения вещественных чисео
        /// </summary>
        private const double EPS = 1e-6;
        /// <summary>
        /// Нахождение обратной матрицы методом Гаусса
        /// </summary>
        public Matrix Inverse(Matrix matrix)
        {
            if (matrix.Rows != matrix.Cols)
            {
                throw new ArgumentException("Matrix should be square");
            }
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
                if (Math.Abs(matrixCopy[i, i].Evaluate(null).RealValue - 1) > EPS)
                {
                    throw new ArgumentException("Given matrix is uninvertible");
                }

                for (var j = 0; j < i; j++)
                {
                    if (Math.Abs(matrixCopy[i, j].Evaluate(null).RealValue) > EPS)
                    {
                        throw new ArgumentException("Given matrix is uninvertible");
                    }
                }
                for (var j = i + 1; j < matrixCopy.Rows; j++)
                {
                    if (Math.Abs(matrixCopy[i, j].Evaluate(null).RealValue) > EPS)
                    {
                        throw new ArgumentException("Given matrix is uninvertible");
                    }
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