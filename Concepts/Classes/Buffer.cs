//https://github.com/TASVideos/BizHawk/blob/master/BizHawk.Common
namespace Media.Concepts.Classes
{
    public static unsafe class Util
    {
        public static void CopyStream(System.IO.Stream src, System.IO.Stream dest, long len)
        {
            const int size = 0x2000;
            byte[] buffer = new byte[size];
            while (len > 0)
            {
                long todo = len;
                if (len > size) todo = size;
                int n = src.Read(buffer, 0, (int)todo);
                dest.Write(buffer, 0, n);
                len -= n;
            }
        }

        public static int SaveRamBytesUsed(byte[] saveRam)
        {
            for (var i = saveRam.Length - 1; i >= 0; i--)
            {
                if (saveRam[i] != 0)
                {
                    return i + 1;
                }
            }

            return 0;
        }

        // Could be extension method
        public static void WriteByteBuffer(System.IO.BinaryWriter bw, byte[] data)
        {
            if (data == null)
            {
                bw.Write(0);
            }
            else
            {
                bw.Write(data.Length);
                bw.Write(data);
            }
        }

        public static bool[] ByteBufferToBoolBuffer(byte[] buf)
        {
            var ret = new bool[buf.Length];
            for (int i = 0; i < buf.Length; i++)
            {
                ret[i] = buf[i] != 0;
            }
            return ret;
        }

        public static byte[] BoolBufferToByteBuffer(bool[] buf)
        {
            var ret = new byte[buf.Length];
            for (int i = 0; i < buf.Length; i++)
            {
                ret[i] = (byte)(buf[i] ? 1 : 0);
            }
            return ret;
        }

        public static short[] ByteBufferToShortBuffer(byte[] buf)
        {
            int num = buf.Length / 2;
            var ret = new short[num];
            for (int i = 0; i < num; i++)
            {
                ret[i] = (short)(buf[i * 2] | (buf[i * 2 + 1] << 8));
            }

            return ret;
        }

        public static byte[] ShortBufferToByteBuffer(short[] buf)
        {
            int num = buf.Length;
            var ret = new byte[num * 2];
            for (int i = 0; i < num; i++)
            {
                ret[i * 2 + 0] = (byte)(buf[i] & 0xFF);
                ret[i * 2 + 1] = (byte)((buf[i] >> 8) & 0xFF);
            }

            return ret;
        }

        public static ushort[] ByteBufferToUshortBuffer(byte[] buf)
        {
            int num = buf.Length / 2;
            var ret = new ushort[num];
            for (int i = 0; i < num; i++)
            {
                ret[i] = (ushort)(buf[i * 2] | (buf[i * 2 + 1] << 8));
            }

            return ret;
        }

        public static byte[] UshortBufferToByteBuffer(ushort[] buf)
        {
            int num = buf.Length;
            var ret = new byte[num * 2];
            for (int i = 0; i < num; i++)
            {
                ret[i * 2 + 0] = (byte)(buf[i] & 0xFF);
                ret[i * 2 + 1] = (byte)((buf[i] >> 8) & 0xFF);
            }

            return ret;
        }

        public static uint[] ByteBufferToUintBuffer(byte[] buf)
        {
            int num = buf.Length / 4;
            var ret = new uint[num];
            for (int i = 0; i < num; i++)
            {
                ret[i] = (uint)(buf[i * 4] | (buf[i * 4 + 1] << 8) | (buf[i * 4 + 2] << 16) | (buf[i * 4 + 3] << 24));
            }

            return ret;
        }

        public static byte[] UintBufferToByteBuffer(uint[] buf)
        {
            int num = buf.Length;
            var ret = new byte[num * 4];
            for (int i = 0; i < num; i++)
            {
                ret[i * 4 + 0] = (byte)(buf[i] & 0xFF);
                ret[i * 4 + 1] = (byte)((buf[i] >> 8) & 0xFF);
                ret[i * 4 + 2] = (byte)((buf[i] >> 16) & 0xFF);
                ret[i * 4 + 3] = (byte)((buf[i] >> 24) & 0xFF);
            }

            return ret;
        }

        public static int[] ByteBufferToIntBuffer(byte[] buf)
        {
            int num = buf.Length / 4;
            var ret = new int[num];
            for (int i = 0; i < num; i++)
            {
                ret[i] = buf[(i * 4) + 3];
                ret[i] <<= 8;
                ret[i] |= buf[(i * 4) + 2];
                ret[i] <<= 8;
                ret[i] |= buf[(i * 4) + 1];
                ret[i] <<= 8;
                ret[i] |= buf[(i * 4)];
            }

            return ret;
        }

        public static byte[] IntBufferToByteBuffer(int[] buf)
        {
            int num = buf.Length;
            var ret = new byte[num * 4];
            for (int i = 0; i < num; i++)
            {
                ret[i * 4 + 0] = (byte)(buf[i] & 0xFF);
                ret[i * 4 + 1] = (byte)((buf[i] >> 8) & 0xFF);
                ret[i * 4 + 2] = (byte)((buf[i] >> 16) & 0xFF);
                ret[i * 4 + 3] = (byte)((buf[i] >> 24) & 0xFF);
            }

            return ret;
        }

