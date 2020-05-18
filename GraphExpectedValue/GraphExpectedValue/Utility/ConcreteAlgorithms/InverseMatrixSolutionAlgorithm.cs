using System;
using GraphExpectedValue.GraphLogic;
using MathNet.Symbolics;

namespace GraphExpectedValue.Utility.ConcreteAlgorithms
{
    public class InverseMatrixSolutionAlgorithm : LinearEquationSolutionAlgorithm
    {
        public override Tuple<int, SymbolicExpression>[] Solve(GraphMetadata metadata)
        {
            FormMatrices(metadata);
            var rows = matrix.Rows;
            var A = StrassenMultiplyAlgorithm.GetSubMatrix(
                matrix,
                new Tuple<int, int>(0, 0),
                new Tuple<int, int>(rows, rows),
                ((matrix1, i, j) => matrix1[i, j])
            );
            var b = StrassenMultiplyAlgorithm.GetSubMatrix(
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