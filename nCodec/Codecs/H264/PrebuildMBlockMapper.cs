using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Codecs.H264
{
    public class PrebuiltMBlockMapper : Mapper
    {

        private MBToSliceGroupMap map;
        private int firstMBInSlice;
        private int groupId;
        private int picWidthInMbs;
        private int indexOfFirstMb;

        public PrebuiltMBlockMapper(MBToSliceGroupMap map, int firstMBInSlice, int picWidthInMbs)
        {
            this.map = map;
            this.firstMBInSlice = firstMBInSlice;
            this.groupId = map.getGroups()[firstMBInSlice];
            this.picWidthInMbs = picWidthInMbs;
            this.indexOfFirstMb = map.getIndices()[firstMBInSlice];
        }

        public int getAddress(int mbIndex)
        {
            return map.getInverse()[groupId][mbIndex + indexOfFirstMb];
        }

        public bool leftAvailable(int mbIndex)
        {
            int mbAddr = map.getInverse()[groupId][mbIndex + indexOfFirstMb];
            int leftMBAddr = mbAddr - 1;

            return !((leftMBAddr < firstMBInSlice) || ((mbAddr % picWidthInMbs) == 0) || (map.getGroups()[leftMBAddr] != groupId));
        }

        public bool topAvailable(int mbIndex)
        {
            int mbAddr = map.getInverse()[groupId][mbIndex + indexOfFirstMb];
            int topMBAddr = mbAddr - picWidthInMbs;

            return !((topMBAddr < firstMBInSlice) || (map.getGroups()[topMBAddr] != groupId));
        }

        public int getMbX(int index)
        {
            return getAddress(index) % picWidthInMbs;
        }

        public int getMbY(int index)
        {
            return getAddress(index) / picWidthInMbs;
        }

        public bool topRightAvailable(int mbIndex)
        {
            int mbAddr = map.getInverse()[groupId][mbIndex + indexOfFirstMb];
            int topRMBAddr = mbAddr - picWidthInMbs + 1;

            return !((topRMBAddr < firstMBInSlice) || (((mbAddr + 1) % picWidthInMbs) == 0) || (map.getGroups()[topRMBAddr] != groupId));
        }

        public bool topLeftAvailable(int mbIndex)
        {
            int mbAddr = map.getInverse()[groupId][mbIndex + indexOfFirstMb];
            int topLMBAddr = mbAddr - picWidthInMbs - 1;

            return !((topLMBAddr < firstMBInSlice) || ((mbAddr % picWidthInMbs) == 0) || (map.getGroups()[topLMBAddr] != groupId));
        }
    }
}
