foreach ($file in Get-ChildItem -Path "Assets\Battle\*.anim" -Recurse) {
    if ($file.Name -match "Run|Idle") {
        # Keep Loop Time ON for Run and Idle
        $lines = Get-Content $file.FullName -Raw
        $lines = $lines -replace 'm_LoopTime: 0', 'm_LoopTime: 1'
        Set-Content $file.FullName -Value $lines
    } else {
        # Turn Loop Time OFF for all other animations (Attack, Hit, Guard, etc.)
        $lines = Get-Content $file.FullName -Raw
        $lines = $lines -replace 'm_LoopTime: 1', 'm_LoopTime: 0'
        Set-Content $file.FullName -Value $lines
    }
}
