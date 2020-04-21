using System;
using GraphExpectedValue.GraphWidgets;

namespace GraphExpectedValue.GraphLogic
{
    [Serializable]
    public class EdgeMetadata
    {
        public int StartVertexNumber, EndVertexNumber;
        public double Length;

        public EdgeMetadata()
        {

        }
        public EdgeMetadata(Vertex startVertex, Vertex endVertex, double length)
        {
            StartVertexNumber = startVertex.Number;
            EndVertexNumber = endVertex.Number;
            Length = length;
            startVertex.PropertyChanged += (sender, args) =>
            {
                if (sender is Vertex vertex)
                {
                    StartVertexNumber = vertex.Number;
                }
            };
            endVertex.PropertyChanged += (sender, args) =>
            {
                if (sender is Vertex vertex)
                {
                    EndVertexNumber = vertex.Number;
                }
            };
        }
    }
}