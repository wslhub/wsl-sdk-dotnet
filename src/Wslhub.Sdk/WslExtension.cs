using System.IO;

namespace Wslhub.Sdk
{
    public static class WslExtension
    {
        public static string RunWslCommand(this DistroRegistryInfo distroInfo, string commandLine, int bufferLength = 65535)
        {
            return Wsl.RunWslCommand(distroInfo.DistroName, commandLine, bufferLength);
        }

        public static long RunWslCommand(this DistroRegistryInfo distroInfo, string commandLine, Stream outputStream, int bufferLength = 65535)
        {
            return Wsl.RunWslCommand(distroInfo.DistroName, commandLine, outputStream, bufferLength);
        }
    }
}
