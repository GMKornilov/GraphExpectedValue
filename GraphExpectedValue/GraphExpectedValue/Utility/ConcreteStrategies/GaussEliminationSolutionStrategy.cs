using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using GraphExpectedValue.GraphLogic;
using GraphExpectedValue.GraphWidgets;
using MathNet.Symbolics;

namespace GraphExpectedValue.Utility.ConcreteStrategies
{
    /// <summary>
    /// "Стратегия" нахождения искомых математических ожиданий методом Гаусса
    /// </summary>
    public class GaussEliminationSolutionStrategy : SolutionStrategy
    {
        /// <summary>
        /// Константа для сравнения вещественных чисел
        /// </summary>
        private const double EPS = 1e-6;
        /// <summary>
        /// Матрица, представляющая СЛАУ графа
        /// </summary>
        private Matrix matrix;
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
        /// <summary>
        /// Индексы для матрицы
        /// </summary>
        private List<int> vertexMatrixIndex;
        /// <summary>
        /// Решение СЛАУ при помощи метода Гаусса
        /// </summary>
        /// <param name="metadata">Данные графа</param>
        /// <returns>Искомые математические ожидания</returns>
        public Tuple<int, SymbolicExpression>[] Solve(GraphMetadata metadata)
        {
            FormMatrices(metadata);
            if (!GaussElimination(out var result))
            {
                throw new ArgumentException("bad graph");
            }
            return result;
        }
        /// <summary>
        /// Формирование матрицы СЛАУ
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
            matrix = new Matrix(vertexPseudoIndexes.Count, vertexPseudoIndexes.Count + 1);
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
                        matrix[vertexMatrixIndex[startVertexIndex], vertexMatrixIndex[endVertexIndex]] = -startProba;
                    }

                    matrix[vertexMatrixIndex[startVertexIndex], matrix.Cols - 1] += startProba * lengthExpr;
                }
                // unoriented and end vertex is not ending
                if (!metadata.IsOriented && !isEnding[endVertexIndex])
                {
                    var endProba = 1.0 / endVertexDegree;
                    // start vertex is not ending
                    if (!isEnding[startVertexIndex])
                    {
                        matrix[vertexMatrixIndex[endVertexIndex], vertexMatrixIndex[startVertexIndex]] = -endProba;
                    }

                    matrix[vertexMatrixIndex[endVertexIndex], metadata.VertexMetadatas.Count - 1] = endProba * lengthExpr;
                }
            }

            for (var i = 0; i < matrix.Rows && i < matrix.Cols; i++)
            {
                matrix[i, i] += 1;
            }

            formed = true;
        }
        /// <summary>
        /// Решение СЛАУ при помощи метода Гаусса
        /// </summary>
        public bool GaussElimination(out Tuple<int, SymbolicExpression>[] result)
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
                result[i] = new Tuple<int, SymbolicExpression>(vertexPseudoIndexes[i], matrix[i, matrix.Cols - 1]);
            }

            return true;
        }

        public override string ToString() => "Gauss Elimination";
    }
}