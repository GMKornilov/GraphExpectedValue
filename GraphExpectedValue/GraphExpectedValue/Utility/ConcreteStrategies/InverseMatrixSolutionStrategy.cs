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
    public class InverseMatrixSolutionStrategy : SolutionStrategy
    {
        /// <summary>
        /// Матрицы, представляющие СЛАУ графа
        /// </summary>
        private Matrix A, b;
        /// <summary>
        /// Была ли сформирована матрица графа
        /// </summary>
        private bool formed;
        /// <summary>
        /// Позиции номеров вершин в матрице для восстановления ответа
        /// </summary>
        private List<int> vertexPseudoIndexes;
        /// <summary>
        /// Какие вершины являются поглощающими
        /// </summary>
        private List<bool> isEnding;

        private List<int> vertexMatrixIndex;
        /// <summary>
        /// Решение СЛАУ при помощи обратной матрицы
        /// </summary>
        /// <param name="metadata">Данные графа</param>
        /// <returns>Искомые математические ожидания</returns>
        public Tuple<int, SymbolicExpression>[] Solve(GraphMetadata metadata)
        {
            FormMatrices(metadata);
            var inverse = A ^ (-1);
            var resMatrix = inverse * b;
            var res = new Tuple<int, SymbolicExpression>[resMatrix.Rows];
            for (var i = 0; i < res.Length; i++)
            {
                res[i] = new Tuple<int, SymbolicExpression>(vertexPseudoIndexes[i], resMatrix[i, 0]);
            }

            return res;
        }
        /// <summary>
        /// Формирование матриц СЛАУ
        /// </summary>
        /// <param name="metadata">Данные графа</param>
        public void FormMatrices(GraphMetadata metadata)
        {
            var endVertexesIndexes = new List<int>();
            isEnding = new List<bool>(Enumerable.Repeat(false, metadata.VertexMetadatas.Count));
            foreach (var vertexMetadata in metadata.VertexMetadatas)
            {
                if (vertexMetadata.Type == VertexType.EndVertex)
                {
                    isEnding[vertexMetadata.Number - 1] = true;
                    endVertexesIndexes.Add(vertexMetadata.Number - 1);
                }
            }
            vertexMatrixIndex = new List<int>(Enumerable.Repeat(0, metadata.VertexMetadatas.Count));
            vertexPseudoIndexes = new List<int>(Enumerable.Repeat(0, metadata.VertexMetadatas.Count - endVertexesIndexes.Count));
            var index = 0;
            foreach (var vertexMetadata in metadata.VertexMetadatas)
            {
                if (vertexMetadata.Type == VertexType.PathVertex)
                {
                    vertexPseudoIndexes[index] = vertexMetadata.Number - 1;
                    vertexMatrixIndex[vertexMetadata.Number - 1] = index;
                    index++;
                }
            }
            A = new Matrix(
                vertexPseudoIndexes.Count,
                vertexPseudoIndexes.Count
            );
            b = new Matrix(
                vertexPseudoIndexes.Count,
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
                var lengthExpr = SymbolicExpression.Parse(edge.Length);

                var startVertexDegree = vertexDegrees[edge.StartVertexNumber - 1];
                var endVertexDegree = vertexDegrees[edge.EndVertexNumber - 1];

                var startProba = 1.0 / startVertexDegree;

                var startVertexIndex = edge.StartVertexNumber - 1;

                var endVertexIndex = edge.EndVertexNumber - 1;
                // start vertex is not ending
                if (!isEnding[startVertexIndex])
                {
                    // end vertex is not ending
                    if (!isEnding[endVertexIndex])
                    {
                        A[vertexMatrixIndex[startVertexIndex], vertexMatrixIndex[endVertexIndex]] = -startProba;
                    }

                    b[vertexMatrixIndex[startVertexIndex], 0] += startProba * lengthExpr;
                }
                // unoriented and end vertex is not ending
                if (!metadata.IsOriented && !isEnding[endVertexIndex])
                {
                    var endProba = 1.0 / endVertexDegree;
                    // start vertex is not ending
                    if (!isEnding[startVertexIndex])
                    {
                        A[vertexMatrixIndex[endVertexIndex], vertexMatrixIndex[startVertexIndex]] = -endProba;
                    }

                    b[vertexMatrixIndex[endVertexIndex], 0] = endProba * lengthExpr;
                }
            }

            for (var i = 0; i < A.Rows && i < A.Cols; i++)
            {
                A[i, i] += 1;
            }

            formed = true;
        }

        public override string ToString() => "Inverse matrix";
    }
}