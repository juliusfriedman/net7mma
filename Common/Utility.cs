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
using System.Net;
using System.Net.Sockets;
using Media.Common;

#endregion

namespace Media
{
    /// <summary>
    /// Contains common functions
    /// </summary>
    [CLSCompliant(false)]
    public static class Utility
    {
        public static byte[] Empty = new byte[0];        

        public const String UnknownString = "Unknown";

        public static void TryWaitOnHandleAndDispose(ref System.Threading.WaitHandle handle)
        {

            if (handle == null) return;

            try
            {
                handle.WaitOne();
            }
            catch(ObjectDisposedException)
            {
                return;
            }
            catch(Exception ex)
            {
               Media.Common.Extensions.Exception.ExceptionExtensions.TryRaiseTaggedException(handle, "An exception occured while waiting.", ex);
            }
            finally
            {
                if(handle != null) handle.Dispose();
            }

            handle = null;
        }

        public static bool TrySignalHandle(System.Threading.WaitHandle handle, int timeoutMsec = (int)Media.Common.Extensions.TimeSpan.TimeSpanExtensions.MicrosecondsPerMillisecond, bool exitContext = false)
        {
            return System.Threading.WaitHandle.SignalAndWait(handle, handle, timeoutMsec, exitContext);
        }

        #region Properties

        public static System.Security.Cryptography.MD5 CreateMD5HashAlgorithm() { return System.Security.Cryptography.MD5.Create(); }

        public static Random Random = new Random();

        #endregion

        #region Hex Functions

        public static byte HexCharToByte(char c) { c = char.ToUpperInvariant(c); return (byte)(c > '9' ? c - 'A' + 10 : c - '0'); }

        /// <summary>
        /// Converts a String in the form 0011AABB to a Byte[] using the chars in the string as bytes to caulcate the decimal value.
        /// </summary>
        /// <notes>
        /// Reduced string allocations from managed version substring
        /// About 10 milliseconds faster then Managed when doing it 100,000 times. otherwise no change
        /// </notes>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] HexStringToBytes(string str, int start = 0, int length = -1)
        {
            if (length == 0) return null;
            if (length <= -1) length = str.Length;
            if (start > length - start) throw new ArgumentOutOfRangeException("start");
            if (length > length - start) throw new ArgumentOutOfRangeException("length");
            List<byte> result = new List<byte>(length / 2);
            //Dont check the results for overflow
            unchecked
            {
                //Iterate the pointer using the managed length ....
                for (int i = start, e = length; i < e; i += 2)
                {
                    //to reduce string manipulations pre call
                    //while (str[i] == '-') i++;

                    //Conver 2 Chars to a byte
                    result.Add((byte)(HexCharToByte(str[i]) << 4 | HexCharToByte(str[i + 1])));
                }
            }

            //Dont use a List..

            //Return the bytes
            return result.ToArray();
        }       

        #endregion

        internal static char[] CredentialSplit = new char[] { (char)Common.ASCII.AtSign, (char)Common.ASCII.ForwardSlash, (char)Common.ASCII.BackSlash, (char)Common.ASCII.Colon };

        public static NetworkCredential ParseUserInfo(Uri uri)
        {
            if (uri == null || string.IsNullOrWhiteSpace(uri.UserInfo)) return null;

            NetworkCredential result = null;

            //Split into tokens taking only 3 tokens max
            string[] parts = uri.UserInfo.Split(CredentialSplit, 3);

            //cache the length of the split
            int partsLength = parts.Length;

            //If there are atleast two tokens
            if (partsLength > 1)
            {
                //If there is a domain use it
                if (partsLength > 2)
                {
                    result = new NetworkCredential(parts[0], parts[2], parts[1]);
                }
                else //Use the username and password. (optionally use the host as the domain)
                {
                    result = new NetworkCredential(parts[0], parts[1]);//, uri.Host);
                }
            }//There was only one token?

            return result;
        }

        public static bool TryParseUserInfo(Uri uri, out NetworkCredential result) { return (result = ParseUserInfo(uri)) != null; }

        #region byte Array Helper Functions        

