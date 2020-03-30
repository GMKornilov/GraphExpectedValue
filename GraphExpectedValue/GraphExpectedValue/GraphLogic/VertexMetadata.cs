using System;
using System.Windows;
using GraphExpectedValue.GraphWidgets;

namespace GraphExpectedValue.GraphLogic
{
    [Serializable]
    public class VertexMetadata
    {
        public int Number { get; set; }
        public VertexType Type { get; set; }
        public Point Position { get; set; }

        public VertexMetadata(Vertex graphicVertex)
        {
            Number = graphicVertex.Number;
            Type = graphicVertex.VertexType;
            Position = graphicVertex.Center;
        }

        public VertexMetadata()
        {

        }

        public override bool Equals(object obj)
        {
            if (obj is VertexMetadata metadata)
            {
                return Number == metadata.Number;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Number;
        }
    }
}