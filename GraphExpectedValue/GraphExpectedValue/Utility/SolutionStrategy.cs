using GraphExpectedValue.GraphLogic;
using MathNet.Symbolics;

namespace GraphExpectedValue.Utility
{
    public interface SolutionStrategy
    {
        SymbolicExpression[] Solve(GraphMetadata metadata);
        void FormMatrices(GraphMetadata metadata);
    }
}