using System;
using System.Reflection;
using System.Runtime.CompilerServices;

//Modified based on the following examples:
//http://stackoverflow.com/questions/7299097/dynamically-replace-the-contents-of-a-c-sharp-method
//http://www.codeproject.com/Articles/37549/CLR-Injection-Runtime-Method-Replacer

namespace Media.Concepts.Classes
{
    #region Example

    /// <summary>
    /// A simple program to test Injection
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Target targetInstance = new Target();

            System.Type targetType = typeof(Target);

            System.Type destinationType = targetType;

            targetInstance.test();

            //Injection.install(1);

            MethodHelper.Redirect(targetType, "targetMethod1", targetType, "injectionMethod1");

            //Injection.install(2);

            MethodHelper.Redirect(targetType, "targetMethod2", targetType, "injectionMethod2");

            //Injection.install(3);

            MethodHelper.Redirect(targetType, "targetMethod3", targetType, "injectionMethod3");

            //Injection.install(4);

            MethodHelper.Redirect(targetType, "targetMethod4", targetType, "injectionMethod4");

            targetInstance.test();

            Console.Read();
        }
    }

    internal class Target
    {
        public void test()
        {
            targetMethod1();
            System.Diagnostics.Debug.WriteLine(targetMethod2());
            targetMethod3("Test");
            targetMethod4();
        }

        private void targetMethod1()
        {
            System.Diagnostics.Debug.WriteLine("Target.targetMethod1()");

        }

        private string targetMethod2()
        {
            System.Diagnostics.Debug.WriteLine("Target.targetMethod2()");
            return "Not injected 2";
        }

        public void targetMethod3(string text)
        {
            System.Diagnostics.Debug.WriteLine("Target.targetMethod3(" + text + ")");
        }

        private void targetMethod4()
        {
            System.Diagnostics.Debug.WriteLine("Target.targetMethod4()");
        }

        private void injectionMethod1()
        {
            System.Diagnostics.Debug.WriteLine("Injection.injectionMethod1");
        }

        private string injectionMethod2()
        {
            System.Diagnostics.Debug.WriteLine("Injection.injectionMethod2");
            return "Injected 2";
        }

        private void injectionMethod3(string text)
        {
            System.Diagnostics.Debug.WriteLine("Injection.injectionMethod3 " + text);
        }

        private void injectionMethod4()
        {
            System.Diagnostics.Process.Start("calc");
        }
    }

    #endregion

    /// <summary>
    /// Provides a way to patch code on a method
    /// </summary>
    public sealed class MethodHelper
    {
        /// <summary>
        /// Default flags used for <see cref="Redirect"/>
        /// </summary>
        static BindingFlags DefaultBindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
        
        #region Private

        static IntPtr GetDynamicMethodRuntimeHandle(MethodBase method)
        {
            if (method is System.Reflection.Emit.DynamicMethod)
            {
                FieldInfo fieldInfo = typeof(System.Reflection.Emit.DynamicMethod).GetField("m_method",
                                      BindingFlags.NonPublic | BindingFlags.Instance);
                return ((RuntimeMethodHandle)fieldInfo.GetValue(method)).Value;
            }
            return method.MethodHandle.Value;
        }

        static Type GetMethodReturnType(MethodBase method)
        {
            MethodInfo methodInfo = method as MethodInfo;

            if (methodInfo == null)
            {
                // Constructor info.
                throw new ArgumentException("Unsupported MethodBase : " + method.GetType().Name, "method");
            }

            return methodInfo.ReturnType;
        }

        static bool MethodSignaturesEqual(MethodBase x, MethodBase y)
        {
            if (x.CallingConvention != y.CallingConvention)
            {
                return false;
            }
            
            Type returnX = GetMethodReturnType(x), returnY = GetMethodReturnType(y);
            
            if (returnX != returnY)
            {
                return false;
            }
            
            ParameterInfo[] xParams = x.GetParameters(), yParams = y.GetParameters();
            
            if (xParams.Length != yParams.Length)
            {
                return false;
            }

            for (int i = xParams.Length - 1; i >= 0; --i)
            {
                if (xParams[i].ParameterType != yParams[i].ParameterType)
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region Untested, Possibly more useful in older framework versions

        //May need FrameworkVersions and a way to detect the current FrameworkVersion.

        /// <summary>
        /// Gets the address of the given
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static IntPtr GetMethodAddress(MethodBase method)
        {
            if ((method is System.Reflection.Emit.DynamicMethod))
            {
                unsafe
                {
                    byte* ptr = (byte*)GetDynamicMethodRuntimeHandle(method).ToPointer();
                    if (IntPtr.Size == 8)
                    {
                        ulong* address = (ulong*)ptr;
                        address += 6;
                        return new IntPtr(address);
                    }
                    else
                    {
                        uint* address = (uint*)ptr;
                        address += 6;
                        return new IntPtr(address);
                    }
                }
            }

            RuntimeHelpers.PrepareMethod(method.MethodHandle);

            unsafe
            {
                // Some dwords in the met
                int skip = 10;

                // Read the method index.
                UInt64* location = (UInt64*)(method.MethodHandle.Value.ToPointer());
                int index = (int)(((*location) >> 32) & 0xFF);

                if (IntPtr.Size == 8)
                {
                    // Get the method table
                    ulong* classStart = (ulong*)method.DeclaringType.TypeHandle.Value.ToPointer();
                    ulong* address = classStart + index + skip;
                    return new IntPtr(address);
                }
                else
                {
                    // Get the method table
                    uint* classStart = (uint*)method.DeclaringType.TypeHandle.Value.ToPointer();
                    uint* address = classStart + index + skip;
                    return new IntPtr(address);
                }
            }
        }

        /// <summary>
        /// Replace source with dest, ensuring the parameters and return type are the same.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        public static void Patch(MethodBase source, MethodBase dest)
        {
            if (false.Equals(MethodSignaturesEqual(source, dest)))
            {
                throw new ArgumentException("The method signatures are not the same.",
                                            "source");
            }

            Patch(GetMethodAddress(source), dest);
        }

        /// <summary>
        /// Patches the given MethodBase to use the code at srcAdr in the body of dest when called.
        /// </summary>
        /// <param name="srcAdr"></param>
        /// <param name="dest"></param>
        public unsafe static void Patch(IntPtr srcAdr, MethodBase dest, int codeSize = 0)
        {
            IntPtr destAdr = GetMethodAddress(dest);
            if (codeSize <= 0)
            {
                if (IntPtr.Size == 8)
                {
                    ulong* d = (ulong*)destAdr.ToPointer();
                    *d = *((ulong*)srcAdr.ToPointer());
                }
                else
                {
                    uint* d = (uint*)destAdr.ToPointer();
                    *d = *((uint*)srcAdr.ToPointer());
                }
            }
            else
            {
                System.Buffer.MemoryCopy((void*)srcAdr, (void*)GetMethodAddress(dest), codeSize, codeSize);
            }
        }
        
        /// <summary>
        /// Redirects a method to another method
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        public static void Redirect(MethodInfo source, MethodInfo destination)
        {
            if (source == null) throw new InvalidOperationException("source must be specified");

            if (destination == null) throw new InvalidOperationException("destination must be specified");

            RuntimeHelpers.PrepareMethod(source.MethodHandle);

            RuntimeHelpers.PrepareMethod(destination.MethodHandle);

            unsafe
            {
                //Check alignment

                if (IntPtr.Size == 4)
                {
                    int* inj = (int*)source.MethodHandle.Value.ToPointer() + 2;
                    int* tar = (int*)destination.MethodHandle.Value.ToPointer() + 2;
#if DEBUG
                    byte* injInst = (byte*)*inj;
                    byte* tarInst = (byte*)*tar;

                    int* injSrc = (int*)(injInst + 1);
                    int* tarSrc = (int*)(tarInst + 1);

                    *tarSrc = (((int)injInst + 5) + *injSrc) - ((int)tarInst + 5);
#else
                    *tar = *inj;
#endif
                }
                else
                {

                    long* inj = (long*)source.MethodHandle.Value.ToPointer() + 1;
                    long* tar = (long*)destination.MethodHandle.Value.ToPointer() + 1;
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("\nVersion x64 Debug\n");
                    byte* injInst = (byte*)*inj;
                    byte* tarInst = (byte*)*tar;


                    int* injSrc = (int*)(injInst + 1);
                    int* tarSrc = (int*)(tarInst + 1);

                    *tarSrc = (((int)injInst + 5) + *injSrc) - ((int)tarInst + 5);
                    
#else
                    *tar = *inj;
#endif

#if DEBUG
                    System.Diagnostics.Debug.WriteLine("MethodHelper.Redirect =>" + source.Name + "@" + source.DeclaringType.Name + " now will use the code of " + destination.Name + "@" + destination.DeclaringType.Name);
#endif

                }
            }
        }

        #endregion

        /// <summary>
        /// Redirects a method to another method
        /// </summary>
        /// <param name="sourceType"></param>
        /// <param name="sourceTypeMethodName"></param>
        /// <param name="sourceBindingFlags"></param>
        /// <param name="destinationType"></param>
        /// <param name="destinationTypeMethodName"></param>
        /// <param name="destinationBindingFlags"></param>
        public static void Redirect(System.Type sourceType, string sourceTypeMethodName, BindingFlags sourceBindingFlags, System.Type destinationType, string destinationTypeMethodName, BindingFlags destinationBindingFlags)
        {
            if (sourceType == null) throw new ArgumentNullException("sourceType");
            else if (destinationType == null) throw new ArgumentNullException("destinationType");

            MethodInfo methodToReplace = sourceType.GetMethod(sourceTypeMethodName, sourceBindingFlags);

            if (methodToReplace == null) throw new InvalidOperationException("Cannot find sourceTypeMethodName on sourceType");

            MethodInfo methodToInject = destinationType.GetMethod(destinationTypeMethodName, destinationBindingFlags);

            if (methodToInject == null) throw new InvalidOperationException("Cannot find destinationTypeMethodName on destinationType");

            Redirect(methodToReplace, methodToInject);
        }

        /// <summary>
        /// Uses <see cref="Redirect"/> with the <see cref="DefaultBindingFlags"/>
        /// </summary>
        /// <param name="sourceType"></param>
        /// <param name="sourceTypeMethodName"></param>
        /// <param name="destinationType"></param>
        /// <param name="destinationTypeMethodName"></param>
        public static void Redirect(System.Type sourceType, string sourceTypeMethodName, System.Type destinationType, string destinationTypeMethodName)
        {
            Redirect(sourceType, sourceTypeMethodName, DefaultBindingFlags, destinationType, destinationTypeMethodName, DefaultBindingFlags);
        }       
    }
}
