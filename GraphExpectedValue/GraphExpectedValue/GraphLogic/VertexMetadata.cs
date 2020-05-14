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

        public VertexMetadata(Vertex graphicVertex) : this(graphicVertex.Number, graphicVertex.VertexType, graphicVertex.Center)
        {

        }

        public VertexMetadata(int number, VertexType type, Point position)
        {
            Number = number;
            Type = type;
            Position = position;
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