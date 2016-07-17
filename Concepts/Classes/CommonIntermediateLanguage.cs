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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Media;

/*
Copyright (c) 2013 juliusfriedman@gmail.com
  
 SR. Software Engineer ASTI Transportation Inc.

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

//namespace Media.Common
//{
//    #region CommonIntermediateLanguage

//    /// <summary>
//    /// An IL Parser which supports a few new text based commands.
//    /// This class can be subclassed to allow for creations of new languages based on the CLR without the use of the DLR.
//    /// It converts given code directly to <see cref="System.Reflection.Opcodes"/>.
//    /// Parsers could be written such as CLanguage, JavaLanaguage or PHPLanguage which could then convert to and from the required formats.
//    /// </summary>
//    public sealed class CommonIntermediateLanguage //: ILanguage
//    {
//        //#region Statics

//        //public static bool HasElements(Array array) { return !Null(array) && array.Length > 0; }

//        //public static bool Null(Object o) { return o == null; }

//        //static string[] LineSplits = new string[] { Environment.NewLine };

//        //static string[] TokenSplits = new string[] { " " };

//        //static char[] UnsignedNotation = new char[] { 'U', 'u' };

//        //static char[] BytesNotation = new char[] { 'Y', 'y' };

//        //static char[] ShortsNotation = new char[] { 'S', 's' };

//        //static char[] LongNotation = new char[] { 'L', 'l' };

//        //static char[] FloatNotation = new char[] { 'F', 'f' };

//        //static char[] DecimalNotation = new char[] { 'M', 'm' };

//        //static char[] DoubleNotation = new char[] { 'D', 'd' };

//        //const char Qualifier = ':';

//        //const char MemberQualifier = '.';

//        //static char[][] NotedTypes = new char[][]
//        //{
//        //    UnsignedNotation,
//        //    BytesNotation,
//        //    ShortsNotation,
//        //    LongNotation,
//        //    //ULongs Given by Unsinged
//        //    FloatNotation,
//        //    DecimalNotation,
//        //    DoubleNotation
//        //};

//        //static Type GetType(string noted, out bool unsigned, out MemberInfo[] members, out System.Runtime.InteropServices.CallingConvention callingConvention)
//        //{
//        //    callingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl;
//        //    //Read if requried
//        //    members = null;
//        //    unsigned = false;
//        //    if (string.IsNullOrWhiteSpace(noted)) return null;
//        //    else
//        //    {
//        //        int notationIndex = noted.IndexOfAny(UnsignedNotation);
//        //        //If the notationIndex of UnsignedNotation is at the very end this is a uint
//        //        if (notationIndex == noted.Length)
//        //        {
//        //            unsigned = true;
//        //            return CLR.Types.UInt; //Byte compatibility will be performed inline or with the use of Y
//        //        }
//        //        else if (notationIndex > 0) //If this is the last part this is a unsigned type              
//        //        {
//        //            unsigned = true;
//        //            char nextToLast = noted[noted.Length - 1];
//        //            if (notationIndex != -1 && LongNotation.Contains(nextToLast)) return CLR.Types.ULong;
//        //            else if (notationIndex != -1 && ShortsNotation.Contains(nextToLast)) return CLR.Types.UShort; //Added
//        //            else if (notationIndex != -1 && BytesNotation.Contains(nextToLast)) return CLR.Types.Byte; //Added
//        //            else Utility.BreakIfAttached();//
//        //        }
//        //        else if (noted.IndexOfAny(LongNotation) != -1) return CLR.Types.Long;
//        //        else if (noted.IndexOfAny(DecimalNotation) != -1) return CLR.Types.Decimal;
//        //        else if (noted.IndexOfAny(DoubleNotation) != -1) return CLR.Types.Double;
//        //        else if (noted.IndexOfAny(ShortsNotation) != -1) return CLR.Types.Short; //Added US
//        //        else if (noted.IndexOfAny(BytesNotation) != -1) return CLR.Types.SByte;  //Added UY           
//        //        else if (char.IsNumber(noted[0])) return CLR.Types.Int; //Must be a int because U or u was not used?
//        //        else //Check for qualified MemberInfo 
//        //        {
//        //            notationIndex = noted.IndexOf(Qualifier);
//        //            //If there is a qualfied member from a Type
//        //            if (notationIndex > 0 && noted.IndexOf(Qualifier, notationIndex) == notationIndex + 1)
//        //            {
//        //                //Determine the type
//        //                Type result = Type.GetType(noted.Substring(0, notationIndex + 1).Trim());
//        //                if (result != null)
//        //                {
//        //                    members = result.GetMember(noted.Substring(notationIndex + 2).Trim());//Skip .
//        //                    return typeof(MemberInfo);
//        //                }
//        //            }
//        //            else return Type.GetType(noted);// Must be a type load it on up...?        
//        //            //A more mature implementation might throw an exception which would then be resolved via loading...
//        //        }
//        //    }
//        //    //Can't determine what type noted is.
//        //    Utility.BreakIfAttached();
//        //    return null;
//        //}

//        ////Should be in an order with another array or maybe a dictionary where the char gets you the Type... e.g. Double or single.. etc.
//        //static char[] PreciseNotation = new char[] { '.', ',' };

//        //const string HexadecimalNotation = @"0x";

//        //static bool HasHexadecimalNotation(string check)
//        //{
//        //    if (string.IsNullOrWhiteSpace(check)) return false;
//        //    else return check.StartsWith(HexadecimalNotation);
//        //}

//        //static bool HasPreciseNotation(string check, char[] additional = null)
//        //{
//        //    return check.IndexOfAny((HasElements(additional) ?
//        //            PreciseNotation.Concat(additional) :
//        //                PreciseNotation).ToArray()) != -1;
//        //}

//        //static Dictionary<string, OpCode> Lookup = new Dictionary<string, OpCode>();

//        //static CommonIntermediateLanguage()
//        //{
//        //    //Load all known and Allowed OpCodes by the CLR
//        //    foreach (OpCode o in typeof(OpCodes).GetFields().Select(p => (OpCode)p.GetValue(null)))
//        //        Lookup.Add(o.Name, o);
//        //}

//        //#endregion

//        //#region Methods

//        ///// <summary>
//        ///// Creates a method with the given name on the first module found in the given assembly
//        ///// </summary>
//        ///// <param name="methodName">The name of the method you are creating</param>
//        ///// <param name="IL">The Intermedie Code associated with said method</param>
//        ///// <param name="prototype">A Delegate which looks like and returns the same value as the function you are passing</param>
//        ///// <param name="assemlby"></param>
//        ///// <returns>The function pointer created to the compiled IL</returns>
//        //public static Delegate CreateMethod(string methodName, string IL, Delegate prototype, Assembly assemlby = null)
//        //{
//        //    MethodInfo mi = prototype.GetMethodInfo();

//        //    ParameterInfo[] methodParams = mi.GetParameters().ToArray();

//        //    return CreateMethod(methodName, IL, prototype.Method.ReturnParameter.ParameterType, methodParams.Select(p => p.ParameterType).ToArray(), prototype.GetType(), assemlby);
//        //}

//        ///// <summary>
//        ///// Creates a method with the given name on the first module found in the given assembly
//        ///// </summary>
//        ///// <param name="methodName">The name of the method you are creating</param>
//        ///// <param name="IL">The Intermedie Code associated with said method</param>
//        ///// <param name="returnType">The type to return from the method you are creating</param>
//        ///// <param name="parameterTypes">The type of the parameters the method you are creating will accept</param>
//        ///// <param name="delegateType">The Type representing the Delegate you are Emulating</param>
//        ///// <param name="assembly"></param>
//        ///// <returns></returns>
//        //public static Delegate CreateMethod(string methodName, string IL, Type returnType, Type[] parameterTypes, Type delegateType, Assembly assembly = null)
//        //{

//        //    ///Use the current assembly
//        //    if (assembly == null) assembly = Assembly.GetExecutingAssembly();

//        //    //The methods real name
//        //    string dynamicName = methodName + returnType.ToString();

//        //    //A Module
//        //    Module module = assembly.Modules.First();

//        //    //Make a new method
//        //    DynamicMethod result = new DynamicMethod(dynamicName, returnType, parameterTypes, module);

//        //    //Get the generator for the method
//        //    System.Reflection.Emit.ILGenerator generator = result.GetILGenerator();

//        //    //store labels created.
//        //    Dictionary<string, Label> labels = new Dictionary<string, Label>();

//        //    string[] lines = IL.Split(LineSplits, StringSplitOptions.RemoveEmptyEntries);

//        //    //Default calling convention type in case of a call
//        //    System.Runtime.InteropServices.CallingConvention callingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl;

//        //    //The values used when parsing the IL
//        //    string anyValue = null;
//        //    Type valueType = null;
//        //    MemberInfo[] members = null;
//        //    bool unsigned = false;

//        //    //Enumerate each IL Directive and parse it into an OpCode and Directive combination if requried
//        //    for (int i = 0, e = lines.Length; i < e; ++i)
//        //    {
//        //        string line = lines[i];
//        //        string comment = string.Empty, code;
//        //        int commentsIndex = line.IndexOf("//");//Comments
//        //        if (commentsIndex != -1)
//        //        {
//        //            code = line.Substring(0, commentsIndex).Trim();
//        //            comment = line.Substring(code.Length, line.Length - code.Length);
//        //        }
//        //        else code = line.Trim();

//        //        //Whitespare only
//        //        if (string.IsNullOrEmpty(code)) continue;

//        //        int qualifierIndex = code.IndexOf(Qualifier);

//        //        //Label...
//        //        if (qualifierIndex > 0)
//        //        {
//        //            //Label support
//        //            labels.Add(code.Substring(0, qualifierIndex), generator.DefineLabel());
//        //            continue;
//        //        }
//        //        else if (qualifierIndex == 0) Utility.BreakIfAttached();

//        //        //Split into tokens
//        //        string[] tokens = code.Split(TokenSplits, StringSplitOptions.RemoveEmptyEntries);

//        //        //Cache max length
//        //        int tokensLength = tokens.Length;

//        //        //Get the instruction from the table
//        //        OpCode opCode = Lookup[tokens[0]];

//        //        //No OpCode found
//        //        if (opCode == null)
//        //        {
//        //            Utility.BreakIfAttached();
//        //            continue;
//        //        }

//        //        //Simple enough
//        //        if (tokens.Length == 1)
//        //        {
//        //            generator.Emit(opCode);
//        //            continue;
//        //        }

//        //        //Get the first token
//        //        anyValue = tokens[1];

//        //        //If this is an instance call
//        //        if (anyValue.ToLowerInvariant().Trim() == "instance")
//        //        {
//        //            //Invalid call?
//        //            if (3 < tokensLength)
//        //            {
//        //                Utility.BreakIfAttached();
//        //                continue;
//        //            }
//        //            else anyValue = tokens[2];
//        //        }

//        //        //Get the type from the function along with any required MemberInfos
//        //        valueType = GetType(anyValue, out unsigned, out members, out callingConvention);

//        //        //Determine if instructions follow
//        //        if (opCode != null && !string.IsNullOrEmpty(anyValue))
//        //        {
//        //            if (opCode.Name == "goto") //Check for a label first
//        //            {
//        //                //Try to see if the directive matches a label
//        //                Label found;

//        //                if (labels.TryGetValue(anyValue, out found))
//        //                    generator.Emit(opCode, found);
//        //                else if (string.IsNullOrEmpty(anyValue))
//        //                    generator.Emit(opCode);
//        //                continue;
//        //            }
//        //            else if (opCode.Name == "newobj")
//        //            {
//        //                //Allocation
//        //                generator.Emit(opCode, members.OfType<ConstructorInfo>().FirstOrDefault());
//        //                continue;
//        //            }
//        //            else if (opCode.Name.StartsWith("call") /* || opCode.Name.StartsWith("calli") || opCode.Name.StartsWith("callvirt") */)
//        //            {

