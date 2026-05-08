using BankingAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankingAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ServiceProvidersController : ControllerBase
    {
        private readonly BankingDbContext _context;

        public ServiceProvidersController(BankingDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetServiceProviders()
        {
            var providers = await _context.ServiceProviders
                .Where(p => p.IsActive)
                .OrderBy(p => p.ProviderName)
                .Select(p => new
                {
                    providerId = p.ProviderId,
                    providerName = p.ProviderName,
                    providerAccountIban = p.ProviderAccountIban,
                    isActive = p.IsActive
                })
                .ToListAsync();

            return Ok(providers);
        }
    }
}