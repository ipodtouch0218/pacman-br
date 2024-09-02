param (
    [Parameter(Mandatory)][String] $UnityEditorPath,
    [String] $QuantumUnityPath = "quantum_unity",
    [String] $QuantumCodePath = "quantum_code/quantum.code",
    [String] $AssetDBPath = "Quantum3MigrationAssets",
    [String] $Quantum3PackagePath = "Photon-Quantum-3.0.0-Preview-0.unitypackage",
    [String] $Quantum3MigrationPackagePath = "Photon-Quantum-3.0.0-Preview-Migration-0.unitypackage",
    [String] $Quantum2MigrationPreparationPackagePath = "Quantum3MigrationPreparation.unitypackage",
    [String] $Quantum3BotSdkPath = "",
    [String] $PreCompileErrorDetectionGitPatchPath = "",
    [switch] $SkipQuantum2Preparation,
    [switch] $SkipQuantum3PackageImports,
    [switch] $SkipQuantumCodeCopy,
    [switch] $SkipInitialCodeGen,
    [switch] $SkipCompileErrorDetection,
    [switch] $SkipAssetsUpgrade,
    [string] $AssemblyDefinitionsDecision,
    [switch] $PauseAfterEachStep,
    [string] $LogBasePath
)


$global:StepCount = 14.0
$global:StepsToPercent = 100.0 / $global:StepCount
$global:Step = 0
$global:LastStep = ""
$global:LastLogPath = ""

function ConditionalPause {
    if ($PauseAfterEachStep) {
        Read-Host "[$global:Step/$global:StepCount] $global:LastStep done, press enter to continue"
    } else {
        Write-Output "[$global:Step/$global:StepCount] $global:LastStep done"
    }
}

