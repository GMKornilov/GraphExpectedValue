using System;
using GraphExpectedValue.GraphLogic;
using MathNet.Symbolics;

namespace GraphExpectedValue.Utility
{
    public interface SolutionAlgorithm
    {
        Tuple<int, SymbolicExpression>[] Solve(GraphMetadata metadata);
    }
}