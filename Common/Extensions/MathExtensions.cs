#region Copyright
/*
This file came from Managed Media Aggregation, You can always find the latest version @ https://net7mma.codeplex.com/
  
 Julius.Friedman@gmail.com / (SR. Software Engineer ASTI Transportation Inc. http://www.asti-trans.com)

Permission is hereby granted, free of charge, 
 * to any person obtaining a copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, 
 * including without limitation the rights to :
 * use, 
 * copy, 
 * modify, 
 * merge, 
 * publish, 
 * distribute, 
 * sublicense, 
 * and/or sell copies of the Software, 
 * and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * 
 * JuliusFriedman@gmail.com should be contacted for further details.

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
 * 
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
 * TORT OR OTHERWISE, 
 * ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * 
 * v//
 */
#endregion

namespace Media.Common.Extensions.Math
{
    public static class MathExtensions
    {
        public static byte Clamp(byte value, byte min, byte max)
        {
            return System.Math.Min(System.Math.Max(min, value), max);
        }

        public static int Clamp(int value, int min, int max)
        {
            return System.Math.Min(System.Math.Max(min, value), max);
        }

        public static long Clamp(long value, long min, long max)
        {
            return System.Math.Min(System.Math.Max(min, value), max);
        }

        public static double Clamp(double value, double min, double max)
        {
            return System.Math.Min(System.Math.Max(min, value), max);
        }

        public static byte BinaryClamp(byte value, byte min, byte max)
        {
            return Media.Common.Binary.Clamp(ref value, ref min, ref max);
        }

        public static int BinaryClamp(int value, int min, int max)
        {
            return Media.Common.Binary.Clamp(ref value, ref min, ref max);
        }

        public static long BinaryClamp(long value, long min, long max)
        {
            return Media.Common.Binary.Clamp(ref value, ref min, ref max);
        }

        public static double BinaryClamp(double value, double min, double max)
        {
            return Media.Common.Binary.Clamp(ref value, ref min, ref max);
        }
    }
}
