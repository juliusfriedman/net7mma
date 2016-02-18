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

using System;
using System.Collections.Generic;
using System.Linq;

namespace Media.Containers.Mpeg
{
    /// <summary>
    /// Represents the logic necessary to read Mpeg Transport Streams.
    /// Transport Stream is specified in MPEG-2 Part 1, Systems (formally known as ISO/IEC standard 13818-1 or ITU-T Rec. H.222.0).[3]
    /// Transport stream specifies a container format encapsulating packetized elementary streams, with error correction and stream synchronization features for maintaining transmission integrity when the signal is degraded.
    /// </summary>
    public class TransportStreamReader : Media.Container.MediaFileStream, Media.Container.IMediaContainer
    {
        #region References

        //https://github.com/antiochus/tsremux

        //http://basemedia.Codeplex.com

        //http://iknowu.duckdns.org/files/public/MP4Maker/MP4Maker.htm

        #endregion

        #region Constants

        public const int IdentiferSize = 4, LengthSize = 0, StandardUnitLength = 188, PayloadLength = StandardUnitLength - IdentiferSize, MaximumUnitOverhead = IdentiferSize * 5;

        #endregion

        #region Statics

        public static string ToTextualConvention(TransportStreamReader reader, byte[] identifier)
        {
            TransportStreamUnit.PacketIdentifier result = TransportStreamUnit.GetPacketIdentifier(identifier, reader.UnitOverhead);
            if (TransportStreamUnit.IsReserved(result)) return "Reserved";
            if (TransportStreamUnit.IsDVBMetaData(result)) return "DVBMetaData";
            if (TransportStreamUnit.IsUserDefined(result)) return "UserDefined";
            return result.ToString();
        }

        #endregion

        #region Node Methods

        #region TsUnit

        public static bool HasTransportErrorIndicator(TransportStreamReader reader, Container.Node tsUnit) { return TransportStreamUnit.HasTransportErrorIndicator(tsUnit.Identifier, reader.UnitOverhead); }

        public static bool HasPayloadUnitStartIndicator(TransportStreamReader reader, Container.Node tsUnit) { return TransportStreamUnit.HasPayloadUnitStartIndicator(tsUnit.Identifier, reader.UnitOverhead); }

        public static bool HasTransportPriority(TransportStreamReader reader, Container.Node tsUnit) { return TransportStreamUnit.HasTransportPriority(tsUnit.Identifier, reader.UnitOverhead); }

        public static TransportStreamUnit.PacketIdentifier GetPacketIdentifier(TransportStreamReader reader, byte[] identifier) { return TransportStreamUnit.GetPacketIdentifier(identifier, reader.UnitOverhead); }

        public static TransportStreamUnit.AdaptationFieldControl GetAdaptationFieldControl(TransportStreamReader reader, Container.Node tsUnit) { return TransportStreamUnit.GetAdaptationFieldControl(tsUnit.Identifier, reader.UnitOverhead); }

        public static bool HasAdaptationField(TransportStreamReader reader, Container.Node tsUnit) { return TransportStreamUnit.GetAdaptationFieldControl(tsUnit.Identifier, reader.UnitOverhead) > TransportStreamUnit.AdaptationFieldControl.None; }

        public static bool HasPayload(TransportStreamReader reader, Container.Node tsUnit) { return GetAdaptationFieldControl(reader, tsUnit).HasFlag(TransportStreamUnit.AdaptationFieldControl.AdaptationFieldAndPayload); }

        public static TransportStreamUnit.ScramblingControl GetScramblingControl(TransportStreamReader reader, Container.Node tsUnit) { return TransportStreamUnit.GetScramblingControl(tsUnit.Identifier, reader.UnitOverhead); }

        public static int GetContinuityCounter(TransportStreamReader reader, Container.Node tsUnit) { return TransportStreamUnit.GetContinuityCounter(tsUnit.Identifier, reader.UnitOverhead); }

        #endregion

        #region AdaptationField

        public static TransportStreamUnit.AdaptationField.AdaptationFieldFlags GetAdaptationFieldFlags(TransportStreamReader reader, Media.Container.Node tsUnit) { return (TransportStreamUnit.AdaptationField.AdaptationFieldFlags)TransportStreamUnit.AdaptationField.GetAdaptationFieldData(tsUnit.Identifier, reader.UnitOverhead, tsUnit.Data, 0).FirstOrDefault(); }

        public static byte[] GetAdaptationFieldData(TransportStreamReader reader, Container.Node tsUnit)
        {
            return TransportStreamUnit.AdaptationField.GetAdaptationFieldData(tsUnit.Identifier, reader.UnitOverhead, tsUnit.Data, 0);
        }

        #endregion

        #endregion

