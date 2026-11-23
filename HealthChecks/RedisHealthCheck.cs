using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Crud.HealthChecks;

//asp.net core health check for redis
public class RedisHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _distributedCache;

    public RedisHealthCheck(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await _distributedCache.GetStringAsync("health-check", cancellationToken);

            return HealthCheckResult.Healthy("Redis is available.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis is unavailable.", ex);
        }
    }
}
