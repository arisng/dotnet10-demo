# Migrate Issue Metadata to YAML Frontmatter
# Converts legacy inline metadata to YAML frontmatter format

param(
    [string]$IssuesPath = "_docs/issues",
    [string]$TestFile = "",
    [switch]$DryRun = $false,
    [switch]$Verbose = $false
)

# Type mappings for normalization
$typeMap = @{
    "Technical Issue"           = "bug"
    "Design Decision"           = "design-decision"
    "Architecture Decision"     = "adr"
    "Learning Insight"          = "task"
    "Work Item / Task"          = "task"
    "Bug"                       = "bug"
    "Feature"                   = "feature"
    "RFC"                       = "rfc"
    "ADR"                       = "adr"
    "Task"                      = "task"
    "Epic"                      = "epic"
}

# Status mappings for normalization
$statusMap = @{
    "Resolved"          = "resolved"
    "✅ Completed"      = "completed"
    "Completed"         = "completed"
    "In Progress"       = "in-progress"
    "Proposed"          = "proposed"
    "Documented"        = "completed"
    "Deprecated"        = "deprecated"
    "Draft"             = "draft"
    "Open for Comment"  = "open-for-comment"
    "Accepted"          = "accepted"
    "Rejected"          = "rejected"
    "Not Started"       = "not-started"
    "Planning"          = "proposed"
    "Open"              = "draft"
}

# Severity mappings
$severityMap = @{
    "Critical" = "critical"
    "High"     = "high"
    "Medium"   = "medium"
    "Low"      = "low"
}

function Write-VerboseLog {
    param([string]$Message)
    if ($Verbose) {
        Write-Host "[VERBOSE] $Message" -ForegroundColor Cyan
    }
}

function Normalize-Type {
    param([string]$Type)

    $Type = $Type.Trim()
    if ($typeMap.ContainsKey($Type)) {
        return $typeMap[$Type]
    }

    # Return lowercase kebab-case version
    return $Type.ToLower() -replace '\s+', '-'
}

function Normalize-Status {
    param([string]$Status)

    $Status = $Status.Trim()

    # Remove emoji and special characters first
    $Status = $Status -replace '[✅❌🚧]', ''
    $Status = $Status -replace '---.*$', ''  # Remove everything after ---
    $Status = $Status.Trim()

    if ($statusMap.ContainsKey($Status)) {
        return $statusMap[$Status]
    }

    # Handle common patterns
    if ($Status -match 'completed|done|finished') { return "completed" }
    if ($Status -match 'resolved|fixed|closed') { return "resolved" }
    if ($Status -match 'in[- ]?progress|ongoing|wip') { return "in-progress" }
    if ($Status -match 'not[- ]?started|pending|todo') { return "not-started" }
    if ($Status -match 'planning|planned') { return "proposed" }
    if ($Status -match 'open|new') { return "draft" }

    # Return lowercase kebab-case version
    return $Status.ToLower() -replace '\s+', '-' -replace '[^a-z0-9-]', ''
}

function Normalize-Severity {
    param([string]$Severity)

    $Severity = $Severity.Trim()
    if ($severityMap.ContainsKey($Severity)) {
        return $severityMap[$Severity]
    }

    return $Severity.ToLower()
}

function Extract-Tags {
    param([string]$Content)

    $tags = @()

    # Look for "**Tags:**" pattern at the end of document
    if ($Content -match '\*\*Tags:\*\*\s*([^\r\n]+)') {
        $tagLine = $matches[1].Trim()
        $tags = $tagLine -split '\s+' | Where-Object { $_ -match '\w+' }
    }

    return $tags
}

