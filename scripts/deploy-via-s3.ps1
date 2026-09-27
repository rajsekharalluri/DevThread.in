<#
.SYNOPSIS
  Builds the API and Angular app locally and uploads them to S3, for servers where SSH/SCP
  is blocked by the local network (see DEPLOYMENT.md, "Path B"). Uses the AWS CLI, so run
  `aws configure` yourself first if you haven't already.

.PARAMETER BucketName
  The S3 bucket used for file transfer. Defaults to this deployment's bucket.

.PARAMETER Region
  The AWS region the bucket lives in. Defaults to this deployment's region.

.EXAMPLE
  ./scripts/deploy-via-s3.ps1
  # Then, in an EC2 Instance Connect browser terminal on the server:
  #   cd /tmp
  #   aws s3 cp s3://academy-deploy-906969953319/api.zip . --region ap-south-2 && unzip -oq api.zip -d /var/www/academy-api
  #   aws s3 cp s3://academy-deploy-906969953319/web.zip . --region ap-south-2 && unzip -oq web.zip -d /var/www/academy-web
  #   sudo systemctl restart academy-api
#>

param(
    [string]$BucketName = "academy-deploy-906969953319",
    [string]$Region = "ap-south-2",
    [string]$PublicBaseUrl = "https://devthread.in"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

Write-Host "Publishing API (Release)..." -ForegroundColor Cyan
dotnet publish "$repoRoot/src/DeveloperEngineeringAcademy.Web/DeveloperEngineeringAcademy.Web.csproj" `
    -c Release -o "$repoRoot/publish/api"

Write-Host "Building Angular app (production)..." -ForegroundColor Cyan
Push-Location "$repoRoot/client/developer-academy"
try { npm run build } finally { Pop-Location }

Write-Host "Zipping build output with forward-slash paths..." -ForegroundColor Cyan
# Do not use Compress-Archive here: it writes Windows backslash paths, which causes unzip warnings
# and awkward content paths on Linux. Windows tar/bsdtar creates portable ZIP paths instead.
# Remove old archives first: recreating over an existing ZIP can leave duplicate stale entries.
Remove-Item -Force "$repoRoot/publish/api.zip", "$repoRoot/publish/web.zip", "$repoRoot/publish/content.zip" -ErrorAction SilentlyContinue
tar -a -c -f "$repoRoot/publish/api.zip" -C "$repoRoot/publish/api" .
tar -a -c -f "$repoRoot/publish/web.zip" -C "$repoRoot/client/developer-academy/dist/developer-academy/browser" .
tar -a -c -f "$repoRoot/publish/content.zip" -C "$repoRoot/content" .

Write-Host "Uploading API, Angular UI, and content to s3://$BucketName ..." -ForegroundColor Cyan
aws s3 cp "$repoRoot/publish/api.zip" "s3://$BucketName/api.zip" --region $Region
aws s3 cp "$repoRoot/publish/web.zip" "s3://$BucketName/web.zip" --region $Region
aws s3 cp "$repoRoot/publish/content.zip" "s3://$BucketName/content.zip" --region $Region

Write-Host ""
Write-Host "Upload complete. Open an EC2 Instance Connect browser terminal and paste this deployment block:" -ForegroundColor Green
Write-Host "  set -e"
Write-Host "  cd /tmp"
Write-Host "  aws s3 cp s3://$BucketName/api.zip . --region $Region"
Write-Host "  aws s3 cp s3://$BucketName/web.zip . --region $Region"
Write-Host "  aws s3 cp s3://$BucketName/content.zip . --region $Region"
Write-Host "  sudo mkdir -p /var/www/academy-api /var/www/academy-web /var/www/academy-content"
Write-Host "  unzip -oq api.zip -d /var/www/academy-api"
Write-Host "  unzip -oq web.zip -d /var/www/academy-web"
Write-Host "  unzip -oq content.zip -d /var/www/academy-content"
Write-Host "  sudo chown -R ubuntu:ubuntu /var/www/academy-api /var/www/academy-web /var/www/academy-content"
Write-Host "  sudo systemctl restart academy-api"
Write-Host "  echo 'Waiting for API to become ready...'"
Write-Host '  for i in {1..30}; do if curl -fsS --max-time 3 http://127.0.0.1:5000/api/topics -o /tmp/academy-topics.json; then break; fi; sleep 2; done'
Write-Host '  if [ ! -s /tmp/academy-topics.json ]; then echo "API did not become ready"; sudo systemctl status academy-api --no-pager; sudo journalctl -u academy-api -n 40 --no-pager; exit 1; fi'
Write-Host "  sudo nginx -t && sudo systemctl reload nginx"
Write-Host "  curl -fsS --retry 10 --retry-all-errors --retry-delay 2 $PublicBaseUrl/api/topics -o /tmp/academy-live-topics.json"
Write-Host '  python3 -c "import json; data=json.load(open(\"/tmp/academy-live-topics.json\")); print(\"Topics:\", len(data))"'
Write-Host "  curl -fsS --retry 10 --retry-all-errors --retry-delay 2 $PublicBaseUrl/api/topics/csharp/fundamentals | head -c 200"
Write-Host ""
Write-Host "The script intentionally does not execute remote commands: this environment's network blocks outbound SSH. Use EC2 Instance Connect in the AWS Console for the final block."
