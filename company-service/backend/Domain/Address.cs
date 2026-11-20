using CustomExceptions.CompanyExceptions;

namespace Domain;

public sealed class Address
{
    public string Street { get; set; } = String.Empty;
    public int Number { get; set; }
    public string City { get; set; } = String.Empty;
    public string State { get; set; } = String.Empty;
    public string ZipCode { get; set; } = String.Empty;
    public string Country { get; set; } = String.Empty;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Street))
            throw new CreateCompanyException("Street cannot be empty.");

        if (Number <= 0)
            throw new CreateCompanyException("Number must be greater than zero.");

        if (string.IsNullOrWhiteSpace(City))
            throw new CreateCompanyException("City cannot be empty.");

        if (string.IsNullOrWhiteSpace(State))
            throw new CreateCompanyException("State cannot be empty.");

        if (string.IsNullOrWhiteSpace(ZipCode))
            throw new CreateCompanyException("Zip code cannot be empty.");

        if (string.IsNullOrWhiteSpace(Country))
            throw new CreateCompanyException("Country cannot be empty.");
    }
}