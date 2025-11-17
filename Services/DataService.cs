using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NLP_Azure_Kernel_Function.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NLP_Azure_Kernel_Function.Services
{
    internal class DataService : IDataService
    {
        private List<Product> _products = new();
        private readonly List<Feedback> _feedbacks = new();
        private readonly ILogger<DataService> _logger;

        public DataService(ILogger<DataService> logger)
        {
            _logger = logger;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                _logger.LogInformation("Starting bearing data loading...");

                var currentDir = Directory.GetCurrentDirectory();
                var dataDir = Path.Combine(currentDir, "Data");

                _logger.LogInformation($"Data directory: {dataDir}");

                // Load all JSON files in the Data directory
                if (Directory.Exists(dataDir))
                {
                    var jsonFiles = Directory.GetFiles(dataDir, "*.json");
                    _logger.LogInformation($"Found {jsonFiles.Length} JSON files in data directory");

                    foreach (var filePath in jsonFiles)
                    {
                        try
                        {
                            var jsonContent = File.ReadAllText(filePath);
                            var product = JsonSerializer.Deserialize<Product>(jsonContent, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (product != null)
                            {
                                // Create search attributes for better searchability
                                product.SearchAttributes = CreateSearchAttributes(product);
                                _products.Add(product);
                                _logger.LogInformation($"Loaded product: {product.Designation} - {product.Title}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Failed to load file: {Path.GetFileName(filePath)}");
                        }
                    }
                }
                else
                {
                    _logger.LogWarning($"Data directory not found: {dataDir}");
                }

                // If no data loaded, create sample bearing data
                if (_products.Count == 0)
                {
                    _logger.LogInformation("No data files found, creating sample bearing data...");
                    CreateSampleBearingData();
                }

                _logger.LogInformation($"Total bearing products loaded: {_products.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading bearing product data");
                CreateSampleBearingData();
            }
        }

        private Dictionary<string, string> CreateSearchAttributes(Product product)
        {
            var attributes = new Dictionary<string, string>();

            // Add basic product info
            attributes["designation"] = product.Designation;
            attributes["category"] = product.Category;
            attributes["taxonomy"] = product.Taxonomy;
            attributes["description"] = product.Description;
            attributes["benefits"] = product.Benefits;

            // Add dimensions
            foreach (var dim in product.Dimensions)
            {
                attributes[$"dim_{dim.Name.ToLower()}"] = $"{dim.Value} {dim.Unit}";
                if (!string.IsNullOrEmpty(dim.Symbol))
                {
                    attributes[$"dim_{dim.Symbol.ToLower()}"] = $"{dim.Value} {dim.Unit}";
                }
            }

            // Add properties
            foreach (var prop in product.Properties)
            {
                attributes[$"prop_{prop.Name.ToLower()}"] = prop.Value;
            }

            // Add performance data
            foreach (var perf in product.Performance)
            {
                var valueStr = perf.Value?.ToString() ?? "";
                if (!string.IsNullOrEmpty(perf.Unit))
                {
                    valueStr += $" {perf.Unit}";
                }
                attributes[$"perf_{perf.Name.ToLower()}"] = valueStr;
                if (!string.IsNullOrEmpty(perf.Symbol))
                {
                    attributes[$"perf_{perf.Symbol.ToLower()}"] = valueStr;
                }
            }

            return attributes;
        }

        private void CreateSampleBearingData()
        {
            _products = new List<Product>
        {
            new Product
            {
                Id = "6205-pim-en-metric",
                Designation = "6205",
                Title = "6205",
                Category = "Deep groove ball bearings",
                Taxonomy = "Bearings Ball bearings Deep groove ball bearings",
                ShortDescription = "Deep groove ball bearing",
                Description = "Single row deep groove ball bearings are particularly versatile, have low friction and are optimized for low noise and low vibration, which enables high rotational speeds. They accommodate radial and axial loads in both directions, are easy to mount, and require less maintenance than many other bearing types.",
                Benefits = "Simple, versatile and robust design Low friction High-speed capability Accommodate radial and axial loads in both directions Require little maintenance",
                System = "metric",
                Language = "en",
                Source = "pim",
                Dimensions = new List<Dimension>
                {
                    new Dimension { Name = "Outside diameter", Value = 52, Unit = "mm", Symbol = "D" },
                    new Dimension { Name = "Bore diameter", Value = 25, Unit = "mm", Symbol = "d" },
                    new Dimension { Name = "Width", Value = 15, Unit = "mm", Symbol = "B" }
                },
                Properties = new List<Property>
                {
                    new Property { Name = "Tolerance class", Value = "Class P6 (P6)" },
                    new Property { Name = "Material, bearing", Value = "Bearing steel" },
                    new Property { Name = "Relubrication feature", Value = "Without" },
                    new Property { Name = "Coating", Value = "Without" },
                    new Property { Name = "Lubricant", Value = "None" }
                },
                Performance = new List<Performance>
                {
                    new Performance { Name = "Limiting speed", Value = 18000, Unit = "rmin", Symbol = "nlim" },
                    new Performance { Name = "Basic static load rating", Value = 7.8, Unit = "kN", Symbol = "C0" },
                    new Performance { Name = "Reference speed", Value = 28000, Unit = "rmin" },
                    new Performance { Name = "SKF performance class", Value = "SKF Explorer" },
                    new Performance { Name = "Basic dynamic load rating", Value = 14.8, Unit = "kN", Symbol = "C" }
                },
                Logistics = new List<Logistic>
                {
                    new Logistic { Name = "Product net weight", Value = 0.125, Unit = "kg" },
                    new Logistic { Name = "Products per pack", Value = "1" }
                }
            },
            new Product
            {
                Id = "6305-pim-en-metric",
                Designation = "6305",
                Title = "6305",
                Category = "Deep groove ball bearings",
                Taxonomy = "Bearings Ball bearings Deep groove ball bearings",
                ShortDescription = "Deep groove ball bearing",
                Description = "Medium series deep groove ball bearing with higher load capacity",
                Benefits = "Higher load capacity Robust design Versatile application",
                System = "metric",
                Language = "en",
                Source = "pim",
                Dimensions = new List<Dimension>
                {
                    new Dimension { Name = "Outside diameter", Value = 62, Unit = "mm", Symbol = "D" },
                    new Dimension { Name = "Bore diameter", Value = 25, Unit = "mm", Symbol = "d" },
                    new Dimension { Name = "Width", Value = 17, Unit = "mm", Symbol = "B" }
                },
                Performance = new List<Performance>
                {
                    new Performance { Name = "Basic dynamic load rating", Value = 22.5, Unit = "kN", Symbol = "C" },
                    new Performance { Name = "Basic static load rating", Value = 11.5, Unit = "kN", Symbol = "C0" }
                }
            }
        };

            // Create search attributes for sample data
            foreach (var product in _products)
            {
                product.SearchAttributes = CreateSearchAttributes(product);
            }

            _logger.LogInformation($"Created {_products.Count} sample bearing products");
        }

        public async Task<List<Product>> SearchProductsAsync(string query)
        {
            return await Task.Run(() =>
            {
                if (string.IsNullOrWhiteSpace(query))
                    return _products;

                var searchTerms = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                var results = _products
                    .Where(p => searchTerms.Any(term =>
                        p.Designation.ToLower().Contains(term) ||
                        p.Category.ToLower().Contains(term) ||
                        p.Taxonomy.ToLower().Contains(term) ||
                        p.Description.ToLower().Contains(term) ||
                        p.Benefits.ToLower().Contains(term) ||
                        p.SearchAttributes.Values.Any(v => v?.ToLower().Contains(term) == true)
                    ))
                    .ToList();

                _logger.LogInformation($"Search for '{query}' returned {results.Count} bearing products");
                return results;
            });
        }

        public async Task<List<Product>> GetProductsByCategoryAsync(string category)
        {
            return await Task.Run(() =>
            {
                var results = _products
                    .Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                _logger.LogInformation($"Category search for '{category}' returned {results.Count} bearing products");
                return results;
            });
        }

        public async Task<Product?> GetProductByIdAsync(string id)
        {
            return await Task.Run(() =>
            {
                return _products.FirstOrDefault(p =>
                    p.Id.Equals(id, StringComparison.OrdinalIgnoreCase) ||
                    p.Designation.Equals(id, StringComparison.OrdinalIgnoreCase));
            });
        }

        public async Task StoreFeedbackAsync(Feedback feedback)
        {
            await Task.Run(() => _feedbacks.Add(feedback));
        }

        public async Task<List<Feedback>> GetFeedbackBySessionAsync(string sessionId)
        {
            return await Task.Run(() =>
            {
                return _feedbacks.Where(f => f.SessionId == sessionId).ToList();
            });
        }
    }
}
