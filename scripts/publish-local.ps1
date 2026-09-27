<#
.SYNOPSIS
  Publishes the API and builds the Angular app locally, ready to scp to the server.
  See DEPLOYMENT.md for the full AWS EC2 deployment walkthrough this supports.

.EXAMPLE
  ./scripts/publish-local.ps1
  # then:
  # scp -i $key -r ./publish/api/*                                             ubuntu@<ip>:/var/www/academy-api/
  # scp -i $key -r ./client/developer-academy/dist/developer-academy/browser/* ubuntu@<ip>:/var/www/academy-web/
#>

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

Write-Host "Publishing API (Release)..." -ForegroundColor Cyan
dotnet publish "$repoRoot/src/DeveloperEngineeringAcademy.Web/DeveloperEngineeringAcademy.Web.csproj" `
    -c Release -o "$repoRoot/publish/api"

Write-Host "Building Angular app (production)..." -ForegroundColor Cyan
Push-Location "$repoRoot/client/developer-academy"
try {
    npm run build
} finally {
    Pop-Location
}

Write-Host ""
Write-Host "Done. Output ready at:" -ForegroundColor Green
Write-Host "  API:     $repoRoot/publish/api"
Write-Host "  Angular: $repoRoot/client/developer-academy/dist/developer-academy/browser"
Write-Host "  Content: $repoRoot/content  (copy as-is; it's read from disk at runtime)"
