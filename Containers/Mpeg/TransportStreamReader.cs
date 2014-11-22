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
        public enum ScramblingControl : byte
        {
            NotScambled = 0,
            Reserved = 1,
            ScrambledWithEvenKey = 2,
            ScambledWithOddKey = 3,
        }

        public enum PacketIdentifier : short
        {
            ///Program Association Table (PAT) contains a directory listing of all Program Map Tables
            ProgramAssociationTable = 0x0000, // TID 0x00
            /// <summary>
            /// Conditional Access Table (CAT) contains a directory listing of
            /// all ITU-T Rec. H.222 entitlement management message streams
            /// used by Program Map Tables
            /// </summary>
            ConditionalAccessTable = 0x0001, // TID 0x01
            //Transport Stream Description Table contains descriptors relating to the overall transport stream
            DescriptionTable = 0x0002, // TID 0x03
            ControlInformationTable = 0x0003, //IPMP
            // 0x04 to 0x0F RESERVED
            NetworkInformationTable = 0x0010, // TID 0x40 (current) & 0x41 (other)
            ServiceDescriptionTable = 0x0011, // TID 0x42,0x46 (ServiceDescription) & 0x4A (BouquetAssociation)
            EventInformationTable = 0x0012,
            RunningStatusTable = 0x0013,
            TimeTables = 0x0014, // TID 0x70 (TimeAndDate) & 0x73 (TimeOffset)
            NetworkSynchronization = 0x0015,
            ResolutionNotificationTable = 0x0016,
            // 0x0017 to 0x001B RESERVED
            InbandSignalling = 0x001C,
            Measurement = 0x001D,
            DiscontinuityInformation = 0x001E,
            SectionIformation = 0x001F,
            // USER DEFINED
            ATSCMetaData = 0x1FB,
            NullPacket = 0x1FFF
        }

        public enum TableIdentifier : byte
        {
            ProgramAssociation = 0x00,
            ConditionalAccess = 0x01,
            ProgramMap = 0x02,
            Description = 0x03,
            SceneDescriptor = 0x04,
            ObjectDescriptor = 0x05,
            // 0x06 to 0x37 RESERVED
            // 0x38 to 0x39 ISO/IEC 13818-6 reserved
            NetworkInformation = 0x40,
            OtherNetworkInformation = 0x41,
            ServiceDescription = 0x42,
            // 0x43 to 0x45 RESERVED 
            ServiceDescriptionOther = 0x46,
            // 0x47 to 0x49 RESERVED
            BouquetAssociation = 0x4A,
            // 0x4B to 0x4D RESERVED
            EventInformation = 0x4E,
            EventInformationOther = 0x4F,
            EventInformationSchedule0 = 0x50,
            EventInformationSchedule1 = 0x51,
            EventInformationSchedule2 = 0x52,
            EventInformationSchedule3 = 0x53,
            EventInformationSchedule4 = 0x54,
            EventInformationSchedule5 = 0x55,
            EventInformationSchedule6 = 0x56,
            EventInformationSchedule7 = 0x57,
            EventInformationSchedule8 = 0x58,
            EventInformationSchedule9 = 0x59,
            EventInformationScheduleA = 0x5A,
            EventInformationScheduleB = 0x5B,
            EventInformationScheduleC = 0x5C,
            EventInformationScheduleD = 0x5D,
            EventInformationScheduleE = 0x5E,
            EventInformationScheduleF = 0x5F,
            EventInformationScheduleOther0 = 0x60,
            EventInformationScheduleOther1 = 0x61,
            EventInformationScheduleOther2 = 0x62,
            EventInformationScheduleOther3 = 0x63,
            EventInformationScheduleOther4 = 0x64,
            EventInformationScheduleOther5 = 0x65,
            EventInformationScheduleOther6 = 0x66,
            EventInformationScheduleOther7 = 0x67,
            EventInformationScheduleOther8 = 0x68,
            EventInformationScheduleOther9 = 0x69,
            EventInformationScheduleOtherA = 0x6A,
            EventInformationScheduleOtherB = 0x6B,
            EventInformationScheduleOtherC = 0x6C,
            EventInformationScheduleOtherD = 0x6D,
            EventInformationScheduleOtherE = 0x6E,
            EventInformationScheduleOtherF = 0x6F,
            TimeAndDate = 0x70,
            RunningStatus = 0x71,
            Stuffing = 0x72,
            TimeOffset = 0x73,
            ApplicationInformation = 0x74,
            Container = 0x75,
            RelatedContent = 0x76,
            ContentIdentification = 0x77,
            FowardErrorCorrection = 0x78,
            ResolutionNotification = 0x79,
            InterburstForwardErrorCorrection = 0x7A,
            // 0x7B to 0x7D RESERVED
            DiscontinuityInformation = 0x7E,
            SectionInformation = 0x7F,
            // 0x80 to 0xFE USER DEFINED
            // 0xFF RESERVED
        }

        public static bool IsReserved(TableIdentifier identifier)
        {
            byte tid = (byte)identifier;
            return tid >= 0x06 && tid <= 0x37
                || tid >= 0x38 && tid <= 0x39 //ISO/IEC 13818-6 reserved
                || tid >= 0x43 && tid <= 0x45
                || tid >= 0x47 && tid <= 0x49
                || tid >= 0x4B && tid <= 0x4D 
                || tid >= 0x7B && tid <= 0x7D 
                || tid == byte.MaxValue;
        }

        public static bool IsUserDefined(TableIdentifier identifier)
        {
            byte tid = (byte)identifier;
            return tid >= 0x80 && tid <= 0xFE;
        }

        public static bool IsReserved(PacketIdentifier identifier)
        {
            ushort sid = (ushort)identifier;
            return sid >= 0x04 && sid <= 0x0F || sid >= 0x0017 && sid <= 0x001B;
        }

        public static bool IsDVBMetaData(PacketIdentifier identifier)
        {
            ushort sid = (ushort)identifier;
            return sid >= 16 || sid <= Common.Binary.FiveBitMaxValue;
        }

        public static bool IsUserDefined(PacketIdentifier identifier)
        {
            ushort sid = (ushort)identifier; //Encompass ASTCMetaData?
            return sid >= 0x20 && sid <= 0x1FFA || sid >= 0x1FFC && sid <= 0x1FFE;
        }

        public const byte SyncByte = 0x47;

        internal const byte ScramblingControlMask = 0xC0, ContinuityCounterMask = Common.Binary.FourBitMaxValue,
            PayloadMask = 16, PriorityMask = 32, AdaptationFieldMask = PriorityMask, PayloadStartUnitMask = 64, ErrorMask = 128;

        [Flags]
        public enum AdaptationFieldFlags : byte
        {
            None = 0,
            Extension = 1,
            PrivateData = 2,
            SpliceCountdown = 4,
            OriginalProgramClockReference = 8,
            ProgramClockReference = PayloadMask, //16
            ElementaryStreamPriority = PriorityMask,//32
            RandomAccess = PayloadStartUnitMask, //64
            TransportError = ErrorMask//128
        }

        internal const int PacketIdentifierMask = 0x1FFF;

        public const int IdentifierSize = 4, LengthSize = 0, UnitLength = 188;

        public static string ToTextualConvention(byte[] identifier)
        {
            PacketIdentifier result = GetPacketIdentifier(identifier);
            if (IsReserved(result)) return "Reserved";
            if (IsDVBMetaData(result)) return "DVBMetaData";
            if (result != PacketIdentifier.ATSCMetaData && IsUserDefined(result)) return "UserDefined";
            return result.ToString();
        }

        #region TsUnit

        public static bool HasTransportErrorIndicator(Container.Node tsUnit) { return (tsUnit.Identifier[1] & ErrorMask) > 0; }

        public static bool HasPayloadUnitStartIndicator(Container.Node tsUnit) { return (tsUnit.Identifier[1] & PayloadStartUnitMask) > 0; }

        public static bool HasTransportPriority(Container.Node tsUnit) { return (tsUnit.Identifier[1] & PriorityMask) > 0; }

        public static PacketIdentifier GetPacketIdentifier(byte[] identifier) { return (PacketIdentifier)(Common.Binary.ReadU16(identifier, 1, BitConverter.IsLittleEndian) & PacketIdentifierMask); }

        public static bool HasAdaptationField(Container.Node tsUnit) { return (tsUnit.Identifier[3] & AdaptationFieldMask) != 0; }

        public static ScramblingControl GetScramblingControl(Container.Node tsUnit) { return (ScramblingControl)((tsUnit.Identifier[3] & ScramblingControlMask) >> 6); }        

        public static int GetContinuityCounter(Container.Node tsUnit) { return tsUnit.Identifier[3] & ContinuityCounterMask; }

        #endregion

        #region AdaptationField

        public static AdaptationFieldFlags GetAdaptationFieldFlags(Media.Container.Node tsUnit) { return (AdaptationFieldFlags)GetAdaptationFieldData(tsUnit)[0]; }

        //Todo Must come from tsUnit.Data

        public static byte[] GetAdaptationFieldData(Container.Node tsUnit)
        {
            if (tsUnit == null) throw new ArgumentNullException("tsUnit");

            //Size is known but includes length byte
            int size = tsUnit.LengthSize - 1;

            if (size < 1 || !HasAdaptationField(tsUnit)) return Utility.Empty;

            long position = tsUnit.Master.BaseStream.Position, neededPosition = tsUnit.Offset + IdentifierSize + 1;

            tsUnit.Master.BaseStream.Seek(neededPosition, System.IO.SeekOrigin.Begin);

            byte[] data = new byte[size];

            tsUnit.Master.BaseStream.Read(data, 0, size);
            
            tsUnit.Master.BaseStream.Seek(position, System.IO.SeekOrigin.Begin);

            return data;
        }

        public static TimeSpan? ProgramClockReference(byte[] adaptationField)
        {
            int offset = 0;
            AdaptationFieldFlags adaptationFlags = (AdaptationFieldFlags)adaptationField[offset++];
            return (adaptationFlags.HasFlag(AdaptationFieldFlags.ProgramClockReference)) ? (TimeSpan?)ProgramClockReferenceToTimeSpan(adaptationField, offset) : null;
        }
            

        public static TimeSpan? OriginalProgramClockReference(byte[] adaptationField)
        {
            int offset = 0;
            AdaptationFieldFlags adaptationFlags = (AdaptationFieldFlags)adaptationField[offset++];
            if (adaptationFlags.HasFlag(AdaptationFieldFlags.ProgramClockReference)) offset += ProgramClockReferenceSize;
            return (adaptationFlags.HasFlag(AdaptationFieldFlags.OriginalProgramClockReference)) ? (TimeSpan?)ProgramClockReferenceToTimeSpan(adaptationField, offset) : null;
        }

        public static int SpliceCountdown(byte[] adaptationField)
        {
            int offset = 0;
            AdaptationFieldFlags adaptationFlags = (AdaptationFieldFlags)adaptationField[offset++];
            if(adaptationFlags.HasFlag(AdaptationFieldFlags.ProgramClockReference)) offset += ProgramClockReferenceSize;
            if(adaptationFlags.HasFlag(AdaptationFieldFlags.OriginalProgramClockReference)) offset += ProgramClockReferenceSize;
            return adaptationFlags.HasFlag(AdaptationFieldFlags.SpliceCountdown) ? adaptationField[offset] : -1;
        }

        const int ProgramClockReferenceSize = 6;

        public static TimeSpan ProgramClockReferenceToTimeSpan(byte[] pcr, int offset)
        {
            if (pcr == null) throw new ArgumentNullException("pcr");
            
            int length = pcr.Length;

            if (offset > length) throw new IndexOutOfRangeException("offset must point to a location within pcr");

            if (length - offset < ProgramClockReferenceSize) throw new InvalidOperationException("pcr must contain at least " + ProgramClockReferenceSize + " bytes.");

            long pcrBase = pcr[offset++] << 25 | pcr[offset++] << 17 | pcr[offset++] << 9 | pcr[offset++] << 1 | (pcr[offset] & 0x80) >> 7;

            long pcrExtension = (pcr[offset++] & 0x01) << 8 | pcr[offset++];

            long pcrBaseTicks = (pcrBase * 1111111) / 10000; // Convert 90khz to Ticks (multiply by 111.1111)

            long pcrExtensionTicks = ((pcrExtension * 10) / 27) / 10; // Convert 27Mhz to Ticks (divide by 2.7)

            long pcrTicks = pcrBaseTicks + pcrExtensionTicks;

            return new TimeSpan(pcrTicks);
        }

        public static bool HasPrivateData(byte[] adaptationField) { return ((AdaptationFieldFlags)adaptationField[0]).HasFlag(AdaptationFieldFlags.PrivateData); }

        public static bool HasExtension(byte[] adaptationField) { return ((AdaptationFieldFlags)adaptationField[0]).HasFlag(AdaptationFieldFlags.Extension); }

        //GetAdaptationFieldPrivateData

        #endregion

        #region AdaptationFieldExtension

        public static int AdaptationFieldExtensionLength(byte[] adaptationField)
        {
            if (adaptationField == null) throw new ArgumentNullException("adaptationField");
            return adaptationField[1];
        }

        //GetAdaptationFieldExtensionData

        public static bool HasLegalTimeWindow(byte[] adaptationFieldExtension)
        {
            if (adaptationFieldExtension == null) throw new ArgumentNullException("adaptationFieldExtension");
            return ((adaptationFieldExtension[2] & ErrorMask) != 0);
        }

        public static bool HasPiecewiseRate(byte[] adaptationFieldExtension)
        {
            if (adaptationFieldExtension == null) throw new ArgumentNullException("adaptationFieldExtension");
            return ((adaptationFieldExtension[2] & PayloadStartUnitMask) != 0);
        }

        public static bool HasSeamlessSplice(byte[] adaptationFieldExtension)
        {
            if (adaptationFieldExtension == null) throw new ArgumentNullException("adaptationFieldExtension");
            return ((adaptationFieldExtension[2] & PriorityMask) != 0);
        }

        public static bool HasAdaptationFieldStuffing(byte[] adaptationFieldExtension, out int length)
        {
            //Get the whole extension field's length
            int fieldLength = adaptationFieldExtension.Length;
            //Determine the size of the extensions
            length = AdaptationFieldExtensionLength(adaptationFieldExtension);
            //The size of the stuffing is equal to the length of the entire extension field - the length of the extension
            length = fieldLength - length;
            //Indicate if there was any stuffing
            return fieldLength < adaptationFieldExtension.Length;
        }

        #endregion

        public TransportStreamReader(string filename, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(filename, access) { }

        public TransportStreamReader(Uri source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

        public TransportStreamReader(System.IO.FileStream source, System.IO.FileAccess access = System.IO.FileAccess.Read) : base(source, access) { }

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

        public IEnumerable<Media.Container.Node> ReadUnits(long offset, long count, params PacketIdentifier[] names)
        {
            long position = Position;

            Position = offset;

            foreach (var tsUnit in this)
            {
                if (names == null || names.Count() == 0 || names.Contains(GetPacketIdentifier(tsUnit.Identifier)))
                {
                    yield return tsUnit;

                    count -= tsUnit.TotalSize; //Could use Constant

                    if (count <= 0) break;

                    continue;
                }
            }

            Position = position;

            yield break;
        }

        public Media.Container.Node ReadUnit(PacketIdentifier name, long offset = 0)
        {
            long positionStart = Position;

            Media.Container.Node result = ReadUnits(offset, Length - offset, name).FirstOrDefault();

            Position = positionStart;

            return result;
        }

        /// <summary>
        /// Reads a single 188 byte TransportUnit.
        /// </summary>
        /// <returns>The <see cref="Node"/> which represents the TransportUnit.</returns>
        public Container.Node ReadNext()
        {
            byte[] identifier = new byte[IdentifierSize];

            Read(identifier, 0, IdentifierSize);

            if (identifier[0] != SyncByte) throw new InvalidOperationException("Cannot Find Marker");

            //Should be part of Node Data to be more efficient
            //Length would always be 188.
            //Methods would change to read from the Data
            //No checking would be performed
            //int length = UnitLength - IdentifierSize;
            //return new Container.Node(this, identifier, LengthSize, Position, length, length <= Remaining);

            //The last byte determine if there is a variable length (adaptation) field
            byte last = identifier[3];

            //Determine the length of the packet and amount of bytes required to read the length
            int length = UnitLength - IdentifierSize, lengthSize = LengthSize;

            //Check for varible length field
            if ((last & AdaptationFieldMask) != 0)
            {
                //Determine its size by reading a byte
                lengthSize = (byte)((ReadByte()) & byte.MaxValue);
                
                //If it has any more data skip past it (obtained with read at DataOffset - LengthSize)
                if (lengthSize > 0) Skip(lengthSize);

                //Do include the size byte in the length size
                lengthSize++;

                //Don't include the variable length data in the size
                length -= lengthSize;
            }

            //Check for absence of PayloadMask which indicates 0 length
            if ((last & PayloadMask) == 0) length = 0;

            //Return the Node
            return new Container.Node(this, identifier, lengthSize, Position, length, length <= Remaining);
        }

        public override IEnumerator<Container.Node> GetEnumerator()
        {
            while (Remaining >= UnitLength)
            {
                Container.Node next = ReadNext();

                if (next == null) yield break;

                yield return next;

                Skip(next.DataSize);
            }
        }

        public override string ToTextualConvention(Container.Node node)
        {
            if (node.Master.Equals(this)) return TransportStreamReader.ToTextualConvention(node.Identifier);
            return base.ToTextualConvention(node);
        }

        //Maps from Program to PacketIdentifer?

        //void ProgramAssociationTable

        //void ParseProgramStreamMaps

        public override Container.Node Root
        {
            get { return ReadUnit(PacketIdentifier.ProgramAssociationTable); }
        }

        //ProgramAssociationTable to get stream ProgramStreams Id's
        //Then first with ProgramStreamMap?
        public override Container.Node TableOfContents
        {
            get { throw new NotImplementedException(); }
        }

        //Read all with Start of Payload then determine from PacketIdentifers
        public override IEnumerable<Container.Track> GetTracks()
        {
            throw new NotImplementedException();
        }

        public override byte[] GetSample(Container.Track track, out TimeSpan duration)
        {
            throw new NotImplementedException();
        }

        ////GetPacketizedElementaryStreams?
    }
}
