using System;
using System.Linq;
using System.Collections.Generic;

namespace Media.Rtp.RtpDump
{
    //Defines commonly used values for writing rtpdump files.
    internal sealed class RtpDumpConstants
    {
        internal static double Version = 1.0;

        /// <summary>
        /// Format:
        /// 0 - Version (1.0)
        /// 1 - Address (IP Address Obtained From)
        /// 2 - Port (Port from Address)
        /// </summary>
        internal static string FileHeader = "#!rtpplay{0} {1}/{2}\n";

        internal static char [] SpaceSplit = new char[] { ' ' };

        static RtpDumpConstants() { }

        internal static string ReadDelimitedValue(System.IO.BinaryReader reader, char delimit = ' ')
        {
            byte temp;
            List<byte> buffer = new List<byte>();
            while ((temp = (byte)reader.ReadByte()) != delimit)
            {                
                buffer.Add(temp);
            }
            return System.Text.Encoding.ASCII.GetString(buffer.ToArray());
        }

    }

    #region Nested Types

    /// <summary>
    /// The known formats of a rtpdump file.
    /// </summary>
    public enum DumpFormat
    {
        Unknown = 0,
        Binary,
        Header,
        Payload,
        Ascii,
        Hex,
        Rtcp,
        Short
    }

    /// <summary>
    /// The types of items found in a rtpdump
    /// </summary>
    public enum DumpItemType
    {
        Invalid = 0,
        Rtp,
        Rtcp,
        //VatC,
        //VatD
    }

    /// <summary>
    /// Represents the beginning of a rtpdump File.
    /// Processes the !#rtpplay header and the DumpHeader which occurs after
    /// </summary>
    internal struct DumpHeader
    {
        const int HeaderSize = 22;

        public DateTime UtcStart { get; internal set; }
        public System.Net.IPEndPoint SourceEndPoint { get; internal set; }

        public DumpHeader(System.IO.BinaryReader reader)
            : this()
        {
            if (reader.BaseStream.Length - reader.BaseStream.Position < HeaderSize) throw new ArgumentOutOfRangeException("Cannot read the required bytes because not enough bytes remain! Required: " + HeaderSize + " Had: " + (reader.BaseStream.Position - reader.BaseStream.Length));
            try
            {
                UtcStart = new System.DateTime(Utility.UtcEpoch1970.Ticks + (long)(System.Net.IPAddress.HostToNetworkOrder(reader.ReadInt64()) * TimeSpan.TicksPerSecond) + (((long)System.Net.IPAddress.HostToNetworkOrder(reader.ReadInt64())) * TimeSpan.TicksPerMillisecond) / 1000); //16 Bytes                
            }
            catch { UtcStart = DateTime.UtcNow; }
            //UtcStart = new DateTime(Constants.TicksAtEpoch);
            //UtcStart.AddSeconds((double)(System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt64()) * TimeSpan.TicksPerSecond));
            //UtcStart.AddMilliseconds((double)(System.Net.IPAddress.NetworkToHostOrder(reader.ReadInt64()) * TimeSpan.TicksPerMillisecond) / 1000);
            SourceEndPoint = new System.Net.IPEndPoint((long)Utility.ReverseUnsignedInt(reader.ReadUInt32()), Utility.ReverseUnsignedShort(reader.ReadUInt16())); //6 Bytes
        }

        public DumpHeader(byte[] dumpData, ref int offset)
        {
            this = new DumpHeader(new System.IO.BinaryReader(new System.IO.MemoryStream(dumpData)));
            offset += HeaderSize;
        }

        public void Write(System.IO.BinaryWriter writer, DumpFormat format)
        {
            //Can't leave the BinaryWriter open with 4.0 ...
            //using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(stream, System.Text.Encoding.Default, true))                

            if (format == DumpFormat.Binary)
            {
                byte[] fileHeader = System.Text.Encoding.ASCII.GetBytes(string.Format(RtpDumpConstants.FileHeader, RtpDumpConstants.Version.ToString("0.0"), SourceEndPoint.Address, SourceEndPoint.Port));
                writer.Write(fileHeader, 0, fileHeader.Length);
            }

            //TODO FIX THIS Calculation
            long seconds = UtcStart.Ticks - Utility.UtcEpoch1970.Ticks / TimeSpan.TicksPerSecond;
            long microseconds = (seconds * TimeSpan.TicksPerSecond / 0x100000000L);

            writer.Write(System.Net.IPAddress.HostToNetworkOrder(seconds));
            writer.Write(System.Net.IPAddress.HostToNetworkOrder(microseconds));
            //writer.Write((byte)0);
            writer.Write(Utility.ReverseUnsignedInt((uint)SourceEndPoint.Address.Address));
            writer.Write(Utility.ReverseUnsignedShort((ushort)SourceEndPoint.Port));
        }
    }

    /// <summary>
    /// Implements the individual items found in a rtpdump.
    /// http://www.cs.columbia.edu/irt/software/rtptools/
    /// </summary>
    internal struct DumpItem
    {
        const int DumpItemSize = 8;

        #region Properties

        public DumpItemType ItemType
        {
            get
            {
                //return PacketLength == 0 ? DumpItemType.Rtcp : DumpItemType.Rtp; } }
                if (PacketLength == 0)
                {
                    if (Packet[0] >> 6 == 2)
                    {
                        return DumpItemType.Rtcp;
                    }
                    else
                    {
                        //return DumpItemType.VatC;
                        return DumpItemType.Invalid;
                    }
                }
                else
                {
                    if (Packet[0] >> 6 == 2)
                    {
                        return DumpItemType.Rtp;
                    }
                    else
                    {
                        //return DumpItemType.VatD;
                        return DumpItemType.Invalid;
                    }
                }
            }
        }

        /// <summary>
        /// Length of the DumpItem including Packet Length
        /// </summary>
        public ushort Length { get; internal set; }

        /// <summary>
        /// The length of the packet which follows.
        /// This is the actual header+payload length for RTP, 0 for RTCP
        /// </summary>
        public ushort PacketLength { get; internal set; }

        /// <summary>
        /// Offset in time since start of the recording
        /// </summary>
        public TimeSpan TimeOffset { get; internal set; }

        /// <summary>
        /// The offset from which this item was retireved from a dump
        /// </summary>
        public long FileOffset { get; internal set; }

        /// <summary>
        /// The packet contained in this item
        /// </summary>
        public byte[] Packet { get; internal set; }

