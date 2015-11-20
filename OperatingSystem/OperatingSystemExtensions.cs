
namespace OperatingSystem
{
    public static class OperatingSystemExtensions
    {
        public static System.PlatformID EnvironmentOsPlatformId = System.Environment.OSVersion.Platform;

        internal const int PlatformIdMono = 4, PlatformIdMonoMac = 6, PlatformIdMono2 = 128;

        static int IntPlatformIdValue { get { return (int)EnvironmentOsPlatformId; } }

        public static bool IsLinux
        {
            get
            {
                switch (IntPlatformIdValue)
                {
                    case PlatformIdMono:
                    case PlatformIdMonoMac:
                    case PlatformIdMono2:
                        return true;
                    default: return false;
                }
            }
        }

        public static bool IsMac
        {
            get
            {
                switch (IntPlatformIdValue)
                {
                    case PlatformIdMonoMac:
                        return true;
                    default: return false;
                }
            }
        }

        public static bool IsWindows
        {
            get
            {
                switch (IntPlatformIdValue)
                {
                    case PlatformIdMono:
                    case PlatformIdMonoMac:
                    case PlatformIdMono2:
                        return false;
                    default: return true;
                }
            }
        }

    }
}
