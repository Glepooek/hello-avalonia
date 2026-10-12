using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.DataTemplatesDemo.Models;

namespace Avalonia.DataTemplatesDemo.DataTemplates
{
    /// <summary>
    /// Picks a template per item at runtime. Avalonia has no DataTemplateSelector
    /// base class like WPF; any IDataTemplate whose Build looks at the data is one.
    /// </summary>
    public class PersonDataTemplateSelector : IDataTemplate
    {
        public IDataTemplate? MaleDataTemplate { get; set; }

        public IDataTemplate? FemaleDataTemplate { get; set; }

        public Control? Build(object? param)
        {
            var person = (Person)param!;
            var template = person.Sex == Sex.Male ? MaleDataTemplate : FemaleDataTemplate;
            return template?.Build(param);
        }

        // Asked first, for every item. Returning false lets the next template in the collection try.
        public bool Match(object? data) => data is Person;
    }
}