        public int PacketVersion
        {
            get
            {
                return Packet != null ? Packet[0] >> 6 : 0;
            }
        }

        public byte PacketType
        {
            get
            {
                return Packet != null ?
                    ItemType == DumpItemType.Rtp ?
                    //Rtp                      //Rtcp      //Other
                        (byte)(Packet[1] & 0x7f) : Packet[1] : byte.MinValue;
            }
        }

        public bool PacketMarker
        {
            get
            {
                return Packet != null ? Packet[1] >> 7 == 1 : false;
            }
        }

        public uint PacketTimestamp
        {
            get
            {
                return Packet != null ? Utility.ReverseUnsignedInt(System.BitConverter.ToUInt32(Packet, 4)) : 0;
            }
        }

        public ushort PacketSequenceNumber
        {
            get
            {
                return Packet != null ? Utility.ReverseUnsignedShort(System.BitConverter.ToUInt16(Packet, 2)) : ushort.MinValue;
            }
        }

        #endregion

        #region Constructor

        public DumpItem(System.IO.BinaryReader reader, DumpFormat format)
            : this()
        {
            if (!reader.BaseStream.CanRead) throw new ArgumentException("Cannot read stream", "Dump");
            if (reader.BaseStream.Position >= reader.BaseStream.Length) throw new System.IO.EndOfStreamException("Reader is already at the End of the Stream.");
            if (format == DumpFormat.Unknown) throw new Exception("Cannot write unknown format");
            if (format == DumpFormat.Binary)
            {
                #region Binary Format

                //Save offset
                FileOffset = reader.BaseStream.Position;

                //Read fields
                Length = Utility.ReverseUnsignedShort(reader.ReadUInt16());
                if (Length <= DumpItemSize) throw new InvalidOperationException("Invald DumpItem, Length must be greater than DumpItemSize.");
                PacketLength = Utility.ReverseUnsignedShort(reader.ReadUInt16());
                TimeOffset = TimeSpan.FromMilliseconds(Utility.ReverseUnsignedInt(reader.ReadUInt32()));

                //Rtcp
                if (PacketLength == 0) 
                {
                    //Read length from RtcpHeader
                    byte compound = reader.ReadByte();
                    int version = compound >> 6;
                    if (version != 2) throw new NotSupportedException("Invalid DumpItem. (Invald Rtcp Version, VAT Not Supported) Only version 2 is defined. Found: " + version);
                    byte type = reader.ReadByte();
                    //Get the length
                    byte h = reader.ReadByte(), l = reader.ReadByte();
                    //Decode it
                    PacketLength = (ushort)(h << 8 | l);
                    //Make the packet
                    Packet = new byte[4 + (PacketLength * 4)];
                    //Put the bytes back
                    Packet[0] = compound;
                    Packet[1] = type;
                    Packet[2] = h;
                    Packet[3] = l;
                    //Read the rest
                    reader.Read(Packet, 4, (PacketLength * 4));
                }
                else //Rtp
                {
                    Packet = new byte[PacketLength];
                    reader.Read(Packet, 0, PacketLength);
                }

                #endregion
            }
            else if (format == DumpFormat.Rtcp && ItemType == DumpItemType.Rtcp || format == DumpFormat.Ascii || format == DumpFormat.Hex)
            {

                TimeOffset = TimeSpan.FromMilliseconds(double.Parse(RtpDumpConstants.ReadDelimitedValue(reader), System.Globalization.CultureInfo.InvariantCulture));

                string type = RtpDumpConstants.ReadDelimitedValue(reader);

                if (type != "RTP" && type != "RTCP") throw new Exception("Invalid Dump, Expected RTP or RTCP, Found: " + type);

                PacketLength = ushort.Parse(RtpDumpConstants.ReadDelimitedValue(reader).Split('=')[1], System.Globalization.CultureInfo.InvariantCulture);

                //Read from
                string from = RtpDumpConstants.ReadDelimitedValue(reader);

                //Determine further action based on type

                if (type == "RTP")
                {
                    //v=2 p=0 x=0 cc=0 m=0 pt=5 (IDVI,1,8000) seq=28178 ts=954052737 ssrc=0x124e2b58                

                    //Read v=
                    int version = RtpDumpConstants.ReadDelimitedValue(reader).Last() - 0x30;

                    //Read p=
                    bool padding = RtpDumpConstants.ReadDelimitedValue(reader).Last() == 0x30 ? false : true;

                    //Read x=
                    bool extensions = RtpDumpConstants.ReadDelimitedValue(reader).Last() == 0x30 ? false : true;

                    //Read cc=
                    int csc = int.Parse(RtpDumpConstants.ReadDelimitedValue(reader).Split('=')[1], System.Globalization.CultureInfo.InvariantCulture);

                    //Read m=
                    bool marker = RtpDumpConstants.ReadDelimitedValue(reader).Last() == 0x30 ? false : true;

                    //Read pt=
                    byte payloadType = (byte)(int.Parse(RtpDumpConstants.ReadDelimitedValue(reader).Split('=')[1], System.Globalization.CultureInfo.InvariantCulture) & 0x7f);

                    //Skip format info
                    string formatInfo = RtpDumpConstants.ReadDelimitedValue(reader);

#if DEBUG
                    System.Diagnostics.Debug.WriteLine("Read Dump with FormatInfo:" + formatInfo);
#endif

                    //Read seq=
                    int seq = int.Parse(RtpDumpConstants.ReadDelimitedValue(reader).Split('=')[1], System.Globalization.CultureInfo.InvariantCulture);

                    //Read ts=
                    int ts = int.Parse(RtpDumpConstants.ReadDelimitedValue(reader).Split('=')[1], System.Globalization.CultureInfo.InvariantCulture);

                    //Read ssrc=
                    int ssrc = int.Parse(RtpDumpConstants.ReadDelimitedValue(reader).Split('=')[1], System.Globalization.NumberStyles.HexNumber);

                    //Read ext info
                    //
                    //

                    //Put packetBytes together
                    RtpPacket packet = new RtpPacket(PacketLength);
                    packet.Marker = marker;
                    packet.Padding = padding;
                    packet.PayloadType = payloadType;
                    while (csc > 0)
                    {
                        packet.ContributingSources.Add((uint)csc--);
                    }
                    Packet = packet.ToBytes();

                }
                else //RTCP
                {

                    //Determined from values
                    PacketLength = 0;

                    if (!string.IsNullOrWhiteSpace(RtpDumpConstants.ReadDelimitedValue(reader, '('))) throw new Exception("Invalid Dump File.");

                    //Read the type
                    string subtype = RtpDumpConstants.ReadDelimitedValue(reader);
                    if (subtype != "SR" && subtype != "RR" && subtype != "SDES" && subtype != "BYE")
                    {
                        throw new NotSupportedException("Invalid Rtcp Subtype '" + subtype + '\'');
                    }
                    
                    //Read ssrc / src =
                    int ssrc = 0;
                    
                    //Does not occur in SDES
                    if (subtype != "SDES")
                    {

                        //Read ssrc=
                        ssrc = int.Parse(RtpDumpConstants.ReadDelimitedValue(reader).Split('=')[1], System.Globalization.NumberStyles.HexNumber);
                    }

                    //Read p=
                    bool padding = RtpDumpConstants.ReadDelimitedValue(reader).Last() == 0x30 ? false : true;

                    //Read count=
                    int blockCount = int.Parse(RtpDumpConstants.ReadDelimitedValue(reader).Split('=')[1], System.Globalization.NumberStyles.HexNumber);

                    //Read Len
                    short len = short.Parse(RtpDumpConstants.ReadDelimitedValue(reader).Split('=')[1], System.Globalization.CultureInfo.InvariantCulture);

                    Rtcp.RtcpPacket packet = null;

                    if (subtype == "SR")
                    {
                        packet = new Rtcp.RtcpPacket(Rtcp.RtcpPacket.RtcpPacketType.SendersReport);
                    }
                    else if (subtype == "RR")
                    {
                        packet = new Rtcp.RtcpPacket(Rtcp.RtcpPacket.RtcpPacketType.ReceiversReport);
                    }
                    else if (subtype == "SDES")
                    {
                        packet = new Rtcp.RtcpPacket(Rtcp.RtcpPacket.RtcpPacketType.SourceDescription);
                    }
                    else if (subtype == "BYE")
                    {
                        packet = new Rtcp.RtcpPacket(Rtcp.RtcpPacket.RtcpPacketType.Goodbye);
                    }
                    else throw new NotSupportedException("The RTCP subtype: '" + subtype + "' is not supported.");

                    packet.Padding = padding;
                    packet.Length = len;
                    packet.BlockCount = blockCount;

                    //Ensure(
                    //Read while temp != )
                    //Example
                    //ntp=xxxx ssrc=bc64b658 fraction=0.503906 lost=4291428375 last_seq=308007791 jit=17987961 lsr=2003335488 dlsr=825440558)

                    //Read until end of description
                    string[] directives = RtpDumpConstants.ReadDelimitedValue(reader, ')').Split(RtpDumpConstants.SpaceSplit, StringSplitOptions.RemoveEmptyEntries);

                    if (subtype == "SR")
                    {
                        Rtcp.SendersReport sr = new Rtcp.SendersReport((uint)ssrc);

                        if (directives.Length > 0 && !string.IsNullOrWhiteSpace(directives[0]))
                        {
                            sr.NtpTimestamp = ulong.Parse(directives[0].Split('=')[1], System.Globalization.CultureInfo.InvariantCulture);
                        }

                        if (directives.Length > 1 && !string.IsNullOrWhiteSpace(directives[1]))
                        {
                            sr.RtpTimestamp = uint.Parse(directives[1].Split('=')[1], System.Globalization.CultureInfo.InvariantCulture);
                        }

                        if (directives.Length > 2 && !string.IsNullOrWhiteSpace(directives[2]))
                        {
                            sr.SendersPacketCount = uint.Parse(directives[2].Split('=')[1], System.Globalization.CultureInfo.InvariantCulture);
                        }

                        if (directives.Length > 3 && !string.IsNullOrWhiteSpace(directives[3]))
                        {
                            sr.SendersOctetCount = uint.Parse(directives[3].Split('=')[1], System.Globalization.CultureInfo.InvariantCulture);
                        }

                        //Determine if there are blocks
                        bool containsBlocks = directives.Any(d => d.StartsWith("(ssrc="));

                        if (containsBlocks)
                        {
                            int blockIndex = 0;

                            while (blockIndex < directives.Length)
                            {
                                sr.Blocks.Add(new Rtcp.ReportBlock(uint.Parse(directives[blockIndex++].Split('=')[1], System.Globalization.CultureInfo.InvariantCulture))
                                {
                                    FractionLost = uint.Parse(directives[blockIndex++].Split('=')[1], System.Globalization.CultureInfo.InvariantCulture),
                                    CumulativePacketsLost = int.Parse(directives[blockIndex++].Split('=')[1], System.Globalization.CultureInfo.InvariantCulture),
                                    ExtendedHigestSequenceNumber = uint.Parse(directives[blockIndex++].Split('=')[1], System.Globalization.CultureInfo.InvariantCulture),
                                    InterArrivalJitter = uint.Parse(directives[blockIndex++].Split('=')[1], System.Globalization.CultureInfo.InvariantCulture),
                                    LastSendersReport = uint.Parse(directives[blockIndex++].Split('=')[1], System.Globalization.CultureInfo.InvariantCulture),
                                    DelaySinceLastSendersReport = uint.Parse(directives[blockIndex++].Split('=')[1], System.Globalization.CultureInfo.InvariantCulture),
                                });
                            }
                        }

                        //Use ToPacket to build field
                        Packet = sr.ToPacket().ToBytes();
                    }
                    else if (subtype == "RR")
                    {
                        Rtcp.ReceiversReport rr = new Rtcp.ReceiversReport((uint)ssrc);

                        //Determine if there are blocks
                        bool containsBlocks = directives.Any(d => d.StartsWith("(ssrc="));

                        if (containsBlocks)
                        {
                             int blockIndex = 0;

                             while (blockIndex < directives.Length)
                             {
                                 rr.Blocks.Add(new Rtcp.ReportBlock(uint.Parse(directives[blockIndex++].Split('=')[1], System.Globalization.CultureInfo.InvariantCulture))
                                 {
                                     FractionLost = uint.Parse(directives[blockIndex++].Split('=')[1], System.Globalization.CultureInfo.InvariantCulture),
                                     CumulativePacketsLost = int.Parse(directives[blockIndex++].Split('=')[1], System.Globalization.CultureInfo.InvariantCulture),
                                     ExtendedHigestSequenceNumber = uint.Parse(directives[blockIndex++].Split('=')[1], System.Globalization.CultureInfo.InvariantCulture),
                                     InterArrivalJitter = uint.Parse(directives[blockIndex++].Split('=')[1], System.Globalization.CultureInfo.InvariantCulture),
                                     LastSendersReport = uint.Parse(directives[blockIndex++].Split('=')[1], System.Globalization.CultureInfo.InvariantCulture),
                                     DelaySinceLastSendersReport = uint.Parse(directives[blockIndex++].Split('=')[1], System.Globalization.CultureInfo.InvariantCulture),
                                 });
                             }
                        }

                        //Use ToPacket to build field
                        Packet = rr.ToPacket().ToBytes();

                    }
                    else if (subtype == "SDES")
                    {

                        Rtcp.SourceDescription sd = null;

                        //Determine if there are blocks
                        bool containsBlocks = directives.Any(d => d.StartsWith("(src="));

                        if (containsBlocks)
                        {
                            uint src = uint.Parse(directives[0].Split('=')[1], System.Globalization.NumberStyles.HexNumber);

                            sd = new Rtcp.SourceDescription(src);

                            int blockIndex = 1;

                            while (blockIndex < directives.Length)
                            {
                                string[] parts = directives[blockIndex++].Split('=');
                                sd.Add(new Rtcp.SourceDescription.SourceDescriptionItem((Rtcp.SourceDescription.SourceDescriptionType)Enum.Parse(typeof(Rtcp.SourceDescription.SourceDescriptionType), parts[0], true))
                                {
                                    Text = parts[1].Replace("\"", string.Empty)
                                });
                            }

                            //Use ToPacket to build field
                            Packet = sd.ToPacket().ToBytes();

                        }
                        else
                        {
                            //Use ToBytes of Packet to build field
                            Packet = packet.ToBytes();
                        }
                        
                    }                   
                }

                //Read "\r\n)"
                RtpDumpConstants.ReadDelimitedValue(reader, ')');
                
                //Read \r\n
                RtpDumpConstants.ReadDelimitedValue(reader, '\n');

                //Check for \r\n
                if (reader.Read() == (byte)'\r')
                {
                    if (!(reader.Read() == (byte)'\n'))
                    {
                        reader.BaseStream.Seek(-1, System.IO.SeekOrigin.Current);
                    }
                }
                else
                {
                    reader.BaseStream.Seek(-1, System.IO.SeekOrigin.Current);
                }

                //Determine if data is present (This may preceed ASCII data or ASCII should not be present in hex?)
                if (reader.Read() == (byte)'d')
                {
                    format = DumpFormat.Hex;
                    //reader.BaseStream.Seek(-1, System.IO.SeekOrigin.Current);
                }
                else
                {
                    reader.BaseStream.Seek(-1, System.IO.SeekOrigin.Current);
                }

                //Handle hex at the same time we handle ASCII just incase
                if (format == DumpFormat.Hex)
                {
                    List<byte> buffer = new List<byte>();
                    byte temp;

                    //Read Hex Format data=
                    if (reader.Read() == (byte)'a')
                    {
                        reader.Read();//t
                        reader.Read();//a
                        reader.Read();//=
                        buffer.Clear();
                        while ((temp = (byte)reader.Read()) != (byte)'\n' && temp != (byte)'\r')
                        {
                            if (temp != (byte)'-')
                            {
                                buffer.Add(temp);
                                PacketLength++;
                            }
                        }

                        //We already have the data?
                        //Either I am missing something and HEX does not get the ASCII data or this is redundant..
                        //Packet = System.Text.Encoding.ASCII.GetBytes(System.Text.Encoding.ASCII.GetString(buffer.ToArray()).Replace("-", string.Empty));
                    }
                    else
                    {
                        //ASCII Already parsed
                        reader.BaseStream.Seek(-1, System.IO.SeekOrigin.Current);
                    }
                }              
            }
            else if (format == DumpFormat.Header)
            {
                //Read Headers?
            }
            else if (format == DumpFormat.Payload)
            {
                //Payloads only Rtp, (RTCP?)
            }           
            else if (format == DumpFormat.Short)
            {
                /*
                 RTP or vat data in tabular form: [-]time ts [seq], where a - indicates a set marker bit. The sequence number seq is only used for RTP packets.
                 844525727.800600 954849217 30667
                 844525727.837188 954849537 30668
                 844525727.877249 954849857 30669
                 844525727.922518 954850177 30670
                 */
            }

        }

