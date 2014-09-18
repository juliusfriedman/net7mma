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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Media.Common;

namespace Media.RtpTools
{

    #region Reference And Remarks

    /* http://www.cs.columbia.edu/irt/software/rtptools/#rtpsend
        
             typedef struct {
              struct timeval start;  //start of recording (GMT) (Could be 8 bytes or 16 depending on machine architecture)
              u_int32 source;        //network source (multicast address)
              u_int16 port;          //UDP port
            } RD_hdr_t;
         
            typedef struct {
               struct timeval start;  // start of recording (GMT)
               u_int32 source;        // network source (multicast address)
               u_int16 port;          // UDP port
            } RD_hdr_t;
        */

    ///<summary>
    /// All entries take (At least) 128 bytes of memory.        
    ///
    /// If the data is aligned properly then then <see cref="First64Bytes"/> should contain the information which describes the RD_hdr_T in Binary Format.
    /// 
    /// In Text Format it will describe the same information if aligned as follows:
    ///
    /// The total size should be 32 bytes for an entry which consisted of only that information e.g. 
    /// 0.000000 RTCP len=0 from=0.0.0.0:0(which is 34 bytes, 34 for RTP)
    /// 
    /// All entries [in Text format] would then also have a `()` expression indicting the version, padding etc for example:
    /// (RR ssrc=0x0 p=0 count=0 len=0()) [The () may not present and would not be required in this example of 0]
    ///     Or
    /// (
    /// (RR ssrc=0x0 p=0 count=0 len=0)
    /// (SDES p=0 count=0 len=0())
    /// )
    /// 
    /// Such 0 based entries are 31 bytes when created with no additional comments or white space including the `()` expression characters which may not be present (29 then),
    /// 
    /// 31 + 29 = 63
    /// 
    /// + 1 for "\n" = 64
    /// 
    /// Hex Format adds more data.
    /// 
    ///</summary>

    #endregion

    public class RtpToolEntry : Common.BaseDisposable
    {

        #region Statics

        public static IEnumerable<byte> CreatePacketHeader(DateTime time, System.Net.IPEndPoint source, Common.IPacket packet, int offset)
        {
            if (!BitConverter.IsLittleEndian)
                return BitConverter.GetBytes(time.TimeOfDay.Milliseconds).Concat(BitConverter.GetBytes((uint)0)).
                    Concat(BitConverter.GetBytes((int)source.Address.Address)).
                    Concat(BitConverter.GetBytes((ushort)source.Port)).
                   Concat(BitConverter.GetBytes((ushort)packet.Length + sizeOf_RD_packet_T)).
                    Concat
                    (packet is Rtcp.RtcpPacket ?
                    Utility.Empty.Concat(BitConverter.GetBytes((ushort)0))
                :
                    Utility.Empty.Concat(BitConverter.GetBytes((ushort)(packet.Length))
                ).Concat(BitConverter.GetBytes(offset)));

            return BitConverter.GetBytes(time.TimeOfDay.Milliseconds).Reverse().Concat(BitConverter.GetBytes((uint)0)).
                Concat(BitConverter.GetBytes((int)source.Address.Address).Reverse()).
                Concat(BitConverter.GetBytes((ushort)source.Port).Reverse()).
                Concat(BitConverter.GetBytes((ushort)packet.Length + sizeOf_RD_packet_T).Reverse()).
                Concat
                (packet is Rtcp.RtcpPacket ?
                Utility.Empty.Concat(BitConverter.GetBytes((ushort)0))
            :
                Utility.Empty.Concat(BitConverter.GetBytes((ushort)(packet.Length)).Reverse())
            ).Concat(BitConverter.GetBytes(offset).Reverse());
        }

        //Because 32 bytes reads exactly this:
        //A RD_hdr_t,RD_packet_t and the first 6 bytes of the entry which
        //can identify the Version, PayloadType etc.
        public const int DefaultEntrySize = 24,
            //26 bytes hdr_t and packet_t;
            sizeOf_RD_hdr_t = 16, 
            sizeOf_RD_packet_T = 8;

        internal static RtpToolEntry CreateShortEntry(byte[] memory, long fileOffset = 0)
        {
            /* Only the header can be restored / represented and indicates a VAT or RTP Packet
               RTP or vat data in tabular form: [-]time ts [seq], where a - indicates a set marker bit. The sequence number seq is only used for RTP packets.
               844525727.800600 954849217 30667
               844525727.837188 954849537 30668
               844525727.877249 954849857 30669
               844525727.922518 954850177 30670
           */

            return new RtpToolEntry(FileFormat.Short, memory, fileOffset);

            //Will be performed if ManagedPacket is accessed
            //BuildPacket(created);

            //Only the read data can be restored. which consists barely of a header, this should be done when retrieved by the ManagedPacketProperty
            //m_Boxed = new Rtp.RtpPacket(new Rtp.RtpHeader(2, false, false, false, 127, 1, 1, 9, 11), Enumerable.Empty<byte>(), true)
        }

