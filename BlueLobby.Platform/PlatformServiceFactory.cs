using System;

namespace BlueLobby.Platform
{
    public static class PlatformServiceFactory
    {
        public static IPlatformServices Create()
        {
            if (OperatingSystem.IsWindows())
            {
                return new Windows.WindowsPlatformServices();
            }

            if (OperatingSystem.IsLinux())
            {
                return new Linux.LinuxPlatformServices();
            }

            throw new PlatformNotSupportedException("BlueLobby yalnızca Windows ve Linux'u destekler.");
        }
    }
}
