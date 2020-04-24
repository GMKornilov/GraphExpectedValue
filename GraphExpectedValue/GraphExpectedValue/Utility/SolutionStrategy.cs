using GraphExpectedValue.GraphLogic;

namespace GraphExpectedValue.Utility
{
    public interface SolutionStrategy
    {
        double[] Solve(GraphMetadata metadata);
        void FormMatrices(GraphMetadata metadata);
    }
}