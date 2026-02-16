# NCIEMS

Government-grade baseline for a Cyber Crime Case Management System.

## Stack
- Backend: `.NET 10`, `EF Core 10`, `SQL Server`, `MediatR`, `FluentValidation`, `Serilog`
- Frontend: `Angular` (module-based app with auth/guards/interceptor)

## Project Structure
- Backend code: `backend/src`
- Backend tests: `backend/tests`
- Frontend code: `frontend`

## Backend Run
Windows Application Control note:
- Build outputs are redirected to `C:\Dev\nciems-artifacts\...` via `Directory.Build.props` to avoid policy blocks when project is on Desktop.

1. Update secrets in `backend/src/Nciems.Api/appsettings.json`:
- `Jwt:Key`
- `EvidenceStorage:EncryptionKey` (base64 32-byte key)
- `BootstrapAdmin:Password`

2. Setup local HTTPS certificate:
```powershell
powershell -ExecutionPolicy Bypass -File scripts/setup-https-dev.ps1
```

3. Build:
```powershell
dotnet build Nciems.slnx
```

4. Apply migrations (already generated):
```powershell
dotnet ef database update -p backend/src/Nciems.Infrastructure/Nciems.Infrastructure.csproj -s backend/src/Nciems.Api/Nciems.Api.csproj
```

5. Run API:
```powershell
dotnet run --project backend/src/Nciems.Api/Nciems.Api.csproj
```

## Frontend Run
```powershell
cd frontend
npm install
npm start
```

Angular URL: `http://localhost:4200`

## Docker Run
From the repository root:

```powershell
# optional: copy .env.example to .env and change MSSQL_SA_PASSWORD
docker compose up --build
```

Services:
- Frontend: `http://localhost:4200`
- API (HTTP): `http://localhost:5289`
- API (HTTPS): `https://localhost:7261`
- SQL Server: `localhost:1433`

Notes:
- The API container runs in `Development` and applies EF migrations on startup.
- The frontend is built for production and served by Nginx.
- SQL data is persisted in the named volume `sqlserver-data`.
- SQL password can be overridden with `MSSQL_SA_PASSWORD` in a root `.env` file.

## Kubernetes (Docker Desktop)
1. In Docker Desktop, enable Kubernetes:
- `Settings` -> `Kubernetes` -> `Enable Kubernetes`

2. Verify context:
```powershell
kubectl config current-context
kubectl get nodes
```

3. Deploy manifests:
```powershell
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/
```

4. Check resources:
```powershell
kubectl get pods,svc -n cccms
```

Endpoints:
- Frontend: `http://localhost:4200`
- API OpenAPI: `https://localhost:7261/openapi/v1.json`
- Prometheus: `http://localhost:9090`
- Grafana: `http://localhost:3000`

If LoadBalancer is not exposed on localhost in your Docker Desktop setup, use:
```powershell
kubectl port-forward svc/cccms-frontend 4200:4200 -n cccms
kubectl port-forward svc/cccms-api 7261:7261 -n cccms
kubectl port-forward svc/cccms-prometheus 9090:9090 -n cccms
kubectl port-forward svc/cccms-grafana 3000:3000 -n cccms
```

Notes:
- Default SQL password in `k8s/secret.yaml` is `sa@123456789` (change it before shared usage).
- Default Grafana credentials in `k8s/monitoring-secret.yaml`:
  `admin` / `GrafanaAdmin@2026!`
- If GHCR images are private, create an image pull secret and attach it to the deployments.

## Helm (Docker Desktop Kubernetes)
1. Install Helm:
```powershell
winget install Helm.Helm
```
or:
```powershell
choco install kubernetes-helm
```

2. Install/upgrade chart:
```powershell
helm upgrade --install cccms ./helm/cccms --namespace cccms --create-namespace
```

Monitoring (enabled by default in Helm values):
- Prometheus service: `cccms-prometheus` (port `9090`)
- Grafana service: `cccms-grafana` (port `3000`)
- Grafana default login: `admin` / `GrafanaAdmin@2026!`

Example port-forward:
```powershell
kubectl port-forward svc/cccms-prometheus 9090:9090 -n cccms
kubectl port-forward svc/cccms-grafana 3000:3000 -n cccms
```

3. (Optional) Use GHCR pull secret:
```powershell
kubectl create secret docker-registry ghcr-creds `
  --docker-server=ghcr.io `
  --docker-username=<github-username> `
  --docker-password=<github-token> `
  --namespace cccms

helm upgrade --install cccms ./helm/cccms `
  --namespace cccms `
  --set global.imagePullSecrets[0].name=ghcr-creds
```

4. Disable monitoring (optional):
```powershell
helm upgrade --install cccms ./helm/cccms --namespace cccms --set monitoring.enabled=false
```

5. Uninstall:
```powershell
helm uninstall cccms -n cccms
```

## QA / QC Tests
Run automated quality and security checks:

```powershell
dotnet test Nciems.slnx
```

Security-focused tests are in:
- `backend/tests/Nciems.Security.Tests/InputValidationSecurityTests.cs`
- `backend/tests/Nciems.Security.Tests/MiddlewareSecurityTests.cs`

## Default Bootstrap User
- Username: from `BootstrapAdmin:UserName` (default `superadmin`)
- Password: from `BootstrapAdmin:Password`

Change it immediately before real usage.

## Development Seed Users
Created automatically on API startup in Development (`NciemsDb.Dev`):

| Role | Username | Password | MFA |
|---|---|---|---|
| SuperAdmin + SystemAdmin | `admin@govportal.com` | `GovPortal@2026!Secure` | Required |
| SystemAdmin | `system.admin@govportal.com` | `SystemAdmin@2026!Secure` | Required |
| IntakeOfficer | `intake.officer@govportal.com` | `IntakeOfficer@2026!Secure` | Disabled |
| Investigator | `investigator.one@govportal.com` | `Investigator@2026!Secure` | Required |
| ForensicAnalyst | `analyst.one@govportal.com` | `ForensicAnalyst@2026!Secure` | Required |
| Prosecutor | `prosecutor.one@govportal.com` | `Prosecutor@2026!Secure` | Disabled |

### Get Latest MFA OTP (Docker SQL Server)
Use this command to fetch the latest OTP notification for `analyst.one@govportal.com`:

```powershell
docker exec cccms-sqlserver /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "sa@123456789" -d "NciemsDb.Dev" -Q "SET NOCOUNT ON; SELECT TOP 1 n.Message, n.CreatedAtUtc FROM Notifications n JOIN Users u ON u.Id=n.UserId WHERE u.UserName='analyst.one@govportal.com' OR u.Email='analyst.one@govportal.com' ORDER BY n.CreatedAtUtc DESC;"
```

## Key Rules Enforced
- Role-based access and JWT auth with refresh tokens.
- MFA challenge for privileged roles.
- Lockout after 5 failed logins.
- Input security validation for XSS/SQL-injection patterns on sensitive commands and search filters.
- Password complexity policy for new user registration.
- Complaint -> Case workflow.
- Case status state machine with close restriction:
  case cannot close without `AnalystTechnical` report.
- Evidence upload is append-only by version.
- Evidence upload hardening (extension/path checks + max file size).
- SHA-256 + MD5 computed for each evidence version.
- Evidence encrypted at rest (AES).
- Evidence access logs and audit logs are mandatory writes.
- Security headers middleware (CSP, nosniff, frame deny, referrer policy, permissions policy, and no server header).
