namespace Hexalith.Tenants.UI.Services.Gateways;

/// <summary>Bounds one complete search load, including index windows, hydration and ordinary-list fallback.</summary>
public sealed class TenantSearchOptions
{
    /// <summary>Gets the configuration section.</summary>
    public const string SectionName = "Tenants:Search";

    /// <summary>Gets or sets the complete search-load budget. The initial policy is ten seconds.</summary>
    public TimeSpan LoadBudget { get; set; } = TimeSpan.FromSeconds(10);
}
