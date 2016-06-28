using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo
{
    public static class Arrays
    {
        public static void fill<T>(T[] t, T item)
        {
            for (int i = 0; i < t.Length; ++i)
                t[i] = item;
        }
    }
}
