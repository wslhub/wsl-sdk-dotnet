using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace Wslhub.Sdk
{
    internal static class WslPipe
    {
        internal static long CopyTo(SafeFileHandle readPipe, Stream output, int bufferLength)
        {
            var buffer = new byte[bufferLength];
            var length = 0L;
            while (true)
            {
                if (!NativeMethods.ReadFile(readPipe, buffer, buffer.Length, out int read, System.IntPtr.Zero))
                {
                    var error = Marshal.GetLastWin32Error();
                    if (error == NativeMethods.ERROR_BROKEN_PIPE)
                        return length;
                    throw new Win32Exception(error, "Cannot read data from the WSL output pipe.");
                }

                // A short read (including a zero-byte write) does not mean EOF for a pipe.
                output.Write(buffer, 0, read);
                length += read;
            }
        }
    }
}
