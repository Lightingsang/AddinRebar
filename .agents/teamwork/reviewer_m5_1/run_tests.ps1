$resultsDir = Join-Path $env:TEMP ("mtp-rev51-" + [guid]::NewGuid().ToString())
New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null
Write-Host "Results directory: $resultsDir"

Write-Host "--- Running HPKata.AutoCad.Tests ---"
dotnet test HPKata/HPKata.AutoCad.Tests/HPKata.AutoCad.Tests.csproj -c Release --no-build --disable-build-servers --results-directory $resultsDir

Write-Host "--- Running full HPKata.slnx ---"
dotnet test HPKata/HPKata.slnx -c Release --no-build --disable-build-servers --results-directory $resultsDir
