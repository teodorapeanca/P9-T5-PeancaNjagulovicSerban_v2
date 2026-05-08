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
    public class NotificationsController : ControllerBase
    {
        private readonly BankingDbContext _context;

        public NotificationsController(BankingDbContext context)
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

        [HttpGet("my")]
        public async Task<IActionResult> GetMyNotifications()
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            var notifications = await _context.Notifications
                .Where(n => n.UserId == currentUser.UserId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return Ok(notifications);
        }

        [HttpGet("my/unread")]
        public async Task<IActionResult> GetMyUnreadNotifications()
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            var notifications = await _context.Notifications
                .Where(n => n.UserId == currentUser.UserId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return Ok(notifications);
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == currentUser.UserId);

            if (notification == null)
                return NotFound("Notificarea nu a fost găsită.");

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notificarea a fost marcată ca citită." });
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost("maintenance")]
        public async Task<IActionResult> SendMaintenanceNotification([FromBody] MaintenanceNotificationRequest request)
        {
            var admin = await GetValidatedActiveUserAsync();
            if (admin == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var users = await _context.Users
                .Where(u => u.IsActive && u.NotificationsEnabled)
                .ToListAsync();

            var notifications = users.Select(u => new Notification
            {
                NotificationUid = Guid.NewGuid(),
                UserId = u.UserId,
                Type = "MAINTENANCE",
                Title = request.Title,
                Message = request.Message,
                Channel = "IN_APP",
                IsRead = false,
                IsSent = true,
                RelatedTransactionId = null,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Notificările de mentenanță au fost trimise.",
                count = notifications.Count
            });
        }
    }
}