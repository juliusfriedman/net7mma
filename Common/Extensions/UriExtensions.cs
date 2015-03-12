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

namespace Media.Common.Extensions.Uri
{
    public static class UriExtensions
    {
        internal static char[] CredentialSplit = new char[] { (char)Common.ASCII.AtSign, (char)Common.ASCII.ForwardSlash, (char)Common.ASCII.BackSlash, (char)Common.ASCII.Colon };

        public static System.Net.NetworkCredential ParseUserInfo(System.Uri uri)
        {
            if (uri == null || string.IsNullOrWhiteSpace(uri.UserInfo)) return null;

            System.Net.NetworkCredential result = null;

            //Split into tokens taking only 3 tokens max
            string[] parts = uri.UserInfo.Split(CredentialSplit, 3);

            //cache the length of the split
            int partsLength = parts.Length;

            //If there are atleast two tokens
            if (partsLength > 1)
            {
                //If there is a domain use it
                if (partsLength > 2)
                {
                    result = new System.Net.NetworkCredential(parts[0], parts[2], parts[1]);
                }
                else //Use the username and password. (optionally use the host as the domain)
                {
                    result = new System.Net.NetworkCredential(parts[0], parts[1]);//, uri.Host);
                }
            }//There was only one token?

            return result;
        }

        public static bool TryParseUserInfo(System.Uri uri, out System.Net.NetworkCredential result) { return (result = ParseUserInfo(uri)) != null; }

    }
}