        public static byte[] ReadByteBuffer(System.IO.BinaryReader br, bool returnNull)
        {
            int len = br.ReadInt32();
            if (len == 0 && returnNull)
            {
                return null;
            }

            var ret = new byte[len];
            int ofs = 0;
            while (len > 0)
            {
                int done = br.Read(ret, ofs, len);
                ofs += done;
                len -= done;
            }

            return ret;
        }

        public static int Memcmp(void* a, string b, int len)
        {
            fixed (byte* bp = System.Text.Encoding.ASCII.GetBytes(b))
                return Memcmp(a, bp, len);
        }

        public static int Memcmp(void* a, void* b, int len)
        {
            var ba = (byte*)a;
            var bb = (byte*)b;
            for (int i = 0; i < len; i++)
            {
                byte _a = ba[i];
                byte _b = bb[i];
                int c = _a - _b;
                if (c != 0)
                {
                    return c;
                }
            }

            return 0;
        }

        public static void Memset(void* ptr, int val, int len)
        {
            var bptr = (byte*)ptr;
            for (int i = 0; i < len; i++)
            {
                bptr[i] = (byte)val;
            }
        }

        public static void Memset32(void* ptr, int val, int len)
        {
            System.Diagnostics.Debug.Assert(len % 4 == 0);
            int dwords = len / 4;
            int* dwptr = (int*)ptr;
            for (int i = 0; i < dwords; i++)
            {
                dwptr[i] = val;
            }
        }

        public static string FormatFileSize(long filesize)
        {
            decimal size = filesize;

            string suffix;
            if (size > 1024 * 1024 * 1024)
            {
                size /= 1024 * 1024 * 1024;
                suffix = "GB";
            }
            else if (size > 1024 * 1024)
            {
                size /= 1024 * 1024;
                suffix = "MB";
            }
            else if (size > 1024)
            {
                size /= 1024;
                suffix = "KB";
            }
            else
            {
                suffix = "B";
            }

            const string precision = "2";
            return string.Format("{0:N" + precision + "}{1}", size, suffix);
        }

