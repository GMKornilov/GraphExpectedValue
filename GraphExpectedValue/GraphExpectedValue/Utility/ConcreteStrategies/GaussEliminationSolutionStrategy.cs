using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using GraphExpectedValue.GraphLogic;
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
        /// Решение СЛАУ при помощи метода Гаусса
        /// </summary>
        /// <param name="metadata">Данные графа</param>
        /// <returns>Искомые математические ожидания</returns>
        public SymbolicExpression[] Solve(GraphMetadata metadata)
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
            matrix = new Matrix(metadata.VertexMetadatas.Count - 1, metadata.VertexMetadatas.Count);
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
                startVertexIndex -= (startVertexIndex >= metadata.EndVertexNumber) ? 1 : 0;

                var endVertexIndex = edge.EndVertexNumber - 1;
                endVertexIndex -= (endVertexIndex >= metadata.EndVertexNumber) ? 1 : 0;

                if (edge.StartVertexNumber != metadata.EndVertexNumber)
                {
                    if (edge.EndVertexNumber != metadata.EndVertexNumber)
                    {
                        matrix[startVertexIndex, endVertexIndex] = -startProba;
                    }

                    matrix[startVertexIndex, metadata.VertexMetadatas.Count - 1] += startProba * lengthExpr;
                }

                if (!metadata.IsOriented && edge.EndVertexNumber != metadata.EndVertexNumber)
                {
                    var endProba = 1.0 / endVertexDegree;
                    if (edge.StartVertexNumber != metadata.EndVertexNumber)
                    {
                        matrix[endVertexIndex, startVertexIndex] = -endProba;
                    }

                    matrix[endVertexIndex, metadata.VertexMetadatas.Count - 1] = endProba * lengthExpr;
                }
            }

            for (var i = 0; i < metadata.VertexMetadatas.Count - 1; i++)
            {
                matrix[i, i] += 1;
            }

            formed = true;
        }
        /// <summary>
        /// Решение СЛАУ при помощи метода Гаусса
        /// </summary>
        public bool GaussElimination(out SymbolicExpression[] result)
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
            result = new SymbolicExpression[matrix.Rows];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = matrix[i, matrix.Cols - 1];
            }

            return true;
        }

        public override string ToString() => "Gauss Elimination";
    }
}