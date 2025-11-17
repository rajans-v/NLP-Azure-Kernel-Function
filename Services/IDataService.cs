using NLP_Azure_Kernel_Function.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLP_Azure_Kernel_Function.Services
{
    internal interface IDataService
    {
        Task<List<Product>> SearchProductsAsync(string query);
        Task<List<Product>> GetProductsByCategoryAsync(string category);
        Task<Product?> GetProductByIdAsync(string id);
        Task StoreFeedbackAsync(Feedback feedback);
        Task<List<Feedback>> GetFeedbackBySessionAsync(string sessionId);
    }
}
