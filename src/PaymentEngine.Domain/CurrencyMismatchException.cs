namespace PaymentEngine.Domain;

// Mixing currencies is a bug in our code, not an expected business situation,
// so it is an exception and not a Result.
public sealed class CurrencyMismatchException(Currency expected, Currency actual)
    : InvalidOperationException($"Cannot operate {expected} with {actual}.");