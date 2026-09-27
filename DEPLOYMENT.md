# Deploying to AWS (single EC2 instance, IP-only)

This guide deploys the whole app — the ASP.NET Core API and the Angular SPA — to a single AWS EC2 instance, accessed by its public IP address. Nginx serves the Angular build as static files and reverse-proxies `/api` to the .NET app running as a `systemd` service.

This is the simplest possible production topology for this app: no load balancer, no domain, no database, no container registry — matching the project's "don't over-engineer" principle. You don't need to own a domain to get real, trusted HTTPS either — see [HTTPS without owning a domain](#https-without-owning-a-domain-this-deployments-actual-approach), which this deployment actually uses via a free `sslip.io` hostname.

```text
Browser
   │  https://<sslip.io hostname>/        → Nginx (443, TLS) → serves Angular static files
   │  https://<sslip.io hostname>/api/... → Nginx (443, TLS) → reverse proxy → Kestrel (127.0.0.1:5000) → API
   │  http://... (any host)               → Nginx (80) → 301 redirect to https://<sslip.io hostname>/...
   ▼
EC2 instance (Ubuntu 24.04)
   ├── /var/www/academy-web       (Angular production build)
   ├── /var/www/academy-api       (published .NET app)
   ├── /var/www/academy-content   (the /content Markdown folder)
   ├── /etc/letsencrypt/          (Certbot-managed TLS certificate, auto-renewing)
   └── Nginx (80 + 443) + systemd service (academy-api, port 5000 on localhost only)
```

## Two ways to get files/commands onto the server

There are two independent problems to solve: (1) provisioning the AWS resources, and (2) getting build output and commands onto the server. This repo's actual deployment used:

- **Provisioning**: AWS CLI, from a local PowerShell terminal (Path A below).
- **File transfer + remote commands**: some networks (corporate firewalls, some ISPs) intercept or reset outbound SSH/SCP (port 22) — ours did (confirmed by a Palo Alto Networks firewall banner appearing even when SSHing to GitHub, a completely unrelated server). If `ssh`/`scp` to your instance hangs or resets, skip straight to **Path B**: upload build output to S3, then run commands via the browser-based **EC2 Instance Connect** terminal in the AWS Console. That terminal's SSH connection is made by AWS's own infrastructure, not your local machine, so it isn't affected by a local network blocking port 22.

Try `ssh -v -o ConnectTimeout=10 ubuntu@<ELASTIC_IP> echo ok` first (after Step 3 below) — if it hangs, resets, or shows a banner that isn't OpenSSH's real one, use Path B.

## Prerequisites

- An AWS account, with an IAM user that has (at minimum) `AmazonEC2FullAccess`. Path B additionally needs `AmazonS3FullAccess` and `IAMFullAccess` on that user (to create the S3 bucket and the EC2 instance role).
- AWS CLI installed. If you don't have admin rights to run the official MSI installer, `pip install --user awscli` works without admin rights.
- Run `aws configure` **yourself**, directly in your own terminal — never paste an AWS secret key into a chat/agent, since it's the one credential AWS shows you only once.

---

## Step 1 — Create a key pair

```powershell
aws ec2 create-key-pair --key-name academy-key --query "KeyMaterial" --output text --region <YOUR_REGION> | Out-File -FilePath "$HOME\.ssh\academy-key.pem" -Encoding ascii
icacls "$HOME\.ssh\academy-key.pem" /inheritance:r
icacls "$HOME\.ssh\academy-key.pem" /grant:r "$($env:USERNAME):(R)"
```

(Console equivalent: EC2 → **Key Pairs** → **Create key pair**, type RSA, format `.pem`.)

## Step 2 — Security group

```powershell
$vpcId = aws ec2 describe-vpcs --filters "Name=isDefault,Values=true" --query "Vpcs[0].VpcId" --output text --region <YOUR_REGION>
$sgId = aws ec2 create-security-group --group-name academy-sg --description "Academy server" --vpc-id $vpcId --query "GroupId" --output text --region <YOUR_REGION>

$myIp = (Invoke-WebRequest -Uri "https://checkip.amazonaws.com" -UseBasicParsing).Content.Trim()
aws ec2 authorize-security-group-ingress --group-id $sgId --protocol tcp --port 22 --cidr "$myIp/32" --region <YOUR_REGION>
aws ec2 authorize-security-group-ingress --group-id $sgId --protocol tcp --port 80 --cidr "0.0.0.0/0" --region <YOUR_REGION>
aws ec2 authorize-security-group-ingress --group-id $sgId --protocol tcp --port 443 --cidr "0.0.0.0/0" --region <YOUR_REGION>
```

