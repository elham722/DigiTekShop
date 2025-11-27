namespace DigiTekShop.API.Services.Search;
using DigiTekShop.Contracts.DTOs.Search;
using Elastic.Clients.Elasticsearch;
using Microsoft.EntityFrameworkCore;

public sealed class UserSearchIndexingService
{
    private readonly DigiTekShopIdentityDbContext _db;
    private readonly ElasticsearchClient _es;
    private readonly ILogger<UserSearchIndexingService> _logger;

    private const string UsersIndexName = "digitek-users";

    public UserSearchIndexingService(
        DigiTekShopIdentityDbContext db,
        ElasticsearchClient es,
        ILogger<UserSearchIndexingService> logger)
    {
        _db = db;
        _es = es;
        _logger = logger;
    }

    public async Task ReindexAllUsersAsync(CancellationToken ct = default)
    {
        const int batchSize = 500;
        var total = await _db.Users.CountAsync(ct);
        var processed = 0;

        _logger.LogInformation("Starting user reindex. Total users: {Total}", total);

        while (true)
        {
            var users = await _db.Users
                .OrderBy(u => u.Id)
                .Skip(processed)
                .Take(batchSize)
                .AsNoTracking()
                .ToListAsync(ct);

            if (users.Count == 0)
                break;

            var docs = users.Select(u => new UserSearchDocument
            {
                Id = u.Id.ToString(),
                FullName = u.UserName,
                Phone = u.PhoneNumber ?? string.Empty,
                Email = u.Email,
                IsPhoneConfirmed = u.PhoneNumberConfirmed,
                IsLocked = u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow,
                IsDeleted = u.IsDeleted,
                CreatedAtUtc = u.CreatedAtUtc.UtcDateTime,
                LastLoginAtUtc = u.LastLoginAtUtc?.UtcDateTime,
                Roles = Array.Empty<string>() // بعداً roles رو هم اضافه می‌کنیم
            }).ToList();

            var bulkResponse = await _es.BulkAsync(b => b
                .Index(UsersIndexName)
                .IndexMany(docs, (descriptor, doc) => descriptor.Id(doc.Id)),
                ct);

            if (!bulkResponse.IsValidResponse)
            {
                _logger.LogError("Bulk indexing failed: {@Error}", bulkResponse);
                throw new Exception("Bulk indexing users failed.");
            }

            processed += users.Count;
            _logger.LogInformation("Indexed {Processed}/{Total} users", processed, total);
        }

        _logger.LogInformation("User reindex completed.");
    }
}
