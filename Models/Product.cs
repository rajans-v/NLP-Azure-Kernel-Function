namespace NLP_Azure_Kernel_Function.Models
{
    public class Product
    {
        public string Id { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Taxonomy { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Benefits { get; set; } = string.Empty;
        public string System { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;

        public List<Dimension> Dimensions { get; set; } = new();
        public List<Property> Properties { get; set; } = new();
        public List<Performance> Performance { get; set; } = new();
        public List<Logistic> Logistics { get; set; } = new();
        public List<Specification> Specifications { get; set; } = new();

        // Helper properties for search
        public Dictionary<string, string> SearchAttributes { get; set; } = new();
    }

    // Supporting models
    public class Dimension
    {
        public string Name { get; set; } = string.Empty;
        public object? Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
    }

    public class Property
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class Performance
    {
        public string Name { get; set; } = string.Empty;
        public object? Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
    }

    public class Logistic
    {
        public string Name { get; set; } = string.Empty;
        public object? Value { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    public class Specification
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
