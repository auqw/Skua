<#
.SYNOPSIS
    Automated build script for Skua project
.PARAMETER Configuration
    Build configuration (Debug or Release). Default: Release
.PARAMETER Platforms
    Platforms to build. Default: x64, x86
.PARAMETER SkipClean
    Skip cleaning before build
.PARAMETER SkipInstaller
    Skip building WiX installer
.EXAMPLE
    .\Build-Skua.ps1 -Configuration Debug -Platforms x64 -SkipInstaller
#>

param(
    [ValidateSet("Debug", "Release")][string]$Configuration = "Release",
    [ValidateSet("x64", "x86")][string[]]$Platforms = @("x64", "x86"),
    [switch]$SkipClean,
    [string]$OutputPath = ".\build",
    [switch]$SkipInstaller,
    [switch]$BinaryLog,
    [switch]$Parallel
)

$ProgressPreference = "SilentlyContinue"
$script:WixInstalled = $false
$script:MSBuildPath = $null

function Write-Header([string]$Message) {
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host $Message -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan
}
function Write-Success([string]$Message) { Write-Host "[SUCCESS] $Message" -ForegroundColor Green }
function Write-BuildError([string]$Message) { Write-Host "[ERROR] $Message" -ForegroundColor Red }
function Write-Info([string]$Message) { Write-Host "[INFO] $Message" -ForegroundColor Yellow }

function Get-MSBuildPath {
    $vsWhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vsWhere) {
        $installPath = & $vsWhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
        if ($installPath) {
            $msbuildPath = Join-Path $installPath "MSBuild\Current\Bin\MSBuild.exe"
            if (Test-Path $msbuildPath) { return $msbuildPath }
        }
    }
    
    $msbuildPath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\*\MSBuild\Current\Bin\MSBuild.exe"
    $found = Get-ChildItem -Path $msbuildPath -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($found) { return $found.FullName }
    
    $msbuildPath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
    if (Test-Path $msbuildPath) { return $msbuildPath }
    
    return $null
}

function Test-WixInstalled {
    try {
        $null = wix --version 2>$null
        return $LASTEXITCODE -eq 0
    }
    catch { return $false }
}

function Test-Prerequisites {
    Write-Header "Checking Prerequisites"
    $hasErrors = $false
    
    $script:MSBuildPath = Get-MSBuildPath
    if ($script:MSBuildPath) { Write-Success "MSBuild found: $script:MSBuildPath" }
    else { Write-BuildError "MSBuild not found. Install Visual Studio or Build Tools"; $hasErrors = $true }
    
    $dotnetList = dotnet --list-sdks 2>$null
    $hasNet10 = $dotnetList | Where-Object { $_ -match "^10\." }
    if ($hasNet10) {
        $net10Version = ($hasNet10 | Select-Object -First 1) -split ' ' | Select-Object -First 1
        Write-Success ".NET 10 SDK found: $net10Version"
    }
    else {
        Write-BuildError ".NET 10 SDK not found. Install from https://dotnet.microsoft.com/download/dotnet/10.0"
        $hasErrors = $true
    }
    
    $script:WixInstalled = Test-WixInstalled
    if ($script:WixInstalled) { Write-Success "WiX CLI found: v$(wix --version 2>$null)" }
    else { Write-BuildError "WiX CLI v6+ not found. Install: dotnet tool install --global wix"; $hasErrors = $true }
    
    if ($hasErrors) { throw "Prerequisites check failed. Please install missing components." }
    Write-Success "All prerequisites met"
}

