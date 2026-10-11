using System.Collections.Generic;

namespace Avalonia.ControlsDemo.ViewModels
{
    /// <summary>A read-only tree node; TreeDataTemplate walks <see cref="Children" /> to build the next level.</summary>
    public sealed class DemoTreeNode
    {
        public DemoTreeNode(string label, params DemoTreeNode[] children)
        {
            Label = label;
            Children = children;
        }

        public string Label { get; }

        public IReadOnlyList<DemoTreeNode> Children { get; }

        public static List<DemoTreeNode> Sample() =>
        [
            new("文档", new DemoTreeNode("报告", new DemoTreeNode("2025 年报"), new DemoTreeNode("2026 季报")), new DemoTreeNode("合同")),
            new("图片", new DemoTreeNode("旅行")),
        ];
    }
}
