namespace workly_api.DTOs.Company;

public record CreateCompanyRequest(
    string Name,
    CreateAddressRequest Address,
    string Website,
    string Base64Logo
)
{
    public Domain.Company ToEntity()
    {
        return new Domain.Company
        {
            Name = Name,
            Address = new Domain.Address
            {
                Street = Address.Street,
                Number = Address.Number,
                City = Address.City,
                State = Address.State,
                ZipCode = Address.ZipCode,
                Country = Address.Country
            },
            Website = Website,
            Base64Logo = Base64Logo
        };
    }
}

public record CreateAddressRequest(
    string Street,
    int Number,
    string City,
    string State,
    string ZipCode,
    string Country
);