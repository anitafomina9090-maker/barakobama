#!/bin/bash

echo "Building C# Crypter Builder..."

cd Builder

# Build with .NET SDK
if command -v dotnet &> /dev/null; then
    echo "[*] Building with dotnet..."
    dotnet build -c Release
    echo "[+] Done! Binary: Builder/bin/Release/net6.0/Builder"
else
    echo "[!] .NET SDK not found. Install from: https://dotnet.microsoft.com/download"
    exit 1
fi

echo ""
echo "Usage:"
echo "  ./Builder/bin/Release/net6.0/Builder -in malware.exe -password mypass -stealth"
