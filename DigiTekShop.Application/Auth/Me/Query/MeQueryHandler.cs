using DigiTekShop.Contracts.Abstractions.Identity.Auth;
using DigiTekShop.Contracts.DTOs.Auth.Me;

namespace DigiTekShop.Application.Auth.Me.Query;
public sealed class MeQueryHandler : IQueryHandler<MeQuery,MeResponse>
{
    private readonly IMeService _svc;
    public MeQueryHandler(IMeService svc) => _svc = svc;

    public Task<Result<MeResponse>> Handle(MeQuery request, CancellationToken ct)
        => _svc.GetAsync(ct);
}