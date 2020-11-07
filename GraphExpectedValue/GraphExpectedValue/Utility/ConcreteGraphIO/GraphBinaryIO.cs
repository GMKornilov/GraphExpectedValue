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
    class GraphBinaryIO : GraphReader, GraphWriter
    {
        private VertexMetadata ReadVertex(BinaryReader reader)
        {
            int number;
            string vertexTypeStr;
            VertexType vertexType;
            double pointX, pointY;
            Point position;

            number = reader.ReadInt32();
            
            vertexTypeStr = reader.ReadString();
            vertexType = (VertexType)Enum.Parse(typeof(VertexType), vertexTypeStr);

            pointX = reader.ReadDouble();
            pointY = reader.ReadDouble();
            position = new Point(pointX, pointY);

            return new VertexMetadata(number, vertexType, position);
        }

        private EdgeMetadata ReadEdge(BinaryReader reader, bool isOriented, bool customProbas)
        {
            int startVertexNumber, endVertexNumber;

            string length, probability = "", backProbability = "";

            startVertexNumber = reader.ReadInt32();
            endVertexNumber = reader.ReadInt32();

            length = reader.ReadString();
            if(customProbas)
            {
                probability = reader.ReadString();
                if(!isOriented)
                {
                    backProbability = reader.ReadString();
                }
            }

            var edgeMetadata = new EdgeMetadata
            {
                StartVertexNumber = startVertexNumber,
                EndVertexNumber = endVertexNumber,
                Length = length,
                Probability = probability,
                BackProbability = backProbability
            };

            return edgeMetadata;
        }

        public GraphMetadata ReadGraph(Stream stream)
        {
            int vertexes, edges;
            List<VertexMetadata> vertexMetadatas;
            List<EdgeMetadata> edgeMetadatas;
            bool isOriented, customProbas;

            using (var reader = new BinaryReader(stream))
            {
                vertexes = reader.ReadInt32();
                edges = reader.ReadInt32();

                isOriented = reader.ReadBoolean();
                customProbas = reader.ReadBoolean();

                vertexMetadatas = new List<VertexMetadata>(vertexes);
                edgeMetadatas = new List<EdgeMetadata>(edges);

                for(var i = 0; i < vertexes; i++)
                {
                    var vertexMetadata = ReadVertex(reader);
                    vertexMetadatas.Add(vertexMetadata);
                }

                for(var i = 0; i < edges; i++)
                {
                    var edgeMetadata = ReadEdge(reader, isOriented, customProbas);
                    edgeMetadatas.Add(edgeMetadata);
                }
            }

            var res = new GraphMetadata(vertexMetadatas, edgeMetadatas, customProbas);
            res.IsOriented = isOriented;
            return res;
        }

        public void WriteEdge(BinaryWriter writer, EdgeMetadata metadata, bool customProba, bool isOriented)
        {
            writer.Write(metadata.StartVertexNumber);
            writer.Write(metadata.EndVertexNumber);
            writer.Write(metadata.Length);
            if (customProba)
            {
                writer.Write(metadata.Probability);
                if (!isOriented)
                {
                    writer.Write(metadata.BackProbability);
                }
            }
        }

        public void WriteGraph(GraphMetadata metadata, Stream stream)
        {
            var edgeCount = metadata.EdgeMetadatas.Count + metadata.EdgeMetadatas.Count(edgeMetadata => !string.IsNullOrEmpty(edgeMetadata.BackLength));
            using(var writer = new BinaryWriter(stream))
            {
                writer.Write(metadata.VertexMetadatas.Count);
                writer.Write(edgeCount);
                
                writer.Write(metadata.IsOriented);
                writer.Write(metadata.CustomProbabilities);

                foreach(var vertexMetadata in metadata.VertexMetadatas)
                {
                    writer.Write(vertexMetadata.Number);
                    writer.Write(vertexMetadata.Type.ToString());
                    writer.Write(vertexMetadata.Position.X);
                    writer.Write(vertexMetadata.Position.Y);
                }

                foreach(var edgeMetadata in metadata.EdgeMetadatas)
                {
                    WriteEdge(writer, edgeMetadata, metadata.CustomProbabilities, metadata.IsOriented);
                    if (!string.IsNullOrEmpty(edgeMetadata.BackLength))
                    {
                        var tempMetadata = new EdgeMetadata()
                        {
                            StartVertexNumber = edgeMetadata.EndVertexNumber,
                            EndVertexNumber = edgeMetadata.StartVertexNumber,
                            Length = edgeMetadata.BackLength,
                            Probability = edgeMetadata.BackProbability,
                            BackProbability = edgeMetadata.Probability
                        };
                        WriteEdge(writer, tempMetadata, metadata.CustomProbabilities, metadata.IsOriented);
                    }
                }
            }
        }
    }
}
