# PROJECT_GUIDE.md

## 1. Quick Start (5-Minute Understanding)
This project is a production-grade Cyber Crime Case Management System for law-enforcement workflows.

Core goals:
- Manage complaints and investigation cases.
- Preserve forensic evidence integrity (hashing, encryption, immutable versions).
- Enforce strict role-based access and full auditability.
- Produce court-ready reports.

Mandatory principle:
- Anything previously "optional" is mandatory in this system.

## 2. Architecture Overview
Architecture style:
- Clean Architecture + DDD + CQRS + MediatR

Layers:
- `backend/src/Nciems.Api`: HTTP endpoints, auth middleware, request pipeline.
- `backend/src/Nciems.Application`: use-cases, commands/queries, validators, DTOs.
- `backend/src/Nciems.Domain`: entities, value objects, domain rules.
- `backend/src/Nciems.Infrastructure`: EF Core persistence, file storage, crypto services, integrations.

Rules:
- Controllers never access `DbContext` directly.
- All writes go through commands.
- All reads go through queries.

## 3. Module Breakdown
1. Identity and Access
- Users, roles, JWT, refresh token, MFA, lockout.

2. Complaints
- Intake officer creates complaint.
- Admin approves or rejects complaint.

3. Case Management
- Admin creates case from approved complaint.
- Investigator manages case status workflow.

4. Suspects
- Multiple suspects per case.
- Track identifiers: phone, IP, account info.

5. Evidence (forensic critical)
- Upload evidence version only.
- SHA-256 + MD5 required.
- Encrypted at rest.
- No delete, no in-place update.

6. Forensic Analysis
- Timeline, tagging, IP correlation, GeoIP enrichment.

7. Reports
- Case dossier, evidence report, chain of custody, analyst report.
- Digital signature and QR verification.

8. Audit and Security Logs
- Login success/failure, role changes, case status changes, evidence access.

9. Search and Intelligence
- Search by case ID, hash, phone, IP, suspect.

10. Dashboards
- Operational and intelligence views.

## 4. Database Schema Summary
Required tables:
- `Users`
- `Roles`
- `UserRoles`
- `RefreshTokens`
- `Complaints`
- `Cases`
- `CaseAssignments`
- `Suspects`
- `Evidence`
- `EvidenceVersions`
- `EvidenceAccessLogs`
- `Reports`
- `AuditLogs`
- `Notifications`

ERD source of truth:
- `ERD.dbml`

## 5. Security Model
Mandatory controls:
- HTTPS only + HSTS.
- Strict CORS allow-list.
- JWT access token + refresh token rotation.
- MFA for privileged roles.
- Lockout after 5 failed attempts.
- File whitelist + max upload size.
- Rate limiting.
- Antivirus scan hook before final evidence acceptance.

Authorization:
- Role-based + policy-based checks.
- Prosecutor is read-only.
- Evidence access requires explicit policy and must be logged.

## 6. Forensic Rules
1. No evidence without a case.
2. Evidence hash is immutable.
3. Evidence data is append-only by version.
4. Every evidence view/download is logged.
5. Chain of custody is mandatory for every evidence version event.
6. Case cannot close without analyst report.

## 7. API Conventions
General:
- Prefix: `/api/v1`
- Use `ProblemDetails` for errors.
- Include correlation ID header in requests and logs.

Patterns:
- Commands: POST/PUT actions that change state.
- Queries: GET actions that read state.
- Never leak internal entity models directly.

Suggested endpoint groups:
- `/api/v1/auth`
- `/api/v1/complaints`
- `/api/v1/cases`
- `/api/v1/suspects`
- `/api/v1/evidence`
- `/api/v1/reports`
- `/api/v1/search`
- `/api/v1/admin`

## 8. Angular Structure
Expected structure:
- `frontend/src/app/core`: auth, interceptors, guards, layout shell.
- `frontend/src/app/features/dashboard`
- `frontend/src/app/features/complaints`
- `frontend/src/app/features/cases`
- `frontend/src/app/features/evidence`
- `frontend/src/app/features/reports`
- `frontend/src/app/features/admin`
- `frontend/src/app/shared`: common components, pipes, utilities.

Client requirements:
- JWT interceptor with refresh flow.
- Route guards by role and policy.
- Reactive forms + validation.

## 9. Development Workflow
1. Create branch per feature.
2. Implement command/query + validation + handler.
3. Add auth and policy checks.
4. Add audit and evidence access logging where needed.
5. Add tests (unit + integration).
6. Run migrations for schema changes.
7. Validate security checklist before merge.

Baseline quality gates:
- Build success.
- Tests pass.
- No bypass of forensic or audit rules.

## 10. AI Agent Modification Protocol
When changing this system, always:
1. Preserve forensic invariants (immutability, hashing, chain of custody).
2. Preserve audit trails (never remove required logs).
3. Enforce role/policy authorization at API and application layers.
4. Keep controllers thin and use application use-cases.
5. Add/update validation for every new input.
6. Update `ERD.dbml` when schema changes.

Do not:
- Add direct evidence delete/update operations.
- Allow case closure without analyst report.
- Return sensitive data to unauthorized roles.
