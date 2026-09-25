using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Autoframe {
    public static class Cancellation {
        delegate bool Handler(uint control);
        [DllImport("kernel32.dll",SetLastError=true)] static extern bool SetConsoleCtrlHandler(Handler h,bool add);
        static readonly Handler callback=control => { if(control<=1) { Requested=true; return true; } return false; };
        public static volatile bool Requested;
        static bool registered;
        public static void Register() { Requested=false; registered=SetConsoleCtrlHandler(callback,true); }
        public static void Unregister() { if(registered) SetConsoleCtrlHandler(callback,false); registered=false; Requested=false; }
    }
    public static class Json {
        public static object Parse(string value) {
            using var doc=JsonDocument.Parse(value,new JsonDocumentOptions { MaxDepth=100 });
            return Read(doc.RootElement);
        }
        static object Read(JsonElement e) {
            switch(e.ValueKind) {
                case JsonValueKind.Object:
                    var map=new System.Collections.Hashtable(StringComparer.Ordinal);
                    foreach(var p in e.EnumerateObject()) {
                        if(map.Contains(p.Name)) throw new InvalidDataException("Duplicate JSON key: "+p.Name);
                        map.Add(p.Name,Read(p.Value));
                    }
                    return map;
                case JsonValueKind.Array:
                    var list=new List<object>(); foreach(var item in e.EnumerateArray()) list.Add(Read(item)); return list.ToArray();
                case JsonValueKind.String: return e.GetString();
                case JsonValueKind.Number: if(e.TryGetInt64(out long n)) return n; return e.GetDouble();
                case JsonValueKind.True: return true;
                case JsonValueKind.False: return false;
                default: return null;
            }
        }
        // Ordinal keys and UTF-8 without BOM are the hash contract. Duplicate keys are never collapsed.
        public static string Canonical(string value) {
            using var doc = JsonDocument.Parse(value, new JsonDocumentOptions { MaxDepth = 100 });
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream)) Write(writer, doc.RootElement);
            return Encoding.UTF8.GetString(stream.ToArray());
        }
        static void Write(Utf8JsonWriter w, JsonElement e) {
            if (e.ValueKind == JsonValueKind.Object) {
                var props = new SortedDictionary<string, JsonElement>(StringComparer.Ordinal);
                foreach (var p in e.EnumerateObject())
                    if (!props.TryAdd(p.Name, p.Value)) throw new InvalidDataException("Duplicate JSON key: " + p.Name);
                w.WriteStartObject();
                foreach (var p in props) { w.WritePropertyName(p.Key); Write(w,p.Value); }
                w.WriteEndObject();
            } else if (e.ValueKind == JsonValueKind.Array) {
                w.WriteStartArray(); foreach (var item in e.EnumerateArray()) Write(w,item); w.WriteEndArray();
            } else e.WriteTo(w);
        }
        public static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
        public static string FileHash(string path) {
            return FileHash(path,null);
        }
        public static string FileHash(string path, Action tick) {
            using var f = new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read,65536,FileOptions.SequentialScan);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer=System.Buffers.ArrayPool<byte>.Shared.Rent(65536);
            var clock=Stopwatch.StartNew();
            try {
                int n;
                while((n=f.Read(buffer,0,buffer.Length))!=0) {
                    hash.AppendData(buffer,0,n);
                    if(tick!=null && clock.ElapsedMilliseconds>=1000) { tick(); clock.Restart(); }
                }
                return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
            } finally { System.Buffers.ArrayPool<byte>.Shared.Return(buffer); }
        }
        public static void StoreEvidence(string source, string destination, string expectedHash, Action tick) {
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            string tmp=destination+"."+Guid.NewGuid().ToString("N")+".tmp";
            var buffer=System.Buffers.ArrayPool<byte>.Shared.Rent(65536);
            try {
                tick?.Invoke();
                using(var input=new FileStream(source,FileMode.Open,FileAccess.Read,FileShare.Read,65536,FileOptions.SequentialScan))
                using(var output=new FileStream(tmp,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                using(var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256)) {
                    var clock=Stopwatch.StartNew(); int n;
                    while((n=input.Read(buffer,0,buffer.Length))!=0) {
                        output.Write(buffer,0,n); hash.AppendData(buffer,0,n);
                        if(clock.ElapsedMilliseconds>=1000) { tick?.Invoke(); clock.Restart(); }
                    }
                    if(Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant()!=expectedHash)
                        throw new InvalidDataException("Evidence hash mismatch.");
                    output.Flush(true);
                }
                if(File.Exists(destination)) {
                    if(FileHash(destination,tick)==expectedHash) return;
                    File.Copy(destination,destination+".corrupt-"+Guid.NewGuid().ToString("N"),false);
                }
                tick?.Invoke();
                File.Move(tmp,destination,true);
            } finally {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                if(File.Exists(tmp)) File.Delete(tmp);
            }
        }
        public static void Atomic(string path, string value) {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string tmp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try {
                using (var f = new FileStream(tmp,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {
                    byte[] b = Encoding.UTF8.GetBytes(value); f.Write(b,0,b.Length); f.Flush(true);
                }
                File.Move(tmp,path,true);
            } finally { if(File.Exists(tmp)) File.Delete(tmp); }
        }
    }

    // A gated launcher is assigned to a kill-on-close job before it is allowed to spawn the CLI.
    // The kernel tracks descendants even if they outlive their parent or the runner crashes.
    public sealed class Child : IDisposable {
        [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern IntPtr CreateJobObject(IntPtr a,string n);
        [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)] static extern IntPtr OpenJobObject(uint access,bool inherit,string name);
        [DllImport("kernel32.dll",SetLastError=true)] static extern bool SetInformationJobObject(IntPtr j,int c,IntPtr p,uint l);
        [DllImport("kernel32.dll",SetLastError=true)] static extern bool AssignProcessToJobObject(IntPtr j,IntPtr p);
        [DllImport("kernel32.dll",SetLastError=true)] static extern bool QueryInformationJobObject(IntPtr j,int c,out Accounting p,uint l,IntPtr r);
        [DllImport("kernel32.dll",SetLastError=true)] static extern bool TerminateJobObject(IntPtr j,uint code);
        [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
        [StructLayout(LayoutKind.Sequential)] struct Basic { public long PerProcess,PerJob; public uint Flags; public UIntPtr Min,Max; public uint Active; public UIntPtr Affinity; public uint Priority,Scheduling; }
        [StructLayout(LayoutKind.Sequential)] struct Io { public ulong ReadOps,WriteOps,OtherOps,ReadBytes,WriteBytes,OtherBytes; }
        [StructLayout(LayoutKind.Sequential)] struct Extended { public Basic Basic; public Io Io; public UIntPtr ProcessMemory,JobMemory,PeakProcess,PeakJob; }
        [StructLayout(LayoutKind.Sequential)] struct Accounting { public long User,Kernel,PeriodUser,PeriodKernel; public uint Faults,Total,Active,Terminated; }
        IntPtr job;
        Process process;
        FileStream stdout,stderr;
        Task outputCopy,errorCopy;
        public int Id => process.Id;
        public string StartUtc => process.StartTime.ToUniversalTime().ToString("O");
        public bool Exited => process.HasExited;
        public int ExitCode => process.ExitCode;
        public bool Drained => outputCopy.IsCompleted && errorCopy.IsCompleted;
        public uint ActiveCount {
            get {
                if (!QueryInformationJobObject(job,1,out Accounting a,(uint)Marshal.SizeOf<Accounting>(),IntPtr.Zero))
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                return a.Active;
            }
        }
        public static bool RecoverJob(string name) {
            IntPtr handle=OpenJobObject(0x0004|0x0008,false,name);
            if(handle==IntPtr.Zero) {
                if(Marshal.GetLastWin32Error()==2) return true;
                throw new System.ComponentModel.Win32Exception();
            }
            try {
                if(!TerminateJobObject(handle,2)) throw new System.ComponentModel.Win32Exception();
                if(!QueryInformationJobObject(handle,1,out Accounting a,(uint)Marshal.SizeOf<Accounting>(),IntPtr.Zero)) throw new System.ComponentModel.Win32Exception();
                return a.Active==0;
            } finally { CloseHandle(handle); }
        }
        public Child(string executable,string[] arguments,string cwd,string outPath,string errPath,string jobName) {
            if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("This adapter requires Windows Job Objects.");
            job=CreateJobObject(IntPtr.Zero,jobName);
            if(job==IntPtr.Zero) throw new System.ComponentModel.Win32Exception();
            var info=new Extended(); info.Basic.Flags=0x2000; // JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
            IntPtr mem=Marshal.AllocHGlobal(Marshal.SizeOf<Extended>());
            try {
                Marshal.StructureToPtr(info,mem,false);
                if(!SetInformationJobObject(job,9,mem,(uint)Marshal.SizeOf<Extended>())) throw new System.ComponentModel.Win32Exception();
                var si=new ProcessStartInfo(executable) {WorkingDirectory=cwd,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
                foreach(string a in arguments) si.ArgumentList.Add(a);
                stdout=new FileStream(outPath,FileMode.CreateNew,FileAccess.Write,FileShare.Read);
                stderr=new FileStream(errPath,FileMode.CreateNew,FileAccess.Write,FileShare.Read);
                process=Process.Start(si);
                if(!AssignProcessToJobObject(job,process.Handle)) throw new System.ComponentModel.Win32Exception();
                outputCopy=process.StandardOutput.BaseStream.CopyToAsync(stdout);
                errorCopy=process.StandardError.BaseStream.CopyToAsync(stderr);
            } catch { Dispose(); throw; }
            finally { Marshal.FreeHGlobal(mem); }
        }
        public void Flush() {
            // A failed async copy must interrupt the live Worker, not wait for its exit.
            if(outputCopy?.IsCompleted==true) outputCopy.GetAwaiter().GetResult();
            if(errorCopy?.IsCompleted==true) errorCopy.GetAwaiter().GetResult();
            stdout?.Flush(); stderr?.Flush();
        }
        public void Stop() { if(job!=IntPtr.Zero && !TerminateJobObject(job,2)) throw new System.ComponentModel.Win32Exception(); }
        public void Finish() { outputCopy.GetAwaiter().GetResult(); errorCopy.GetAwaiter().GetResult(); stdout.Flush(true); stderr.Flush(true); }
        public void Dispose() {
            if(job!=IntPtr.Zero) { CloseHandle(job); job=IntPtr.Zero; }
            try { if(process!=null && !process.HasExited) process.Kill(true); } catch { }
            // Do not block indefinitely waiting for a pipe held by an unconfirmed child.
            if(outputCopy==null || outputCopy.IsCompleted) stdout?.Dispose();
            if(errorCopy==null || errorCopy.IsCompleted) stderr?.Dispose();
            process?.Dispose();
        }
    }
}
