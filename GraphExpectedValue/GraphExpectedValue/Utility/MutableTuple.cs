using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphExpectedValue.Utility
{
    public class MutableTuple<T1, T2, T3>
    {
        private T1 field1;
        private T2 field2;
        private T3 field3;

        public T1 Item1
        {
            get => field1;
            set => field1 = value;
        }

        public T2 Item2
        {
            get => field2;
            set => field2 = value;
        }

        public T3 Item3
        {
            get => field3;
            set => field3 = value;
        }

        public MutableTuple(T1 item1, T2 item2, T3 item3)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
        }

        public void Deconstruct(out T1 item1, out T2 item2, out T3 item3)
        {
            item1 = Item1;
            item2 = Item2;
            item3 = Item3;
        }
    }
}
