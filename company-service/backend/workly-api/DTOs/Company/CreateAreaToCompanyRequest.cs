using System.ComponentModel.DataAnnotations;
using CustomExceptions.AreaExceptions;
using Domain;

namespace workly_api.DTOs.Company;

public sealed record CreateAreaToCompanyRequest
(
    string Name,
    string Description
)
{
    public Area ToEntity()
    {
        var area = new Area
        {
            Name = this.Name,
            Description = this.Description
        };

        Validate();
        return area;
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new CreateAreaException("Area name cannot be empty.");

        if (string.IsNullOrWhiteSpace(Description))
            throw new CreateAreaException("Area description cannot be empty.");
    }
}