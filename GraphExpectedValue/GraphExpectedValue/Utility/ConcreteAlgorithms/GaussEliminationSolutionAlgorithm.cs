using System;
using GraphExpectedValue.GraphLogic;
using MathNet.Symbolics;

namespace GraphExpectedValue.Utility.ConcreteAlgorithms
{
    public class GaussEliminationSolutionAlgorithm : LinearEquationSolutionAlgorithm
    {
        private const double EPS = 1e-6;
        
        public override Tuple<int, SymbolicExpression>[] Solve(GraphMetadata metadata)
        {
            FormMatrices(metadata);
            if (!GaussElimination(out var result))
            {
                throw new ArgumentException("bad graph");
            }
            return result;
        }
        
        private bool GaussElimination(out Tuple<int, SymbolicExpression>[] result)
        {
            if (!formed)
            {
                throw new Exception("Form matrix before doing elimination");
            }
            matrix.GaussElimination();
            for (var checkRow = 0; checkRow < matrix.Rows; checkRow++)
            {
                if (Math.Abs(matrix[checkRow, checkRow].Evaluate(null).RealValue - 1) > EPS)
                {
                    result = null;
                    return false;
                }
            }
            result = new Tuple<int, SymbolicExpression>[matrix.Rows];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = new Tuple<int, SymbolicExpression>(vertexPseudoIndexes[i] + 1, matrix[i, matrix.Cols - 1]);
            }

            return true;
        }

        public override string ToString() => "Gauss Elimination";
    }
}