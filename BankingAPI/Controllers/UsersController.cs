using System.Text;
using BankingAPI.Data;
using BankingAPI.DTOs;
using BankingAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankingAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly BankingDbContext _context;

        public UsersController(BankingDbContext context)
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

        [Authorize(Roles = "Administrator")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetUsers()
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            var users = await _context.Users
                .OrderBy(u => u.UserId)
                .Select(u => new
                {
                    u.UserId,
                    u.ClientIdentifier,
                    u.Email,
                    u.Phone,
                    u.FirstName,
                    u.LastName,
                    u.Address,
                    u.DateOfBirth,
                    u.EmailConfirmed,
                    u.IsActive,
                    u.NotificationLevel,
                    u.NotificationsEnabled,
                    u.CreatedAt,
                    u.UpdatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetUser(long id)
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            var isAdmin = User.IsInRole("Administrator");

            if (!isAdmin && currentUser.UserId != id)
                return Forbid();

            var user = await _context.Users
                .Where(u => u.UserId == id)
                .Select(u => new
                {
                    u.UserId,
                    u.ClientIdentifier,
                    u.Email,
                    u.Phone,
                    u.FirstName,
                    u.LastName,
                    u.Address,
                    u.DateOfBirth,
                    u.EmailConfirmed,
                    u.IsActive,
                    u.NotificationLevel,
                    u.NotificationsEnabled,
                    u.CreatedAt,
                    u.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound("Utilizatorul nu a fost găsit.");

            return Ok(user);
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            return Ok(new
            {
                currentUser.UserId,
                currentUser.ClientIdentifier,
                currentUser.Email,
                currentUser.Phone,
                currentUser.FirstName,
                currentUser.LastName,
                currentUser.Address,
                currentUser.DateOfBirth,
                currentUser.EmailConfirmed,
                currentUser.IsActive,
                currentUser.NotificationLevel,
                currentUser.NotificationsEnabled,
                currentUser.CreatedAt,
                currentUser.UpdatedAt
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(long id, [FromBody] UpdateUserRequest request)
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var isAdmin = User.IsInRole("Administrator");

            if (!isAdmin && currentUser.UserId != id)
                return Forbid();

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("Utilizatorul nu a fost găsit.");

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Phone = request.Phone;
            user.Address = request.Address;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await AddAuditLogAsync(currentUser.UserId, "UPDATE_USER_PROFILE", "USER", user.UserId.ToString());

            return Ok(new
            {
                message = "Datele personale au fost actualizate cu succes.",
                userId = user.UserId,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                phone = user.Phone,
                address = user.Address,
                notificationLevel = user.NotificationLevel,
                notificationsEnabled = user.NotificationsEnabled,
                updatedAt = user.UpdatedAt
            });
        }

        [HttpPut("notification-level")]
        public async Task<IActionResult> UpdateNotificationLevel([FromBody] UpdateNotificationLevelRequest request)
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var allowedLevels = new[] { "Low", "Medium", "High" };
            var level = request.NotificationLevel.Trim();

            if (!allowedLevels.Contains(level))
                return BadRequest("Nivelul de notificare trebuie să fie Low, Medium sau High.");

            currentUser.NotificationLevel = level;
            currentUser.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await AddAuditLogAsync(currentUser.UserId, "UPDATE_NOTIFICATION_LEVEL", "USER", currentUser.UserId.ToString());

            return Ok(new
            {
                message = "Nivelul de notificare a fost actualizat cu succes.",
                userId = currentUser.UserId,
                notificationLevel = currentUser.NotificationLevel,
                updatedAt = currentUser.UpdatedAt
            });
        }

        [HttpPut("notifications/toggle")]
        public async Task<IActionResult> ToggleNotifications([FromBody] ToggleNotificationsRequest request)
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            currentUser.NotificationsEnabled = request.Enabled;
            currentUser.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await AddAuditLogAsync(currentUser.UserId, "TOGGLE_NOTIFICATIONS", "USER", currentUser.UserId.ToString());

            return Ok(new
            {
                message = "Preferința pentru notificări a fost actualizată cu succes.",
                userId = currentUser.UserId,
                notificationsEnabled = currentUser.NotificationsEnabled,
                updatedAt = currentUser.UpdatedAt
            });
        }

        [Authorize(Roles = "Administrator")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(long id)
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("Utilizatorul nu a fost găsit.");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            await AddAuditLogAsync(currentUser.UserId, "DELETE_USER", "USER", id.ToString());

            return NoContent();
        }
    }
}