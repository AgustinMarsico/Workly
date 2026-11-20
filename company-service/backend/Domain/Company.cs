using CustomExceptions.CompanyExceptions;

namespace Domain;

public sealed class Company
{
    public Guid Id { get; set; }
    public string Name { get; set; } = String.Empty;
    public Address Address { get; set; } = new();
    public string Website { get; set; } = String.Empty;
    public string Base64Logo { get; set; } = String.Empty;

    public ICollection<Area> Areas { get; set; } = new List<Area>();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new CreateCompanyException("Company name cannot be empty.");

        if (string.IsNullOrWhiteSpace(Website))
            throw new CreateCompanyException("Company website cannot be empty.");

        if (string.IsNullOrWhiteSpace(Base64Logo))
            throw new CreateCompanyException("Company logo cannot be empty.");

        Address.Validate();
    }
}
