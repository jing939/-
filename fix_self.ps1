foreach ($file in Get-ChildItem -Path "Assets\Battle\*.controller") {
    $lines = Get-Content $file.FullName -Raw
    $lines = $lines -replace 'm_CanTransitionToSelf: 1', 'm_CanTransitionToSelf: 0'
    Set-Content $file.FullName -Value $lines
}
