using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo
{
    public class Rect
    {
        private int x;
        private int y;
        private int width;
        private int height;

        public Rect(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public int getX()
        {
            return x;
        }

        public int getY()
        {
            return y;
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
            result = prime * result + x;
            result = prime * result + y;
            return result;
        }

        public override bool Equals(Object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            Rect other = (Rect)obj;
            if (height != other.height)
                return false;
            if (width != other.width)
                return false;
            if (x != other.x)
                return false;
            if (y != other.y)
                return false;
            return true;
        }

        public override String ToString()
        {
            return "Rect [x=" + x + ", y=" + y + ", width=" + width + ", height=" + height + "]";
        }
    }
}
