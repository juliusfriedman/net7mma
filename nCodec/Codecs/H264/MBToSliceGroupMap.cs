using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nVideo
{
    public class MBToSliceGroupMap
    {
        private int[] groups;
        private int[] indices;
        private int[][] inverse;

        public MBToSliceGroupMap(int[] groups, int[] indices, int[][] inverse)
        {
            this.groups = groups;
            this.indices = indices;
            this.inverse = inverse;
        }

        public int[] getGroups()
        {
            return groups;
        }

        public int[] getIndices()
        {
            return indices;
        }

        public int[][] getInverse()
        {
            return inverse;
        }
    }
}
