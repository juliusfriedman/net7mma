﻿#region Copyright
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

    internal delegate T GenericFunc<T>(ref T t); //IntPtr where

    internal delegate void GenericAction<T>(ref T t); //IntPtr where

    internal delegate T GenericActionObject<T>(object o); //As

    internal delegate int SizeOfDelegate<T>();

    //Used to build the UnalignedReadDelegate for each T
    internal static class Generic<T>
    {
        //Public API which will be in the framework is at
        //Try to provide versions of everything @
        //https://github.com/dotnet/corefx/blob/ca5d1174dbaa12b8b6e55dc494fcd4609ed553cc/src/System.Runtime.CompilerServices.Unsafe/src/System.Runtime.CompilerServices.Unsafe.il

        internal static readonly GenericFunc<T> UnalignedRead;

        internal static readonly GenericFunc<T> Read;

        internal static readonly GenericAction<T> Write;

        internal static readonly GenericActionObject<T> _As;

        internal static readonly SizeOfDelegate<T> SizeOf;

        //AsPointer

        /// <summary>
        /// Generate method logic for each T.
        /// </summary>
        static Generic()
        {
            System.Type typeOfT = typeof(T), typeOfTRef = typeOfT.MakeByRefType();

            System.Type[] args = { typeOfTRef }; //, typeof(T).MakeGenericType()

            System.Reflection.Emit.ILGenerator generator;

            //Works but has to be generated for each type.
            #region SizeOf

            System.Reflection.Emit.DynamicMethod sizeOfMethod = new System.Reflection.Emit.DynamicMethod("_SizeOf", typeof(int), System.Type.EmptyTypes);

            generator = sizeOfMethod.GetILGenerator();

            generator.Emit(System.Reflection.Emit.OpCodes.Sizeof, typeOfT);

            generator.Emit(System.Reflection.Emit.OpCodes.Ret);

            SizeOf = (SizeOfDelegate<T>)sizeOfMethod.CreateDelegate(typeof(SizeOfDelegate<T>));

            #endregion

            //Need locals or to manually define the IL in the stream.

            //Not yet working,  requires an argument for where to read IntPtr

            #region UnalignedRead

            System.Reflection.Emit.DynamicMethod unalignedReadMethod = new System.Reflection.Emit.DynamicMethod("_UnalignedRead", typeOfT, args);

            generator = unalignedReadMethod.GetILGenerator();

            generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);

            generator.Emit(System.Reflection.Emit.OpCodes.Unaligned); //, Size()

            //generator.Emit(System.Reflection.Emit.OpCodes.Unaligned, System.Reflection.Emit.Label label);

            //This would probably work but needs the pointer
            //generator.Emit(System.Reflection.Emit.OpCodes.Unaligned, long address)

            generator.Emit(System.Reflection.Emit.OpCodes.Ldobj, typeOfT);

            generator.Emit(System.Reflection.Emit.OpCodes.Ret);

            UnalignedRead = (GenericFunc<T>)unalignedReadMethod.CreateDelegate(typeof(GenericFunc<T>));

            #endregion

            //Not yet working, would be easier to rewrite a body of a stub method as there is no way to define a GenricMethod on an existing assembly easily or to re-write the method at runtime.

            //https://blogs.msdn.microsoft.com/zelmalki/2009/03/29/msil-injection-rewrite-a-non-dynamic-method-at-runtime/
            //http://stackoverflow.com/questions/7299097/dynamically-replace-the-contents-of-a-c-sharp-method

            //Could also just define a generic type dynamically and save it out to disk...

            //Could destabalize the runtime
            #region As

            System.Reflection.Emit.DynamicMethod asMethod = new System.Reflection.Emit.DynamicMethod("__As", typeOfT, new System.Type[]{ typeof(object) });

            generator = asMethod.GetILGenerator();

            //Not on the evalutation stack..
            generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);

            generator.Emit(System.Reflection.Emit.OpCodes.Ret);

            _As = (GenericActionObject<T>)asMethod.CreateDelegate(typeof(GenericActionObject<T>));

            #endregion

            //Not yet working, requires an argument for where to read IntPtr
            
            #region Read

            System.Reflection.Emit.DynamicMethod readMethod = new System.Reflection.Emit.DynamicMethod("_Read", typeOfT, args);

            generator = readMethod.GetILGenerator();

            generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);

            generator.Emit(System.Reflection.Emit.OpCodes.Ldobj, typeOfT);

            generator.Emit(System.Reflection.Emit.OpCodes.Ret);

            Read = (GenericFunc<T>)readMethod.CreateDelegate(typeof(GenericFunc<T>));

            #endregion

            //Not yet working, required an argument for where to write IntPtr

            #region Write

            System.Reflection.Emit.DynamicMethod writeMethod = new System.Reflection.Emit.DynamicMethod("_Write", null, args);

            generator = writeMethod.GetILGenerator();

            generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);// T to write but where...

            //generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);

            generator.Emit(System.Reflection.Emit.OpCodes.Ldobj, typeOfT);

            generator.Emit(System.Reflection.Emit.OpCodes.Stobj, typeOfT);

            generator.Emit(System.Reflection.Emit.OpCodes.Ret);

            Write = (GenericAction<T>)writeMethod.CreateDelegate(typeof(GenericAction<T>));

            #endregion
        }

        //public static U As<U>(ref T t) { return default(U); }
    }

    public static class CommonIntermediateLanguage
    {
        static readonly System.Action<System.IntPtr, byte, int> InitblkDelegate;

        static readonly System.Action<System.IntPtr, System.IntPtr, int> CpyblkDelegate;

        //static readonly System.Func<System.Type, int> SizeOfDelegate;

        //static readonly System.Func<int, int> SizeOfDelegate2;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized | System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static CommonIntermediateLanguage()
        {
            if (InitblkDelegate != null | CpyblkDelegate != null) return;

            #region Initblk
            System.Reflection.Emit.DynamicMethod initBlkMethod = new System.Reflection.Emit.DynamicMethod("Initblk",
                System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static, System.Reflection.CallingConventions.Standard,
                typeof(void), new[] { typeof(System.IntPtr), typeof(byte), typeof(int) }, typeof(CommonIntermediateLanguage), true);

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
                typeof(void), new[] { typeof(System.IntPtr), typeof(System.IntPtr), typeof(int) }, typeof(CommonIntermediateLanguage), true);

             generator = cpyBlkMethod.GetILGenerator();

             generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);//dst
             generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);//src
             generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_2);//len
             generator.Emit(System.Reflection.Emit.OpCodes.Cpblk);
             generator.Emit(System.Reflection.Emit.OpCodes.Ret);             

             CpyblkDelegate = (System.Action<System.IntPtr, System.IntPtr, int>)cpyBlkMethod.CreateDelegate(typeof(System.Action<System.IntPtr, System.IntPtr, int>));

            #endregion

            #region Unused

            //void Read could be done with IntPtr where and SizeOf

            //void Write would be done with IntPtr where, IntPtr what and SizeOf

            //As would be difficult to represent, same boat as SizeOf.

            ////#region SizeOf

            //// System.Reflection.Emit.DynamicMethod sizeOfMethod = new System.Reflection.Emit.DynamicMethod("__SizeOf",
            ////     System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static, System.Reflection.CallingConventions.Standard,
            ////     typeof(int), new System.Type[] { typeof(System.Type) }, typeof(CommonIntermediateLanguage), true);

            //// generator = sizeOfMethod.GetILGenerator();

            //////Bad class token..
            //// generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);            

            //////could try to either pass the handle or call for it..
            //////typeof(System.Type).GetProperty("TypeHandle").GetValue()

            //// //typeof(CommonIntermediateLanguage).GetMethod("SizeOf").GetGenericArguments()[0].MakeGenericType().MetadataToken

            //// generator.Emit(System.Reflection.Emit.OpCodes.Call, typeof(System.Type).GetMethod("GetTypeFromHandle"));

            //// generator.Emit(System.Reflection.Emit.OpCodes.Sizeof);
            //// generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
            //// generator.Emit(System.Reflection.Emit.OpCodes.Nop);
            //// generator.Emit(System.Reflection.Emit.OpCodes.Nop);
            //// generator.Emit(System.Reflection.Emit.OpCodes.Nop);
            //// generator.Emit(System.Reflection.Emit.OpCodes.Nop);
            //// generator.Emit(System.Reflection.Emit.OpCodes.Ret);

            //// goto next;

            //// //T is not bound yet.
            //// //generator.Emit(System.Reflection.Emit.OpCodes.Sizeof, typeof(CommonIntermediateLanguage).GetMethod("SizeOf").GetGenericArguments()[0].GetElementType());

            //// //Putting it into the local is only useful for WriteLine
            //// //Define a local which has the type of Type
            //// System.Reflection.Emit.LocalBuilder localBuilder = generator.DeclareLocal(typeof(System.Type)); //typeof(System.TypedReference)             

            //// //Load an argument address, in short form, onto the evaluation stack.
            //// //generator.Emit(System.Reflection.Emit.OpCodes.Ldarga_S, 0);
            //// generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);

            //// //Loads an object reference as a type O (object reference) onto the evaluation stack indirectly.
            ////// generator.Emit(System.Reflection.Emit.OpCodes.Ldind_Ref);

            //// //Cast the object reference to System.Type
            //// //generator.Emit(System.Reflection.Emit.OpCodes.Castclass, typeof(System.Type));

            //// //Pops the current value from the top of the evaluation stack and stores it in a the local variable
            //// generator.Emit(System.Reflection.Emit.OpCodes.Stloc, localBuilder);

            //// //Stack empty.             

            //// //Correct type...
            //// generator.EmitWriteLine(localBuilder);

            //// //Missing type, not read from stack
            //// //generator.Emit(System.Reflection.Emit.OpCodes.Sizeof);

            //// //not a token, a type
            //// //generator.Emit(System.Reflection.Emit.OpCodes.Ldtoken, localBuilder);

            //// //Loads the local variable at a specific index onto the evaluation stack.
            //// //generator.Emit(System.Reflection.Emit.OpCodes.Ldloc, localBuilder);

            //// //Not giving the sizeOf the local builders type because it is not bound right here.
            //// generator.Emit(System.Reflection.Emit.OpCodes.Sizeof, (System.Type)localBuilder.LocalType);

            //// //need to get a Type instance to give to Sizeof and it can't come from the locals...

            //// //Even if you pass object the value seen here in the int representation of the type

            //// //Call sizeof on the builders type (always 8 since it is not yet bound)
            //// //generator.Emit(System.Reflection.Emit.OpCodes.Sizeof, (System.Type)localBuilder.LocalType);


            //// //generator.Emit(System.Reflection.Emit.OpCodes.Stloc_0);

            //// //generator.Emit(System.Reflection.Emit.OpCodes.Sizeof, localBuilder);

            ////// generator.Emit(System.Reflection.Emit.OpCodes.Ldtoken);

            //// //System.Reflection.MethodInfo getTypeFromHandle = typeof(System.Type).GetMethod("GetTypeFromHandle");
            //// //generator.Emit(System.Reflection.Emit.OpCodes.Call, getTypeFromHandle); 
            
            //////Type handle is on the top 

            //// //Works to get the type handle, can't get the type without a local , then would need to read the local's type which is not faster then just writing this in pure il.

            //// //Could also try to pass IntPtr to TypeHandle..

            //// generator.Emit(System.Reflection.Emit.OpCodes.Ret);
            ////next:

            //// SizeOfDelegate = (System.Func<System.Type, int>)sizeOfMethod.CreateDelegate(typeof(System.Func<System.Type, int>));

            //// #endregion

            //// #region SizeOf

            //// System.Reflection.Emit.DynamicMethod sizeOfMethod2 = new System.Reflection.Emit.DynamicMethod("__SizeOf2",
            ////     System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static, System.Reflection.CallingConventions.Standard,
            ////     typeof(int), new System.Type[] { typeof(int) }, typeof(CommonIntermediateLanguage), true);

            //// generator = sizeOfMethod2.GetILGenerator();

            //// generator.Emit(System.Reflection.Emit.OpCodes.Sizeof);
            //// generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
            //// //generator.Emit(System.Reflection.Emit.OpCodes.Nop);
            //// //generator.Emit(System.Reflection.Emit.OpCodes.Nop);
            //// //generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
            //// generator.Emit(System.Reflection.Emit.OpCodes.Ret);

            //// SizeOfDelegate2 = (System.Func<int, int>)sizeOfMethod2.CreateDelegate(typeof(System.Func<int, int>));

            //// #endregion

            #endregion
        }

        //Could possibly avoid pinning using the addresss but already have the unsafe variants

        //Todo, no pinning logic first, then add PinnedBlockCopy.

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void Initblk(byte[] array, byte what, int length) //InitBlock
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

        public static void Initblk(byte[] array, int offset, byte what, int length) //InitBlock
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
        public static unsafe void Initblk(byte* array, byte what, int len) //InitBlock
        {
            InitblkDelegate((System.IntPtr)array, what, len);
        }

        public static void Cpyblk(byte[] src, byte[] dst, int length) //CopyBlock
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

        public static void Cpyblk(byte[] src, byte[] dst, int offset, int length) //CopyBlock
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

        public static void Cpyblk(byte[] src, int srcOffset, byte[] dst, int dstOffset, int length) //CopyBlock
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
        public static unsafe void Cpyblk(byte* src, byte* dst, int len) //CopyBlock
        {
            CpyblkDelegate((System.IntPtr)dst, (System.IntPtr)src, len);
        }

        //Note that 4.6 Has System.Buffer.MemoryCopy 
            //=>Internal Memove and Memcopy uses optomized copy impl which can be replicated for other types also.
            //https://github.com/dotnet/corefx/issues/493

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public unsafe static void Cpyblk<T>(T[] src, int srcOffset, T[] dst, int dstOffset, int length) //CopyBlock (void *)
        {
            System.Buffer.MemoryCopy((void*)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<T>(src, srcOffset), (void*)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<T>(dst, dstOffset), length, length);
        }

        //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        //public unsafe static T UnalignedRead<T>(ref T t)
        //{
        //    return Generic<T>.UnalignedRead(ref t);
        //}

        /// <summary>
        /// <see cref="System.Runtime.InteropServices.Marshal.SizeOf"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int SizeOf<T>()
        {
            return System.Runtime.InteropServices.Marshal.SizeOf<T>();
            //typeof(T).TypeHandle.Value is IntPtr but SizeOf will not take a value on the evalutation stack, would be hacky to provide anything useful without a dictionary.
            //return CommonIntermediateLanguage.SizeOfDelegate2(typeof(T).MetadataToken);
        }

        /// <summary>
        /// <see cref="typeof(T).MetadataToken"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int MetadataToken<T>() { return typeof(T).MetadataToken; }

        internal static void UsageTest()
        {
            byte[] src = new byte[] { 1, 2, 3, 4 };

            byte[] dst = new byte[] { 0, 0, 0, 0 };

            //Set the value 5 to indicies 0,1,2 in dst 
            Concepts.Classes.CommonIntermediateLanguage.Initblk(dst, 5, 3);

            //Set the value 5 to indicies 1 & 2 in dst (count is absolute)
            Concepts.Classes.CommonIntermediateLanguage.Initblk(dst, 1, 5, 2);

            //Show it was set to 5
            System.Console.WriteLine(dst[0]);

            //Show it was not set to 5
            System.Console.WriteLine(dst[3]);

            //Copy values 0 - 3 from src to dst
            Concepts.Classes.CommonIntermediateLanguage.Cpyblk(src, dst, 3);

            Concepts.Classes.CommonIntermediateLanguage.Cpyblk<byte>(src, 0, dst, 0, 3);

            //Copy values 1 - 3 from src to dst @ 0 (count is absolute)
            Concepts.Classes.CommonIntermediateLanguage.Cpyblk(src, 1, dst, 0, 2);

            //Show they were copied
            System.Console.WriteLine(dst[0]);

            //Show they were not copied
            System.Console.WriteLine(dst[3]);

        }

    }
}
