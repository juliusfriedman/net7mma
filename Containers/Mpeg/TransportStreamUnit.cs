using System;

namespace Media.Containers.Mpeg
{
    //Could be IPacket and allow API similar to RtpPackets
    //Implement Set methods and then worry about that.
    public sealed class TransportStreamUnit
    {
        private TransportStreamUnit() { }

        public const byte SyncByte = 0x47; // (G) Sometimes [g, p or P] for BluRay Sup?

        internal const byte ScramblingControlMask = 0xC0, //192
            ContinuityCounterMask = Common.Binary.FourBitMaxValue,//15
            PayloadMask = 16, //ContinuityCounterMask + 1
            PriorityMask = 32, // PayloadMask * 2
            PayloadStartUnitMask = 64, // PriorityMask * 2
            ErrorMask = 128, // PayloadStartUnitMask * 2
            AdaptationFieldMask = PriorityMask; //Same as Priority, only provided for ease of reading.

        internal const short PacketIdentifierMask = 0x1FFF;

        #region Nested Types

        /// <summary>
        /// Describes the ScramblingControl
        /// </summary>
        public enum ScramblingControl : byte
        {
            NotScambled = 0,
            Reserved = 1,
            ScrambledWithEvenKey = 2,
            ScambledWithOddKey = 3,
        }

        /// <summary>
        /// Describes the AdaptationFieldControl
        /// </summary>
        public enum AdaptationFieldControl : byte
        {
            Reserved = 0,
            None = 1,
            AdaptationFieldOnly = 2,
            AdaptationFieldAndPayload = 3
        }

        /// <summary>
        /// Defines the known packet PacketIdentifier's
        /// </summary>
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
            // 0x04 to 0x0F Unused (MPEG reseved)
            NetworkInformationTable = 0x0010, // TID 0x40 (current) & 0x41 (other)
            ServiceDescriptionTable = 0x0011, // TID 0x42,0x46 (ServiceDescription) & 0x4A (BouquetAssociation)
            EventInformationTable = 0x0012,
            RunningStatusTable = 0x0013,
            TimeAndDateTables = 0x0014, // TID 0x70 (TimeAndDate) & 0x73 (TimeOffset)
            NetworkSynchronizationTable = 0x0015,
            //Reserved for future DVB use
            ResolutionNotificationTable = 0x0016,
            //-> 0x 1B
            InbandSignalling = 0x001C,
            Measurement = 0x001D,
            DiscontinuityInformationTable = 0x001E,
            SelectionInformationTable = 0x001F,
            //ARIB	Assigned or reserved //IsArib?
            //32 - 47 (0x2f)
            //USER DEFINED - As assigned in PMT, PMT-E, MGT or MGT-E
            //0x30 - 0x1FF6
            ATSCProgramAssociationTable = 0x1FF7, //ATSC A/65D PAT-E
            ATSCProgramInformationTable = 0x1FF8, //ATSC A/65D STT-PID-E
            ATSCProgramIdentificationTable = 0x1FF9, //ATSC A/65D base_pid_e
            ASTCOperationalOrManagement = 0x1FFA, //ATSC Operational and management packets T3/S9-131
            ASTCBaseProgramIdentificationTable = 0x1FFB, //EAM_CLEAR_PID, PSIP_PID ATSC A/65 base_pid
            //FC?
            ASTCReserved = 0x1FFD, //Formerly A-55
            //ATSCMetaData = 0x1FFB,
            DOCSIS = 0x1FFE, //SCTE
            NullPacket = 0x1FFF
        }

        /// <summary>
        /// Defines the known TableIdentifier's
        /// </summary>
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

        #region AdaptationField

        public sealed class AdaptationField
        {
            private AdaptationField() { }

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

            public static int GetAdaptationFieldLength(byte[] header, int headerOffset, byte[] data, int dataOffset)
            {
                if (header == null) throw new ArgumentNullException("header");

                if (data == null) throw new ArgumentNullException("data");

                if (false == HasAdaptationField(header, headerOffset)) return -1;

                return data[dataOffset];
            }