        /// <summary>
        /// Indicates the position of the match in a given buffer to a given set of octets.
        /// If the match fails the start parameter will reflect the position of the last partial match, otherwise it will be incremented by <paramref name="octetCount"/>
        /// Additionally start and count will reflect the position of the last partially matched byte, E.g. if 1 octets were match start was incremented by 1.
        /// </summary>
        /// <param name="buffer">The bytes to search</param>
        /// <param name="start">The 0 based index to to start the forward based search</param>
        /// <param name="count">The amount of bytes to search in the buffer</param>
        /// <param name="octets">The bytes to search for</param>
        /// <param name="octetStart">The 0 based offset in the octets to search from</param>
        /// <param name="octetCount">The amount of octets required for a successful match</param>
        /// <returns>
        /// -1 if the match failed or could not be performed; otherwise,
        /// the position within the buffer reletive to the start position in which the first occurance of octets given the octetStart and octetCount was matched.
        /// If more than 1 octet is required for a match and the buffer does not encapsulate the entire match start will still reflect the occurance of the partial match.
        /// </returns>
        public static int ContainsBytes(this byte[] buffer, ref int start, ref int count, byte[] octets, int octetStart, int octetCount)
        {
            //If the buffer or the octets are null no dice
            if (buffer == null || octets == null) return -1;

            //Cache the lengths
            int bufferLength = buffer.Length, octetsLength = octets.Length;

            //Make sure there is no way to run out of bounds given correct input
            if (bufferLength < octetCount || start + octetCount > bufferLength || octetCount > octetsLength) return -1;

            //Nothing to search nothing to return, leave start where it was.
            if (octetCount == 0 && bufferLength == 0 || count == 0) return -1;

            //Create the variables we will use in the searching process
            int checkedBytes = 0, matchedBytes = 0, lastPosition = -1;

            //Ensure we account for the bytes checked.
            int position = start + checkedBytes,
                depth = bufferLength - position;

            //Loop the buffer from start to count while the checkedBytes has not increased past the amount of octets required.
            while (count > 0 && 
                start < bufferLength && 
                matchedBytes < octetCount)
            {
                //Find the next occurance of the required octet storing the result in lastPosition
                //If the result occured after the start
                if ((lastPosition = Array.IndexOf<byte>(buffer, octets[checkedBytes++], position, depth)) >= start)
                {
                    //Check for completion
                    if (++matchedBytes == octetCount) break;

                    //Partial match only
                    start = lastPosition;
                }
                else
                {
                    //No bytes were matched
                    matchedBytes = 0;

                    //Check for another possible match
                    if (checkedBytes < octetsLength) continue;

                    //The match failed at the current offset
                    checkedBytes = 0;

                    //Move the position
                    start += depth;

                    //Decrease the amount which remains
                    count -= depth;
                }
            }

            //start now reflects the position after a parse occurs

            //Return the last position of the partial match
            return lastPosition;
        }

        public static int Find(this byte[] array, byte[] needle, int startIndex, int sourceLength)
        {
            int needleLen = needle.Length;

            int index;

            while (sourceLength >= needleLen)
            {
                // find needle's starting element
                index = Array.IndexOf(array, needle[0], startIndex, sourceLength - needleLen + 1);

                // if we did not find even the first element of the needls, then the search is failed
                if (index == -1)
                    return -1;

                int i, p;
                // check for needle
                for (i = 0, p = index; i < needleLen; i++, p++)
                {
                    if (array[p] != needle[i])
                    {
                        break;
                    }
                }

                if (i == needleLen)
                {
                    // needle was found
                    return index;
                }

                // continue to search for needle
                sourceLength -= (index - startIndex + 1);
                startIndex = index + 1;
            }
            return -1;
        }

        #endregion

        #region Clamp

        public static byte Clamp(byte value, byte min, byte max)
        {
            return Math.Min(Math.Max(min, value), max);
        }

        public static int Clamp(int value, int min, int max)
        {
            return Math.Min(Math.Max(min, value), max);
        }

        public static long Clamp(long value, long min, long max)
        {
            return Math.Min(Math.Max(min, value), max);
        }

