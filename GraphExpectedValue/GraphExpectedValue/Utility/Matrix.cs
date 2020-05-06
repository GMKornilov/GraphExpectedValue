using System;
using System.Diagnostics;
using System.Text;
using GraphExpectedValue.Utility.ConcreteStrategies;

namespace GraphExpectedValue.Utility
{
    /// <summary>
    /// Класс для работы с матрицей
    /// </summary>
    public class Matrix
    {
        /// <summary>
        /// Константа для сравнения вещественных чисел
        /// </summary>
        private const double EPS = 1e-6;
        /// <summary>
        /// Элементы матрицы
        /// </summary>
        private readonly double[][] content;
        /// <summary>
        /// Количество строк матрицы
        /// </summary>
        public int Rows => content.Length;
        /// <summary>
        /// Количесвто столбцов матрицы
        /// </summary>
        public int Cols => content[0].Length;
        /// <summary>
        /// "Стратегия" умножения двух матриц
        /// </summary>
        public static MultiplyStrategy multiplyStrategy
        {
            get;
            set;
        }
        /// <summary>
        /// "Стратегия" нахождения обратной матрицы
        /// </summary>
        public static InverseStrategy inverseStrategy
        {
            get;
            set;
        }

        public double this[int row, int col]
        {
            get => content[row][col];
            set => content[row][col] = value;
        }

        public Matrix()
        {
            multiplyStrategy = new SimpleMultiplyStrategy();
            inverseStrategy = new GaussEliminationInverseStrategy();
        }
        public Matrix(int rows, int cols) : this()
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

        public Matrix(double[][] content) : this()
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
        /// <summary>
        /// Метод,создающий матрицу с такими же элементами
        /// </summary>
        /// <returns></returns>
        public Matrix Copy() => new Matrix(content);
        /// <summary>
        /// Транспонирование матрицы
        /// </summary>
        /// <returns></returns>
        public Matrix Transpose()
        {
            var result = new Matrix(Cols, Rows);
            for (var i = 0; i < result.Rows; i++)
            {
                for (var j = 0; j < result.Cols; j++)
                {
                    result[i, j] = this[j, i];
                }
            }

            return result;
        }
        /// <summary>
        /// Меняет местами 2 строки в матрице
        /// </summary>
        /// <param name="row1">Номер первой строки</param>
        /// <param name="row2">Номер второй строки</param>
        public void SwapRows(int row1, int row2)
        {
            var temp = content[row1];
            content[row1] = content[row2];
            content[row2] = temp;
        }
        /// <summary>
        /// Умножает элементы строки на вещественне число
        /// </summary>
        /// <param name="row">Номер строки</param>
        /// <param name="coeff">Коэффициент умножения</param>
        public void MultiplyRow(int row, double coeff)
        {
            for (var j = 0; j < Cols; j++)
            {
                content[row][j] *= coeff;
            }
        }
        /// <summary>
        /// Добавляет первую строку ко второй с определенным коэффициентом
        /// </summary>
        /// <param name="row1">Номер первой строки</param>
        /// <param name="row2">Номер второй строки</param>
        /// <param name="coeff">Коэффициент прибавления</param>
        public void AddRow(int row1, int row2, double coeff)
        {
            for (var j = 0; j < Cols; j++)
            {
                content[row2][j] += content[row1][j] * coeff;
            }
        }
        /// <summary>
        /// Применение метода Гаусса к матрицу
        /// </summary>
        public void GaussElimination()
        {
            for (var col = 0; col < Rows && col < Cols; col++)
            {
                if (Math.Abs(content[col][col]) < EPS)
                {
                    var swapRow = col + 1;
                    var foundPivot = false;
                    for (; swapRow < Rows; swapRow++)
                    {
                        if (Math.Abs(content[swapRow][col]) > EPS)
                        {
                            foundPivot = true;
                            break;
                        }
                    }

                    if (!foundPivot)
                    {
                        continue;
                    }

                    SwapRows(col, swapRow);
                }
                MultiplyRow(col, 1.0 / content[col][col]);
                for (var elimRow = 0; elimRow < Rows; elimRow++)
                {
                    if (elimRow == col || Math.Abs(content[elimRow][col]) < EPS)
                    {
                        continue;
                    }
                    AddRow(col, elimRow, -content[elimRow][col]);
                }
            }
        }
        /// <summary>
        /// Возведение матрицы в заданную целую степень
        /// </summary>
        private static Matrix Pow(Matrix matrix, int power)
        {
            if (matrix.Rows != matrix.Cols)
            {
                throw new ArgumentException("Matrix should be square");
            }

            if (power == 0)
            {
                return IdentityMatrix(matrix.Rows);
            }

            if (power == 1)
            {
                return matrix.Copy();
            }

            if (power == -1)
            {
                return inverseStrategy.Inverse(matrix);
            }

            Matrix res;
            if (power % 2 == 0)
            {
                res = Pow(matrix, power / 2);
                return res * res;
            }

            res = Pow(matrix, power - 1);
            return matrix * res;
        }
        /// <summary>
        /// Возвращает единичную матрицу с заданным числом строк
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        public static Matrix IdentityMatrix(int rows)
        {
            var result = new Matrix(rows);
            for (var i = 0; i < rows; i++)
            {
                result[i, i] = 1;
            }

            return result;
        }
        /// <summary>
        /// Оператор множения двух матриц
        /// </summary>
        public static Matrix operator *(Matrix lhs, Matrix rhs) => multiplyStrategy.Multiply(lhs, rhs);
        /// <summary>
        /// Оператор сложения двух матриц
        /// </summary>
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
        /// <summary>
        /// Оператор вычитания двух матриц
        /// </summary>
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
        /// <summary>
        /// Оператор умножения матрицы на число
        /// </summary>
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
        /// <summary>
        /// Оператор умножения числа на матрицу
        /// </summary>
        public static Matrix operator *(double a, Matrix matrix) => matrix * a;
        /// <summary>
        /// Унарный оператор противоположной по знаку матрицы
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static Matrix operator -(Matrix matrix) => -1 * matrix;
        /// <summary>
        /// Оператор возведения матрицы в целую степень
        /// </summary>
        public static Matrix operator ^(Matrix matrix, int pow) => Pow(matrix, pow);
        
        public override string ToString()
        {
            var builder = new StringBuilder();
            for (var i = 0; i < Rows; i++)
            {
                builder.Append(string.Join(" ", content[i]));
                builder.Append("\n");
            }

            return builder.ToString();
        }
    }
}