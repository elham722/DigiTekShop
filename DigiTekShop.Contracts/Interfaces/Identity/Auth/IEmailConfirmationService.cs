using DigiTekShop.Contracts.DTOs.Auth.EmailConfirmation;
using DigiTekShop.SharedKernel.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Interfaces.Identity.Auth
{
    public interface IEmailConfirmationService
    {
       
        Task<Result> SendAsync(string userId, CancellationToken ct = default);
        Task<Result> ConfirmEmailAsync(ConfirmEmailRequestDto request, CancellationToken ct = default);
        Task<Result> ResendAsync(ResendEmailConfirmationRequestDto request, CancellationToken ct = default);
    }
}
