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

//netMF older versions will need Emit class.

namespace Media.Concepts.Classes
{
    public static class CommonIntermediateLanguage
    {
        public static System.Action<System.IntPtr, byte, int> InitblkDelegate;

        static CommonIntermediateLanguage()
        {
            System.Reflection.Emit.DynamicMethod dynamicMethod = new System.Reflection.Emit.DynamicMethod("Initblk",
                System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static, System.Reflection.CallingConventions.Standard,
                null, new[] { typeof(System.IntPtr), typeof(byte), typeof(int) }, typeof(CommonIntermediateLanguage), true);

            System.Reflection.Emit.ILGenerator generator = dynamicMethod.GetILGenerator();
            generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
            generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
            generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_2);
            generator.Emit(System.Reflection.Emit.OpCodes.Initblk);
            generator.Emit(System.Reflection.Emit.OpCodes.Ret);

            InitblkDelegate = (System.Action<System.IntPtr, byte, int>)dynamicMethod.CreateDelegate(typeof(System.Action<System.IntPtr, byte, int>));
        }

        public static void Initblk(byte[] array, byte what, int length)
        {
            System.Runtime.InteropServices.GCHandle gcHandle = System.Runtime.InteropServices.GCHandle.Alloc(array, System.Runtime.InteropServices.GCHandleType.Pinned);
            InitblkDelegate(gcHandle.AddrOfPinnedObject(), what, length);
            gcHandle.Free();
        }

        [System.CLSCompliant(false)]
        public static unsafe void Initblk(byte* array, byte what, int len)
        {
            InitblkDelegate((System.IntPtr)array, what, len);
        }

    }
}