> **If you end up on Path B** (browser-based EC2 Instance Connect), also open SSH broadly, since that connection comes from AWS's own IPs, not your laptop's:
> ```powershell
> aws ec2 authorize-security-group-ingress --group-id $sgId --protocol tcp --port 22 --cidr "0.0.0.0/0" --region <YOUR_REGION>
> ```
> This is lower-risk than it sounds — reaching port 22 still requires a valid, AWS-issued temporary key plus your AWS console login; nothing works without both. If you want to tighten this later, restrict it to [AWS's published EC2 Instance Connect IP ranges](https://ip-ranges.amazonaws.com/ip-ranges.json) for your region instead of `0.0.0.0/0`.

## Step 3 — Launch the instance and attach an Elastic IP

```powershell
$amiId = aws ec2 describe-images --owners 099720109477 `
  --filters "Name=name,Values=ubuntu/images/hvm-ssd-gp3/ubuntu-noble-24.04-amd64-server-*" "Name=state,Values=available" `
  --query "sort_by(Images, &CreationDate)[-1].ImageId" --output text --region <YOUR_REGION>

$instanceId = aws ec2 run-instances --image-id $amiId --instance-type t3.small `
  --key-name academy-key --security-group-ids $sgId `
  --tag-specifications 'ResourceType=instance,Tags=[{Key=Name,Value=academy-server}]' `
  --query "Instances[0].InstanceId" --output text --region <YOUR_REGION>

aws ec2 wait instance-running --instance-ids $instanceId --region <YOUR_REGION>

$allocation = aws ec2 allocate-address --domain vpc --region <YOUR_REGION> --output json | ConvertFrom-Json
aws ec2 associate-address --instance-id $instanceId --allocation-id $allocation.AllocationId --region <YOUR_REGION>
"Elastic IP: $($allocation.PublicIp)"
```

Wait ~30–60 seconds after this before connecting — a fresh instance's SSH daemon and network association need a moment to settle.

---

## Path A — SSH/SCP works on your network

### A4 — Install server dependencies over SSH

```powershell
ssh -i "$HOME\.ssh\academy-key.pem" ubuntu@<ELASTIC_IP>
```
```bash
sudo apt-get update
sudo apt-get install -y nginx unzip curl
curl -sSL https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -o /tmp/packages-microsoft-prod.deb
sudo dpkg -i /tmp/packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y aspnetcore-runtime-10.0
dotnet --list-runtimes   # confirm Microsoft.AspNetCore.App 10.x is listed

sudo mkdir -p /var/www/academy-api /var/www/academy-web /var/www/academy-content
sudo chown -R ubuntu:ubuntu /var/www/academy-api /var/www/academy-web /var/www/academy-content
```

### A5 — Build locally and copy over

```powershell
dotnet publish src/DeveloperEngineeringAcademy.Web/DeveloperEngineeringAcademy.Web.csproj -c Release -o ./publish/api
cd client/developer-academy; npm run build; cd ../..

$ip = "<ELASTIC_IP>"; $key = "$HOME\.ssh\academy-key.pem"
scp -i $key -r ./publish/api/*                                             ubuntu@${ip}:/var/www/academy-api/
scp -i $key -r ./client/developer-academy/dist/developer-academy/browser/* ubuntu@${ip}:/var/www/academy-web/
scp -i $key -r ./content/*                                                 ubuntu@${ip}:/var/www/academy-content/
```

Then continue with [Step 5 — systemd + Nginx](#step-5--systemd-service--nginx-both-paths) below.

---

## Path B — SSH/SCP is blocked on your network (this repo's actual deployment used this)

### B4 — Give the server an IAM role so it can pull files from S3

```powershell
'{"Version":"2012-10-17","Statement":[{"Effect":"Allow","Principal":{"Service":"ec2.amazonaws.com"},"Action":"sts:AssumeRole"}]}' | Out-File -FilePath "$env:TEMP\trust-policy.json" -Encoding ascii -NoNewline

aws iam create-role --role-name academy-ec2-role --assume-role-policy-document "file://$env:TEMP/trust-policy.json"
aws iam attach-role-policy --role-name academy-ec2-role --policy-arn arn:aws:iam::aws:policy/AmazonS3ReadOnlyAccess
aws iam create-instance-profile --instance-profile-name academy-ec2-profile
aws iam add-role-to-instance-profile --instance-profile-name academy-ec2-profile --role-name academy-ec2-role
Start-Sleep -Seconds 10
aws ec2 associate-iam-instance-profile --instance-id $instanceId --iam-instance-profile Name=academy-ec2-profile --region <YOUR_REGION>
```

### B5 — Build locally and upload to S3

```powershell
$bucketName = "academy-deploy-<YOUR_ACCOUNT_ID>"   # must be globally unique
aws s3api create-bucket --bucket $bucketName --region <YOUR_REGION> --create-bucket-configuration LocationConstraint=<YOUR_REGION>
aws s3api put-public-access-block --bucket $bucketName --public-access-block-configuration BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true

dotnet publish src/DeveloperEngineeringAcademy.Web/DeveloperEngineeringAcademy.Web.csproj -c Release -o ./publish/api
cd client/developer-academy; npm run build; cd ../..

Compress-Archive -Path "./publish/api/*" -DestinationPath "./publish/api.zip" -Force
Compress-Archive -Path "./client/developer-academy/dist/developer-academy/browser/*" -DestinationPath "./publish/web.zip" -Force
Compress-Archive -Path "./content/*" -DestinationPath "./publish/content.zip" -Force

aws s3 cp ./publish/api.zip     s3://$bucketName/api.zip
aws s3 cp ./publish/web.zip     s3://$bucketName/web.zip
aws s3 cp ./publish/content.zip s3://$bucketName/content.zip
```

### B6 — Open a browser terminal to the server

EC2 Console → **Instances** → select `academy-server` → **Connect** button → **EC2 Instance Connect** tab → **Connect**. This opens a real terminal in a new browser tab — its SSH connection is made by AWS itself, so a local firewall blocking outbound SSH never sees it.

### B7 — Paste this into that browser terminal (installs everything + deploys in one go)

```bash
set -e
sudo apt-get update -y
sudo apt-get install -y nginx unzip curl

# AWS CLI v2 — installed via the official installer, NOT apt (Ubuntu 24.04 dropped the apt "awscli" package)
if ! command -v aws &> /dev/null; then
  curl -sSL "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o /tmp/awscliv2.zip
  cd /tmp && unzip -oq awscliv2.zip && sudo ./aws/install
fi

curl -sSL https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -o /tmp/packages-microsoft-prod.deb
sudo dpkg -i /tmp/packages-microsoft-prod.deb
sudo apt-get update -y
sudo apt-get install -y aspnetcore-runtime-10.0

sudo mkdir -p /var/www/academy-api /var/www/academy-web /var/www/academy-content
sudo chown -R ubuntu:ubuntu /var/www/academy-api /var/www/academy-web /var/www/academy-content

cd /tmp
aws s3 cp s3://<YOUR_BUCKET>/api.zip . --region <YOUR_REGION>
aws s3 cp s3://<YOUR_BUCKET>/web.zip . --region <YOUR_REGION>
aws s3 cp s3://<YOUR_BUCKET>/content.zip . --region <YOUR_REGION>
unzip -oq api.zip -d /var/www/academy-api
unzip -oq web.zip -d /var/www/academy-web
unzip -oq content.zip -d /var/www/academy-content
```

Then continue with the next section (the systemd + Nginx setup is identical for both paths).

---

## Step 5 — systemd service + Nginx (both paths)

Run on the server (over SSH for Path A, or the browser terminal for Path B):

```bash
sudo tee /etc/systemd/system/academy-api.service > /dev/null <<'EOF'
[Unit]
Description=Developer Engineering Academy API
After=network.target

[Service]
WorkingDirectory=/var/www/academy-api
ExecStart=/usr/bin/dotnet /var/www/academy-api/DeveloperEngineeringAcademy.Web.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=academy-api
User=ubuntu
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000
Environment=Content__RootPath=/var/www/academy-content

[Install]
WantedBy=multi-user.target
EOF

sudo systemctl daemon-reload
sudo systemctl enable academy-api
sudo systemctl restart academy-api

sudo tee /etc/nginx/sites-available/academy > /dev/null <<'EOF'
server {
    listen 80 default_server;
    listen [::]:80 default_server;
    server_name _;

    root /var/www/academy-web;
    index index.html;

    location /api/ {
        proxy_pass http://127.0.0.1:5000/api/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location / {
        try_files $uri $uri/ /index.html;
    }
}
EOF

sudo rm -f /etc/nginx/sites-enabled/default
sudo ln -sf /etc/nginx/sites-available/academy /etc/nginx/sites-enabled/academy
sudo nginx -t
sudo systemctl restart nginx
sudo systemctl enable nginx

sudo systemctl status academy-api --no-pager | head -6
curl -s http://127.0.0.1/api/topics | head -c 200
```

`ASPNETCORE_URLS=http://127.0.0.1:5000` binds to localhost only — the API is never directly reachable from the internet, only through Nginx. `Content__RootPath` is the environment-variable form of the `Content:RootPath` setting in `Program.cs`, needed because a published app has no reliable relative path back to `/content`.

## Step 6 — Verify from your own machine

```text
http://<ELASTIC_IP>/                      → Angular homepage
http://<ELASTIC_IP>/api/topics             → JSON topic list
http://<ELASTIC_IP>/api/content/validate  → content validation results
```

The Angular app calls a **relative** `/api` path, so it works identically by IP or (later) by domain, with no rebuild needed — `/api` requests are same-origin through Nginx, so no CORS configuration is required for the deployed app itself.

Once you add HTTPS (next section — highly recommended even without a real domain), the canonical URL switches to the HTTPS hostname; see below.

---

## Redeploying after a code change

**Path A (SSH works):**
```powershell
dotnet publish src/DeveloperEngineeringAcademy.Web/DeveloperEngineeringAcademy.Web.csproj -c Release -o ./publish/api
cd client/developer-academy; npm run build; cd ../..
scp -i $key -r ./publish/api/*                                             ubuntu@${ip}:/var/www/academy-api/
scp -i $key -r ./client/developer-academy/dist/developer-academy/browser/* ubuntu@${ip}:/var/www/academy-web/
```
```bash
sudo systemctl restart academy-api   # Nginx needs no restart for static file changes
```

**Path B (S3 + browser terminal):**
```powershell
dotnet publish src/DeveloperEngineeringAcademy.Web/DeveloperEngineeringAcademy.Web.csproj -c Release -o ./publish/api
cd client/developer-academy; npm run build; cd ../..
Compress-Archive -Path "./publish/api/*" -DestinationPath "./publish/api.zip" -Force
Compress-Archive -Path "./client/developer-academy/dist/developer-academy/browser/*" -DestinationPath "./publish/web.zip" -Force
aws s3 cp ./publish/api.zip s3://<YOUR_BUCKET>/api.zip
aws s3 cp ./publish/web.zip s3://<YOUR_BUCKET>/web.zip
```
Then, in a fresh EC2 Instance Connect browser terminal:
```bash
cd /tmp
aws s3 cp s3://<YOUR_BUCKET>/api.zip . --region <YOUR_REGION> && unzip -oq api.zip -d /var/www/academy-api
aws s3 cp s3://<YOUR_BUCKET>/web.zip . --region <YOUR_REGION> && unzip -oq web.zip -d /var/www/academy-web
sudo systemctl restart academy-api
```

See `scripts/publish-local.ps1` for a script that automates the local publish/build step.

---

## HTTPS without owning a domain (this deployment's actual approach)

Let's Encrypt cannot issue a certificate for a bare IP address — but you don't need to own a domain to get a **real, browser-trusted** certificate. [sslip.io](https://sslip.io) is a free public DNS service that resolves any hostname of the form `<ip-with-dashes>.sslip.io` to that exact IP automatically — no registration, no configuration. Since it's a genuine, publicly-resolvable DNS name, Certbot can validate and issue a normal certificate for it.

For Elastic IP `18.61.131.74`, the hostname is `18-61-131-74.sslip.io` (dots → dashes). Run this in an EC2 Instance Connect browser terminal (see Path B, Step B6, for how to open it):

```bash
sudo apt-get update -y
sudo apt-get install -y certbot python3-certbot-nginx

# Point the existing Nginx site at the sslip.io hostname (it stays default_server, so the raw IP still resolves too)
sudo sed -i 's/server_name _;/server_name 18-61-131-74.sslip.io;/' /etc/nginx/sites-available/academy
sudo nginx -t && sudo systemctl reload nginx

# Get the certificate, deploy it, and add the HTTP -> HTTPS redirect, all in one step
sudo certbot --nginx -d 18-61-131-74.sslip.io --register-unsafely-without-email --agree-tos --redirect --non-interactive

curl -s https://18-61-131-74.sslip.io/api/topics | head -c 200
```

Certbot edits the Nginx config automatically (adds the `ssl_certificate` directives and a 443 server block) and installs a `systemd` timer for automatic renewal — nothing else to maintain. Certificates from this method are real, CA-signed, and show no browser warnings.

**Known side effect:** Certbot's `--redirect` inserts a `server_name`-specific redirect check into the port-80 block, so plain `http://<ELASTIC_IP>/` (using the raw IP as the `Host` header, not the sslip.io name) now 404s instead of serving content — only requests to the sslip.io hostname redirect/serve correctly. This is expected and not a problem in practice, since `https://18-61-131-74.sslip.io` is the real URL to use and share, not the bare IP.

After this, update two local files so the app's own config matches the new HTTPS origin:
- `client/developer-academy/proxy.conf.json` → `"target": "https://18-61-131-74.sslip.io"`, `"secure": true`
- `src/DeveloperEngineeringAcademy.Web/appsettings.Production.json` → `"Cors": { "AllowedOrigins": ["https://18-61-131-74.sslip.io"] }`

### When you get a real domain later

1. At your domain registrar, create an **A record** pointing to `<ELASTIC_IP>`.
2. Re-run Certbot for the new domain: `sudo certbot --nginx -d yourdomain.com -d www.yourdomain.com --redirect`.
3. Update `server_name` in `/etc/nginx/sites-available/academy` to the new domain (Certbot may do this automatically).
4. Update `proxy.conf.json` and `Cors:AllowedOrigins` to `https://yourdomain.com` and redeploy.

No other application code changes are required — the API and Angular app are domain-agnostic (relative API paths) specifically so this step is additive, not a redesign.

## Troubleshooting

| Symptom | Check |
|---|---|
| SSH/SCP hangs, resets, or shows a non-OpenSSH banner | Your network is likely intercepting port 22 — switch to Path B |
| `502 Bad Gateway` from Nginx | `sudo systemctl status academy-api` — the API process probably isn't running; `journalctl -u academy-api -n 50 --no-pager` for the error |
| API starts then immediately exits, log mentions content directory | `Content__RootPath` env var in the systemd unit doesn't match where the content folder actually is |
| Angular loads but every API call 404s | Confirm the Nginx `location /api/` block's `proxy_pass` trailing slash is present (`http://127.0.0.1:5000/api/`, not `/api`) |
| Refreshing a deep link like `/csharp/generics` gives an Nginx 404 | Confirm the `try_files $uri $uri/ /index.html;` fallback is in the Nginx config (Angular's router needs this) |
| `apt-get install awscli` fails with "no installation candidate" | Ubuntu 24.04 dropped the apt package — install the official AWS CLI v2 zip instead (see Step B7) |
| `aws sts`/`ssm`/etc. calls fail with AccessDenied despite correct IAM policies, and the IAM policy simulator says "allowed" | Likely an AWS Organizations Service Control Policy blocking that service account-wide — not fixable from inside the account; use Path B (S3 + EC2 Instance Connect) instead of SSM |
| Can't SSH in at all (Path A) | Security group's SSH rule source is "My IP" and your IP changed — update the rule, or switch to Path B |
| Plain `http://<ELASTIC_IP>/` 404s after enabling HTTPS via sslip.io | Expected — Certbot's redirect only matches the sslip.io hostname's `Host` header; use `https://<dashes>.sslip.io` instead of the bare IP |

## This deployment's actual resources

Recorded here so future redeploys/cleanup reference the real resource IDs instead of placeholders:

| Resource | Value |
|---|---|
| Region | `ap-south-2` |
| Key pair | `academy-key` (`~/.ssh/academy-key.pem`) |
| Security group | `academy-sg` |
| EC2 instance | `academy-server` |
| Elastic IP | `18.61.131.74` |
| HTTPS hostname (sslip.io) | `18-61-131-74.sslip.io` |
| Certificate | Let's Encrypt via Certbot, auto-renews via `systemd` timer, expires 2026-12-24 |
| S3 bucket (Path B file transfer) | `academy-deploy-906969953319` |
| IAM role / instance profile | `academy-ec2-role` / `academy-ec2-profile` |

**Live site (canonical, HTTPS):** `https://18-61-131-74.sslip.io/`
**Live site (HTTP, redirects to the above):** `http://18-61-131-74.sslip.io/`
