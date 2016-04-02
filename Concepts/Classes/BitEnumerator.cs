namespace Media.Concepts.Classes
{
    public sealed class BitEnumerator
    {
        #region BitEnumerator

        /// <summary>
        /// Iterates the bits in data according to the host <see cref="BitOrder"/>
        /// </summary>
        /// <param name="data"></param>
        /// <param name="byteOffset"></param>
        /// <param name="bitOffset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static System.Collections.Generic.IEnumerable<bool> GetEnumerator(byte[] data, int byteOffset, int bitOffset, int count)
        {
            if (count <= 0) yield break;

            unchecked
            {
                //While there is a bit needed decrement for the bit consumed
                while (count-- > Common.Binary.Zero)
                {
                    //Check for the end of bits
                    if (bitOffset >= Common.Binary.BitsPerByte)
                    {
                        //Reset the bit offset
                        bitOffset = Common.Binary.Zero;

                        //Advance the index of the byte
                        ++byteOffset;
                    }

                    //Yeild the result of reading the bit at the bitOffset, increasing the bitOffset
                    yield return Common.Binary.GetBit(ref data[byteOffset], bitOffset++);
                }
            }
        }

        /// <summary>
        /// Interates the bits in data in reverse of the host <see cref="BitOrder"/>
        /// </summary>
        /// <param name="data"></param>
        /// <param name="byteOffset"></param>
        /// <param name="bitOffset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static System.Collections.Generic.IEnumerable<bool> GetReverseEnumerator(byte[] data, int byteOffset, int bitOffset, int count)
        {
            if (count <= 0) yield break;

            unchecked
            {
                //While there is a bit needed decrement for the bit consumed
                while (count-- > Common.Binary.Zero)
                {
                    //Check for the end of bits
                    if (bitOffset >= Common.Binary.BitsPerByte)
                    {
                        //reset the bit offset
                        bitOffset = Common.Binary.Zero;

                        //Advance the index of the byte being read
                        ++byteOffset;
                    }

                    //Yeild the result of reading the reverse bit at the bitOffset, increasing the bitOffset
                    yield return Common.Binary.GetBitReverse(ref data[byteOffset], bitOffset++);
                }
            }
        }

        #endregion
    }
}
