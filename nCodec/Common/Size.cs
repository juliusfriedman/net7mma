using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Common
{
    public class Size
    {
        private int width;
        private int height;

        public Size(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public int getWidth()
        {
            return width;
        }

        public int getHeight()
        {
            return height;
        }

        public override int GetHashCode()
        {
            int prime = 31;
            int result = 1;
            result = prime * result + height;
            result = prime * result + width;
            return result;
        }

        public override bool Equals(Object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            //if (getClass() != obj.getClass())
            //  return false;
            Size other = (Size)obj;
            if (height != other.height)
                return false;
            if (width != other.width)
                return false;
            return true;
        }

    }
}
