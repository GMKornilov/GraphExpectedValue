using System;
using System.Collections.Generic;
using System.Linq;
using GraphExpectedValue.GraphLogic;
using GraphExpectedValue.GraphWidgets;
using MathNet.Symbolics;

namespace GraphExpectedValue.Utility.ConcreteStrategies
{
    /// <summary>
    /// "Стратегия" нахождения искомых математических ожиданий при помощи обратной матрицы
    /// </summary>
    public class InverseMatrixSolutionStrategy : LinearEquationSolutionStrategy
    {
        /// <summary>
        /// Решение СЛАУ при помощи обратной матрицы
        /// </summary>
        /// <param name="metadata">Данные графа</param>
        /// <returns>Искомые математические ожидания</returns>
        public override Tuple<int, SymbolicExpression>[] Solve(GraphMetadata metadata)
        {
            FormMatrices(metadata);
            var rows = matrix.Rows;
            var A = StrassenMultiplyStrategy.GetSubMatrix(
                matrix,
                new Tuple<int, int>(0, 0),
                new Tuple<int, int>(rows, rows),
                ((matrix1, i, j) => matrix1[i, j])
            );
            var b = StrassenMultiplyStrategy.GetSubMatrix(
                matrix,
                new Tuple<int, int>(0, rows),
                new Tuple<int, int>(rows, rows + 1),
                ((matrix1, i, j) => matrix1[i, j])
            );
            var inverse = A ^ (-1);
            var resMatrix = inverse * b;
            var res = new Tuple<int, SymbolicExpression>[resMatrix.Rows];
            for (var i = 0; i < res.Length; i++)
            {
                res[i] = new Tuple<int, SymbolicExpression>(vertexPseudoIndexes[i] + 1, resMatrix[i, 0]);
            }

            return res;
        }

        public override string ToString() => "Inverse matrix";
    }
}