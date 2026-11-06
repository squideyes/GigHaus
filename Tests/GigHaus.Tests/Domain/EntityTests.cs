using GigHaus.Domain.Entities;
using GigHaus.Domain.Enums;

namespace GigHaus.Tests.Domain;

public class EntityTests
{
    [Fact]
    public void Artist_DefaultValues_AreCorrect()
    {
        var artist = new Artist();

        Assert.NotEqual(Guid.Empty, artist.Id);
        Assert.Equal(string.Empty, artist.Name);
        Assert.Equal(string.Empty, artist.Email);
        Assert.Null(artist.StripeConnectAccountId);
        Assert.Empty(artist.Events);
    }

    [Fact]
    public void Host_DefaultValues_AreCorrect()
    {
        var host = new Host();

        Assert.NotEqual(Guid.Empty, host.Id);
        Assert.Equal(string.Empty, host.Name);
        Assert.Equal(string.Empty, host.Address);
        Assert.Equal(0, host.MaxCapacity);
    }

    [Fact]
    public void Event_DefaultPricingPolicy_MatchesLegalRequirements()
    {
        var ev = new Event();

        // Per legal doc: default should be suggested-only, pay-what-you-can
        Assert.Equal(PricingMode.SuggestedOnly, ev.PricingMode);
        Assert.True(ev.AllowPayWhatYouCan);
        Assert.True(ev.AllowCashAtDoor);
        Assert.True(ev.AllowOnlinePayment);
        Assert.False(ev.AllowReserveNowPayLater);
        Assert.Null(ev.MinimumContributionAmount);
    }

    [Fact]
    public void Event_DefaultDisclosure_ContainsRequiredLanguage()
    {
        var ev = new Event();

        // Legal doc requires: "not charitable donations and are not tax-deductible"
        Assert.Contains("not charitable donations", ev.PricingDisclosureText);
        Assert.Contains("not tax-deductible", ev.PricingDisclosureText);
    }

    [Fact]
    public void Event_DefaultLabel_SaysContribution()
    {
        var ev = new Event();

        Assert.Contains("Suggested donation", ev.SuggestedDonationLabel);
        Assert.Contains("support the artist", ev.SuggestedDonationLabel);
    }

    [Fact]
    public void Reservation_DefaultStatus_IsPending()
    {
        var res = new Reservation();

        Assert.Equal(ReservationStatus.Pending, res.Status);
        Assert.Equal(1, res.NumberOfGuests);
    }

    [Fact]
    public void PaymentRecord_Amounts_DefaultToZero()
    {
        var payment = new PaymentRecord();

        Assert.Equal(0m, payment.GrossAmount);
        Assert.Equal(0m, payment.ProcessorFeeAmount);
        Assert.Equal(0m, payment.PlatformFeeAmount);
        Assert.Equal(0m, payment.NetAmount);
        Assert.Equal(0m, payment.AttributableToArtistAmount);
    }

    [Fact]
    public void PaymentRecord_DefaultStatus_IsPending()
    {
        var payment = new PaymentRecord();
        Assert.Equal(PaymentStatus.Pending, payment.Status);
    }

    [Fact]
    public void Reservation_WorkflowTimestamps_DefaultToNull()
    {
        var res = new Reservation();
        Assert.Null(res.ConfirmedAt);
        Assert.Null(res.CancelledAt);
        Assert.Null(res.CheckedInAt);
        Assert.Null(res.CancellationReason);
    }

    [Fact]
    public void SettlementSummary_DefaultValues_AreCorrect()
    {
        var summary = new SettlementSummary();

        Assert.Equal(0m, summary.TotalGrossCollected);
        Assert.Equal(0m, summary.NetAttributableToArtist);
        Assert.False(summary.IsFinalized);
        Assert.Equal("Pending", summary.PayoutStatus);
        Assert.Null(summary.StripePayoutId);
    }

    [Theory]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Succeeded)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Refunded)]
    [InlineData(PaymentStatus.Disputed)]
    public void PaymentStatus_AllValues_AreDefined(PaymentStatus status)
    {
        Assert.True(Enum.IsDefined(status));
    }

    [Fact]
    public void BaseEntity_CreatedAt_IsRecentUtc()
    {
        var artist = new Artist();
        var now = DateTime.UtcNow;

        Assert.True((now - artist.CreatedAt).TotalSeconds < 5);
        Assert.Equal(DateTimeKind.Utc, artist.CreatedAt.Kind);
    }

    [Theory]
    [InlineData(PricingMode.SuggestedOnly)]
    [InlineData(PricingMode.MinimumRequired)]
    [InlineData(PricingMode.ReserveNowPayLater)]
    [InlineData(PricingMode.DoorOnly)]
    public void PricingMode_AllValues_AreDefined(PricingMode mode)
    {
        Assert.True(Enum.IsDefined(mode));
    }
}

