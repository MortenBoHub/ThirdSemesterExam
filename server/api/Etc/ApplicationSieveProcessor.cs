using Microsoft.Extensions.Options;
using Sieve.Models;
using Sieve.Services;
using dataccess;

namespace api.Etc;

/// <summary>
///     Custom Sieve processor with fluent API configuration for entities
/// </summary>
public class ApplicationSieveProcessor : SieveProcessor
{
    public ApplicationSieveProcessor(IOptions<SieveOptions> options) : base(options)
    {
    }

    protected override SievePropertyMapper MapProperties(SievePropertyMapper mapper)
    {
        // Players
        mapper.Property<Player>(p => p.Name)
            .CanFilter()
            .CanSort();

        mapper.Property<Player>(p => p.Email)
            .CanFilter()
            .CanSort();

        mapper.Property<Player>(p => p.Createdat)
            .CanFilter()
            .CanSort();

        // Boards
        mapper.Property<Board>(b => b.Year)
            .CanFilter()
            .CanSort();

        mapper.Property<Board>(b => b.Weeknumber)
            .CanFilter()
            .CanSort();

        // Fund Requests
        mapper.Property<Fundrequest>(r => r.Createdat)
            .CanFilter()
            .CanSort();

        mapper.Property<Fundrequest>(r => r.Status)
            .CanFilter()
            .CanSort();

        mapper.Property<Fundrequest>(r => r.Amount)
            .CanFilter()
            .CanSort();

        mapper.Property<Fundrequest>(r => r.Transactionnumber)
            .CanFilter()
            .CanSort();

        mapper.Property<Fundrequest>(r => r.Playerid)
            .CanFilter()
            .CanSort();

        return mapper;
    }
}