//        //                //i, virt - constrainted flag?
//        //                MethodInfo methodInfo = members.OfType<MethodInfo>().ToArray().FirstOrDefault();

//        //                //Part the paramters to the method info
//        //                ParameterInfo[] parameters = methodInfo.GetParameters();

//        //                //Emit call with parameters
//        //                if (methodInfo == null)
//        //                {
//        //                    //No Method Info
//        //                    Utility.BreakIfAttached();
//        //                    continue;
//        //                }
//        //                else //There is Method Info
//        //                {
//        //                    //If there are parameters
//        //                    if (HasElements(parameters))
//        //                    {
//        //                        if (opCode.Name == "calli") generator.EmitCalli(opCode, callingConvention, returnType, parameters.Select(p => p.ParameterType).ToArray());
//        //                        else generator.EmitCall(opCode, methodInfo, parameters.Select(p => p.ParameterType).ToArray());
//        //                    }
//        //                    else //Emit call without parameters
//        //                    {
//        //                        if (opCode.Name == "calli") generator.EmitCalli(opCode, callingConvention, returnType, null);
//        //                        else generator.EmitCall(opCode, methodInfo, null);
//        //                    }
//        //                    //Done
//        //                    continue;
//        //                }
//        //            }
//        //            else if (unsigned)
//        //            {
//        //                if (valueType == CLR.Types.Byte)
//        //                    generator.Emit(opCode, byte.Parse(anyValue));
//        //                else if (valueType == typeof(ushort))
//        //                    generator.Emit(opCode, ushort.Parse(anyValue));
//        //                else if (valueType == typeof(uint))
//        //                    generator.Emit(opCode, uint.Parse(anyValue));
//        //                else if (valueType == typeof(long))
//        //                    generator.Emit(opCode, ulong.Parse(anyValue));
//        //                //Done
//        //                continue;
//        //            }
//        //            else if (valueType == typeof(sbyte))
//        //                generator.Emit(opCode, sbyte.Parse(anyValue));
//        //            else if (valueType == CLR.Types.Short)
//        //                generator.Emit(opCode, short.Parse(anyValue));
//        //            else if (valueType == typeof(double))
//        //                generator.Emit(opCode, double.Parse(anyValue));
//        //            else if (valueType == CLR.Types.Float)
//        //                generator.Emit(opCode, double.Parse(anyValue));
//        //            else if (valueType == typeof(long)) //Use long
//        //                generator.Emit(opCode, long.Parse(anyValue));
//        //            else if (valueType == CLR.Types.Int && anyValue.Length > 3)//use int
//        //                generator.Emit(opCode, int.Parse(anyValue));
//        //            else if (valueType == CLR.Types.Int) //Check for byte compatibility
//        //            {
//        //                //Check for byte
//        //                byte v = default(byte);
//        //                int x = default(int);
//        //                //Check for hex notation
//        //                if (HasHexadecimalNotation(anyValue))
//        //                {
//        //                    if (anyValue.Length <= 5 && byte.TryParse(anyValue, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out v))
//        //                        generator.Emit(opCode, v);
//        //                    else if (int.TryParse(anyValue, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out x))
//        //                        generator.Emit(opCode, x);
//        //                }
//        //                else if (int.TryParse(anyValue, out x)) //Parsed in
//        //                    generator.Emit(opCode, x);
//        //                else Utility.BreakIfAttached();//Can't parse input
//        //                continue;
//        //            }
//        //            else //Not a signed type so it must be an object or something else
//        //            {
//        //                //Didn't Emit Anything?
//        //                Utility.BreakIfAttached();
//        //            }

