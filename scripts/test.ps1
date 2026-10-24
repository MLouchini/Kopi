$ErrorActionPreference='Stop'
dotnet test tests/Kopi.Tests --no-build -c Release --logger 'trx;LogFileName=windows.trx'
$testExit=$LASTEXITCODE
Get-ChildItem tests -Recurse -Filter windows.trx | ForEach-Object {
  [xml]$trx=Get-Content $_.FullName
  foreach ($result in $trx.TestRun.Results.UnitTestResult) {
    if ($result.outcome -eq 'Failed') {
      $message=([string]$result.Output.ErrorInfo.Message).Replace('%','%25').Replace("`r",'%0D').Replace("`n",'%0A')
      Write-Output "::error title=$($result.testName)::$message"
    }
  }
}
exit $testExit
