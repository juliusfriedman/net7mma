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

namespace Media.Common.Structures
{

    #region IStruct

    /// <summary>
    /// An <see cref="interface"/> intended to support the super position of a future defined <see cref="class">Struct</see>
    /// </summary>
    public interface IStruct : Media.Common.Interfaces.ITryGet, Media.Common.Interfaces.ITrySet
    {
        //Create, Destroy, Size, Get, Set, GetHashCode, ToString, Equals
    }

    #endregion

    //IReference

    #region IReferenceInfomation

    //Struct or Class, IReference?

    /// <summary>
    /// Define an <see cref="interface"/> which can allow for the creation and deletion of references...
    /// </summary>
    interface IReferenceInfomation : Media.Common.Interfaces.Interface//, IReference
    {
        //TakeReference
        //RemoveReference
        //HasReference
    }

    #endregion

    #region Delegates

    /// <summary>
    /// A <see cref="Delegate"/> which will return a <see cref="String"/> instance which represents the given type.
    /// </summary>
    /// <typeparam name="T">The type, where <see cref="IStruct"/> is required</typeparam>
    /// <param name="t">The element</param>
    /// <returns>The string which was created as a result of calling <see cref="ToString"/></returns>
    public delegate string ToString<T>(ref T t) where T : IStruct;

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="t"></param>
    /// <returns></returns>
    public delegate int GetHashCode<T>(ref T t) where T : IStruct;

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="T"></param>
    /// <param name="t"></param>
    /// <returns></returns>
    public delegate bool Equals<T>(ref T T, ref T t) where T : IStruct;

    /// <summary>
    /// Allows another type of <see cref="IStruct"/> to be returned in addition to the typed parameter
    /// </summary>
    /// <typeparam name="T">The type</typeparam>
    /// <param name="t">The address of the variable which will recieve the result</param>
    /// <returns>An <see cref="IStruct"/> which may or may not be <see cref="Equals"/> to the <paramref name="t"/></returns>
    public delegate IStruct Getter<T>(out T t) where T : IStruct;

    /// <summary>
    /// Gets the instace with type assoicated.
    /// </summary>
    /// <typeparam name="T">The type</typeparam>
    /// <param name="t">The reference to the instance</param>
    /// <returns>True or false where <see cref="true"/> indicates success.</returns>
    public delegate bool Get<T>(ref T t) where T : IStruct;

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="t"></param>
    /// <returns></returns>
    public delegate IStruct Setter<T>(out T t) where T : IStruct;

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="t"></param>
    /// <returns></returns>
    public delegate bool Set<T>(ref T t) where T : IStruct;

    //Todo, IReadOnly, IWriteOnly, IMutable

    #endregion

    /// <summary>
    /// Methods which are useful for a <see cref="Struct"/>
    /// </summary>
    public static class IStructExtensions
    {
        public static bool Equals(ref IStruct @this, ref IStruct that)
        {
            //Use the hashcode of a System.TypedReference
            return __makeref(@this).GetHashCode().Equals((__makeref(that).GetHashCode()));

            //Box
            //return System.TypedReference.Equals(@this, that);

            //System.TypedReference is clearly not an Object. 

            //__makeref(@this).Equals(__makeref(that));
        }

        public static string ToString(ref IStruct @this)
        {
            return @this.ToString();
        }

        public static int GetHashCode(ref IStruct @this)
        {
            return @this.GetHashCode();
        }

        public static IStruct GetDefault(out IStruct @this)
        {
            return @this = @this = default(IStruct);
        }

        public static bool Get(ref IStruct @this)
        {
            System.TypedReference tr = __makeref(@this);

            @this = __refvalue(tr, IStruct);

            return true;
        }

        //readonly - would need to be set via reflection.

        /// <summary>
        /// The implemenation assoicated with <see cref="object.ToString"/>
        /// </summary>
        public static readonly ToString<IStruct> ToIStructString = new ToString<IStruct>(ToString);

        /// <summary>
        /// The implemenation assoicated with <see cref="object.Equals"/>
        /// </summary>
        public static readonly Equals<IStruct> EqualsIStruct = new Equals<IStruct>(Equals);

        /// <summary>
        /// The implemenation assoicated with <see cref="object.GetHashCode"/>
        /// </summary>
        public static readonly GetHashCode<IStruct> GetHashCodeIStruct = new GetHashCode<IStruct>(GetHashCode);