//        //            //Describes a prefix instruction that modifies the behavior of the following instruction.
//        //            if (opCode.OpCodeType == OpCodeType.Prefix)
//        //            {
//        //                //generator.Emit(
//        //            }

//        //            //Done
//        //            continue;
//        //        }
//        //    }

//        //    //Emit Return
//        //    //generator.Emit(System.Reflection.Emit.OpCodes.Ret);

//        //    //Return the delegate
//        //    return result.CreateDelegate(delegateType);
//        //}

//        //#endregion
//    }

//    #endregion
//}


namespace Media.Concepts.Classes
{

    //See also http://www.codeproject.com/Articles/9927/Fast-Dynamic-Property-Access-with-C

    internal delegate T ReferenceFunc<T>(ref T t); //IntPtr where

    internal delegate void ReferenceAction<T>(ref T t); //IntPtr where

    internal delegate T BoxingReference<T>(ref object o); //As

    internal delegate int SizeOfDelegate<T>();

    //Used to build the UnalignedReadDelegate for each T
    internal static class Generic<T>
    {
        //Public API which will be in the framework is at
        //Try to provide versions of everything @
        //https://github.com/dotnet/corefx/blob/ca5d1174dbaa12b8b6e55dc494fcd4609ed553cc/src/System.Runtime.CompilerServices.Unsafe/src/System.Runtime.CompilerServices.Unsafe.il

