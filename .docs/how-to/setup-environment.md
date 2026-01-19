# How to Set Up the .NET 10 Demo Environment

This guide walks you through setting up your development environment to run the .NET 10 Modern Architecture Workshop demos on either Windows or Linux.

## Prerequisites

- **Operating System**: Windows 10/11 or Linux (Ubuntu, Debian, CentOS, etc.)
- **Internet Connection**: Required for downloading .NET SDK and tools
- **Administrator/Sudo Access**: Required for installing workloads and trusting certificates

## Quick Setup (For Experienced Users)

If you're familiar with .NET development, run these commands in sequence:

```bash
# Verify .NET 10 SDK installation
dotnet --version

# Install EF Core tools
dotnet tool install --global dotnet-ef

# Install WebAssembly workload
dotnet workload install wasm-tools

# Update templates
dotnet new update

# Trust HTTPS certificate
dotnet dev-certs https --trust

# Verify setup
dotnet workload list
dotnet tool list --global
```

Then proceed to [Verification](#verification).

## Detailed Setup

Follow these steps in order for a complete setup.

### Step 1: Verify .NET 10 SDK Installation

The demos require .NET 10.0.100 or later.

```bash
dotnet --version
```

**Expected Output:**
```
10.0.100
```

If you don't have .NET 10 installed:
- Download from: https://dotnet.microsoft.com/download/dotnet/10.0
- Install the SDK for your platform
- Restart your terminal/command prompt

### Step 2: Check Installed SDKs and Runtimes

Verify you have the required runtimes:

```bash
dotnet --list-sdks
dotnet --list-runtimes
```

**Expected Output:**
```
SDKs:
10.0.100 [/usr/lib/dotnet/sdk]

Runtimes:
Microsoft.AspNetCore.App 10.0.0
Microsoft.NETCore.App 10.0.0
```

### Step 3: Install EF Core Tools

The demos use Entity Framework Core for database operations.

```bash
dotnet tool install --global dotnet-ef
```

**Note for Linux/macOS:** Add the tools directory to your PATH:

```bash
# For bash/zsh
export PATH="$PATH:$HOME/.dotnet/tools"

# For PowerShell
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"
```

### Step 4: Install WebAssembly Workload

Required for Blazor WebAssembly development and publishing.

```bash
dotnet workload install wasm-tools
```

This installs the necessary tools for WebAssembly compilation and may take several minutes.

### Step 5: Update .NET Templates

Ensure you have the latest project templates, including updated Identity scaffolding.

```bash
dotnet new update
```

### Step 6: Set Up HTTPS Development Certificate

Required for running the web demos with SSL.

First, check if a certificate exists:

```bash
dotnet dev-certs https --check
```

If no valid certificate exists, generate one:

```bash
dotnet dev-certs https
```

Trust the certificate (requires admin/sudo):

```bash
dotnet dev-certs https --trust
```

**Platform Notes:**
- **Windows**: May prompt for administrator approval
- **Linux/macOS**: Uses system trust store
- **WSL**: Certificate trust may need to be done in Windows if using Windows browser

## Verification

After completing setup, verify everything works:

### Check Workloads

```bash
dotnet workload list
```

**Expected Output:**
```
Installed Workload Id      Manifest Version       Installation Source
---------------------------------------------------------------------
wasm-tools                 10.0.102/10.0.100      SDK 10.0.100
```

### Check Global Tools

```bash
dotnet tool list --global
```

**Expected Output:**
```
Package Id      Version      Commands
-------------------------------------
dotnet-ef       10.0.2       dotnet-ef
```

### Verify EF Core Tools

```bash
dotnet ef --version
```

**Expected Output:**
```
Entity Framework Core .NET Command-line Tools
10.0.2
```

### Test Build a Demo

Build demo1 to ensure all dependencies work:

```bash
cd demo1/Demo1.IdentityFoundation
dotnet build
```

**Expected Output:** Build succeeds with no errors.

## Platform-Specific Notes

### Windows

- Use PowerShell or Command Prompt
- PATH updates persist across sessions
- Certificate trust works with Windows Certificate Manager

### Linux (including WSL)

- Use bash/zsh
- Add `.dotnet/tools` to PATH in your shell profile (`.bashrc`, `.zshrc`)
- Certificate trust may require browser-specific steps for WSL

### macOS

- Similar to Linux, but uses Keychain for certificates
- PATH setup same as Linux

## Troubleshooting

### "dotnet command not found"

- Ensure .NET SDK is installed and in PATH
- Restart your terminal/command prompt
- Check installation: `where dotnet` (Windows) or `which dotnet` (Linux/macOS)

### EF Core tools not found after installation

- Add `.dotnet/tools` to PATH
- Restart terminal
- Check: `dotnet tool list --global`

### Workload installation fails

- Ensure internet connection
- Try with admin/sudo privileges
- Check available disk space

### HTTPS certificate issues

- Run `dotnet dev-certs https --clean` then regenerate
- For WSL: Trust certificate in Windows if using Windows browser
- Check firewall/antivirus settings

### Build fails

- Ensure all workloads are installed
- Clear NuGet cache: `dotnet nuget locals all --clear`
- Restore packages: `dotnet restore`

## Next Steps

With your environment set up:

1. **Start with demo1**: Follow the Quick Start in `README.md`
2. **Run the first demo**:
   ```bash
   cd demo1/Demo1.IdentityFoundation/Demo1.IdentityFoundation
   dotnet ef database update
   dotnet watch
   ```
3. **Open in browser**: Navigate to `https://localhost:7210`
4. **Continue through demos**: Each demo builds on the previous

## Related Documentation

- [Main README](../README.md) - Workshop overview and demo lineup
- [Demo1 README](../demo1/README.md) - First demo walkthrough
- [.NET 10 Release Notes](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview) - What's new in .NET 10