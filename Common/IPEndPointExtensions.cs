using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Common
{
    public static class IPEndPointExtensions
    {
        const string SchemeSeperator = "://";

        public static Uri ToUri(this System.Net.IPEndPoint endPoint, string scheme = null)
        {
            if (endPoint == null) throw new ArgumentNullException();                

            return new Uri(string.IsNullOrWhiteSpace(scheme) ? endPoint.ToString() : string.Join(SchemeSeperator, scheme, endPoint.ToString()));
        }
    }
}
