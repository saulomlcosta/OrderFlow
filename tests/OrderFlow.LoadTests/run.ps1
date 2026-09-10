[CmdletBinding()]
param(
    [ValidateRange(1, 1000)]
    [int]$VirtualUsers = 10,

    [ValidateRange(1, 100000)]
    [int]$Iterations = 100,

    [ValidatePattern('^\d+(ms|s|m|h)$')]
    [string]$MaxDuration = '2m'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$composeFile = Join-Path $repositoryRoot 'compose.load.yaml'
$apiProject = Join-Path $repositoryRoot 'src\OrderFlow.Api\OrderFlow.Api.csproj'
$runId = [Guid]::NewGuid().ToString('N')
$apiStandardOutput = Join-Path $env:TEMP "orderflow-load-api-$runId.out.log"
$apiStandardError = Join-Path $env:TEMP "orderflow-load-api-$runId.err.log"
$apiProcess = $null
$succeeded = $false

$environmentNames = @(
    'ASPNETCORE_ENVIRONMENT',
    'ASPNETCORE_URLS',
    'ConnectionStrings__OrderFlow',
    'Persistence__InitializeOnStartup',
    'Persistence__Provider'
)

$previousEnvironment = @{}
foreach ($name in $environmentNames) {
    $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
}

try {
    & docker compose -f $composeFile up -d --wait
    if ($LASTEXITCODE -ne 0) {
        throw 'The isolated PostgreSQL load-test container did not start.'
    }

    [Environment]::SetEnvironmentVariable('ASPNETCORE_ENVIRONMENT', 'Development', 'Process')
    [Environment]::SetEnvironmentVariable('ASPNETCORE_URLS', 'http://localhost:5217', 'Process')
    [Environment]::SetEnvironmentVariable(
        'ConnectionStrings__OrderFlow',
        'Host=localhost;Port=5433;Database=orderflow_load;Username=orderflow;Password=orderflow-load',
        'Process')
    [Environment]::SetEnvironmentVariable('Persistence__InitializeOnStartup', 'true', 'Process')
    [Environment]::SetEnvironmentVariable('Persistence__Provider', 'PostgreSql', 'Process')

    $dotnet = (Get-Command dotnet).Source
    $startProcessParameters = @{
        FilePath = $dotnet
        ArgumentList = @(
            'run',
            '--project', $apiProject,
            '--configuration', 'Release',
            '--no-launch-profile',
            '--no-restore'
        )
        WorkingDirectory = $repositoryRoot
        RedirectStandardOutput = $apiStandardOutput
        RedirectStandardError = $apiStandardError
        PassThru = $true
    }

    if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) {
        $startProcessParameters.WindowStyle = 'Hidden'
    }

    $apiProcess = Start-Process @startProcessParameters

    $ready = $false
    for ($attempt = 1; $attempt -le 60; $attempt++) {
        if ($apiProcess.HasExited) {
            break
        }

        try {
            $response = Invoke-WebRequest `
                -UseBasicParsing `
                -Uri 'http://localhost:5217/health/ready' `
                -TimeoutSec 2

            if ($response.StatusCode -eq 200) {
                $ready = $true
                break
            }
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }

    if (!$ready) {
        if (Test-Path $apiStandardOutput) {
            Get-Content $apiStandardOutput | Write-Host
        }
        if (Test-Path $apiStandardError) {
            Get-Content $apiStandardError | Write-Host
        }

        throw 'The isolated OrderFlow API did not become ready.'
    }

    $scriptMount = "$($PSScriptRoot):/scripts:ro"
    & docker run `
        --rm `
        --add-host 'host.docker.internal:host-gateway' `
        --env 'BASE_URL=http://host.docker.internal:5217' `
        --env "VUS=$VirtualUsers" `
        --env "ITERATIONS=$Iterations" `
        --env "MAX_DURATION=$MaxDuration" `
        --volume $scriptMount `
        'grafana/k6:2.2.0' `
        run '/scripts/checkout-baseline.js'

    if ($LASTEXITCODE -ne 0) {
        throw 'The k6 baseline failed its correctness thresholds.'
    }

    $succeeded = $true
}
finally {
    if ($null -ne $apiProcess -and !$apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force
        $apiProcess.WaitForExit()
    }

    foreach ($name in $environmentNames) {
        [Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name], 'Process')
    }

    & docker compose -f $composeFile down --remove-orphans

    if ($succeeded) {
        Remove-Item $apiStandardOutput, $apiStandardError -Force -ErrorAction SilentlyContinue
    }
    else {
        Write-Host "API logs were retained at $apiStandardOutput and $apiStandardError"
    }
}
