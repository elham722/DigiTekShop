namespace DigiTekShop.Contracts.DTOs.Auth.Register
{
    public sealed record RegisterRequestDto
    {
        public required string Email { get; init; }
        public required string Password { get; init; }
        public required string ConfirmPassword { get; init; }
        public string? PhoneNumber { get; init; }          
        public required bool AcceptTerms { get; init; }   
        public string? DeviceId { get; init; }          
        public string? UserAgent { get; init; }         
        public string? Ip { get; init; }             
    }
}
