using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Concepts.Classes.E
{
    /// <summary>
    /// 
    /// </summary>
    public interface IExtensions : Media.Common.Interfaces.Interface{ }

    /// <summary>
    /// 
    /// </summary>
    public abstract class Extensions : IExtensions
    {
        public const Extensions NilExtensions = null;

        public static bool operator ==(Extensions a, object b)
        {
            return object.ReferenceEquals(b, NilExtensions) ? object.ReferenceEquals(a, NilExtensions) : a.Equals(b);
        }

        public static bool operator !=(Extensions a, object b) { return (a == b).Equals(false); }


        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public unsafe bool Equals(Extensions other)
        {
            return object.ReferenceEquals(this, NilExtensions) ? object.ReferenceEquals(other, NilExtensions) : Unsafe.AddressOf(this) == Unsafe.AddressOf(other);
        }

        public override bool Equals(object obj)
        {
            //System.Object
            if (object.ReferenceEquals(this, obj)) return true;

            if ((obj is Extensions).Equals(false)) return false;

            return Equals(obj as Extensions);
        }

        //Todo, @IHashCode{}:ICode{},IHash{}
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
