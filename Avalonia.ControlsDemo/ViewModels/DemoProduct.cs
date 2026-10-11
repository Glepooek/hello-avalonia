using System.Collections.Generic;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>A plain editable row shared by the DataGrid, TableView and ContentControl pages.</summary>
    public sealed class DemoProduct
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool InStock { get; set; }
        public string Description { get; set; } = string.Empty;

        public static List<DemoProduct> Sample() =>
        [
            new() { Name = "Apple", Category = "Fruit", Price = 1.2m, InStock = true, Description = "fresh" },
            new() { Name = "Bread", Category = "Bakery", Price = 3.5m, InStock = false, Description = "daily" },
            new() { Name = "Cherry", Category = "Fruit", Price = 5.0m, InStock = true, Description = "sweet" },
            new() { Name = "Bagel", Category = "Bakery", Price = 2.2m, InStock = true, Description = "seeded" },
            new() { Name = "Banana", Category = "Fruit", Price = 0.8m, InStock = true, Description = "ripe" },
            new() { Name = "Croissant", Category = "Bakery", Price = 2.9m, InStock = false, Description = "buttery" },
        ];
    }
}
