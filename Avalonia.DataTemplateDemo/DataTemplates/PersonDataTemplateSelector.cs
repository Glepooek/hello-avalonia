using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.DataTemplateDemo.Models;

namespace Avalonia.DataTemplateDemo.DataTemplates
{
    public class PersonDataTemplateSelector : IDataTemplate
    {
        public IDataTemplate MaleDataTemplate { get; set; }
        public IDataTemplate FemaleDataTemplate { get; set; }

        public Control? Build(object? param)
        {
            if (param is Person person)
            {
                if (person.Sex == Sex.Male)
                {
                    return MaleDataTemplate.Build(param);
                }
                else
                {
                    return FemaleDataTemplate.Build(param);
                }
            };

            return new Control();
        }

        public bool Match(object? data)
        {
            return data is Person;
        }
    }
}
