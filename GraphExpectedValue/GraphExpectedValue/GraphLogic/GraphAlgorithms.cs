using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphExpectedValue.GraphLogic
{
    [Flags]
    public enum CheckStatus
    {
        Ok = 0,
        EndVertexNotSelected = (1 << 0),
        WrongConnectionComponents = (1 << 1)
    }
    /// <summary>
    /// Класс, проверяющий корректность заданного графа
    /// </summary>
    public class GraphAlgorithms
    {
        /// <summary>
        /// Список смежности данного графа
        /// </summary>
        private List<List<int>> adjacencyList;
        /// <summary>
        /// Список смежности транспорированного графа
        /// </summary>
        private List<List<int>> reversedAdjacencyList;
        /// <summary>
        /// Список, проверяющий, какие вершины были посещены во время обхода в глубину
        /// </summary>
        private List<bool> used;
        /// <summary>
        /// Поглощающая вершина в данном графе
        /// </summary>
        private int startVertex;

        public GraphAlgorithms(GraphMetadata metadata) => FormData(metadata);
        /// <summary>
        /// Проверяет граф, данный в конструкторе, на корректность
        /// </summary>
        public CheckStatus Check()
        {
            var res = CheckStatus.Ok;
            if (startVertex == -1)
            {
                res |= CheckStatus.EndVertexNotSelected;
            }
            var strongComponents = StrongComponents();
            if (strongComponents.Count != 1)
            {
                res |= CheckStatus.WrongConnectionComponents;
            }

            return res;
        }
        /// <summary>
        /// Формирует списки смежности заданного графа
        /// </summary>
        /// <param name="metadata">Данные о графе</param>
        private void FormData(GraphMetadata metadata)
        {
            adjacencyList = new List<List<int>>();
            for (var i = 0; i < metadata.VertexMetadatas.Count; i++)
            {
                adjacencyList.Add(new List<int>());
            }
            reversedAdjacencyList = new List<List<int>>();
            for (var i = 0; i < metadata.VertexMetadatas.Count; i++)
            {
                reversedAdjacencyList.Add(new List<int>());
            }
            used = new List<bool>(
                Enumerable.Repeat(false, metadata.VertexMetadatas.Count)
            );
            foreach (var edge in metadata.EdgeMetadatas)
            {
                var start = edge.StartVertexNumber - 1;
                var end = edge.EndVertexNumber - 1;
                adjacencyList[start].Add(end);
                reversedAdjacencyList[end].Add(start);
                if (!metadata.IsOriented)
                {
                    adjacencyList[end].Add(start);
                    reversedAdjacencyList[start].Add(end);
                }
            }

            if (metadata.EndVertexNumber == -1)
            {
                startVertex = -1;
            }
            else
            {
                startVertex = metadata.EndVertexNumber - 1;
            }
        }
        /// <summary>
        /// Обход графа в глубину по заданному списку смежности, заодно запоминая обратный порядок обхода.
        /// </summary>
        /// <param name="vertexNumber">Номер текущей рассматриваемой вершины</param>
        /// <param name="adjList">Список смежности графа</param>
        /// <param name="content">Обратный порядок обхода</param>
        private void DFS(
            int vertexNumber,
            List<List<int>> adjList,
            List<int> content
        )
        {
            used[vertexNumber] = true;
            foreach (var neigh in adjList[vertexNumber].Where(neigh => !used[neigh]))
            {
                DFS(neigh, adjList, content);
            }
            content.Add(vertexNumber);
        }
        /// <summary>
        /// Находит компоненты сильной связанности данного графа
        /// </summary>
        /// <returns>Список списков - все компоненты сильной связанности</returns>
        private List<List<int>> StrongComponents()
        {
            var order = new List<int>();
            for (var i = 0; i < adjacencyList.Count; i++)
            {
                if (!used[i])
                {
                    DFS(i, reversedAdjacencyList, order);
                }
            }
            var res = new List<List<int>>();
            used = new List<bool>(Enumerable.Repeat(false, adjacencyList.Count));
            for (var i = 0; i < order.Count; i++)
            {
                if (!used[i])
                {
                    var comp = new List<int>();
                    DFS(i, adjacencyList, comp);
                    res.Add(comp);
                }
            }

            return res;
        }
    }
}