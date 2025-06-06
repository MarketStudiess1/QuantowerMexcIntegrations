
namespace MexcVendor.Extensions;

internal static class DecimalExtensions
{
    public static double ToDouble(this decimal? value) => (double)(value ?? 0m);
}