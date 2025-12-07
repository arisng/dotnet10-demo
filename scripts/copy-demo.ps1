<#
# VS Code Scripts

This directory contains automation scripts for the dotnet10-demo workspace.

## copy-demo.ps1

Automates the creation of a new incremental demo by copying the previous demo and renaming all files, folders, and namespaces with a custom demo name.

### Namespace Structure

The script creates projects following this naming convention:

- `Demo$NewDemoNumber.$DemoName` (main project)
- `Demo$NewDemoNumber.$DemoName.Client` (client project)
- `Demo$NewDemoNumber.$DemoName.Shared` (shared project)

Example: `Demo5.ApiGateway`, `Demo5.ApiGateway.Client`, `Demo5.ApiGateway.Shared`

### Usage

#### Via VS Code Task (Recommended)

1. Press `Ctrl+Shift+P` (or `Cmd+Shift+P` on Mac)
2. Type "Tasks: Run Task"
3. Select "Create New Demo (Copy Previous)"
4. Enter the new demo number when prompted (e.g., 5)
5. Enter the demo name in PascalCase (e.g., ApiGateway, GraphQLIntegration)

#### Via PowerShell

```powershell
.\.vscode\scripts\copy-demo.ps1 -NewDemoNumber 5 -DemoName "ApiGateway"
```

### What It Does

1. **Detects** the source demo name automatically
2. **Copies** the previous demo folder (e.g., demo4 → demo5)
3. **Renames** all folders and files with the new demo number and name
4. **Updates** all file contents:
   - Namespaces in C# files (e.g., `Demo4.EntraIntegration` → `Demo5.ApiGateway`)
   - Project references in .csproj files
   - Solution files (.slnx)
   - Configuration files
   - README references
5. **Cleans** bin, obj, and .vs directories
6. **Removes** old .sln files (keeps only .slnx)
7. **Reports** what needs to be done next

### What You Need to Do After

1. Update `demo<N>/README.md` with the new demo's purpose and "What's New" section
2. Update the root `README.md` to include the new demo in the list
3. Build and test: `dotnet build demo<N>`
4. Implement the new features for this incremental demo

### Requirements

- PowerShell 5.1 or higher
- Previous demo must exist (e.g., demo4 must exist to create demo5)
- Target demo must not already exist
- Demo name must be in PascalCase format

### Error Handling

The script will stop and show an error if:

- You try to create demo1 (no demo0 to copy from)
- The source demo doesn't exist
- The target demo already exists
- Demo name is not in PascalCase format
- Any file operation fails
#>

param(
    [Parameter(Mandatory=$true)]
    [int]$NewDemoNumber,
    
    [Parameter(Mandatory=$true)]
    [string]$DemoName
)

$ErrorActionPreference = "Stop"

# Calculate source demo number
$SourceDemoNumber = $NewDemoNumber - 1

if ($SourceDemoNumber -lt 1) {
    Write-Error "Cannot create demo1 from demo0. Please specify a demo number >= 2"
    exit 1
}

# Validate DemoName (PascalCase, no spaces)
if ($DemoName -notmatch '^[A-Z][a-zA-Z0-9]*$') {
    Write-Error "DemoName must be in PascalCase format (e.g., 'EntraIntegration', 'ApiGateway')"
    exit 1
}

# Define paths
$WorkspaceRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$SourceDir = Join-Path $WorkspaceRoot "demo$SourceDemoNumber"
$TargetDir = Join-Path $WorkspaceRoot "demo$NewDemoNumber"

# Detect source demo name from the source directory
$sourceDemoProjects = Get-ChildItem -Path $SourceDir -Directory | Where-Object { 
    $_.Name -match "^Demo$SourceDemoNumber\.([A-Za-z]+)$" 
}
if ($sourceDemoProjects.Count -eq 0) {
    Write-Error "Could not detect source demo name pattern in $SourceDir"
    exit 1
}
$SourceDemoName = $sourceDemoProjects[0].Name -replace "^Demo$SourceDemoNumber\.", ""
$SourceDemoName = $SourceDemoName -replace "\.(Client|Shared)$", ""

Write-Host "Detected source demo name: $SourceDemoName" -ForegroundColor Cyan

# Validate source exists
if (-not (Test-Path $SourceDir)) {
    Write-Error "Source directory does not exist: $SourceDir"
    exit 1
}

# Validate target doesn't exist
if (Test-Path $TargetDir) {
    Write-Error "Target directory already exists: $TargetDir"
    exit 1
}

Write-Host "Copying demo$SourceDemoNumber ($SourceDemoName) to demo$NewDemoNumber ($DemoName)..." -ForegroundColor Cyan

# Copy the entire directory, excluding .vs folder
Write-Host "  Excluding: .vs, bin, obj folders" -ForegroundColor Gray
robocopy $SourceDir $TargetDir /E /NFL /NDL /NJH /NJS /NC /NS /NP /XD ".vs" "bin" "obj" | Out-Null
if ($LASTEXITCODE -ge 8) {
    Write-Error "Copy operation failed with exit code $LASTEXITCODE"
    exit 1
}

