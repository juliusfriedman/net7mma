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
    #region Listener

    /// <summary>
    /// Represents an Inbound Connection.
    /// </summary>
    public class Listener : NetworkConnection
    {

        public Listener(bool registerForEvents)
            : base()
        {
            Initialize(registerForEvents);
        }

        public Listener() : this(false) { }

        public bool IsBound { get { return LocalEndPoint != null; } }

        public virtual void Bind(System.Net.IPEndPoint localEndPoint, System.Net.NetworkInformation.NetworkInterface networkInterface)
        {
            //Check not already attached.
            if (false == NetworkConnectionFlags.HasFlag(NetworkConnectionState.Bound))
            {
                //Check for a null input
                if (localEndPoint == null) throw new System.ArgumentNullException("localEndPoint");

                //Ensure the latest details are availble.
                Refresh();

                //Double check not bound
                if (IsBound) throw new System.InvalidOperationException("Already Bound on: " + LocalEndPoint.ToString());

                //Assign the end point
                LocalEndPoint = localEndPoint;

                //Assign the network interface
                NetworkInterface = networkInterface ?? Common.Extensions.NetworkInterface.NetworkInterfaceExtensions.GetNetworkInterface(localEndPoint);

                //Indicate Bound
                FlagBound();
            }
        }

        public void Unbind()
        {
            //Check not already attached.
            if (IsBound)
            {
                ConnectionSocket.Close();

                //Indicate No longer bound
                UnFlagBound();
            }
        }

        public override void Dispose()
        {
            Unbind();

            base.Dispose();
        }
    }

    #endregion
}
