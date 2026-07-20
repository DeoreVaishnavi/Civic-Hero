namespace CivicHero.Backend.Core.ValueObjects;

/// <summary>
/// Represents a physical address.
/// Immutable value object.
/// </summary>
public sealed class Address
{
    /// <summary>
    /// Street name and house/building number.
    /// </summary>
    public string Street { get; private set; }

    /// <summary>
    /// City name.
    /// </summary>
    public string City { get; private set; }

    /// <summary>
    /// State or province.
    /// </summary>
    public string State { get; private set; }

    /// <summary>
    /// Postal or ZIP code.
    /// </summary>
    public string PostalCode { get; private set; }

    /// <summary>
    /// Country name.
    /// </summary>
    public string Country { get; private set; }

    /// <summary>
    /// Required by Entity Framework Core.
    /// Initializes properties with safe default values.
    /// </summary>
    private Address()
    {
        Street = string.Empty;
        City = string.Empty;
        State = string.Empty;
        PostalCode = string.Empty;
        Country = string.Empty;
    }

    /// <summary>
    /// Creates a new address.
    /// </summary>
    public Address(
        string street,
        string city,
        string state,
        string postalCode,
        string country)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new ArgumentException("Street is required.", nameof(street));

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required.", nameof(city));

        if (string.IsNullOrWhiteSpace(state))
            throw new ArgumentException("State is required.", nameof(state));

        if (string.IsNullOrWhiteSpace(postalCode))
            throw new ArgumentException("Postal code is required.", nameof(postalCode));

        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country is required.", nameof(country));

        Street = street.Trim();
        City = city.Trim();
        State = state.Trim();
        PostalCode = postalCode.Trim();
        Country = country.Trim();
    }

    /// <summary>
    /// Returns the formatted address.
    /// </summary>
    public override string ToString()
    {
        return $"{Street}, {City}, {State}, {PostalCode}, {Country}";
    }

    /// <summary>
    /// Returns the formatted address.
    /// </summary>
    public string ToSingleLine()
    {
        return $"{Street}, {City}, {State}, {PostalCode}, {Country}";
    }
}