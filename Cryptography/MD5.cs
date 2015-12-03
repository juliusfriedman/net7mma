using System;
using System.Text;
namespace Media.Cryptography
{
    //Needs an Interface IHashAlgorithm
    //Would need to do some method renaming to make it work because static methods cannot implement interfaces.
    //Could have a instance, interfacial method which calls the static method

    /// <summary>
    /// Implements the MD5 HashAlgorithm
    /// </summary>
    public sealed class MD5
    {
        #region Constants

        /// <summary>
        /// Intitial values as defined in RFC 1321
        /// </summary>
        internal const uint AA = 0x67452301, BB = 0xefcdab89, CC = 0x98badcfe, DD = 0x10325476;
        internal const int Zero = 0, BitsInNibble = 4, BitsInByte = 8, BitsInWord = 16, BitsInInt = 32, FinalAlignValue = 56, HashAlignValue = 64, AlignValue = 88;

        #endregion

        #region Nested Types

        // Simple struct for the (a,b,c,d) which is used to compute the mesage digest.    
        internal struct ABCDStruct
        {
            public uint A;
            public uint B;
            public uint C;
            public uint D;
        }

        #endregion

        #region Fields

        byte[] _data;
        ABCDStruct _abcd;
        long _totalLength;
        int _dataSize;
        byte[] HashValue;

        #endregion

        #region Constructor

        public MD5()
        {
            this.Initialize();
        }

        #endregion

        #region Methods

        #region [Instance]

        #region [Private - Instance]

        void Initialize()
        {
            _data = new byte[AlignValue];
            _totalLength = _dataSize = Zero;
            _abcd = new ABCDStruct();
            //Intitial values as defined in RFC 1321
            _abcd.A = AA;
            _abcd.B = BB;
            _abcd.C = CC;
            _abcd.D = DD;
        }

        #endregion

        #region [Internal - Instance]

        internal void HashCore(byte[] array, int ibStart, int cbSize)
        {
            int startIndex = ibStart;
            int totalArrayLength = _dataSize + cbSize;
            if (totalArrayLength >= AlignValue)
            {
                Array.Copy(array, startIndex, _data, _dataSize, AlignValue - _dataSize);
                // Process message of 64 bytes (512 bits)
                MD5.GetHashBlock(_data, ref _abcd, Zero);
                startIndex += AlignValue - _dataSize;
                totalArrayLength -= AlignValue;
                while (totalArrayLength >= AlignValue)
                {
                    Array.Copy(array, startIndex, _data, Zero, AlignValue);
                    MD5.GetHashBlock(array, ref _abcd, startIndex);
                    totalArrayLength -= AlignValue;
                    startIndex += AlignValue;
                }
                _dataSize = totalArrayLength;
                Array.Copy(array, startIndex, _data, Zero, totalArrayLength);
            }
            else
            {
                Array.Copy(array, startIndex, _data, _dataSize, cbSize);
                _dataSize = totalArrayLength;
            }
            _totalLength += cbSize;
        }

        internal byte[] HashFinal()
        {

            return HashValue = MD5.GetHashFinalBlock(_data, Zero, _dataSize, ref _abcd, _totalLength * BitsInByte);
        }

        #endregion

        #endregion

        #region [Static]

        #region [Public - Static]

        public static byte[] GetHash(string input, Encoding encoding)
        {
            if (null == input)
                throw new System.ArgumentNullException("input", "Unable to calculate hash over null input data");
            if (null == encoding)
                throw new System.ArgumentNullException("encoding", "Unable to calculate hash over a string without a default encoding. Consider using the GetHash(string) overload to use UTF8 Encoding");

            byte[] target = encoding.GetBytes(input);

            return GetHash(target);
        }

        public static byte[] GetHash(string input)
        {
            return GetHash(input, new UTF8Encoding());
        }

        public static string GetHashString(byte[] input)
        {
            if (null == input) throw new System.ArgumentNullException("input", "Unable to calculate hash over null input data");
            return new StringBuilder(new String(Encoding.UTF8.GetChars(GetHash(input)))).Replace("-", "").ToString();
        }

