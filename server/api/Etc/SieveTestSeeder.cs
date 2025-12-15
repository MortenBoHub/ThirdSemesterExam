using dataccess;

namespace api.Etc;

// Legacy test seeder (library domain) replaced with no-op to avoid build errors.
public class SieveTestSeeder(MyDbContext ctx, TimeProvider timeProvider) : ISeeder
{
    public Task Seed()
    {
        // Intentionally left blank. Implement domain-specific seeding here if needed.
        return Task.CompletedTask;
    }
}