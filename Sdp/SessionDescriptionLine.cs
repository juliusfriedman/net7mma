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
    /// <remarks>Should use byte[] and should have Encoding / Grammar as a property</remarks>
    public class SessionDescriptionLine : IEnumerable<String>
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
                    case Media.Sdp.Lines.SessionVersionLine.VersionType: return new Media.Sdp.Lines.SessionVersionLine(sdpLines, ref index);
                    case Media.Sdp.Lines.SessionOriginLine.OriginType: return new Media.Sdp.Lines.SessionOriginLine(sdpLines, ref index);
                    case Media.Sdp.Lines.SessionNameLine.NameType: return new Media.Sdp.Lines.SessionNameLine(sdpLines, ref index);
                    case Media.Sdp.Lines.SessionConnectionLine.ConnectionType: return new Media.Sdp.Lines.SessionConnectionLine(sdpLines, ref index);
                    case Media.Sdp.Lines.SessionUriLine.LocationType: return new Media.Sdp.Lines.SessionUriLine(sdpLines, ref index);
                    case Media.Sdp.Lines.SessionEmailLine.EmailType: return new Media.Sdp.Lines.SessionEmailLine(sdpLines, ref index);
                    case Media.Sdp.Lines.SessionPhoneNumberLine.PhoneType: return new Media.Sdp.Lines.SessionPhoneNumberLine(sdpLines, ref index);
                    case 'z': //TimeZone Information
                    case Sdp.Lines.SessionAttributeLine.AttributeType: //Attribute
                    case Sdp.Lines.SessionBandwidthLine.BandwidthType: //Bandwidth
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

        #endregion

        #region Readonly Fields

        //readonly ValueType
        public readonly char Type;

        //array would allow easier parsing,
        internal readonly protected string m_Seperator = SessionDescription.SemiColon.ToString();

        internal readonly protected List<string> m_Parts;

        #endregion

        #region Properties

        //Todo, Use Enumerable.
        public IEnumerable<string> Parts { get { return m_Parts; } }

        /// <summary>
        /// Calculates the length in bytes of this line.
        /// </summary>
        public int Length
        {
            //Each part gets a type, =, all parts are joined with 'm_Seperator' and lines are ended with `\r\n\`.
            get { return 2 + m_Parts.Sum(p => p.Length) + (m_Parts.Count > 0 ? m_Seperator.Length * m_Parts.Count - 1 : 0) + 2; }
        }

        #endregion

        #region Methods

        internal string GetPart(int index) { return m_Parts.Count > index ? m_Parts[index] : string.Empty; }

        internal void SetPart(int index, string value) { if (m_Parts.Count > index) m_Parts[index] = value; }

        internal void EnsureParts(int count)
        {
            while (m_Parts.Count < count) m_Parts.Add(string.Empty);

            //if(count > Count) Array.Resize(ref m_Parts, count);
        }

        internal void Insert(int index, string part)
        {
            m_Parts.Insert(index, part);
        }

        internal void Add(string part)
        {
            m_Parts.Add(part);
        }

        internal void RemoveAt(int index)
        {
            m_Parts.RemoveAt(index);
        }

        #endregion

        #region Constructor

        public SessionDescriptionLine(SessionDescriptionLine other)
        {
            if (other == null) throw new ArgumentNullException();

            Type = other.Type;

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
         
            Type = type;
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
        public SessionDescriptionLine(string line, string seperator = null)
        {
            //Trim the line
            line = line.Trim();

            //Validate the line.

            if (string.IsNullOrWhiteSpace(line)) throw new InvalidOperationException("line cannot be null or consist only of whitespace");
            
            if (line.Length < 2 
                || 
                line[1] != SessionDescription.EqualsSign) Media.Common.Extensions.Exception.ExceptionExtensions.RaiseTaggedException(this, "Invalid SessionDescriptionLine: \"" + line + "\"");

            if (false == string.IsNullOrEmpty(seperator)) m_Seperator = seperator;

            //Assign the type
            Type = char.ToLower(line[0]);

            //Split the parts
            
            //a=<flag>|<name>|:<value> where value = {...,...,...;x;y;z}

            m_Parts = new List<string>(line.Substring(2).Split(new string[] { m_Seperator }, StringSplitOptions.RemoveEmptyEntries));
            
            //m_Parts = new List<string>(line.Substring(2).Split(SessionDescription.SemiColonSplit));
        }

        public SessionDescriptionLine(string[] sdpLines, ref int index)
            : this(sdpLines[++index]) { }

        public SessionDescriptionLine(string[] sdpLines, ref int index, string seperator, char expected, int partCount = 0)            
            :this(sdpLines[++index], seperator)
        {
            if(Type != expected) throw new InvalidOperationException("Expected: " + Type + ", Found: " + Type);

            //Should have option to throw less parts than expected?
            if (partCount > 0) EnsureParts(partCount);
        }

        [CLSCompliant(false)]
        public SessionDescriptionLine(string[] sdpLines, int index, string seperator, char expected, int partCount = 0)
            : this(sdpLines, ref index, seperator, expected, partCount) { }

        //Description derivived type would allow for easier parsing of media and time descirptions, would implicitly be a line and inherit from the Line class...

        ///Could also have it's own m_Lines and store itself in m_Lines... weird but would allow custom ordering...

        //internal class DescriptionLine : SessionDescriptionLine, IEnumerable<SessionDescriptionLine>
        //{
        //    public DescriptionLine(char type, int partCount = 0)
        //        : base(type, partCount)
        //    {

        //    }

        //    public DescriptionLine(char type, int partCount = 0, string seperator = null)
        //        : base(type, seperator, partCount)
        //    {

        //    }

        //    //Could be overridden if not explicit and would allow other lines to be added.
        //    IEnumerator<SessionDescriptionLine> IEnumerable<SessionDescriptionLine>.GetEnumerator()
        //    {
        //        return Common.Extensions.Linq.LinqExtensions.Yield(this).GetEnumerator();
        //    }

        //    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        //    {
        //        return GetEnumerator();
        //    }
        //}

        //Possibly have a Field concept which can be used to join multiple parts but retain a type?..

        #endregion

        #region Overrides

        public override int GetHashCode()
        {
            return Type ^ m_Parts.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (System.Object.ReferenceEquals(this, obj)) return true;

            if (false == (obj is SessionDescriptionLine)) return false;

            SessionDescriptionLine other = obj as SessionDescriptionLine;

            return other.Type == Type
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

                result.Append(Type);

                result.Append(SessionDescription.EqualsSign);

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

        public IEnumerator<string> GetEnumerator()
        {
            //Widens char to string
            yield return Type.ToString();

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
