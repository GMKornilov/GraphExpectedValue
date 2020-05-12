using System;
using GraphExpectedValue.GraphLogic;
using MathNet.Symbolics;

namespace GraphExpectedValue.Utility
{
    public interface SolutionStrategy
    {
        Tuple<int, SymbolicExpression>[] Solve(GraphMetadata metadata);
        void FormMatrices(GraphMetadata metadata);
    }
}