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
using System.Text;

namespace Media.Sdp
{
    #region SessionDescriptionLine

    /// <summary>
    /// Low level class for dealing with Sdp lines with a format of 'X=V{st:sv0,sv1;svN}'    
    /// </summary>
    /// <remarks>Should use byte[]</remarks>
    public class SessionDescriptionLine : IEnumerable<String>//, IUpdateable
    {
        #region Statics

        public static SessionDescriptionLine Parse(params string[] lines) //index, count
        {
            int index = 0;

            return SessionDescriptionLine.Parse(lines, ref index);
        }

        public static SessionDescriptionLine Parse(string[] sdpLines, ref int index) //Count?
        {
            string sdpLine = sdpLines[index].Trim();

            //X= should maybe be allowed and continued to be parsed as a line, if more data occurs on the same line and the token is repeated it can be considered a continuation and ignored
            if (sdpLine.Length <= 2) return null;
            else if (sdpLine[1] != SessionDescription.EqualsSign) return null;
            
            char type = sdpLine[0];

            //Invalid Line, other types? (check grammar)
            if (type == default(char)) return null;

            try
            {
                switch (type)
                {
                    case Sdp.Lines.SessionVersionLine.VersionType: return new Sdp.Lines.SessionVersionLine(sdpLines, ref index);
                    case Sdp.Lines.SessionOriginLine.OriginType: return new Sdp.Lines.SessionOriginLine(sdpLines, ref index);
                    case Sdp.Lines.SessionNameLine.NameType: return new Sdp.Lines.SessionNameLine(sdpLines, ref index);
                    case Sdp.Lines.SessionConnectionLine.ConnectionType: return new Sdp.Lines.SessionConnectionLine(sdpLines, ref index);
                    case Sdp.Lines.SessionUriLine.UriType: return new Sdp.Lines.SessionUriLine(sdpLines, ref index);
                    case Sdp.Lines.SessionEmailLine.EmailType: return new Sdp.Lines.SessionEmailLine(sdpLines, ref index);
                    case Sdp.Lines.SessionPhoneNumberLine.PhoneType: return new Sdp.Lines.SessionPhoneNumberLine(sdpLines, ref index);
                    case Sdp.Lines.SessionTimeZoneLine.TimeZoneType: return new Sdp.Lines.SessionTimeZoneLine(sdpLines, ref index);
                    case Sdp.Lines.SessionInformationLine.InformationType: return new Sdp.Lines.SessionInformationLine(sdpLines, ref index);
                    case Sdp.Lines.SessionMediaDescriptionLine.MediaDescriptionType: return new Sdp.Lines.SessionMediaDescriptionLine(sdpLines, ref index);
                    case Sdp.Lines.SessionTimeDescriptionLine.TimeType: return new Sdp.Lines.SessionTimeDescriptionLine(sdpLines, ref index);
                    case Sdp.Lines.SessionRepeatTimeLine.RepeatType: return new Sdp.Lines.SessionRepeatTimeLine(sdpLines, ref index);
                    case Sdp.Lines.SessionAttributeLine.AttributeType: return new Sdp.Lines.SessionAttributeLine(sdpLines, ref index);
                    case Sdp.Lines.SessionBandwidthLine.BandwidthType: return new Sdp.Lines.SessionBandwidthLine(sdpLines, ref index);
                    default:
                        {
                            ++index;
                            return new SessionDescriptionLine(sdpLine);
                        }
                }
            }
            catch
            {
                throw;
            }
        }

        public static bool TryParse(string[] sdpLines, ref int index, out SessionDescriptionLine result)
        {
            try
            {
                result = Parse(sdpLines, ref index);

                return result != null;
            }
            catch
            {
                result = null;

                return false;
            }
        }

        //static char[] char4 = new char[] { default(char), SessionDescription.EqualsSign, SessionDescription.NewLine, SessionDescription.LineFeed };

        ////Ugly but saves a little memory at the cost of contention.
        //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        //static int CalucateRequiredLength(Encoding enc, char type)
        //{            
        //    char4[0] = type;

        //    return enc.GetByteCount(char4);
        //}

        //ChangeType.

        #endregion

        #region Fields

        //[Common.Attributes.NotReadOnlyForEfficiency]
        internal char m_Type;

        //array would allow easier parsing,
        internal readonly protected string m_Seperator = SessionDescription.SemiColon.ToString();

        internal readonly protected List<string> m_Parts;

        internal readonly Encoding m_Encoding = SessionDescription.DefaultEncoding;

        //bool m_AllowWhiteSpace, m_AssumedPart.

        //IUpdateable, IUsable

        #endregion

        #region Properties

        /// <summary>
        /// Gets the Encoding of the line.
        /// </summary>
        Encoding Encoding { get { return m_Encoding; } }

        /// <summary>
        /// Gets the Type of the line.
        /// </summary>
        public char Type { get { return m_Type; } }
        
        /// <summary>
        /// Gets the Parts of the line.
        /// </summary>
        public IEnumerable<string> Parts { get { return m_Parts; } }

