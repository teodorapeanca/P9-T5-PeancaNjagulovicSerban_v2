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
    public class TransactionsController : ControllerBase
    {
        private readonly BankingDbContext _context;

        private static readonly Dictionary<string, decimal> ExchangeRatesToRon = new()
        {
            { "RON", 1.00m },
            { "EUR", 4.97m },
            { "USD", 4.58m },
            { "GBP", 5.82m }
        };

        private static int RetentionDays = 30;

        public TransactionsController(BankingDbContext context)
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

        private static string NormalizeCurrency(string currency) => currency.Trim().ToUpper();

        private static bool IsSupportedCurrency(string currency) => ExchangeRatesToRon.ContainsKey(currency);

        private static bool IsActiveAccount(Account account)
        {
            return account.Status == "APPROVED" || account.Status == "REACTIVATED";
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

        private static decimal ConvertCurrency(decimal amount, string fromCurrency, string toCurrency)
        {
            var from = NormalizeCurrency(fromCurrency);
            var to = NormalizeCurrency(toCurrency);

            if (!IsSupportedCurrency(from) || !IsSupportedCurrency(to))
                throw new InvalidOperationException("Unsupported currency.");

            if (from == to)
                return Math.Round(amount, 2);

            var amountInRon = amount * ExchangeRatesToRon[from];
            var convertedAmount = amountInRon / ExchangeRatesToRon[to];

            return Math.Round(convertedAmount, 2);
        }

        private static decimal CalculateCommission(string transactionType, decimal baseAmount, bool isCrossCurrency = false)
        {
            return transactionType switch
            {
                "DEPOSIT" => 0m,
                "WITHDRAWAL" => Math.Round(baseAmount * 0.01m, 2),
                "TRANSFER" => isCrossCurrency ? Math.Round(baseAmount * 0.01m, 2) : Math.Round(baseAmount * 0.005m, 2),
                "SERVICE_PAYMENT" => Math.Round(baseAmount * 0.005m, 2),
                _ => 0m
            };
        }

        private static bool RequiresAmlReview(decimal amount, string currency)
        {
            var normalized = NormalizeCurrency(currency);
            if (!IsSupportedCurrency(normalized))
                return false;

            var amountRon = ConvertCurrency(amount, normalized, "RON");
            return amountRon >= 10000m;
        }

        private IQueryable<Transaction> ApplyHistoryFilters(IQueryable<Transaction> query, TransactionHistoryFilterRequest request)
        {
            if (request.DateFrom.HasValue)
            {
                var dateFromUtc = EnsureUtc(request.DateFrom.Value);
                query = query.Where(t => t.CreatedAt >= dateFromUtc);
            }

            if (request.DateTo.HasValue)
            {
                var dateToUtc = EnsureUtc(request.DateTo.Value);
                query = query.Where(t => t.CreatedAt <= dateToUtc);
            }

            if (request.MinAmount.HasValue)
                query = query.Where(t => t.Amount >= request.MinAmount.Value);

            if (request.MaxAmount.HasValue)
                query = query.Where(t => t.Amount <= request.MaxAmount.Value);

            if (!string.IsNullOrWhiteSpace(request.Type))
                query = query.Where(t => t.Type == request.Type.Trim().ToUpper());

            if (!string.IsNullOrWhiteSpace(request.Status))
                query = query.Where(t => t.Status == request.Status.Trim().ToUpper());

            if (!string.IsNullOrWhiteSpace(request.Currency))
            {
                var currency = NormalizeCurrency(request.Currency);
                query = query.Where(t => t.Currency == currency);
            }

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

        private async Task CreateNotificationAsync(long userId, string type, string title, string message, long? relatedTransactionId = null, string channel = "IN_APP")
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.NotificationsEnabled)
                return;

            _context.Notifications.Add(new Notification
            {
                NotificationUid = Guid.NewGuid(),
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                Channel = channel,
                IsRead = false,
                IsSent = true,
                RelatedTransactionId = relatedTransactionId,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        private async Task CreateTransactionNotificationsAsync(long userId, Transaction transaction, string successMessage)
        {
            await CreateNotificationAsync(
                userId,
                "TRANSACTION_CONFIRMATION",
                "Confirmare tranzacție",
                successMessage,
                transaction.TransactionId,
                "EMAIL"
            );

            if (transaction.AmlFlag)
            {
                await CreateNotificationAsync(
                    userId,
                    "AML_ALERT",
                    "Tranzacție marcată AML",
                    $"Tranzacția #{transaction.TransactionId} a fost marcată pentru verificare AML.",
                    transaction.TransactionId,
                    "IN_APP"
                );
            }
        }

        [Authorize(Roles = "Administrator")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactions()
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);

            var transactions = await _context.Transactions
                .Where(t => t.CreatedAt >= cutoff)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return Ok(transactions);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Transaction>> GetTransaction(long id)
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null)
                return NotFound("Tranzacția nu a fost găsită.");

            var isAdmin = User.IsInRole("Administrator");
            if (!isAdmin && transaction.InitiatedByUserId != currentUser.UserId)
                return Forbid();

            return Ok(transaction);
        }

        [HttpGet("{id}/details")]
        public async Task<IActionResult> GetTransactionDetails(long id)
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null)
                return NotFound("Tranzacția nu a fost găsită.");

            var isAdmin = User.IsInRole("Administrator");
            if (!isAdmin && transaction.InitiatedByUserId != currentUser.UserId)
                return Forbid();

            var fromAccount = transaction.FromAccountId.HasValue
                ? await _context.Accounts.FindAsync(transaction.FromAccountId.Value)
                : null;

            var toAccount = transaction.ToAccountId.HasValue
                ? await _context.Accounts.FindAsync(transaction.ToAccountId.Value)
                : null;

            var provider = transaction.ProviderId.HasValue
                ? await _context.ServiceProviders.FirstOrDefaultAsync(p => p.ProviderId == transaction.ProviderId.Value)
                : null;

            return Ok(new
            {
                transaction.TransactionId,
                transaction.TransactionUid,
                transaction.InitiatedByUserId,
                transaction.FromAccountId,
                fromAccountIban = fromAccount?.Iban,
                transaction.ToAccountId,
                toAccountIban = toAccount?.Iban,
                transaction.ProviderId,
                providerName = provider?.ProviderName,
                transaction.Type,
                transaction.Amount,
                transaction.Currency,
                transaction.FeeAmount,
                transaction.Status,
                transaction.AmlFlag,
                transaction.Description,
                transaction.CreatedAt
            });
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactionsByUser(long userId)
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            var isAdmin = User.IsInRole("Administrator");
            if (!isAdmin && currentUser.UserId != userId)
                return Forbid();

            var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);

            var transactions = await _context.Transactions
                .Where(t => t.InitiatedByUserId == userId && t.CreatedAt >= cutoff)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return Ok(transactions);
        }

        [HttpGet("my-history")]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetMyTransactionHistory()
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);

            var transactions = await _context.Transactions
                .Where(t => t.InitiatedByUserId == currentUser.UserId && t.CreatedAt >= cutoff)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return Ok(transactions);
        }

        [HttpPost("my-history/filter")]
        public async Task<IActionResult> FilterMyTransactionHistory([FromBody] TransactionHistoryFilterRequest request)
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);

            var query = _context.Transactions
                .Where(t => t.InitiatedByUserId == currentUser.UserId && t.CreatedAt >= cutoff)
                .AsQueryable();

            query = ApplyHistoryFilters(query, request);

            var transactions = await query
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            await AddAuditLogAsync(currentUser.UserId, "FILTER_TRANSACTION_HISTORY", "TRANSACTION_HISTORY", currentUser.UserId.ToString());

            return Ok(transactions);
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost("user/{userId}/filter")]
        public async Task<IActionResult> FilterTransactionsByUser(long userId, [FromBody] TransactionHistoryFilterRequest request)
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);

            var query = _context.Transactions
                .Where(t => t.InitiatedByUserId == userId && t.CreatedAt >= cutoff)
                .AsQueryable();

            query = ApplyHistoryFilters(query, request);

            var transactions = await query
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            await AddAuditLogAsync(currentUser.UserId, "FILTER_USER_TRANSACTION_HISTORY", "TRANSACTION_HISTORY", userId.ToString());

            return Ok(transactions);
        }

        [HttpGet("my-history/export")]
        public async Task<IActionResult> ExportMyHistoryToExcelLikeCsv()
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);

            var transactions = await _context.Transactions
                .Where(t => t.InitiatedByUserId == currentUser.UserId && t.CreatedAt >= cutoff)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var sb = new StringBuilder();

            sb.AppendLine("TransactionId;TransactionUid;Type;Amount;Currency;FeeAmount;Status;AmlFlag;Description;CreatedAt");

            foreach (var t in transactions)
            {
                var description = (t.Description ?? string.Empty)
                    .Replace(";", ",")
                    .Replace(Environment.NewLine, " ")
                    .Replace("\n", " ")
                    .Replace("\r", " ");

                sb.AppendLine(
                    $"{t.TransactionId};" +
                    $"{t.TransactionUid};" +
                    $"{t.Type};" +
                    $"{t.Amount:0.00};" +
                    $"{t.Currency};" +
                    $"{t.FeeAmount:0.00};" +
                    $"{t.Status};" +
                    $"{t.AmlFlag};" +
                    $"{description};" +
                    $"{t.CreatedAt:yyyy-MM-dd HH:mm:ss}"
                );
            }

            await AddAuditLogAsync(currentUser.UserId, "EXPORT_TRANSACTION_HISTORY", "TRANSACTION_HISTORY", currentUser.UserId.ToString());

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"transactions_{currentUser.UserId}.csv");
        }

        [HttpGet("retention-policy")]
        public IActionResult GetRetentionPolicy()
        {
            return Ok(new
            {
                minimumRetentionDays = 30,
                currentRetentionDays = RetentionDays
            });
        }

        [Authorize(Roles = "Administrator")]
        [HttpPut("retention-policy/{days}")]
        public async Task<IActionResult> SetRetentionPolicy(int days)
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            if (days < 30)
                return BadRequest("Politica de retenție nu poate fi mai mică de 30 zile.");

            RetentionDays = days;

            await AddAuditLogAsync(currentUser.UserId, "SET_TRANSACTION_RETENTION_POLICY", "SYSTEM", days.ToString());

            return Ok(new
            {
                message = "Politica de retenție a fost actualizată cu succes.",
                currentRetentionDays = RetentionDays
            });
        }

        [Authorize(Roles = "Client,Administrator")]
        [HttpPost("repeat")]
        public async Task<IActionResult> RepeatTransaction([FromBody] RepeatTransactionRequest request)
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var original = await _context.Transactions.FindAsync(request.TransactionId);
            if (original == null)
                return NotFound("Tranzacția originală nu a fost găsită.");

            var isAdmin = User.IsInRole("Administrator");
            if (!isAdmin && original.InitiatedByUserId != currentUser.UserId)
                return Forbid();

            if (original.Type == "TRANSFER")
            {
                if (!original.FromAccountId.HasValue || !original.ToAccountId.HasValue)
                    return BadRequest("Tranzacția originală nu poate fi repetată.");

                var fromAccount = await _context.Accounts.FindAsync(original.FromAccountId.Value);
                var toAccount = await _context.Accounts.FindAsync(original.ToAccountId.Value);

                if (fromAccount == null || toAccount == null)
                    return BadRequest("Conturile tranzacției originale nu mai sunt disponibile.");

                if (!IsActiveAccount(fromAccount) || !IsActiveAccount(toAccount))
                    return BadRequest("Conturile trebuie să fie active pentru repetarea tranzacției.");

                if (!string.Equals(original.Currency, fromAccount.Currency, StringComparison.OrdinalIgnoreCase))
                    return BadRequest("Moneda tranzacției originale nu mai corespunde cu contul sursă.");

                var isCrossCurrency = !string.Equals(fromAccount.Currency, toAccount.Currency, StringComparison.OrdinalIgnoreCase);
                var feeAmount = CalculateCommission("TRANSFER", original.Amount, isCrossCurrency);
                var totalDebit = original.Amount + feeAmount;

                if (fromAccount.Balance < totalDebit)
                    return BadRequest("Fonduri insuficiente pentru repetarea tranzacției.");

                var creditedAmount = ConvertCurrency(original.Amount, fromAccount.Currency!, toAccount.Currency!);
                var amlFlag = RequiresAmlReview(original.Amount, original.Currency);

                fromAccount.Balance -= totalDebit;
                toAccount.Balance += creditedAmount;
                fromAccount.UpdatedAt = DateTime.UtcNow;
                toAccount.UpdatedAt = DateTime.UtcNow;

                var repeated = new Transaction
                {
                    TransactionUid = Guid.NewGuid(),
                    InitiatedByUserId = currentUser.UserId,
                    FromAccountId = fromAccount.AccountId,
                    ToAccountId = toAccount.AccountId,
                    ProviderId = null,
                    Type = "TRANSFER",
                    Amount = original.Amount,
                    Currency = original.Currency,
                    FeeAmount = feeAmount,
                    Status = "COMPLETED",
                    AmlFlag = amlFlag,
                    Description = $"Repeated transaction from #{original.TransactionId}",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Transactions.Add(repeated);
                await _context.SaveChangesAsync();

                await AddAuditLogAsync(currentUser.UserId, "REPEAT_TRANSACTION", "TRANSACTION", repeated.TransactionId.ToString());

                await CreateTransactionNotificationsAsync(
                    currentUser.UserId,
                    repeated,
                    $"Transferul repetat de {repeated.Amount} {repeated.Currency} a fost efectuat cu succes."
                );

                return Ok(new
                {
                    message = "Tranzacția a fost repetată cu succes.",
                    originalTransactionId = original.TransactionId,
                    newTransactionId = repeated.TransactionId
                });
            }

            if (original.Type == "SERVICE_PAYMENT")
            {
                if (!original.FromAccountId.HasValue || !original.ProviderId.HasValue)
                    return BadRequest("Tranzacția originală nu poate fi repetată.");

                var fromAccount = await _context.Accounts.FindAsync(original.FromAccountId.Value);
                var provider = await _context.ServiceProviders.FirstOrDefaultAsync(p => p.ProviderId == original.ProviderId.Value);

                if (fromAccount == null || provider == null)
                    return BadRequest("Datele tranzacției originale nu mai sunt disponibile.");

                if (!IsActiveAccount(fromAccount))
                    return BadRequest("Contul sursă nu este activ.");

                if (!provider.IsActive)
                    return BadRequest("Furnizorul este inactiv.");

                var feeAmount = CalculateCommission("SERVICE_PAYMENT", original.Amount);
                var totalDebit = original.Amount + feeAmount;

                if (fromAccount.Balance < totalDebit)
                    return BadRequest("Fonduri insuficiente pentru repetarea tranzacției.");

                var amlFlag = RequiresAmlReview(original.Amount, original.Currency);

                fromAccount.Balance -= totalDebit;
                fromAccount.UpdatedAt = DateTime.UtcNow;

                var repeated = new Transaction
                {
                    TransactionUid = Guid.NewGuid(),
                    InitiatedByUserId = currentUser.UserId,
                    FromAccountId = fromAccount.AccountId,
                    ToAccountId = null,
                    ProviderId = provider.ProviderId,
                    Type = "SERVICE_PAYMENT",
                    Amount = original.Amount,
                    Currency = original.Currency,
                    FeeAmount = feeAmount,
                    Status = "COMPLETED",
                    AmlFlag = amlFlag,
                    Description = $"Repeated transaction from #{original.TransactionId}",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Transactions.Add(repeated);
                await _context.SaveChangesAsync();

                await AddAuditLogAsync(currentUser.UserId, "REPEAT_TRANSACTION", "TRANSACTION", repeated.TransactionId.ToString());

                await CreateTransactionNotificationsAsync(
                    currentUser.UserId,
                    repeated,
                    $"Plata repetată de {repeated.Amount} {repeated.Currency} a fost efectuată cu succes."
                );

                return Ok(new
                {
                    message = "Plata a fost repetată cu succes.",
                    originalTransactionId = original.TransactionId,
                    newTransactionId = repeated.TransactionId
                });
            }

            return BadRequest("Doar tranzacțiile de tip TRANSFER și SERVICE_PAYMENT pot fi repetate.");
        }

        [Authorize(Roles = "Client,Administrator")]
        [HttpPost("deposit")]
        public async Task<IActionResult> Deposit([FromBody] DepositRequest request)
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currency = NormalizeCurrency(request.Currency);
            if (!IsSupportedCurrency(currency))
                return BadRequest("Moneda nu este suportată.");

            if (request.Amount <= 0)
                return BadRequest("Suma trebuie să fie mai mare decât 0.");

            var isAdmin = User.IsInRole("Administrator");
            if (!isAdmin && currentUser.UserId != request.InitiatedByUserId)
                return Forbid();

            var account = await _context.Accounts.FindAsync(request.AccountId);
            if (account == null)
                return NotFound("Contul nu a fost găsit.");

            if (!isAdmin && account.UserId != currentUser.UserId)
                return Forbid();

            if (!IsActiveAccount(account))
                return BadRequest("Contul nu este activ pentru tranzacții.");

            if (string.IsNullOrWhiteSpace(account.Currency) || !IsSupportedCurrency(account.Currency))
                return BadRequest("Moneda contului nu este validă.");

            var creditedAmount = ConvertCurrency(request.Amount, currency, account.Currency);
            var amlFlag = RequiresAmlReview(request.Amount, currency);

            account.Balance += creditedAmount;
            account.UpdatedAt = DateTime.UtcNow;

            var transaction = new Transaction
            {
                TransactionUid = Guid.NewGuid(),
                InitiatedByUserId = request.InitiatedByUserId,
                FromAccountId = null,
                ToAccountId = request.AccountId,
                ProviderId = null,
                Type = "DEPOSIT",
                Amount = request.Amount,
                Currency = currency,
                FeeAmount = 0,
                Status = "COMPLETED",
                AmlFlag = amlFlag,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            await AddAuditLogAsync(request.InitiatedByUserId, "DEPOSIT", "TRANSACTION", transaction.TransactionId.ToString());

            if (transaction.AmlFlag)
                await AddAuditLogAsync(request.InitiatedByUserId, "AML_FLAGGED_TRANSACTION", "TRANSACTION", transaction.TransactionId.ToString());

            await CreateTransactionNotificationsAsync(
                request.InitiatedByUserId,
                transaction,
                $"Depunerea de {transaction.Amount} {transaction.Currency} a fost efectuată cu succes."
            );

            return Ok(new
            {
                message = amlFlag ? "Depunere realizată cu succes și marcată pentru AML review." : "Depunere realizată cu succes.",
                transactionId = transaction.TransactionId,
                accountId = account.AccountId,
                amountReceived = request.Amount,
                requestCurrency = currency,
                creditedAmount,
                creditedCurrency = account.Currency,
                feeAmount = transaction.FeeAmount,
                amlFlag = transaction.AmlFlag,
                balanceAfter = account.Balance,
                createdAt = transaction.CreatedAt
            });
        }

        [Authorize(Roles = "Client,Administrator")]
        [HttpPost("withdraw")]
        public async Task<IActionResult> Withdraw([FromBody] WithdrawRequest request)
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currency = NormalizeCurrency(request.Currency);
            if (!IsSupportedCurrency(currency))
                return BadRequest("Moneda nu este suportată.");

            if (request.Amount <= 0)
                return BadRequest("Suma trebuie să fie mai mare decât 0.");

            var isAdmin = User.IsInRole("Administrator");
            if (!isAdmin && currentUser.UserId != request.InitiatedByUserId)
                return Forbid();

            var account = await _context.Accounts.FindAsync(request.AccountId);
            if (account == null)
                return NotFound("Contul nu a fost găsit.");

            if (!isAdmin && account.UserId != currentUser.UserId)
                return Forbid();

            if (!IsActiveAccount(account))
                return BadRequest("Contul nu este activ pentru tranzacții.");

            if (string.IsNullOrWhiteSpace(account.Currency) || !IsSupportedCurrency(account.Currency))
                return BadRequest("Moneda contului nu este validă.");

            var amountToDebit = ConvertCurrency(request.Amount, currency, account.Currency);
            var feeAmount = CalculateCommission("WITHDRAWAL", amountToDebit);
            var totalDebit = amountToDebit + feeAmount;

            if (account.Balance < totalDebit)
                return BadRequest("Fonduri insuficiente pentru retragere și comision.");

            var amlFlag = RequiresAmlReview(request.Amount, currency);

            account.Balance -= totalDebit;
            account.UpdatedAt = DateTime.UtcNow;

            var transaction = new Transaction
            {
                TransactionUid = Guid.NewGuid(),
                InitiatedByUserId = request.InitiatedByUserId,
                FromAccountId = request.AccountId,
                ToAccountId = null,
                ProviderId = null,
                Type = "WITHDRAWAL",
                Amount = request.Amount,
                Currency = currency,
                FeeAmount = feeAmount,
                Status = "COMPLETED",
                AmlFlag = amlFlag,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            await AddAuditLogAsync(request.InitiatedByUserId, "WITHDRAWAL", "TRANSACTION", transaction.TransactionId.ToString());

            if (transaction.AmlFlag)
                await AddAuditLogAsync(request.InitiatedByUserId, "AML_FLAGGED_TRANSACTION", "TRANSACTION", transaction.TransactionId.ToString());

            await CreateTransactionNotificationsAsync(
                request.InitiatedByUserId,
                transaction,
                $"Retragerea de {transaction.Amount} {transaction.Currency} a fost efectuată cu succes."
            );

            return Ok(new
            {
                message = amlFlag ? "Retragere realizată cu succes și marcată pentru AML review." : "Retragere realizată cu succes.",
                transactionId = transaction.TransactionId,
                accountId = account.AccountId,
                withdrawnAmount = request.Amount,
                requestCurrency = currency,
                debitedAmount = amountToDebit,
                debitedCurrency = account.Currency,
                feeAmount,
                amlFlag = transaction.AmlFlag,
                balanceAfter = account.Balance,
                createdAt = transaction.CreatedAt
            });
        }

        [Authorize(Roles = "Client,Administrator")]
        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currency = NormalizeCurrency(request.Currency);
            if (!IsSupportedCurrency(currency))
                return BadRequest("Moneda nu este suportată.");

            if (request.Amount <= 0)
                return BadRequest("Suma trebuie să fie mai mare decât 0.");

            var isAdmin = User.IsInRole("Administrator");
            if (!isAdmin && currentUser.UserId != request.InitiatedByUserId)
                return Forbid();

            var fromAccount = await _context.Accounts.FindAsync(request.FromAccountId);
            var toAccount = await _context.Accounts.FindAsync(request.ToAccountId);

            if (fromAccount == null)
                return NotFound("Contul sursă nu a fost găsit.");

            if (toAccount == null)
                return NotFound("Contul destinație nu a fost găsit.");

            if (fromAccount.AccountId == toAccount.AccountId)
                return BadRequest("Contul sursă și contul destinație nu pot fi același.");

            if (!isAdmin && fromAccount.UserId != currentUser.UserId)
                return Forbid();

            if (!IsActiveAccount(fromAccount))
                return BadRequest("Contul sursă nu este activ pentru tranzacții.");

            if (!IsActiveAccount(toAccount))
                return BadRequest("Contul destinație nu este activ pentru tranzacții.");

            if (string.IsNullOrWhiteSpace(fromAccount.Currency) || !IsSupportedCurrency(fromAccount.Currency))
                return BadRequest("Moneda contului sursă nu este validă.");

            if (string.IsNullOrWhiteSpace(toAccount.Currency) || !IsSupportedCurrency(toAccount.Currency))
                return BadRequest("Moneda contului destinație nu este validă.");

            if (!string.Equals(currency, fromAccount.Currency, StringComparison.OrdinalIgnoreCase))
                return BadRequest("Moneda transferului trebuie să corespundă cu moneda contului sursă.");

            var isCrossCurrency = !string.Equals(fromAccount.Currency, toAccount.Currency, StringComparison.OrdinalIgnoreCase);
            var feeAmount = CalculateCommission("TRANSFER", request.Amount, isCrossCurrency);
            var totalDebit = request.Amount + feeAmount;

            if (fromAccount.Balance < totalDebit)
                return BadRequest("Fonduri insuficiente pentru transfer și comision.");

            var creditedAmount = ConvertCurrency(request.Amount, fromAccount.Currency!, toAccount.Currency!);
            var amlFlag = RequiresAmlReview(request.Amount, currency);

            fromAccount.Balance -= totalDebit;
            toAccount.Balance += creditedAmount;
            fromAccount.UpdatedAt = DateTime.UtcNow;
            toAccount.UpdatedAt = DateTime.UtcNow;

            var transaction = new Transaction
            {
                TransactionUid = Guid.NewGuid(),
                InitiatedByUserId = request.InitiatedByUserId,
                FromAccountId = request.FromAccountId,
                ToAccountId = request.ToAccountId,
                ProviderId = null,
                Type = "TRANSFER",
                Amount = request.Amount,
                Currency = currency,
                FeeAmount = feeAmount,
                Status = "COMPLETED",
                AmlFlag = amlFlag,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            await AddAuditLogAsync(request.InitiatedByUserId, "TRANSFER", "TRANSACTION", transaction.TransactionId.ToString());

            if (transaction.AmlFlag)
                await AddAuditLogAsync(request.InitiatedByUserId, "AML_FLAGGED_TRANSACTION", "TRANSACTION", transaction.TransactionId.ToString());

            await CreateTransactionNotificationsAsync(
                request.InitiatedByUserId,
                transaction,
                $"Transferul de {transaction.Amount} {transaction.Currency} a fost efectuat cu succes."
            );

            return Ok(new
            {
                message = amlFlag ? "Transfer realizat cu succes și marcat pentru AML review." : "Transfer realizat cu succes.",
                transactionId = transaction.TransactionId,
                fromAccountId = fromAccount.AccountId,
                toAccountId = toAccount.AccountId,
                debitedAmount = request.Amount,
                debitedCurrency = fromAccount.Currency,
                creditedAmount,
                creditedCurrency = toAccount.Currency,
                feeAmount,
                amlFlag = transaction.AmlFlag,
                sourceBalanceAfter = fromAccount.Balance,
                destinationBalanceAfter = toAccount.Balance,
                createdAt = transaction.CreatedAt
            });
        }
        [Authorize(Roles = "Client,Administrator")]
        [HttpPost("transfer-by-iban")]
        public async Task<IActionResult> TransferByIban([FromBody] TransferByIbanRequest request)
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(request.FromIban))
                return BadRequest("IBAN-ul contului sursă este obligatoriu.");

            if (string.IsNullOrWhiteSpace(request.ToIban))
                return BadRequest("IBAN-ul contului destinație este obligatoriu.");

            if (request.FromIban == request.ToIban)
                return BadRequest("Contul sursă și contul destinație nu pot fi același.");

            var currency = NormalizeCurrency(request.Currency);
            if (!IsSupportedCurrency(currency))
                return BadRequest("Moneda nu este suportată.");

            if (request.Amount <= 0)
                return BadRequest("Suma trebuie să fie mai mare decât 0.");

            var isAdmin = User.IsInRole("Administrator");
            if (!isAdmin && currentUser.UserId != request.InitiatedByUserId)
                return Forbid();

            var fromAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Iban == request.FromIban);

            var toAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Iban == request.ToIban);

            if (fromAccount == null)
                return NotFound("Contul sursă nu a fost găsit.");

            if (toAccount == null)
                return NotFound("Contul destinație nu a fost găsit.");

            if (!isAdmin && fromAccount.UserId != currentUser.UserId)
                return Forbid();

            if (!IsActiveAccount(fromAccount))
                return BadRequest("Contul sursă nu este activ pentru tranzacții.");

            if (!IsActiveAccount(toAccount))
                return BadRequest("Contul destinație nu este activ pentru tranzacții.");

            if (string.IsNullOrWhiteSpace(fromAccount.Currency) || !IsSupportedCurrency(fromAccount.Currency))
                return BadRequest("Moneda contului sursă nu este validă.");

            if (string.IsNullOrWhiteSpace(toAccount.Currency) || !IsSupportedCurrency(toAccount.Currency))
                return BadRequest("Moneda contului destinație nu este validă.");

            if (!string.Equals(currency, fromAccount.Currency, StringComparison.OrdinalIgnoreCase))
                return BadRequest("Moneda transferului trebuie să corespundă cu moneda contului sursă.");

            var isCrossCurrency = !string.Equals(fromAccount.Currency, toAccount.Currency, StringComparison.OrdinalIgnoreCase);
            var feeAmount = CalculateCommission("TRANSFER", request.Amount, isCrossCurrency);
            var totalDebit = request.Amount + feeAmount;

            if (fromAccount.Balance < totalDebit)
                return BadRequest("Fonduri insuficiente pentru transfer și comision.");

            var creditedAmount = ConvertCurrency(request.Amount, fromAccount.Currency!, toAccount.Currency!);
            var amlFlag = RequiresAmlReview(request.Amount, currency);

            fromAccount.Balance -= totalDebit;
            toAccount.Balance += creditedAmount;

            fromAccount.UpdatedAt = DateTime.UtcNow;
            toAccount.UpdatedAt = DateTime.UtcNow;

            var transaction = new Transaction
            {
                TransactionUid = Guid.NewGuid(),
                InitiatedByUserId = request.InitiatedByUserId,
                FromAccountId = fromAccount.AccountId,
                ToAccountId = toAccount.AccountId,
                ProviderId = null,
                Type = "TRANSFER",
                Amount = request.Amount,
                Currency = currency,
                FeeAmount = feeAmount,
                Status = "COMPLETED",
                AmlFlag = amlFlag,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            await AddAuditLogAsync(request.InitiatedByUserId, "TRANSFER_BY_IBAN", "TRANSACTION", transaction.TransactionId.ToString());

            if (transaction.AmlFlag)
                await AddAuditLogAsync(request.InitiatedByUserId, "AML_FLAGGED_TRANSACTION", "TRANSACTION", transaction.TransactionId.ToString());

            await CreateTransactionNotificationsAsync(
                request.InitiatedByUserId,
                transaction,
                $"Transferul către IBAN {toAccount.Iban} în valoare de {transaction.Amount} {transaction.Currency} a fost efectuat cu succes."
            );

            return Ok(new
            {
                message = amlFlag ? "Transfer realizat cu succes și marcat pentru AML review." : "Transfer realizat cu succes.",
                transactionId = transaction.TransactionId,
                fromIban = fromAccount.Iban,
                toIban = toAccount.Iban,
                debitedAmount = request.Amount,
                debitedCurrency = fromAccount.Currency,
                creditedAmount,
                creditedCurrency = toAccount.Currency,
                feeAmount,
                amlFlag = transaction.AmlFlag,
                sourceBalanceAfter = fromAccount.Balance,
                destinationBalanceAfter = toAccount.Balance,
                createdAt = transaction.CreatedAt
            });
        }
        [Authorize(Roles = "Client,Administrator")]
        [HttpPost("service-payment")]
        public async Task<IActionResult> ServicePayment([FromBody] ServicePaymentRequest request)
        {
            var currentUser = await GetValidatedActiveUserAsync();
            if (currentUser == null)
                return Unauthorized("Sesiunea a expirat după 10 minute de inactivitate. Te rugăm să te autentifici din nou.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currency = NormalizeCurrency(request.Currency);
            if (!IsSupportedCurrency(currency))
                return BadRequest("Moneda nu este suportată.");

            if (request.Amount <= 0)
                return BadRequest("Suma trebuie să fie mai mare decât 0.");

            var isAdmin = User.IsInRole("Administrator");
            if (!isAdmin && currentUser.UserId != request.InitiatedByUserId)
                return Forbid();

            var fromAccount = await _context.Accounts
    .FirstOrDefaultAsync(a => a.Iban == request.FromIban);
            if (fromAccount == null)
                return NotFound("Contul sursă nu a fost găsit.");

            if (!isAdmin && fromAccount.UserId != currentUser.UserId)
                return Forbid();

            if (!IsActiveAccount(fromAccount))
                return BadRequest("Contul sursă nu este activ pentru tranzacții.");

            if (string.IsNullOrWhiteSpace(fromAccount.Currency) || !IsSupportedCurrency(fromAccount.Currency))
                return BadRequest("Moneda contului sursă nu este validă.");

            if (!string.Equals(currency, fromAccount.Currency, StringComparison.OrdinalIgnoreCase))
                return BadRequest("Moneda tranzacției trebuie să corespundă cu moneda contului sursă.");

            var provider = await _context.ServiceProviders.FirstOrDefaultAsync(p => p.ProviderId == request.ProviderId);
            if (provider == null)
                return NotFound("Furnizorul nu a fost găsit.");

            if (!provider.IsActive)
                return BadRequest("Furnizorul este inactiv.");

            var feeAmount = CalculateCommission("SERVICE_PAYMENT", request.Amount);
            var totalDebit = request.Amount + feeAmount;

            if (fromAccount.Balance < totalDebit)
                return BadRequest("Fonduri insuficiente pentru plata serviciului și comision.");

            var amlFlag = RequiresAmlReview(request.Amount, currency);

            fromAccount.Balance -= totalDebit;
            fromAccount.UpdatedAt = DateTime.UtcNow;

            var transaction = new Transaction
            {
                TransactionUid = Guid.NewGuid(),
                InitiatedByUserId = request.InitiatedByUserId,
                FromAccountId = fromAccount.AccountId,
                ToAccountId = null,
                ProviderId = request.ProviderId,
                Type = "SERVICE_PAYMENT",
                Amount = request.Amount,
                Currency = currency,
                FeeAmount = feeAmount,
                Status = "COMPLETED",
                AmlFlag = amlFlag,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            await AddAuditLogAsync(request.InitiatedByUserId, "SERVICE_PAYMENT", "TRANSACTION", transaction.TransactionId.ToString());

            if (transaction.AmlFlag)
                await AddAuditLogAsync(request.InitiatedByUserId, "AML_FLAGGED_TRANSACTION", "TRANSACTION", transaction.TransactionId.ToString());

            await CreateTransactionNotificationsAsync(
                request.InitiatedByUserId,
                transaction,
                $"Plata către furnizor pentru suma de {transaction.Amount} {transaction.Currency} a fost efectuată cu succes."
            );

            return Ok(new
            {
                message = amlFlag ? "Plata către furnizor a fost efectuată și marcată pentru AML review." : "Plata către furnizor a fost efectuată cu succes.",
                transactionId = transaction.TransactionId,
                providerId = provider.ProviderId,
                providerName = provider.ProviderName,
                amount = transaction.Amount,
                currency = transaction.Currency,
                feeAmount = transaction.FeeAmount,
                amlFlag = transaction.AmlFlag,
                sourceBalanceAfter = fromAccount.Balance,
                createdAt = transaction.CreatedAt
            });
        }

        [HttpGet("help")]
        public IActionResult GetHelp()
        {
            return Ok(new
            {
                module = "Procesarea tranzactiilor",
                supportedOperations = new[]
                {
                    "Deposit",
                    "Withdrawal",
                    "Transfer",
                    "Service Payment"
                },
                supportedCurrencies = ExchangeRatesToRon.Keys,
                commissions = new
                {
                    deposit = "0%",
                    withdrawal = "1%",
                    transferSameCurrency = "0.5%",
                    transferCrossCurrency = "1%",
                    servicePayment = "0.5%"
                },
                amlRule = "Tranzactiile cu echivalent >= 10000 RON sunt marcate pentru AML review.",
                notes = new[]
                {
                    "Conturile trebuie sa fie APPROVED sau REACTIVATED.",
                    "Pentru retrageri si transferuri se verifica soldul disponibil.",
                    "Transferurile cross-currency folosesc conversie valutara interna."
                }
            });
        }
    }
}