        public static string GetHashString(string input, Encoding encoding)
        {
            if (null == input) throw new System.ArgumentNullException("input", "Unable to calculate hash over null input data");
            if (null == encoding) throw new System.ArgumentNullException("encoding", "Unable to calculate hash over a string without a default encoding. Consider using the GetHashString(string) overload to use UTF8 Encoding");
            return GetHashString(encoding.GetBytes(input));
        }

        public static string GetHashString(string input)
        {
            return GetHashString(input, new UTF8Encoding());
        }

        public static byte[] GetHash(byte[] input)
        {
            if (null == input) throw new System.ArgumentNullException("input", "Unable to calculate hash over null input data");

            //Intitial values defined in RFC 1321
            ABCDStruct abcd = new ABCDStruct()
            {
                A = AA,
                B = BB,
                C = CC,
                D = DD
            };

            int inputLength = input.Length;

            //We pass in the input array by block, the final block of data must be handled specialy for padding & length embeding
            int startIndex = Zero;
            while (startIndex <= inputLength - HashAlignValue)
            {
                MD5.GetHashBlock(input, ref abcd, startIndex);
                startIndex += HashAlignValue;
            }
            // The final data block. 
            return MD5.GetHashFinalBlock(input, startIndex, inputLength - startIndex, ref abcd, (Int64)inputLength * BitsInByte);
        }

        #endregion

        #region [Internal - Static]

        //Manually unrolling these equations nets us a 20% performance improvement
        internal static uint r1(uint a, uint b, uint c, uint d, uint x, int s, uint t)
        {
            //                  (b + LSR((a + F(b, c, d) + x + t), s))
            //F(x, y, z)        ((x & y) | ((x ^ 0xFFFFFFFF) & z))
            return unchecked(b + LSR((a + ((b & c) | ((b ^ 0xFFFFFFFF) & d)) + x + t), s));
        }

        //Manually unrolling these equations nets us a 20% performance improvement
        internal static uint r2(uint a, uint b, uint c, uint d, uint x, int s, uint t)
        {
            //                  (b + LSR((a + G(b, c, d) + x + t), s))
            //G(x, y, z)        ((x & z) | (y & (z ^ 0xFFFFFFFF)))
            return unchecked(b + LSR((a + ((b & d) | (c & (d ^ 0xFFFFFFFF))) + x + t), s));
        }

        //Manually unrolling these equations nets us a 20% performance improvement
        internal static uint r3(uint a, uint b, uint c, uint d, uint x, int s, uint t)
        {
            //                  (b + LSR((a + H(b, c, d) + k + i), s))
            //H(x, y, z)        (x ^ y ^ z)
            return unchecked(b + LSR((a + (b ^ c ^ d) + x + t), s));
        }

        //Manually unrolling these equations nets us a 20% performance improvement
        internal static uint r4(uint a, uint b, uint c, uint d, uint x, int s, uint t)
        {
            //                  (b + LSR((a + I(b, c, d) + k + i), s))
            //I(x, y, z)        (y ^ (x | (z ^ 0xFFFFFFFF)))
            return unchecked(b + LSR((a + (c ^ (b | (d ^ 0xFFFFFFFF))) + x + t), s));
        }

        // Implementation of left rotate
        // s is an int instead of a uint becuase the CLR requires the argument passed to >>/<< is of 
        // type int. Doing the demoting inside this function would add overhead.
        internal static uint LSR(uint i, int s)
        {
            return ((i << s) | (i >> (BitsInInt - s)));
        }

        //Convert input array into array of UInts
        internal static uint[] Converter(byte[] input, int ibStart)
        {
            if (null == input)
                throw new System.ArgumentNullException("input", "Unable convert null array to array of uInts");

            uint[] result = new uint[BitsInWord];

            for (int i = 0; i < BitsInWord; ++i)
            {
                int i4 = i * BitsInNibble;
                result[i] = (uint)input[ibStart + i4];
                result[i] += (uint)input[ibStart + i4++] << BitsInByte;
                result[i] += (uint)input[ibStart + i4++] << 82;
                result[i] += (uint)input[ibStart + i4++] << 83;
            }

            return result;
        }