function CleanSolution {
    Write-Header "Cleaning Previous Builds"
    
    @("bin", "obj", "build", "dist", "publish") | Where-Object { Test-Path $_ } | ForEach-Object {
        Write-Info "Removing $_..."
        Remove-Item -Path $_ -Recurse -Force -ErrorAction SilentlyContinue
    }
    
    Get-ChildItem -Path . -Directory | ForEach-Object {
        $projName = $_.Name
        $projPath = $_.FullName
        @("bin", "obj") | ForEach-Object {
            $targetDir = Join-Path $projPath $_
            if (Test-Path $targetDir) {
                Write-Info "Cleaning $projName\$_..."
                Remove-Item -Path $targetDir -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
    }
    Write-Success "Clean completed"
}

function Restore-Projects {
    Write-Info "Restoring full solution..."
    $result = dotnet restore Skua.sln --verbosity minimal 2>&1
    if ($LASTEXITCODE -ne 0) { Write-BuildError "Restore failed"; Write-Host $result -ForegroundColor Red; throw "Restore failed" }
}

function Build-SourceGenerators([string]$Config) {
    Write-Info "Building source generators (AnyCPU)..."
    $generatorProj = ".\Skua.Core.Generators\Skua.Core.Generators.csproj"
    $result = & dotnet build $generatorProj --configuration $Config -p:Platform=AnyCPU --verbosity minimal 2>&1
    if ($LASTEXITCODE -ne 0) { Write-BuildError "Generator build failed"; Write-Host $result -ForegroundColor Red; throw "Generator build failed" }
    Write-Success "Source generators built successfully"
}

function Build-Platform([string]$Platform, [string]$Config, [bool]$EnableBinLog = $false) {
    Write-Header "Building $Platform - $Config"
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    
    try {
        Restore-Projects
        Build-SourceGenerators -Config $Config
        
        Write-Info "Building Skua.sln..."
        $buildArgs = @("build", "Skua.sln", "--configuration", $Config, "-p:Platform=$Platform", "--no-restore", "--verbosity", "minimal", "-p:WarningLevel=0", "/p:BuildInParallel=true")
        if ($Platform -eq "x86") { $buildArgs += "-p:PlatformTarget=x86" }
        if ($EnableBinLog) { $buildArgs += "/bl:build-$Platform-$Config.binlog" }
        
        $result = & dotnet $buildArgs 2>&1
        if ($LASTEXITCODE -ne 0) { Write-BuildError "Build failed"; Write-Host $result -ForegroundColor Red; throw "Build failed" }
        
        $stopwatch.Stop()
        Write-Success "Build completed for $Platform in $($stopwatch.Elapsed.TotalSeconds.ToString('F2'))s"
    }
    catch {
        $stopwatch.Stop()
        Write-BuildError "Build failed after $($stopwatch.Elapsed.TotalSeconds.ToString('F2'))s"
        throw
    }
}


function Build-Installer([string]$Platform) {
    if ($SkipInstaller) { Write-Info "Skipping installer build"; return }
    if (-not $script:MSBuildPath) { Write-BuildError "MSBuild not found, skipping installer"; return }
    
    Write-Header "Building WiX Installer for $Platform"
    $installerProject = ".\Skua.Installer\Skua.Installer.wixproj"
    if (-not (Test-Path $installerProject)) { Write-BuildError "Installer project not found"; return }
    
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        Write-Info "Building installer..."
        $result = & $script:MSBuildPath $installerProject "/p:Configuration=$Configuration" "/p:Platform=$Platform" "/t:Rebuild" "/verbosity:minimal" "/nologo" 2>&1
        if ($LASTEXITCODE -ne 0) { Write-BuildError "Installer build failed"; Write-Host $result -ForegroundColor Red; throw "Installer build failed" }
        
        $installers = Get-ChildItem -Path ".\Skua.Installer\bin\$Platform\$Configuration\*.msi" -ErrorAction SilentlyContinue
        if ($installers) {
            $installerDest = Join-Path $OutputPath "Installers"
            if (-not (Test-Path $installerDest)) { New-Item -ItemType Directory -Path $installerDest -Force | Out-Null }
            $installers | ForEach-Object {
                $destName = "Skua_${Configuration}_${Platform}_$($_.Name)"
                Copy-Item -Path $_.FullName -Destination (Join-Path $installerDest $destName) -Force
                Write-Success "Installer created: $destName"
            }
        }
        $stopwatch.Stop()
        Write-Success "Installer build completed in $($stopwatch.Elapsed.TotalSeconds.ToString('F2'))s"
    }
    catch {
        $stopwatch.Stop()
        Write-BuildError "Installer build failed after $($stopwatch.Elapsed.TotalSeconds.ToString('F2'))s"
        Write-Info "Continuing without installer..."
    }
}

function Build-AS3 {
    Write-Header "Building Skua AS3"

    $as3Directory = Join-Path $PSScriptRoot "Skua.AS3"
    $compilerScript = Join-Path $as3Directory "compile-as3.ps1"
    $outputFile = Join-Path $as3Directory "skua\bin\skua.swf"

    $mxmlc = Get-Command mxmlc -ErrorAction SilentlyContinue

    if (-not $mxmlc) {
        $compilerPath = $null

        # First try the standard FLEX_HOME environment variable.
        if (![string]::IsNullOrWhiteSpace($env:FLEX_HOME)) {
            $candidate = Join-Path $env:FLEX_HOME "bin\mxmlc.bat"

            if (Test-Path -LiteralPath $candidate) {
                $compilerPath = $candidate
            }
        }

        # Then look for a Moonshine-installed Flex SDK.
        if (-not $compilerPath) {
            $moonshineRoot = "C:\MoonshineSDKs\Flex_SDK"

            if (Test-Path -LiteralPath $moonshineRoot) {
                $moonshineSdk = Get-ChildItem -LiteralPath $moonshineRoot -Directory |
                    Sort-Object Name -Descending |
                    Where-Object {
                        Test-Path -LiteralPath(
                            Join-Path $_.FullName "bin\mxmlc.bat"
                        )
                    } |
                    Select-Object -First 1

                if ($moonshineSdk) {
                    $env:FLEX_HOME = $moonshineSdk.FullName
                    $compilerPath = Join-Path $env:FLEX_HOME "bin\mxmlc.bat"
                }
            }
        }

        if ($compilerPath) {
            $compilerDirectory = Split-Path $compilerPath -Parent
            $env:PATH = "$compilerDirectory;$env:PATH"
            $mxmlc = Get-Command mxmlc -ErrorAction SilentlyContinue
        }
    }

    if (-not $mxmlc) {
        throw "No ActionScript compiler was found. Install a Flex SDK or configure FLEX_HOME."
    }

    Write-Info "Using ActionScript compiler: $($mxmlc.Source)"

    # Only remove the old output after confirming that a compiler exists.
    if (Test-Path -LiteralPath $outputFile) {
        Remove-Item -LiteralPath $outputFile -Force
    }

    Push-Location $as3Directory

    try {
        $result = & pwsh -NoProfile -ExecutionPolicy Bypass `
            -File $compilerScript 2>&1

        $exitCode = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }

    if ($exitCode -ne 0 -or !(Test-Path -LiteralPath $outputFile)) {
        Write-Host $result -ForegroundColor Red
        throw "AS3 compilation failed. Refusing to package a stale or missing skua.swf."
    }

    $swf = Get-Item -LiteralPath $outputFile
    Write-Success "AS3 compiled successfully: $($swf.Length) bytes"
}

function Show-Summary([TimeSpan]$TotalTime, [bool]$Success) {
    Write-Header "Build Summary"
    if ($Success) { Write-Success "Build completed successfully!" } else { Write-BuildError "Build completed with errors" }
    Write-Info "Configuration: $Configuration | Platforms: $($Platforms -join ', ') | Installer: $(if ($SkipInstaller) { 'No' } else { 'Yes' })"
    Write-Info "Total time: $($TotalTime.TotalSeconds.ToString('F2'))s"
    
    if ($Success -and (Test-Path $OutputPath)) {
        Write-Host "`nOutput: $(Resolve-Path $OutputPath)" -ForegroundColor Yellow
        Get-ChildItem -Path $OutputPath -Recurse -File | Where-Object { $_.Extension -in @('.exe', '.msi') } | ForEach-Object {
            Write-Host "  • $($_.FullName.Replace((Resolve-Path $OutputPath).Path, '').TrimStart('\'))" -ForegroundColor Gray
        }
    }
}

function Wait-ForKeyPress([int]$ExitCode = 0) {
    Write-Host "`n========================================" -ForegroundColor DarkGray
    Write-Host $(if ($ExitCode -eq 0) { "Press any key to exit..." } else { "Build failed. Press any key to exit..." }) -ForegroundColor $(if ($ExitCode -eq 0) { "Green" } else { "Red" })
    Write-Host "========================================" -ForegroundColor DarkGray
    try { $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyUp") } catch { /* ignored */ }
    exit $ExitCode
}

function Main {
    $totalStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $success = $false
    $exitCode = 0
    $ErrorActionPreference = "Stop"
    
    $dotnetPath = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
    if ($dotnetPath) {
        $dotnetDir = Split-Path $dotnetPath -Parent
        $env:PATH = "$dotnetDir;$env:PATH"
    }
    
    try {
        Write-Header "Skua Build Automation"
        Test-Prerequisites
        if (-not $SkipClean) { CleanSolution }
		
		Build-AS3
        
        # Warn if using -Parallel with single platform
        if ($Parallel -and $Platforms.Count -eq 1) {
            Write-Info "Note: -Parallel flag is only beneficial when building multiple platforms."
            Write-Info "Building single platform ($($Platforms[0])) sequentially..."
        }
        
        if ($Parallel -and $Platforms.Count -gt 1) {
            Write-Info "Building platforms in parallel..."
            
            # Restore and build generators once before parallel builds
            Restore-Projects
            Build-SourceGenerators -Config $Configuration
            
            $jobs = @()
            foreach ($platform in $Platforms) {
                $job = Start-Job -ScriptBlock {
                    param($platform, $config, $enableBinLog)
                    
                    $buildArgs = @("build", "Skua.sln", "--configuration", $config, "-p:Platform=$platform", "--no-restore", "--verbosity", "minimal", "-p:WarningLevel=0", "/p:BuildInParallel=true")
                    if ($platform -eq "x86") { $buildArgs += "-p:PlatformTarget=x86" }
                    if ($enableBinLog) { $buildArgs += "/bl:build-$platform-$config.binlog" }
                    
                    $result = & dotnet $buildArgs 2>&1
                    if ($LASTEXITCODE -ne 0) {
                        return @{ Success = $false; Platform = $platform; Output = $result }
                    }
                    return @{ Success = $true; Platform = $platform; Output = $result }
                } -ArgumentList $platform, $Configuration, $BinaryLog
                $jobs += $job
            }
            
            Write-Info "Waiting for parallel builds to complete..."
            $allSuccess = $true
            $jobs | Wait-Job | ForEach-Object {
                $result = Receive-Job -Job $_
                if (-not $result.Success) {
                    Write-BuildError "Build failed for $($result.Platform)"
                    Write-Host $result.Output -ForegroundColor Red
                    $allSuccess = $false
                }
                else {
                    Write-Success "Build completed for $($result.Platform)"
                }
                Remove-Job -Job $_
            }
            
            if (-not $allSuccess) {
                throw "One or more platform builds failed"
            }
            
            # Build installers in parallel
            if (-not $SkipInstaller) {
                Write-Info "Building installers in parallel..."
                $installerJobs = @()
                foreach ($platform in $Platforms) {
                    $job = Start-Job -ScriptBlock {
                        param($msbuildPath, $installerProject, $config, $platform)
                        
                        $result = & $msbuildPath $installerProject "/p:Configuration=$config" "/p:Platform=$platform" "/t:Rebuild" "/verbosity:minimal" "/nologo" 2>&1
                        if ($LASTEXITCODE -ne 0) {
                            return @{ Success = $false; Platform = $platform; Output = $result }
                        }
                        return @{ Success = $true; Platform = $platform; Output = $result }
                    } -ArgumentList $script:MSBuildPath, ".\Skua.Installer\Skua.Installer.wixproj", $Configuration, $platform
                    $installerJobs += $job
                }
                
                Write-Info "Waiting for installer builds to complete..."
                $installerJobs | Wait-Job | ForEach-Object {
                    $result = Receive-Job -Job $_
                    if (-not $result.Success) {
                        Write-BuildError "Installer build failed for $($result.Platform)"
                        Write-Host $result.Output -ForegroundColor Red
                    }
                    else {
                        Write-Success "Installer completed for $($result.Platform)"
                        
                        # Copy installer to Installers folder
                        $installers = Get-ChildItem -Path ".\Skua.Installer\bin\$($result.Platform)\$Configuration\*.msi" -ErrorAction SilentlyContinue
                        if ($installers) {
                            $installerDest = Join-Path $OutputPath "Installers"
                            if (-not (Test-Path $installerDest)) { New-Item -ItemType Directory -Path $installerDest -Force | Out-Null }
                            $installers | ForEach-Object {
                                $destName = "Skua_${Configuration}_$($result.Platform)_$($_.Name)"
                                Copy-Item -Path $_.FullName -Destination (Join-Path $installerDest $destName) -Force
                                Write-Success "Installer created: $destName"
                            }
                        }
                    }
                    Remove-Job -Job $_
                }
            }
        }
        else {
            foreach ($platform in $Platforms) {
                Build-Platform -Platform $platform -Config $Configuration -EnableBinLog $BinaryLog
                if (-not $SkipInstaller) { Build-Installer -Platform $platform }
            }
        }
        $success = $true
    }
    catch {
        Write-BuildError "Build failed: $_"
        $exitCode = 1
    }
    finally {
        $totalStopwatch.Stop()
        Show-Summary -TotalTime $totalStopwatch.Elapsed -Success $success
        Wait-ForKeyPress -ExitCode $exitCode
    }
}

Main