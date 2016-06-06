namespace Media.Common.Extensions.Generic
{
    /// <summary>
    /// Provides extensions method for generic types.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class GenericExtensions
    {
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