        // http://stackoverflow.com/questions/3928822/comparing-2-dictionarystring-string-instances
        public static bool DictionaryEqual<TKey, TValue>(
            System.Collections.Generic.IDictionary<TKey, TValue> first, System.Collections.Generic.IDictionary<TKey, TValue> second)
        {
            if (first == second)
            {
                return true;
            }

            if ((first == null) || (second == null))
            {
                return false;
            }

            if (first.Count != second.Count)
            {
                return false;
            }

            var comparer = System.Collections.Generic.EqualityComparer<TValue>.Default;

            foreach (var kvp in first)
            {
                TValue secondValue;
                if (!second.TryGetValue(kvp.Key, out secondValue))
                {
                    return false;
                }

                if (!comparer.Equals(kvp.Value, secondValue))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public static class VLInteger
    {
        public static void WriteUnsigned(uint value, byte[] data, ref int index)
        {
            // This is optimized for good performance on both the x86 and x64 JITs. Don't change anything without benchmarking.
            do
            {
                uint x = value & 0x7FU;
                value >>= 7;
                data[index++] = (byte)((value != 0U ? 0x80U : 0U) | x);
            }
            while (value != 0U);
        }

        public static uint ReadUnsigned(byte[] data, ref int index)
        {
            // This is optimized for good performance on both the x86 and x64 JITs. Don't change anything without benchmarking.
            uint value = 0U;
            int shiftCount = 0;
            bool isLastByte; // Negating the comparison and moving it earlier in the loop helps a lot on x86 for some reason
            do
            {
                uint x = (uint)data[index++];
                isLastByte = (x & 0x80U) == 0U;
                value |= (x & 0x7FU) << shiftCount;
                shiftCount += 7;
            }
            while (!isLastByte);
            return value;
        }
    }

    [System.Serializable]
    public class NotTestedException : System.Exception
    {
    }

    internal class SuperGloballyUniqueID
    {
        private static readonly string StaticPart;
        private static int ctr;

        static SuperGloballyUniqueID()
        {
            StaticPart = "bizhawk-" + System.Diagnostics.Process.GetCurrentProcess().Id + "-" + System.Guid.NewGuid();
        }

        public static string Next()
        {
            int myctr;
            lock (typeof(SuperGloballyUniqueID))
            {
                myctr = ctr++;
            }

            return StaticPart + "-" + myctr;
        }
    }

    public static class ReflectionUtil
    {
        // http://stackoverflow.com/questions/9273629/avoid-giving-namespace-name-in-type-gettype
        /// <summary>
        /// Gets a all Type instances matching the specified class name with just non-namespace qualified class name.
        /// </summary>
        /// <param name="className">Name of the class sought.</param>
        /// <returns>Types that have the class name specified. They may not be in the same namespace.</returns>
        public static System.Type[] GetTypeByName(string className)
        {
            var returnVal = new System.Collections.Generic.List<System.Type>();

            foreach (System.Reflection.Assembly a in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                System.Type[] assemblyTypes = a.GetTypes();
                for (int j = 0; j < assemblyTypes.Length; j++)
                {
                    if (assemblyTypes[j].Name.ToLower() == className.ToLower())
                    {
                        returnVal.Add(assemblyTypes[j]);
                    }
                }
            }

            return returnVal.ToArray();
        }
    }

    /// <summary>
    /// Implements a data simple data buffer with proper life cycle and no bounds checking
    /// </summary>
    public unsafe class CBuffer<T> : System.IDisposable
    {
        public System.Runtime.InteropServices.GCHandle Hnd;
        public T[] Arr;
        public void* Ptr;
        public byte* Byteptr;
        public int Len;
        public int Itemsize;

        public static CBuffer<T> malloc(int amt, int itemsize)
        {
            return new CBuffer<T>(amt, itemsize);
        }

        public void Write08(uint addr, byte val) { this.Byteptr[addr] = val; }
        public void Write16(uint addr, ushort val) { *(ushort*)(this.Byteptr + addr) = val; }
        public void Write32(uint addr, uint val) { *(uint*)(this.Byteptr + addr) = val; }
        public void Write64(uint addr, ulong val) { *(ulong*)(this.Byteptr + addr) = val; }
        public byte Read08(uint addr) { return this.Byteptr[addr]; }
        public ushort Read16(uint addr) { return *(ushort*)(this.Byteptr + addr); }
        public uint Read32(uint addr) { return *(uint*)(this.Byteptr + addr); }
        public ulong Read64(uint addr) { return *(ulong*)(this.Byteptr + addr); }
        public void Write08(int addr, byte val) { this.Byteptr[addr] = val; }
        public void Write16(int addr, ushort val) { *(ushort*)(this.Byteptr + addr) = val; }
        public void Write32(int addr, uint val) { *(uint*)(this.Byteptr + addr) = val; }
        public void Write64(int addr, ulong val) { *(ulong*)(this.Byteptr + addr) = val; }
        public byte Read08(int addr) { return this.Byteptr[addr]; }
        public ushort Read16(int addr) { return *(ushort*)(this.Byteptr + addr); }
        public uint Read32(int addr) { return *(uint*)(this.Byteptr + addr); }
        public ulong Read64(int addr) { return *(ulong*)(this.Byteptr + addr); }

        public CBuffer(T[] arr, int itemsize)
        {
            this.Itemsize = itemsize;
            this.Len = arr.Length;
            this.Arr = arr;
            this.Hnd = System.Runtime.InteropServices.GCHandle.Alloc(arr, System.Runtime.InteropServices.GCHandleType.Pinned);
            this.Ptr = this.Hnd.AddrOfPinnedObject().ToPointer();
            this.Byteptr = (byte*)this.Ptr;
        }
        public CBuffer(int amt, int itemsize)
        {
            this.Itemsize = itemsize;
            this.Len = amt;
            this.Arr = new T[amt];
            this.Hnd = System.Runtime.InteropServices.GCHandle.Alloc(this.Arr, System.Runtime.InteropServices.GCHandleType.Pinned);
            this.Ptr = this.Hnd.AddrOfPinnedObject().ToPointer();
            this.Byteptr = (byte*)this.Ptr;
            Util.Memset(this.Byteptr, 0, this.Len * itemsize);
        }

        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.Arr != null)
                {
                    this.Hnd.Free();
                }
                this.Arr = null;
            }
        }

        ~CBuffer() { Dispose(true); }
    }

    public sealed class ByteBuffer : CBuffer<byte>
    {
        public ByteBuffer(int amt) : base(amt, 1) { }
        public ByteBuffer(byte[] arr) : base(arr, 1) { }
        public byte this[int index]
        {
#if DEBUG
            get { return this.Arr[index]; }
            set { this.Arr[index] = value; }
#else
				set { Write08(index, value); } 
				get { return Read08(index);}
#endif
        }
    }

    public sealed class IntBuffer : CBuffer<int>
    {
        public IntBuffer(int amt) : base(amt, 4) { }
        public IntBuffer(int[] arr) : base(arr, 4) { }
        public int this[int index]
        {
#if DEBUG
            get { return this.Arr[index]; }
            set { this.Arr[index] = value; }
#else
				set { Write32(index<<2, (uint) value); }
				get { return (int)Read32(index<<2);}
#endif
        }
    }

    public sealed class ShortBuffer : CBuffer<short>
    {
        public ShortBuffer(int amt) : base(amt, 2) { }
        public ShortBuffer(short[] arr) : base(arr, 2) { }
        public short this[int index]
        {
#if DEBUG
            get { return this.Arr[index]; }
            set { this.Arr[index] = value; }
#else
			set { Write32(index << 1, (uint)value); }
			get { return (short)Read16(index << 1); }
#endif
        }
    }
}
