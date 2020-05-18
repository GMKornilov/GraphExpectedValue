using System;
using System.Collections.Generic;
using GraphExpectedValue.GraphWidgets;
using GraphExpectedValue.Utility;
using GraphExpectedValue.Utility.ConcreteStrategies;
using MathNet.Symbolics;

namespace GraphExpectedValue.GraphLogic
{
    /// <summary>
    /// Представление графа для сериализации
    /// </summary>
    [Serializable]
    public class GraphMetadata
    {
        /// <summary>
        /// Является ли граф ориентированным
        /// </summary>
        public bool IsOriented { get; set; }
        //public int StartVertexNumber { get; set; }
        /// <summary>
        /// Список представлений вершин для сериализации
        /// </summary>
        public List<VertexMetadata> VertexMetadatas { get; private set; }
        /// <summary>
        /// Список представлений ребер для сериализации
        /// </summary>
        public List<EdgeMetadata> EdgeMetadatas { get; private set; }

        public bool CustomProbabilities { get; set; }
        /// <summary>
        /// Стратегия нахождения искомых математических ожиданий
        /// </summary>
        public static SolutionStrategy solutionStrategy = new GaussEliminationSolutionStrategy();

        public GraphMetadata()
        {
            //StartVertexNumber = -1;
            VertexMetadatas = new List<VertexMetadata>();
            EdgeMetadatas = new List<EdgeMetadata>();
            CustomProbabilities = false;
        }

        public GraphMetadata(List<VertexMetadata> vertexMetadatas, List<EdgeMetadata> edgeMetadatas, bool customProbabilities)
        {
            //StartVertexNumber = startVertexNumber;

            VertexMetadatas = vertexMetadatas;
            EdgeMetadatas = edgeMetadatas;
            CustomProbabilities = customProbabilities;
        }
        /// <summary>
        /// Находит искомые математические ожидания
        /// </summary>
        public Tuple<int, SymbolicExpression>[] Solve()
        {
            return solutionStrategy.Solve(this);
        }
    }
}