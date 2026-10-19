$resultsDir = Join-Path $env:TEMP ("mtp-rev51-" + [guid]::NewGuid().ToString())
New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null
dotnet test HPKata/HPKata.slnx -c Release --no-build --disable-build-servers -m:1 --results-directory $resultsDir
