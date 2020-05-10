using System;
using GraphExpectedValue.GraphWidgets;

namespace GraphExpectedValue.GraphLogic
{
    /// <summary>
    /// Представление ребра для сериализации
    /// </summary>
    [Serializable]
    public class EdgeMetadata
    {
        /// <summary>
        /// Номера начальной и конечной вершины ребра
        /// </summary>
        public int StartVertexNumber, EndVertexNumber;
        /// <summary>
        /// Длина ребра
        /// </summary>
        public string Length;

        public EdgeMetadata()
        {

        }
        public EdgeMetadata(Vertex startVertex, Vertex endVertex, string length)
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