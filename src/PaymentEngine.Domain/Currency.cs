namespace PaymentEngine.Domain;

// ISO 4217 numeric codes. No currency is 0,
// so an unset Currency never becomes a valid one by accident.
public enum Currency
{
    USD = 840,
    BRL = 986,
}