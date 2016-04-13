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

namespace Media.Common.Extensions.IPEndPoint
{
    public static class IPEndPointExtensions
    {
        const char PortSeperator = ':';

        const string SchemeSeperator = "://";

        public static readonly System.Net.IPEndPoint Any = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 0);

        public static readonly System.Net.IPEndPoint IPv6Any = new System.Net.IPEndPoint(System.Net.IPAddress.IPv6Any, 0);

        public static bool IsMulticast(this System.Net.IPEndPoint endPoint) { return Common.Extensions.IPAddress.IPAddressExtensions.IsMulticast(endPoint.Address); }

        public static System.Uri ToUri(this System.Net.IPEndPoint endPoint, string scheme = null)
        {
            if (endPoint == null) throw new System.ArgumentNullException();                

            return new System.Uri(string.IsNullOrWhiteSpace(scheme) ? endPoint.ToString() : string.Join(SchemeSeperator, scheme, endPoint.ToString()));
        }

        public static System.Net.IPEndPoint Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new System.ArgumentNullException();

            string[] parts = text.Split(PortSeperator);

            if (parts.Length > 1)
            {
                return new System.Net.IPEndPoint(System.Net.IPAddress.Parse(parts[0]), int.Parse(parts[1]));
            }

            return new System.Net.IPEndPoint(System.Net.IPAddress.Parse(parts[0]), 0);
        }

        public static bool TryParse(string text, out System.Net.IPEndPoint result)
        {
            try
            {
                result = Parse(text);

                return true;
            }
            catch
            {
                result = null;
                
                return false;
            }
        }
    }
}