function RunUnity {
    param (
        [String] $StepName,
        [String] $UnityArgs,
        [switch] $InteractiveMode,
        [switch] $DontTerminateOnError,
        [switch] $NoPause
    )

    $global:Step++
    Write-Progress -Activity "[$global:Step/$global:StepCount]" -PercentComplete ($global:Step * $global:StepsToPercent) -Status $StepName

    $global:LastStep = $StepName
    $StepNameSafe = $StepName.Replace(" ", "_")
    $LogPath = "Quantum3MigrationLog_{0:d2}_$StepNameSafe.log" -f $global:Step
    if ($LogBasePath) {
      $LogPath = "$LogBasePath/$LogPath"
    }
    $global:LastLogPath = $LogPath

    $safeUnityPath = $QuantumUnityPath.trimEnd('\')

    if ($InteractiveMode) {
        $CommonUnityArgs = "-logFile `"$LogPath`" -projectpath `"$safeUnityPath`""
    } else {
        $CommonUnityArgs = "-quit -batchmode -logFile `"$LogPath`" -projectpath `"$safeUnityPath`""        
    }
    
    $p = (Start-Process "$UnityEditorPath" -PassThru -Wait -ArgumentList $UnityArgs,$CommonUnityArgs)

    if (-Not $DontTerminateOnError) {
        if ($p.ExitCode -ne 0) {
            throw "Last call failed, check $LogPath for details."
        }
    }


    if (-not $NoPause) {
        ConditionalPause
    }

    if ($DontTerminateOnError) {
        $p.ExitCode    
    }
}

function ResolvePath($path) {
    # this is insane, but regular Resolve-Path fails on non-existant paths
    $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($path).trimEnd('\')
}


$FullDBPath = ResolvePath $AssetDBPath


if (-Not $SkipQuantum2Preparation) {
    $FullQuantum2MigrationPreparationPackagePath = ResolvePath $Quantum2MigrationPreparationPackagePath
    RunUnity "Importing Quantum 2 Migration Preparation" "-importPackage `"$FullQuantum2MigrationPreparationPackagePath`""
    RunUnity "Adding defines"                     "-executeMethod QuantumMigrationPreparation.AddMigrationDefines"
    RunUnity "Exporting Quantum 2 Assets"         "-executeMethod QuantumMigrationPreparation.ExportAssets -quantumAssetExportPath `"$FullDBPath`""
    RunUnity "Deleting Photon"                    "-executeMethod QuantumMigrationPreparation.DeletePhoton"
} else {
    $global:Step += 4;
}



if (-Not $SkipQuantum3PackageImports) {   
    $FullQuantum3PackagePath = ResolvePath $Quantum3PackagePath
    $FullQuantum3MigrationPackagePath = ResolvePath $Quantum3MigrationPackagePath
    RunUnity "Importing Quantum 3 Packages - SDK"            "-ignorecompilererrors -importPackage `"$FullQuantum3PackagePath`""
    $global:Step -= 1;
    RunUnity "Importing Quantum 3 Packages - Migration"  "-ignorecompilererrors -importPackage `"$FullQuantum3MigrationPackagePath`""
    $global:Step -= 1;
    if ($Quantum3BotSdkPath) {
        $FullQuantum3BotSdkPath = ResolvePath $Quantum3BotSdkPath
        RunUnity "Importing Quantum 3 Packages - BotSDK" "-ignorecompilererrors -importPackage `"$FullQuantum3BotSdkPath`""
    } else {
        $global:Step += 1;
    }
} else {
    $global:Step += 1;
}

if (-Not $SkipQuantumCodeCopy) {
    $FullCodePath = ResolvePath $QuantumCodePath
    RunUnity "Copying quantum_code" "-ignorecompilererrors -executeMethod QuantumMigration.ImportSimulationProject -quantumCodeProjectFolder `"$FullCodePath`""
} else {
    $global:Step += 1;
}

if (-Not $SkipInitialCodeGen) {
    RunUnity "Initial CodeGen"      "-ignorecompilererrors -executeMethod QuantumMigration.RunInitialCodeGen"
} else {
    $global:Step += 1;
}

$NoValues = @("no", "n", "No", "N")
$YesValues = @("yes", "y", "Yes", "Y")
$answer = $AssemblyDefinitionsDecision

while (-not ($NoValues -contains $answer -or $YesValues -contains $answer)) {
    $answer = Read-Host "Do you want to delete Quantum Assembly Definitions (Yes/No)? Select Yes only if your Quantum 2 project did not use Assembly Definitions for Quantum code. [No]"
    if (-not $answer) {
        $answer = "no"
    }
}

if ($YesValues -contains $answer) {
    RunUnity "Deleting Assembly Definitions" "-ignorecompilererrors -executeMethod QuantumMigration.DeleteAssemblyDefinitions"
} else {
    $global:Step += 1;
}

if (-Not $SkipCompileErrorDetection) {

    if ($PreCompileErrorDetectionGitPatchPath) {
        $FullPreCompileErrorDetectionGitPatchPath = ResolvePath $PreCompileErrorDetectionGitPatchPath
        # apply git patch
        Write-Output "Applying Post Initial CodeGen Code Patch: $FullPreCompileErrorDetectionGitPatchPath"
        git apply $FullPreCompileErrorDetectionGitPatchPath
    }

    # first run has to be ignored, due to false-positive errors

    while (($err = RunUnity "Waiting for user to fix all errors" "-ignorecompilererrors -executeMethod QuantumMigration.CheckAssetObjectScriptsBeingReady" -DontTerminateOnError -NoPause) -ne 0) {
        $global:Step -= 1;
        
        if ($err -eq 2) {
            Write-Output "Not all non-generic, non-abstract AssetObject are in their own .cs files:"
            $errors = Select-String "\[Quantum Migration\] (Failed .*)" -Path "$global:LastLogPath"
            foreach ($error in $errors) {
                Write-Output $error.Matches[0].Groups[1].Value
            }
        } else {
            Write-Output "There are compile errors (ExitCode: $err). Waiting for user to fix all errors."
        }

        $dummy = RunUnity "Waiting for user to fix all errors interactive" "-dummy" -InteractiveMode -DontTerminateOnError -NoPause
        $global:Step -= 1;
    }

    ConditionalPause
} else {
     $global:Step += 1;
}


if (-Not $SkipAssetsUpgrade) {
    RunUnity "Moving AssetBase script GUIDs to AssetObject scripts" "-executeMethod QuantumMigration.TransferAssetBaseGuidsToAssetObjects"
    RunUnity "Restoring AssetObject data" "-executeMethod QuantumMigration.RestoreAssetObjectData -quantumAssetExportPath `"$FullDBPath`""
    RunUnity "Enabling AssetObject Postprocessor" "-executeMethod QuantumMigration.EnableAssetObjectPostprocessor"
    RunUnity "Reimporting All AssetObjects" "-executeMethod QuantumMigration.ReimportAllAssetObjects"
    RunUnity "Installing Quantum user files" "-executeMethod Quantum.Editor.QuantumEditorHubWindow.InstallAllUserFiles"
} else {
    $global:Step += 5;   
}

#if (-Not $SkipCodeUpgrade) {
#    RunUnity "Enabling Code Upgrade" "-executeMethod QuantumMigration.EnableCodeUpgrade"
#    RunUnity "Running Upgrade Code Gen" "-accept-apiupdate -executeMethod QuantumMigration.RunUpgradeCodeGen"
#    RunUnity "Disable Code Upgrade" "-accept-apiupdate -ignorecompilererrors -executeMethod QuantumMigration.DisableCodeUpgrade"
#} else {
#    $global:Step += 3;   
#}

Write-Output "Done!"