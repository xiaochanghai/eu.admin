using EU.Core.Common.Caches;

namespace EU.Core.Common.Module;

public class UserModuleColumn
{
    private static RedisCacheService _redisInstance;
    private static RedisCacheService Redis => _redisInstance ??= RedisCacheService.Create(1);


}
