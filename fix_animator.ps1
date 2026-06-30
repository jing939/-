foreach ($file in Get-ChildItem -Path "Assets\Battle\*.controller") {
    $lines = Get-Content $file.FullName
    $outLines = @()
    $inTransition = $false
    $isUnconditional = $false
    
    foreach ($line in $lines) {
        if ($line -match 'AnimatorStateTransition:') {
            $inTransition = $true
            $isUnconditional = $false
        }
        elseif ($line -match '--- !u!') {
            $inTransition = $false
        }
        
        if ($inTransition) {
            if ($line -match 'm_Conditions:\s*\[\]') {
                $isUnconditional = $true
            }
            elseif ($line -match 'm_Conditions:') {
                $isUnconditional = $false
            }
            
            if ($line -match 'm_HasExitTime:') {
                if ($isUnconditional) {
                    $line = $line -replace 'm_HasExitTime: \d', 'm_HasExitTime: 1'
                } else {
                    $line = $line -replace 'm_HasExitTime: \d', 'm_HasExitTime: 0'
                }
            }
            if ($line -match 'm_CanTransitionToSelf:') {
                if ($isUnconditional) {
                    $line = $line -replace 'm_CanTransitionToSelf: \d', 'm_CanTransitionToSelf: 0'
                } else {
                    $line = $line -replace 'm_CanTransitionToSelf: \d', 'm_CanTransitionToSelf: 1'
                }
            }
        }
        $outLines += $line
    }
    Set-Content $file.FullName -Value ($outLines -join "
")
}
