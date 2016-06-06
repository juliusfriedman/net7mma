using System;
using System.Reflection;
using System.Runtime.CompilerServices;

//Modified based on the following example:
//http://stackoverflow.com/questions/7299097/dynamically-replace-the-contents-of-a-c-sharp-method

namespace Media.Concepts.Classes
{
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

            InjectionHelper.Install(targetType, "targetMethod1", targetType, "injectionMethod1");

            //Injection.install(2);

            InjectionHelper.Install(targetType, "targetMethod2", targetType, "injectionMethod2");

            //Injection.install(3);

            InjectionHelper.Install(targetType, "targetMethod3", targetType, "injectionMethod3");

            //Injection.install(4);

            InjectionHelper.Install(targetType, "targetMethod4", targetType, "injectionMethod4");

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

    /// <summary>
    /// Provides a way to patch code on a method
    /// </summary>
    public sealed class InjectionHelper
    {
        /// <summary>
        /// Default flags used for <see cref="Install"/>
        /// </summary>
        static BindingFlags DefaultBindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        /// <summary>
        /// Installs a method as another method
        /// </summary>
        /// <param name="sourceType"></param>
        /// <param name="sourceTypeMethodName"></param>
        /// <param name="sourceBindingFlags"></param>
        /// <param name="destinationType"></param>
        /// <param name="destinationTypeMethodName"></param>
        /// <param name="destinationBindingFlags"></param>
        public static void Install(System.Type sourceType, string sourceTypeMethodName, BindingFlags sourceBindingFlags, System.Type destinationType, string destinationTypeMethodName, BindingFlags destinationBindingFlags)
        {
            MethodInfo methodToReplace = sourceType.GetMethod(sourceTypeMethodName, sourceBindingFlags);

            if (methodToReplace == null) throw new InvalidOperationException("Cannot find sourceTypeMethodName on sourceType");

            MethodInfo methodToInject = destinationType.GetMethod(destinationTypeMethodName, destinationBindingFlags);

            if (methodToInject == null) throw new InvalidOperationException("Cannot find destinationTypeMethodName on destinationType");
            
            RuntimeHelpers.PrepareMethod(methodToReplace.MethodHandle);

            RuntimeHelpers.PrepareMethod(methodToInject.MethodHandle);

            unsafe
            {
                //Check alignment

                if (IntPtr.Size == 4)
                {
                    int* inj = (int*)methodToInject.MethodHandle.Value.ToPointer() + 2;
                    int* tar = (int*)methodToReplace.MethodHandle.Value.ToPointer() + 2;
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

                    long* inj = (long*)methodToInject.MethodHandle.Value.ToPointer() + 1;
                    long* tar = (long*)methodToReplace.MethodHandle.Value.ToPointer() + 1;
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
                    System.Diagnostics.Debug.WriteLine("Install =>" + sourceType.Name + "@" + sourceTypeMethodName + " now will use the code of " + destinationType.Name + "@" + destinationTypeMethodName);
#endif

                }
            }
        }

        /// <summary>
        /// Uses <see cref="Install"/> with the <see cref="DefaultBindingFlags"/>
        /// </summary>
        /// <param name="sourceType"></param>
        /// <param name="sourceTypeMethodName"></param>
        /// <param name="destinationType"></param>
        /// <param name="destinationTypeMethodName"></param>
        public static void Install(System.Type sourceType, string sourceTypeMethodName, System.Type destinationType, string destinationTypeMethodName)
        {
            Install(sourceType, sourceTypeMethodName, DefaultBindingFlags, destinationType, destinationTypeMethodName, DefaultBindingFlags);

        }       
    }
}
