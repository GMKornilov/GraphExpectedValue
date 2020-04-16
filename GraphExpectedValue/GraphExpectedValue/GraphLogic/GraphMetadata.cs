using System;
using System.Collections.Generic;
using GraphExpectedValue.GraphWidgets;

namespace GraphExpectedValue.GraphLogic
{
    [Serializable]
    public class GraphMetadata
    {
        public bool IsOriented { get; set; }
        public int StartVertexNumber { get; set; }
        public int EndVertexNumber { get; set; }
        public List<VertexMetadata> VertexMetadatas { get; private set; }
        public List<EdgeMetadata> EdgeMetadatas { get; private set; }

        public GraphMetadata()
        {
            StartVertexNumber = -1;
            EndVertexNumber = -1;
            VertexMetadatas = new List<VertexMetadata>();
            EdgeMetadatas = new List<EdgeMetadata>();
        }

        public GraphMetadata(List<VertexMetadata> vertexMetadatas, List<EdgeMetadata> edgeMetadatas, int startVertexNumber = -1, int endVertexNumber = -1)
        {
            StartVertexNumber = startVertexNumber;
            EndVertexNumber = endVertexNumber;

            VertexMetadatas = vertexMetadatas;
            EdgeMetadatas = edgeMetadatas;
        }
    }
}