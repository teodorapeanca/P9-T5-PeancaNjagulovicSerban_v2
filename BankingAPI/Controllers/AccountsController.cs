using BankingAPI.Data;
using BankingAPI.DTOs;
using BankingAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;

namespace BankingAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly BankingDbContext _context;

        public AccountsController(BankingDbContext context)
        {
            _context = context;
        }

        private async Task<User?> GetValidatedActiveUserAsync()
        {
            var userIdClaim = User.FindFirst("userId");
            if (userIdClaim == null)
                return null;

            if (!long.TryParse(userIdClaim.Value, out var userId))
                return null;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null || !user.IsActive)
                return null;

            if (!user.LastActivityAt.HasValue ||
                user.LastActivityAt.Value.AddMinutes(10) < DateTime.UtcNow)
            {
                user.LastActivityAt = null;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return null;
            }

            user.LastActivityAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return user;
        }

        private async Task AddAuditLogAsync(long? userId, string action, string entityType, string entityId)
        {
            IPAddress? ipAddress = HttpContext.Connection.RemoteIpAddress;

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

        private async Task CreateAccountStatusNotificationAsync(long userId, string title, string message)
        {
            var owner = await _context.Users.FindAsync(userId);
            if (owner == null || !owner.NotificationsEnabled)
                return;

            _context.Notifications.Add(new Notification
            {
                NotificationUid = Guid.NewGuid(),
                UserId = userId,
                Type = "ACCOUNT_BLOCKED",
                Title = title,
                Message = message,
                Channel = "IN_APP",
                IsRead = false,
                IsSent = true,
                RelatedTransactionId = null,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        private async Task<string> GenerateUniqueIbanAsync()
        {
            var random = new Random();
            string iban;

            do
            {
                var digits = random.NextInt64(1000000000000000, 9999999999999999).ToString();
                iban = $"RO49DIBT{digits}";
            }
            while (await _context.Accounts.AnyAsync(a => a.Iban == iban));

            return iban;
        }

        [Authorize(Roles = "Administrator")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Account>>> GetAccounts()
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            var accounts = await _context.Accounts
                .OrderBy(a => a.AccountId)
                .ToListAsync();

            return Ok(accounts);
        }

        [HttpGet("my")]
        public async Task<ActionResult<IEnumerable<Account>>> GetMyAccounts()
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            var accounts = await _context.Accounts
                .Where(a => a.UserId == currentUser.UserId)
                .OrderBy(a => a.AccountId)
                .ToListAsync();

            return Ok(accounts);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Account>> GetAccount(long id)
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
                return NotFound("Contul nu a fost găsit.");

            var isAdmin = User.IsInRole("Administrator");
            if (!isAdmin && account.UserId != currentUser.UserId)
                return Forbid();

            return Ok(account);
        }

        [HttpPost]
        public async Task<ActionResult> CreateAccount([FromBody] CreateAccountRequest request)
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var allowedCurrencies = new[] { "RON", "EUR", "USD", "GBP" };

            if (string.IsNullOrWhiteSpace(request.Currency))
                return BadRequest("Moneda este obligatorie.");

            var currency = request.Currency.Trim().ToUpper();

            if (!allowedCurrencies.Contains(currency))
                return BadRequest("Moneda trebuie să fie RON, EUR, USD sau GBP.");

            if (request.Balance < 0)
                return BadRequest("Soldul inițial nu poate fi negativ.");

            if (request.DailyLimit < 0)
                return BadRequest("Limita zilnică nu poate fi negativă.");

            var account = new Account
            {
                UserId = currentUser.UserId,
                Currency = currency,
                Balance = request.Balance,
                DailyLimit = request.DailyLimit,
                AccountUid = Guid.NewGuid(),
                Iban = await GenerateUniqueIbanAsync(),
                Status = "PENDING_APPROVAL",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            await AddAuditLogAsync(
                currentUser.UserId,
                "CREATE_ACCOUNT",
                "ACCOUNT",
                account.AccountId.ToString()
            );

            return CreatedAtAction(nameof(GetAccount), new { id = account.AccountId }, new
            {
                message = "Contul a fost creat și așteaptă aprobarea administratorului.",
                accountId = account.AccountId,
                iban = account.Iban,
                currency = account.Currency,
                balance = account.Balance,
                dailyLimit = account.DailyLimit,
                status = account.Status,
                createdAt = account.CreatedAt
            });
        }

        [Authorize(Roles = "Administrator")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingAccounts()
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            var accounts = await _context.Accounts
                .Where(a => a.Status == "PENDING_APPROVAL")
                .OrderBy(a => a.AccountId)
                .ToListAsync();

            return Ok(accounts);
        }

        [Authorize(Roles = "Administrator")]
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> ApproveAccount(long id)
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
                return NotFound("Contul nu a fost găsit.");

            var oldStatus = account.Status;
            account.Status = "APPROVED";
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await AddAuditLogAsync(
                currentUser.UserId,
                "APPROVE_ACCOUNT",
                "ACCOUNT",
                account.AccountId.ToString()
            );

            return Ok(new
            {
                message = "Cont aprobat cu succes.",
                accountId = account.AccountId,
                oldStatus,
                newStatus = account.Status,
                updatedAt = account.UpdatedAt
            });
        }

        [Authorize(Roles = "Administrator")]
        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> DeactivateAccount(long id)
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
                return NotFound("Contul nu a fost găsit.");

            if (account.Status == "CLOSED")
                return BadRequest("Un cont închis nu poate fi dezactivat.");

            var oldStatus = account.Status;
            account.Status = "DEACTIVATED";
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await AddAuditLogAsync(
                currentUser.UserId,
                "DEACTIVATE_ACCOUNT",
                "ACCOUNT",
                account.AccountId.ToString()
            );

            await CreateAccountStatusNotificationAsync(
                account.UserId,
                "Cont dezactivat",
                $"Contul cu ID {account.AccountId} a fost dezactivat de administrator."
            );

            return Ok(new
            {
                message = "Cont dezactivat cu succes.",
                accountId = account.AccountId,
                oldStatus,
                newStatus = account.Status,
                updatedAt = account.UpdatedAt
            });
        }

        [Authorize(Roles = "Administrator")]
        [HttpPut("{id}/reactivate")]
        public async Task<IActionResult> ReactivateAccount(long id)
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
                return NotFound("Contul nu a fost găsit.");

            if (account.Status == "CLOSED")
                return BadRequest("Un cont închis nu poate fi reactivat.");

            var oldStatus = account.Status;
            account.Status = "REACTIVATED";
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await AddAuditLogAsync(
                currentUser.UserId,
                "REACTIVATE_ACCOUNT",
                "ACCOUNT",
                account.AccountId.ToString()
            );

            return Ok(new
            {
                message = "Cont reactivat cu succes.",
                accountId = account.AccountId,
                oldStatus,
                newStatus = account.Status,
                updatedAt = account.UpdatedAt
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccount(long id)
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
                return NotFound("Contul nu a fost găsit.");

            var isAdmin = User.IsInRole("Administrator");
            if (!isAdmin && account.UserId != currentUser.UserId)
                return Forbid();

            if (account.Balance != 0)
                return BadRequest("Contul nu poate fi închis dacă soldul este diferit de 0.");

            var oldStatus = account.Status;
            account.Status = "CLOSED";
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await AddAuditLogAsync(
                currentUser.UserId,
                "CLOSE_ACCOUNT",
                "ACCOUNT",
                account.AccountId.ToString()
            );

            await CreateAccountStatusNotificationAsync(
                account.UserId,
                "Cont închis",
                $"Contul cu ID {account.AccountId} a fost închis."
            );

            return Ok(new
            {
                message = "Contul a fost închis cu succes.",
                accountId = account.AccountId,
                oldStatus,
                newStatus = account.Status,
                updatedAt = account.UpdatedAt
            });
        }
    }
}