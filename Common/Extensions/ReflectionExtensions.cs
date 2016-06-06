//https://blogs.msdn.microsoft.com/dotnet/2012/08/28/evolving-the-reflection-api/

namespace Media.Common.Extensions
{
    static class ReflectionExtensions
    {
        public static System.Collections.Generic.IEnumerable<System.Reflection.MethodInfo> GetMethods(this System.Type someType, System.Reflection.BindingFlags flags)
        {
            System.Type localType = someType;

            //While there is a type
            while (localType != null)
            {
                //Get the TypeInfo
                System.Reflection.TypeInfo typeInfo = System.Reflection.IntrospectionExtensions.GetTypeInfo(localType);

                //Iterate the methods only on the type given , not the inhertied type
                foreach (System.Reflection.MethodInfo methodInfo in typeInfo.DeclaredMethods)
                {
                    if (flags == System.Reflection.BindingFlags.DeclaredOnly)
                    {
                        yield return methodInfo;
                    }
                    //Check access based on the given binding flags.
                    else if (flags.HasFlag(System.Reflection.BindingFlags.Public) && methodInfo.IsPublic)
                    {                        
                        yield return methodInfo;
                    }
                    else if(flags.HasFlag(System.Reflection.BindingFlags.NonPublic) && methodInfo.IsPrivate)
                    {
                        yield return methodInfo;
                    }
                }
                    
                //change to the base type and proceed again
                localType = typeInfo.BaseType;
            }
        }

    }
}
