using System.ComponentModel.DataAnnotations;

namespace enterprise_order_api.Contracts;

public sealed class CustomerRequest
{
    private string _name = string.Empty;
    private string _email = string.Empty;

    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string Name
    {
        get => _name;
        init => _name = value?.Trim() ?? string.Empty;
    }

    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email
    {
        get => _email;
        init => _email = value?.Trim() ?? string.Empty;
    }

    [Phone]
    [StringLength(30)]
    public string? Phone { get; init; }
}