function Parse-LegacyMetadata {
    param([string]$Content)

    $metadata = @{
        Date     = ""
        Type     = ""
        Severity = ""
        Status   = ""
        Tags     = @()
    }

    # Split content to get header section before first heading or first ---
    $lines = $Content -split "`r?`n"
    $headerEndIndex = 0

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]

        # Stop at first markdown heading (that's not part of metadata)
        if ($line -match '^#\s+\w+' -and $i -gt 5) {
            $headerEndIndex = $i
            break
        }

        # Stop at horizontal rule if we already have some metadata
        if ($line -match '^---+$' -and $i -gt 0 -and ($metadata.Date -or $metadata.Type)) {
            $headerEndIndex = $i
            break
        }
    }

    if ($headerEndIndex -eq 0) {
        $headerEndIndex = [Math]::Min(20, $lines.Count)
    }

    $header = ($lines[0..$headerEndIndex] -join "`n")

    Write-VerboseLog "Header section (first $headerEndIndex lines)"

    # Extract Date
    if ($header -match '\*\*Date:\*\*\s*([^\r\n]+)') {
        $metadata.Date = $matches[1].Trim()
        Write-VerboseLog "Found Date: $($metadata.Date)"
    }

    # Extract Type (handle both "Type:" and "Issue Type:")
    if ($header -match '\*\*(?:Issue\s+)?Type:\*\*\s*([^\r\n]+)') {
        $metadata.Type = $matches[1].Trim()
        Write-VerboseLog "Found Type: $($metadata.Type)"
    }

    # Extract Severity
    if ($header -match '\*\*Severity:\*\*\s*([^\r\n]+)') {
        $metadata.Severity = $matches[1].Trim()
        Write-VerboseLog "Found Severity: $($metadata.Severity)"
    }

    # Extract Status
    if ($header -match '\*\*Status:\*\*\s*([^\r\n]+)') {
        $metadata.Status = $matches[1].Trim()
        Write-VerboseLog "Found Status: $($metadata.Status)"
    }

    # Extract Tags from end of document
    $metadata.Tags = Extract-Tags -Content $Content
    if ($metadata.Tags.Count -gt 0) {
        Write-VerboseLog "Found Tags: $($metadata.Tags -join ', ')"
    }

    return $metadata
}

function Remove-LegacyMetadata {
    param([string]$Content)

    # Remove inline metadata fields
    $Content = $Content -replace '\*\*Date:\*\*[^\r\n]+\r?\n?', ''
    $Content = $Content -replace '\*\*(?:Issue\s+)?Type:\*\*[^\r\n]+\r?\n?', ''
    $Content = $Content -replace '\*\*Severity:\*\*[^\r\n]+\r?\n?', ''
    $Content = $Content -replace '\*\*Status:\*\*[^\r\n]+\r?\n?', ''
    $Content = $Content -replace '\*\*Tags:\*\*[^\r\n]+\r?\n?', ''

    # Remove standalone horizontal rules at the beginning (after title)
    $lines = $Content -split "`r?`n"
    $cleanedLines = @()
    $skipNextHR = $false
    $titleFound = $false
    $emptyLinesAfterTitle = 0

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]

        # Track if we found the title
        if ($line -match '^#\s+') {
            $titleFound = $true
            $skipNextHR = $true
            $emptyLinesAfterTitle = 0
            $cleanedLines += $line
            continue
        }

        # After title, count empty lines and skip HR
        if ($titleFound -and $skipNextHR) {
            if ($line -match '^\s*$') {
                $emptyLinesAfterTitle++
                continue
            }
            elseif ($line -match '^---+$') {
                $skipNextHR = $false
                continue
            }
            else {
                # Found non-empty, non-HR line - stop skipping
                $skipNextHR = $false
                # Add back blank line before content
                if ($emptyLinesAfterTitle -eq 0) {
                    $cleanedLines += ""
                }
            }
        }

        # Skip empty lines at the very beginning
        if ($cleanedLines.Count -eq 0 -and $line -match '^\s*$') {
            continue
        }

        $cleanedLines += $line
    }

    # Clean up multiple consecutive blank lines
    $result = ($cleanedLines -join "`n") -replace '(\r?\n\s*){3,}', "`n`n"

    return $result.Trim()
}

