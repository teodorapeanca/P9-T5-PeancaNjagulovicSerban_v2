using System.Text;
using BankingAPI.Data;
using BankingAPI.DTOs;
using BankingAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankingAPI.Controllers
{
    [Authorize(Roles = "Administrator")]
    [ApiController]
    [Route("api/[controller]")]
    public class AuditController : ControllerBase
    {
        private readonly BankingDbContext _context;
        private static int AuditRetentionDays = 30;

        public AuditController(BankingDbContext context)
        {
            _context = context;
        }

        private async Task<BankingAPI.Models.User?> GetValidatedActiveUserAsync()
        {
            var userIdClaim = User.FindFirst("userId");
            if (userIdClaim == null)
                return null;

            if (!long.TryParse(userIdClaim.Value, out var userId))
                return null;

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (currentUser == null || !currentUser.IsActive)
                return null;

            if (!currentUser.LastActivityAt.HasValue ||
                currentUser.LastActivityAt.Value.AddMinutes(10) < DateTime.UtcNow)
            {
                currentUser.LastActivityAt = null;
                currentUser.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return null;
            }

            currentUser.LastActivityAt = DateTime.UtcNow;
            currentUser.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return currentUser;
        }

        private static DateTime EnsureUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }

        private IQueryable<AuditLog> ApplyFilters(IQueryable<AuditLog> query, AuditLogFilterRequest request)
        {
            if (request.DateFrom.HasValue)
            {
                var dateFromUtc = EnsureUtc(request.DateFrom.Value);
                query = query.Where(a => a.CreatedAt >= dateFromUtc);
            }

            if (request.DateTo.HasValue)
            {
                var dateToUtc = EnsureUtc(request.DateTo.Value);
                query = query.Where(a => a.CreatedAt <= dateToUtc);
            }

            if (!string.IsNullOrWhiteSpace(request.Action))
                query = query.Where(a => a.Action == request.Action.Trim().ToUpper());

            if (!string.IsNullOrWhiteSpace(request.EntityType))
                query = query.Where(a => a.EntityType == request.EntityType.Trim().ToUpper());

            if (request.UserId.HasValue)
                query = query.Where(a => a.UserId == request.UserId.Value);

            return query;
        }

        private async Task AddAuditLogAsync(long? userId, string action, string entityType, string entityId)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress;
            var hashSource = $"{userId}-{action}-{entityType}-{entityId}-{DateTime.UtcNow:O}";
            var hash = Convert.ToBase64String(Encoding.UTF8.GetBytes(hashSource));

            _context.AuditLogs.Add(new AuditLog
            {
                AuditUid = Guid.NewGuid(),
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                IpAddress = ipAddress,
                HashValue = hash,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        [HttpGet]
        public async Task<IActionResult> GetLogs()
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            var cutoff = DateTime.UtcNow.AddDays(-AuditRetentionDays);

            var logs = await _context.AuditLogs
                .Where(a => a.CreatedAt >= cutoff)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    a.AuditId,
                    a.AuditUid,
                    a.UserId,
                    a.Action,
                    a.EntityType,
                    a.EntityId,
                    IpAddress = a.IpAddress != null ? a.IpAddress.ToString() : null,
                    a.HashValue,
                    a.CreatedAt
                })
                .ToListAsync();

            return Ok(logs);
        }

        [HttpPost("filter")]
        public async Task<IActionResult> FilterLogs([FromBody] AuditLogFilterRequest request)
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var cutoff = DateTime.UtcNow.AddDays(-AuditRetentionDays);

            var query = _context.AuditLogs
                .Where(a => a.CreatedAt >= cutoff)
                .AsQueryable();

            query = ApplyFilters(query, request);

            var logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    a.AuditId,
                    a.AuditUid,
                    a.UserId,
                    a.Action,
                    a.EntityType,
                    a.EntityId,
                    IpAddress = a.IpAddress != null ? a.IpAddress.ToString() : null,
                    a.HashValue,
                    a.CreatedAt
                })
                .ToListAsync();

            await AddAuditLogAsync(currentUser.UserId, "FILTER_AUDIT_LOGS", "AUDIT", currentUser.UserId.ToString());

            return Ok(logs);
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportLogs()
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            var cutoff = DateTime.UtcNow.AddDays(-AuditRetentionDays);

            var logs = await _context.AuditLogs
                .Where(a => a.CreatedAt >= cutoff)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("AuditId;AuditUid;UserId;Action;EntityType;EntityId;IpAddress;HashValue;CreatedAt");

            foreach (var log in logs)
            {
                var entityId = (log.EntityId ?? string.Empty)
                    .Replace(";", ",")
                    .Replace(Environment.NewLine, " ")
                    .Replace("\n", " ")
                    .Replace("\r", " ");

                var ipAddress = log.IpAddress?.ToString() ?? string.Empty;
                var hashValue = (log.HashValue ?? string.Empty).Replace(";", ",");

                sb.AppendLine(
                    $"{log.AuditId};" +
                    $"{log.AuditUid};" +
                    $"{log.UserId};" +
                    $"{log.Action};" +
                    $"{log.EntityType};" +
                    $"{entityId};" +
                    $"{ipAddress};" +
                    $"{hashValue};" +
                    $"{log.CreatedAt:yyyy-MM-dd HH:mm:ss}"
                );
            }

            await AddAuditLogAsync(currentUser.UserId, "EXPORT_AUDIT_LOGS", "AUDIT", currentUser.UserId.ToString());

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", "audit_logs.csv");
        }

        [HttpGet("retention-policy")]
        public async Task<IActionResult> GetAuditRetentionPolicy()
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            return Ok(new
            {
                minimumRetentionDays = 30,
                currentRetentionDays = AuditRetentionDays
            });
        }

        [HttpPut("retention-policy/{days}")]
        public async Task<IActionResult> SetAuditRetentionPolicy(int days)
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            if (days < 30)
                return BadRequest("Politica de retenție pentru audit nu poate fi mai mică de 30 zile.");

            AuditRetentionDays = days;

            await AddAuditLogAsync(currentUser.UserId, "SET_AUDIT_RETENTION_POLICY", "AUDIT", days.ToString());

            return Ok(new
            {
                message = "Politica de retenție pentru audit a fost actualizată.",
                currentRetentionDays = AuditRetentionDays
            });
        }

        [HttpGet("{id}/verify-hash")]
        public async Task<IActionResult> VerifyHash(long id)
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            var log = await _context.AuditLogs.FindAsync(id);
            if (log == null)
                return NotFound("Audit log-ul nu a fost găsit.");

            var isHashPresent = !string.IsNullOrWhiteSpace(log.HashValue);

            await AddAuditLogAsync(currentUser.UserId, "VERIFY_AUDIT_HASH", "AUDIT", id.ToString());

            return Ok(new
            {
                auditId = log.AuditId,
                hashPresent = isHashPresent,
                hashValue = log.HashValue
            });
        }
    }
}