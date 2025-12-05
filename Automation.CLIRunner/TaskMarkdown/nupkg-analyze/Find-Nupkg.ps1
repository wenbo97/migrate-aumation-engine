param(
    [Parameter(Mandatory = $true)]
    [string]$RootPath
)

$logOutputPath = Join-Path $PSScriptRoot "nupkg_found.log"

if (Test-Path $logOutputPath) {
    Clear-Content $logOutputPath
}

Get-ChildItem -Path $RootPath -File -Recurse | Where-Object {$_.Name -like "*.nupkg.proj" -or $_.Name -like "*.nupkg.csproj" } |
ForEach-Object {
    $logMessage = $($_.FullName)
    Write-Host $logMessage
    Add-Content -Path $logOutputPath -Value $logMessage
}