param(
    [string] $nugetApiKey = "",
    [bool]   $nugetPublish = $false
)

Install-package BuildUtils -Confirm:$false -Scope CurrentUser -Force
Import-Module BuildUtils

$runningDirectory = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition

$nugetTempDir = "$runningDirectory/artifacts/NuGet"

if (Test-Path $nugetTempDir) 
{
    Write-host "Cleaning temporary nuget path $nugetTempDir"
    Remove-Item $nugetTempDir -Recurse -Force
}

dotnet tool restore
Assert-LastExecution -message "Unable to restore tooling." -haltExecution $true

$gitVersionOutput = dotnet-gitversion /config .config/GitVersion.yml | Out-String
Write-host "GitVersion output: $gitVersionOutput"
$version = $gitVersionOutput | Out-String | ConvertFrom-Json

Write-Verbose "Parsed value to be returned"
$assemblyVer = $version.AssemblySemVer 
$assemblyFileVersion = $version.AssemblySemFileVer
$nugetPackageVersion = $version.NuGetVersionV2
$assemblyInformationalVersion = $version.FullBuildMetaData

Write-host "assemblyInformationalVersion   = $assemblyInformationalVersion"
Write-host "assemblyVer                    = $assemblyVer"
Write-host "assemblyFileVersion            = $assemblyFileVersion"
Write-host "nugetPackageVersion            = $nugetPackageVersion"

# Now restore packages and build everything.
Write-Host "\n\n*******************RESTORING PACKAGES*******************"
dotnet restore "$runningDirectory/src/KernelMemory.ElasticSearch.sln"
Assert-LastExecution -message "Error in restoring packages." -haltExecution $true

Write-Host "\n\n*******************TESTING SOLUTION*******************"
dotnet test "$runningDirectory/src/KernelMemory.ElasticSearch.FunctionalTests/KernelMemory.ElasticSearch.FunctionalTests.csproj" /p:CollectCoverage=true /p:CoverletOutput=TestResults/ /p:CoverletOutputFormat=lcov
Assert-LastExecution -message "Error in test running." -haltExecution $true

Write-Host "\n\n*******************BUILDING SOLUTION*******************"
dotnet build "$runningDirectory/src/KernelMemory.ElasticSearch.sln" --configuration release
Assert-LastExecution -message "Error in building in release configuration" -haltExecution $true

Write-Host "\n\n*******************PUBLISHING SOLUTION*******************"
dotnet pack "$runningDirectory/src/KernelMemory.ElasticSearch/KernelMemory.ElasticSearch.csproj" --configuration release -o "$runningDirectory/artifacts/NuGet" /p:PackageVersion=$nugetPackageVersion /p:AssemblyVersion=$assemblyVer /p:FileVersion=$assemblyFileVer /p:InformationalVersion=$assemblyInformationalVersion
Assert-LastExecution -message "Error in creating nuget packages.." -haltExecution $true

if ($true -eq $nugetPublish) 
{
    Write-Host "\n\n*******************PUBLISHING NUGET PACKAGE*******************"
    dotnet nuget push .\artifacts\NuGet\** --source https://api.nuget.org/v3/index.json --api-key $nugetApiKey --skip-duplicate
    Assert-LastExecution -message "Error pushing nuget packages to nuget.org." -haltExecution $true
}