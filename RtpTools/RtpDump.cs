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

namespace Media.RtpTools.RtpDump
{
    #region DumpReader

    /// <summary>
    /// Reads files made with a compatible implementation of `<see href="http://www.cs.columbia.edu/irt/software/rtptools/">rtpdump</see>`, <see cref="FileFormat"/> for list of Formats supported.
    /// </summary>
    public sealed class DumpReader : Common.BaseDisposable, IEnumerable<RtpToolEntry>
    {

        #region Fields

        internal DateTime m_StartTime;

        //The format of the underlying dump
        internal FileFormat m_Format;

        //A List detailing the offsets at which RtpToolEntries occurs (maybe used by the writer to allow removal of packets from a stream without erasing them from the source?);
        internal List<long> m_Offsets = new List<long>();

        internal System.IO.BinaryReader m_Reader;

        internal byte[] m_FileHeader;

        #endregion

        #region Properties

        /// <summary>
        /// The format of the stream determined via reading the file, (Until ReadNext has been called the format may not be correctly identified)
        /// </summary>
        public FileFormat Format
        {
            get
            {
                return Disposed ?FileFormat.Unknown : m_Format;
            }
        }

        //The amount of items contained in the dump thus far in reading. (Might not be worth keeping?)
        public int ReadItems { get { return m_Offsets.Count; } }

        /// <summary>
        /// The position in the stream
        /// </summary>
        public long Position { get { return m_Reader.BaseStream.Position; } }

        /// <summary>
        /// The length of the stream
        /// </summary>
        public long Length { get { return m_Reader.BaseStream.Length; } }

        public bool HasNext { get { return Length - Position >= RtpToolEntry.DefaultEntrySize; } }

        #endregion

        #region Constructor / Destructor

        /// <summary>
        /// Creates a DumpReader on the given stream
        /// </summary>
        /// <param name="stream">The stream to read</param>
        /// <param name="leaveOpen">Indicates if the stream should be left open after reading</param>
        /// <param name="DumpFormat">The optional format to force the reader to read the dump in (Useful for <see cref="DumpFormat.Header"/>)</param>
        public DumpReader(System.IO.Stream stream, bool leaveOpen = false, FileFormat? format = null)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            m_Reader = new System.IO.BinaryReader(stream, System.Text.Encoding.ASCII, leaveOpen); // new System.IO.BinaryReader(stream, System.Text.Encoding.ASCII, leaveOpen);
            m_Format = format ?? FileFormat.Unknown;
        }

        /// <summary>
        /// Creates a DumpReader on the path given which must be a valid rtpdump format file. An excpetion will be thrown if the file does not exist or is invalid.
        /// The stream will be closed when the Reader is Disposed.
        /// </summary>
        /// <param name="path">The file to read</param>
        public DumpReader(string path, FileFormat? format = null) : this(new System.IO.FileStream(path, System.IO.FileMode.Open), false, format) { }

        ~DumpReader() { Dispose(); }

        #endregion

        #region Methods

        /// <summary>
        /// Attempts to read the binary file header at the current position of the reader, the header is determined to be persent by 'peeking' and if present and thus determines if the file is Binary or Ascii in the process.
        /// If the file header has already been read then this function will not read anything
        /// </summary>
        void ReadBinaryFileHeader()
        {
            if (m_FileHeader != null) return;

            //If the next character in the stream is not '#' then this is not a binary format
            if (m_Reader.PeekChar() != RtpDumpExtensions.Hash)
            {
                m_Format = FileFormat.Text;
                return;
            }

            //////Progress past the FileHeader should be #!rtpplay1.0 and IP/Port\n
            m_FileHeader = RtpDumpExtensions.ReadDelimitedValue(m_Reader.BaseStream);


            int length = m_FileHeader.Length;

            //strict /..
            //Check for Hash, Bang? someone might have wrote something else...                  


            //Check for rtpplay
            //byte[] rtpPlay = Encoding.ASCII.GetBytes(RtpPlay.RtpPlayFormat);

            //after Hash and Bang
            int start = RtpPlay.RtpPlayBinaryIndex,
                count = RtpPlay.RtpPlayFormatLength;

            if (length < count) goto Invalid;

            //Look for rtpPlay at those precalculated index's
            //Utility.ContainsBytes(m_FileHeader, ref start, ref count, rtpPlay, 0, RtpPlay.RtpPlayFormatLength);

            //rtpplay is not present or if the format was unknown then
            if (!m_FileHeader.Skip(start).Take(count).SequenceEqual(Encoding.ASCII.GetBytes(RtpPlay.RtpPlayFormat))
                ||
                Array.IndexOf<byte>(m_FileHeader, 0x2f, start + count, m_FileHeader.Length - (start + count)) == -1) // start == -1
            {
                goto Invalid;
            }

            //Version...

            // check for an address and port in ASCII delimited by `/`, 22 comes from 
            //255.255.255.255/65535\n
            //               ^
            //if (Array.LastIndexOf<byte>(m_FileHeader, 0x2f, 22, 22) == -1) goto Invalid;

            //Binary type header, maybe header only
            if(m_Format == FileFormat.Unknown) m_Format = FileFormat.Binary;

            //No problems
            return;

        Invalid:
                Common.ExceptionExtensions.CreateAndRaiseException(this, "Binary header is invalid.");

        }

