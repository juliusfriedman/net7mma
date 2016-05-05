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

//Rtti interop example 
//http://stackoverflow.com/questions/33802676/how-to-get-a-raw-memory-pointer-to-a-managed-class

//netMF older versions will need Emit class.

namespace Media.Concepts.Classes
{

    //See also http://www.codeproject.com/Articles/9927/Fast-Dynamic-Property-Access-with-C

    internal delegate T UnalignedReadDelegate<T>(ref T t);

    //Used to build the UnalignedReadDelegate for each T
    internal static class Generic<T>
    {
        //static System.Action<T> UnalignedReadDelegater<T>(ref T t);

        internal static readonly UnalignedReadDelegate<T> UnalignedRead;

        internal static readonly UnalignedReadDelegate<T> Read;

        internal static readonly UnalignedReadDelegate<T> Write;

        static Generic()
        {
            System.Type typeOfT = typeof(T);

            #region UnalignedRead

            System.Type[] args = { typeOfT.MakeByRefType() };

            System.Reflection.Emit.DynamicMethod unalignedReadMethod = new System.Reflection.Emit.DynamicMethod("UnalignedReadDelegate", typeOfT, args);

            System.Reflection.Emit.ILGenerator generator = unalignedReadMethod.GetILGenerator();

            generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
            generator.Emit(System.Reflection.Emit.OpCodes.Unaligned);
            generator.Emit(System.Reflection.Emit.OpCodes.Ldobj);
            //generator.Emit(System.Reflection.Emit.OpCodes.Unbox_Any);
            generator.Emit(System.Reflection.Emit.OpCodes.Ret);

            UnalignedRead = (UnalignedReadDelegate<T>)unalignedReadMethod.CreateDelegate(typeof(UnalignedReadDelegate<T>));

            #endregion

            #region Read

            #endregion

            #region Write

            #endregion
        }
    }

    public static class CommonIntermediateLanguage
    {
        static readonly System.Action<System.IntPtr, byte, int> InitblkDelegate;