        #endregion

        #region Fields

        public readonly long FileOffset;

        /// <summary>
        /// The <see cref="FileFormat"/> on the RtpToolEntry.
        /// </summary>
        public readonly FileFormat Format;

        /// <summary>
        /// Controls the offset in which values are returned from the Blob structure.
        /// </summary>
        public int TimevalSize = 8;


        /// <summary>
        /// Allows the buffer to be used in a circular fashion or be large then visibly seen with the data property.
        /// </summary>
        public int MaxSize = RtpToolEntry.DefaultEntrySize;

        /// <summary>
        /// Indicates if the values being read are on a system which needs to reverse them before processing
        /// </summary>
        public bool ReverseValues = BitConverter.IsLittleEndian;

        #endregion

        #region Properties

        /// <summary>
        /// The data of the entry including the RD_hdr_t and PD_packet_t as well as the data which would follow.
        /// </summary>
        public byte[] Blob { get; private set; }

        /// <summary>
        /// Skips the binary data in <see cref="Blob"/> which is not related to the underlying stored data and describes the Length and Time of the entry when
        /// used in conjunction with <see cref="TimevalSize"/> and allows multiple `addressings` of the underlying information
        /// </summary>
        public IEnumerable<byte> Data
        {
            get { return Blob.Skip(DefaultEntrySize).Take(MaxSize); }
        }

        public int Pointer = 16;

        /// <summary>
        /// Can be used in various ways to display information relating to the Time the entry was received.
        /// By convention the Time <see cref="Utility.UnixEpoc1970"/> is integrated as a base time into this value.
        /// </summary>
        public double Timeoffset
        {
            get
            {
                if (Disposed) return 0;
                //Binary
                return BitConverter.ToDouble(Blob, 0);
                //Text must be parsed
            }
            set
            {
                if (Disposed) return;

                IEnumerable<byte> endian = BitConverter.GetBytes(value);

                if (ReverseValues) endian = endian.Reverse();

                endian.ToArray().CopyTo(Blob, 0);
            }
        }

        /// <summary>
        /// <see cref="Timeoffset"/>
        /// </summary>
        public int StartSeconds
        {
            get
            {
                if (Disposed) return 0;
                return (int)Common.Binary.ReadU32(Blob, 0, ReverseValues);
            }
            set
            {
                if (Disposed) return;

                Common.Binary.WriteNetwork32(Blob, 0, ReverseValues, (uint)value);
            }
        }

        /// <summary>
        /// <see cref="Timeoffset"/>
        /// </summary>
        public long LongSeconds
        {
            get
            {
                if (Disposed) return 0;
                return (long)Common.Binary.ReadU64(Blob, 0, ReverseValues);
            }
            set
            {
                if (Disposed) return;                

                Common.Binary.WriteNetwork32(Blob, 0, ReverseValues, (uint)value);
            }
        }

        /// <summary>
        /// <see cref="Timeoffset"/>
        /// </summary>
        public int Microseconds
        {
            get
            {
                if (Disposed) return 0;
                return (int)Common.Binary.ReadU32(Blob, 4, ReverseValues);
            }
            set
            {
                if (Disposed) return;

                Common.Binary.WriteNetwork32(Blob, 4, ReverseValues, (uint)value);
            }
        }

        /// <summary>
        /// <see cref="Timeoffset"/>
        /// </summary>
        public long LongMicroseconds
        {
            get
            {
                if (Disposed) return 0;
                return (long)Common.Binary.ReadU64(Blob, TimevalSize, ReverseValues);
            }
            set
            {
                if (Disposed) return;

                Common.Binary.WriteNetwork32(Blob, TimevalSize, ReverseValues, (uint)value);
            }
        }

        public int Source
        {
            get
            {
                if (Disposed) return 0;

                return (int)Common.Binary.ReadU32(Blob, TimevalSize, ReverseValues);
            }
            set
            {
                if (Disposed) return;

                Common.Binary.WriteNetwork16(Blob, TimevalSize, ReverseValues, (ushort)value);
            }
        }

        public int Port
        {
            get
            {
                if (Disposed) return 0;

                return Common.Binary.ReadU16(Blob, TimevalSize + 2, false);
            }
            set
            {
                if (Disposed) return;

                Common.Binary.WriteNetwork16(Blob, TimevalSize + 2, ReverseValues, (ushort)value);
            }
        }

