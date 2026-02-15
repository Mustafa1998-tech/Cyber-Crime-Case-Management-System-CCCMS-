using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Nciems.Application.Interfaces;
using Nciems.Domain.Entities;
using Nciems.Domain.Enums;
using Nciems.Infrastructure.Options;

namespace Nciems.Infrastructure.Persistence;

public sealed class DbSeeder(
    AppDbContext dbContext,
    IPasswordHasher passwordHasher,
    IEvidenceFileService evidenceFileService,
    IOptions<BootstrapAdminOptions> bootstrapOptions,
    IHostEnvironment hostEnvironment)
{
    private const string HistoricalSeedVersion = "HistoricalDatasetV1";
    private const string DashboardDimensionSeedVersion = "DashboardDimensionEnrichmentV1";

    private static readonly string[] CrimeTypes =
    [
        "Extortion",
        "Fraud",
        "Defamation",
        "Unauthorized Access",
        "Identity Theft",
        "Phishing",
        "Account Takeover"
    ];

    private static readonly string[] FirstNames =
    [
        "Mohamed", "Ahmed", "Omar", "Hassan", "Mustafa", "Yousef", "Ali", "Khalid",
        "Sara", "Mona", "Lina", "Rania", "Noor", "Huda", "Fatima", "Aya"
    ];

    private static readonly string[] LastNames =
    [
        "Ibrahim", "Mahmoud", "Abdullah", "Farouk", "Salim", "Ismail", "Yasin", "Kareem",
        "Hassan", "Nasser", "Abbas", "Rahman", "Osman", "Saad", "Hamid", "Qasim"
    ];

    private static readonly string[] Platforms =
    [
        "WhatsApp",
        "Telegram",
        "Facebook",
        "Instagram",
        "X",
        "Email",
        "Marketplace",
        "Banking App"
    ];

    private static readonly (string State, int Weight)[] SudanStates =
    [
        ("Khartoum", 26),
        ("Al Jazirah", 11),
        ("River Nile", 8),
        ("Red Sea", 8),
        ("North Kordofan", 7),
        ("South Kordofan", 7),
        ("West Kordofan", 5),
        ("Gedaref", 6),
        ("Kassala", 6),
        ("Blue Nile", 4),
        ("Sennar", 5),
        ("Northern", 3),
        ("White Nile", 6),
        ("East Darfur", 4),
        ("West Darfur", 3),
        ("South Darfur", 5),
        ("North Darfur", 4),
        ("Central Darfur", 3)
    ];

    private static readonly string[] DeviceProfiles =
    [
        "Windows 11 / Chrome",
        "Windows 11 / Edge",
        "Android / Chrome Mobile",
        "iOS / Safari",
        "Forensic Workstation / Secure Client"
    ];

    private static readonly (string Extension, string MimeType)[] SeedFileFormats =
    [
        (".txt", "text/plain"),
        (".json", "application/json"),
        (".csv", "text/csv"),
        (".html", "text/html"),
        (".eml", "message/rfc822")
    ];

    private sealed record SeedUser(
        string UserName,
        string Email,
        string Password,
        bool MfaEnabled,
        IReadOnlyCollection<string> Roles);

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);

        var existingRoles = await dbContext.Roles.Select(x => x.Name).ToListAsync(cancellationToken);
        var missingRoles = RoleNames.All.Except(existingRoles).ToArray();

        foreach (var roleName in missingRoles)
        {
            dbContext.Roles.Add(new Role
            {
                Name = roleName,
                Description = $"{roleName} role"
            });
        }

        if (missingRoles.Length > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var adminConfig = bootstrapOptions.Value;
        var admin = await dbContext.Users
            .Include(x => x.UserRoles)
            .Where(x => x.UserName == adminConfig.UserName || x.Email == adminConfig.Email)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (admin is null)
        {
            var password = passwordHasher.HashPassword(adminConfig.Password);
            admin = new User
            {
                UserName = adminConfig.UserName,
                Email = adminConfig.Email,
                PasswordHash = password.Hash,
                PasswordSalt = password.Salt,
                MfaEnabled = true
            };
            dbContext.Users.Add(admin);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else if (hostEnvironment.IsDevelopment())
        {
            // In development, keep bootstrap credentials aligned with appsettings
            // to avoid stale password/hash issues during iterative setup.
            var password = passwordHasher.HashPassword(adminConfig.Password);
            admin.UserName = adminConfig.UserName;
            admin.Email = adminConfig.Email;
            admin.PasswordHash = password.Hash;
            admin.PasswordSalt = password.Salt;
            admin.MfaEnabled = true;
            admin.FailedLoginAttempts = 0;
            admin.IsLocked = false;
            admin.LockoutEndUtc = null;
            admin.PendingMfaCodeHash = null;
            admin.PendingMfaCodeExpiresAtUtc = null;
            admin.UpdatedAtUtc = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var roleMap = await dbContext.Roles.ToDictionaryAsync(x => x.Name, x => x.Id, cancellationToken);
        var requiredAdminRoles = new[] { RoleNames.SuperAdmin, RoleNames.SystemAdmin };
        var assignedRoleIds = await dbContext.UserRoles
            .Where(x => x.UserId == admin.Id)
            .Select(x => x.RoleId)
            .ToListAsync(cancellationToken);

        foreach (var roleName in requiredAdminRoles)
        {
            var roleId = roleMap[roleName];
            if (assignedRoleIds.Contains(roleId))
            {
                continue;
            }

            dbContext.UserRoles.Add(new UserRole
            {
                UserId = admin.Id,
                RoleId = roleId
            });
        }

        if (hostEnvironment.IsDevelopment())
        {
            var devUsers = new[]
            {
                new SeedUser(
                    "system.admin@govportal.com",
                    "system.admin@govportal.com",
                    "SystemAdmin@2026!Secure",
                    true,
                    [RoleNames.SystemAdmin]),
                new SeedUser(
                    "intake.officer@govportal.com",
                    "intake.officer@govportal.com",
                    "IntakeOfficer@2026!Secure",
                    false,
                    [RoleNames.IntakeOfficer]),
                new SeedUser(
                    "investigator.one@govportal.com",
                    "investigator.one@govportal.com",
                    "Investigator@2026!Secure",
                    true,
                    [RoleNames.Investigator]),
                new SeedUser(
                    "analyst.one@govportal.com",
                    "analyst.one@govportal.com",
                    "ForensicAnalyst@2026!Secure",
                    true,
                    [RoleNames.ForensicAnalyst]),
                new SeedUser(
                    "prosecutor.one@govportal.com",
                    "prosecutor.one@govportal.com",
                    "Prosecutor@2026!Secure",
                    false,
                    [RoleNames.Prosecutor])
            };

            foreach (var seedUser in devUsers)
            {
                await UpsertDevelopmentUserAsync(seedUser, roleMap, cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await SeedHistoricalDevelopmentDataAsync(admin.Id, cancellationToken);
            await SeedDashboardDimensionEnrichmentAsync(admin.Id, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertDevelopmentUserAsync(
        SeedUser seedUser,
        IReadOnlyDictionary<string, long> roleMap,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(x => x.UserRoles)
            .Where(x => x.UserName == seedUser.UserName || x.Email == seedUser.Email)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var password = passwordHasher.HashPassword(seedUser.Password);
        if (user is null)
        {
            user = new User
            {
                UserName = seedUser.UserName,
                Email = seedUser.Email,
                PasswordHash = password.Hash,
                PasswordSalt = password.Salt,
                MfaEnabled = seedUser.MfaEnabled,
                FailedLoginAttempts = 0,
                IsLocked = false,
                LockoutEndUtc = null
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            user.UserName = seedUser.UserName;
            user.Email = seedUser.Email;
            user.PasswordHash = password.Hash;
            user.PasswordSalt = password.Salt;
            user.MfaEnabled = seedUser.MfaEnabled;
            user.FailedLoginAttempts = 0;
            user.IsLocked = false;
            user.LockoutEndUtc = null;
            user.PendingMfaCodeHash = null;
            user.PendingMfaCodeExpiresAtUtc = null;
            user.UpdatedAtUtc = DateTime.UtcNow;
        }

        var desiredRoleIds = seedUser.Roles.Select(roleName => roleMap[roleName]).ToHashSet();
        var existingRoles = user.UserRoles.ToArray();

        foreach (var userRole in existingRoles.Where(x => !desiredRoleIds.Contains(x.RoleId)))
        {
            dbContext.UserRoles.Remove(userRole);
        }

        foreach (var roleId in desiredRoleIds)
        {
            if (existingRoles.Any(x => x.RoleId == roleId))
            {
                continue;
            }

            dbContext.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = roleId
            });
        }
    }

    private async Task SeedHistoricalDevelopmentDataAsync(long adminUserId, CancellationToken cancellationToken)
    {
        var seedAlreadyApplied = await dbContext.AuditLogs.AnyAsync(
            x => x.Action == "DevelopmentSeed" &&
                 x.EntityName == "HistoricalDataset" &&
                 x.EntityId == HistoricalSeedVersion,
            cancellationToken);

        if (seedAlreadyApplied)
        {
            return;
        }

        var users = await dbContext.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .ToListAsync(cancellationToken);

        var admins = GetUsersInRoles(users, RoleNames.SuperAdmin, RoleNames.SystemAdmin);
        var intakeOfficers = GetUsersInRoles(users, RoleNames.IntakeOfficer, RoleNames.SystemAdmin, RoleNames.SuperAdmin);
        var investigators = GetUsersInRoles(users, RoleNames.Investigator, RoleNames.SystemAdmin, RoleNames.SuperAdmin);
        var analysts = GetUsersInRoles(users, RoleNames.ForensicAnalyst, RoleNames.SystemAdmin, RoleNames.SuperAdmin);
        var prosecutors = GetUsersInRoles(users, RoleNames.Prosecutor, RoleNames.SystemAdmin);

        if (admins.Count == 0)
        {
            admins.Add(adminUserId);
        }

        if (intakeOfficers.Count == 0)
        {
            intakeOfficers.Add(adminUserId);
        }

        if (investigators.Count == 0)
        {
            investigators.Add(adminUserId);
        }

        if (analysts.Count == 0)
        {
            analysts.Add(adminUserId);
        }

        if (prosecutors.Count == 0)
        {
            prosecutors.Add(adminUserId);
        }

        var random = new Random(20260215);
        var nowUtc = DateTime.UtcNow;
        var startDateUtc = nowUtc.AddYears(-3).Date;

        var complaints = new List<Complaint>(1500);
        for (var day = startDateUtc; day <= nowUtc.Date; day = day.AddDays(1))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dailyVolume = random.NextDouble() switch
            {
                < 0.05 => 3,
                < 0.22 => 2,
                _ => 1
            };

            for (var i = 0; i < dailyVolume; i++)
            {
                var createdAtUtc = day
                    .AddHours(random.Next(0, 24))
                    .AddMinutes(random.Next(0, 60))
                    .AddSeconds(random.Next(0, 60));

                if (createdAtUtc > nowUtc)
                {
                    createdAtUtc = nowUtc.AddMinutes(-random.Next(5, 60));
                }

                var crimeType = Pick(CrimeTypes, random);
                var complainantName = $"{Pick(FirstNames, random)} {Pick(LastNames, random)}";
                var platform = Pick(Platforms, random);
                var state = PickWeightedState(random);
                var complaintStatus = DetermineComplaintStatus(createdAtUtc, nowUtc, random);

                complaints.Add(new Complaint
                {
                    ComplainantName = complainantName,
                    Phone = GenerateSudanLikePhone(random),
                    CrimeType = crimeType,
                    Description = BuildComplaintDescription(state, platform),
                    Status = complaintStatus,
                    CreatedByUserId = Pick(intakeOfficers, random),
                    CreatedAtUtc = createdAtUtc
                });
            }
        }

        dbContext.Complaints.AddRange(complaints);
        await dbContext.SaveChangesAsync(cancellationToken);

        var approvedComplaints = complaints
            .Where(x => x.Status == ComplaintStatus.Approved)
            .OrderBy(x => x.CreatedAtUtc)
            .ToArray();

        var cases = new List<Case>(approvedComplaints.Length);
        foreach (var complaint in approvedComplaints)
        {
            var caseCreatedAtUtc = complaint.CreatedAtUtc.AddHours(random.Next(2, 72));
            if (caseCreatedAtUtc > nowUtc)
            {
                caseCreatedAtUtc = nowUtc.AddMinutes(-random.Next(5, 90));
            }

            var status = DetermineCaseStatus(caseCreatedAtUtc, nowUtc, random);
            var closedAtUtc = status == CaseStatus.Closed
                ? ComputeClosedAt(caseCreatedAtUtc, nowUtc, random)
                : null;

            cases.Add(new Case
            {
                ComplaintId = complaint.Id,
                AssignedInvestigatorId = Pick(investigators, random),
                Status = status,
                Priority = ResolvePriority(complaint.CrimeType, random),
                CreatedAtUtc = caseCreatedAtUtc,
                ClosedAtUtc = closedAtUtc
            });
        }

        dbContext.Cases.AddRange(cases);
        await dbContext.SaveChangesAsync(cancellationToken);

        var assignments = cases
            .Where(x => x.AssignedInvestigatorId.HasValue)
            .Select(x => new CaseAssignment
            {
                CaseId = x.Id,
                InvestigatorId = x.AssignedInvestigatorId!.Value,
                AssignedByUserId = Pick(admins, random),
                AssignedAtUtc = ClampToNow(x.CreatedAtUtc.AddMinutes(random.Next(10, 360)), nowUtc)
            })
            .ToArray();

        dbContext.CaseAssignments.AddRange(assignments);

        var suspects = new List<Suspect>(cases.Count * 2);
        foreach (var caseEntity in cases)
        {
            var suspectCount = caseEntity.Status switch
            {
                CaseStatus.New => random.NextDouble() < 0.25 ? 1 : 0,
                CaseStatus.UnderInvestigation => random.Next(1, 3),
                _ => random.Next(1, 4)
            };

            for (var i = 0; i < suspectCount; i++)
            {
                suspects.Add(new Suspect
                {
                    CaseId = caseEntity.Id,
                    Name = $"{Pick(FirstNames, random)} {Pick(LastNames, random)}",
                    NationalId = $"NID-{random.Next(100000000, 999999999)}",
                    Phone = GenerateSudanLikePhone(random),
                    IpAddress = GenerateIp(random),
                    AccountInfo = $"{Pick(Platforms, random)}: user_{random.Next(10000, 99999)}",
                    Notes = "Historical seed suspect profile.",
                    CreatedAtUtc = ClampToNow(caseEntity.CreatedAtUtc.AddHours(random.Next(6, 240)), nowUtc)
                });
            }
        }

        dbContext.Suspects.AddRange(suspects);
        await dbContext.SaveChangesAsync(cancellationToken);

        var evidenceItems = new List<Evidence>(cases.Count);
        foreach (var caseEntity in cases)
        {
            var evidenceCount = caseEntity.Status switch
            {
                CaseStatus.New => random.NextDouble() < 0.2 ? 1 : 0,
                CaseStatus.UnderInvestigation => random.Next(1, 3),
                CaseStatus.ForensicAnalysis => random.Next(1, 3),
                CaseStatus.ProsecutorReview => random.Next(2, 4),
                CaseStatus.Closed => random.Next(2, 4),
                _ => 1
            };

            for (var i = 0; i < evidenceCount; i++)
            {
                var createdByUserId = random.NextDouble() < 0.7
                    ? caseEntity.AssignedInvestigatorId ?? Pick(investigators, random)
                    : Pick(analysts, random);

                evidenceItems.Add(new Evidence
                {
                    CaseId = caseEntity.Id,
                    Title = $"Evidence Item {i + 1} - {caseEntity.Priority}",
                    Description = $"Evidence captured from {Pick(Platforms, random)} channel.",
                    CreatedByUserId = createdByUserId,
                    CreatedAtUtc = ClampToNow(caseEntity.CreatedAtUtc.AddHours(random.Next(8, 360)), nowUtc)
                });
            }
        }

        dbContext.Evidence.AddRange(evidenceItems);
        await dbContext.SaveChangesAsync(cancellationToken);

        var evidenceVersions = new List<EvidenceVersion>(evidenceItems.Count + 600);
        foreach (var evidence in evidenceItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var versionCount = random.NextDouble() < 0.18 ? 2 : 1;
            for (var versionNumber = 1; versionNumber <= versionCount; versionNumber++)
            {
                var fileFormat = Pick(SeedFileFormats, random);
                var originalFileName = $"case-{evidence.CaseId}-evidence-{evidence.Id}-v{versionNumber}{fileFormat.Extension}";

                var payload = BuildSeedEvidencePayload(evidence, versionNumber, random);
                var fileBytes = Encoding.UTF8.GetBytes(payload);
                var savedFile = await evidenceFileService.SaveEncryptedAsync(fileBytes, originalFileName, cancellationToken);

                var uploadedAtUtc = ClampToNow(evidence.CreatedAtUtc.AddHours(random.Next(1, 144)), nowUtc);
                var uploadedBy = versionNumber > 1
                    ? Pick(analysts, random)
                    : evidence.CreatedByUserId;

                evidenceVersions.Add(new EvidenceVersion
                {
                    EvidenceId = evidence.Id,
                    VersionNumber = versionNumber,
                    OriginalFileName = originalFileName,
                    StoredFilePath = savedFile.StoredFilePath,
                    Sha256Hash = savedFile.Sha256Hash,
                    Md5Hash = savedFile.Md5Hash,
                    FileSizeBytes = savedFile.FileSizeBytes,
                    MimeType = fileFormat.MimeType,
                    EncryptionIv = savedFile.EncryptionIv,
                    DeviceInfo = Pick(DeviceProfiles, random),
                    UploadedByUserId = uploadedBy,
                    UploadedAtUtc = uploadedAtUtc
                });
            }
        }

        dbContext.EvidenceVersions.AddRange(evidenceVersions);
        await dbContext.SaveChangesAsync(cancellationToken);

        var evidenceAccessLogs = new List<EvidenceAccessLog>(evidenceVersions.Count * 3);
        var accessUsers = investigators
            .Concat(analysts)
            .Concat(prosecutors)
            .Concat(admins)
            .Distinct()
            .ToArray();

        foreach (var version in evidenceVersions)
        {
            var accessCount = random.Next(1, 5);
            for (var i = 0; i < accessCount; i++)
            {
                evidenceAccessLogs.Add(new EvidenceAccessLog
                {
                    EvidenceVersionId = version.Id,
                    AccessedByUserId = Pick(accessUsers, random),
                    AccessType = DetermineAccessType(random),
                    AccessedAtUtc = ClampToNow(version.UploadedAtUtc.AddHours(random.Next(1, 720)), nowUtc)
                });
            }
        }

        dbContext.EvidenceAccessLogs.AddRange(evidenceAccessLogs);

        var reports = new List<Report>(cases.Count * 2);
        foreach (var caseEntity in cases)
        {
            var reportTimeline = caseEntity.ClosedAtUtc ?? nowUtc;
            var cursor = ClampToNow(caseEntity.CreatedAtUtc.AddDays(random.Next(1, 20)), reportTimeline);

            if (caseEntity.Status >= CaseStatus.ForensicAnalysis)
            {
                reports.Add(CreateReport(
                    caseEntity.Id,
                    ReportType.AnalystTechnical,
                    Pick(analysts, random),
                    cursor,
                    random));
                cursor = ClampToNow(cursor.AddDays(random.Next(1, 10)), reportTimeline);
            }

            if (caseEntity.Status >= CaseStatus.ProsecutorReview)
            {
                reports.Add(CreateReport(
                    caseEntity.Id,
                    ReportType.ChainOfCustody,
                    Pick(analysts, random),
                    cursor,
                    random));
                cursor = ClampToNow(cursor.AddDays(random.Next(1, 10)), reportTimeline);

                reports.Add(CreateReport(
                    caseEntity.Id,
                    ReportType.CaseDossier,
                    Pick(prosecutors, random),
                    cursor,
                    random));
            }

            if (caseEntity.Status == CaseStatus.Closed || random.NextDouble() < 0.35)
            {
                var reportTime = ClampToNow(cursor.AddDays(random.Next(0, 5)), reportTimeline);
                reports.Add(CreateReport(
                    caseEntity.Id,
                    ReportType.EvidenceReport,
                    Pick(analysts, random),
                    reportTime,
                    random));
            }
        }

        dbContext.Reports.AddRange(reports);

        var notifications = new List<Notification>(260);
        for (var month = 0; month < 36; month++)
        {
            var monthlyDate = startDateUtc.AddMonths(month);
            var monthNotifications = random.Next(4, 10);
            for (var i = 0; i < monthNotifications; i++)
            {
                var createdAtUtc = monthlyDate
                    .AddDays(random.Next(0, 28))
                    .AddHours(random.Next(7, 20))
                    .AddMinutes(random.Next(0, 60));

                if (createdAtUtc > nowUtc)
                {
                    createdAtUtc = nowUtc.AddMinutes(-random.Next(30, 360));
                }

                var notificationCase = cases[random.Next(cases.Count)];
                notifications.Add(new Notification
                {
                    UserId = Pick(accessUsers, random),
                    Title = "Operational Alert",
                    Message = $"Case #{notificationCase.Id} moved to {notificationCase.Status}.",
                    Read = random.NextDouble() < 0.72,
                    CreatedAtUtc = createdAtUtc
                });
            }
        }

        dbContext.Notifications.AddRange(notifications);

        var auditLogs = new List<AuditLog>(complaints.Count + cases.Count * 2 + reports.Count + evidenceVersions.Count + 8);

        auditLogs.AddRange(complaints.Select(complaint => new AuditLog
        {
            UserId = complaint.CreatedByUserId,
            Action = "ComplaintCreated",
            EntityName = nameof(Complaint),
            EntityId = complaint.Id.ToString(),
            TimestampUtc = complaint.CreatedAtUtc,
            Details = $"Complaint created ({complaint.CrimeType})."
        }));

        auditLogs.AddRange(cases.Select(caseEntity => new AuditLog
        {
            UserId = caseEntity.AssignedInvestigatorId,
            Action = "CaseCreated",
            EntityName = nameof(Case),
            EntityId = caseEntity.Id.ToString(),
            TimestampUtc = caseEntity.CreatedAtUtc,
            Details = $"Case opened with priority {caseEntity.Priority}."
        }));

        auditLogs.AddRange(cases
            .Where(x => x.Status != CaseStatus.New)
            .Select(caseEntity => new AuditLog
            {
                UserId = caseEntity.AssignedInvestigatorId,
                Action = "CaseStatusChanged",
                EntityName = nameof(Case),
                EntityId = caseEntity.Id.ToString(),
                TimestampUtc = caseEntity.ClosedAtUtc ?? ClampToNow(caseEntity.CreatedAtUtc.AddDays(5), nowUtc),
                Details = $"Status changed to {caseEntity.Status}."
            }));

        auditLogs.AddRange(evidenceVersions.Select(version => new AuditLog
        {
            UserId = version.UploadedByUserId,
            Action = "EvidenceUploaded",
            EntityName = nameof(EvidenceVersion),
            EntityId = version.Id.ToString(),
            TimestampUtc = version.UploadedAtUtc,
            Details = $"Evidence version {version.VersionNumber} uploaded."
        }));

        auditLogs.AddRange(reports.Select(report => new AuditLog
        {
            UserId = report.GeneratedByUserId,
            Action = "ReportGenerated",
            EntityName = nameof(Report),
            EntityId = report.Id.ToString(),
            TimestampUtc = report.GeneratedAtUtc,
            Details = $"Generated {report.ReportType} report."
        }));

        auditLogs.Add(new AuditLog
        {
            UserId = adminUserId,
            Action = "DevelopmentSeed",
            EntityName = "HistoricalDataset",
            EntityId = HistoricalSeedVersion,
            TimestampUtc = nowUtc,
            Details = $"Generated 3-year dataset: complaints={complaints.Count}, cases={cases.Count}, suspects={suspects.Count}, evidence={evidenceItems.Count}, versions={evidenceVersions.Count}, reports={reports.Count}."
        });

        dbContext.AuditLogs.AddRange(auditLogs);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedDashboardDimensionEnrichmentAsync(long adminUserId, CancellationToken cancellationToken)
    {
        var seedAlreadyApplied = await dbContext.AuditLogs.AnyAsync(
            x => x.Action == "DevelopmentSeed" &&
                 x.EntityName == "DashboardDimensions" &&
                 x.EntityId == DashboardDimensionSeedVersion,
            cancellationToken);

        if (seedAlreadyApplied)
        {
            return;
        }

        var complaints = await dbContext.Complaints
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        if (complaints.Count == 0)
        {
            return;
        }

        var random = new Random(20260216);
        var stateCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var platformCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var complaint in complaints)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (ContainsDashboardTags(complaint.Description))
            {
                var existingState = ExtractTaggedValue(complaint.Description, "State");
                var existingPlatform = ExtractTaggedValue(complaint.Description, "Platform");
                if (!string.IsNullOrWhiteSpace(existingState))
                {
                    stateCounts[existingState] = (stateCounts.GetValueOrDefault(existingState) + 1);
                }

                if (!string.IsNullOrWhiteSpace(existingPlatform))
                {
                    platformCounts[existingPlatform] = (platformCounts.GetValueOrDefault(existingPlatform) + 1);
                }

                continue;
            }

            var state = PickWeightedState(random);
            var platform = Pick(Platforms, random);
            complaint.Description = BuildComplaintDescription(state, platform, complaint.Description);
            stateCounts[state] = stateCounts.GetValueOrDefault(state) + 1;
            platformCounts[platform] = platformCounts.GetValueOrDefault(platform) + 1;
        }

        var stateSummary = string.Join(
            ", ",
            stateCounts.OrderByDescending(x => x.Value).Take(6).Select(x => $"{x.Key}:{x.Value}"));
        var platformSummary = string.Join(
            ", ",
            platformCounts.OrderByDescending(x => x.Value).Take(4).Select(x => $"{x.Key}:{x.Value}"));

        dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = adminUserId,
            Action = "DevelopmentSeed",
            EntityName = "DashboardDimensions",
            EntityId = DashboardDimensionSeedVersion,
            TimestampUtc = DateTime.UtcNow,
            Details = $"Enriched complaint dimensions with state/platform tags. Top states => {stateSummary}. Top platforms => {platformSummary}."
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static List<long> GetUsersInRoles(IReadOnlyCollection<User> users, params string[] roles)
    {
        var roleSet = roles.ToHashSet(StringComparer.Ordinal);
        return users
            .Where(user => user.UserRoles.Any(x => x.Role is not null && roleSet.Contains(x.Role.Name)))
            .Select(x => x.Id)
            .Distinct()
            .ToList();
    }

    private static ComplaintStatus DetermineComplaintStatus(DateTime createdAtUtc, DateTime nowUtc, Random random)
    {
        var ageDays = (nowUtc - createdAtUtc).TotalDays;
        if (ageDays < 2)
        {
            return random.NextDouble() < 0.75
                ? ComplaintStatus.New
                : ComplaintStatus.Approved;
        }

        if (ageDays < 14)
        {
            var roll = random.NextDouble();
            if (roll < 0.2)
            {
                return ComplaintStatus.New;
            }

            return roll < 0.84
                ? ComplaintStatus.Approved
                : ComplaintStatus.Rejected;
        }

        return random.NextDouble() < 0.82
            ? ComplaintStatus.Approved
            : ComplaintStatus.Rejected;
    }

    private static CaseStatus DetermineCaseStatus(DateTime caseCreatedAtUtc, DateTime nowUtc, Random random)
    {
        var ageDays = (nowUtc - caseCreatedAtUtc).TotalDays;
        if (ageDays < 3)
        {
            return random.NextDouble() < 0.65
                ? CaseStatus.New
                : CaseStatus.UnderInvestigation;
        }

        if (ageDays < 15)
        {
            return random.NextDouble() < 0.55
                ? CaseStatus.UnderInvestigation
                : CaseStatus.ForensicAnalysis;
        }

        if (ageDays < 45)
        {
            var roll = random.NextDouble();
            if (roll < 0.35)
            {
                return CaseStatus.UnderInvestigation;
            }

            if (roll < 0.7)
            {
                return CaseStatus.ForensicAnalysis;
            }

            return roll < 0.88
                ? CaseStatus.ProsecutorReview
                : CaseStatus.Closed;
        }

        var matureRoll = random.NextDouble();
        if (matureRoll < 0.12)
        {
            return CaseStatus.UnderInvestigation;
        }

        if (matureRoll < 0.26)
        {
            return CaseStatus.ForensicAnalysis;
        }

        if (matureRoll < 0.42)
        {
            return CaseStatus.ProsecutorReview;
        }

        return CaseStatus.Closed;
    }

    private static DateTime? ComputeClosedAt(DateTime caseCreatedAtUtc, DateTime nowUtc, Random random)
    {
        var closedAtUtc = caseCreatedAtUtc.AddDays(random.Next(6, 120))
            .AddHours(random.Next(0, 24))
            .AddMinutes(random.Next(0, 60));

        return ClampToNow(closedAtUtc, nowUtc);
    }

    private static string ResolvePriority(string crimeType, Random random) =>
        crimeType switch
        {
            "Extortion" => random.NextDouble() < 0.6 ? "Critical" : "High",
            "Identity Theft" => random.NextDouble() < 0.45 ? "High" : "Medium",
            "Account Takeover" => random.NextDouble() < 0.45 ? "High" : "Medium",
            "Fraud" => random.NextDouble() < 0.35 ? "High" : "Medium",
            _ => random.NextDouble() < 0.2 ? "High" : "Medium"
        };

    private static EvidenceAccessType DetermineAccessType(Random random)
    {
        var roll = random.NextDouble();
        if (roll < 0.6)
        {
            return EvidenceAccessType.View;
        }

        if (roll < 0.85)
        {
            return EvidenceAccessType.Analysis;
        }

        return EvidenceAccessType.Download;
    }

    private static Report CreateReport(
        long caseId,
        ReportType reportType,
        long generatedByUserId,
        DateTime generatedAtUtc,
        Random random)
    {
        var signature = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var pdfFileName = $"{reportType}-{caseId}-{generatedAtUtc:yyyyMMddHHmmss}.pdf";
        return new Report
        {
            CaseId = caseId,
            ReportType = reportType,
            Content = BuildReportContent(reportType),
            GeneratedByUserId = generatedByUserId,
            GeneratedAtUtc = generatedAtUtc,
            PdfPath = Path.Combine("reports", generatedAtUtc.ToString("yyyy"), generatedAtUtc.ToString("MM"), pdfFileName),
            DigitalSignature = signature,
            QrPayload = $"nciems://verify/report/{caseId}/{(int)reportType}/{signature[..16]}/{random.Next(100000, 999999)}"
        };
    }

    private static string BuildReportContent(ReportType reportType) =>
        reportType switch
        {
            ReportType.AnalystTechnical => "Analyst findings: device metadata, timeline correlation, and attribution indicators.",
            ReportType.ChainOfCustody => "Chain of custody verified with complete evidence access trail and integrity checks.",
            ReportType.CaseDossier => "Case dossier prepared for prosecutorial review, including witness and evidence summary.",
            ReportType.EvidenceReport => "Evidence report generated with hashes, file provenance, and relevance summary.",
            _ => "Report generated."
        };

    private static string BuildSeedEvidencePayload(Evidence evidence, int versionNumber, Random random)
    {
        var lines = new[]
        {
            $"CaseId: {evidence.CaseId}",
            $"EvidenceId: {evidence.Id}",
            $"Version: {versionNumber}",
            $"CapturedFrom: {Pick(Platforms, random)}",
            $"Reference: REF-{random.Next(100000, 999999)}-{random.Next(1000, 9999)}",
            "Summary: Historical dataset payload for development and analytics simulation."
        };

        return string.Join(Environment.NewLine, lines);
    }

    private static bool ContainsDashboardTags(string description) =>
        description.Contains("[State:", StringComparison.OrdinalIgnoreCase) &&
        description.Contains("[Platform:", StringComparison.OrdinalIgnoreCase);

    private static string? ExtractTaggedValue(string description, string tagName)
    {
        var startToken = $"[{tagName}:";
        var startIndex = description.IndexOf(startToken, StringComparison.OrdinalIgnoreCase);
        if (startIndex < 0)
        {
            return null;
        }

        var valueStart = startIndex + startToken.Length;
        var endIndex = description.IndexOf(']', valueStart);
        if (endIndex <= valueStart)
        {
            return null;
        }

        return description[valueStart..endIndex].Trim();
    }

    private static string PickWeightedState(Random random)
    {
        var totalWeight = SudanStates.Sum(x => x.Weight);
        var roll = random.Next(1, totalWeight + 1);
        var cumulative = 0;

        foreach (var (state, weight) in SudanStates)
        {
            cumulative += weight;
            if (roll <= cumulative)
            {
                return state;
            }
        }

        return SudanStates[0].State;
    }

    private static string BuildComplaintDescription(string state, string platform, string? existingDescription = null)
    {
        var suffix = string.IsNullOrWhiteSpace(existingDescription)
            ? "Historical seed record for realistic operational load."
            : existingDescription.Trim();

        return $"[State:{state}][Platform:{platform}] {suffix}";
    }

    private static string GenerateSudanLikePhone(Random random)
    {
        var local = random.Next(10000000, 99999999);
        return $"+2499{local}";
    }

    private static string GenerateIp(Random random) =>
        $"{random.Next(2, 223)}.{random.Next(0, 256)}.{random.Next(0, 256)}.{random.Next(1, 255)}";

    private static DateTime ClampToNow(DateTime value, DateTime nowUtc)
    {
        if (value >= nowUtc)
        {
            return nowUtc.AddMinutes(-1);
        }

        return value;
    }

    private static T Pick<T>(IReadOnlyList<T> values, Random random) =>
        values[random.Next(values.Count)];
}
