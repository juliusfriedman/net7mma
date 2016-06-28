using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Common
{
    public static class StreamExtensions
    {

        public static long limit(this MemoryStream ms)
        {
            return ms.Length;
        }

        public static void limit(this MemoryStream ms, long l)
        {
            ms.SetLength(l);
        }

        public static void write(this MemoryStream ms, MemoryStream o)
        {
            ms.Write(o.ToArray(), 0, (int)o.Length);
        }

        public static void clear(this MemoryStream ms)
        {
            ms = new MemoryStream((int)ms.Length);
        }

        public static int remaining(this MemoryStream ms)
        {
            return (int)(ms.Position - ms.Length);
        }

        public static short getShort(this MemoryStream ms)
        {
            return (short)(ms.ReadByte() * 256 + ms.ReadByte());
        }

        public static int getInt(this MemoryStream ms)
        {
            return getShort(ms) << 16 | getShort(ms);
        }

        public static long getLong(this MemoryStream ms)
        {
            return getInt(ms) << 32 | getInt(ms);
        }

        public static int position(this MemoryStream ms)
        {
            return (int)(ms.Position);
        }

        public static void position(this MemoryStream ms, long position)
        {
            ms.Position = position;
        }

        public static bool hasRemaining(this MemoryStream ms)
        {
            return ms.Length - ms.Position > 0;
        }

        public static byte get(this MemoryStream ms)
        {
            return (byte)ms.ReadByte();
        }

        public static byte get(this MemoryStream ms, int pos)
        {
            position(ms, pos);
            return (byte)ms.ReadByte();
        }

        public static void put(this MemoryStream ms, int pos, byte b)
        {
            position(ms, pos);
            ms.WriteByte(b);
        }

        public static void put(this MemoryStream ms, string str)
        {
            foreach (char c in str)
                ms.put((byte)c);
        }

        public static void put(this MemoryStream ms, byte b)
        {
            ms.WriteByte(b);
        }

        public static void putInt(this MemoryStream ms, int n)
        {
            ms.Write(BitConverter.GetBytes(n).Reverse().ToArray(), 0, 4);
        }

        public static void putShort(this MemoryStream ms, short n)
        {
            ms.Write(BitConverter.GetBytes(n).Reverse().ToArray(), 0, 2);
        }


        public static void put(this MemoryStream ms, MemoryStream bb)
        {
            bb.CopyTo(ms);
        }

        public static MemoryStream duplicate(this MemoryStream ms)
        {
            return new MemoryStream(ms.ToArray());
        }

        public static void flip(this MemoryStream ms)
        {
            ms.SetLength((int)ms.Position);
            return;
        }

        internal static MemoryStream read(MemoryStream dup, int len)
        {
            byte[] temp = new byte[len];
            dup.Read(temp, 0, len);
            return new MemoryStream(temp);
        }

        internal static int skip(MemoryStream input, int p)
        {
            int toSkip = Math.Min(input.remaining(), p);
            input.position(input.position() + toSkip);
            return toSkip;
        }

        public static string asciiString(string str)
        {
            return Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(str));
        }

        public static byte[] toArray(MemoryStream buffer)
        {
            byte[] result = new byte[buffer.remaining()];
            buffer.duplicate().Read(result, 0, result.Length);
            return result;
        }

        internal static string readPascalString(MemoryStream input, int p)
        {
            MemoryStream sub = read(input, p + 1);
            return Encoding.Default.GetString(toArray(read(sub, Math.Min(sub.get() & 0xff, p))));
        }

        internal static string readString(MemoryStream input, int p)
        {
            return Encoding.Default.GetString(toArray(read(input, p)));
        }
        public static void writePascalString(MemoryStream buffer, String str, int maxLen)
        {
            buffer.put((byte)str.Length);
            buffer.put(asciiString(str));
            skip(buffer, maxLen - str.Length);
        }

        public static void writePascalString(MemoryStream buffer, String name)
        {
            buffer.put((byte)name.Length);
            buffer.put(asciiString(name));
        }

        public static String readPascalString(MemoryStream buffer)
        {
            return readString(buffer, buffer.get() & 0xff);
        }

        public static String readNullTermString(MemoryStream buffer)
        {
            return readNullTermString(buffer, Encoding.Default);
        }

        public static String readNullTermString(MemoryStream buffer, Encoding encoding)
        {
            MemoryStream fork = buffer.duplicate();
            while (buffer.hasRemaining() && buffer.get() != 0)
                ;
            if (buffer.hasRemaining())
                fork.limit(buffer.position() - 1);
            return encoding.GetString(toArray(fork));
        }

        public static byte[] toArray(MemoryStream buffer, int count)
        {
            byte[] result = new byte[Math.Min(buffer.remaining(), count)];
            buffer.duplicate().Read(result, 0, count);
            return result;
        }


        internal static MemoryStream slice(MemoryStream buf)
        {
            throw new NotImplementedException();
        }

        internal static MemoryStream fetchFrom(Stream channel, int p)
        {
            throw new NotImplementedException();
        }

        internal static MemoryStream wrap(byte[] tsPkt, int p1, int p2)
        {
            throw new NotImplementedException();
        }
    }
}
