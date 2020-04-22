using System;
using GraphExpectedValue.Utility.ConcreteStrategies;

namespace GraphExpectedValue.Utility
{
    public class Matrix
    {
        private double[][] content;
        public int Rows => content.Length;
        public int Cols => content[0].Length;

        private static MultiplyStrategy strategy = new SimpleMultiplyStrategy();

        public double this[int row, int col]
        {
            get => content[row][col];
            set => content[row][col] = value;
        }
        public Matrix(int rows, int cols)
        {
            content = new double[rows][];
            for (var i = 0; i < rows; i++)
            {
                content[i] = new double[cols];
            }
        }

        public Matrix(int n) : this(n, n)
        {

        }

        public Matrix(double[][] content)
        {
            var cols = content[0].Length;
            for (var i = 1; i < content.Length; i++)
            {
                if (content[i].Length != cols)
                {
                    throw new ArgumentException("2d array should be rectangular");
                }
            }
            this.content = content;
        }
        public static Matrix operator *(Matrix lhs, Matrix rhs) => strategy.Multiply(lhs, rhs);

        public static Matrix operator +(Matrix lhs, Matrix rhs)
        {
            if (lhs.Rows != rhs.Rows || lhs.Cols != rhs.Cols)
            {
                throw new ArgumentException("Matrixes should be equal size");
            }
            var result = new Matrix(lhs.Rows, lhs.Cols);
            for (var i = 0; i < lhs.Rows; i++)
            {
                for (var j = 0; j < rhs.Rows; j++)
                {
                    result[i, j] = lhs[i, j] + rhs[i, j];
                }
            }

            return result;
        }

        public static Matrix operator -(Matrix lhs, Matrix rhs)
        {
            if (lhs.Rows != rhs.Rows || lhs.Cols != rhs.Cols)
            {
                throw new ArgumentException("Matrixes should be equal size");
            }
            var result = new Matrix(lhs.Rows, lhs.Cols);
            for (var i = 0; i < lhs.Rows; i++)
            {
                for (var j = 0; j < rhs.Rows; j++)
                {
                    result[i, j] = lhs[i, j] - rhs[i, j];
                }
            }

            return result;
        }

        public static Matrix operator *(Matrix matrix, double a)
        {
            var result = new Matrix(matrix.Rows, matrix.Cols);
            for (var i = 0; i < matrix.Rows; i++)
            {
                for (var j = 0; j < matrix.Cols; j++)
                {
                    result[i, j] = matrix[i, j] * a;
                }
            }

            return result;
        }

        public static Matrix operator *(double a, Matrix matrix) => matrix * a;

        public static Matrix operator -(Matrix matrix) => -1 * matrix;
    }
}