        public static double Clamp(double value, double min, double max)
        {
            return Math.Min(Math.Max(min, value), max);
        }

        #endregion

        #region RgbYuv.cs

        /// <summary>
        /// Provides cached RGB to YUV lookup without alpha support.
        /// </summary>
        /// <remarks>
        /// This class is public so a user can manually load and unload the lookup table.
        /// Looking up a color calculates the lookup table if not present.
        /// All methods except UnloadLookupTable should be thread-safe, although there will be a performance overhead if GetYUV is called while Initialize has not finished.
        /// </remarks>
        public static class RgbYuv
        {
            const uint RgbMask = 0x00ffffff;

            private static volatile int[] lookupTable;
            private static int[] LookupTable
            {
                get
                {
                    if (lookupTable == null) Initialize();
                    return lookupTable;
                }
            }

            /// <summary>
            /// Gets whether the lookup table is ready.
            /// </summary>
            public static bool Initialized
            {
                get
                {
                    return lookupTable != null;
                }
            }

            /// <summary>
            /// Returns the 24bit YUV equivalent of the provided 24bit RGB color.
            /// <para>Any alpha component is dropped.</para>
            /// </summary>
            /// <param name="rgb">A 24bit rgb color.</param>
            /// <returns>The corresponding 24bit YUV color.</returns>
            public static int GetYuv(uint rgb)
            {
                return LookupTable[rgb & RgbMask];
            }

            /// <summary>
            /// Calculates the lookup table.
            /// </summary>
            public static unsafe void Initialize()
            {
                var lTable = new int[0x1000000]; // 256 * 256 * 256
                fixed (int* lookupP = lTable)
                {
                    byte* lP = (byte*)lookupP;
                    for (uint i = 0; i < lTable.Length; i++)
                    {
                        float r = (i & 0xff0000) >> 16;
                        float g = (i & 0x00ff00) >> 8;
                        float b = (i & 0x0000ff);

                        lP++; //Skip alpha byte
                        *(lP++) = (byte)(.299 * r + .587 * g + .114 * b);
                        *(lP++) = (byte)((int)(-.169 * r - .331 * g + .5 * b) + 128);
                        *(lP++) = (byte)((int)(.5 * r - .419 * g - .081 * b) + 128);
                    }
                }
                lookupTable = lTable;
            }

            /// <summary>
            /// Releases the reference to the lookup table.
            /// <para>The table has to be calculated again for the next lookup.</para>
            /// </summary>
            public static void UnloadLookupTable()
            {
                lookupTable = null;
            }
        }

        #endregion

        #region Color Conversion Routines

        //Todo standardize

        public static unsafe void Bgr32ToBgr24(byte[] source, int srcOffset, byte[] destination, int destOffset, int pixelCount)
        {
            fixed (byte* sourcePtr = source, destinationPtr = destination)
            {
                var sourceStart = sourcePtr + srcOffset;
                var destinationStart = destinationPtr + destOffset;
                var sourceEnd = sourceStart + 4 * pixelCount;
                var src = sourceStart;
                var dest = destinationStart;
                while (src < sourceEnd)
                {
                    *(dest++) = *(src++);
                    *(dest++) = *(src++);
                    *(dest++) = *(src++);
                    src++;
                }
            }
        }

        public static void FlipVertical(byte[] source, int srcOffset, byte[] destination, int destOffset, int height, int stride)
        {
            var src = srcOffset;
            var dest = destOffset + (height - 1) * stride;
            for (var y = 0; y < height; y++)
            {
                Buffer.BlockCopy(source, src, destination, dest, stride);
                src += stride;
                dest -= stride;
            }
        }

        //Should follow the same api as below...