        public TransportStreamReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public TransportStreamReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        public TransportStreamReader(System.IO.FileStream source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        //uses a temp file for now :(
        public TransportStreamReader(Uri uri, System.IO.Stream source, int bufferSize = 8192) : base(uri, source, null, bufferSize, true) { } 

        /// <summary>
        /// Used in variations of the Mpeg Transport Stream format which require additional data such as ATSC or M2TS.
        /// The additional bytes are then stored in the <see cref="Node.Identifier"/>.
        /// </summary>
        public virtual int UnitOverhead { get; protected set; }

        /// <summary>
        /// Gets a value which indicates if the units are standard 188 byte length
        /// </summary>
        public bool IsStandardTransportStream { get { return UnitOverhead == 0; } }

        /// <summary>
        /// Gets the size in bytes of each TransportStreamUnit including any header which preceeds the TransportStreamUnit.
        /// </summary>
        public int TransportUnitSize { get { return StandardUnitLength + UnitOverhead; } }

        ///// <summary>
        ///// Always seeks in different of the offset given and the modulo ofr the TransportStream PacketLength (188)
        ///// </summary>
        ///// <param name="offset"></param>
        ///// <param name="origin"></param>
        ///// <returns></returns>
        //public override long Seek(long offset, System.IO.SeekOrigin origin) { return base.Seek(offset - (offset % PacketLength), origin); }

        ///// <summary>
        ///// Skips in terms of packets
        ///// </summary>
        ///// <param name="count"></param>
        ///// <returns></returns>
        //public override long Skip(long count) { return base.Skip(count); }

        public IEnumerable<Media.Container.Node> ReadUnits(long offset, long count, params TransportStreamUnit.PacketIdentifier[] names)
        {
            long position = Position;

            Position = offset;

            foreach (var tsUnit in this)
            {
                if (names == null || names.Count() == 0 || names.Contains(GetPacketIdentifier(this, tsUnit.Identifier)))
                {
                    yield return tsUnit;

                    count -= tsUnit.TotalSize;

                    if (count <= 0) break;

                    continue;
                }
            }

            Position = position;

            yield break;
        }

        public Media.Container.Node ReadUnit(TransportStreamUnit.PacketIdentifier name, long offset = 0)
        {
            long positionStart = Position;

            Media.Container.Node result = ReadUnits(offset, Length - offset, name).FirstOrDefault();

            Position = positionStart;

            return result;
        }

        /// <summary>
        /// Reads a single 188 byte TransportUnit + UnitOverhead.
        /// If Position is at 0 AND UnitOverhead is also 0 and Syncronization cannot be found then it will attempted to be found within the MaximumIdentiferSize.
        /// </summary>
        /// <returns>The <see cref="Node"/> which represents the TransportUnit.</returns>
        public Container.Node ReadNext()
        {
            //Read the identifier which will consist of any known UnitOverhead and the DefaultIdentiferSize.

            int identifierSize = IdentiferSize + UnitOverhead;

            byte[] identifier = new byte[identifierSize];

            Read(identifier, 0, identifierSize);

            //Check for sync and then if the first unit was read and if not determine the UnitOverhead
            if (identifier[UnitOverhead] != TransportStreamUnit.SyncByte)
            {
                //Find the size with sanity (Only on the first packet)
                if (Position < MaximumUnitOverhead)
                {
                    //size is already IdentiferSize
                    int size = IdentiferSize;
                    
                    //Might need a few checks for Subtitle Format Streams because of P, p, g byte.
                    while (UnitOverhead < MaximumUnitOverhead)
                    {
                        //Increase UnitOverhead by 4 bytes (DefaultIdentifierSize)
                        UnitOverhead += IdentiferSize;
                        
                        //Thew newSize is size + IdentiferSize
                        int newSize = (size + IdentiferSize);
                        Array.Resize(ref identifier, newSize);
                        
                        //Read the next bytes starting at the newly allocated space
                        Read(identifier, size, IdentiferSize);
                        
                        //Check for sync
                        if (identifier[UnitOverhead] == TransportStreamUnit.SyncByte) goto FoundSync;

                        //declare the new size and iterate again
                        size = newSize;
                    };
                }
                
                //Canot find sync
                throw new InvalidOperationException("Cannot Find Marker");
            }

            //We have sync
            FoundSync:

            //Return the Node
            return new Container.Node(this, identifier, LengthSize, Position, PayloadLength, PayloadLength <= Remaining);
        }

        /// <summary>
        /// Enumerates each unit found in the TransportStream.
        /// When a unit is found it is either added to the Streams or it is parsed to obtain information.
        /// </summary>
        /// <returns></returns>
        public override IEnumerator<Container.Node> GetEnumerator()
        {
            //Ensure a Unit remains
            while (Remaining >= TransportUnitSize)
            {
                //Read a unit
                Container.Node next = ReadNext();

                //No more units stops the enumeration
                if (next == null) yield break;

                //Return the unit for external handling
                yield return next;

                //Get the type of packet this is
                TransportStreamUnit.PacketIdentifier pid = GetPacketIdentifier(this, next.Identifier);                

                //Need to parse the packet to ensure that GetTracks can always be updated.
                switch (pid)
                {
                    case TransportStreamUnit.PacketIdentifier.ProgramAssociationTable:
                        {
                            /*
                             
                            The Program Association Table (PAT) is the entry point for the Program Specific Information (PSI) tables.  
                            
                            It is always carried in packets with PID (packet ID) = 0.  For each assigned program number, the PAT lists the PID for packets containing that program's PMT.  

                            The PMT lists all the PIDs for packets containing elements of a particular program (audio, video, aux data, and Program Clock Reference (PCR)).

                            The PAT also contains the PIDs for the NIT(s).  
                            
                            The NIT is an optional table that maps channel frequencies, transponder numbers, and other guide information for programs.
                             
                             */

                            ParseProgramAssociationTable(next);

                            goto default;
                        }
                    case TransportStreamUnit.PacketIdentifier.DescriptionTable: // TableIdentifier.ProgramMap
                        {
                            //Parse the table
                            ParseDescriptionTable(next);

                            goto default;
                        }
                    //case PacketIdentifier.Program
                    case TransportStreamUnit.PacketIdentifier.ConditionalAccessTable:
                    case TransportStreamUnit.PacketIdentifier.SelectionInformationTable:
                    case TransportStreamUnit.PacketIdentifier.NetworkInformationTable:
                        {
                            //Todo
                            goto default;
                        }
                    default:
                        {
                            //Include the PId with the StreamId
                            m_ProgramIds.Add(pid);
                            break;
                        }
                }

                //Done with the unit
                Skip(next.DataSize);
            }
        }

        HashSet<TransportStreamUnit.PacketIdentifier> m_ProgramIds = new HashSet<TransportStreamUnit.PacketIdentifier>();

        public override string ToTextualConvention(Container.Node node)
        {
            if (node.Master.Equals(this)) return TransportStreamReader.ToTextualConvention(this, node.Identifier);
            return base.ToTextualConvention(node);
        }

        System.Collections.Concurrent.ConcurrentDictionary<ushort, TransportStreamUnit.PacketIdentifier> m_ProgramAssociations = new System.Collections.Concurrent.ConcurrentDictionary<ushort, TransportStreamUnit.PacketIdentifier>();

        static internal int ReadPointerField(Container.Node node, int offset = 0)
        {
            int pointer = node.Data[offset++];

            return pointer > 0 ? offset += pointer * Common.Binary.BitsPerByte : offset;
        }

        //Static and take reader?
        //Maps from Program to PacketIdentifer?
        internal protected virtual void ParseProgramAssociationTable(Container.Node node)
        {

            int offset = ReadPointerField(node);

            //Get the table id
            TransportStreamUnit.TableIdentifier tableId = (TransportStreamUnit.TableIdentifier)node.Data[offset++];

            if (tableId != TransportStreamUnit.TableIdentifier.ProgramAssociation) return;

            /*
                Program Association Table (PAT) section syntax
                syntax	bit index	# of bits	mnemonic
                table_id	0	8	uimsbf
                section_syntax_indicator	8	1	bslbf
                '0'	9	1	bslbf
                reserved	10	2	bslbf
                section_length	12	12	uimsbf
                transport_stream_id	24	16	uimsbf
                reserved	40	2	bslbf
                version_number	42	5	uimsbf
                current_next_indicator	47	1	bslbf
                section_number	48	8	bslbf
                last_section_number	56	8	bslbf
                for i = 0 to N
                  program_number	56 + (i * 4)	16	uimsbf
                  reserved	72 + (i * 4)	3	bslbf
                  if program_number = 0
                    network_PID	75 + (i * 4)	13	uimsbf
                  else
                    program_map_pid	75 + (i * 4)	13	uimsbf
                  end if
                next
                CRC_32	88 + (i * 4)	32	rpchof
                Table section legend
            */


            //section syntax indicator, 0 bit, and 2 reserved bits.

            //Section Length The number of bytes that follow for the syntax section (with CRC value) and/or table data. These bytes must not exceed a value of 1021.
            // section_length field is a 12-bit field that gives the length of the table section beyond this field
            //Since it is carried starting at bit index 12 in the section (the second and third bytes), the actual size of the table section is section_length + 3.
            ushort sectionLength = (ushort)(Common.Binary.ReadU16(node.Data, ref offset, BitConverter.IsLittleEndian) & 0x0FFF);

            //transport_stream_id	24	16	uimsbf
            ushort transportStreamId = (ushort)(Common.Binary.ReadU16(node.Data, ref offset, BitConverter.IsLittleEndian) & 0x0FFF);

            //Skip reserved, version number and current/next indicator.
            ++offset;

            //Skip section number
            ++offset;

            //Skip last section number
            ++offset;

            //Determine where to end (don't count the crc)
            int end = (sectionLength + 3) - 4;

            //4 bytes per ProgramInfo in node.Data
            while (offset < end)
            {
                //2 Bytes ProgramNumber
                ushort programNumber = Common.Binary.ReadU16(node.Data, offset, BitConverter.IsLittleEndian);

                //3 bits reserved

                //2 Bytes ProgramID
                TransportStreamUnit.PacketIdentifier pid = TransportStreamUnit.GetPacketIdentifier(node.Data, offset + 2);

                //Add or update the entry
                m_ProgramAssociations.AddOrUpdate(programNumber, pid, (id, old) => pid);

                //Move the offset
                offset += 4;
            }

            //CRC
        }

        System.Collections.Concurrent.ConcurrentDictionary<byte, Tuple<TransportStreamUnit.PacketIdentifier, ushort>> m_ProgramDescriptions = new System.Collections.Concurrent.ConcurrentDictionary<byte, Tuple<TransportStreamUnit.PacketIdentifier, ushort>>();

        internal protected virtual void ParseDescriptionTable(Container.Node node)
        {
            int offset = ReadPointerField(node);

            //Get the table id
            TransportStreamUnit.TableIdentifier tableId = (TransportStreamUnit.TableIdentifier)node.Data[offset++];

            if (tableId != TransportStreamUnit.TableIdentifier.ProgramAssociation) return;

            TransportStreamUnit.PacketIdentifier pcrPid = (TransportStreamUnit.PacketIdentifier)(Common.Binary.ReadU16(node.Data, offset, BitConverter.IsLittleEndian) & TransportStreamUnit.PacketIdentifierMask);

            int infoLength = Common.Binary.ReadU16(node.Data, ref offset, BitConverter.IsLittleEndian) & 0x0FFF;

            //Determine where to end (don't count the crc)
            int end = (infoLength + 3) - 4;

            while (offset < end)
            {

                byte esType = node.Data[offset++];
                
                //Could store two bytes and have methods to extract with masks.

                TransportStreamUnit.PacketIdentifier esPid = (TransportStreamUnit.PacketIdentifier)(Common.Binary.ReadU16(node.Data, offset, BitConverter.IsLittleEndian) & TransportStreamUnit.PacketIdentifierMask);

                ushort descLength = (ushort)(Common.Binary.ReadU16(node.Data, offset, BitConverter.IsLittleEndian) & 0x0FFF);

                var entry = new Tuple<TransportStreamUnit.PacketIdentifier, ushort>(esPid, descLength);

                m_ProgramDescriptions.AddOrUpdate(esType, entry, (a, old) => entry);

                offset += 2;

            }

            //CRC
        }

        public override Container.Node Root { get { return ReadUnit(TransportStreamUnit.PacketIdentifier.ProgramAssociationTable); } }

        //ProgramAssociationTable to get stream ProgramStreams Id's
        //Then first with ProgramStreamMap?
        public override Container.Node TableOfContents
        {
            get { throw new NotImplementedException(); }
        }

        public override IEnumerable<Container.Track> GetTracks()
        {
            //For each packet a PID and StreamId was extracted.
            foreach (var pid in m_ProgramIds)
            {

                Sdp.MediaType mediaType = Sdp.MediaType.unknown;

                byte streamId = (byte)pid;

                if (Mpeg.StreamTypes.IsMpeg1or2AudioStream(streamId)) mediaType = Sdp.MediaType.audio;
                else if (Mpeg.StreamTypes.IsMpeg1or2VideoStream(streamId)) mediaType = Sdp.MediaType.video;
                else continue; //Don't show unknown streams for now. (Could filter by Null only but then tables would be included, could also skip tables with a IsTable function...)

                //Program name could be retieved if present

                //Todo track samples can be counted by adding logic in GetEnumerator..

                //Start and stop times should be defined already, just not stored, probably in a table.

                //Codec indication comes from the streamId or a table...

                yield return new Container.Track(null, string.Empty, streamId, FileInfo.CreationTimeUtc, FileInfo.LastWriteTimeUtc, 0, 0, 0, TimeSpan.Zero, TimeSpan.Zero, 0, mediaType, BitConverter.GetBytes(0));
            }
        }

        public override byte[] GetSample(Container.Track track, out TimeSpan duration)
        {
            throw new NotImplementedException();
        }

        //ConcurrentThesaraus<int, Node> Programs

        //Get Programs / ToProgramStream / Tune(int programId)?
    }
}
