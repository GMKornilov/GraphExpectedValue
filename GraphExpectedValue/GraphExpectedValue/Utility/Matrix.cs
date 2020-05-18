using System;
using System.Text;
using GraphExpectedValue.Utility.ConcreteAlgorithms;
using MathNet.Symbolics;

namespace GraphExpectedValue.Utility
{
    public class Matrix
    {
        private const double EPS = 1e-6;
        
        private readonly SymbolicExpression[][] content;
        
        public int Rows => content.Length;
        
        public int Cols => content[0].Length;
        
        public static MultiplyAlgorithm multiplyStrategy
        {
            get;
            set;
        } 
        
        public static InverseAlgorithm inverseStrategy
        {
            get;
            set;
        } 

        public SymbolicExpression this[int row, int col]
        {
            get => content[row][col];
            set => content[row][col] = value;
        }

        static Matrix()
        {
            multiplyStrategy = new SimpleMultiplyAlgorithm();
            inverseStrategy = new GaussEliminationInverseAlgorithm();
        }
        public Matrix(int rows, int cols)
        {
            content = new SymbolicExpression[rows][];
            for (var i = 0; i < rows; i++)
            {
                content[i] = new SymbolicExpression[cols];
                for (var j = 0; j < cols; j++)
                {
                    content[i][j] = SymbolicExpression.Zero;
                }
            }
        }

        public Matrix(int n) : this(n, n)
        {

        }

        public Matrix(SymbolicExpression[][] content)
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
        
        public Matrix Copy() => new Matrix(content);
        
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
        
        public void SwapRows(int row1, int row2)
        {
            var temp = content[row1];
            content[row1] = content[row2];
            content[row2] = temp;
        }
        
        public void MultiplyRow(int row, SymbolicExpression coeff)
        {
            for (var j = 0; j < Cols; j++)
            {
                content[row][j] *= coeff;
            }
        }
        
        public void AddRow(int row1, int row2, SymbolicExpression coeff)
        {
            for (var j = 0; j < Cols; j++)
            {
                content[row2][j] += content[row1][j] * coeff;
            }
        }
        
        public void GaussElimination()
        {
            for (var col = 0; col < Rows && col < Cols; col++)
            {
                if (Math.Abs(content[col][col].Evaluate(null).RealValue) < EPS)
                {
                    var swapRow = col + 1;
                    var foundPivot = false;
                    for (; swapRow < Rows; swapRow++)
                    {
                        if (Math.Abs(content[swapRow][col].Evaluate(null).RealValue) > EPS)
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
                MultiplyRow(col, SymbolicExpression.One / content[col][col]);
                for (var elimRow = 0; elimRow < Rows; elimRow++)
                {
                    if (elimRow == col || Math.Abs(content[elimRow][col].Evaluate(null).RealValue) < EPS)
                    {
                        continue;
                    }
                    AddRow(col, elimRow, -content[elimRow][col]);
                }
            }
        }
        
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
        
        public static Matrix IdentityMatrix(int rows)
        {
            var result = new Matrix(rows);
            for (var i = 0; i < rows; i++)
            {
                result[i, i] = SymbolicExpression.One;
            }

            return result;
        }
        
        public static Matrix operator *(Matrix lhs, Matrix rhs) => multiplyStrategy.Multiply(lhs, rhs);
        
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
        
        public static Matrix operator *(Matrix matrix, SymbolicExpression a)
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
        
        public static Matrix operator *(SymbolicExpression a, Matrix matrix) => matrix * a;
        
        public static Matrix operator -(Matrix matrix) => SymbolicExpression.MinusOne * matrix;
        
        public static Matrix operator ^(Matrix matrix, int pow) => Pow(matrix, pow);
        
        public override string ToString()
        {
            var builder = new StringBuilder();
            for (var i = 0; i < Rows; i++)
            {
                builder.Append(string.Join<SymbolicExpression>(" ", content[i]));
                builder.Append("\n");
            }

            return builder.ToString();
        }
    }
}