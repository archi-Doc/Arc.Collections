using System;
using System.Diagnostics;
using System.IO;
using System.IO.Hashing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Threading;
using System.Threading.Tasks;
using Arc.Collections;
using Xunit;

namespace XunitTest;

public class BuilderHashQualityXxHash3Test
{
    private const int MaxExhaustiveLength = 2100;
    private const string HwIntrinsicVariable = "DOTNET_EnableHWIntrinsic";
    private const string Avx2Variable = "DOTNET_EnableAVX2";

    private static readonly long[] Seeds = [0, 1, -1, 0x0123456789ABCDEF, long.MinValue, long.MaxValue];

    [Fact]
    public void Hash64MatchesSystemIoHashingForEveryLengthAndSeed()
    {
        // A child process runs this test with SIMD disabled; make sure the requested path is really taken.
        if (Environment.GetEnvironmentVariable(HwIntrinsicVariable) == "0")
        {
            Assert.False(Vector128.IsHardwareAccelerated);
        }

        if (Environment.GetEnvironmentVariable(Avx2Variable) == "0")
        {
            Assert.False(Vector256.IsHardwareAccelerated);
        }

        var data = new byte[MaxExhaustiveLength + 8];
        new Random(318).NextBytes(data);
        foreach (var seed in Seeds)
        {
            for (var length = 0; length <= MaxExhaustiveLength; length++)
            {
                var source = data.AsSpan(length & 7, length); // Also exercises unaligned input.
                var expected = XxHash3.HashToUInt64(source, seed);
                var actual = XxHash3Slim.Hash64(source, seed);
                if (expected != actual)
                {
                    Assert.Fail($"Length {length}, seed {seed}: expected {expected:X16}, actual {actual:X16}.");
                }
            }
        }
    }

    [Fact]
    public void HashingDoesNotAllocate()
    {
        var data = new byte[5000];
        new Random(318).NextBytes(data);
        int[] lengths = [0, 3, 8, 16, 100, 200, 241, 1024, 5000];

        ulong hash = 0;
        var allocated = AllocationHelper.Measure(() => hash = HashAll(data, lengths));

        Assert.Equal(HashAll(data, lengths), hash);
        AllocationHelper.SkipUnlessOptimized();
        Assert.Equal(0, allocated);

        static ulong HashAll(byte[] data, int[] lengths)
        {
            ulong hash = 0;
            foreach (var length in lengths)
            {
                hash ^= XxHash3Slim.Hash64(data.AsSpan(0, length));
                hash ^= XxHash3Slim.Hash64(data.AsSpan(0, length), 12345);
                hash ^= XxHash3Slim.Hash64(MemoryMarshal.Cast<byte, char>(data.AsSpan(0, length & ~1)));
            }

            return hash;
        }
    }

    [Fact]
    public void CombineFunctionsMatchReferenceFormulas()
    {
        var random = new Random(318);
        Span<byte> buffer = stackalloc byte[16];
        for (var i = 0; i < 1000; i++)
        {
            random.NextBytes(buffer);
            var hash1 = BitConverter.ToUInt64(buffer);
            var hash2 = BitConverter.ToUInt64(buffer.Slice(8));
            Assert.Equal(ReferenceCombine(hash1, hash2), XxHash3Slim.Combine(hash1, hash2));
            Assert.Equal(ReferenceCombineRotateMultiply(hash1, hash2), XxHash3Slim.CombineRotateMultiply(hash1, hash2));
        }

        ulong[] edges = [0, 1, ulong.MaxValue, 0x8000_0000_0000_0000];
        foreach (var hash1 in edges)
        {
            foreach (var hash2 in edges)
            {
                Assert.Equal(ReferenceCombine(hash1, hash2), XxHash3Slim.Combine(hash1, hash2));
                Assert.Equal(ReferenceCombineRotateMultiply(hash1, hash2), XxHash3Slim.CombineRotateMultiply(hash1, hash2));
            }
        }

        // Both combiners depend on argument order.
        Assert.NotEqual(XxHash3Slim.Combine(1, 2), XxHash3Slim.Combine(2, 1));
        Assert.NotEqual(XxHash3Slim.CombineRotateMultiply(1, 2), XxHash3Slim.CombineRotateMultiply(2, 1));
    }

