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

namespace Media.Sockets
{
    public class IPAddressInformation
        : System.Net.NetworkInformation.IPAddressInformation
    {

        public IPAddressInformation(System.Net.IPAddress address, bool usesDns, bool transient)
        {
            if (address == null) throw new System.ArgumentNullException("address");

            IPAddress = address;

            UsesDNS = usesDns;

            Transient = transient;
        }

        public IPAddressInformation(System.Net.IPAddress address)
            : this(address, false, false)
        {

        }

        protected readonly System.Net.IPAddress IPAddress = System.Net.IPAddress.None;

        protected readonly bool UsesDNS;

        protected readonly bool Transient;

        public override System.Net.IPAddress Address
        {
            get { return IPAddress; }
        }

        public override bool IsDnsEligible
        {
            get { return UsesDNS; }
        }

        public override bool IsTransient
        {
            get { return Transient; }
        }
    }
}
