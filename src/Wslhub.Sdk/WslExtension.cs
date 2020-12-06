using System.IO;

namespace Wslhub.Sdk
{
    /// <summary>
    /// This class contains helper methods that help you call WSL functions using the provided WSL information model object.
    /// </summary>
    public static class WslExtension
    {
        /// <summary>
        /// Execute the specified command through the default shell of a specific WSL distribution, and get the result as a string.
        /// </summary>
        /// <remarks>
        /// When receiving data from WSL, it is encoded as UTF-8 data without the byte order mark.
        /// </remarks>
        /// <param name="distroInfo">The WSL distribution on which to run the command.</param>
        /// <param name="commandLine">The command you want to run.</param>
        /// <param name="bufferLength">Specifies the size of the buffer array to use when copying from anonymous pipes to the underlying stream. You do not need to specify a value.</param>
        /// <returns>Returns the collected output string.</returns>
        public static string RunWslCommand(this DistroRegistryInfo distroInfo, string commandLine, int bufferLength = 65535)
        {
            return Wsl.RunWslCommand(distroInfo.DistroName, commandLine, bufferLength);
        }

        /// <summary>
        /// Execute the specified command through the default shell of a specific WSL distribution, and get the result as a System.IO.Stream object.
        /// </summary>
        /// <param name="distroInfo">The WSL distribution on which to run the command.</param>
        /// <param name="commandLine">The command you want to run.</param>
        /// <param name="outputStream">The System.IO.Stream object to receive the results. It must be writable.</param>
        /// <param name="bufferLength">Specifies the size of the buffer array to use when copying from anonymous pipes to the underlying stream. You do not need to specify a value.</param>
        /// <returns>Returns the sum of the number of bytes received.</returns>
        public static long RunWslCommand(this DistroRegistryInfo distroInfo, string commandLine, Stream outputStream, int bufferLength = 65535)
        {
            return Wsl.RunWslCommand(distroInfo.DistroName, commandLine, outputStream, bufferLength);
        }
    }
}
