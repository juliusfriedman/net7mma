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

#region Using Statements

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Media.Common;

#endregion
namespace Media.Rtcp
{
    #region ApplicationSpecificReport

    /// <summary>
    /// Provides an implemenation of the Application-Defined RTCP Packet found in http://tools.ietf.org/html/rfc3550#section-6.6
    /// </summary>
    /// <remarks>
    /// The APP packet is intended for experimental use as new applications and new features are developed, without requiring packet type value  registration.  
    /// APP packets with unrecognized names SHOULD be ignored. After testing and if wider use is justified, it is RECOMMENDED that each APP packet be redefined without the subtype and name fields and registered with IANA using an RTCP packet type
    /// </remarks>
    public class ApplicationSpecificReport : RtcpReport
    {
        #region Constants and Statics

        public new const int PayloadType = 204;

        /// <summary>
        /// The amount of bytes which occupy the <see cref="Name"/> field in any ApplicationSpecificReport.
        /// </summary>
        public const int NameSize = 4;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs a new ApplicationSpecificReport with the given values.
        /// </summary>
        /// <param name="version">The version of the report</param>
        /// <param name="padding">Indicates if the Padding bit should be set in the header</param>
        /// <param name="subType">The subType of the ApplicationSpecificReport</param>
        /// <param name="name">The name of the ApplicationSpecificReport</param>
        /// <param name="applicationDependentData">Optional Application specific data.</param>
        public ApplicationSpecificReport(int version, int padding, int ssrc, int subType, byte[] name, byte[] applicationDependentData, bool shouldDispose = true)
            : base(version, PayloadType, padding, ssrc, 
            subType, //BlockCount
            0, //BlockSize
            Media.Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(applicationDependentData) ? NameSize : NameSize + applicationDependentData.Length + 1,//Extension size in bytes
            shouldDispose) 
        {
            //Copy the given name
            if (false == Media.Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(name)) Array.Copy(name, 0, Payload.Array, Payload.Offset, NameSize);

            //Copy the ApplicationDependentData
            if (HasExtensionData)
            {
                applicationDependentData.CopyTo(Payload.Array, Payload.Offset + NameSize);
            }
        }

        public ApplicationSpecificReport(int version, int ssrc, int subType, byte[] name, byte[] applicationDependentData, bool shouldDispose = true)
            : this(version, 0, ssrc, subType, name, applicationDependentData, shouldDispose)
        {

        }

        public ApplicationSpecificReport(int version, int ssrc, int subType, string name, byte[] applicationDependentData, bool shouldDispose = true)
            : this(version, 0, ssrc, subType, Encoding.UTF8.GetBytes(name), applicationDependentData, shouldDispose)
        {

        }

        /// <summary>
        /// Constructs a new ApplicationSpecificReport from the given <see cref="RtcpHeader"/> and payload.
        /// Changes to the header and payload are immediately reflected in this instance.
        /// </summary>
        /// <param name="header">The header</param>
        /// <param name="payload">The payload</param>
        public ApplicationSpecificReport(RtcpHeader header, Common.MemorySegment payload, bool shouldDispose = true)
            : base(header, payload, shouldDispose)
        {
            if (Header.PayloadType != PayloadType) throw new ArgumentException("Header.PayloadType is not equal to the expected type of 204.", "reference");
        }

        /// <summary>
        /// Constructs a new ApplicationSpecificReport from the given <see cref="RtcpHeader"/> and payload.
        /// Changes to the header are immediately reflected in this instance.
        /// Changes to the payload are not immediately reflected in this instance.
        /// </summary>
        /// <param name="header">The header</param>
        /// <param name="payload">The payload</param>
        public ApplicationSpecificReport(RtcpHeader header, IEnumerable<byte> payload, bool shouldDispose = true)
            : base(header, payload, shouldDispose)
        {
            if (Header.PayloadType != PayloadType) throw new ArgumentException("Header.PayloadType is not equal to the expected type of 204.", "reference");
        }

        public ApplicationSpecificReport(RtcpPacket reference, bool shouldDispose = true)
            :base(reference.Header, reference.Payload, shouldDispose)
        {
            if (Header.PayloadType != PayloadType) throw new ArgumentException("Header.PayloadType is not equal to the expected type of 204.", "reference");
        }

        #endregion

