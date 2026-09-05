#pragma warning disable IDE0051 // Remove unused private members

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Runtime.Versioning;

namespace Wslhub.Sdk.Test
{
    [SupportedOSPlatform("windows")]
    static class WslTest
    {
        private static DistroRegistryInfo RequireDefaultDistro() => Wsl.GetDefaultDistro()
            ?? throw new InvalidOperationException("Install and select a default WSL distribution before running integration tests.");

        static void Test_LargeOutput()
        {
            var output = RequireDefaultDistro().RunWslCommand("head -c 1048576 /dev/zero | tr '\\000' x");
            if (output.Length != 1048576 || output.Any(character => character != 'x'))
                throw new Exception("Large output was truncated or corrupted.");
        }

        static void Test_DelayedOutput()
        {
            var output = RequireDefaultDistro().RunWslCommand("printf first; sleep 1; printf second");
            if (output != "firstsecond")
                throw new Exception("A short pipe read was mistaken for the end of output.");
        }

        static void Test_Utf8AcrossBufferBoundaries()
        {
            var output = RequireDefaultDistro().RunWslCommand("printf '\\355\\225\\234\\352\\270\\200'", 1);
            if (output != "한글")
                throw new Exception("UTF-8 output was corrupted across buffer boundaries.");
        }

        static void Test_EmptyOutput()
        {
            if (RequireDefaultDistro().RunWslCommand("true").Length != 0)
                throw new Exception("Expected empty output.");
        }

        static void Test_NonZeroExitCode()
        {
            try
            {
                RequireDefaultDistro().RunWslCommand("printf failure; exit 7");
            }
            catch (InvalidOperationException exception) when (exception.Message.Contains("7"))
            {
                return;
            }

            throw new Exception("A non-zero exit code was not reported.");
        }

        static void Test_SystemAssertion()
        {
            Wsl.AssertWslSupported();
        }

        static void Test_GetDistroListFromRegistryTest()
        {
            var distros = Wsl.GetDistroListFromRegistry();

            if (distros == null)
                throw new Exception("Cannot query registry keys.");
        }

        static void Test_QueryDistroInfo()
        {
            var distros = Wsl.GetDistroQueryResult();

            if (distros == null)
                throw new ArgumentNullException(nameof(distros));

            if (distros.Any(x => !x.IsRegistered))
                throw new Exception("Cannot query distro properties.");

            var expected = Wsl.GetDistroListFromRegistry().OrderBy(distro => distro.DistroId).ToArray();
            var actual = distros.OrderBy(distro => distro.DistroId).ToArray();
            if (!expected.Select(distro => distro.DistroId).SequenceEqual(actual.Select(distro => distro.DistroId)))
                throw new Exception("The query omitted a distribution.");
            if (actual.Any(distro => distro.IsDefault != distro.IsDefaultDistro))
                throw new Exception("Default distribution flags disagree.");
        }

        static void Test_GetDefaultDistro()
        {
            var defaultDistro = Wsl.GetDefaultDistro();

            if (defaultDistro == null)
                throw new Exception("Cannot find the default distro.");
        }

        static void Test_ExecTest()
        {
            var outputContent = RequireDefaultDistro().RunWslCommand("cat /etc/passwd");

            if (outputContent == null)
                throw new ArgumentNullException(nameof(outputContent));

            if (outputContent.Length == 0)
                throw new Exception("No output.");
        }

        static void Test_ExecTest_OutputStream()
        {
            using var outputStream = new MemoryStream();
            var outputLength = RequireDefaultDistro().RunWslCommand("ls /dev | gzip -", outputStream);

            if (outputLength == 0)
                throw new Exception("No output.");

            outputStream.Seek(0L, SeekOrigin.Begin);
            using var gzStream = new GZipStream(outputStream, CompressionMode.Decompress, true);
            using var streamReader = new StreamReader(gzStream, new UTF8Encoding(false), false);
            var content = streamReader.ReadToEnd();

            if (string.IsNullOrWhiteSpace(content))
                throw new Exception("No output.");

            if (!content.Contains("null"))
                throw new Exception("Invalid output.");
        }
    }
}

#pragma warning restore IDE0051 // Remove unused private members
