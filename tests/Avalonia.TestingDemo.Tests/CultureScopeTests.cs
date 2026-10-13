using Avalonia.Tests.Common;
using System.Globalization;
using Xunit;

namespace Avalonia.TestingDemo.Tests
{
    public class CultureScopeTests
    {
        [Fact]
        public void Invariant_pins_the_culture_inside_the_scope_and_restores_it_afterwards()
        {
            var original = CultureInfo.CurrentCulture;
            var other = new CultureInfo("sv-SE");
            CultureInfo.CurrentCulture = other;
            try
            {
                using (CultureScope.Invariant())
                {
                    Assert.Same(CultureInfo.InvariantCulture, CultureInfo.CurrentCulture);
                    Assert.Equal("0.5", 0.5.ToString("F1", CultureInfo.CurrentCulture));
                }

                Assert.Same(other, CultureInfo.CurrentCulture);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }
    }
}
