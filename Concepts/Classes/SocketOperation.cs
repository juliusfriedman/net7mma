using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Concepts.Classes
{
    public class SocketOperation
    {
    }

    public static class Test
    {
        static void  TestCode()
        {
            var options =  System.Net.Sockets.SocketInformationOptions.Connected; //socketInformation.Options;

            bool AcceptedSocketIsListening = (options & System.Net.Sockets.SocketInformationOptions.Listening) != 0;

            bool AcceptedSocketIsConnected = (options & System.Net.Sockets.SocketInformationOptions.Connected) != 0;

            bool AcceptedSocketIBlocking = (options & System.Net.Sockets.SocketInformationOptions.NonBlocking) == 0;

            bool AcceptedSocketUseOverlappedIO = (options & System.Net.Sockets.SocketInformationOptions.UseOnlyOverlappedIO) != 0;

            #region References

            //Get the WSAPROTOCOL_INFO or equivenent under mono..
            //https://msdn.microsoft.com/en-us/library/windows/desktop/ms741675%28v=vs.85%29.aspx
            //https://msdn.microsoft.com/en-us/library/windows/desktop/ms741671%28v=vs.85%29.aspx
            //http://blogs.msdn.com/b/malarch/archive/2005/12/26/507461.aspx
            //https://github.com/mono/mono/blob/master/mcs/class/System/System.Net.Sockets/Socket.cs
            //https://github.com/mono/mono/blob/master/mcs/class/System/System.Net.Sockets/SocketInformation.cs
            //http://lists.ximian.com/pipermail/mono-bugs/2007-November/064068.html
            //http://stackoverflow.com/questions/19799701/socket-duplicateandclose-and-recreate-socket-by-socketinformation

            #endregion

            byte[] AcceptedSocketProtocolInformation = null; //socketInformation.ProtocolInformation;

            long protocolInformationLength;

            if (Media.Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(AcceptedSocketProtocolInformation, out protocolInformationLength)) throw new System.Exception("Unexpected ProtocolInformation");

            System.Net.IPEndPoint localIpEndPoint = null,//(System.Net.IPEndPoint)localEndPoint,
            remoteIpEndPoint = null;// (System.Net.IPEndPoint)remoteEndPoint;

            System.Console.WriteLine("LocalIPEndPoint:" + localIpEndPoint);

            System.Console.WriteLine("RemoteIPEndPoint:" + remoteIpEndPoint);

            int localIpOffset = Utility.Find(AcceptedSocketProtocolInformation, localIpEndPoint.Address.GetAddressBytes(), 0, (int)protocolInformationLength),
                localPortOffset = Utility.Find(AcceptedSocketProtocolInformation, Media.Common.Binary.GetBytes((short)localIpEndPoint.Port), 0, (int)protocolInformationLength),
                remoteIpOffset = Utility.Find(AcceptedSocketProtocolInformation, remoteIpEndPoint.Address.GetAddressBytes(), 0, (int)protocolInformationLength),
                remotePortOffset = Utility.Find(AcceptedSocketProtocolInformation, Media.Common.Binary.GetBytes((short)remoteIpEndPoint.Port), 0, (int)protocolInformationLength);

            System.Net.Sockets.AddressFamily FoundAddressFamily = (System.Net.Sockets.AddressFamily)Media.Common.Binary.Read32(AcceptedSocketProtocolInformation, 0, false);

            System.Net.Sockets.SocketType FoundSocketType = (System.Net.Sockets.SocketType)Media.Common.Binary.Read32(AcceptedSocketProtocolInformation, 4, false);

            System.Net.Sockets.ProtocolType FoundProtocolType = (System.Net.Sockets.ProtocolType)Media.Common.Binary.Read32(AcceptedSocketProtocolInformation, 8, false);

            System.Net.Sockets.ProtocolType BoundProtocolType = (System.Net.Sockets.ProtocolType)Media.Common.Binary.Read32(AcceptedSocketProtocolInformation, 12, false);

            System.IntPtr SocketPtr = (System.IntPtr)Media.Common.Binary.Read32(AcceptedSocketProtocolInformation, 16, false);

            System.Console.WriteLine("localIpOffset:" + localIpOffset);

            System.Console.WriteLine("localPortOffset:" + localPortOffset);

            System.Console.WriteLine("remoteIpOffset:" + remoteIpOffset);

            System.Console.WriteLine("remotePortOffset:" + remotePortOffset);

            System.Console.WriteLine("FoundAddressFamily:" + FoundAddressFamily);

            System.Console.WriteLine("FoundSocketType:" + FoundSocketType);

            System.Console.WriteLine("FoundProtocolType:" + FoundProtocolType);

            System.Console.WriteLine("BoundProtocolType:" + BoundProtocolType);

            System.Console.WriteLine("SocketPtr:" + SocketPtr);

            //information.ProtocolInformation
        }
    }
}
