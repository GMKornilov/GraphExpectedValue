using GraphExpectedValue.GraphLogic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphExpectedValue.Utility
{
    public interface GraphWriter
    {
        void WriteGraph(GraphMetadata metadata, Stream stream);
    }
}
