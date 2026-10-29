namespace PaymentEngine.Domain;

// An expected business failure: a stable code for machines and a message for humans and logs.
public sealed record Error(string Code, string Message);