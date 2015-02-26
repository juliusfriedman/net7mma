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

using System;
using System.Linq;

namespace Media.Common.Extensions.Encoding
{
    [CLSCompliant(true)]
    public static class EncodingExtensions
    {
        public static char[] EmptyChar = new char[0];

        #region Number Extraction

        //Todo, See Media.Common.ASCII for an idea of the API required.

        //Candidates for method names:

        //ReadEncodedNumberFrom

        //ReadEncodedNumberWithSignFrom

        #endregion

        #region Read Delimited Data

        /// <summary>
        /// Reads the data in the stream using the given encoding until the first occurance of any of the given delimits are found, count is reached or the end of stream occurs.
        /// </summary>
        /// <param name="encoding"></param>
        /// <param name="stream"></param>
        /// <param name="delimits"></param>
        /// <param name="count"></param>
        /// <param name="result"></param>
        /// <param name="includeDelimits"></param>
        /// <returns>True if a given delimit was found, otherwise false.</returns>        
        [CLSCompliant(false)]
        public static bool ReadDelimitedDataFrom(this System.Text.Encoding encoding, System.IO.Stream stream, char[] delimits, ulong count, out string result, out ulong read, bool includeDelimits = true)
        {
            read = 0;

            if (delimits == null) delimits = EmptyChar;

            if (stream == null || false == stream.CanRead || count == 0)
            {
                result = null;

                return false;
            }

            //Use default..
            if (encoding == null) encoding = System.Text.Encoding.Default;

            System.Text.StringBuilder builder = null;

            bool sawDelimit = false;

            try
            {
                //Make the builder
                builder = new System.Text.StringBuilder();

                //Use the BinaryReader on the stream to ensure ReadChar reads in the correct size
                //This prevents manual conversion from byte to char and uses the encoding's code page.
                using (var br = new System.IO.BinaryReader(stream, encoding, true))
                {
                    //Read a while permitted, check for EOS
                    while (count-- > 0 && stream.CanRead)
                    {
                        //Increment read
                        ++read;

                        //Get the Byte
                        char cached = br.ReadChar();

                        //If the Byte was a delemit 
                        if (Array.IndexOf<char>(delimits, cached) >= 0)
                        {
                            //Indicate the delimit was seen
                            sawDelimit = true;

                            //if the delemit should be included, include it.
                            if (includeDelimits) builder.Append(cached);

                            //Do not read further
                            goto Done;
                        }

                        //Create a string and append
                        builder.Append(cached);
                    }
                }
            }
            catch
            {
                goto Done;
            }

        Done:

            if (builder == null)
            {
                result = null;

                return sawDelimit;
            }

            result = builder.ToString();

            return sawDelimit;
        }

        public static bool ReadDelimitedDataFrom(this System.Text.Encoding encoding, System.IO.Stream stream, char[] delimits, int count, out string result, out int read, bool includeDelimits = true)
        {
            ulong cast;

            bool found = ReadDelimitedDataFrom(encoding, stream, delimits, (ulong)count, out result, out cast, includeDelimits);

            read = (int)cast;

            return found;
        }

        public static bool ReadDelimitedDataFrom(this System.Text.Encoding encoding, System.IO.Stream stream, char[] delimits, long count, out string result, out long read, bool includeDelimits = true)
        {
            ulong cast;

            bool found = ReadDelimitedDataFrom(encoding, stream, delimits, (ulong)count, out result, out cast, includeDelimits);

            read = (long)cast;

            return found;
        }

        #endregion
    }

    internal class EncodingExtensionsTests
    {

        /// <summary>
        /// Performs a test that `ReadDelimitedDataFrom` can read the same data back as was written in various different encodings.
        /// </summary>
        public void TestReadDelimitedDataFrom()
        {
            //Unicode string
            string testString = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz01234567890!@#$%^&*()_+-=";

            int testStringLength = testString.Length;

            //With every encoding in the system
            foreach (var encodingInfo in System.Text.Encoding.GetEncodings())
            {
                //Create a new memory stream
                using (var ms = new System.IO.MemoryStream())
                {
                    //Get the encoding
                    var encoding = encodingInfo.GetEncoding();

                    System.Console.WriteLine("Testing: " + encoding.EncodingName);

                    //Create a writer on that same stream using a small buffer
                    using (var streamWriter = new System.IO.StreamWriter(ms, encoding, 1, true))
                    {
                        //Get the binary representation of the string in the encoding being tested
                        var encodedData = encoding.GetBytes(testString);

                        //Cache the length of the data
                        int encodedDataLength = encodedData.Length;

                        //Write the value in the encoding
                        streamWriter.Write(testString);

                        //Ensure in the stream
                        streamWriter.Flush();

                        //Go back to the beginning
                        ms.Position = 0;

                        string actual;

                        int read;                        

                        //Ensure dat was read correctly using the binary length and not the string length
                        //(should try to over read)
                        if (false != EncodingExtensions.ReadDelimitedDataFrom(encoding, ms, null, encodedDataLength, out actual, out read))
                        {
                            throw new System.Exception("ReadDelimitedDataFrom failed.");
                        }

                        //Ensure the position 
                        if (ms.Position > encodedDataLength + encoding.GetPreamble().Length)
                        {
                            throw new System.Exception("Stream.Position is not correct.");
                        }

                        //Ensure the strings are equal (The extra byte is spacing)
                        int difference = string.Compare(encoding.GetString(encoding.GetBytes(testString)), actual);
                        if (difference != 0 && difference > 1)
                        {
                            throw new System.Exception("string data is incorrect.");
                        }

                        Console.WriteLine(actual);
                    }
                }

            }

        }

    }
}
