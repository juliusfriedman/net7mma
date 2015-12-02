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

#if NETMF

//The Micro Framework does not support unsafe code.
//Determine if a placeholder Unsafe class will be required there, if so then implement a new class here rather than check in each method

#else

namespace Media.Concepts.Classes
{
    /// <summary>
    /// Provides various functionality which can only be achieved with the used of unsafe code.
    /// </summary>
    public static unsafe class Unsafe
    {
        #region AddressOf

        public static System.IntPtr AddressOf(object obj)
        {
            if (obj == null) return System.IntPtr.Zero;

            System.TypedReference reference = __makeref(obj);

            System.TypedReference* pRef = &reference;

            return (System.IntPtr)pRef; //(&pRef)
        }

        /// <summary>
        /// Returns the unmanaged address of the given array.
        /// </summary>
        /// <param name="array"></param>
        /// <returns><see cref="IntPtr.Zero"/> if null, otherwise the address of the array</returns>
        public static System.IntPtr AddressOfByteArray(byte[] array)
        {
            if (array == null) return System.IntPtr.Zero;

            fixed (byte* ptr = array)
                return (System.IntPtr)(ptr - 2 * sizeof(void*)); //Todo staticaly determine size of void?
        }

        #endregion

        #region OffsetOf

        /// <summary>
        /// Returns the field offset of the unmanaged form of the managed type using <see cref="System.Runtime.InteropServices.Marshal.OffsetOf"/>
        /// </summary>
        /// <param name="t"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static System.IntPtr OffsetOf(System.Type t, string fieldName)
        {
            return System.Runtime.InteropServices.Marshal.OffsetOf(t, fieldName);
        }

        #endregion

        #region BytesPer

        //http://stackoverflow.com/questions/4764573/why-is-typedreference-behind-the-scenes-its-so-fast-and-safe-almost-magical

        internal static class ArrayOfTwoElements<T> { internal static readonly T[] Value = new T[2]; }

        internal static uint BytesPer<T>()
        {
            System.TypedReference
                    elem1 = __makeref(ArrayOfTwoElements<T>.Value[0] ),
                    elem2 = __makeref(ArrayOfTwoElements<T>.Value[1] );

            return (uint)((byte*)*(System.IntPtr*)(&elem2) - (byte*)*(System.IntPtr*)(&elem1));
        }

        #endregion

        #region Read

        /// <summary>
        /// <see cref="Read"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address"></param>
        /// <returns></returns>
        static T Read<T>(System.IntPtr address)
        {
            T obj = default(T);

            System.TypedReference tr = __makeref(obj);

            *(System.IntPtr*)(&tr) = address;

            return __refvalue( tr,T);
        }

        /// <summary>
        /// This bypasses the restriction that you can't have a pointer to.
        /// letting you write very high-performance generic code.
        /// It's dangerous if you don't know what you're doing, but very worth if you do.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        static object Read(System.IntPtr address)
        {
            //Create memory for a boxed object
            object box = default(object);

            //Make a TypedReference to the object created
            System.TypedReference tr = __makeref(box);

            //This is equivalent to shooting yourself in the foot
            //but it's the only high-perf solution in some cases
            //it sets the first field of the TypedReference (which is a pointer)
            //to the address you give it, then it dereferences the value.
            //Better be 10000% sure that your type T is unmanaged/blittable...
            *(System.IntPtr*)(&tr) = address;

            //Using __refvalue
            //return __refvalue(tr, object);

            //Convert from the TypedReference back to object
            return System.TypedReference.ToObject(tr);
        }

        #endregion

        #region Write

        //public void Write(where, what){ }

        //public void Write<T>(where, what){ }

        //public void Write(where, what, offset, length){ }

        #endregion

        [System.CLSCompliant(false)]
        public static unsafe int UInt32ToInt32Bits(uint x)
        {
            return *((int*)(void*)&x);
        }

        //Should use Write

        /// <summary>
        /// Attempts to modify the given string at the given index with the new value.
        /// </summary>
        /// <param name="toModify"></param>
        /// <param name="index"></param>
        /// <param name="newValue"></param>
        public unsafe static bool UnsafeTryModifyString(string toModify, int index, char newValue)
        {
            try
            {
                UnsafeModifyString(toModify, ref index, ref newValue);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public unsafe static void UnsafeModifyString(string toModify, ref int index, ref char newValue)
        {
            fixed (char* str = toModify)
            {
                str[index] = newValue;
            }
        }
    }
}

#endif


