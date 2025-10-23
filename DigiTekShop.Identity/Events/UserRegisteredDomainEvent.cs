namespace DigiTekShop.Identity.Events
{
    public sealed record UserRegisteredDomainEvent : DomainEvent
    {
        public Guid UserId { get; init; }
        public string Email { get; init; }
        public string? FullName { get; init; }
        public string? PhoneNumber { get; init; }

        public UserRegisteredDomainEvent(
            Guid UserId,
            string Email,
            string? FullName,
            string? PhoneNumber,
            DateTimeOffset OccurredOn,
            string? CorrelationId = null)
            : base(OccurredOn, CorrelationId)
        {
            this.UserId = UserId;
            this.Email = Email;
            this.FullName = FullName;
            this.PhoneNumber = PhoneNumber;
        }
    }
}