        /// <summary>
        /// Reads a <see cref="RtpToolEntry"/> from the stream and adds the offset to the list of offets. 
        /// The entry is persisted in memory and should be disposed.
        /// </summary>
        /// <returns>The DumpItem read</returns>
        internal RtpToolEntry ReadToolEntry()
        {
            try
            {
                //Create an item with the format guessed by reading the header.
                RtpToolEntry entry = null;

                if (!HasNext) return entry;

                //Read the file header if no other data has been read yet and the format possibly has header (Determines Binary or Text FileFormat)
                if (m_FileHeader == null && RtpDumpExtensions.HasFileHeader(m_Format) && m_Reader.BaseStream.Position == 0) ReadBinaryFileHeader();
                
                //If no format can be determined then raise a DumpReader type exception with the following message.
                if (m_Format == FileFormat.Unknown) Common.ExceptionExtensions.CreateAndRaiseException(this, "Unable to determine format!");

                //Add the offset if we didn't already know about it
                if (m_Offsets.Count == 0 || m_Reader.BaseStream.Position > m_Offsets.Last())
                {
                    //Which is the position in the stream
                    m_Offsets.Add(m_Reader.BaseStream.Position);
                }

                //Only contains data if something goes wrong during parsing,
                //And would then contain the data consumed while attempting to parse.
                byte[] unexpectedData = null;

                //This allows certain text items to have a data= token and others to not.
                //It also allows reading of Rtp in Rtcp only mode
                FileFormat foundFormat = m_Format;

                //If the format is not text then
                if (m_Format < FileFormat.Text)
                {

                    //Could keep each entry stored in a buffer and then use MemorySegment to read the entry into the buffer provided by the reader

                    entry = new RtpToolEntry(foundFormat, new byte[RtpToolEntry.DefaultEntrySize]); //32 bytes allocated per entry.

                    m_Reader.Read(entry.Blob, 0, RtpToolEntry.DefaultEntrySize);

                    //26 bytes read by default here

                    //The format of the item does not match the reader(which would only happen if given an unknown format)
                    if (foundFormat != m_Format)
                    {
                        //If the the format of the entry found was binary
                        if (entry.Format < FileFormat.Text)
                        {
                            //The unexpected data consists of the supposed fileheader.
                            if (m_FileHeader == null)
                            {
                                m_FileHeader = unexpectedData;
                                unexpectedData = null;
                                //Change to binary format, might want to signal this with an exception?
                                //m_Format = entry.Format;
                            }
                            else Common.ExceptionExtensions.CreateAndRaiseException(unexpectedData, "Encountered a Binary file header when already parsed the header. The Tag property contains the data unexpected.");
                        }
                        else if (unexpectedData != null) Common.ExceptionExtensions.CreateAndRaiseException(entry, "Unexpected data found while parsing a Text format. See the Tag property of the InnerException", new Common.Exception<byte[]>(unexpectedData));
                    }                    

                    int itemLength = entry.Length - RtpToolEntry.sizeOf_RD_packet_T;

                    //If there are any more bytes related to the item itemLength will be > 0
                    if (itemLength > 0)
                    {
                        entry.Concat(m_Reader.ReadBytes(itemLength));
                    }

                }
                else
                {
                    //Parse data and build packet from the textual data,
                    //If a Binary format is found m_FileHeader will contain any data which was unexpected by the ParseTextEntry process which should consists of the `#!rtpplay1.0 ...\n`                       
                    entry = RtpSend.ParseText(m_Reader, ref foundFormat, out unexpectedData);
                }

                //Todo fix Offset writing
                /*if (m_Offsets.Count == 1 && entry.Offset != 0) Common.ExceptionExtensions.CreateAndRaiseException(entry, "First Entry Must have Offset 0");
                else*/ if(m_Offsets.Count == 1) m_StartTime = Utility.UtcEpoch1970.AddSeconds(entry.StartSeconds);

                //Call determine format so item has the correct format (Header [or Payload])
                if (foundFormat == FileFormat.Unknown)
                {
                    Common.ExceptionExtensions.CreateAndRaiseException(entry, "Unknown format");
                }

                return entry;
            }
            catch { throw; }
        }