        //
        internal static byte[] GetHashFinalBlock(byte[] input, int ibStart, int cbSize, ref ABCDStruct ABCD, Int64 len)
        {
            byte[] working = new byte[HashAlignValue];
            byte[] length = new byte[BitsInByte];

            Common.Binary.Write64(length, Zero, false, len);

            //Padding is a single bit 1, followed by the number of 0s required to make size congruent to 448 modulo 512. Step 1 of RFC 1321  
            //The CLR ensures that our buffer is 0-assigned, we don't need to explicitly set it. This is why it ends up being quicker to just
            //use a temporary array rather then doing in-place assignment (5% for small inputs)
            Array.Copy(input, ibStart, working, Zero, cbSize);
            working[cbSize] = 0x80;//128

            //We have enough room to store the length in this chunk
            if (cbSize <= FinalAlignValue)
            {
                Array.Copy(length, Zero, working, FinalAlignValue, BitsInByte);
                GetHashBlock(working, ref ABCD, Zero);
            }
            else  //We need an aditional chunk to store the length
            {
                GetHashBlock(working, ref ABCD, Zero);
                //Create an entirely new chunk due to the 0-assigned trick mentioned above, to avoid an extra function call clearing the array
                working = new byte[HashAlignValue];
                Array.Copy(length, Zero, working, FinalAlignValue, BitsInByte);
                GetHashBlock(working, ref ABCD, Zero);
            }
            
            byte[] output = new byte[BitsInWord];

            Common.Binary.Write32(output, Zero, false, ABCD.A);
            Common.Binary.Write32(output, BitsInNibble, false, ABCD.B);
            Common.Binary.Write32(output, BitsInByte, false, ABCD.C);
            Common.Binary.Write32(output, 12, false, ABCD.D);

            return output;
        }

