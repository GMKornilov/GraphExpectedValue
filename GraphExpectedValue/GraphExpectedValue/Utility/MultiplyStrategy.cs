namespace GraphExpectedValue.Utility
{
    public interface MultiplyStrategy
    {
        Matrix Multiply(Matrix lhs, Matrix rhs);
    }
}