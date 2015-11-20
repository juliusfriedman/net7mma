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

#region Using Statements

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Media.Common;

#endregion

namespace Media
{
    /// <summary>
    /// Contains common functions
    /// </summary>
    [CLSCompliant(true)]
    public static class Utility
    {
        #region Properties

        public readonly static Random Random = new Random();

        #endregion

        /// <summary>
        /// Indicates the position of the match in a given buffer to a given set of octets.
        /// If the match fails the start parameter will reflect the position of the last partial match, otherwise it will be incremented by <paramref name="octetCount"/>
        /// Additionally start and count will reflect the position of the last partially matched byte, E.g. if 1 octets were match start was incremented by 1.
        /// </summary>
        /// <param name="buffer">The bytes to search</param>
        /// <param name="start">The 0 based index to to start the forward based search</param>
        /// <param name="count">The amount of bytes to search in the buffer</param>
        /// <param name="octets">The bytes to search for</param>
        /// <param name="octetStart">The 0 based offset in the octets to search from</param>
        /// <param name="octetCount">The amount of octets required for a successful match</param>
        /// <returns>
        /// -1 if the match failed or could not be performed; otherwise,
        /// the position within the buffer reletive to the start position in which the first occurance of octets given the octetStart and octetCount was matched.
        /// If more than 1 octet is required for a match and the buffer does not encapsulate the entire match start will still reflect the occurance of the partial match.
        /// </returns>
        public static int ContainsBytes(byte[] buffer, ref int start, ref int count, byte[] octets, int octetStart, int octetCount)
        {
            //If the buffer or the octets are null no dice
            if (buffer == null || octets == null) return -1;

            //Cache the lengths
            int bufferLength = buffer.Length, octetsLength = octets.Length;

            //Make sure there is no way to run out of bounds given correct input
            if (bufferLength < octetCount || start + octetCount > bufferLength || octetCount > octetsLength) return -1;

            //Nothing to search nothing to return, leave start where it was.
            if (octetCount == 0 && bufferLength == 0 || count == 0) return -1;

            //Create the variables we will use in the searching process
            int checkedBytes = 0, matchedBytes = 0, lastPosition = -1;

            //Ensure we account for the bytes checked.
            int position = start + checkedBytes,
                depth = bufferLength - position;

            //Loop the buffer from start to count while the checkedBytes has not increased past the amount of octets required.
            while (count > 0 &&
                start < bufferLength &&
                matchedBytes < octetCount)
            {
                //Find the next occurance of the required octet storing the result in lastPosition
                //If the result occured after the start
                if ((lastPosition = Array.IndexOf<byte>(buffer, octets[checkedBytes++], position, depth)) >= start)
                {
                    //Check for completion
                    if (++matchedBytes == octetCount) break;

                    //Partial match only
                    start = lastPosition;
                }
                else
                {
                    //No bytes were matched
                    matchedBytes = 0;

                    //Check for another possible match
                    if (checkedBytes < octetsLength) continue;

                    //The match failed at the current offset
                    checkedBytes = 0;

                    //Move the position
                    start += depth;

                    //Decrease the amount which remains
                    count -= depth;
                }
            }

            //start now reflects the position after a parse occurs

            //Should alsp export matchedBytes with count or out

            //Return the last position of the partial match
            return lastPosition;
        }

        public static int Find(byte[] array, byte[] needle, int startIndex, int sourceLength)
        {
            int needleLen = needle.Length;

            int index;

            while (sourceLength >= needleLen)
            {
                // find needle's starting element
                index = Array.IndexOf(array, needle[0], startIndex, sourceLength - needleLen + 1);

                // if we did not find even the first element of the needls, then the search is failed
                if (index == -1)
                    return -1;

                int i, p;
                // check for needle
                for (i = 0, p = index; i < needleLen; i++, p++)
                {
                    if (array[p] != needle[i])
                    {
                        break;
                    }
                }

                if (i == needleLen)
                {
                    // needle was found
                    return index;
                }

                // continue to search for needle
                sourceLength -= (index - startIndex + 1);
                startIndex = index + 1;
            }
            return -1;
        }
    }
}