        #region Properties

        public override int ReportBlockOctets { get { return NameSize; } }

        /// <summary>
        /// The subtype of the Application-Defined RTCP Packet
        /// </summary>
        public virtual int Subtype
        {
            get { return Header.BlockCount; }
            protected set
            {
                if (IsReadOnly) throw new InvalidOperationException("Can only set the Subtype property when IsReadOnly is false.");

                Header.BlockCount = value;
            }
        }

        /// <summary>
        /// Gets the 32 bit name assoicted with the ApplicationSpecificReport.
        /// </summary>
        public virtual byte[] Name
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return Payload.Take(NameSize).ToArray(); }
            //set
            //{
            //    if (IsReadOnly) throw new InvalidOperationException("Can only set the Name property when IsReadOnly is false.");

            //    if (HasExtensionData)
            //    {

            //    }

            //    Array.Copy(value, 0, Payload.Array, Payload.Offset, NameSize);
            //    //value.Take(NameSize).ToArray().CopyTo(Payload.Array, Payload.Offset);
            //}
        }

        /// <summary>
        /// Indicates if any ApplicationDependentData is present.
        /// </summary>
        public virtual bool HasApplicationDependentData
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return HasExtensionData; }
        }

        public virtual int ApplicationDependentOctets
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return ExtensionDataOctets;
            }
        }

        /// <summary>
        /// Gets the ApplicationDependentData defined in the ApplicationSpecificReport.
        /// If HasApplicationDependentData is false an Empty sequence is returned.
        /// </summary>
        public virtual IEnumerable<byte> ApplicationDependentData
        {
            get
            {
                return Payload.Skip(NameSize).Take(ExtensionDataOctets);
            }
            //set
            //{
            //    ExtensionData = value;
            //}
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a clone of this ApplicationSpecificReport.
        /// If reference is true changes in either instance will be reflected in both.
        /// </summary>
        /// <param name="reference">Indicates if the new instance should reference this instance</param>
        /// <returns>The newly created instance.</returns>
        public ApplicationSpecificReport Clone(bool reference)
        {
            if (reference) return new ApplicationSpecificReport(Header, Payload);

            return new ApplicationSpecificReport(Header.Clone(), Prepare().ToArray());
        }

        #endregion

        public override IEnumerator<IReportBlock> GetEnumerator()
        {
            yield break;
        }
    }

    #endregion
}


