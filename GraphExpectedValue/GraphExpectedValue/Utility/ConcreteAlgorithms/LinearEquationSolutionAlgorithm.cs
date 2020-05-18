using System;
using System.Collections.Generic;
using System.Linq;
using GraphExpectedValue.GraphLogic;
using GraphExpectedValue.GraphWidgets;
using MathNet.Symbolics;

namespace GraphExpectedValue.Utility.ConcreteAlgorithms
{
    public abstract class LinearEquationSolutionAlgorithm : SolutionAlgorithm
    {
        protected Matrix matrix;

        protected bool formed;

        protected List<int> vertexPseudoIndexes;

        protected List<bool> isEnding;

        protected List<int> vertexMatrixIndex;

        public abstract Tuple<int, SymbolicExpression>[] Solve(GraphMetadata metadata);

        protected void FormMatrices(GraphMetadata metadata)
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

                var parseStartString = metadata.CustomProbabilities ? edge.Probability : "1.0 / " + startVertexDegree;
                var startProba = SymbolicExpression.Parse(parseStartString);

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
                    var parseEndString = metadata.CustomProbabilities ? edge.Probability : "1.0 / " + endVertexDegree;
                    var endProba = SymbolicExpression.Parse(parseEndString);
                    // start vertex is not ending
                    if (!isEnding[startVertexIndex])
                    {
                        matrix[vertexMatrixIndex[endVertexIndex], vertexMatrixIndex[startVertexIndex]] = -endProba;
                    }

                    matrix[vertexMatrixIndex[endVertexIndex], matrix.Cols - 1] = endProba * lengthExpr;
                }
            }

            for (var i = 0; i < matrix.Rows && i < matrix.Cols; i++)
            {
                matrix[i, i] += SymbolicExpression.One;
            }

            formed = true;
        }
    }
}