function Build-YamlFrontmatter {
    param(
        [hashtable]$Metadata,
        [string]$FileName
    )

    $yaml = @()
    $yaml += "---"

    # Date (required)
    if ($Metadata.Date) {
        $yaml += "date: $($Metadata.Date)"
    }
    else {
        # Try to extract from filename (e.g., 251209_...)
        if ($FileName -match '^(\d{6})') {
            $dateStr = $matches[1]
            $year = "20" + $dateStr.Substring(0, 2)
            $month = $dateStr.Substring(2, 2)
            $day = $dateStr.Substring(4, 2)
            $yaml += "date: $year-$month-$day"
            Write-Host "  [WARN] No date found, inferred from filename: $year-$month-$day" -ForegroundColor Yellow
        }
        else {
            $yaml += "date: $(Get-Date -Format 'yyyy-MM-dd')"
            Write-Host "  [WARN] No date found, using today's date" -ForegroundColor Yellow
        }
    }

    # Type (required)
    if ($Metadata.Type) {
        $normalizedType = Normalize-Type -Type $Metadata.Type
        $yaml += "type: $normalizedType"
    }
    else {
        $yaml += "type: task"
        Write-Host "  [WARN] No type found, defaulting to 'task'" -ForegroundColor Yellow
    }

    # Status (required)
    if ($Metadata.Status) {
        $normalizedStatus = Normalize-Status -Status $Metadata.Status
        $yaml += "status: $normalizedStatus"
    }
    else {
        $yaml += "status: draft"
        Write-Host "  [WARN] No status found, defaulting to 'draft'" -ForegroundColor Yellow
    }

    # Severity (optional)
    if ($Metadata.Severity) {
        $normalizedSeverity = Normalize-Severity -Severity $Metadata.Severity
        $yaml += "severity: $normalizedSeverity"
    }

    # Tags (required, multi-line array format)
    $yaml += "tags:"
    if ($Metadata.Tags.Count -gt 0) {
        foreach ($tag in $Metadata.Tags) {
            # Remove backticks, commas, and other special characters, then trim and lowercase
            $cleanTag = $tag -replace '[`'',]', '' | ForEach-Object { $_.Trim().ToLower() }
            if ($cleanTag) {
                $yaml += "  - $cleanTag"
            }
        }
    }
    else {
        # Infer tags from type
        if ($Metadata.Type) {
            $inferredTag = (Normalize-Type -Type $Metadata.Type)
            $yaml += "  - $inferredTag"
            Write-Host "  [WARN] No tags found, inferred from type: $inferredTag" -ForegroundColor Yellow
        }
        else {
            $yaml += "  - untagged"
            Write-Host "  [WARN] No tags found, using 'untagged'" -ForegroundColor Yellow
        }
    }

    $yaml += "---"
    $yaml += ""

    return ($yaml -join "`n")
}

function Has-YamlFrontmatter {
    param([string]$Content)

    # Check if content starts with --- (possibly with BOM or whitespace)
    # Must be at the very beginning, and followed by YAML-like content
    if ($Content -match '^\s*---\s*[\r\n]+\w+:') {
        return $true
    }
    return $false
}

