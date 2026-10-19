$guid = [System.Guid]::NewGuid().ToString()
$resDir = Join-Path $env:TEMP "mtp-$guid"
Write-Host "Running dotnet test with results directory: $resDir"
dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory $resDir
$exitCode = $LASTEXITCODE
Write-Host "Exit Code: $exitCode"
exit $exitCode
