using System;
using System.Collections.Generic;
using GraphExpectedValue.Utility;
using GraphExpectedValue.Utility.ConcreteAlgorithms;
using MathNet.Symbolics;

namespace GraphExpectedValue.GraphLogic
{
    [Serializable]
    public class GraphMetadata
    {
        public bool IsOriented { get; set; }
        
        public List<VertexMetadata> VertexMetadatas { get; private set; }
       
        public List<EdgeMetadata> EdgeMetadatas { get; private set; }

        public bool CustomProbabilities { get; set; }
        
        public static SolutionAlgorithm solutionStrategy = new GaussEliminationSolutionAlgorithm();

        public GraphMetadata()
        {
            VertexMetadatas = new List<VertexMetadata>();
            EdgeMetadatas = new List<EdgeMetadata>();
            CustomProbabilities = false;
        }

        public GraphMetadata(List<VertexMetadata> vertexMetadatas, List<EdgeMetadata> edgeMetadatas, bool customProbabilities)
        {
            VertexMetadatas = vertexMetadatas;
            EdgeMetadatas = edgeMetadatas;
            CustomProbabilities = customProbabilities;
        }
        
        public Tuple<int, SymbolicExpression>[] Solve()
        {
            return solutionStrategy.Solve(this);
        }
    }
}