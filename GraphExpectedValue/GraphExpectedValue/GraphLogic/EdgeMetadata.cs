using System;

namespace GraphExpectedValue.GraphLogic
{
    [Serializable]
    public class EdgeMetadata
    {
        public VertexMetadata StartVertexMetadata, EndVertexMetadata;
        public double Length;
    }
}