    // SIMD availability is fixed per process, so the fallback paths are verified by running
    // Hash64MatchesSystemIoHashingForEveryLengthAndSeed in a child test process.
    [Theory]
    [InlineData(HwIntrinsicVariable)] // Scalar fallback.
    [InlineData(Avx2Variable)] // Vector128 path on x64.
    public async Task FallbackPathsMatchSystemIoHashingInChildProcess(string variable)
    {
        var assemblyPath = typeof(BuilderHashQualityXxHash3Test).Assembly.Location;
        var processPath = Environment.ProcessPath;
        ProcessStartInfo startInfo;
        if (processPath is not null && Path.GetFileNameWithoutExtension(processPath).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        {// Hosted by the dotnet muxer.
            startInfo = new(processPath);
            startInfo.ArgumentList.Add("exec");
            startInfo.ArgumentList.Add(assemblyPath);
        }
        else
        {// Use the application host of the test assembly.
            var appHostPath = Path.ChangeExtension(assemblyPath, OperatingSystem.IsWindows() ? ".exe" : null);
            Assert.SkipUnless(File.Exists(appHostPath), "The test application host was not found.");
            startInfo = new(appHostPath);
        }

        var resultsDirectory = Path.Combine(Path.GetTempPath(), "ArcCollectionsXxHash3-" + Guid.NewGuid().ToString("N"));
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.UseShellExecute = false;
        startInfo.WorkingDirectory = Path.GetDirectoryName(assemblyPath)!;
        startInfo.ArgumentList.Add("--filter-method");
        startInfo.ArgumentList.Add($"{typeof(BuilderHashQualityXxHash3Test).FullName}.{nameof(this.Hash64MatchesSystemIoHashingForEveryLengthAndSeed)}");
        startInfo.ArgumentList.Add("--results-directory");
        startInfo.ArgumentList.Add(resultsDirectory);
        startInfo.Environment[variable] = "0";

        try
        {
            using var process = Process.Start(startInfo)!;
            var output = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
            var error = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken))
            {
                timeout.CancelAfter(TimeSpan.FromMinutes(5));
                try
                {
                    await process.WaitForExitAsync(timeout.Token);
                }
                catch (OperationCanceledException)
                {
                    process.Kill(true);
                    throw;
                }
            }

            Assert.True(process.ExitCode == 0, $"Exit code {process.ExitCode}.\n{await output}\n{await error}");
        }
        finally
        {
            try
            {
                Directory.Delete(resultsDirectory, true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    private static ulong ReferenceAvalanche(ulong hash)
    {
        hash ^= hash >> 37;
        hash *= 0x165667919E3779F9;
        return hash ^ (hash >> 32);
    }

    private static ulong ReferenceCombine(ulong hash1, ulong hash2)
    {
        var upper = Math.BigMul(hash1 ^ 0xBE4BA423396CFEB8, hash2 ^ 0x1CAD21F72C81017C, out var lower);
        return ReferenceAvalanche((upper ^ lower) + hash1 + BitOperations.RotateLeft(hash2, 27));
    }

    private static ulong ReferenceCombineRotateMultiply(ulong hash1, ulong hash2)
    {
        const ulong Prime1 = 0x9E3779B185EBCA87;
        const ulong Prime2 = 0xC2B2AE3D27D4EB4F;
        var state = unchecked(2 * Prime1);
        state = BitOperations.RotateLeft(state + (hash1 * Prime2), 31) * Prime1;
        state = BitOperations.RotateLeft(state + (hash2 * Prime2), 31) * Prime1;
        return ReferenceAvalanche(state);
    }
}
