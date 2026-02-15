SHELL := powershell.exe
.SHELLFLAGS := -NoProfile -ExecutionPolicy Bypass -Command

MANAGER_PROJECT := Skua.Manager.Avalonia/Skua.Manager.Avalonia.csproj
CORE_PROJECT := Skua.Core/Skua.Core.csproj

.PHONY: help core-build manager-lock-clean manager-build manager-rebuild manager-run manager-run-safe

help:
	Write-Host "Targets:"
	Write-Host "  make core-build        - Build Skua.Core"
	Write-Host "  make manager-lock-clean- Kill common lock holders and clean manager obj/Debug"
	Write-Host "  make manager-build     - Build Skua.Manager.Avalonia"
	Write-Host "  make manager-rebuild   - Clean lock + build manager"
	Write-Host "  make manager-run       - Run Skua.Manager.Avalonia"
	Write-Host "  make manager-run-safe  - Rebuild then run without rebuild"

core-build:
	dotnet build $(CORE_PROJECT) -nologo -m:1

manager-lock-clean:
	Get-Process dotnet,VBCSCompiler,MSBuild,Skua.Manager.Avalonia -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue; Start-Sleep -Milliseconds 400; Remove-Item -Recurse -Force Skua.Manager.Avalonia/obj/Debug -ErrorAction SilentlyContinue

manager-build:
	dotnet build $(MANAGER_PROJECT) -nologo -m:1

manager-rebuild: manager-lock-clean manager-build

manager-run:
	dotnet run --project Skua.Manager.Avalonia -m:1

manager-run-safe: manager-rebuild
	dotnet run --project Skua.Manager.Avalonia -m:1 --no-build
