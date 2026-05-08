using BankingAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DbTestController : ControllerBase
    {
        private readonly BankingDbContext _db;

        public DbTestController(BankingDbContext db)
        {
            _db = db;
        }

        [HttpGet("ping")]
        public async Task<IActionResult> Ping()
        {
            try
            {
                // test real: încearcă să deschidă conexiunea
                var conn = _db.Database.GetDbConnection();
                await conn.OpenAsync();
                await conn.CloseAsync();

                return Ok(new { canConnect = true });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    canConnect = false,
                    error = ex.Message,
                    type = ex.GetType().FullName
                });
            }
        }
        [HttpGet("tables")]
        public async Task<IActionResult> Tables()
        {
            var tables = await _db.Database
                .SqlQueryRaw<string>("SELECT table_name FROM information_schema.tables WHERE table_schema='public' ORDER BY table_name;")
                .ToListAsync();

            return Ok(tables);
        }
    }
}