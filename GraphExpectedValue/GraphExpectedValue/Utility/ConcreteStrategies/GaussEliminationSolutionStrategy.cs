using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using GraphExpectedValue.GraphLogic;

namespace GraphExpectedValue.Utility.ConcreteStrategies
{
    public class GaussEliminationSolutionStrategy : SolutionStrategy
    {
        private const double EPS = 1e-6;
        private Matrix matrix;
        private bool formed;
        public double[] Solve(GraphMetadata metadata)
        {
            FormMatrices(metadata);
            if (!GaussElimination(out var result))
            {
                throw new ArgumentException("bad graph");
            }
            return result;
        }

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

                    matrix[startVertexIndex, metadata.VertexMetadatas.Count - 1] += startProba * edge.Length;
                }

                if (!metadata.IsOriented && edge.EndVertexNumber != metadata.EndVertexNumber)
                {
                    var endProba = 1.0 / endVertexDegree;
                    if (edge.StartVertexNumber != metadata.EndVertexNumber)
                    {
                        matrix[endVertexIndex, startVertexIndex] = -endProba;
                    }

                    matrix[endVertexIndex, metadata.VertexMetadatas.Count - 1] = endProba * edge.Length;
                }
            }

            for (var i = 0; i < metadata.VertexMetadatas.Count - 1; i++)
            {
                matrix[i, i] += 1;
            }

            formed = true;
        }

        public bool GaussElimination(out double[] result)
        {
            if (!formed)
            {
                throw new Exception("Form matrix before doing elimination");
            }

            //for (var col = 0; col < matrix.Cols - 1; col++)
            //{ 
            //    if (Math.Abs(matrix[col, col]) < EPS)
            //    {
            //        var swapRow = col + 1;
            //        var foundPivot = false;
            //        for (; swapRow < matrix.Rows; swapRow++)
            //        {
            //            if (Math.Abs(matrix[swapRow, col]) > EPS)
            //            {
            //                foundPivot = true;
            //                break;
            //            }
            //        }

            //        if (!foundPivot)
            //        {
            //            result = null;
            //            return false;
            //        }

            //        matrix.SwapRows(col, swapRow);
            //    }
            //    matrix.MultiplyRow(col, 1 / matrix[col, col]);
            //    for (var elimRow = 0; elimRow < matrix.Rows; elimRow++)
            //    {
            //        if (elimRow == col || Math.Abs(matrix[elimRow, col]) < EPS)
            //        {
            //            continue;
            //        }
            //        matrix.AddRow(col, elimRow, -matrix[elimRow, col]);
            //    }
            //}
            matrix.ShowMatrix();
            matrix.GaussElimination();
            matrix.ShowMatrix();
            for (var checkRow = 0; checkRow < matrix.Rows; checkRow++)
            {
                if (Math.Abs(matrix[checkRow, checkRow] - 1) > EPS)
                {
                    result = null;
                    return false;
                }
            }
            result = new double[matrix.Rows];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = matrix[i, matrix.Cols - 1];
            }

            return true;
        }

        public override string ToString() => "Gauss Elimination";
    }
}