namespace Avalonia.DataTemplateDemo.Models
{
    public class Person
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public Sex Sex { get; set; }
    }

    public enum Sex
    {
        Male,
        Female
    }
}
