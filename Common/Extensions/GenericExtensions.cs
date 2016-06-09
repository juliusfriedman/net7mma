#region References
//http://stackoverflow.com/questions/6607033/c-sharp-language-generics-open-closed-bound-unbound-constructed
//http://stackoverflow.com/questions/1735035/generics-open-and-closed-constructed-types
//http://stackoverflow.com/questions/2173107/what-exactly-is-an-open-generic-type-in-net
#endregion

namespace Media.Common.Extensions.Generic
{
    /// <summary>
    /// Provides extensions method for generic types.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class GenericExtensions
    {
        /// <summary>
        /// The type of <see cref="System.Nullable"/> without any type specified.
        /// </summary>
        public static readonly System.Type OpenNullableGenericType = typeof(System.Nullable<>);

        /// <summary>
        /// <see cref="typeof(T).MetadataToken"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int MetadataToken<T>(T t) { return typeof(T).MetadataToken; }

        /// <summary>
        /// Castclass support
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static U As<T, U>(T t) where U : class { return t as U; }

        //These projects use IL Weaving to allow Enum and Delegate as constraints in a where clause.

        //https://github.com/Fody/ExtraConstraints

        //https://github.com/jskeet/unconstrained-melody

    }
}
