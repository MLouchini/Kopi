param([string]$PublishDir='artifacts/app')
$ErrorActionPreference='Stop'
# App-local deployment of the Visual C++ redistributable avoids a separate admin installer.
$crt=Get-ChildItem "${env:ProgramFiles}\Microsoft Visual Studio\*\*\VC\Redist\MSVC\*\x64\Microsoft.VC143.CRT" -Directory | Sort-Object FullName -Descending | Select-Object -First 1
if (!$crt) { throw 'Visual C++ x64 redistributable directory was not found.' }
New-Item -ItemType Directory -Force "$PublishDir/native-runtime" | Out-Null
Copy-Item "$($crt.FullName)/*.dll" "$PublishDir/native-runtime/"
$m=Get-Content model-manifest.json -Raw | ConvertFrom-Json
Invoke-WebRequest $m.runtime.url -OutFile artifacts/native-runtime.zip
$hash=(Get-FileHash artifacts/native-runtime.zip -Algorithm SHA256).Hash.ToLower()
if ($hash -ne $m.runtime.sha256) { throw 'Native runtime checksum mismatch.' }
Expand-Archive artifacts/native-runtime.zip artifacts/native-smoke -Force
Copy-Item "$($crt.FullName)/*.dll" artifacts/native-smoke/
& "$PWD/artifacts/native-smoke/llama-server.exe" --version
if ($LASTEXITCODE -ne 0) { throw 'Native inference executable did not start.' }
