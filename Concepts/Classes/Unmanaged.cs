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
            return System.Runtime.InteropServices.Marshal.AllocHGlobal((int)(Unsafe.BytesPer<T>() * elementCount)).ToPointer();
        }

        public static void* NewAndInit<T>(int elementCount)
            where T : struct
        {
            int newSizeInBytes = (int)(Unsafe.BytesPer<T>() * elementCount);

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
                new System.IntPtr(Unsafe.BytesPer<T>() * newElementCount))).ToPointer();
        }
    }
}

#endif


