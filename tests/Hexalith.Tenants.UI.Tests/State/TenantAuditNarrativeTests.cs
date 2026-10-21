using System.Globalization;

using Hexalith.Tenants.UI.State.TenantAudit;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

/// <summary>Qualifies the explicit-zone ISO grammar of support-safe narrative timestamps.</summary>
public sealed class TenantAuditNarrativeTests
{
    /// <summary>Provides every supported fractional precision with UTC and signed offsets.</summary>
    public static IEnumerable<object[]> SupportedTimestamps()
    {
        foreach (string zone in new[] { "Z", "+02:30", "-05:00" })
        {
            for (int precision = 0; precision <= 7; precision++)
            {
                string digits = "1234567"[..precision];
                string fraction = precision == 0 ? string.Empty : $".{digits}";
                long ticks = precision == 0 ? 0 : long.Parse(digits.PadRight(7, '0'), CultureInfo.InvariantCulture);
                TimeSpan offset = zone switch
                {
                    "+02:30" => new(2, 30, 0),
                    "-05:00" => new(-5, 0, 0),
                    _ => TimeSpan.Zero,
                };
                yield return [$"2026-06-01T12:34:56{fraction}{zone}", ticks, offset];
            }
        }
    }

    /// <summary>Preserves supported timestamp evidence and displays it as canonical UTC.</summary>
    [Theory]
    [MemberData(nameof(SupportedTimestamps))]
    public void ExplicitZoneTimestampsPreservePrecisionAndNormalizeDisplay(string input, long ticks, TimeSpan offset)
    {
        DateTimeOffset expected = new DateTimeOffset(2026, 6, 1, 12, 34, 56, offset).AddTicks(ticks);
        TenantAuditNarrative narrative = TenantAuditNarrative.FromPayload(new Dictionary<string, string>
        {
            ["timestamp"] = input,
            ["occurredAt"] = input,
        });

        narrative.Timestamp.ShouldBe(expected);
        narrative.OccurredAt.ShouldBe(expected);
        narrative.Timestamp!.Value.Offset.ShouldBe(offset);
        narrative.OccurredAt!.Value.Offset.ShouldBe(offset);
        string canonical = expected.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
        narrative.ToDisplayString().ShouldBe($"timestamp: {canonical}; occurredAt: {canonical}");
    }

    /// <summary>Rejects ambiguous, malformed and unsupported narrative date text without displaying it.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("2026-06-01")]
    [InlineData("2026-06-01T12:34:56")]
    [InlineData("2026-06-01T12:34:56.1234567")]
    [InlineData("01/06/2026 12:34:56")]
    [InlineData("2026-06-01 12:34:56Z")]
    [InlineData("2026-06-01T12:34:56z")]
    [InlineData("2026-06-01T12:34:56.Z")]
    [InlineData("2026-06-01T12:34:56.+02:30")]
    [InlineData("2026-06-01T12:34:56.12345678Z")]
    [InlineData("2026-06-01T12:34:56.12345678+02:30")]
    [InlineData("2026-06-01T12:34:56+0230")]
    [InlineData("2026-06-01T12:34:56+2:30")]
    [InlineData("2026-06-01T12:34:56+02:3")]
    [InlineData("2026-06-01T12:34:56+15:00")]
    [InlineData("2026-02-30T12:34:56Z")]
    [InlineData("2026-06-01T12:34:60Z")]
    [InlineData(" 2026-06-01T12:34:56Z")]
    [InlineData("2026-06-01T12:34:56Z ")]
    [InlineData("2026-06-01T12:34:56Z; role: TenantOwner")]
    public void UnsupportedTimestampsAreOmittedFromTypedEvidenceAndDisplay(string input)
    {
        TenantAuditNarrative narrative = TenantAuditNarrative.FromPayload(new Dictionary<string, string>
        {
            ["timestamp"] = input,
            ["occurredAt"] = input,
            ["notAllowed"] = "2026-06-01T12:34:56Z",
        });

        narrative.Timestamp.ShouldBeNull();
        narrative.OccurredAt.ShouldBeNull();
        narrative.ToDisplayString().ShouldBeEmpty();
    }

    /// <summary>Preserves the original server round-trip representation with UTC and trailing zeros.</summary>
    [Theory]
    [InlineData("2026-06-01T12:34:56.0000000+00:00", 0)]
    [InlineData("2026-06-01T12:34:56.1200000+00:00", 1200000)]
    public void CanonicalServerTimestampsRemainSupported(string input, long ticks)
    {
        TenantAuditNarrative narrative = TenantAuditNarrative.FromPayload(new Dictionary<string, string>
        {
            ["timestamp"] = input,
            ["occurredAt"] = input,
        });
        DateTimeOffset expected = new DateTimeOffset(2026, 6, 1, 12, 34, 56, TimeSpan.Zero).AddTicks(ticks);

        narrative.Timestamp.ShouldBe(expected);
        narrative.OccurredAt.ShouldBe(expected);
        narrative.ToDisplayString().ShouldBe($"timestamp: {input}; occurredAt: {input}");
    }
}
