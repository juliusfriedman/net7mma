//This entire file may not ever be used in the libary, it currently is for testing API styles. Please don't rely on it.
namespace Media.Sdp
{
    ////Basically composes the Lines list and seperates the functionality for enumerating for some reason.
    //class LineGroup : System.Collections.Generic.IEnumerable<SessionDescriptionLine>
    //{
    //    readonly System.Collections.Generic.List<SessionDescriptionLine> Lines = new System.Collections.Generic.List<SessionDescriptionLine>();

    //    System.Collections.Generic.IEnumerator<SessionDescriptionLine> System.Collections.Generic.IEnumerable<SessionDescriptionLine>.GetEnumerator()
    //    {
    //        return Lines.GetEnumerator();
    //    }

    //    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    //    {
    //        return ((System.Collections.Generic.IEnumerator<SessionDescriptionLine>)this);
    //    }
    //}

    ////Even simpler
    //class LineList : System.Collections.Generic.List<SessionDescriptionLine>
    //{
    //    //Should enforce line start and ends,
    //}

    //Implementation should not seperate Lines, IEnumerable Lines would be a better expectation (this way line index would be absolute from parent session description)
    //The lines could be accessed from the Lines Enumerator.. See DescriptionLineGroup

    //public class MediaDescription : LineList
    //{
    //    //Must enforce the first line to be MediaDescription, must not stop parsing until the next MediaDescription.
    //}

    //Could also be used when encodings or char sets are different for lines, could group them accordingly.

    internal class DescriptionLineGroup : SessionDescriptionLine, System.Collections.Generic.IEnumerable<SessionDescriptionLine>
    {
        #region Fields

        //AllowedTypes

        System.Collections.Generic.IEnumerable<SessionDescriptionLine> GroupedLines = System.Linq.Enumerable.Empty<SessionDescriptionLine>();

        #endregion

        #region Constructor

        public DescriptionLineGroup(char type, int partCount = 0)
            : base(type, partCount)
        {

        }

        public DescriptionLineGroup(char type, int partCount = 0, string seperator = null)
            : base(type, seperator, partCount)
        {

        }

        public DescriptionLineGroup(string[] sdpLines, ref int index, char expectedType, int partCount) //= 0
            : base(sdpLines, ref index, SessionDescription.SpaceString, expectedType, partCount)
        {
            //This will cause a line to be have to be extracted from GroupedLines so the pattern is not complete
            //Could use TaggedException to provide the line at the expense of the throwing the exception.

            //Continue parsing until the line type is equal to the m_Type
            while (index < sdpLines.Length && System.Linq.Enumerable.Last(GroupedLines).m_Type != m_Type)
            {
                //Could peek the type here.

                //Must determine when to stop parsing
                SessionDescriptionLine toAdd = new SessionDescriptionLine(sdpLines, ref index);

                try
                {
                    //Add the line created
                    Add(toAdd);
                }
                catch (System.Exception ex)
                {
                    if (ex is System.InvalidOperationException) throw new Common.TaggedException<SessionDescriptionLine>(toAdd);
                    else throw ex;
                }
            }
        }

        #endregion

        #region Methods

        public void Add(SessionDescriptionLine line)
        {
            //Ensure not the same type
            if (line.m_Type == m_Type) throw new System.InvalidOperationException("Line Type cannot be the same as LineGroup Type.");

            //Should check if allowed at this level

            //Concat the line with the GroupedLines and assign again.
            GroupedLines = Common.Extensions.Linq.LinqExtensions.Concat(GroupedLines, line);
        }

        #endregion

        #region IEnumerable

        System.Collections.Generic.IEnumerator<SessionDescriptionLine> System.Collections.Generic.IEnumerable<SessionDescriptionLine>.GetEnumerator()
        {
            yield return this;

            foreach (SessionDescriptionLine line in GroupedLines) yield return line;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

}

//Description derivived type would allow for easier parsing of media and time descirptions, would implicitly be a line and inherit from the Line class...

///Could also have it's own m_Lines and store itself in m_Lines... weird but would allow custom ordering...

//Possibly have a Field concept which can be used to join multiple parts but retain a type?..
