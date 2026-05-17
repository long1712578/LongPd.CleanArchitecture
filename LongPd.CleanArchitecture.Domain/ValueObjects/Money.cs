using LongPd.CleanArchitecture.Domain.Exceptions;

namespace LongPd.CleanArchitecture.Domain.ValueObjects;

/// <summary>
/// Value object representing monetary amount + currency.
/// Immutable by design — equality based on value, not reference.
/// </summary>
public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Of(decimal amount, string currency)
    {
        if (amount < 0)
            throw new DomainException("Money amount cannot be negative.");

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new DomainException("Currency must be a 3-letter ISO 4217 code (e.g., VND, USD).");

        return new Money(amount, currency.ToUpperInvariant());
    }

    public static Money Zero(string currency) => Of(0, currency);

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new DomainException($"Cannot add different currencies: {a.Currency} and {b.Currency}.");
        return Of(a.Amount + b.Amount, a.Currency);
    }

    public override string ToString() => $"{Amount:N0} {Currency}";
}