        /// <summary>
        /// Reads the entire dump maintaing a list of all offsets encountered where items occur
        /// </summary>
        public void ReadToEnd()
        {
            RtpToolEntry current = null;
            while (Position < Length && (current = ReadToolEntry()) != null)
            {
#if DEBUG
                 System.Diagnostics.Debug.WriteLine("Found DumpItem @ " + m_Offsets.Last());
#endif

                 current.Dispose();
            }

            //Should be at the end of the stream here
        }

        /// <summary>
        /// Reads the next
        /// </summary>
        /// <param name="type">The optional specific type of packet to find so inspection will be performed for you</param>
        /// <returns>The data which makes up the packet if found, otherwise null</returns>
        public RtpToolEntry ReadNext()
        {
            return ReadToolEntry();
        }

        /// <summary>
        /// Reads the next packet from the dump which corresponds to the given time and type.
        /// </summary>
        /// <param name="fromBeginning">The TimeSpan from the beginning of the dump</param>
        /// <param name="type">The optional type of item to find</param>
        /// <returns>The data which makes up the packet at the location in the dump</returns>
        public RtpToolEntry ReadNext(TimeSpan fromBeginning)
        {
            return InternalReadNext(fromBeginning);
        }

        /// <summary>
        /// Skips the given amount of items in the dump from the current position (forwards or backwards)
        /// </summary>
        /// <param name="count">The amount of items to skip</param>
        public void Skip(int count)
        {
            //Going forwards must try to parse
            if (count > 0)
            {
                while (HasNext && count > 0)
                {
                    using (RtpToolEntry entry = ReadToolEntry()) 
                    {
                        --count;
                    }
                }
            }
            else
            {
                //Ensure not Out of Range
                if (m_Offsets.Count + count > m_Offsets.Count) throw new ArgumentOutOfRangeException("count");

                //We already know the offsets
                m_Reader.BaseStream.Seek(m_Offsets[m_Offsets.Count + count], System.IO.SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Reads a DumpItem from the beginning of the file with respect to the given options
        /// </summary>
        /// <param name="fromBeginning">The amount of time which must pass in the file before a return will be possible</param>
        /// <param name="type"></param>
        /// <returns></returns>
        internal RtpToolEntry InternalReadNext(TimeSpan fromBeginning)
        {
            if (fromBeginning < TimeSpan.Zero) throw new ArgumentOutOfRangeException("timeOffset cannot be less than the start of the file which is defined in the header.");
            RtpToolEntry current = null;
            m_Reader.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);
            ////
            while ((current = ReadToolEntry()) != null && fromBeginning.TotalMilliseconds >= 0)
            {
                fromBeginning -= TimeSpan.FromMilliseconds(current.Timeoffset);
            }
            return current;
        }

        /// <summary>
        /// Closes the underlying stream if was not set to leave open
        /// </summary>
        public void Close()
        {
            m_Reader.Dispose();
        }

        /// <summary>
        /// Calls Close
        /// </summary>
        public override void Dispose()
        {
            if (Disposed) return;
            base.Dispose();
            m_FileHeader = null;
            Close();
        }

        /// <summary>
        /// Enumerates the packets found
        /// </summary>
        /// <returns>A yield around each Item returned</returns>
        public IEnumerator<RtpToolEntry> GetEnumerator()
        {
            m_Reader.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);
            while (HasNext) yield return ReadToolEntry();
        }

        /// <summary>
        /// Enumerates the byte[]'s which contains packets in the dump file
        /// </summary>
        /// <returns></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

    #endregion

    #region DumpWriter

    /// <summary>
    /// Writes files which should be compatible with an implementation of `<see href="http://www.cs.columbia.edu/irt/software/rtptools/">rtp tools</see>`, 
    /// <see cref="FileFormat"/> for list of Formats supported.
    /// </summary>
    public sealed class DumpWriter : Common.BaseDisposable
    {
        #region Fields

        //The format the DumpWriter is writing in
        FileFormat m_Format;

        //The file header of the Dump being written
        byte[] m_FileHeader;

        System.IO.BinaryWriter m_Writer;

        /// <summary>
        /// The ip and port which is realted to all entries being written if not explicitly given when creating or writing an entry.
        /// </summary>
        System.Net.IPEndPoint m_Source;

        /// <summary>
        /// The date and time in which the first entry was created in the resulting file being written.
        /// </summary>
        System.DateTimeOffset m_Start;

        /// <summary>
        /// Indicates if any required file header was written by this instance, will be true if the stream was modified.
        /// </summary>
        bool m_WroteHeader;

        /// <summary>
        /// A cached count of the amount of <see cref="RtpToolEntry"/> instances written to the underlying stream by this RtpDumpWriter.
        /// </summary>
        int m_ItemsWritten = 0;

        #endregion

        #region Properties

        /// <summary>
        /// The position in the stream
        /// </summary>
        public long Position { get { return m_Writer.BaseStream.Position; } }

        /// <summary>
        /// The length of the stream
        /// </summary>
        public long Length { get { return m_Writer.BaseStream.Length; } }

        /// <summary>
        /// The count of items written to the dump thus far
        /// </summary>
        public int Count { get { return m_ItemsWritten; } }

        /// <summary>
        /// The Date and Time which corresponds to the first entry in the file being written
        /// </summary>
        public DateTimeOffset StartTime { get { return m_Start; } }

        /// <summary>
        /// The IPEndPoint which the writer defaults to when writing an entry if not provided.
        /// </summary>
        public System.Net.IPEndPoint Source { get { return m_Source; } }

        #endregion

        #region Constructor / Destructor

        /// <summary>
        /// Creates a DumpWriter which writes rtpdump comptaible files.
        /// If <param name="modify" /> is true the StartTime and Source properties will be attempted to be copied from the source stream, if they are not present they will be used from the parameters given.
        /// 
        /// Throws a <see cref="ArgumentNullException"/> if <paramref name="stream"/> or <paramref name="source"/> is null.
        /// Throws a <see cref="Common.Exception"/> with the Type `DumpWriter` if an unexpected error occurs, if the underlying exception is realted to reading any information being read from a stream being modified then it will be of type `DumpReader`.
        /// </summary>
        /// <param name="stream">The stream to write to</param>
        /// <param name="format">The format to write in</param>
        /// <param name="source">The source where packets came from in this dump</param>
        /// <param name="utcStart">The optional start of the file recording (used in the header)</param>
        /// <param name="modify">Optionally indicates if the file should be modified or created, by default it will not overwrite files if they exist.</param>
        /// <param name="leaveOpen">Indicates if the stream should be left open after calling Close or Dipose</param>
        public DumpWriter(System.IO.Stream stream, FileFormat format, System.Net.IPEndPoint defaultSource, DateTimeOffset? startTime = null, bool modify = false, bool leaveOpen = false)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            
            if (defaultSource == null) throw new ArgumentNullException("source");
            
            m_Format = format;

            m_Source = defaultSource;

            m_Start = startTime ?? DateTimeOffset.UtcNow;

            if (!modify) // New file
            {
                //Create the file header now
                m_FileHeader = RtpDumpExtensions.CreateFileHeader(m_Source);

                m_WroteHeader = false;

                //Create the writer
                m_Writer = new System.IO.BinaryWriter(stream, Encoding.ASCII, leaveOpen);
            }
            else//Modifying
            {

                //The stream should be the same format.
                FileFormat streamFormat = m_Format;

                try
                {
                    //Header already written when modifying a file
                    //Need to read the header and advance the stream to the end, indicate the header was already written so it is not again.
                    using (DumpReader reader = new DumpReader(stream, m_WroteHeader = true))
                    {
                        //Create the writer forcing ASCII Encoding, leave the stream open if indicated
                        m_Writer = new System.IO.BinaryWriter(stream, Encoding.ASCII, leaveOpen);

                        //Read to the end so packets can be added
                        reader.ReadToEnd();

                        //Ensure the format of the writer matches the reader, if not throw an exception so it can be handled appropriately.
                        //The exception will be of type `DumpReader`
                        if (reader.m_Format != m_Format) Common.ExceptionExtensions.CreateAndRaiseException(reader, "Format of writer does not match reader, Expected: " + m_Format + " Found: " + reader.m_Format);

                        //Copy the file header from the reader if present, otherwise the file will have no header when written.
                        m_FileHeader = reader.m_FileHeader;

                        //Check for the header to be present on existing files if the format has a header. (only Binary)
                        //The exception will be of type `DumpReader`
                        if (m_FileHeader == null && m_Format.HasFileHeader()) Common.ExceptionExtensions.CreateAndRaiseException(reader, "Did not find the expected Binary file header.");

                        //If not present use the start time indicated in the first entry...
                        if (m_Start == null) m_Start = startTime ?? reader.m_StartTime;
                    }
                }
                catch (Exception ex)//Only catch exceptions which are unexpected and raise a generic DumpWriter exception
                {
                    Common.ExceptionExtensions.CreateAndRaiseException(this, "An unexpected exception occured while reading the existing information present in the stream. See InnerException for more details.", ex);
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
        public DumpWriter(string filePath, FileFormat format, System.Net.IPEndPoint source, DateTime? utcStart = null, bool overWrite = false, bool modify = false) : this(new System.IO.FileStream(filePath, !modify ? overWrite ? System.IO.FileMode.Create : System.IO.FileMode.CreateNew : System.IO.FileMode.OpenOrCreate), format, source, utcStart, modify) { }

        ~DumpWriter()
        {
            Dispose();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Writes the rtpdump file header
        /// </summary>
        public void WriteFileHeader(bool force = false)
        {
            if (!force && m_WroteHeader) return;

            //Header is only written in Binary files
            if(m_Format < FileFormat.Text) m_Writer.Write(m_FileHeader, 0, m_FileHeader.Length);
            
            //We wrote the header...
            m_WroteHeader = true;
        }

        /// <summary>
        /// Writes the given RtpPacket to the stream at the current position.
        /// If written in <see cref="DumpFormat.Binary"/> or <see cref="DumpFormat.Header"/> the packet will contain an 8 Byte overhead. 
        /// If written in <see cref="DumpFormat.Header"/> the packet will not contain the RtpPacket Payload or the Extension Data if present.
        /// If written in <see cref="DumpFormat.Payload"/> the Rtp Packet will only contain the RTP Payload and will not be able to be read back into RtpPackets with this class.        
        /// </summary>
        /// <param name="packet">The time</param>
        /// <param name="timeOffset">The optional time the packet was recieved relative to the beginning of the file. If the packet has a Created time that will be used otherwise DateTime.UtcNow.</param>
        public void WritePacket(Rtp.RtpPacket packet, TimeSpan? timeOffset = null, System.Net.IPEndPoint source = null)
        {
            if (timeOffset < TimeSpan.Zero) throw new ArgumentOutOfRangeException("timeOffset cannot be less than the start of the file which is defined in the header. ");

            //here source may be null and should be read from the file header

            if (m_FileHeader == null && source == null) throw new ArgumentNullException("source cannot be null when there is no file header");

            //Use the tool entry so it is disposed immediately
            using (var entry = new RtpToolEntry(source ?? m_Source, packet)) 
            {
                //If given a specific timeoffset use it
                if (timeOffset.HasValue) entry.Timeoffset = timeOffset.Value.TotalMilliseconds;
                else entry.Timeoffset = (DateTime.UtcNow - m_Start).TotalMilliseconds; //otherwise calulcate it

                //Write the item
                WriteToolEntry(entry);
            }
        }

        /// <summary>
        /// Writes a RtcpPacket to the dump. 
        /// If written in Binary the packet will contain an 8 Byte overhead. If written in Payload or Header the Rtcp Packet is silently ignored.
        /// </summary>
        /// <param name="packet">The packet to write</param>
        /// <param name="timeOffset">The optional time the packet was recieved relative to the beginning of the file. If the packet has a Created time that will be used otherwise DateTime.UtcNow.</param>
        public void WritePacket(Rtcp.RtcpPacket packet, TimeSpan? timeOffset = null, System.Net.IPEndPoint source = null)
        {
            if (timeOffset < TimeSpan.Zero) throw new ArgumentOutOfRangeException("timeOffset cannot be less than the start of the file which is defined in the header. ");

            //here source may be null and should be read from the file header

            if (m_FileHeader == null && source == null) throw new ArgumentNullException("source cannot be null when there is no file header");

            //Use the tool entry so it is disposed immediately
            using (var entry = new RtpToolEntry(source ?? m_Source, packet))
            {
                //If given a specific timeoffset use it


                /* start of recording (GMT) */
                if (timeOffset.HasValue) entry.Timeoffset = timeOffset.Value.TotalMilliseconds;
                else entry.Timeoffset = (DateTime.UtcNow - m_Start).TotalMilliseconds; //otherwise calulcate it

                //entry.Offset = /* milliseconds since the start of recording */

                //Write the item
                WriteToolEntry(entry);
            }
        }

        //WritePackets(RtcpPacket[])

        //WritePackets(RtpFrame frame)

        /// <summary>
        /// Allows writing of a binary item to the dump. May be used for to write Compound RTCP Packets in a single item.
        /// (Maybe should have RtcpPacket[] rather then itemBytes binary)
        /// </summary>
        /// <param name="itemBytes">The bytes of the item</param>
        /// <param name="timeOffset">The optional timeoffset since the file was created to store in the item</param>
        /// <param name="source">The optional source EndPoint from which the itemBytes arrived</param>
        //public void WriteBinary(byte[] itemBytes, TimeSpan? timeOffset = null, System.Net.IPEndPoint source = null)
        //{
        //    if (timeOffset < TimeSpan.Zero) throw new ArgumentOutOfRangeException("timeOffset cannot be less than the start of the file which is defined in the header. ");
        //    DumpHeader header;
        //    if (source != null) header = new DumpHeader()
        //    {
        //        SourceEndPoint = source,
        //        UtcStart = m_Header.UtcStart
        //    };
        //    else header = m_Header;
        //    WriteDumpItem(new DumpItem()
        //    {
        //        Header = header,                
        //        Packet = itemBytes,
        //        Length = (ushort)itemBytes.Length,
        //        PacketLength = 0, //Should be allowed to be set E.g. make this take a array of RtcpPacket...
        //        TimeOffset = timeOffset ?? (m_Header.UtcStart - DateTime.UtcNow)
        //    });            
        //}

        /// <summary>
        /// Writes a DumpItem to the underlying stream
        /// </summary>
        /// <param name="item">The DumpItem to write</param>
        internal void WriteToolEntry(RtpToolEntry entry)
        {
            //If already written nothing occurs
            WriteFileHeader();

            //Write binary entry
            if (m_Format < FileFormat.Text)
            {
                if (m_Format == FileFormat.Header)
                {

                    if (entry.IsRtcp)
                    {
                        using(Rtcp.RtcpPacket rtcp = new Rtcp.RtcpPacket(entry.Blob, entry.Pointer))
                            m_Writer.Write(rtcp.Header.ToArray());
                    }
                    else if(m_Format != FileFormat.Rtcp) using (Rtp.RtpPacket rtp = new Rtp.RtpPacket(entry.Blob, entry.Pointer))
                        {
                            m_Writer.Write(rtp.Header.ToArray());
                            if (rtp.ContributingSourceCount > 0) m_Writer.Write(rtp.GetSourceList().AsBinaryEnumerable().ToArray());
                            if (rtp.Extension) m_Writer.Write(rtp.GetExtension().ToArray());
                        }
                }
                else if (m_Format == FileFormat.Binary)
                {
                    m_Writer.Write(entry.Blob);
                }
                else if (m_Format == FileFormat.Payload)
                {
                    if (entry.IsRtcp)using (Rtcp.RtcpPacket rtcp = new Rtcp.RtcpPacket(entry.Blob, entry.Pointer)) m_Writer.Write(rtcp.Payload.ToArray());
                    else using (Rtp.RtpPacket rtp = new Rtp.RtpPacket(entry.Blob, entry.Pointer)) m_Writer.Write(rtp.Coefficients.ToArray());
                }
            }
            else
            {
                //Write the textual version of the entry
                m_Writer.Write(System.Text.Encoding.ASCII.GetBytes(entry.ToString(m_Format)));
            }

            //Increment for the entry written
            ++m_ItemsWritten;
        }

        /// <summary>
        /// Closes and Disposes the underlying stream if indicated to do so when constructing the DumpWriter
        /// </summary>
        public void Close()
        {
            if (m_Writer != null) m_Writer.Dispose();
        }

        /// <summary>
        /// Calls Close
        /// </summary>
        public override void Dispose()
        {
            if (Disposed) return;
            base.Dispose();
            Close();
            m_FileHeader = null;
            m_Writer = null;
        }

        #endregion
    }

    #endregion    


    public class Program : Common.BaseDisposable
    {

        public RtpDump.DumpWriter Writer { get; set;  }

        public static void Main(string[] args)
        {

        }
    }

    /// <summary>
    /// Provides useful methods for working with RtpDump formatted data.
    /// </summary>
    public static class RtpDumpExtensions
    {        
        internal const string HashBang = "#!";

        internal const char Hash = '#';

        internal const char Bang = '!';

        /// <summary>
        /// Indicates if the <see cref="DumpFormat"/> has a <see cref="FileHeaderFormat"/>
        /// </summary>
        /// <param name="format">The format to check</param>
        /// <returns>true if <paramref name="format"/> has a file header.</returns>
        public static bool HasFileHeader(this FileFormat format) { return format <= FileFormat.Payload; }

        /// <summary>
        /// The format is as follows: The file starts with[:]
        ///     #!rtpplay1.0 address/port\n        
        /// </summary>
        /// <remarks>
        /// ------- Interpretation  of the Binary Header used with 'rtpdump' and possibly elsewhere. -------
        /// 
        /// The file  `bark.rtp` contains the following data at offset 0x00:
        ///
        /// `!#rtpplay1[0x2e]0[0x20]0.0.0.0/8526[0x0A]`
        /// 
        /// The length of this header is dependent upon the address and port used to create the header
        /// and thus because the encoding of the data in the file header is never actually specified and could (possibly) be interpreted as the default encoding used in the system (which may be more then 8 bits wide),
        /// for these reasons among others it is chosen to respect the above conventions when creating an 'rtpdump' file and the following rules should be defined:
        /// 
        /// ASCII Encoding is used to generate the binary data of the header.
        /// 
        ///     `#!` - should not reflect the length of the data which is contained in the file itself.
        ///     
        ///     'rtpplay' - Should never be anything but 'rtpplay';
        /// 
        ///     `Version` - should never be anything but '1[0x2e]0' when creating compatible files.
        /// 
        ///     'address/port' - can really only be interpreted based on the value in the `bark.rtp` file which is explained below.
        ///         For this fact in which is observed it is assumed (correctly so) that the 'address/port' sequence is also in the same encoding,
        ///         although it would have been safe to use binary encoding and specify big endian encoding(such as JPEG did).
        ///      
        ///     '0x0A' - Is explained below.
        /// 
        /// If the <see cref="DumpFormat"/> of the file in question is <see cref="DumpFormat.Text"/> or greater the header of the file will not be present at all.
        /// 
        /// It is worth noting there are caveats using this format beyond those are specified [or not] below
        /// and implies that storage is better suited in another format such as one which can faciliate O(1) access of a packet 
        /// (with or without the time offset and other details which may well be implicit to the storage application and the endian used in the system.)
        /// 
        /// ===============================
        ///            16 Bits - '#!' 
        /// ===============================
        ///         
        /// '#' is 0x22 hexidecimal (100010)
        /// 
        /// '!' is 0x21 hexidecimal [100001] 
        /// 
        ///     This is actually the length of the data in the file in hexidecimal [big endian] format (8993 decimal which backwards is 2821 decimal, B05 hexidecimal)
        ///     When modifying packets this value should not be updated and this is assumingly because more packets can be added without updating the header.
        ///     See notes below.
        /// 
        /// ===============================
        ///            56 Bits - 'rtpplay'
        /// ===============================
        /// 
        /// File Format indicator
        ///     Is due to the fact 'rtpplay' and 'rtpsend' use different 'dump' formats, 'rtpsend' uses <see cref="DumpFormat.Ascii"/> and 'rtpplay' uses <see cref="DumpFormat.Binary"/>.
        ///      Since 'rtpplay' nor 'rtpsend' output any files the 'rtpdump' program output files only in a single version with a specific header format,
        ///     and subsequently did not use a header when outputting in non - binary formats because the binary data would be encoded in a textual encoding per the storage of string instances in that operating system.
        ///     It is also worth noting that 'rtptrans' amother other 'rtp tools' may be able to use any of the file formats so long as the format is given correctly to 'rtpdump' which is `outputting` the format.
        ///     This  implied `output` would then be <see href="http://en.wikipedia.org/wiki/Redirection_(computing)">redirected</see> to the input of the other program on a <see href="http://en.wikipedia.org/wiki/Unix">Unix</see>  like <see href="http://en.wikipedia.org/wiki/Operating_System">Operating System</see>.
        ///     The Tool Name e.g. 'rtpplay' should never equal 'rtpdump' or 'rtpsend' or any other value because the original tools would probably not read them.
        ///     
        /// ===============================
        ///            24 Bits - '[0x3]1[0x2e][0x3]0'
        /// ===============================
        /// 
        ///     Corresponds to the version of the file format used. 
        ///     Appears to be ASCII Encoded, literally `1.0`
        /// 
        /// ===============================
        ///            104 Bits - `0.0.0.0/8526[0x0A]`
        /// ===============================
        /// 'address/port' - 
        ///     Is [hopefully] meant to contain two entities.
        ///     A single <see href="http://en.wikipedia.org/wiki/IP_Address">IP Address</see>,
        ///     A '/' character,
        ///     A unsigned 16 bit integer which corresponds to the network port [which was `possibly` used to originally receive the data contained in the file but possibly modified or never set].
        ///     The encoding of these values was never specified and thus was interpreted from the `bark.rtp` file and explained above.
        ///
        /// It is worth nothing that there could be more or less than 104 Bits remaining in this header and the only way to know is by looking for...
        /// 
        /// [0x0a] hexidecimal which is Line Feed in <see href="http://en.wikipedia.org/wiki/ASCII">ASCII Encoding</see>.
        /// </remarks>
        internal const string FileHeaderFormat = HashBang + RtpPlay.RtpPlayFormat + " {0}/{1}\n";

        /// <summary>
        /// Creates a byte[] with which describes a file which is compatible with 'rtpdump'.
        /// </summary>
        /// <param name="source">The <see cref="IPEndPoint"/> which will be indicated in the header.</param>
        /// <returns>The bytes created</returns>
        internal static byte[] CreateFileHeader(System.Net.IPEndPoint source)
        {
            //All files must indicate a source address and port
            if (source == null) throw new ArgumentNullException("source");
            
            //Strings in .Net are encoded in Unicode encoding unless otherwise specified, e.g. in the MicroFramework UTF-8
            return System.Text.Encoding.ASCII.GetBytes(string.Format(FileHeaderFormat, source.Address.ToString(), source.Port.ToString()));
        }

        /// <summary>
        /// Reads a binary sequence terminated by first by <paramref name="delimit"/> and then by the end of the file if not found.
        /// </summary>
        /// <param name="reader">The binary read to read from</param>
        /// <param name="delimit">The byte to read for</param>
        /// <param name="includeDelimit">An optional value indicating if delimit should be present in the result</param>
        /// <returns>The bytes read from the reader</returns>
        internal static byte[] ReadDelimitedValue(System.IO.Stream stream, byte delimit = Common.ASCII.LineFeed, bool includeDelimit = false)
        {
            //The result of reading from the stream
            byte[] result = null;

            try
            {
                //Declare a value which will end up in a register on the stack
                int register = -1;

                //Indicate when to terminate reading.
                bool terminate = false;

                //Use a MemoryStream as to not lock the reader
                using (var buffer = new System.IO.MemoryStream())
                {
                    //While data can be read from the stream
                    while (!terminate) 
                    {
                        //Read a byte from the stream
                        register = stream.ReadByte();

                        //Check for termination
                        terminate = register == -1 || register == delimit;

                        //If the byte read is equal to the delimit and the delimit byte is not included then return the array contained in the MemoryStream.
                        if (terminate && !includeDelimit) break;

                        //Write the value read from the reader to the MemoryStream
                        buffer.WriteByte((byte)register);
                    }
                    //If terminating then return return the array contained in the MemoryStream.
                    result = buffer.ToArray();
                }
            }
            catch { /*Hide*/ }
            
            //Return the bytes read from the stream
            return result;
        }

        internal static void ReadAsciiSpace(System.IO.Stream stream, out byte[] result) 
        {
            result = ReadDelimitedValue(stream, Common.ASCII.Space, true);
            return;
        }

        /// <summary>
        /// Reads a binary sequence terminated by first by <paramref name="delimit"/> and then by the end of the file if not found.
        /// </summary>
        /// <param name="reader">The binary read to read from</param>
        /// <param name="delimit">The byte to read for</param>
        /// <param name="includeDelimit">An optional value indicating if delimit should be present in the result</param>
        /// <returns>The bytes read from the reader</returns>
        internal static byte[] ReadDelimitedValue(System.IO.Stream stream, byte[] delimit, bool includeDelimit = false, bool matchDelemit = true)
        {
            //The result of reading from the stream
            byte[] result = null;

            try
            {
                //Declare a value which will end up in a register on the stack
                int delimitLength = delimit.Length;

                //Indicate when to terminate reading.
                bool terminate = false;

                //Use a BinaryReader to get ReadByte, we are only reading bytes
                using (var buffer = new System.IO.BinaryReader(stream, System.Text.Encoding.Default, true))
                {
                    //Use a MemoryStream to not force allocation until delimit or EOS is found
                    using (var memory = new System.IO.MemoryStream(delimitLength))
                    {
                        //While data can be read from the stream
                        while (!terminate)
                        {
                            result = buffer.ReadBytes(delimitLength);

                            int resultingLength = result.Length;

                            terminate = resultingLength < delimitLength || (matchDelemit ? result.SequenceEqual(delimit) : result.Any(b => delimit.Any(d => d == b)));

                            if (terminate && !includeDelimit) break;
                            
                            memory.Write(result, 0, resultingLength);
                        }

                        //The result is the memory in the memory stream
                        result = memory.ToArray();
                    }
                }
            }
            catch { /*Hide*/ }

            //Return the bytes read from the stream
            return result;
        }      
    }
}
