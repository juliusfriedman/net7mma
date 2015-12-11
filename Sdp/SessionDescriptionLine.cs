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
    /// <remarks>Should use byte[] and should have Encoding as a property</remarks>
    public class SessionDescriptionLine : IEnumerable<String>
    {
        #region Statics

        public static SessionDescriptionLine Parse(string[] sdpLines, ref int index)
        {
            string sdpLine = sdpLines[index] = sdpLines[index].Trim();

            if (sdpLine.Length <= 2) return null;
            else if (sdpLine[1] != SessionDescription.EqualsSign) return null;

            char type = sdpLine[0];

            //Invalid Line
            if (type == default(char)) return null;

            try
            {
                switch (type)
                {
                    case Media.Sdp.Lines.SessionVersionLine.VersionType: return new Media.Sdp.Lines.SessionVersionLine(sdpLines, ref index);
                    case Media.Sdp.Lines.SessionOriginatorLine.OriginatorType: return new Media.Sdp.Lines.SessionOriginatorLine(sdpLines, ref index);
                    case Media.Sdp.Lines.SessionNameLine.NameType: return new Media.Sdp.Lines.SessionNameLine(sdpLines, ref index);
                    case Media.Sdp.Lines.SessionConnectionLine.ConnectionType: return new Media.Sdp.Lines.SessionConnectionLine(sdpLines, ref index);
                    case Media.Sdp.Lines.SessionUriLine.LocationType: return new Media.Sdp.Lines.SessionUriLine(sdpLines, ref index);
                    case Media.Sdp.Lines.SessionEmailLine.EmailType: return new Media.Sdp.Lines.SessionEmailLine(sdpLines, ref index);
                    case Media.Sdp.Lines.SessionPhoneLine.PhoneType: return new Media.Sdp.Lines.SessionPhoneLine(sdpLines, ref index);
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

        static char[] ValueSplit = new char[] { ';' };

        public readonly char Type;

        protected string m_Seperator = SessionDescription.SemiColon.ToString();

        protected List<string> m_Parts;

        public System.Collections.ObjectModel.ReadOnlyCollection<string> Parts { get { return m_Parts.AsReadOnly(); } }

        /// <summary>
        /// Calculates the length in bytes of this line.
        /// </summary>
        public int Length
        {
            //Each part gets a type, =, all parts are joined with 'm_Seperator' and lines are ended with `\r\n\`.
            get { return 2 + m_Parts.Sum(p => p.Length) + (m_Parts.Count > 0 ? m_Seperator.Length * m_Parts.Count - 1 : 0) + 2; }
        }

        internal string GetPart(int index) { return m_Parts.Count > index ? m_Parts[index] : string.Empty; }

        internal void SetPart(int index, string value) { if (m_Parts.Count > index) m_Parts[index] = value; }

        internal void EnsureParts(int count)
        {
            while (m_Parts.Count < count) m_Parts.Add(string.Empty);
        }

        internal void Add(string part)
        {
            m_Parts.Add(part);
        }

        public SessionDescriptionLine(SessionDescriptionLine other)
        {
            m_Parts = other.m_Parts;
            Type = other.Type;
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
        public SessionDescriptionLine(char type, string seperator)
            : this(type, 0)
        {
            if (seperator != null) m_Seperator = seperator;
        }

        /// <summary>
        /// Parses and creates a SessionDescriptionLine from the given line
        /// </summary>
        /// <param name="line">The line from a SessionDescription</param>
        public SessionDescriptionLine(string line)
        {
            if (line == null || line.Length < 2 || line[1] != SessionDescription.EqualsSign) Media.Common.Extensions.Exception.ExceptionExtensions.RaiseTaggedException(this, "Invalid SessionDescriptionLine: \"" + line + "\"");

            Type = char.ToLower(line[0]);

            //Two types 
            //a=<flag>
            //a=<name>:<value> where value = {...,...,...;x;y;z}

            m_Parts = new List<string>(line.Remove(0, 2).Split(ValueSplit));
        }

        /// <summary>
        /// The string representation of the SessionDescriptionLine including the required new lines.
        /// </summary>
        /// <returns>The string representation of the SessionDescriptionLine including the required new lines.</returns>
        public override string ToString()
        {
            return Type.ToString() + SessionDescription.EqualsSign + string.Join(m_Seperator, m_Parts.ToArray()) + SessionDescription.NewLine;
        }


        public IEnumerator<string> GetEnumerator()
        {
            yield return Type.ToString();

            yield return SessionDescription.EqualsSign.ToString();

            foreach (string part in m_Parts)
            {
                yield return part;

                yield return m_Seperator;
            }

            yield return SessionDescription.NewLine;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<string>)this).GetEnumerator();
        }
    }

    #endregion
}
