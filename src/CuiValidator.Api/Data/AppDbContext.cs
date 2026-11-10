using Microsoft.EntityFrameworkCore;

namespace CuiValidator.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<CuiCheck> CuiChecks => Set<CuiCheck>();
}