        internal static readonly ReferenceFunc<T> UnalignedRead;

        internal static readonly ReferenceFunc<T> Read;

        internal static readonly ReferenceAction<T> Write;

        internal static readonly BoxingReference<T> _As;

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

            UnalignedRead = (ReferenceFunc<T>)unalignedReadMethod.CreateDelegate(typeof(ReferenceFunc<T>));

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

            _As = (BoxingReference<T>)asMethod.CreateDelegate(typeof(BoxingReference<T>));

            #endregion

            //Not yet working, requires an argument for where to read IntPtr
            
            #region Read

            System.Reflection.Emit.DynamicMethod readMethod = new System.Reflection.Emit.DynamicMethod("_Read", typeOfT, args);

            generator = readMethod.GetILGenerator();

            generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);

            generator.Emit(System.Reflection.Emit.OpCodes.Ldobj, typeOfT);

            generator.Emit(System.Reflection.Emit.OpCodes.Ret);

            Read = (ReferenceFunc<T>)readMethod.CreateDelegate(typeof(ReferenceFunc<T>));

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

            Write = (ReferenceAction<T>)writeMethod.CreateDelegate(typeof(ReferenceAction<T>));

            #endregion
        }

        //public static U As<U>(ref T t) { return default(U); }
    }

    public static class CommonIntermediateLanguage
    {
        public static System.Type TypeOfVoid = typeof(void);

        public static System.Type TypeOfIntPtr = typeof(System.IntPtr);

        static readonly System.Action<System.IntPtr, byte, int> InitblkDelegate;

        static readonly System.Action<System.IntPtr, System.IntPtr, int> CpyblkDelegate;

        //static readonly System.Func<System.Type, int> SizeOfDelegate;

        //static readonly System.Func<int, int> SizeOfDelegate2;

        //Should be IntPtr, int, IntPtr, int...

        //static readonly System.Func<System.IntPtr, int, byte[], int> CallIndirectDelegate1;

        static readonly System.Action<System.IntPtr> CallIndirectPointerStdCall;

        static readonly System.Func<System.IntPtr, System.IntPtr> CallIndirectPointerIntPtr;

        static readonly System.Func<System.IntPtr, ulong> CallIndirectPointerULongStdCall, CallIndirectPointerULongThisCall, CallIndirectPointerULongCdelc, CallIndirectPointerULongFastCall;

        static readonly System.Func<System.IntPtr, uint> CallIndirectPointerUIntStdCall;

        //https://msdn.microsoft.com/ja-jp/windows/ms693373(v=vs.110)
        /*
            HRESULT CallIndirect(
              [out] HRESULT *phrReturn, //The value returned from the invocation of the method.
              [in]  ULONG   iMethod, //The method number to be invoked.
              [in]  void    *pvArgs, //A pointer to the stack frame with which to make the invocation. Details of the exact representation of this stack frame are processor-architecture specific.
              [out] ULONG   *cbArgs //The number of bytes to be popped from the stack to clear the stack of arguments to this invocation.
            );
         */

        //Todo, CallIndirect (byte*, byte[], void*)

        //Todo, Just have IntPtr return so returns can be chained if required, results read at the pointer.
        //Should probably then not clean stack with std call, could also use this call.

        [System.CLSCompliant(false)]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void IndirectCall(System.IntPtr ptr)
        {
            if (ptr == System.IntPtr.Zero) return;

            CallIndirectPointerStdCall(ptr);
        }

        [System.CLSCompliant(false)]
        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void CallIndirect(System.IntPtr ptr, out ulong result)
        {
            if (ptr == System.IntPtr.Zero)
            {
                result = ulong.MinValue;

                return;
            }

            result = CallIndirectPointerULongStdCall(ptr);

            return;
        }

        [System.CLSCompliant(false)]
        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void CallIndirect(System.IntPtr ptr, out System.IntPtr result)
        {
            if (ptr == System.IntPtr.Zero)
            {
                result = System.IntPtr.Zero;

                return;
            }

            result = CallIndirectPointerIntPtr(ptr);

            return;
        }

        /// <summary>
        /// Given a type and a pointer, the pointer is indirectly called and result of the invocation is copied to the result.
        /// </summary>
        /// <typeparam name="T">The type which control the size of memory copied to result</typeparam>
        /// <param name="ptr">The pointer of the code to invoke</param>
        /// <param name="result">The variable which receives the result</param>
        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal unsafe static void CallIndirect<T>(System.IntPtr ptr, ref T result)
        {
            result = default(T);

            if (ptr == System.IntPtr.Zero)
            {
                return;
            }

            //The result in most cases is actually the value not a pointer to it...
            System.IntPtr resultPointer;

            //Get the address of the result or the result itself.
            CallIndirect(ptr, out resultPointer);

            //Determine the size of T (Unsafe.SizeOf<T>)
            int size = Unsafe.ArrayOfTwoElements<T>.AddressingDifference();

            //Make a local reference to the result
            System.TypedReference resultReference = __makeref(result);

            //Make a pointer to the local reference
            System.IntPtr localPointer = *(System.IntPtr*)(&resultReference);

            //Make a pointer to the pointer, which when dereferenced can access the result.
            int* sourcePointer = (int*)&resultPointer;

            //Copy from the source pointer to the handle
            System.Buffer.MemoryCopy((void*)sourcePointer, (void*)localPointer, size, size);

            //Also works but allocates a new instance
            //result = As<System.IntPtr, T>(resultPointer);
        }

        /// <summary>
        /// Given a type and another type will copy the memory between the variables
        /// </summary>
        /// <typeparam name="TSource">Source Type</typeparam>
        /// <typeparam name="TResult">Destination Type</typeparam>
        /// <param name="t">The element to convert</param>
        /// <returns>An instance of U with the memory of T</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public unsafe static TResult As<TSource, TResult>(TSource t)
        {
            //Determine the size of T
            int sizeOfT = Unsafe.ArrayOfTwoElements<TSource>.AddressingDifference();

            //Make a local reference to the source input
            System.TypedReference trSource = __makeref(t);

            //Make a default value for the result, if TResult needs new a constraint should be added and overloads.
            TResult result = default(TResult);

            //Make a local reference to the result
            System.TypedReference trResult = __makeref(result);

            //Make a pointer to the local reference
            System.IntPtr localReferenceSource = *(System.IntPtr*)(&trSource);

            //Make a pointer to the local reference
            System.IntPtr localReferenceResult = *(System.IntPtr*)(&trResult);

            //Make a pointer to the pointer, which when dereferenced can access the result.
            int* sourcePointer = (int*)&trSource;

            //Make a pointer to the pointer, which when dereferenced can access the result.
            int* destPointer = (int*)&trResult;

            //Copy from the source pointer to the handle
            System.Buffer.MemoryCopy((void*)sourcePointer, (void*)destPointer, sizeOfT, sizeOfT);

            //Return the reference value of the result
            return __refvalue(trResult, TResult);
        }

        [System.CLSCompliant(false)]
        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static ulong CallIndirect(System.IntPtr ptr) // ref
        {
            ulong result;

            CallIndirect(ptr, out result);

            return result;
        }

        //Can't define in c# with the same name, Should just define one that return IntPtr...

        //[System.CLSCompliant(false)]
        //[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        //public static uint CallIndirect(System.IntPtr ptr)
        //{
        //    if (ptr == null) return uint.MinValue;

        //    return CallIndirectDelegate3(ptr);
        //}

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized | System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static CommonIntermediateLanguage()
        {
            if (InitblkDelegate != null | CpyblkDelegate != null) return;

            System.Type CommonIntermediaLanguageType = typeof(CommonIntermediateLanguage);

            #region Initblk
            System.Reflection.Emit.DynamicMethod initBlkMethod = new System.Reflection.Emit.DynamicMethod("Initblk",
                System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static, System.Reflection.CallingConventions.Standard,
                TypeOfVoid, new[] { TypeOfIntPtr, typeof(byte), typeof(int) }, CommonIntermediaLanguageType, true);

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
                TypeOfVoid, new[] { TypeOfIntPtr, TypeOfIntPtr, typeof(int) }, CommonIntermediaLanguageType, true);

             generator = cpyBlkMethod.GetILGenerator();

             generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);//dst
             generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);//src
             generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_2);//len
             generator.Emit(System.Reflection.Emit.OpCodes.Cpblk);
             generator.Emit(System.Reflection.Emit.OpCodes.Ret);             

             CpyblkDelegate = (System.Action<System.IntPtr, System.IntPtr, int>)cpyBlkMethod.CreateDelegate(typeof(System.Action<System.IntPtr, System.IntPtr, int>));

            #endregion

            #region Calli

             System.Reflection.Emit.DynamicMethod calliMethod;/* = new System.Reflection.Emit.DynamicMethod("Calli_1",
                 System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static, System.Reflection.CallingConventions.Standard,
                 typeof(int), new[] { TypeOfIntPtr, typeof(int), typeof(byte[]) }, CommonIntermediaLanguageType, true);

             generator = calliMethod.GetILGenerator();
             generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_2);//byte[], should be IntPtr...
             generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);//int
             generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
             generator.EmitCalli(System.Reflection.Emit.OpCodes.Calli, 
                 System.Runtime.InteropServices.CallingConvention.StdCall,
                 typeof(int), new System.Type[] { typeof(int), typeof(byte[]) });
             generator.Emit(System.Reflection.Emit.OpCodes.Ret);

             CallIndirectDelegate1 = (System.Func<System.IntPtr, int, byte[], int>)calliMethod.CreateDelegate(typeof(System.Func<System.IntPtr, int, byte[], int>));*/

             //--- IntPtr

             calliMethod = new System.Reflection.Emit.DynamicMethod("Calli_IntPtr",
                  System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static, System.Reflection.CallingConventions.Standard,
                  TypeOfIntPtr, new[] { TypeOfIntPtr }, CommonIntermediaLanguageType, true);

             generator = calliMethod.GetILGenerator();
             generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0); //ptr             
             generator.EmitCalli(System.Reflection.Emit.OpCodes.Calli, System.Runtime.InteropServices.CallingConvention.StdCall, TypeOfIntPtr, System.Type.EmptyTypes);
             generator.Emit(System.Reflection.Emit.OpCodes.Ret);

             CallIndirectPointerIntPtr = (System.Func<System.IntPtr, System.IntPtr>)calliMethod.CreateDelegate(typeof(System.Func<System.IntPtr, System.IntPtr>));

             //--- void

             calliMethod = new System.Reflection.Emit.DynamicMethod("Calli_Void",
                  System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static, System.Reflection.CallingConventions.Standard,
                  TypeOfVoid, new[] { TypeOfIntPtr }, CommonIntermediaLanguageType, true);

             generator = calliMethod.GetILGenerator();
             generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0); //ptr
             generator.EmitCalli(System.Reflection.Emit.OpCodes.Calli, System.Runtime.InteropServices.CallingConvention.StdCall, TypeOfVoid, System.Type.EmptyTypes);
             generator.Emit(System.Reflection.Emit.OpCodes.Ret);

             CallIndirectPointerStdCall = (System.Action<System.IntPtr>)calliMethod.CreateDelegate(typeof(System.Action<System.IntPtr>));            

            //--- uint

             calliMethod = new System.Reflection.Emit.DynamicMethod("Calli_uint",
                   System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static, System.Reflection.CallingConventions.Standard,
                   typeof(uint), new[] { TypeOfIntPtr }, CommonIntermediaLanguageType, true);

             generator = calliMethod.GetILGenerator();
             generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0); //ptr
             generator.Emit(System.Reflection.Emit.OpCodes.Conv_I); // Convert to native int, pushing native int on stack.
             generator.EmitCalli(System.Reflection.Emit.OpCodes.Calli, System.Runtime.InteropServices.CallingConvention.StdCall, typeof(uint), System.Type.EmptyTypes);
             generator.Emit(System.Reflection.Emit.OpCodes.Ret);

             CallIndirectPointerUIntStdCall = (System.Func<System.IntPtr, uint>)calliMethod.CreateDelegate(typeof(System.Func<System.IntPtr, uint>));


             //--- ulong

             calliMethod = new System.Reflection.Emit.DynamicMethod("Calli_ThisCall_ulong",
                  System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static, System.Reflection.CallingConventions.Standard,
                  typeof(ulong), new[] { TypeOfIntPtr }, CommonIntermediaLanguageType, true);

             generator = calliMethod.GetILGenerator();
             generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0); //ptr
             generator.EmitCalli(System.Reflection.Emit.OpCodes.Calli, System.Runtime.InteropServices.CallingConvention.ThisCall, typeof(ulong), System.Type.EmptyTypes);
             generator.Emit(System.Reflection.Emit.OpCodes.Ret);

             CallIndirectPointerULongThisCall = (System.Func<System.IntPtr, ulong>)calliMethod.CreateDelegate(typeof(System.Func<System.IntPtr, ulong>));

             calliMethod = new System.Reflection.Emit.DynamicMethod("Calli_Cdecl_ulong",
                   System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static, System.Reflection.CallingConventions.Standard,
                   typeof(ulong), new[] { TypeOfIntPtr }, CommonIntermediaLanguageType, true);

             generator = calliMethod.GetILGenerator();
             generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0); //ptr
             generator.EmitCalli(System.Reflection.Emit.OpCodes.Calli, System.Runtime.InteropServices.CallingConvention.Cdecl, typeof(ulong), System.Type.EmptyTypes);
             generator.Emit(System.Reflection.Emit.OpCodes.Ret);

             CallIndirectPointerULongCdelc = (System.Func<System.IntPtr, ulong>)calliMethod.CreateDelegate(typeof(System.Func<System.IntPtr, ulong>));

             calliMethod = new System.Reflection.Emit.DynamicMethod("Calli_FastCall_ulong",
                    System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static, System.Reflection.CallingConventions.Standard,
                    typeof(ulong), new[] { TypeOfIntPtr }, CommonIntermediaLanguageType, true);

             generator = calliMethod.GetILGenerator();
             generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0); //ptr
             generator.EmitCalli(System.Reflection.Emit.OpCodes.Calli, System.Runtime.InteropServices.CallingConvention.FastCall, typeof(ulong), System.Type.EmptyTypes);
             generator.Emit(System.Reflection.Emit.OpCodes.Ret);

             CallIndirectPointerULongFastCall = (System.Func<System.IntPtr, ulong>)calliMethod.CreateDelegate(typeof(System.Func<System.IntPtr, ulong>));

             calliMethod = new System.Reflection.Emit.DynamicMethod("Calli_StdCall_ulong",
                  System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static, System.Reflection.CallingConventions.Standard,
                  typeof(ulong), new[] { TypeOfIntPtr }, CommonIntermediaLanguageType, true);

             generator = calliMethod.GetILGenerator();
             generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0); //ptr
             generator.EmitCalli(System.Reflection.Emit.OpCodes.Calli, System.Runtime.InteropServices.CallingConvention.StdCall, typeof(ulong), System.Type.EmptyTypes);
             generator.Emit(System.Reflection.Emit.OpCodes.Ret);

             CallIndirectPointerULongStdCall = (System.Func<System.IntPtr, ulong>)calliMethod.CreateDelegate(typeof(System.Func<System.IntPtr, ulong>));

            #endregion

            #region Unused

            //void Read could be done with IntPtr where and SizeOf

            //void Write would be done with IntPtr where, IntPtr what and SizeOf

            //As would be difficult to represent, same boat as SizeOf.

            ////#region SizeOf

            //// System.Reflection.Emit.DynamicMethod sizeOfMethod = new System.Reflection.Emit.DynamicMethod("__SizeOf",
            ////     System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.Static, System.Reflection.CallingConventions.Standard,
            ////     typeof(int), new System.Type[] { typeof(System.Type) }, CommonIntermediaLanguageType, true);

            //// generator = sizeOfMethod.GetILGenerator();

            //////Bad class token..
            //// generator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);            

            //////could try to either pass the handle or call for it..
            //////typeof(System.Type).GetProperty("TypeHandle").GetValue()

            //// //CommonIntermediaLanguageType.GetMethod("SizeOf").GetGenericArguments()[0].MakeGenericType().MetadataToken

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
            //// //generator.Emit(System.Reflection.Emit.OpCodes.Sizeof, CommonIntermediaLanguageType.GetMethod("SizeOf").GetGenericArguments()[0].GetElementType());

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

            //stobj gets the value of type typeTo at an address
            //The stobj instruction copies the value type object into the address specified by the address (a pointer of type native int, *, or &). The number of bytes copied depends on the size of the class represented by class, a metadata token representing a value type.

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
            ////     typeof(int), new System.Type[] { typeof(int) }, CommonIntermediaLanguageType, true);

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

        //Maybe add a Pin construct to put the type on the stack to ensure it's not moved.

        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void InitBlock(ref byte[] array, byte what, int length)
        {
            InitblkDelegate(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(array, 0), what, length);
        }

        [System.CLSCompliant(false)]
        public static void InitBlock(byte[] array, byte what, int length)
        {
            InitBlock(ref array, what, length);
        }

        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void InitBlock(ref byte[] array, int offset, byte what, int length)
        {
            InitblkDelegate(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(array, offset), what, length);
        }
        
        [System.CLSCompliant(false)]
        public static void InitBlock(byte[] array, int offset, byte what, int length)
        {
            InitBlock(ref array, offset, what, length);
        }

        [System.CLSCompliant(false)]
        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static unsafe void InitBlock(byte* array, byte what, int len)
        {
            InitblkDelegate((System.IntPtr)array, what, len);
        }

        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void CopyBlock(byte[] src, byte[] dst, int length)
        {
            CpyblkDelegate(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(src, 0), System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(dst, 0), length);
        }

        [System.CLSCompliant(false)]
        public static void CopyBlock(ref byte[] src, ref byte[] dst, int length)
        {
            CopyBlock(ref src, ref dst, length);
        }

        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void CopyBlock(ref byte[] src, ref byte[] dst, int offset, int length)
        {
            CpyblkDelegate(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(src, offset), System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(dst, offset), length);
        }

        [System.CLSCompliant(false)]
        public static void CopyBlock(byte[] src, byte[] dst, int offset, int length)
        {
            CopyBlock(ref src, ref dst, offset, length);
        }

        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void CopyBlock(ref byte[] src, int srcOffset, ref byte[] dst, int dstOffset, int length)
        {
            CpyblkDelegate(System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(src, srcOffset), System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(dst, dstOffset), length);
        }

        [System.CLSCompliant(false)]
        public static void CopyBlock(byte[] src, int srcOffset, byte[] dst, int dstOffset, int length)
        {
            CopyBlock(ref src, srcOffset, ref dst, dstOffset, length);
        }

        [System.CLSCompliant(false)]
        [System.Security.SuppressUnmanagedCodeSecurity]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static unsafe void CopyBlock(byte* src, byte* dst, int len) //CopyBlock
        {
            CpyblkDelegate((System.IntPtr)dst, (System.IntPtr)src, len);
        }

        //Note that 4.6 Has System.Buffer.MemoryCopy 
            //=>Internal Memove and Memcopy uses optomized copy impl which can be replicated /used for other types also.
            //https://github.com/dotnet/corefx/issues/493

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public unsafe static void CopyBlock<T>(T[] src, int srcOffset, T[] dst, int dstOffset, int length) //CopyBlock (void *)
        {
            System.Buffer.MemoryCopy((void*)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<T>(src, srcOffset), (void*)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement<T>(dst, dstOffset), length, length);
        }

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

        //This many bytes in a structure after an array can allow a custom header to be created...
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int ArrayHeaderSize()
        {
            System.Array array = Common.MemorySegment.EmptyBytes;

            return (int)((int)System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(array, 0) - (int)Unsafe.AddressOf(ref array));
        }

        ////[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        ////public static int ObjectHeaderSize()
        ////{

        ////}

        /// <summary>
        /// Should be equal to <see cref="System.Runtime.CompilerServices.RuntimeHelpers.OffsetToStringData"/>
        /// </summary>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public unsafe static int StringHeaderSize()
        {
            //Determine the overhead of the clr header.
            string s = string.Empty;
            fixed (char* t = s)
            {
                return (int)((int)(System.IntPtr)t - (int)Unsafe.AddressOf<string>(ref s));
            }
        }

        internal static void UsageTest()
        {
            byte[] src = new byte[] { 1, 2, 3, 4 };

            byte[] dst = new byte[] { 0, 0, 0, 0 };

            //Set the value 5 to indicies 0,1,2 in dst 
            Concepts.Classes.CommonIntermediateLanguage.InitBlock(dst, 5, 3);

            //Set the value 5 to indicies 1 & 2 in dst (count is absolute)
            Concepts.Classes.CommonIntermediateLanguage.InitBlock(dst, 1, 5, 2);

            //Show it was set to 5
            System.Console.WriteLine(dst[0]);

            //Show it was not set to 5
            System.Console.WriteLine(dst[3]);

            //Copy values 0 - 3 from src to dst
            Concepts.Classes.CommonIntermediateLanguage.CopyBlock(src, dst, 3);

            Concepts.Classes.CommonIntermediateLanguage.CopyBlock<byte>(src, 0, dst, 0, 3);

            //Copy values 1 - 3 from src to dst @ 0 (count is absolute)
            Concepts.Classes.CommonIntermediateLanguage.CopyBlock(src, 1, dst, 0, 2);

            //Show they were copied
            System.Console.WriteLine(dst[0]);

            //Show they were not copied
            System.Console.WriteLine(dst[3]);

        }

    }
}