        /// <summary>
        /// The value of the property `length` as indicated from the RD_packet_t
        /// </summary>
        public short Length
        {
            get
            {
                if (Disposed) return 0;
                //if (Is64BitEntry) return Info->len_64;
                //return Info->len_32;

                //TextEntry or others might have Length = 0 for some reason...

                return (short)Common.Binary.ReadU16(Blob, Pointer, ReverseValues);
            }
            set
            {
                if (Disposed) return;
                Common.Binary.WriteNetwork16(Blob, Pointer, ReverseValues, (ushort)value);

                //if (Is64BitEntry)
                //    Info->len_64 = (ushort)value;
                //else
                //    Info->len_32 = (ushort)value;
            }
        }

        public bool IsRtcp { get { return PacketLength == 0; } }

        public short PacketLength
        {
            get
            {
                if (Disposed) return 0;

                return (short)Common.Binary.ReadU16(Blob, Pointer + 2, ReverseValues);
            }
            set
            {
                if (Disposed) return;
                
                Common.Binary.WriteNetwork16(Blob, Pointer + 2, ReverseValues, (ushort)value);

            }
        }

        public int Offset
        {
            get
            {
                if (Disposed) return 0;

                return (int)Common.Binary.ReadU32(Blob, Pointer + 4, ReverseValues);
            }
            set
            {
                if (Disposed) return;

                Common.Binary.WriteNetwork32(Blob, Pointer + 4, ReverseValues, value);
            }
        }

        #endregion

        #region Constructor

        internal RtpToolEntry(FileFormat format, byte[] memory = null, long fileOffset = 0)
        {
            Format = format;
            Blob = memory;
            FileOffset = fileOffset;
        }

        public RtpToolEntry(System.Net.IPEndPoint source, Common.IPacket packet, int offset = 0, long fileOffset = 0)
            :this(FileFormat.Binary, packet.Prepare().ToArray(), fileOffset)
        {
            Blob = CreatePacketHeader(DateTime.UtcNow, source, packet, offset).Concat(Blob).ToArray();
            MaxSize = (int)packet.Length;
            //Create header from source, and packet.Created, if Is64Bit
            //Also needs rd hdrs
            //Blob = packet.Prepare().ToArray();
            //Format = FileFormat.Binary;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Add all the data given by data to the Blob and increments max size.
        /// </summary>
        /// <param name="data"></param>
        public void Concat(IEnumerable<byte> data) { Blob = Enumerable.Concat(Blob, data).ToArray(); MaxSize += data.Count(); }

        /// <summary>
        /// Performas a write by using <see cref="System.Array.Copy"/> into the underlying Blob with the given parameters
        /// </summary>
        /// <param name="blobOffset">The offset into the blob</param>
        /// <param name="data">The data</param>
        /// <param name="offset">The offset</param>
        /// <param name="count">The length</param>
        public void UnsafeWriteAt(int blobOffset, byte[] data, int offset, int count) { System.Array.Copy(data, offset, Blob, blobOffset, count); }

        /// <summary>
        /// Returns a string forrmated in the rtpsend text format.
        /// Throws a <see cref="NotSupportedException"/> if <see cref="m_ManagedPacket"/> is not a <see cref="Rtp.RtpPacket"/> or <see cref="Rtcp.RtcpPacket"/>
        /// </summary>
        /// <returns></returns>
        public string ToString(FileFormat? format = null)
        {
            //Get the format given or use the format of the Item existing
            format = format ?? Format;

            //If the item was read in as Text it should have m_Format == Text and a boxed packet, just return the bytes as they were as to not waste memory
            if (format == FileFormat.Text && Format >= FileFormat.Text) return Encoding.ASCII.GetString(Blob);
            else return ToTextualConvention(format);
        }

        public string ToTextualConvention(FileFormat? format = null)
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                var ip = new System.Net.IPAddress((uint)Source);
                var ep = new System.Net.IPEndPoint(ip, Port);
                var ts = TimeSpan.FromSeconds(StartSeconds).Add(TimeSpan.FromSeconds(Microseconds / 10000));

                //sb.AppendFormat("{0} len={1} from={2}", Timeoffset, Length, ep);

                if (IsRtcp) sb.Append(RtpSend.ToTextualConvention(format ?? Format, Media.Rtcp.RtcpPacket.GetPackets(Blob, DefaultEntrySize, MaxSize - DefaultEntrySize), ts, ep));
                else using (var rtp = new Rtp.RtpPacket(Blob, DefaultEntrySize)) sb.Append(RtpSend.ToTextualConvention(format ?? Format, rtp, ts, ep));

                return sb.ToString();    
            }
            catch { throw; }
        }

        public override string ToString()
        {
            return ToString(Format);
        }

        public unsafe override void Dispose()
        {
            base.Dispose();
            //Info = null;
            Blob = null;
        }

        #endregion

      
    }
    
}
