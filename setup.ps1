<#  setup.ps1
    Uso:
      - Modo Docker (API + DB):  .\setup.ps1 -Mode docker
      - Modo Local  (API local + DB no Docker): .\setup.ps1 -Mode local

    Dica se o PowerShell bloquear a execução:
      Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
#>

param(
  [ValidateSet('docker','local')]
  [string]$Mode = 'docker'
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$apiProj = Join-Path $root 'Api\Api.csproj'

function Write-Info($msg){ Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Write-Ok($msg){ Write-Host "[OK]   $msg" -ForegroundColor Green }
function Write-Warn($msg){ Write-Host "[WARN] $msg" -ForegroundColor Yellow }
function Write-Err($msg){ Write-Host "[ERRO] $msg" -ForegroundColor Red }

function Test-Docker {
  try {
    docker info --format '{{.ServerVersion}}' | Out-Null
    return $true
  } catch { return $false }
}

function Wait-Http($url, $timeoutSec=120) {
  $sw = [Diagnostics.Stopwatch]::StartNew()
  while ($sw.Elapsed.TotalSeconds -lt $timeoutSec) {
    try {
      $resp = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 5
      if ($resp.StatusCode -ge 200 -and $resp.StatusCode -lt 500) { return $true }
    } catch { Start-Sleep -Milliseconds 800 }
  }
  return $false
}

function Ensure-EnvFile {
  $envPath = Join-Path $root '.env'
  if (-not (Test-Path $envPath)) {
@"
DB_CONN=Host=db;Port=5432;Database=mlopsdb;Username=postgres;Password=postgres
ASPNETCORE_URLS=http://0.0.0.0:8080
ASPNETCORE_ENVIRONMENT=Development
"@ | Set-Content $envPath -Encoding UTF8
    Write-Ok ".env criado em $envPath"
  } else {
    Write-Info ".env já existe (mantido)"
  }
}

function Ensure-AppSettingsLocal {
  $localPath = Join-Path $root 'Api\appsettings.Local.json'
  if (-not (Test-Path $localPath)) {
@"
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=mlopsdb;Username=postgres;Password=postgres"
  }
}
"@ | Set-Content $localPath -Encoding UTF8
    Write-Ok "Api\appsettings.Local.json criado"
  } else {
    Write-Info "Api\appsettings.Local.json já existe (mantido)"
  }
}

function Start-DockerFull {
  if (-not (Test-Docker)) { throw "Docker não está disponível. Abra o Docker Desktop e tente novamente." }

  Ensure-EnvFile

  Write-Info "Subindo API + DB com docker compose..."
  docker compose up -d --build

  Write-Info "Aguardando API responder..."
  if (Wait-Http 'http://localhost:8080/openapi/v1.json' 150) {
    Write-Ok "API pronta em http://localhost:8080"
    Start-Process 'http://localhost:8080/docs'
  } else {
    Write-Warn "API não respondeu no tempo limite. Veja logs com: docker compose logs -f api"
  }
}

function Start-Local {
  if (-not (Test-Docker)) { throw "Docker não está disponível. Abra o Docker Desktop e tente novamente." }

  Write-Info "Subindo apenas o banco (db) via docker compose..."
  docker compose up -d db

  Ensure-AppSettingsLocal

  Write-Info "Restaurando e compilando solução..."
  dotnet restore "$root\ApiProcessing.sln"
  dotnet build "$root\ApiProcessing.sln" -c Release

  Write-Info "Iniciando API local (dotnet run) na porta 8080..."
  # Abre em nova janela
  Start-Process -FilePath "dotnet" -ArgumentList "run --project `"$apiProj`" --urls http://localhost:8080" -WorkingDirectory $root

  Write-Info "Aguardando API responder..."
  if (Wait-Http 'http://localhost:8080/openapi/v1.json' 150) {
    Write-Ok "API pronta em http://localhost:8080"
    Start-Process 'http://localhost:8080/docs'
  } else {
    Write-Warn "API não respondeu no tempo limite. Verifique o console da janela do dotnet run."
  }
}

try {
  Write-Info "Modo selecionado: $Mode"
  switch ($Mode) {
    'docker' { Start-DockerFull }
    'local'  { Start-Local }
  }
  Write-Ok "Concluído."
} catch {
  Write-Err $_.Exception.Message
  exit 1
}
