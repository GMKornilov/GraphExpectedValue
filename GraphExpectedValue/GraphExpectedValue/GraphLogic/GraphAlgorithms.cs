using System;
using System.Collections.Generic;
using System.Linq;
using GraphExpectedValue.GraphWidgets;
using MathNet.Symbolics;

namespace GraphExpectedValue.GraphLogic
{
    [Flags]
    public enum CheckStatus
    {
        Ok = 0,
        EndVertexNotSelected = (1 << 0),
        WrongConnectionComponents = (1 << 1),
        WrongProbabilities = (1 << 2),
        AllVertexesAreEnding = (1 << 3)
    }
    public class GraphAlgorithms
    {
        private List<List<int>> adjacencyList;

        private List<List<int>> reversedAdjacencyList;

        private List<List<SymbolicExpression>> adjProbaList;
        
        private List<bool> used;

        private int _endVertexesAmount;

        private bool _checkProbas;
        public GraphAlgorithms(GraphMetadata metadata)
        {
            FormData(metadata);
            _checkProbas = metadata.CustomProbabilities;
            if (_checkProbas)
            {
                adjProbaList = new List<List<SymbolicExpression>>();
                for (var i = 0; i < metadata.VertexMetadatas.Count; i++)
                {
                    adjProbaList.Add(new List<SymbolicExpression>());
                }

                foreach (var edge in metadata.EdgeMetadatas)
                {
                    var startVertexNumber = edge.StartVertexNumber - 1;
                    adjProbaList[startVertexNumber].Add(Infix.ParseOrThrow(edge.Probability));
                }
            }
        }

        public CheckStatus Check()
        {
            var res = CheckStatus.Ok;
            if (_endVertexesAmount == 0)
            {
                res |= CheckStatus.EndVertexNotSelected;
            }

            if (_endVertexesAmount == adjacencyList.Count)
            {
                res |= CheckStatus.AllVertexesAreEnding;
            }
            var strongComponents = StrongComponents();
            if (strongComponents.Count != 1)
            {
                res |= CheckStatus.WrongConnectionComponents;
            }

            if (_checkProbas && !CheckProbas())
            {
                res |= CheckStatus.WrongProbabilities;
            }

            return res;
        }

        private void FormData(GraphMetadata metadata)
        {
            adjacencyList = new List<List<int>>();
            for (var i = 0; i < metadata.VertexMetadatas.Count; i++)
            {
                adjacencyList.Add(new List<int>());
                if (metadata.VertexMetadatas[i].Type == VertexType.EndVertex)
                {
                    _endVertexesAmount++;
                }
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
                    DFS(i, adjacencyList, order);
                }
            }
            order.Reverse();
            
            var res = new List<List<int>>();
            used = new List<bool>(Enumerable.Repeat(false, adjacencyList.Count));
            foreach (var vertex in order)
            {
                if (!used[vertex])
                {
                    var comp = new List<int>();
                    DFS(vertex, reversedAdjacencyList, comp);
                    res.Add(comp);
                }
            }

            return res;
        }

        private bool CheckProbas()
        {
            foreach (var vertexAdjList in adjProbaList)
            {
                if(vertexAdjList.Count == 0)continue;
                var sum = vertexAdjList.Aggregate(SymbolicExpression.Zero, (current, proba) => current + proba);
                var realSumValue = sum.Evaluate(null).RealValue;
                if (Math.Abs(realSumValue - 1) > 1e-6)
                {
                    return false;
                }
            }

            return true;
        }
    }
}