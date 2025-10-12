using System.ComponentModel.DataAnnotations;

namespace DigiTekShop.Contracts.DTOs.Customer;

/// <summary>
/// Request DTO for registering a new customer
/// </summary>
public sealed record RegisterCustomerRequest(
    [Required(ErrorMessage = "User ID is required")]
    Guid UserId,
    
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters")]
    string FullName,
    
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    string Email,
    
    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    string? Phone
);

/// <summary>
/// Request DTO for adding an address to customer
/// </summary>
public sealed record AddAddressRequest(
    [Required(ErrorMessage = "Line1 is required")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Line1 must be between 5 and 200 characters")]
    string Line1,
    
    [StringLength(200, ErrorMessage = "Line2 cannot exceed 200 characters")]
    string? Line2,
    
    [Required(ErrorMessage = "City is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "City must be between 2 and 100 characters")]
    string City,
    
    [StringLength(100, ErrorMessage = "State cannot exceed 100 characters")]
    string? State,
    
    [Required(ErrorMessage = "Postal code is required")]
    [StringLength(20, MinimumLength = 3, ErrorMessage = "Postal code must be between 3 and 20 characters")]
    string PostalCode,
    
    [Required(ErrorMessage = "Country is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Country must be between 2 and 100 characters")]
    string Country,
    
    bool IsDefault = false
);

/// <summary>
/// Request DTO for changing customer email
/// </summary>
public sealed record ChangeEmailRequest(
    [Required(ErrorMessage = "New email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    string NewEmail
);

/// <summary>
/// Request DTO for updating customer profile
/// </summary>
public sealed record UpdateProfileRequest(
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters")]
    string FullName,
    
    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    string? Phone
);

/// <summary>
/// Request DTO for setting default address
/// </summary>
public sealed record SetDefaultAddressRequest(
    [Range(0, int.MaxValue, ErrorMessage = "Address index must be non-negative")]
    int AddressIndex
);
