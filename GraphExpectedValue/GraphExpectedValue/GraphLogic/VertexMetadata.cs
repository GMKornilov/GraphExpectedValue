using System;
using System.Windows;
using GraphExpectedValue.GraphWidgets;

namespace GraphExpectedValue.GraphLogic
{
    [Serializable]
    public class VertexMetadata
    {
        private Vertex graphicRepr;
        public int Number => graphicRepr.Number;
        public VertexType Type => graphicRepr.VertexType;
        public Point Position => graphicRepr.Center;

        public VertexMetadata(Vertex repr)
        {
            graphicRepr = repr;
        }
    }
}