        static readonly System.Action<System.IntPtr, System.IntPtr, int> CpyblkDelegate;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized | System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static CommonIntermediateLanguage()
        {
            if (InitblkDelegate != null | CpyblkDelegate != null) return;

            #region Initblk
            System.Reflection.Emit.DynamicMethod initBlkMethod = new System.Reflection.Emit.DynamicMethod("Initblk",
                System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static, System.Reflection.CallingConventions.Standard,
                null, new[] { typeof(System.IntPtr), typeof(byte), typeof(int) }, typeof(CommonIntermediateLanguage), true);

            System.Reflection.Emit.ILGenerator generator = initBlkMethod.GetILGenerator();
            generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);//src
            generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);//value
            generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_2);//len
            generator.Emit(System.Reflection.Emit.OpCodes.Initblk);
            generator.Emit(System.Reflection.Emit.OpCodes.Ret);

            InitblkDelegate = (System.Action<System.IntPtr, byte, int>)initBlkMethod.CreateDelegate(typeof(System.Action<System.IntPtr, byte, int>));

            #endregion

            #region Cpyblk

            System.Reflection.Emit.DynamicMethod cpyBlkMethod = new System.Reflection.Emit.DynamicMethod("Cpyblk",
                System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static, System.Reflection.CallingConventions.Standard,
                null, new[] { typeof(System.IntPtr), typeof(System.IntPtr), typeof(int) }, typeof(CommonIntermediateLanguage), true);

             generator = cpyBlkMethod.GetILGenerator();

             generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);//dst
             generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);//src
             generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_2);//len
             generator.Emit(System.Reflection.Emit.OpCodes.Cpblk);
             generator.Emit(System.Reflection.Emit.OpCodes.Ret);             

             CpyblkDelegate = (System.Action<System.IntPtr, System.IntPtr, int>)cpyBlkMethod.CreateDelegate(typeof(System.Action<System.IntPtr, System.IntPtr, int>));

            #endregion

            #region See Generic

            // #region UnalignedRead

            // System.Reflection.AssemblyName asmName = typeof(CommonIntermediateLanguage).Assembly.GetName();

            // //https://msdn.microsoft.com/en-us/library/ms228971(v=vs.110).aspx

            // System.AppDomain domain = System.AppDomain.CurrentDomain;

            // System.Reflection.Emit.AssemblyBuilder demoAssembly = domain.DefineDynamicAssembly(asmName, System.Reflection.Emit.AssemblyBuilderAccess.RunAndSave);

            // // Define the module that contains the code. For an 
            // // assembly with one module, the module name is the 
            // // assembly name plus a file extension.
            // System.Reflection.Emit.ModuleBuilder demoModule = demoAssembly.DefineDynamicModule(asmName.Name, asmName.Name + ".dll");

            // var UnalignedReadTypeBuilder = demoModule.DefineType("UnalignedReader", System.Reflection.TypeAttributes.Public);

            // System.Reflection.Emit.MethodBuilder unalignedReadMethod = UnalignedReadTypeBuilder.DefineMethod("UnalignedReadMethod", System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static);

            // System.Reflection.Emit.GenericTypeParameterBuilder[] genericTypeParameters = unalignedReadMethod.DefineGenericParameters("T");

            // //unalignedReadMethod.DefineParameter(0, System.Reflection.ParameterAttributes.In, "t");

            // unalignedReadMethod.SetReturnType(genericTypeParameters[0]);

            // generator = unalignedReadMethod.GetILGenerator();

            // generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);//dst
            // generator.Emit(System.Reflection.Emit.OpCodes.Unaligned);
            // generator.Emit(System.Reflection.Emit.OpCodes.Ldobj);
            // generator.Emit(System.Reflection.Emit.OpCodes.Ret);

            // //Create the type now.
            // //System.Type UnalignedReaderType = UnalignedReadTypeBuilder.CreateType();

            // //var UnalignedReadMethodInfo = UnalignedReadTypeBuilder.GetMethod("UnalignedReadMethod");

            // //var m = UnalignedReadTypeBuilder.GetMethod("UnalignedReadMethod");

            // //m.MakeGenericMethod

            // //var bound = m.MakeGenericMethod();

            ////genericTypeBuilder.CreateType();

            ////UnalignedReadDelegate = unalignedReadMethod.CreateDelegate(genericTypeParameters[0].UnderlyingSystemType);             

            // #endregion

            #endregion
        }

        //Could possibly avoid pinning using the addresss but already have the unsafe variants

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void Initblk(byte[] array, byte what, int length)
        {
            System.Runtime.InteropServices.GCHandle gcHandle = default(System.Runtime.InteropServices.GCHandle);

            try
            {
                gcHandle = System.Runtime.InteropServices.GCHandle.Alloc(array, System.Runtime.InteropServices.GCHandleType.Pinned);

                InitblkDelegate(gcHandle.AddrOfPinnedObject(), what, length);
            }
            finally
            {
                if(gcHandle.IsAllocated) gcHandle.Free();
            }
        }

        public static void Initblk(byte[] array, int offset, byte what, int length)
        {
            System.Runtime.InteropServices.GCHandle gcHandle = default(System.Runtime.InteropServices.GCHandle);

            try
            {
                gcHandle = System.Runtime.InteropServices.GCHandle.Alloc(array, System.Runtime.InteropServices.GCHandleType.Pinned);

                InitblkDelegate(gcHandle.AddrOfPinnedObject() + offset, what, length);
            }
            finally
            {
                if (gcHandle.IsAllocated) gcHandle.Free();
            }
        }

        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static unsafe void Initblk(byte* array, byte what, int len)
        {
            InitblkDelegate((System.IntPtr)array, what, len);
        }

        public static void Cpyblk(byte[] src, byte[] dst, int length)
        {
            System.Runtime.InteropServices.GCHandle srcHandle = default(System.Runtime.InteropServices.GCHandle);
            System.Runtime.InteropServices.GCHandle dstHandle = default(System.Runtime.InteropServices.GCHandle);

            try
            {
                srcHandle = System.Runtime.InteropServices.GCHandle.Alloc(src, System.Runtime.InteropServices.GCHandleType.Pinned);
                dstHandle = System.Runtime.InteropServices.GCHandle.Alloc(dst, System.Runtime.InteropServices.GCHandleType.Pinned);
                CpyblkDelegate(dstHandle.AddrOfPinnedObject(), srcHandle.AddrOfPinnedObject(), length);
            }
            finally
            {
                if (srcHandle.IsAllocated) srcHandle.Free();
                if (dstHandle.IsAllocated) dstHandle.Free();
            }
        }

        public static void Cpyblk(byte[] src, byte[] dst, int offset, int length)
        {
            System.Runtime.InteropServices.GCHandle srcHandle = default(System.Runtime.InteropServices.GCHandle);
            System.Runtime.InteropServices.GCHandle dstHandle = default(System.Runtime.InteropServices.GCHandle);

            try
            {
                srcHandle = System.Runtime.InteropServices.GCHandle.Alloc(src, System.Runtime.InteropServices.GCHandleType.Pinned);
                dstHandle = System.Runtime.InteropServices.GCHandle.Alloc(dst, System.Runtime.InteropServices.GCHandleType.Pinned);
                CpyblkDelegate(dstHandle.AddrOfPinnedObject(), srcHandle.AddrOfPinnedObject() + offset, length);
            }
            finally
            {
                if (srcHandle.IsAllocated) srcHandle.Free();
                if (dstHandle.IsAllocated) dstHandle.Free();
            }
        }

        public static void Cpyblk(byte[] src, int srcOffset, byte[] dst, int dstOffset, int length)
        {
            System.Runtime.InteropServices.GCHandle srcHandle = default(System.Runtime.InteropServices.GCHandle);
            System.Runtime.InteropServices.GCHandle dstHandle = default(System.Runtime.InteropServices.GCHandle);

            try
            {
                srcHandle = System.Runtime.InteropServices.GCHandle.Alloc(src, System.Runtime.InteropServices.GCHandleType.Pinned);
                dstHandle = System.Runtime.InteropServices.GCHandle.Alloc(dst, System.Runtime.InteropServices.GCHandleType.Pinned);
                CpyblkDelegate(dstHandle.AddrOfPinnedObject() + srcOffset, srcHandle.AddrOfPinnedObject() + dstOffset, length);
            }
            finally
            {
                if (srcHandle.IsAllocated) srcHandle.Free();
                if (dstHandle.IsAllocated) dstHandle.Free();
            }
        }

        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static unsafe void Cpyblk(byte* src, byte* dst, int len)
        {
            CpyblkDelegate((System.IntPtr)dst, (System.IntPtr)src, len);
        }

        //Note that 4.6 Has System.Buffer.MemoryCopy 
            //=>Internal Memove and Memcopy uses optomized copy impl which can be replicated for other types also.
            //https://github.com/dotnet/corefx/issues/493

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public unsafe static void Cpyblk<T>(T[] src, int srcOffset, T[] dst, int dstOffset, int length)
        {
            System.Buffer.MemoryCopy((void*)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<T>(src, srcOffset), (void*)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<T>(dst, dstOffset), Unsafe.ArrayOfTwoElements<T>.AddressingDifference(), length);

            //System.Runtime.InteropServices.GCHandle srcHandle = System.Runtime.InteropServices.GCHandle.Alloc(src, System.Runtime.InteropServices.GCHandleType.Pinned);
            //System.Runtime.InteropServices.GCHandle dstHandle = System.Runtime.InteropServices.GCHandle.Alloc(dst, System.Runtime.InteropServices.GCHandleType.Pinned);

            //System.Buffer.MemoryCopy((void*)srcHandle.AddrOfPinnedObject(), (void*)dstHandle.AddrOfPinnedObject(), Unsafe.BytesPer<T>(), length);

            

            //srcHandle.Free();
            //dstHandle.Free();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public unsafe static T UnalignedRead<T>(ref T t)
        {
            return Generic<T>.UnalignedRead(ref t);
        }


        //internal static void UsageTest()
        //{
        //    byte[] src = new byte[] { 1, 2, 3, 4 };

        //    byte[] dst = new byte[] { 0, 0, 0, 0 };

        //    //Set the value 5 to indicies 0,1,2 in dst 
        //    Concepts.Classes.CommonIntermediateLanguage.Initblk(dst, 5, 3);

        //    //Set the value 5 to indicies 1 & 2 in dst (count is absolute)
        //    Concepts.Classes.CommonIntermediateLanguage.Initblk(dst, 1, 5, 2);

        //    //Show it was set to 5
        //    System.Console.WriteLine(dst[0]);

        //    //Show it was not set to 5
        //    System.Console.WriteLine(dst[3]);

        //    //Copy values 0 - 3 from src to dst
        //    Concepts.Classes.CommonIntermediateLanguage.Cpyblk(src, dst, 3);            

        //    Concepts.Classes.CommonIntermediateLanguage.Cpyblk<byte>(src, dst, 3);

        //    //Copy values 1 - 3 from src to dst @ 0 (count is absolute)
        //    Concepts.Classes.CommonIntermediateLanguage.Cpyblk(src, 1, dst, 0, 2);

        //    //Show they were copied
        //    System.Console.WriteLine(dst[0]);

        //    //Show they were not copied
        //    System.Console.WriteLine(dst[3]);

        //}

    }
}
