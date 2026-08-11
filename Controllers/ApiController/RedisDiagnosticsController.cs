using Microsoft.AspNetCore.Mvc;
using SchoolSystem.Data;
using SchoolSystem.Filters;
using StackExchange.Redis;

namespace SchoolSystem.Controllers.ApiController;

[ApiController]
[Route("api/diagnostics/redis")]
[AuthorizeRoles(RoleNames.Admin)]
public sealed class RedisDiagnosticsController : ControllerBase
{
    private readonly IConnectionMultiplexer _redis;
    public RedisDiagnosticsController(IConnectionMultiplexer redis) => _redis = redis;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var database = _redis.GetDatabase();
        var items = new List<object>();
        foreach (var endpoint in _redis.GetEndPoints())
        {
            var server = _redis.GetServer(endpoint);
            if (!server.IsConnected) continue;
            await foreach (var key in server.KeysAsync(pattern: "SchoolApp_*").WithCancellation(cancellationToken))
            {
                items.Add(new
                {
                    key = key.ToString(),
                    type = (await database.KeyTypeAsync(key)).ToString(),
                    ttlSeconds = (await database.KeyTimeToLiveAsync(key))?.TotalSeconds,
                    bytes = await database.StringLengthAsync(key)
                });
            }
        }
        return Ok(new { connected = _redis.IsConnected, count = items.Count, keys = items });
    }
}
