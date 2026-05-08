using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BankingAPI.Data;
using BankingAPI.DTOs;
using BankingAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BankingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly BankingDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(BankingDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
            if (emailExists)
                return BadRequest("Există deja un utilizator cu acest email.");

            if (request.DateOfBirth > DateTime.UtcNow.Date)
                return BadRequest("Data nașterii nu poate fi în viitor.");

            var age = DateTime.UtcNow.Year - request.DateOfBirth.Year;

            if (request.DateOfBirth.Date > DateTime.UtcNow.AddYears(-age).Date)
                age--;

            if (age < 18)
                return BadRequest("Utilizatorul trebuie să aibă minimum 18 ani pentru a se înregistra.");

            if (!request.AcceptedTerms)
                return BadRequest("Trebuie să accepți termenii și condițiile.");

            var now = DateTime.UtcNow;
            var clientIdentifier = $"CLT-{DateTime.UtcNow:yyyyMMddHHmmssfff}";

            var user = new User
            {
                UserUid = Guid.NewGuid(),
                ClientIdentifier = clientIdentifier,
                Email = request.Email,
                Phone = request.Phone,
                Address = request.Address,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                DateOfBirth = DateTime.SpecifyKind(request.DateOfBirth, DateTimeKind.Utc),
                AcceptedTerms = request.AcceptedTerms,
                FailedLoginAttempts = 0,
                LockoutEnd = null,
                CurrentSessionId = null,

                EmailConfirmed = true,
                EmailConfirmationToken = null,

                PasswordResetToken = null,
                PasswordResetExpiresAt = null,
                TwoFactorCode = null,
                TwoFactorExpiresAt = null,
                LastActivityAt = null,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var clientRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == "Client");

            if (clientRole != null)
            {
                var userRole = new UserRole
                {
                    UserId = user.UserId,
                    RoleId = clientRole.RoleId
                };

                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();
            }

            _context.Notifications.Add(new Notification
            {
                NotificationUid = Guid.NewGuid(),
                UserId = user.UserId,
                Type = "ACCOUNT_CREATED",
                Title = "Cont creat cu succes",
                Message = $"Contul tău de utilizator a fost creat cu succes. Identificator client: {user.ClientIdentifier}.",
                Channel = "EMAIL",
                IsRead = false,
                IsSent = true,
                RelatedTransactionId = null,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Utilizator înregistrat cu succes. A fost generată o notificare de confirmare.",
                userId = user.UserId,
                clientIdentifier = user.ClientIdentifier,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                notification = new
                {
                    type = "ACCOUNT_CREATED",
                    channel = "EMAIL",
                    title = "Cont creat cu succes"
                }
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
                return NotFound("Nu există utilizator cu acest email.");

            var resetToken = Guid.NewGuid().ToString();

            user.PasswordResetToken = resetToken;
            user.PasswordResetExpiresAt = DateTime.UtcNow.AddMinutes(15);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Token-ul de resetare a fost generat.",
                clientIdentifier = user.ClientIdentifier,
                passwordResetToken = resetToken,
                expiresAt = user.PasswordResetExpiresAt
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.ClientIdentifier == request.ClientIdentifier);

            if (user == null)
                return NotFound("Utilizatorul nu a fost găsit.");

            if (string.IsNullOrWhiteSpace(user.PasswordResetToken) ||
                user.PasswordResetToken != request.Token)
            {
                return BadRequest("Token de resetare invalid.");
            }

            if (!user.PasswordResetExpiresAt.HasValue ||
                user.PasswordResetExpiresAt.Value < DateTime.UtcNow)
            {
                return BadRequest("Token-ul de resetare a expirat.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetExpiresAt = null;
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Parola a fost resetată cu succes."
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.ClientIdentifier == request.ClientIdentifier);

            if (user == null)
                return Unauthorized("Identificator sau parolă incorecte.");

            if (!user.IsActive)
                return Unauthorized("Contul este inactiv.");

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                return Unauthorized(
                    $"Contul este blocat temporar până la {user.LockoutEnd.Value:yyyy-MM-dd HH:mm:ss} UTC.");
            }

            var age = DateTime.UtcNow.Year - user.DateOfBirth.Year;
            if (user.DateOfBirth.Date > DateTime.UtcNow.AddYears(-age))
                age--;

            if (age < 18)
            {
                return Unauthorized("Autentificarea este permisă doar utilizatorilor cu vârsta de minimum 18 ani.");
            }

            if (string.IsNullOrWhiteSpace(user.PasswordHash) ||
                !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                user.FailedLoginAttempts += 1;

                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(10);
                    user.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    return Unauthorized("Contul a fost blocat temporar după 5 încercări eșuate.");
                }

                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Unauthorized(
                    $"Identificator sau parolă incorecte. Încercări eșuate: {user.FailedLoginAttempts}/5.");
            }

            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            user.UpdatedAt = DateTime.UtcNow;

            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == user.UserId)
                .Join(_context.Roles,
                    ur => ur.RoleId,
                    r => r.RoleId,
                    (ur, r) => r.RoleName)
                .ToListAsync();

            var isAdministrator = userRoles.Contains("Administrator");
            string? sessionId = null;

            if (isAdministrator)
            {
                sessionId = Guid.NewGuid().ToString();
                user.CurrentSessionId = sessionId;
            }

            var twoFactorCode = new Random().Next(100000, 999999).ToString();
            user.TwoFactorCode = twoFactorCode;
            user.TwoFactorExpiresAt = DateTime.UtcNow.AddMinutes(5);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Credențiale valide. Este necesară verificarea 2FA.",
                clientIdentifier = user.ClientIdentifier,
                roles = userRoles,
                twoFactorCode = twoFactorCode,
                twoFactorExpiresAt = user.TwoFactorExpiresAt,
                sessionId = sessionId
            });
        }

        [HttpPost("verify-2fa")]
        public async Task<IActionResult> VerifyTwoFactor([FromBody] VerifyTwoFactorRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.ClientIdentifier == request.ClientIdentifier);

            if (user == null)
                return NotFound("Utilizatorul nu a fost găsit.");

            if (string.IsNullOrWhiteSpace(user.TwoFactorCode) ||
                user.TwoFactorCode != request.Code)
            {
                return BadRequest("Cod 2FA invalid.");
            }

            if (!user.TwoFactorExpiresAt.HasValue ||
                user.TwoFactorExpiresAt.Value < DateTime.UtcNow)
            {
                return BadRequest("Codul 2FA a expirat.");
            }

            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == user.UserId)
                .Join(_context.Roles,
                    ur => ur.RoleId,
                    r => r.RoleId,
                    (ur, r) => r.RoleName)
                .ToListAsync();

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("userId", user.UserId.ToString()),
                new Claim("clientIdentifier", user.ClientIdentifier ?? ""),
                new Claim("email", user.Email ?? ""),
                new Claim("fullName", $"{user.FirstName} {user.LastName}".Trim())
            };

            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            if (!string.IsNullOrWhiteSpace(user.CurrentSessionId))
            {
                claims.Add(new Claim("sessionId", user.CurrentSessionId));
            }

            user.TwoFactorCode = null;
            user.TwoFactorExpiresAt = null;
            user.LastActivityAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                message = "Autentificare finalizată cu succes.",
                token = jwt,
                userId = user.UserId,
                clientIdentifier = user.ClientIdentifier,
                email = user.Email,
                roles = userRoles
            });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst("userId");
            if (userIdClaim == null)
                return Unauthorized("Utilizator neautentificat.");

            if (!long.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized("Token invalid.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return NotFound("Utilizatorul nu a fost găsit.");

            if (!user.LastActivityAt.HasValue ||
                user.LastActivityAt.Value.AddMinutes(10) < DateTime.UtcNow)
            {
                user.LastActivityAt = null;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");
            }

            if (string.IsNullOrWhiteSpace(user.PasswordHash) ||
                !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return BadRequest("Parola curentă este incorectă.");
            }

            if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash))
            {
                return BadRequest("Noua parolă trebuie să fie diferită de parola curentă.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.LastActivityAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Parola a fost schimbată cu succes."
            });
        }
    }
}