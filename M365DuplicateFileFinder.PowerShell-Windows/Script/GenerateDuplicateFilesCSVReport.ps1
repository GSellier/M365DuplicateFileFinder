$OutputFileNamePrefix = "Reports\PotentialDuplicateFiles-"
$DependenciesFolder = "Script\Dependencies"

$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop

# This script requires PowerShell 7.6 to be able to load the library, which runs on .NET 10.
if ($PSVersionTable.PSVersion -lt 7.6)
{
    throw "PowerShell 7.6 or greater is required."
}

Add-Type -Path "$DependenciesFolder\M365DuplicateFileFinder.dll"

try
{
    $graphReader = New-Object -TypeName M365DuplicateFileFinder.Readers.GraphFileReader

    $duplicateFileFinder = New-Object M365DuplicateFileFinder.DuplicateFileFinder
    $duplicateFileFinder.FileReader = $graphReader

    $duplicateFileGroups = $duplicateFileFinder.GetDuplicateFilesAsync().GetAwaiter().GetResult()
}
finally
{
    if ($null -ne $graphReader)
    {
        $graphReader.Dispose()
    }
}

$fileListToExport = @()

$groupId = 0
$duplicateGroupIdProperty = @{ Name = "DuplicateGroupId"; Expression = {$groupId} }

foreach ($duplicateFileGroup in $duplicateFileGroups)
{
    $groupId++
    $fileListToExport += $duplicateFileGroup | Select-Object $duplicateGroupIdProperty, *
}

$timestamp = Get-Date -Format FileDateTimeUniversal
$outputFileName = $OutputFileNamePrefix + $timestamp + ".csv"

$fileListToExport | Export-Csv -Path $outputFileName -UseCulture

Write-Host "Results saved to $outputFileName"