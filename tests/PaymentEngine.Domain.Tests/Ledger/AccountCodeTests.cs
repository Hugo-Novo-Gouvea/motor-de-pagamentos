using PaymentEngine.Domain.Ledger;
using Shouldly;

namespace PaymentEngine.Domain.Tests.Ledger;

[Trait("Category", "Unit")]
public class AccountCodeTests
{
    private static readonly Guid MerchantId = Guid.NewGuid();

    [Fact]
    public void FixedAccounts_HaveTheCodesFromTheSpec()
    {
        AccountCode.AcquirerReceivable.Value.ShouldBe("acquirer_receivable");
        AccountCode.PlatformCash.Value.ShouldBe("platform_cash");
        AccountCode.AcquirerPayable.Value.ShouldBe("acquirer_payable");
        AccountCode.FeeRevenue.Value.ShouldBe("fee_revenue");
    }

    [Fact]
    public void MerchantPending_ValidMerchant_IncludesTheMerchantId()
    {
        AccountCode.MerchantPending(MerchantId).Value.ShouldBe($"merchant_pending:{MerchantId}");
    }

    [Fact]
    public void MerchantAvailable_ValidMerchant_IncludesTheMerchantId()
    {
        AccountCode.MerchantAvailable(MerchantId).Value.ShouldBe($"merchant_available:{MerchantId}");
    }

    [Fact]
    public void MerchantPending_SameMerchant_AreEqual()
    {
        (AccountCode.MerchantPending(MerchantId) == AccountCode.MerchantPending(MerchantId)).ShouldBeTrue();
    }

    [Fact]
    public void MerchantPending_DifferentMerchants_AreNotEqual()
    {
        (AccountCode.MerchantPending(MerchantId) == AccountCode.MerchantPending(Guid.NewGuid())).ShouldBeFalse();
    }

    [Fact]
    public void MerchantPending_EmptyMerchantId_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => AccountCode.MerchantPending(Guid.Empty));
    }

    [Fact]
    public void MerchantAvailable_EmptyMerchantId_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => AccountCode.MerchantAvailable(Guid.Empty));
    }
}