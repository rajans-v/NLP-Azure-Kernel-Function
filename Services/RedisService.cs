using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace NLP_Azure_Kernel_Function.Services
{
    internal class RedisService : IRedisService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _database;

        public RedisService(IConnectionMultiplexer redis)
        {
            _redis = redis;
            _database = redis.GetDatabase();
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var value = await _database.StringGetAsync(key);
            if (value.IsNull) return default;

            return JsonSerializer.Deserialize<T>(value!);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            var serializedValue = JsonSerializer.Serialize(value);
            await _database.StringSetAsync(key, serializedValue, expiry);
        }

        public async Task<bool> RemoveAsync(string key)
        {
            return await _database.KeyDeleteAsync(key);
        }

        public async Task<bool> KeyExistsAsync(string key)
        {
            return await _database.KeyExistsAsync(key);
        }
    }
}