        internal static unsafe void YUV2RGBManaged(byte[] YUVData, byte[] RGBData, int width, int height)
        {

            //returned pixel format is 2yuv - i.e. luminance, y, is represented for every pixel and the u and v are alternated
            //like this (where Cb = u , Cr = y)
            //Y0 Cb Y1 Cr Y2 Cb Y3 

            /*http://msdn.microsoft.com/en-us/library/ms893078.aspx
             * 
             * C = Y - 16
             D = U - 128
             E = V - 128
             R = clip(( 298 * C           + 409 * E + 128) >> 8)
             G = clip(( 298 * C - 100 * D - 208 * E + 128) >> 8)
             B = clip(( 298 * C + 516 * D           + 128) >> 8)

             * here are a whole bunch more formats for doing this...
             * http://stackoverflow.com/questions/3943779/converting-to-yuv-ycbcr-colour-space-many-versions
             */


            fixed (byte* pRGBs = RGBData, pYUVs = YUVData)
            {
                for (int r = 0; r < height; r++)
                {
                    byte* pRGB = pRGBs + r * width * 3;
                    byte* pYUV = pYUVs + r * width * 2;

                    //process two pixels at a time
                    for (int c = 0; c < width; c += 2)
                    {
                        int C1 = pYUV[1] - 16;
                        int C2 = pYUV[3] - 16;
                        int D = pYUV[2] - 128;
                        int E = pYUV[0] - 128;

                        int R1 = (298 * C1 + 409 * E + 128) >> 8;
                        int G1 = (298 * C1 - 100 * D - 208 * E + 128) >> 8;
                        int B1 = (298 * C1 + 516 * D + 128) >> 8;

                        int R2 = (298 * C2 + 409 * E + 128) >> 8;
                        int G2 = (298 * C2 - 100 * D - 208 * E + 128) >> 8;
                        int B2 = (298 * C2 + 516 * D + 128) >> 8;
#if true
                        //check for overflow
                        //unsurprisingly this takes the bulk of the time.
                        pRGB[0] = (byte)(R1 < 0 ? 0 : R1 > 255 ? 255 : R1);
                        pRGB[1] = (byte)(G1 < 0 ? 0 : G1 > 255 ? 255 : G1);
                        pRGB[2] = (byte)(B1 < 0 ? 0 : B1 > 255 ? 255 : B1);

                        pRGB[3] = (byte)(R2 < 0 ? 0 : R2 > 255 ? 255 : R2);
                        pRGB[4] = (byte)(G2 < 0 ? 0 : G2 > 255 ? 255 : G2);
                        pRGB[5] = (byte)(B2 < 0 ? 0 : B2 > 255 ? 255 : B2);
#else
                    pRGB[0] = (byte)(R1);
                    pRGB[1] = (byte)(G1);
                    pRGB[2] = (byte)(B1);

                    pRGB[3] = (byte)(R2);
                    pRGB[4] = (byte)(G2);
                    pRGB[5] = (byte)(B2);
#endif

                        pRGB += 6;
                        pYUV += 4;
                    }
                }
            }
        }

        public static unsafe byte[] ABGRA2YUV420Managed(int width, int height, IntPtr scan0)
        {

            int frameSize = width * height;
            int chromasize = frameSize / 4;

            int yIndex = 0;
            int uIndex = frameSize;
            int vIndex = frameSize + chromasize;
            byte[] yuv = new byte[frameSize * 3 / 2];

            uint* rgbValues = (uint*)scan0.ToPointer();

            int index = 0;

            //Parrallel
            try
            {
                for (int j = 0; j < height; j++)
                {
                    for (int i = 0; i < width; i++)
                    {
                        uint B = (rgbValues[index] & 0xff000000) >> 24;
                        uint G = (rgbValues[index] & 0xff0000) >> 16;
                        uint R = (rgbValues[index] & 0xff00) >> 8;
                        uint a = (rgbValues[index] & 0xff) >> 0;

                        //int yuvC = Utility.RgbYuv.GetYuv(Common.Binary.ReverseU32(rgbValues[index]));

                        uint Y = ((66 * R + 129 * G + 25 * B + 128) >> 8) + 16;
                        uint U = (uint)(((-38 * R - 74 * G + 112 * B + 128) >> 8) + 128);
                        uint V = ((112 * R - 94 * G - 18 * B + 128) >> 8) + 128;

                        yuv[yIndex++] = (byte)((Y < 0) ? 0 : ((Y > 255) ? 255 : Y));// (byte)((yuvC & 0xff0000) >> 16); //

                        if (j % 2 == 0 && index % 2 == 0)
                        {
                            yuv[uIndex++] = (byte)((U < 0) ? 0 : ((U > 255) ? 255 : U));//(byte)((yuvC  & 0xff00) >> 8);//
                            yuv[vIndex++] = (byte)((V < 0) ? 0 : ((V > 255) ? 255 : V));// (byte)((yuvC & 0xff) >> 0);//
                        }

                        index++;
                    }
                }
                
            }
            catch
            {
                throw;
            }

            return yuv;
        }