namespace Media.UnitTests
{
    /// <summary>
    /// Provides tests which ensure the logic of the ApplicationSpecificReport class is correct
    /// </summary>
    internal class RtcpApplicationSpecificReportUnitTests
    {
        /// <summary>
        /// O( )
        /// </summary>
        public static void TestAConstructor_And_Reserialization()
        {
            //Permute every possible value in the 5 bit BlockCount
            for (byte SubTypeCounter = byte.MinValue; SubTypeCounter <= Media.Common.Binary.FiveBitMaxValue; ++SubTypeCounter)
            {
                //Permute every possible value in the Padding field.
                for (byte PaddingCounter = byte.MinValue; PaddingCounter <= Media.Common.Binary.FiveBitMaxValue; ++PaddingCounter)
                {
                    //Enumerate every possible reason length
                    for (byte ApplicationSpecificDataLength = byte.MinValue; ApplicationSpecificDataLength <= Media.Common.Binary.FiveBitMaxValue; ++ApplicationSpecificDataLength)
                    {
                        //Create a random id
                        int RandomId = RFC3550.Random32(Utility.Random.Next());

                        //Create the Name and ApplicationDependentData
                        IEnumerable<byte> NameData = Array.ConvertAll(Enumerable.Range(1, (int)Rtcp.ApplicationSpecificReport.NameSize).ToArray(), Convert.ToByte),
                            ApplicationDependentData = Array.ConvertAll(Enumerable.Range(1, (int)ApplicationSpecificDataLength).ToArray(), Convert.ToByte);

                        //Create a GoodbyeReport instance using the specified options.
                        using (Media.Rtcp.ApplicationSpecificReport p = new Rtcp.ApplicationSpecificReport(0, PaddingCounter, RandomId, SubTypeCounter, NameData.ToArray(), ApplicationDependentData.ToArray()))
                        {
                            //Check IsComplete
                            System.Diagnostics.Debug.Assert(p.IsComplete, "IsComplete must be true.");

                            //Calculate the length of the ReasonForLeaving, should always be padded to 32 bits for octet alignment.
                            int expectedApplicationSpecificDataLength = ApplicationSpecificDataLength > 0 ? Binary.BytesToMachineWords(ApplicationSpecificDataLength + 1) * Binary.BytesPerInteger : 0;

                            //Check HasApplicationDependentData
                            System.Diagnostics.Debug.Assert(expectedApplicationSpecificDataLength > 0 == p.HasApplicationDependentData, "Unexpected HasApplicationDependentData");

                            //Check the Payload.Count
                            System.Diagnostics.Debug.Assert(p.Payload.Count == Rtcp.ApplicationSpecificReport.NameSize + PaddingCounter + expectedApplicationSpecificDataLength, "Unexpected Payload Count");

                            //Check the Length, 
                            System.Diagnostics.Debug.Assert(p.Length == Rtcp.RtcpHeader.Length + Binary.BytesPerInteger + Rtcp.ApplicationSpecificReport.NameSize + PaddingCounter + expectedApplicationSpecificDataLength, "Unexpected Length");

                            //Check the BlockCount and SubType
                            System.Diagnostics.Debug.Assert(p.BlockCount == SubTypeCounter && p.Subtype == SubTypeCounter, "Unexpected SubType");

                            //Check the RtcpData, Name and ApplicationDependentData
                            System.Diagnostics.Debug.Assert(p.RtcpData.Take(Rtcp.ApplicationSpecificReport.NameSize + ApplicationSpecificDataLength).SequenceEqual(NameData.Concat(ApplicationDependentData))
                                && p.Name.SequenceEqual(NameData)
                                && p.ApplicationDependentData.Count() == expectedApplicationSpecificDataLength
                                && p.ApplicationDependentData.Take(ApplicationSpecificDataLength).SequenceEqual(ApplicationDependentData), "Unexpected ApplicationDependentData data");

                            //Check the PaddingOctets count
                            System.Diagnostics.Debug.Assert(p.PaddingOctets == PaddingCounter, "Unexpected PaddingOctets");

                            //Check all data in the padding but not the padding octet itself.
                            System.Diagnostics.Debug.Assert(p.PaddingData.Take(PaddingCounter - 1).All(b => b == 0), "Unexpected PaddingData");

                            //Serialize and Deserialize the packet and verify again
                            using (Media.Rtcp.ApplicationSpecificReport s = new Rtcp.ApplicationSpecificReport(new Rtcp.RtcpPacket(p.Prepare().ToArray(), 0)))
                            {
                                //Check the Payload.Count
                                System.Diagnostics.Debug.Assert(s.Payload.Count == p.Payload.Count, "Unexpected Payload Count");

                                //Check the Length, 
                                System.Diagnostics.Debug.Assert(s.Length == p.Length, "Unexpected Length");

                                //Check the BlockCount and SubType
                                System.Diagnostics.Debug.Assert(p.BlockCount == SubTypeCounter && p.Subtype == SubTypeCounter, "Unexpected BlockCount");

                                //Check the RtcpData, Name and ApplicationDependentData
                                System.Diagnostics.Debug.Assert(s.RtcpData.Take(Rtcp.ApplicationSpecificReport.NameSize + ApplicationSpecificDataLength).SequenceEqual(NameData.Concat(ApplicationDependentData)) &&
                                    p.Name.SequenceEqual(NameData) &&
                                    s.ApplicationDependentData.Count() == expectedApplicationSpecificDataLength &&
                                    s.ApplicationDependentData.Take(ApplicationSpecificDataLength).SequenceEqual(ApplicationDependentData) &&
                                s.ApplicationDependentData.SequenceEqual(p.ApplicationDependentData), "Unexpected ApplicationDependentData data");

                                //Check the PaddingOctets count
                                System.Diagnostics.Debug.Assert(s.PaddingOctets == p.PaddingOctets, "Unexpected PaddingOctets");

                                //Check all data in the padding but not the padding octet itself.
                                System.Diagnostics.Debug.Assert(s.PaddingData.SequenceEqual(p.PaddingData), "Unexpected PaddingData");
                            }

                        }
                    }
                }
            }
        }
    }
}