function Migrate-IssueFile {
    param(
        [string]$FilePath,
        [switch]$DryRun
    )

    $fileName = Split-Path $FilePath -Leaf
    Write-Host "`nProcessing: $fileName" -ForegroundColor Green

    # Read the file
    $content = Get-Content $FilePath -Raw -Encoding UTF8

    # Check if already has YAML frontmatter
    if (Has-YamlFrontmatter -Content $content) {
        Write-Host "  [SKIP] Already has YAML frontmatter" -ForegroundColor Gray
        return @{
            File      = $fileName
            Status    = "Skipped"
            Reason    = "Already has YAML frontmatter"
            Modified  = $false
        }
    }

    # Parse legacy metadata
    Write-VerboseLog "Parsing legacy metadata..."
    $metadata = Parse-LegacyMetadata -Content $content

    # Build YAML frontmatter
    Write-VerboseLog "Building YAML frontmatter..."
    $yamlFrontmatter = Build-YamlFrontmatter -Metadata $metadata -FileName $fileName

    # Remove legacy metadata from content
    Write-VerboseLog "Removing legacy metadata..."
    $cleanedContent = Remove-LegacyMetadata -Content $content

    # Combine YAML frontmatter with cleaned content
    # Ensure there's a blank line after the title (h1) if immediately followed by another heading (h2, etc.)
    if ($cleanedContent -match '^(#\s+[^\r\n]+)\r?\n(#{2,}\s+)') {
        # Title (h1) immediately followed by another heading - add blank line between them
        $cleanedContent = $cleanedContent -replace '^(#\s+[^\r\n]+)\r?\n(#{2,}\s+)', "`$1`n`n`$2"
    }
    $newContent = $yamlFrontmatter + "`n" + $cleanedContent

    # Write back to file
    if (-not $DryRun) {
        Set-Content -Path $FilePath -Value $newContent -Encoding UTF8 -NoNewline
        Write-Host "  [MIGRATED] Successfully updated" -ForegroundColor Green
    }
    else {
        Write-Host "  [DRY RUN] Would update file" -ForegroundColor Cyan
        Write-Host "  YAML Frontmatter:" -ForegroundColor Cyan
        Write-Host $yamlFrontmatter -ForegroundColor DarkCyan
    }

    return @{
        File      = $fileName
        Status    = if ($DryRun) { "Dry Run" } else { "Migrated" }
        Date      = $metadata.Date
        Type      = $metadata.Type
        Severity  = $metadata.Severity
        Status_   = $metadata.Status
        Tags      = ($metadata.Tags -join ", ")
        Modified  = -not $DryRun
    }
}

# Main execution
Write-Host "=====================================" -ForegroundColor Magenta
Write-Host "Issue Metadata Migration Script" -ForegroundColor Magenta
Write-Host "=====================================" -ForegroundColor Magenta
Write-Host ""

$results = @()

if ($TestFile) {
    # Test mode - process single file
    Write-Host "TEST MODE: Processing single file" -ForegroundColor Yellow
    Write-Host "File: $TestFile" -ForegroundColor Yellow
    Write-Host ""

    $fullPath = Join-Path $IssuesPath $TestFile
    if (-not (Test-Path $fullPath)) {
        Write-Host "ERROR: File not found: $fullPath" -ForegroundColor Red
        exit 1
    }

    $result = Migrate-IssueFile -FilePath $fullPath -DryRun:$DryRun
    $results += $result
}
else {
    # Process all markdown files in issues folder
    Write-Host "BATCH MODE: Processing all issue files" -ForegroundColor Yellow
    Write-Host "Path: $IssuesPath" -ForegroundColor Yellow
    Write-Host ""

    $files = Get-ChildItem -Path $IssuesPath -Filter "*.md" | Where-Object { $_.Name -ne "index.md" }

    Write-Host "Found $($files.Count) files to process`n" -ForegroundColor Yellow

    foreach ($file in $files) {
        $result = Migrate-IssueFile -FilePath $file.FullName -DryRun:$DryRun
        $results += $result
    }
}

# Generate summary report
Write-Host "`n=====================================" -ForegroundColor Magenta
Write-Host "Migration Summary" -ForegroundColor Magenta
Write-Host "=====================================" -ForegroundColor Magenta

$migratedCount = ($results | Where-Object { $_.Status -eq "Migrated" }).Count
$skippedCount = ($results | Where-Object { $_.Status -eq "Skipped" }).Count
$dryRunCount = ($results | Where-Object { $_.Status -eq "Dry Run" }).Count

Write-Host "Total files processed: $($results.Count)"
Write-Host "Migrated: $migratedCount" -ForegroundColor Green
Write-Host "Skipped: $skippedCount" -ForegroundColor Gray
Write-Host "Dry Run: $dryRunCount" -ForegroundColor Cyan

# Save detailed report
$reportPath = Join-Path $IssuesPath "migration-report.txt"
$results | Format-Table -AutoSize | Out-String | Out-File $reportPath -Encoding UTF8
Write-Host "`nDetailed report saved to: $reportPath" -ForegroundColor Cyan

Write-Host "`nDone!" -ForegroundColor Green
