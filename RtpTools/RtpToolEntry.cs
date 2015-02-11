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
              struct timeval start;  //start of recording (GMT) (Could be 8 bytes or otherwise depending on machine architecture)
              u_int32 source;        //network source (multicast address)
              u_int16 port;          //UDP port
            * u_int16 padding;       //Padding
            } RD_hdr_t;
         
           typedef struct {
              u_int16 length;    // length of packet, including this header (may 
                                    be smaller than plen if not whole packet recorded) 
              u_int16 plen;      // actual header+payload length for RTP, 0 for RTCP 
              u_int32 offset;    // milliseconds since the start of recording 
            } RD_packet_t;
        */

    ///<summary>
    /// All entries take (At least) 24 bytes of memory.        
    ///
    /// In Text Format it will describe the same information if aligned as follows:
    ///
    /// The total size should be 32 bytes for an entry which consisted of only that information e.g. 
    /// 0.000000 RTCP len=0 from=0.0.0.0:0(which is 34 bytes)
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

        public static IEnumerable<byte> CreatePacketHeader(Common.IPacket packet, int offset)
        {
            //Only the packet and offset are really needed. 

            //len (2)
            //plen (2)
            //offset (4)

            if (!BitConverter.IsLittleEndian)
                return BitConverter.GetBytes((ushort)(packet.Length + sizeOf_RD_packet_T)).
                    Concat
                    (packet is Rtcp.RtcpPacket ?
                    BitConverter.GetBytes((ushort)0)
                :
                    BitConverter.GetBytes((ushort)(packet.Length)).Concat(BitConverter.GetBytes(offset)));

            return BitConverter.GetBytes((ushort)(packet.Length + sizeOf_RD_packet_T)).Reverse().
                Concat
                (packet is Rtcp.RtcpPacket ?
                BitConverter.GetBytes((ushort)0)
            :
                BitConverter.GetBytes((ushort)(packet.Length)).Reverse()).Concat(BitConverter.GetBytes(offset).Reverse());
        }

        public const int sizeOf_RD_hdr_t = 16, //Columbia RtpTools don't include this for every packet, only the first.
            sizeOf_RD_packet_T = 8;

        internal static RtpToolEntry CreateShortEntry(DateTime timeBase, System.Net.IPEndPoint source, byte[] memory, int offset = 0, long? fileOffset = null)
        {
            /* Only the header can be restored / represented and indicates a VAT or RTP Packet
               RTP or vat data in tabular form: [-]time ts [seq], where a - indicates a set marker bit. The sequence number seq is only used for RTP packets.
               844525727.800600 954849217 30667
               844525727.837188 954849537 30668
               844525727.877249 954849857 30669
               844525727.922518 954850177 30670
           */


            if (source == null) source = new System.Net.IPEndPoint(0, 0);

            //Tokenize the entry
            string [] entryParts = Encoding.ASCII.GetString(memory, 0, memory.Length - 1).Split((char)Common.ASCII.Space);

            int partCount = entryParts.Length;

            //Get timeBase.

            //Parse the TS / Length In Words

            double time = double.Parse(entryParts[0]);

            if (partCount > 2)
            {
                //This is a Vat / Rtp entry

                //Parse the SEQ

                int ts = int.Parse(entryParts[1]);

                return new RtpToolEntry(timeBase.AddMilliseconds(time), source, new Rtp.RtpPacket(2, false, false, ts < 0, 0, 0, 0, int.Parse(entryParts[2]), ts, Utility.Empty), offset, fileOffset ?? 0);

            }
            else
            {
                 return new RtpToolEntry(timeBase.AddMilliseconds(time), source, new Rtcp.RtcpPacket(2, 0, 0, 0, int.Parse(entryParts[1]), partCount > 2 ? int.Parse(entryParts[2]) : 0),offset, fileOffset ?? 0);
            }
        }

        #endregion

        #region Fields

        public readonly long FileOffset;

        /// <summary>
        /// The <see cref="FileFormat"/> on the RtpToolEntry.
        /// </summary>
        public readonly FileFormat Format;

        public readonly System.Net.IPEndPoint Source;

        public readonly DateTime Timebase;

        /// <summary>
        /// Indicates the length of <see cref="Blob"/>.
        /// </summary>
        public int BlobLength = RtpToolEntry.sizeOf_RD_packet_T;

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
        /// Gets the data in <see cref="Blob"/> which consists of the packet octets.
        /// </summary>
        public IEnumerable<byte> Data
        {
            get { return Blob.Skip(Pointer + sizeOf_RD_packet_T); }
        }

        /// <summary>
        /// Controls the offset in which values are returned from the Blob structure.
        /// </summary>
        public int Pointer = 0;

        /// <summary>
        /// The value of the property `length` as indicated from the RD_packet_t
        /// </summary>
        public short Length
        {
            get
            {
                if (IsDisposed) return 0;

                return (short)Common.Binary.ReadU16(Blob, Pointer, ReverseValues);
            }
            set
            {
                if (IsDisposed) return;

                Common.Binary.Write16(Blob, Pointer, ReverseValues, (ushort)value);
            }
        }

        public bool IsRtcp { get { return PacketLength == 0; } }

        public short PacketLength
        {
            get
            {
                if (IsDisposed) return 0;

                return (short)Common.Binary.ReadU16(Blob, Pointer + 2, ReverseValues);
            }
            set
            {
                if (IsDisposed) return;
                
                Common.Binary.Write16(Blob, Pointer + 2, ReverseValues, (ushort)value);

            }
        }

        /// <summary>
        /// Offset in milliseconds since the Timebase.
        /// </summary>
        public int Offset
        {
            get
            {
                if (IsDisposed) return 0;

                return (int)Common.Binary.ReadU32(Blob, Pointer + 4, ReverseValues);
            }
            set
            {
                if (IsDisposed) return;

                Common.Binary.Write32(Blob, Pointer + 4, ReverseValues, value);
            }
        }

        #endregion

        #region Constructor

        internal RtpToolEntry(DateTime timeBase, System.Net.IPEndPoint source, FileFormat format, byte[] memory = null, int? offset = null, long? fileOffset = null)
        {
            Timebase = timeBase;
            Source = source;
            Format = format;
            Blob = memory;
            FileOffset = fileOffset ?? 0;            
            BlobLength = memory.Length;
            if (offset.HasValue) Offset = offset.Value;
        }

        public RtpToolEntry(DateTime timeBase, System.Net.IPEndPoint source, Common.IPacket packet, int? offset = null, long? fileOffset = null)
            : this(timeBase, source, FileFormat.Binary, CreatePacketHeader(packet, offset ?? 0).Concat(packet.Prepare()).ToArray(), offset, fileOffset)
        {
            BlobLength = (int)(sizeOf_RD_packet_T + packet.Length);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Add all the data given by data to the Blob and increments max size.
        /// </summary>
        /// <param name="data"></param>
        public void Concat(IEnumerable<byte> data) { Blob = Enumerable.Concat(Blob, data).ToArray(); BlobLength += data.Count(); }

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

            //If the item was read in as Text it should have m_Format == Text just return the bytes as they were as to not waste memory
            if (format == FileFormat.Text && Format >= FileFormat.Text) return Encoding.ASCII.GetString(Blob);
            else return ToTextualConvention(format);
        }

        public string ToTextualConvention(FileFormat? format = null)
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                var ts = Timebase.TimeOfDay.Add(TimeSpan.FromMilliseconds(Offset));

                if (IsRtcp) sb.Append(RtpSend.ToTextualConvention(format ?? Format, Media.Rtcp.RtcpPacket.GetPackets(Blob, Pointer + sizeOf_RD_packet_T, BlobLength - sizeOf_RD_packet_T), ts, Source));
                else using (var rtp = new Rtp.RtpPacket(Blob, Pointer + sizeOf_RD_packet_T)) sb.Append(RtpSend.ToTextualConvention(format ?? Format, rtp, ts, Source));

                return sb.ToString();    
            }
            catch { throw; }
        }

        public override string ToString()
        {
            return ToString(Format);
        }

        public override void Dispose()
        {
            base.Dispose();
            //Info = null;
            Blob = null;
        }

        #endregion

      
    }
    
}