        #endregion        
        
        /// <summary>
        /// Identifies various formats for Audio Encodings
        /// </summary>
        public enum WaveFormatId : ushort
        {
            /// <summary>UNKNOWN,	Microsoft Corporation</summary>
            Unknown = 0x0000,
            /// <summary>PCM		Microsoft Corporation</summary>
            Pcm = 0x0001,
            /// <summary>ADPCM		Microsoft Corporation</summary>
            Adpcm = 0x0002,
            /// <summary>IEEE_FLOAT Microsoft Corporation</summary>
            IeeeFloat = 0x0003,
            /// <summary>VSELP		Compaq Computer Corp.</summary>
            Vselp = 0x0004,
            /// <summary>IBM_CVSD	IBM Corporation</summary>
            IbmCvsd = 0x0005,
            /// <summary>ALAW		Microsoft Corporation</summary>
            ALaw = 0x0006,
            /// <summary>MULAW		Microsoft Corporation</summary>
            MuLaw = 0x0007,
            /// <summary>DTS		Microsoft Corporation</summary>
            Dts = 0x0008,
            /// <summary>DRM		Microsoft Corporation</summary>
            Drm = 0x0009,
            /// <summary>OKI	OKI</summary>
            OkiAdpcm = 0x0010,
            /// <summary>DVI	Intel Corporation</summary>
            DviAdpcm = 0x0011,
            /// <summary>IMA  Intel Corporation</summary>
            ImaAdpcm = DviAdpcm,
            /// <summary>MEDIASPACE Videologic</summary>
            MediaspaceAdpcm = 0x0012,
            /// <summary>SIERRA Sierra Semiconductor Corp </summary>
            SierraAdpcm = 0x0013,
            /// <summary>G723 Antex Electronics Corporation </summary>
            G723Adpcm = 0x0014,
            /// <summary>DIGISTD DSP Solutions, Inc.</summary>
            DigiStd = 0x0015,
            /// <summary>DIGIFIX DSP Solutions, Inc.</summary>
            DigiFix = 0x0016,
            /// <summary>DIALOGIC_OKI Dialogic Corporation</summary>
            DialogicOkiAdpcm = 0x0017,
            /// <summary>MEDIAVISION Media Vision, Inc.</summary>
            MediaVisionAdpcm = 0x0018,
            /// <summary>CU_CODEC Hewlett-Packard Company </summary>
            CUCodec = 0x0019,
            /// <summary>YAMAHA Yamaha Corporation of America</summary>
            YamahaAdpcm = 0x0020,
            /// <summary>SONARC Speech Compression</summary>
            SonarC = 0x0021,
            /// <summary>DSPGROUP_TRUESPEECH DSP Group, Inc </summary>
            DspGroupTrueSpeech = 0x0022,
            /// <summary>ECHOSC1 Echo Speech Corporation</summary>
            EchoSpeechCorporation1 = 0x0023,
            /// <summary>AUDIOFILE_AF36, Virtual Music, Inc.</summary>
            AudioFileAf36 = 0x0024,
            /// <summary>APTX Audio Processing Technology</summary>
            Aptx = 0x0025,
            /// <summary>AUDIOFILE_AF10, Virtual Music, Inc.</summary>
            AudioFileAf10 = 0x0026,
            /// <summary>PROSODY_1612, Aculab plc</summary>
            Prosody1612 = 0x0027,
            /// <summary>LRC, Merging Technologies S.A. </summary>
            Lrc = 0x0028,
            /// <summary>DOLBY_AC2, Dolby Laboratories</summary>
            DolbyAc2 = 0x0030,
            /// <summary>GSM610, Microsoft Corporation</summary>
            Gsm610 = 0x0031,
            /// <summary>MSNAUDIO, Microsoft Corporation</summary>
            MsnAudio = 0x0032,
            /// <summary>ANTEXE, Antex Electronics Corporation</summary>
            AntexAdpcme = 0x0033,
            /// <summary>CONTROL_RES_VQLPC, Control Resources Limited </summary>
            ControlResVqlpc = 0x0034,
            /// <summary>DIGIREAL, DSP Solutions, Inc. </summary>
            DigiReal = 0x0035,
            /// <summary>DIGIADPCM, DSP Solutions, Inc.</summary>
            DigiAdpcm = 0x0036,
            /// <summary>CONTROL_RES_CR10, Control Resources Limited</summary>
            ControlResCr10 = 0x0037,
            /// <summary>Natural MicroSystems </summary>
            NMS_VBXADPCM = 0x0038,
            /// <summary>Crystal Semiconductor IMA ADPCM </summary>
            CS_IMAADPCM = 0x0039, // 
            /// <summary>Echo Speech Corporation </summary>
            ECHOSC3 = 0x003A, // 
            /// <summary>Rockwell International </summary>
            ROCKWELL = 0x003B, // 
            /// <summary>Rockwell International </summary>
            ROCKWELL_DIGITALK = 0x003C, // Rockwell International 
            /// <summary>Xebec Multimedia Solutions Limited </summary>
            XEBEC = 0x003D, // 
            /// <summary>Antex Electronics Corporation </summary>
            G721 = 0x0040, // 
            /// <summary>Antex Electronics Corporation </summary>
            G728 = 0x0041, // 
            /// <summary></summary>
            MSG723 = 0x0042, // Microsoft Corporation 
            /// <summary></summary>
            Mpeg = 0x0050, // MPEG, Microsoft Corporation 
            /// <summary></summary>
            RT24 = 0x0052, // InSoft, Inc. 
            /// <summary></summary>
            PAC = 0x0053, // InSoft, Inc. 
            /// <summary></summary>
            MpegLayer3 = 0x0055, // MPEGLAYER3, ISO/MPEG Layer3 Format Tag 
            /// <summary></summary>
            LUCENT_G723 = 0x0059, // Lucent Technologies 
            /// <summary></summary>
            CIRRUS = 0x0060, // Cirrus Logic 
            /// <summary></summary>
            ESPCM = 0x0061, // ESS Technology 
            /// <summary></summary>
            VOXWARE = 0x0062, // Voxware Inc 
            /// <summary></summary>
            CANOPUS_ATRAC = 0x0063, // Canopus, co., Ltd. 
            /// <summary></summary>
            G726 = 0x0064, // APICOM 
            /// <summary></summary>
            G722 = 0x0065, // APICOM 
            /// <summary></summary>
            DSAT_DISPLAY = 0x0067, // Microsoft Corporation 
            /// <summary></summary>
            VOXWARE_BYTE_ALIGNED = 0x0069, // Voxware Inc 
            /// <summary></summary>
            VOXWARE_AC8 = 0x0070, // Voxware Inc 
            /// <summary></summary>
            VOXWARE_AC10 = 0x0071, // Voxware Inc 
            /// <summary></summary>
            VOXWARE_AC16 = 0x0072, // Voxware Inc 
            /// <summary></summary>
            VOXWARE_AC20 = 0x0073, // Voxware Inc 
            /// <summary></summary>
            VOXWARE_RT24 = 0x0074, // Voxware Inc 
            /// <summary></summary>
            VOXWARE_RT29 = 0x0075, // Voxware Inc 
            /// <summary></summary>
            VOXWARE_RT29HW = 0x0076, // Voxware Inc 
            /// <summary></summary>
            VOXWARE_VR12 = 0x0077, // Voxware Inc 
            /// <summary></summary>
            VOXWARE_VR18 = 0x0078, // Voxware Inc 
            /// <summary></summary>
            VOXWARE_TQ40 = 0x0079, // Voxware Inc 
            /// <summary></summary>
            SOFTSOUND = 0x0080, // Softsound, Ltd. 
            /// <summary></summary>
            VOXWARE_TQ60 = 0x0081, // Voxware Inc 
            /// <summary></summary>
            MSRT24 = 0x0082, // Microsoft Corporation 
            /// <summary></summary>
            G729A = 0x0083, // AT&T Labs, Inc. 
            /// <summary></summary>
            MVI_MVI2 = 0x0084, // Motion Pixels 
            /// <summary></summary>
            DF_G726 = 0x0085, // DataFusion Systems (Pty) (Ltd) 
            /// <summary></summary>
            DF_GSM610 = 0x0086, // DataFusion Systems (Pty) (Ltd) 
            /// <summary></summary>
            ISIAUDIO = 0x0088, // Iterated Systems, Inc. 
            /// <summary></summary>
            ONLIVE = 0x0089, // OnLive! Technologies, Inc. 
            /// <summary></summary>
            SBC24 = 0x0091, // Siemens Business Communications Sys 
            /// <summary></summary>
            DOLBY_AC3_SPDIF = 0x0092, // Sonic Foundry 
            /// <summary></summary>
            MEDIASONIC_G723 = 0x0093, // MediaSonic 
            /// <summary></summary>
            PROSODY_8KBPS = 0x0094, // Aculab plc 
            /// <summary></summary>
            ZYXEL = 0x0097, // ZyXEL Communications, Inc. 
            /// <summary></summary>
            PHILIPS_LPCBB = 0x0098, // Philips Speech Processing 
            /// <summary></summary>
            PACKED = 0x0099, // Studer Professional Audio AG 
            /// <summary></summary>
            MALDEN_PHONYTALK = 0x00A0, // Malden Electronics Ltd. 
            /// <summary>GSM</summary>
            Gsm = 0x00A1,
            /// <summary>G729</summary>
            G729 = 0x00A2,
            /// <summary>G723</summary>
            G723 = 0x00A3,
            /// <summary>ACELP</summary>
            Acelp = 0x00A4,
            /// <summary></summary>
            RHETOREX = 0x0100, // Rhetorex Inc. 
            /// <summary></summary>
            IRAT = 0x0101, // BeCubed Software Inc. 
            /// <summary></summary>
            VIVO_G723 = 0x0111, // Vivo Software 
            /// <summary></summary>
            VIVO_SIREN = 0x0112, // Vivo Software 
            /// <summary></summary>
            DIGITAL_G723 = 0x0123, // Digital Equipment Corporation 
            /// <summary></summary>
            SANYO_LD = 0x0125, // Sanyo Electric Co., Ltd. 
            /// <summary></summary>
            SIPROLAB_ACEPLNET = 0x0130, // Sipro Lab Telecom Inc. 
            /// <summary></summary>
            SIPROLAB_ACELP4800 = 0x0131, // Sipro Lab Telecom Inc. 
            /// <summary></summary>
            SIPROLAB_ACELP8V3 = 0x0132, // Sipro Lab Telecom Inc. 
            /// <summary></summary>
            SIPROLAB_G729 = 0x0133, // Sipro Lab Telecom Inc. 
            /// <summary></summary>
            SIPROLAB_G729A = 0x0134, // Sipro Lab Telecom Inc. 
            /// <summary></summary>
            SIPROLAB_KELVIN = 0x0135, // Sipro Lab Telecom Inc. 
            /// <summary></summary>
            G726ADPCM = 0x0140, // Dictaphone Corporation 
            /// <summary></summary>
            QUALCOMM_PUREVOICE = 0x0150, // Qualcomm, Inc. 
            /// <summary></summary>
            QUALCOMM_HALFRATE = 0x0151, // Qualcomm, Inc. 
            /// <summary></summary>
            TUBGSM = 0x0155, // Ring Zero Systems, Inc. 
            /// <summary></summary>
            MSAUDIO1 = 0x0160, // Microsoft Corporation 		
            /// <summary>
            /// WMAUDIO2, Microsoft Corporation
            /// </summary>
            WMAUDIO2 = 0x0161,
            /// <summary>
            /// WMAUDIO3, Microsoft Corporation
            /// </summary>
            WMAUDIO3 = 0x0162,
            /// <summary></summary>
            UNISYS_NAP = 0x0170, // Unisys Corp. 
            /// <summary></summary>
            UNISYS_NAP_ULAW = 0x0171, // Unisys Corp. 
            /// <summary></summary>
            UNISYS_NAP_ALAW = 0x0172, // Unisys Corp. 
            /// <summary></summary>
            UNISYS_NAP_16K = 0x0173, // Unisys Corp. 
            /// <summary></summary>
            CREATIVE = 0x0200, // Creative Labs, Inc 
            /// <summary></summary>
            CREATIVE_FASTSPEECH8 = 0x0202, // Creative Labs, Inc 
            /// <summary></summary>
            CREATIVE_FASTSPEECH10 = 0x0203, // Creative Labs, Inc 
            /// <summary></summary>
            UHER = 0x0210, // UHER informatic GmbH 
            /// <summary></summary>
            QUARTERDECK = 0x0220, // Quarterdeck Corporation 
            /// <summary></summary>
            ILINK_VC = 0x0230, // I-link Worldwide 
            /// <summary></summary>
            RAW_SPORT = 0x0240, // Aureal Semiconductor 
            /// <summary></summary>
            ESST_AC3 = 0x0241, // ESS Technology, Inc. 
            /// <summary></summary>
            IPI_HSX = 0x0250, // Interactive Products, Inc. 
            /// <summary></summary>
            IPI_RPELP = 0x0251, // Interactive Products, Inc. 
            /// <summary></summary>
            CS2 = 0x0260, // Consistent Software 
            /// <summary></summary>
            SONY_SCX = 0x0270, // Sony Corp. 
            /// <summary></summary>
            FM_TOWNS_SND = 0x0300, // Fujitsu Corp. 
            /// <summary></summary>
            BTV_DIGITAL = 0x0400, // Brooktree Corporation 
            /// <summary></summary>
            QDESIGN_MUSIC = 0x0450, // QDesign Corporation 
            /// <summary></summary>
            VME_VMPCM = 0x0680, // AT&T Labs, Inc. 
            /// <summary></summary>
            TPC = 0x0681, // AT&T Labs, Inc. 
            /// <summary></summary>
            OLIGSM = 0x1000, // Ing C. Olivetti & C., S.p.A. 
            /// <summary></summary>
            OLIADPCM = 0x1001, // Ing C. Olivetti & C., S.p.A. 
            /// <summary></summary>
            OLICELP = 0x1002, // Ing C. Olivetti & C., S.p.A. 
            /// <summary></summary>
            OLISBC = 0x1003, // Ing C. Olivetti & C., S.p.A. 
            /// <summary></summary>
            OLIOPR = 0x1004, // Ing C. Olivetti & C., S.p.A. 
            /// <summary></summary>
            LH_CODEC = 0x1100, // Lernout & Hauspie 
            /// <summary></summary>
            NORRIS = 0x1400, // Norris Communications, Inc. 
            /// <summary></summary>
            SOUNDSPACE_MUSICOMPRESS = 0x1500, // AT&T Labs, Inc. 
            /// <summary></summary>
            DVM = 0x2000, // FAST Multimedia AG 
            /// <summary>EXTENSIBLE</summary>
            Extensible = 0xFFFE, // Microsoft 
            /// <summary></summary>
            DEVELOPMENT = 0xFFFF,

            // others - not from MS headers
            /// <summary>VORBIS1 "Og" Original stream compatible</summary>
            Vorbis1 = 0x674f,
            /// <summary>VORBIS2 "Pg" Have independent header</summary>
            Vorbis2 = 0x6750,
            /// <summary>VORBIS3 "Qg" Have no codebook header</summary>
            Vorbis3 = 0x6751,
            /// <summary>VORBIS1P "og" Original stream compatible</summary>
            Vorbis1P = 0x676f,
            /// <summary>VORBIS2P "pg" Have independent headere</summary>
            Vorbis2P = 0x6770,
            /// <summary>VORBIS3P "qg" Have no codebook header</summary>
            Vorbis3P = 0x6771,
        }
    }
}
