using System;
using GraphExpectedValue.GraphLogic;

namespace GraphExpectedValue.Utility.ConcreteStrategies
{
    /// <summary>
    /// "Стратегия" нахождения искомых математических ожиданий при помощи обратной матрицы
    /// </summary>
    public class InverseMatrixSolutionStrategy : SolutionStrategy
    {
        /// <summary>
        /// Матрицы, представляющие СЛАУ графа
        /// </summary>
        private Matrix A, b;
        /// <summary>
        /// Решение СЛАУ при помощи обратной матрицы
        /// </summary>
        /// <param name="metadata">Данные графа</param>
        /// <returns>Искомые математические ожидания</returns>
        public double[] Solve(GraphMetadata metadata)
        {
            FormMatrices(metadata);
            var inverse = A ^ (-1);
            var resMatrix = inverse * b;
            var res = new double[resMatrix.Rows];
            for (var i = 0; i < res.Length; i++)
            {
                res[i] = resMatrix[i, 0];
            }

            return res;
        }
        /// <summary>
        /// Формирование матриц СЛАУ
        /// </summary>
        /// <param name="metadata">Данные графа</param>
        public void FormMatrices(GraphMetadata metadata)
        {
            A = new Matrix(
                metadata.VertexMetadatas.Count - 1,
                metadata.VertexMetadatas.Count - 1
            );
            b = new Matrix(
                metadata.VertexMetadatas.Count - 1,
                1
            );
            var vertexDegrees = new int[metadata.VertexMetadatas.Count];
            for (var i = 0; i < vertexDegrees.Length; i++)
            {
                vertexDegrees[i] = 0;
            }

            foreach (var edge in metadata.EdgeMetadatas)
            {
                vertexDegrees[edge.StartVertexNumber - 1]++;
                if (!metadata.IsOriented)
                {
                    vertexDegrees[edge.EndVertexNumber - 1]++;
                }
            }

            foreach (var edge in metadata.EdgeMetadatas)
            {
                var startVertexDegree = vertexDegrees[edge.StartVertexNumber - 1];
                var endVertexDegree = vertexDegrees[edge.EndVertexNumber - 1];

                var startProba = 1.0 / startVertexDegree;

                var startVertexIndex = edge.StartVertexNumber - 1;
                startVertexIndex -= (startVertexIndex >= metadata.EndVertexNumber) ? 1 : 0;

                var endVertexIndex = edge.EndVertexNumber - 1;
                endVertexIndex -= (endVertexIndex >= metadata.EndVertexNumber) ? 1 : 0;

                if (edge.StartVertexNumber != metadata.EndVertexNumber)
                {
                    if (edge.EndVertexNumber != metadata.EndVertexNumber)
                    {
                        A[startVertexIndex, endVertexIndex] = -startProba;
                    }

                    b[startVertexIndex, 0] += startProba * edge.Length;
                }

                if (!metadata.IsOriented && edge.EndVertexNumber != metadata.EndVertexNumber)
                {
                    var endProba = 1.0 / endVertexDegree;
                    if (edge.StartVertexNumber != metadata.EndVertexNumber)
                    {
                        A[endVertexIndex, startVertexIndex] = -endProba;
                    }

                    b[endVertexIndex, 0] = endProba * edge.Length;
                }
            }

            for (var i = 0; i < metadata.VertexMetadatas.Count - 1; i++)
            {
                A[i, i] += 1;
            }
        }

        public override string ToString() => "Inverse matrix";
    }
}