        /// <summary>
        /// Gets the known Count of <see cref="Parts"/>
        /// </summary>
        public int PartsCount { get { return m_Parts.Count; } }

        /// <summary>
        /// Calculates the length in bytes of this line.
        /// </summary>
        public int Length
        {
            get
            {
                //return 2 + m_Parts.Sum(p => p.Length) + (m_Parts.Count > 0 ? m_Seperator.Length * m_Parts.Count - 1 : 0) + 2;

                int total = 0;

                int seperatorSize = m_Encoding.GetByteCount(m_Seperator);

                //Iterate the parts
                foreach (string part in m_Parts)
                {
                    //If after the first part then add the seperator
                    if (total > 0) total += seperatorSize;

                    total += m_Encoding.GetByteCount(part);
                }
                                                                //Each part gets a type, =, all parts are joined with 'm_Seperator' and lines are ended with `\r\n\`.
                //return total + m_Encoding.GetByteCount(new char[] { m_Type, SessionDescription.EqualsSign, SessionDescription.NewLine, SessionDescription.LineFeed });                

                //Sorta efficiently as the array creation is implicit
                return total + Common.Extensions.Encoding.EncodingExtensions.GetByteCount(m_Encoding, m_Type, SessionDescription.EqualsSign, SessionDescription.NewLine, SessionDescription.LineFeed);

                //return total + CalucateRequiredLength(m_Encoding, m_Type); ;

            }
        }        

        //UnderModification

        #endregion

        #region Methods

        //Not needed, could also add an action parameter to the interface to support any type of action (BeginUpdateWith) (EndUpdateFor)

        //BeginUpdate

        //EndUpdate

        //Inline

        /// <summary>
        /// Gets the part with the specified index.
        /// </summary>
        /// <param name="index">The index</param>
        /// <returns>String.Empty if the result was out of range, otherwise the value at the specified index.</returns>
        internal string GetPart(int index)
        {
            return m_Parts.Count > index ? m_Parts[index] : string.Empty;
        }

        //Inline
        /// <summary>
        /// Gets the part at the specified index. If the value is null then String.Empty is used instead.
        /// </summary>
        /// <param name="index">The index</param>
        /// <param name="value">The value</param>
        internal void SetPart(int index, string value)
        {
            if (value == null) value = string.Empty;

            if (m_Parts.Count > index) m_Parts[index] = value;
        }

        internal void EnsureParts(int count)
        {
            while (m_Parts.Count < count) m_Parts.Add(string.Empty);

            //if(count > Count) Array.Resize(ref m_Parts, count);
        }

        internal void Insert(int index, string part)
        {
            //ArrayHelpers
            m_Parts.Insert(index, part);
        }

        internal void Add(string part)
        {
            //ArrayHelpers
            m_Parts.Add(part);
        }

