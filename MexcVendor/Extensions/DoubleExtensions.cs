
using System.Globalization;

namespace MexcVendor.Extensions;

internal static class DoubleExtensions
{
    public static string FormatPrice(this double value) => value.ToString("0.##########", CultureInfo.InvariantCulture);
}