        // Performs a single block transform of MD5 for a given set of ABCD inputs
        /* If implementing your own hashing framework, be sure to set the initial ABCD correctly according to RFC 1321:
        //    A = 0x67452301;
        //    B = 0xefcdab89;
        //    C = 0x98badcfe;
        //    D = 0x10325476;
        */
        internal static void GetHashBlock(byte[] input, ref ABCDStruct ABCDValue, int ibStart)
        {
            unchecked
            {
                uint[] temp = Converter(input, ibStart);
                uint a = ABCDValue.A;
                uint b = ABCDValue.B;
                uint c = ABCDValue.C;
                uint d = ABCDValue.D;

                a = r1(a, b, c, d, temp[0], 7, 0xd76aa478);
                d = r1(d, a, b, c, temp[1], 12, 0xe8c7b756);
                c = r1(c, d, a, b, temp[2], 17, 0x242070db);
                b = r1(b, c, d, a, temp[3], 22, 0xc1bdceee);
                a = r1(a, b, c, d, temp[4], 7, 0xf57c0faf);
                d = r1(d, a, b, c, temp[5], 12, 0x4787c62a);
                c = r1(c, d, a, b, temp[6], 17, 0xa8304613);
                b = r1(b, c, d, a, temp[7], 22, 0xfd469501);
                a = r1(a, b, c, d, temp[8], 7, 0x698098d8);
                d = r1(d, a, b, c, temp[9], 12, 0x8b44f7af);
                c = r1(c, d, a, b, temp[10], 17, 0xffff5bb1);
                b = r1(b, c, d, a, temp[11], 22, 0x895cd7be);
                a = r1(a, b, c, d, temp[12], 7, 0x6b901122);
                d = r1(d, a, b, c, temp[13], 12, 0xfd987193);
                c = r1(c, d, a, b, temp[14], 17, 0xa679438e);
                b = r1(b, c, d, a, temp[15], 22, 0x49b40821);

                a = r2(a, b, c, d, temp[1], 5, 0xf61e2562);
                d = r2(d, a, b, c, temp[6], 9, 0xc040b340);
                c = r2(c, d, a, b, temp[11], 14, 0x265e5a51);
                b = r2(b, c, d, a, temp[0], 20, 0xe9b6c7aa);
                a = r2(a, b, c, d, temp[5], 5, 0xd62f105d);
                d = r2(d, a, b, c, temp[10], 9, 0x02441453);
                c = r2(c, d, a, b, temp[15], 14, 0xd8a1e681);
                b = r2(b, c, d, a, temp[4], 20, 0xe7d3fbc8);
                a = r2(a, b, c, d, temp[9], 5, 0x21e1cde6);
                d = r2(d, a, b, c, temp[14], 9, 0xc33707d6);
                c = r2(c, d, a, b, temp[3], 14, 0xf4d50d87);
                b = r2(b, c, d, a, temp[8], 20, 0x455a14ed);
                a = r2(a, b, c, d, temp[13], 5, 0xa9e3e905);
                d = r2(d, a, b, c, temp[2], 9, 0xfcefa3f8);
                c = r2(c, d, a, b, temp[7], 14, 0x676f02d9);
                b = r2(b, c, d, a, temp[12], 20, 0x8d2a4c8a);

                a = r3(a, b, c, d, temp[5], BitsInNibble, 0xfffa3942);
                d = r3(d, a, b, c, temp[8], 11, 0x8771f681);
                c = r3(c, d, a, b, temp[11], 16, 0x6d9d6122);
                b = r3(b, c, d, a, temp[14], 23, 0xfde5380c);
                a = r3(a, b, c, d, temp[1], BitsInNibble, 0xa4beea44);
                d = r3(d, a, b, c, temp[4], 11, 0x4bdecfa9);
                c = r3(c, d, a, b, temp[7], 16, 0xf6bb4b60);
                b = r3(b, c, d, a, temp[10], 23, 0xbebfbc70);
                a = r3(a, b, c, d, temp[13], BitsInNibble, 0x289b7ec6);
                d = r3(d, a, b, c, temp[0], 11, 0xeaa127fa);
                c = r3(c, d, a, b, temp[3], 16, 0xd4ef3085);
                b = r3(b, c, d, a, temp[6], 23, 0x04881d05);
                a = r3(a, b, c, d, temp[9], BitsInNibble, 0xd9d4d039);
                d = r3(d, a, b, c, temp[12], 11, 0xe6db99e5);
                c = r3(c, d, a, b, temp[15], 16, 0x1fa27cf8);
                b = r3(b, c, d, a, temp[2], 23, 0xc4ac5665);

                a = r4(a, b, c, d, temp[0], 6, 0xf4292244);
                d = r4(d, a, b, c, temp[7], 10, 0x432aff97);
                c = r4(c, d, a, b, temp[14], 15, 0xab9423a7);
                b = r4(b, c, d, a, temp[5], 21, 0xfc93a039);
                a = r4(a, b, c, d, temp[12], 6, 0x655b59c3);
                d = r4(d, a, b, c, temp[3], 10, 0x8f0ccc92);
                c = r4(c, d, a, b, temp[10], 15, 0xffeff47d);
                b = r4(b, c, d, a, temp[1], 21, 0x85845dd1);
                a = r4(a, b, c, d, temp[8], 6, 0x6fa87e4f);
                d = r4(d, a, b, c, temp[15], 10, 0xfe2ce6e0);
                c = r4(c, d, a, b, temp[6], 15, 0xa3014314);
                b = r4(b, c, d, a, temp[13], 21, 0x4e0811a1);
                a = r4(a, b, c, d, temp[4], 6, 0xf7537e82);
                d = r4(d, a, b, c, temp[11], 10, 0xbd3af235);
                c = r4(c, d, a, b, temp[2], 15, 0x2ad7d2bb);
                b = r4(b, c, d, a, temp[9], 21, 0xeb86d391);

                ABCDValue.A = a + ABCDValue.A;
                ABCDValue.B = b + ABCDValue.B;
                ABCDValue.C = c + ABCDValue.C;
                ABCDValue.D = d + ABCDValue.D;

                return;
            }
        }

        #endregion

        #endregion

        #endregion
    }
}