        /// <summary>
        /// The implemenation assoicated with <see cref="object.Get"/>
        /// </summary>
        public static readonly Get<IStruct> GetIStruct = new Get<IStruct>(Get);

        /// <summary>
        /// The implemenation assoicated with <see cref="object.Getter"/>
        /// </summary>
        public static readonly Getter<IStruct> GetDefaultIStruct = new Getter<IStruct>(GetDefault);

        //public static ToString<IStruct> CreateString(ref IStruct structure)
        //{
            //return new ToString<IStruct>(ref structure);
        //}
    }

    #region Struct

    /// <summary>
    /// A <see cref="Struct"/> which implements <see cref="IStruct"/>
    /// </summary>
    public struct Struct : IStruct
    {
        #region Statics

        /// <summary>
        /// <see cref="default"/> of <see cref="Struct"/>
        /// </summary>
        public static readonly Struct DefaultStruct = default(Struct);
        
        /// <summary>
        /// <see cref="DefaultStruct"/> <see cref="Object.ToString"/>
        /// </summary>
        public static readonly int DefaultStructHashCode = DefaultStruct.GetHashCode();

        /// <summary>
        /// <see cref="DefaultStruct"/> <see cref="Object.ToString"/>
        /// </summary>
        public static readonly string DefaultStructString = DefaultStruct.ToString();

        #endregion

        #region Notes

        //Delegates could be stored here but would change the implicit size of the structure...

        //Delegate > ToString, GetHashCode, Equals...

        //Then would call delegates within

        #endregion

        #region Methods

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool Equals(Struct that)
        {
            //Weird that @that cannot be used because `that` is already used.
            IStruct @this = this, _that = that as IStruct;

            return IStructExtensions.EqualsIStruct(ref @this, ref _that);
        }

        #endregion

        #region Overrides

        public override bool Equals(object obj)
        {
            if ((obj is IStruct).Equals(false)) return false;

            IStruct @this = this, that = obj as IStruct;

            return IStructExtensions.EqualsIStruct(ref @this, ref that);
        }

        public override int GetHashCode()
        {
            //return base.GetHashCode();

            IStruct @this = this;

            return IStructExtensions.GetHashCodeIStruct(ref @this);
        }

        public override string ToString()
        {
            //return base.ToString();

            IStruct @this = this;

            return IStructExtensions.ToIStructString(ref @this);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Struct @this, IStruct that)
        {
            return object.ReferenceEquals(@this, null) ? object.ReferenceEquals(@that, null) : object.ReferenceEquals(@this, that);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Struct @this, IStruct that)
        {
            return (@this == that).Equals(false);
        }

        #endregion

        #region ITryGet

        bool Interfaces.ITryGet.TryGet(out object t)
        {
            t = this;

            return true;
        }

        #endregion

        #region ITrySet

        bool Interfaces.ITrySet.TrySet(object t)
        {
            if ((t is IStruct).Equals(false)) return false;

            this = (Struct)(t as IStruct);

            return true;
        }

        #endregion
    }

    #endregion

    #region Struct<T>

    /// <summary>
    /// A generic <see cref="IStruct"/>
    /// </summary>
    /// <typeparam name="T">The type</typeparam>
    public struct Struct<T> : IStruct, Media.Common.Interfaces.ITryGet<T>, Media.Common.Interfaces.ITrySet<T>
    {
        #region ITryGet<T>

        bool Interfaces.ITryGet<T>.TryGet(out T t)
        {
            try
            {
                t = (T)(this as IStruct);

                return true;
            }
            catch
            {
                t = default(T);

                return false;
            }
        }

        #endregion

        #region ITryGet

        bool Interfaces.ITryGet.TryGet(out object t)
        {
            try
            {
                t = this as object;

                return true;
            }
            catch
            {
                t = null;

                return false;
            }
        }

        #endregion

        #region ITrySet<T>

        bool Interfaces.ITrySet<T>.TrySet(ref T t)
        {
            try
            {
                this = (Struct<T>)(IStruct)(t);

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region ITrySet

        bool Interfaces.ITrySet.TrySet(object t)
        {
            try
            {
                this = (Struct<T>)(IStruct)(t);

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }

    #endregion
}
