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

//http://www.codeproject.com/Articles/32125/Unmanaged-Arrays-in-C-No-Problem

//Used Unsafe.BytesPer to fix issues with pointer sizes

namespace Media.Concepts.Classes
{
    /// <summary>
    /// Provides functions for dealing with unmanaged pointers
    /// </summary>
    static unsafe class Unmanaged
    {
        public static void* New<T>(int elementCount)
            where T : struct
        {
            return System.Runtime.InteropServices.Marshal.AllocHGlobal((int)(Unsafe.ArrayOfTwoElements<T>.AddressingDifference() * elementCount)).ToPointer();
        }

        public static void* NewAndInit<T>(int elementCount)
            where T : struct
        {
            int newSizeInBytes = (int)(Unsafe.ArrayOfTwoElements<T>.AddressingDifference() * elementCount);

            byte* newArrayPointer = (byte*)System.Runtime.InteropServices.Marshal.AllocHGlobal(newSizeInBytes).ToPointer();

            for (int i = 0; i < newSizeInBytes; ++i)
                *(newArrayPointer + i) = 0;

            return (void*)newArrayPointer;
        }

        public static void Free(void* pointerToUnmanagedMemory)
        {
            System.Runtime.InteropServices.Marshal.FreeHGlobal(new System.IntPtr(pointerToUnmanagedMemory));
        }

        public static void* Resize<T>(void* oldPointer, int newElementCount)
            where T : struct
        {
            return (System.Runtime.InteropServices.Marshal.ReAllocHGlobal(new System.IntPtr(oldPointer),
                new System.IntPtr(Unsafe.ArrayOfTwoElements<T>.AddressingDifference() * newElementCount))).ToPointer();
        }

        //http://stackoverflow.com/questions/1951290/memory-alignment-of-classes-in-c

        public static class AlignedNew
        {
            public static T New<T>() where T : new()
            {
                System.Collections.Generic.LinkedList<T> candidates = new System.Collections.Generic.LinkedList<T>();
                System.IntPtr pointer = System.IntPtr.Zero;
                bool continue_ = true;

                int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T)) % 8; //IntPtr.Size

                while (continue_)
                {
                    if (size == 0)
                    {
                        object gap = new object();
                    }

                    candidates.AddLast(new T());

                    System.Runtime.InteropServices.GCHandle handle = System.Runtime.InteropServices.GCHandle.Alloc(candidates.Last.Value, System.Runtime.InteropServices.GCHandleType.Pinned);
                    pointer = handle.AddrOfPinnedObject();

                    //IntPtr.Size                              
                    continue_ = (pointer.ToInt64() % 8) != 0 || (pointer.ToInt64() % 64) == 24;

                    handle.Free();

                    if (false == continue_)
                        return candidates.Last.Value;
                }

                return default(T);
            }
        }

        
        
    }
}

#endif


