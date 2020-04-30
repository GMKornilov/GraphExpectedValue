using System.Collections.Generic;
using System.Linq;

namespace GraphExpectedValue.GraphLogic
{
    public class GraphAlgorithms
    {
        private List<List<int>> adjacencyList;
        private List<List<int>> reversedAdjacencyList;
        private List<bool> used;
        private int startVertex;
        private bool isOriented;

        public GraphAlgorithms(GraphMetadata metadata) => FormData(metadata);

        public bool Check()
        {
            if (startVertex == -1) return false;
            var strongComponents = StrongComponents();
            return strongComponents.Count == 1;
        }

        private void FormData(GraphMetadata metadata)
        {
            adjacencyList = new List<List<int>>(
                Enumerable.Repeat(
                    new List<int>(),
                    metadata.VertexMetadatas.Count
                )
            );
            reversedAdjacencyList = new List<List<int>>(
                Enumerable.Repeat(
                    new List<int>(),
                    metadata.VertexMetadatas.Count
                )
            );
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

            startVertex = metadata.EndVertexNumber - 1;
            isOriented = metadata.IsOriented;
        }

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