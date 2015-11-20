using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Common.Classes
{
    //https://github.com/synhershko/NAppUpdate/blob/master/FeedBuilder/FileInfoEx.cs

    //Media.FileInfo

    /// <summary>
    /// Allows a place to start an inheritance chain for the <see cref="System.IO.FileInfo"/> class.
    /// Supports implicit conversion to a <see cref="System.IO.FileInfo"/> and is inherited from <see cref="CommonDisposable"/>
    /// </summary>
    public class FileInfoEx : CommonDisposable
    {
        public readonly DateTimeOffset Created = DateTimeOffset.UtcNow;

        private readonly System.IO.FileInfo myFileInfo;
        private readonly System.Diagnostics.FileVersionInfo myFileVersionInfo;
        private readonly System.Version myFileVersion;
        private readonly int myHash;

        public System.IO.FileInfo FileInfo
        {
            get { return myFileInfo; }
        }

        public System.Version FileVersion
        {
            get { return myFileVersion; }
        }

        public System.Diagnostics.FileVersionInfo FileVersionInfo
        {
            get { return myFileVersionInfo; }
        }

        public int Hash
        {
            get { return myHash; }
        }

        public virtual void Refresh() { myFileInfo.Refresh(); }

        public virtual bool Exists { get { return myFileInfo.Exists; } }

        public virtual long Length { get { return myFileInfo.Length; } }

        public FileInfoEx(System.IO.FileInfo fileInfo)
            : base(true)
        {
            myFileInfo = fileInfo;

            if(myFileInfo != Common.StreamAdapter.NullFileInfo) myFileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(myFileInfo.FullName);

            if (myFileVersion != null)
            {
                myFileVersion = new System.Version(myFileVersionInfo.FileMajorPart, myFileVersionInfo.FileMinorPart, myFileVersionInfo.FileBuildPart, myFileVersionInfo.FilePrivatePart);

                myHash = myFileVersionInfo.GetHashCode();
            }
        }

        public FileInfoEx(string fileName)
            : this(new System.IO.FileInfo(fileName))
        {
            
        }

        public override int GetHashCode()
        {
            return myHash;
        }

        public override string ToString()
        {
            return myFileVersion.ToString();
        }

        public static implicit operator System.IO.FileInfo(FileInfoEx fiex) { return fiex.myFileInfo; }

        public static implicit operator FileInfoEx(System.IO.FileInfo fi) { return new FileInfoEx(fi); }

    }
}