        internal void RemoveAt(int index)
        {
            //ArrayHelpers
            m_Parts.RemoveAt(index);
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Copies an existing instance
        /// </summary>
        /// <param name="other">The instance to copy.</param>
        public SessionDescriptionLine(SessionDescriptionLine other)
        {
            if (other == null) throw new ArgumentNullException();

            m_Encoding = other.m_Encoding;

            //Copies ValueType
            m_Type = other.m_Type;

            m_Seperator = other.m_Seperator;

            m_Parts = other.m_Parts;
        }

        /// <summary>
        /// Constructs a new SessionDescriptionLine with the given type
        /// </summary>
        /// <param name="type">The type of the line</param>
        public SessionDescriptionLine(char type, int partCount = 0)
        {
            m_Parts = new List<string>(partCount);
            
            EnsureParts(partCount);

            m_Type = type;
        }

        /// <summary>
        /// Constructs a new SessionDescriptionLine with the given type and seperator
        /// </summary>
        /// <param name="type"></param>
        /// <param name="seperator"></param>
        public SessionDescriptionLine(char type, string seperator, int partCount = 0)
            : this(type, partCount)
        {
            //Does not allow null or empty seperator
            if (string.IsNullOrEmpty(seperator)) seperator = SessionDescription.SpaceString;
            
            //Assign value
            m_Seperator = seperator;
        }

        /// <summary>
        /// Parses and creates a SessionDescriptionLine from the given line
        /// </summary>
        /// <param name="line">The line from a SessionDescription</param>
        public SessionDescriptionLine(string line, string seperator = null, int partCount = -1)
        {
            //Trim the line (Trim Line Value)?
            line = line.Trim();

            //Validate the line.

            //m_AllowWhiteSpace &&
            if (string.IsNullOrWhiteSpace(line)) throw new InvalidOperationException("line cannot be null or consist only of whitespace");

            //m_AssumedPart &&
            if (line.Length < 2 
                || 
                line[1] != SessionDescription.EqualsSign) Media.Common.TaggedExceptionExtensions.RaiseTaggedException(this, "Invalid SessionDescriptionLine: \"" + line + "\"");

            if (false == string.IsNullOrEmpty(seperator)) m_Seperator = seperator;

            //Assign the type (move up with m_AssumedPart && above)
            m_Type = char.ToLower(line[0]);

            //Split the parts (creates new string array)
            
            //a=<flag>|<name>|:<value> where value = {...,...,...;x;y;z}

            if (partCount > 0)
                m_Parts = new List<string>(line.Substring(2).Split(Common.Extensions.Object.ObjectExtensions.ToArray<string>(m_Seperator), partCount, StringSplitOptions.RemoveEmptyEntries));
            else
                m_Parts = new List<string>(line.Substring(2).Split(Common.Extensions.Object.ObjectExtensions.ToArray<string>(m_Seperator), StringSplitOptions.RemoveEmptyEntries));

            //Should have option to throw less parts than expected or truncate extra parts?
            EnsureParts(partCount);

            //m_Parts = new List<string>(line.Substring(2).Split(SessionDescription.SemiColonSplit));
        }

        public SessionDescriptionLine(string[] sdpLines, ref int index)
            : this(sdpLines[index++]) { }

        public SessionDescriptionLine(string[] sdpLines, ref int index, string seperator, char expected, int partCount = 0)            
            :this(sdpLines[index++], seperator)
        {
            if (m_Type != expected) throw new InvalidOperationException("Expected: " + expected + ", Found: " + m_Type);

            //Should have option to throw less parts than expected or truncate extra parts?
            EnsureParts(partCount);            
        }

        [CLSCompliant(false)]
        public SessionDescriptionLine(string[] sdpLines, int index, string seperator, char expected, int partCount = 0)
            : this(sdpLines, ref index, seperator, expected, partCount) { }

        #endregion

        #region Overrides

        public override int GetHashCode()
        {
            return m_Type ^ m_Parts.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (System.Object.ReferenceEquals(this, obj)) return true;

            if (false == (obj is SessionDescriptionLine)) return false;

            SessionDescriptionLine other = obj as SessionDescriptionLine;

            return other.Encoding == m_Encoding
                &&
                other.m_Type == m_Type
                &&
                other.m_Seperator == m_Seperator                                
                &&
                other.Length == Length
                && //Calling ToString without taking account case
                string.Compare(ToString(), other.ToString(), true) == 0;
        }

        //ToString should be implemented by GetEnumerator and String.Join(string.Empty, GetEnumerator)

        /// <summary>
        /// The string representation of the SessionDescriptionLine including the required new lines.
        /// </summary>
        /// <returns>The string representation of the SessionDescriptionLine including the required new lines.</returns>
        public override string ToString()
        {
            StringBuilder result;

            try
            {
                result = new StringBuilder();

                result.Append(m_Type);

                result.Append(SessionDescription.EqualsSign);

                //Add all parts with seperator
                //Could also loop parts and add seperator
                result.Append(string.Join(m_Seperator, m_Parts));

                result.Append(SessionDescription.NewLineString);

                return result.ToString();
            }
            catch
            {
                throw;
            }
            finally
            {
                result = null;
            }

            //Use IEnumerable overload
            //return Type.ToString() + SessionDescription.EqualsSign + string.Join(m_Seperator, m_Parts) + SessionDescription.NewLineString;

            //using IEnumerable join
            //return string.Join(string.Empty, GetEnumerator());
        }

        #endregion

        //Could have ToString overload with (bool newLine) etc.

        #region Prepare

        public IEnumerable<string> Prepare(bool type, bool equals, bool parts, bool seperator, int partStart, int partCount, string partSeperator = null, bool newLine = true)
        {
            //Widens char to string

            //Include the type if desired.
            if(type) yield return m_Type.ToString();

            //Inlcude the equals size is desired
            if(equals) yield return SessionDescription.EqualsSign.ToString();

            //Track the amount of parts output.
            int count = 0;

            //Iterate the parts
            foreach (string part in m_Parts)
            {
                //Skip to desired part
                if (--partStart >= 0) continue;

                //Include the seperator only after the first part
                if (count++ > 0 && seperator) yield return partSeperator ?? m_Seperator;

                //return the part
                yield return part;

                //If more than the desired parts have been output stop
                if (count > partCount) break;
            }

            //Include the newLine if desired.
            if (newLine) yield return SessionDescription.NewLineString;
        }


        public IEnumerable<string> Prepare()
        {
            return Prepare(true, true, true, true, 0, m_Parts.Count, m_Seperator, true);
        }

        #endregion

        public IEnumerator<string> GetEnumerator()
        {
            //Widens char to string
            yield return m_Type.ToString();

            yield return SessionDescription.EqualsSign.ToString();

            int count = 0;

            foreach (string part in m_Parts)
            {
                if (count++ > 0) yield return m_Seperator;

                yield return part;
            }

            yield return SessionDescription.NewLineString;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<string>)this).GetEnumerator();
        }

        #region Operators

        public static bool operator ==(SessionDescriptionLine a, SessionDescriptionLine b)
        {
            object boxA = a, boxB = b;
            return boxA == null ? boxB == null : a.Equals(b);
        }

        public static bool operator !=(SessionDescriptionLine a, SessionDescriptionLine b) { return false == (a == b); }

        #endregion
    }

    #endregion
}
