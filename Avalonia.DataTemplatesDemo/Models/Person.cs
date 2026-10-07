namespace Avalonia.DataTemplatesDemo.Models
{
    public enum Sex
    {
        Male,
        Female,
    }

    public class Person
    {
        public required int Id { get; init; }

        public required string Name { get; init; }

        public required string Address { get; init; }

        public required Sex Sex { get; init; }
    }
}
