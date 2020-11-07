using GraphExpectedValue.GraphLogic;
using GraphExpectedValue.GraphWidgets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GraphExpectedValue.Utility.ConcreteGraphIO
{
    class GraphMatrixIO : GraphReader
    {
        private double _height, _width;

        public GraphMatrixIO(double width, double height)
        {
            _width = width;
            _height = height;
        }

        public GraphMetadata ReadGraph(Stream stream)
        {
            int totalVertexes = -1, readVertexes, currentVertex = 1;
            var vertexMetadatas = new List<VertexMetadata>();
            var edgeMetadatas = new List<EdgeMetadata>();

            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var lineSplitted = line.Split(' ');
                    readVertexes = lineSplitted.Length;
                    if (totalVertexes == -1)
                    {
                        totalVertexes = readVertexes;
                    }
                    if (totalVertexes != readVertexes)
                    {
                        throw new ArgumentException("different amount of vertexes in different lines");
                    }

                    for (var neighVertex = 1; neighVertex <= lineSplitted.Length; neighVertex++)
                    {
                        var length = lineSplitted[neighVertex - 1];
                        if (length == "0")
                        {
                            continue;
                        }

                        if (neighVertex == currentVertex)
                        {
                            throw new ArgumentException("loop edges are not allowed");
                        }

                        var edgeMetadata = new EdgeMetadata
                        {
                            StartVertexNumber = currentVertex,
                            EndVertexNumber = neighVertex,
                            Length = length
                        };

                        edgeMetadatas.Add(edgeMetadata);
                    }

                    currentVertex++;
                }
                var center = new Point(_width / 2, _height / 2);
                var radius = Math.Min(_width, _height) / 3;
                for (var vertexNumber = 1; vertexNumber <= totalVertexes; vertexNumber++)
                {
                    var x = Math.Cos(2.0 * (vertexNumber - 1) / totalVertexes * Math.PI) * radius + center.X;
                    var y = Math.Sin(2.0 * (vertexNumber - 1) / totalVertexes * Math.PI) * radius + center.Y;
                    var vertexMetadata = new VertexMetadata(
                        vertexNumber,
                        VertexType.PathVertex,
                        new Point(x, y)
                    );
                    vertexMetadatas.Add(vertexMetadata);
                }
                var graphMetadata = new GraphMetadata(vertexMetadatas, edgeMetadatas, false)
                {
                    IsOriented = true
                };
                return graphMetadata;
            }
        }
    }
}
