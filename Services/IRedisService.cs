using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLP_Azure_Kernel_Function.Services
{
    internal interface IRedisService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
        Task<bool> RemoveAsync(string key);
        Task<bool> KeyExistsAsync(string key);
    }
}