Write-Host "Renaming folders and files..." -ForegroundColor Cyan

# Get all directories and files in the new demo (sorted by depth, deepest first for renaming)
$allItems = Get-ChildItem -Path $TargetDir -Recurse | Where-Object { 
    $_.FullName -notmatch '\\(\.vs|bin|obj)\\' 
} | Sort-Object { $_.FullName.Split([IO.Path]::DirectorySeparatorChar).Count } -Descending

# Rename directories and files that contain the old demo name
foreach ($item in $allItems) {
    # Skip if item no longer exists (parent was renamed)
    if (-not (Test-Path $item.FullName)) {
        continue
    }
    
    $oldName = $item.Name
    $newName = $oldName
    
    # Replace Demo<number>.<OldName> with Demo<number>.<NewName>
    $newName = $newName -replace "Demo$SourceDemoNumber\.$SourceDemoName", "Demo$NewDemoNumber.$DemoName"
    
    # Also handle cases without the demo name (unlikely but just in case)
    if ($newName -eq $oldName) {
        $newName = $newName -replace "Demo$SourceDemoNumber", "Demo$NewDemoNumber"
    }
    
    if ($oldName -ne $newName) {
        $parentPath = Split-Path -Parent $item.FullName
        $newPath = Join-Path $parentPath $newName
        Write-Host "  Renaming: $($item.FullName) -> $newPath" -ForegroundColor Gray
        Rename-Item -Path $item.FullName -NewName $newName -Force
    }
}

Write-Host "Updating file contents..." -ForegroundColor Cyan

# Get all text files to update (exclude binary and build artifacts)
$textFiles = Get-ChildItem -Path $TargetDir -Recurse -File | Where-Object {
    $_.FullName -notmatch '\\(\.vs|bin|obj)\\' -and
    $_.Extension -match '\.(cs|csproj|slnx|json|razor|md|css|js|html|xml|config)$'
}

foreach ($file in $textFiles) {
    $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
    $originalContent = $content
    
    # Replace Demo<number>.<OldName> with Demo<number>.<NewName> (namespaces, types, etc.)
    $content = $content -replace "Demo$SourceDemoNumber\.$SourceDemoName", "Demo$NewDemoNumber.$DemoName"
    
    # Replace demo<number> references (lowercase, folder references)
    $content = $content -replace "demo$SourceDemoNumber", "demo$NewDemoNumber"
    
    # For .slnx files, also ensure project paths are correct
    if ($file.Extension -eq '.slnx') {
        $content = $content -replace "Demo$SourceDemoNumber\.$SourceDemoName\.", "Demo$NewDemoNumber.$DemoName."
        $content = $content -replace "/Demo$SourceDemoNumber\.$SourceDemoName\.", "/Demo$NewDemoNumber.$DemoName."
    }
    
    if ($content -ne $originalContent) {
        Write-Host "  Updating: $($file.FullName)" -ForegroundColor Gray
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline
    }
}

# Clean build artifacts and old .sln files
Write-Host "Cleaning build artifacts and old solution files..." -ForegroundColor Cyan

# Remove .vs, bin, and obj directories (if any remain)
Get-ChildItem -Path $TargetDir -Recurse -Directory | Where-Object { 
    $_.Name -eq '.vs' -or $_.Name -eq 'bin' -or $_.Name -eq 'obj' 
} | ForEach-Object {
    Write-Host "  Removing: $($_.FullName)" -ForegroundColor Gray
    Remove-Item -Path $_.FullName -Recurse -Force
}

# Remove old .sln files (keep only .slnx)
Get-ChildItem -Path $TargetDir -Recurse -File | Where-Object {
    $_.Extension -eq '.sln'
} | ForEach-Object {
    Write-Host "  Removing old .sln file: $($_.FullName)" -ForegroundColor Gray
    Remove-Item -Path $_.FullName -Force
}

Write-Host "`nDemo$NewDemoNumber.$DemoName created successfully!" -ForegroundColor Green
Write-Host "Location: $TargetDir" -ForegroundColor Green
Write-Host "Projects:" -ForegroundColor Gray
Write-Host "  - Demo$NewDemoNumber.$DemoName" -ForegroundColor Gray
Write-Host "  - Demo$NewDemoNumber.$DemoName.Client" -ForegroundColor Gray
Write-Host "  - Demo$NewDemoNumber.$DemoName.Shared" -ForegroundColor Gray
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Update demo$NewDemoNumber/README.md with the new demo's purpose" -ForegroundColor Yellow
Write-Host "2. Update the root README.md to include the new demo" -ForegroundColor Yellow
Write-Host "3. Build and test the new demo" -ForegroundColor Yellow
Write-Host "`nTo build: dotnet build `"$TargetDir`"" -ForegroundColor Cyan