        public DumpItem(byte[] dumpBytes, ref int offset, DumpFormat format) : this(new System.IO.BinaryReader(new System.IO.MemoryStream(dumpBytes)), format) { offset += Length; }

        public DumpItem(RtpPacket packet, TimeSpan timeOffset)
            : this()
        {
            PacketLength = 0;
            Length = (ushort)(8 + packet.Length);
            TimeOffset = timeOffset;
            FileOffset = -1;
            Packet = packet.ToBytes();
        }

        public DumpItem(Rtcp.RtcpPacket packet, TimeSpan timeOffset)
            : this()
        {
            PacketLength = 0;
            Length = (ushort)(DumpItemSize + packet.Length + Rtcp.RtcpPacket.RtcpHeaderLength);
            TimeOffset = timeOffset;
            FileOffset = -1;
            Packet = packet.ToBytes();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a binary representation of the DumpItem ready to be written to a rtpdump file.
        /// </summary>
        /// <param name="format">The format to write the DumpItem in</param>
        /// <param name="header">The header of the DumpItem (so packets added later will be consisent with the address)</param>
        /// <returns></returns>
        public byte[] ToBytes(DumpFormat format, DumpHeader header)
        {
            if (format == DumpFormat.Unknown) throw new Exception("Cannot write unknown format");

            List<byte> result = new List<byte>();

            //Add the header if the type is not payload only
            if (format == DumpFormat.Binary)
            {
                result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedShort(Length)));
                result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedShort(PacketLength)));
                result.AddRange(BitConverter.GetBytes(Utility.ReverseUnsignedInt((uint)TimeOffset.TotalMilliseconds)));
            }

            //If the format is binary or hex the data consists of the Packet
            if (format == DumpFormat.Binary)
            {
                //Hex is like ascii, but with hex dump of payload
                result.AddRange(Packet);
            }
            else if (format == DumpFormat.Rtcp && ItemType == DumpItemType.Rtcp || format == DumpFormat.Hex || format == DumpFormat.Ascii)
            {

                //Only rtp and rtcp for now :) VAT later
                if (ItemType != DumpItemType.Rtp && ItemType != DumpItemType.Rtcp) throw new NotSupportedException();

                //RTP ASCII
                if (ItemType == DumpItemType.Rtp)
                {
                    RtpPacket packet = new RtpPacket(Packet);
                    
                    //Need pseudo map to give name of encoding, channel and rate
                    result.AddRange(System.Text.Encoding.ASCII.GetBytes("\r\n" + TimeOffset.TotalMilliseconds.ToString("0.000000") + " RTP len=" + packet.Length + " from=" + header.SourceEndPoint.ToString() + "v=" + packet.Version + " p=" + (packet.Padding ? "1" : "0") + " x=" + (packet.Extensions ? "1" : "0") + " cc=" + packet.ContributingSourceCount + " m=" + (packet.Marker ? "1" : "0") + "pt=" + packet.PayloadType + " seq=" + packet.SequenceNumber + " ts=" + packet.TimeStamp + " ssrc=" + packet.SynchronizationSourceIdentifier.ToString("x")));

                    for (int index = 0; index < packet.ContributingSourceCount; index++)
                    {
                        result.AddRange(System.Text.Encoding.ASCII.GetBytes("csrc[" + index + "] = " + packet.ContributingSources[index].ToString("X") + ' '));
                    }

                    if (packet.Extensions)
                    {
                        byte[] extensionBytes = packet.ExtensionBytes;
                        result.AddRange(System.Text.Encoding.ASCII.GetBytes("ext_type=" + System.Net.IPAddress.HostToNetworkOrder(BitConverter.ToUInt16(extensionBytes, 0)).ToString("X") + ' '));
                        result.AddRange(System.Text.Encoding.ASCII.GetBytes("ext_len=" + System.Net.IPAddress.HostToNetworkOrder(BitConverter.ToUInt16(extensionBytes, 2) + ' ')));
                        result.AddRange(System.Text.Encoding.ASCII.GetBytes("ext_data=" + BitConverter.ToString(extensionBytes, 4)));
                    }

                    if (format == DumpFormat.Hex)
                    {
                        /*if (format == F_hex) {
                          hlen = parse_header(packet->p.data);
                          fprintf(out, "data=");
                          hex(out, packet->p.data + hlen, trunc < len ? trunc : len - hlen);
                        }*/
                        result.AddRange(System.Text.Encoding.ASCII.GetBytes("data=" + BitConverter.ToString(packet.Payload)));
                        result.Add((byte)'\r');
                        result.Add((byte)'\n');
                    }                                   
                }
                else if (ItemType == DumpItemType.Rtcp) //Rtcp ASCII
                {
                    //Make a Packet from the data so we can write it out
                    Rtcp.RtcpPacket packet = new Rtcp.RtcpPacket(Packet);

                    result.AddRange(System.Text.Encoding.ASCII.GetBytes("\r\n" + TimeOffset.TotalMilliseconds.ToString("0.000000") + " RTCP len=" + packet.Length + " from=" + header.SourceEndPoint.ToString() + "\r\n "));

                    if (PacketType == (byte)Rtcp.RtcpPacket.RtcpPacketType.SendersReport)
                    {                        
                        Rtcp.SendersReport sr = new Rtcp.SendersReport(packet);
                        result.AddRange(System.Text.Encoding.ASCII.GetBytes(" (SR ssrc=" + sr.SynchronizationSourceIdentifier.ToString("X") + " pad=" + (packet.Padding ? "1" : "0") + " count=" + packet.BlockCount + " len=" + packet.Length));
                        result.Add((byte)'\r');
                        result.Add((byte)'\n');
                        result.AddRange(System.Text.Encoding.ASCII.GetBytes(" ntp=" + sr.NtpTimestamp + " ts=" + sr.RtpTimestamp + " psent=" + sr.SendersPacketCount + " osent=" + sr.SendersOctetCount));
                        result.Add((byte)')');
                        result.Add((byte)'\r');
                        result.Add((byte)'\n');
                        foreach (Rtcp.ReportBlock rb in sr.Blocks)
                        {
                            result.AddRange(System.Text.Encoding.ASCII.GetBytes("(ssrc=" + rb.SynchronizationSourceIdentifier.ToString("X") + " fraction=" + rb.FractionLost + " lost=" + rb.CumulativePacketsLost + " last_seq=" + rb.ExtendedHigestSequenceNumber + " jit=" + rb.InterArrivalJitter + " lsr=" + rb.LastSendersReport + " dlsr=" + rb.DelaySinceLastSendersReport + ')'));
                            result.Add((byte)'\r');
                            result.Add((byte)'\n');
                        }
                        result.Add((byte)')');
                        result.Add((byte)'\r');
                        result.Add((byte)'\n');
                    }
                    else if (PacketType == (byte)Rtcp.RtcpPacket.RtcpPacketType.ReceiversReport)
                    {
                        Rtcp.ReceiversReport rr = new Rtcp.ReceiversReport(packet);
                        result.AddRange(System.Text.Encoding.ASCII.GetBytes(" (RR ssrc=" + rr.SynchronizationSourceIdentifier.ToString("X") + " p=" + (packet.Padding ? "1" : "0") + " count=" + packet.BlockCount + " len=" + packet.Length));
                        result.Add((byte)'\r');
                        result.Add((byte)'\n');
                        foreach (Rtcp.ReportBlock rb in rr.Blocks)
                        {
                            result.AddRange(System.Text.Encoding.ASCII.GetBytes(" (ssrc=" + rb.SynchronizationSourceIdentifier.ToString("X") + " fraction=" + rb.FractionLost + " lost=" + rb.CumulativePacketsLost + " last_seq=" + rb.ExtendedHigestSequenceNumber + " jit=" + rb.InterArrivalJitter + " lsr=" + rb.LastSendersReport + " dlsr=" + rb.DelaySinceLastSendersReport + ')'));
                            result.Add((byte)'\r');
                            result.Add((byte)'\n');
                        }
                        result.Add((byte)')');
                        result.Add((byte)'\r');
                        result.Add((byte)'\n');
                    }
                    else if (PacketType == (byte)Rtcp.RtcpPacket.RtcpPacketType.SourceDescription)
                    {
                        Rtcp.SourceDescription sd = new Rtcp.SourceDescription(packet);
                        result.AddRange(System.Text.Encoding.ASCII.GetBytes(" (SDES p=" + (packet.Padding ? "1" : "0") + " count=" + packet.BlockCount + " len=" + packet.Length));
                        result.AddRange(System.Text.Encoding.ASCII.GetBytes(" (src=" + sd.SynchronizationSourceIdentifier.ToString("X") + ' '));
                        foreach (Rtcp.SourceDescription.SourceDescriptionItem item in sd.Items)
                        {
                            result.AddRange(System.Text.Encoding.ASCII.GetBytes(item.DescriptionType.ToString().ToUpperInvariant() + "=\"" + item.Text + "\" "));
                        }
                        result.Add((byte)')');
                        result.Add((byte)'\r');
                        result.Add((byte)'\n');
                        result.Add((byte)')');
                        result.Add((byte)'\r');
                        result.Add((byte)'\n');
                    }
                    else if (PacketType == (byte)Rtcp.RtcpPacket.RtcpPacketType.ApplicationSpecific)
                    {
                        throw new NotSupportedException();
                    }
                    else if (PacketType == (byte)Rtcp.RtcpPacket.RtcpPacketType.Goodbye)
                    {
                        Rtcp.Goodbye gb = new Rtcp.Goodbye(packet);
                        result.AddRange(System.Text.Encoding.ASCII.GetBytes(" (BYE p=" + (packet.Padding ? "1" : "0") + " count=" + packet.BlockCount + " len=" + packet.Length));
                        result.AddRange(System.Text.Encoding.ASCII.GetBytes(" (ssrc=" + gb.SynchronizationSourceIdentifier.ToString("X") + (!string.IsNullOrWhiteSpace(gb.Reason) ? "reason=\"" + gb.Reason + '"' : string.Empty) + " )\r\n"));
                    }
                    else
                    {
                        throw new Exception("Invalid Rtcp PacketType");
                    }

                    if (format == DumpFormat.Hex)
                    {
                        /*if (format == F_hex) {
                          hlen = parse_header(packet->p.data);
                          fprintf(out, "data=");
                          hex(out, packet->p.data + hlen, trunc < len ? trunc : len - hlen);
                        }*/
                        result.AddRange(System.Text.Encoding.ASCII.GetBytes("data=" + BitConverter.ToString(packet.Payload)));
                        
                        result.Add((byte)'\r');
                        result.Add((byte)'\n');
                    }

                }
                else
                {
                    throw new NotSupportedException();
                }
            }         
            else if (format == DumpFormat.Short)
            {
                double timeStamp = TimeOffset.TotalMilliseconds;

                //VAT
                if (PacketVersion == 0)
                {
                    throw new NotImplementedException();
                    //Similar to below but need the VAT properties Flags and Timestamp added to DumpItem
                }
                else if (PacketVersion == 2) //RTP or RTCP?
                {
                    if (ItemType == DumpItemType.Rtp)
                    {
                        if (PacketMarker) timeStamp = -timeStamp;
                        System.Text.Encoding.ASCII.GetBytes(string.Join(" ", timeStamp.ToString("0.000000"), PacketTimestamp, PacketSequenceNumber));
                    }
                    else if (ItemType == DumpItemType.Rtcp)
                    {
                        System.Text.Encoding.ASCII.GetBytes(string.Join(" ", timeStamp.ToString("0.000000"), PacketTimestamp));
                    }
                    else throw new NotSupportedException();
                }
                else throw new Exception("Invalid PacketVersion");

            }
            //Done encoding for format
            return result.ToArray();
        }

        public void Write(System.IO.BinaryWriter writer, DumpFormat format, DumpHeader header)
        {
            if (format == DumpFormat.Unknown) throw new Exception("Cannot write unknown format");
            if (!writer.BaseStream.CanWrite) throw new InvalidOperationException("Cannot write to stream");
            else writer.Write(ToBytes(format, header));        
        }

        #endregion
    }

    #endregion

    #region DumpReader DumpWriter

    /// <summary>
    /// Reads rtpdump compatible files.
    /// http://www.cs.columbia.edu/irt/software/rtptools/
    /// Various formats supported
    /// </summary>
    public sealed class DumpReader : IDisposable, IEnumerable<byte[]>
    {
        //The format of the underlying dump
        internal DumpFormat? m_Format;

        //The header of the underlying dump
        internal DumpHeader? m_Header;

        //A List detailing the offsets at which DumpItems occurs (maybe used by the writer to allow removal of packets from a stream without erasing them from the source?);
        internal List<long> m_Offsets = new List<long>();

        internal System.IO.BinaryReader m_Reader;

        bool m_leaveOpen;

        /// <summary>
        /// The format of the stream determined via reading the file
        /// </summary>
        public DumpFormat Format { get { return m_Format ?? DumpFormat.Unknown; } }

        public DateTime StartTime { get { if (m_Header.HasValue) return m_Header.Value.UtcStart; return DateTime.MinValue; } }

        //The amount of items contained in the dump thus far in reading. (Might not be worth keeping?)
        public int ItemCount { get { return m_Offsets.Count; } }

        /// <summary>
        /// The position in the stream
        /// </summary>
        public long Position { get { return m_Reader.BaseStream.Position; } }

        /// <summary>
        /// The length of the stream
        /// </summary>
        public long Length { get { return m_Reader.BaseStream.Length; } }

        /// <summary>
        /// Creates a DumpReader on the given stream
        /// </summary>
        /// <param name="stream">The stream to read</param>
        /// <param name="leaveOpen">Indicates if the stream should be left open after reading</param>
        public DumpReader(System.IO.Stream stream, bool leaveOpen = false)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            m_leaveOpen = leaveOpen;
            m_Reader = new System.IO.BinaryReader(stream);
            ReadHeader();
        }

        /// <summary>
        /// Creates a DumpReader on the path given which must be a valid rtpdump format file. An excpetion will be thrown if the file does not exist or is invalid.
        /// </summary>
        /// <param name="path">The file to read</param>
        public DumpReader(string path) : this(new System.IO.FileStream(path, System.IO.FileMode.Open)) { }

        /// <summary>
        /// Reads the file header if present and thus determines if the file is Binary or Ascii in the process.
        /// </summary>
        internal void ReadHeader()
        {
            if (!m_Header.HasValue)
            {
                if (m_Reader.ReadByte() == (byte)'#')
                {
                    //Progress past the FileHeader should be #!rtpplay1.0
                    if (m_Reader.ReadByte() != '!' || m_Reader.ReadByte() != 'r' || m_Reader.ReadByte() != 't' || m_Reader.ReadByte() != 'p' || m_Reader.ReadByte() != 'p' || m_Reader.ReadByte() != 'l' || m_Reader.ReadByte() != 'a' || m_Reader.ReadByte() != 'y')
                    {
                        throw new Exception("Invalid rtpdump file, Expected #!rtpplay.");
                    }
                    
                    //Ensure Version
                    if(m_Reader.ReadByte() != '1' || m_Reader.ReadByte() != '.' || m_Reader.ReadByte() != '0')
                    {
                        throw new NotSupportedException("Only version 1 is defined");
                    }
                    
                    //Source and Port
                    //0.0.0.0/7\n
                    while (m_Reader.ReadByte() != (byte)'\n') { }
                    
                    //Should be binary unless we find out different
                    m_Format = DumpFormat.Binary; //It may be header only, etc
                }
                else
                {
                    m_Reader.BaseStream.Seek(-1, System.IO.SeekOrigin.Current);
                    m_Format = DumpFormat.Ascii;//Or hex check for data=
                }
                
                //Read DumpHeader (should be per packet)
                m_Header = new DumpHeader(m_Reader);
            }
        }

        /// <summary>
        /// Reads a DumpItem from the stream and adds the offset to the list of offets. 
        /// Also determines if the format is Hex if not already determined of if the format is unknown
        /// </summary>
        /// <returns>The DumpItem read</returns>
        internal DumpItem? ReadDumpItem()
        {
            try
            {
                //Read the item
                DumpItem? item = new DumpItem(m_Reader, m_Format.Value);
                
                //If the fomrat is ASCII AND item has the value
                if (m_Format == DumpFormat.Ascii && item.HasValue)
                {
                    //Add the offset
                    m_Offsets.Add(item.Value.FileOffset);

                    //If the value's packet is null or has no bytes this is the header format
                    if (item.Value.Packet == null || item.Value.Packet.Length == 0)
                    {
                        m_Format = DumpFormat.Header;
                    }
                    else//This is HEX!
                    {
                        m_Format = DumpFormat.Hex;
                    }
                }
                //Cant be binary without the payload unless it's Header, Payload, etc NON ASCII I think                
                else if (m_Format == DumpFormat.Binary &&  (item.Value.Packet == null || item.Value.Packet.Length == 0)) m_Format = DumpFormat.Unknown; 
                return item;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Reads the entire dump maintaing a list of all offsets encountered where items occur
        /// </summary>
        public void ReadToEnd()
        {
            DumpItem? current = null;
            while ((current = ReadDumpItem()).HasValue)
            {
#if DEBUG
                System.Diagnostics.Debug.Write("Found DumpItem @ " + current.Value.FileOffset);
#endif
            }

            //Should be at the end of the stream here

        }

        /// <summary>
        /// Reads the dump until the next packet occurs, the type of packet can be determined by inspecting the first few bytes.
        /// </summary>
        /// <param name="type">The optional specific type of packet to find so inspection will be performed for you</param>
        /// <returns>The data which makes up the packet if found, otherwise null</returns>
        public byte[] ReadNext(DumpItemType? type = null)
        {            
          FindItem:
            DumpItem? item = ReadDumpItem();
            if (item.HasValue)
            {
                if (!type.HasValue || (type.HasValue && item.Value.ItemType == type))
                {
                    return item.Value.Packet;
                }
                else goto FindItem;
            }
            return null;
        }

        /// <summary>
        /// Reads the next packet from the dump which corresponds to the given time and type.
        /// </summary>
        /// <param name="fromBeginning">The TimeSpan from the beginning of the dump</param>
        /// <param name="type">The optional type of item to find</param>
        /// <returns>The data which makes up the packet at the location in the dump</returns>
        public byte[] ReadNext(TimeSpan fromBeginning, DumpItemType? type = null)
        {
            DumpItem? result = InternalReadNext(fromBeginning, type);
            if (result.HasValue) return result.Value.Packet;
            else return null;
        }

        /// <summary>
        /// Skips the given amount of items in the dump from the current position (forwards or backwards)
        /// </summary>
        /// <param name="count">The amount of items to skip</param>
        public void Skip(int count)
        {
            //Going forwards must parse
            if (count > 0)
            {
                while (ReadDumpItem().HasValue && count > 0)
                {
                    --count;
                }
            }
            else//We already know the offsets
            {
                do
                    m_Reader.BaseStream.Seek(m_Offsets[count--], System.IO.SeekOrigin.Begin);
                while (count > 0);
            }
        }

        /// <summary>
        /// Reads a DumpItem from the beginning of the file with respect to the given options
        /// </summary>
        /// <param name="fromBeginning">The amount of time which must pass in the file before a return will be possible</param>
        /// <param name="type"></param>
        /// <returns></returns>
        internal DumpItem? InternalReadNext(TimeSpan fromBeginning, DumpItemType? type = null)
        {
            if (fromBeginning < TimeSpan.Zero) throw new ArgumentOutOfRangeException("timeOffset cannot be less than the start of the file which is defined in the header.");
            DumpItem? current = null;
            m_Reader.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);
            while ((current = ReadDumpItem()).HasValue)
            {
                fromBeginning -= current.Value.TimeOffset;
                if (type.HasValue && current.Value.ItemType != type) continue;
                if (fromBeginning.TotalMilliseconds <= 0) break;
            }
            return current;
        }

        /// <summary>
        /// Closes the underlying stream if was not to leave open
        /// </summary>
        public void Close()
        {
            if (!m_leaveOpen)
            {
                m_Reader.Dispose();                
            }
        }

        /// <summary>
        /// Calls Close
        /// </summary>
        public void Dispose()
        {
            Close(); 
        }

        /// <summary>
        /// Enumerates the packets found
        /// </summary>
        /// <returns>A yield around each Item returned</returns>
        public IEnumerator<byte[]> GetEnumerator()
        {
            m_Reader.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);
            DumpItem? result;
            while ((result = ReadDumpItem()).HasValue)
            {
                yield return result.Value.Packet;
            }
        }

        /// <summary>
        /// Enumerates the byte[]'s which contains packets in the dump file
        /// </summary>
        /// <returns></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// Writes a rtpdump compatible file to a System.IO.Stream
    /// http://www.cs.columbia.edu/irt/software/rtptools/
    /// </summary>
    public sealed class DumpWriter : IDisposable
    {
        //The format the DumpWriter is writing in
        DumpFormat m_Format;

        //The header of the Dump being written
        DumpHeader m_Header;

        System.IO.BinaryWriter m_Writer;

        //Indicates if the headers were written
        bool wroteHeader, m_leaveOpen;

        int itemsWritten = 0;

        /// <summary>
        /// The position in the stream
        /// </summary>
        public long Position { get { return m_Writer.BaseStream.Position; } }

        /// <summary>
        /// The length of the stream
        /// </summary>
        public long Length { get { return m_Writer.BaseStream.Length; } }

        /// <summary>
        /// The count of items written to the dump
        /// </summary>
        public int Count { get { return itemsWritten; } }

        /// <summary>
        /// Creates a DumpWriter which writes rtpdump comptaible files.
        /// </summary>
        /// <param name="stream">The stream to write to</param>
        /// <param name="format">The format to write in</param>
        /// <param name="source">The source where packets came from in this dump</param>
        /// <param name="utcStart">The optional start of the file recording (used in the header)</param>
        /// <param name="modify">Indicates if the file should be modified or created</param>
        /// <param name="leaveOpen">Indicates if the stream should be left open after calling Close or Dipose</param>
        public DumpWriter(System.IO.Stream stream, DumpFormat format, System.Net.IPEndPoint source, DateTime? utcStart, bool modify, bool leaveOpen = false)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (source == null) throw new ArgumentNullException("source");
            m_leaveOpen = leaveOpen;
            m_Format = format;
            if (!modify) // New file
            {
                m_Header = new DumpHeader()
                {
                    SourceEndPoint = source,
                    UtcStart = utcStart.HasValue ? utcStart.Value.ToUniversalTime() : DateTime.UtcNow
                };
                wroteHeader = false;

                //Create the writer
                m_Writer = new System.IO.BinaryWriter(stream);
            }
            else//Modifying
            {
                try
                {
                    //Header already written in modifying
                    //Need to read the header and advance the stream to the end
                    using (DumpReader reader = new DumpReader(stream, wroteHeader = true))
                    {
                        if (reader.m_Format != m_Format) throw new Exception("Format does not match, Expected: " + m_Format + " Found: " + reader.m_Format);
                        m_Header = reader.m_Header.Value;
                        reader.ReadToEnd();
                    }

                    //Create the writer
                    m_Writer = new System.IO.BinaryWriter(stream);
                }
                catch(Exception ex)
                {
                    throw new Exception("Cannot modify existing invalid file. File does not contain required rtpdump Header.", ex);
                }
            }
            
        }

        /// <summary>
        /// Creates a DumpWrite which writes rtpdump compatible files.
        /// Throws and exception if the file given already exists and overWrite is not specified otherwise a new file will be created
        /// </summary>
        /// <param name="filePath">The path to store the created the dump or the location of an existing rtpdump file</param>
        /// <param name="format">The to write the dump in. An exceptio will be thrown if overwrite is false and format does match the existing file's format</param>
        /// <param name="source">The IPEndPoint from which RtpPackets were recieved</param>
        /// <param name="utcStart">The optional time the file started recording</param>
        /// <param name="overWrite">Indicates the file should be overwritten</param>
        public DumpWriter(string filePath, DumpFormat format, System.Net.IPEndPoint source, DateTime? utcStart, bool overWrite) : this(new System.IO.FileStream(filePath, overWrite ? System.IO.FileMode.Create : System.IO.FileMode.CreateNew), format, source, utcStart, overWrite) { }

        /// <summary>
        /// Writes the rtpdump file header
        /// </summary>
        internal void WriteFileHeader()
        {
            if (wroteHeader) return;
            m_Header.Write(m_Writer, m_Format);
            wroteHeader = true;
        }

        /// <summary>
        /// Writes the given packet to the stream at the current position
        /// </summary>
        /// <param name="packet">The time</param>
        /// <param name="timeOffset">The optional time the packet was recieved relative to the beginning of the file. If the packet has a Created time that will be used otherwise DateTime.UtcNow.</param>
        public void WriteRtpPacket(RtpPacket packet, TimeSpan? timeOffset = null) { if (packet.Created.HasValue) timeOffset = packet.Created.Value - m_Header.UtcStart; if (timeOffset < TimeSpan.Zero) throw new ArgumentOutOfRangeException("timeOffset cannot be less than the start of the file which is defined in the header. "); WriteDumpItem(new DumpItem(packet, timeOffset ?? (m_Header.UtcStart - DateTime.UtcNow))); }

        /// <summary>
        /// Writes a RtcpPacket to the dump
        /// </summary>
        /// <param name="packet">The packet to write</param>
        /// <param name="timeOffset">The optional time the packet was recieved relative to the beginning of the file. If the packet has a Created time that will be used otherwise DateTime.UtcNow.</param>
        public void WriteRtcpPacket(Rtcp.RtcpPacket packet, TimeSpan? timeOffset = null) { if (packet.Created.HasValue) timeOffset = packet.Created.Value- m_Header.UtcStart ; if (timeOffset < TimeSpan.Zero) throw new ArgumentOutOfRangeException("timeOffset cannot be less than the start of the file which is defined in the header. "); WriteDumpItem(new DumpItem(packet, timeOffset ?? (m_Header.UtcStart - DateTime.UtcNow))); }

        /// <summary>
        /// Writes a DumpItem to the underlying stream
        /// </summary>
        /// <param name="item">The DumpItem to write</param>
        internal void WriteDumpItem(DumpItem item)
        {
            if (!wroteHeader) WriteFileHeader();
            item.Write(m_Writer, m_Format, m_Header);
            ++itemsWritten;
        }

        /// <summary>
        /// Closes and Disposes the underlying stream if indicated to do so when constructing the DumpWriter
        /// </summary>
        public void Close()
        {
            if (!m_leaveOpen)
            {
                m_Writer.Dispose();                
            }
        }

        /// <summary>
        /// Calls Close
        /// </summary>
        public void Dispose()
        {
            Close();
            m_Writer = null;
        }
    }

    #endregion
}
