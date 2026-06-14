namespace Hexalith.Tenants.UI.State.TenantList;

public readonly record struct TenantCountValue(bool IsKnown, int Value) {
    public static TenantCountValue Unknown { get; } = new(false, 0);

    public static TenantCountValue Known(int value) => new(true, value);
}
