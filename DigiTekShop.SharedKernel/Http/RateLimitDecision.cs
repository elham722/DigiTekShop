namespace DigiTekShop.SharedKernel.Http;

public readonly record struct RateLimitDecision(
    bool Allowed,       
    long Count,            
    int Limit,              
    TimeSpan Window,        
    DateTimeOffset ResetAt, 
    TimeSpan? Ttl           
);