            public static byte[] GetAdaptationFieldData(byte[] header, int headerOffset, byte[] data, int dataOffset)
            {
                if (header == null) throw new ArgumentNullException("tsUnit");

                if (data == null) throw new ArgumentNullException("data");

                int size = GetAdaptationFieldLength(header, headerOffset, data, dataOffset);

                byte[] result = size <= 0 ? Media.Common.MemorySegment.EmptyBytes : new byte[size];

                /* an adaptation field with length 0 is valid and
                * can be used to insert a single stuffing byte */

                //It is also stated that if there are any extra bytes they are related to the adaptation field no matter if size is set or not.
                //0 for PES Means unbounded however it is not stated what this means...
                //E.g when GetAdaptationFieldLength == 0 and there is more then 1 byte in data is there at least 1 byte for the flags? e.f Math.Max(1, size);

                //For anything including 0 return nothing, otherwise skip the length and return the amount of bytes indicated by length.

                Array.Copy(data, dataOffset, result, 0, size);

                return result;
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
                if (adaptationFlags.HasFlag(AdaptationFieldFlags.ProgramClockReference)) offset += ProgramClockReferenceSize;
                if (adaptationFlags.HasFlag(AdaptationFieldFlags.OriginalProgramClockReference)) offset += ProgramClockReferenceSize;
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
        }

        #endregion

        #region AdaptationFieldExtension        

        public sealed class AdaptationFieldExtension
        {
            private AdaptationFieldExtension() { }

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
        }

        #endregion

        #endregion

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

        public static bool IsUserDefined(PacketIdentifier identifier)
        {
            ushort sid = (ushort)identifier; //Encompass ASTCMetaData?
            return sid >= 0x20 && sid <= 0x1FFA || sid >= 0x1FFC && sid <= 0x1FFE;
        }

        public static bool IsDVBMetaData(PacketIdentifier identifier)
        {
            ushort sid = (ushort)identifier;
            return sid >= 16 || sid <= Common.Binary.FiveBitMaxValue;
        }

        public static bool HasTransportErrorIndicator(byte[] header, int offset = 0) { return (header[offset + 1] & ErrorMask) > 0; }

        public static bool HasPayloadUnitStartIndicator(byte[] header, int offset = 0) { return (header[offset + 1] & PayloadStartUnitMask) > 0; }

        public static bool HasTransportPriority(byte[] header, int offset = 0) { return (header[offset + 1] & PriorityMask) > 0; }

        public static PacketIdentifier GetPacketIdentifier(byte[] header, int offset = 0) { return (PacketIdentifier)(Common.Binary.ReadU16(header, offset + 1, Media.Common.Binary.IsLittleEndian) & PacketIdentifierMask); }

        public static AdaptationFieldControl GetAdaptationFieldControl(byte[] header, int offset = 0) { return (AdaptationFieldControl)(header[offset + 3] >> 4 & Common.Binary.FourBitMaxValue); }

        public static bool HasAdaptationField(byte[] header, int offset = 0) { return GetAdaptationFieldControl(header, offset) > AdaptationFieldControl.None; }

        public void SetAdaptationFieldControl(AdaptationFieldControl value, byte[] header, int offset = 0)
        {
            //MSB, Must reverse value or implement WriteReverseBinaryInteger.
            //Common.Binary.WriteBinaryInteger(header, offset, 0, 4, (long)value);
        }


        public static bool HasPayload(byte[] header, int offset = 0) { return GetAdaptationFieldControl(header, offset).HasFlag(AdaptationFieldControl.AdaptationFieldAndPayload); }

        public static ScramblingControl GetScramblingControl(byte[] header, int offset = 0) { return (ScramblingControl)((header[offset + 3] & ScramblingControlMask) >> 6); }

        public static void SetScramblingControl(ScramblingControl value, byte[] header, int offset = 0)
        {
            //MSB, Must reverse value or implement WriteReverseBinaryInteger.
            //Common.Binary.WriteBinaryInteger(header, offset, 0, 2, (long)value);
        }

        public static int GetContinuityCounter(byte[] header, int offset = 0) { return header[offset + 3] & ContinuityCounterMask; }
    }
}
