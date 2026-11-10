namespace PaymentEngine.Domain.Ledger;

// The accounts of spec section 5.4. Codes are only built through these members,
// so a typo can never send money to an account that does not exist.
public sealed record AccountCode
{
    public static readonly AccountCode AcquirerReceivable = new("acquirer_receivable");

    public static readonly AccountCode PlatformCash = new("platform_cash");

    public static readonly AccountCode AcquirerPayable = new("acquirer_payable");

    public static readonly AccountCode FeeRevenue = new("fee_revenue");

    private AccountCode(string value) => Value = value;

    public string Value { get; }

    public static AccountCode MerchantPending(Guid merchantId) => ForMerchant("merchant_pending", merchantId);

    public static AccountCode MerchantAvailable(Guid merchantId) => ForMerchant("merchant_available", merchantId);

    public override string ToString() => Value;

    private static AccountCode ForMerchant(string prefix, Guid merchantId)
    {
        if (merchantId == Guid.Empty)
        {
            throw new ArgumentException("A merchant account needs a merchant id.", nameof(merchantId));
        }

        return new AccountCode($"{prefix}:{merchantId}");
    }
}