namespace OperatingSystem
{
    public static class RuntimeExtensions
    {
        internal static System.Type MonoType = System.Type.GetType("Mono.Runtime");

        public static bool IsMono { get { return MonoType != null; } }

        public static bool IsRunningOnMono() { return IsMono; }

        //IsRyuJit - http://stackoverflow.com/questions/22422021/how-do-i-verify-that-ryujit-is-jitting-my-app

        // Check whether optimized SIMD intrinsics will be generated
        //if (VectorMath.IsHardwareAccelerated)
        //{
        //}
    }
}
