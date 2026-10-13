using System;
using System.Globalization;

namespace Avalonia.Tests.Common
{
    // Pins CultureInfo.CurrentCulture for the length of a test and restores it afterwards, so assertions on formatted
    // text ("40 %", "0.062") do not depend on the machine's regional settings.
    public sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _saved = CultureInfo.CurrentCulture;

        private CultureScope(CultureInfo culture) => CultureInfo.CurrentCulture = culture;

        public static CultureScope Invariant() => new(CultureInfo.InvariantCulture);

        public void Dispose() => CultureInfo.CurrentCulture = _saved;
    }
}
