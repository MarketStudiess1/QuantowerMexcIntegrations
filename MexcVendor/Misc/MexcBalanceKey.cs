
namespace MexcVendor.Misc;

public readonly struct MexcBalanceKey
{
    public string Asset { get; }
    public string AccountType { get; }

    public MexcBalanceKey(string asset, string accountType)
    {
        Asset = asset;
        AccountType = accountType;
    }

    public override bool Equals(object obj) =>
        obj is MexcBalanceKey other && Asset == other.Asset && AccountType == other.AccountType;

    public override int GetHashCode() =>
        (Asset?.GetHashCode() ?? 0) ^ (AccountType?.GetHashCode() ?? 0);

    public override string ToString() => $"{Asset}:{AccountType}";
}