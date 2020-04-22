using System;

namespace GraphExpectedValue.Utility.ConcreteStrategies
{
    public class SimpleMultiplyStrategy : MultiplyStrategy
    {
        public Matrix Multiply(Matrix lhs, Matrix rhs)
        {
            if (lhs.Cols != rhs.Rows)
            {
                throw new ArgumentException("Incorrect matrix sizes");
            }
            var result = new Matrix(lhs.Rows, rhs.Cols);
            for (var i = 0; i < result.Rows; i++)
            {
                for (var j = 0; j < result.Cols; j++)
                {
                    result[i, j] = 0;
                    for (var k = 0; k < lhs.Cols; k++)
                    {
                        result[i, j] += lhs[i, k] * rhs[k, j];
                    }
                }
            }

            return result;
        }
    }
}