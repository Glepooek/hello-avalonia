namespace Avalonia.DataTemplatesDemo.Models
{
    // A small hierarchy so that implicit templates have something to choose between.
    public abstract class Shape
    {
        public string Name { get; set; } = string.Empty;
    }

    public class Circle : Shape
    {
        public double Radius { get; set; }
    }

    public class Square : Shape
    {
        public double Side { get; set; }
    }

    // Deliberately has no template of its own: shows how DataType matching falls back.
    public class Triangle : Shape
    {
        public double Base { get; set; }
    }
}
