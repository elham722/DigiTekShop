using DigiTekShop.Contracts.DTOs.Auth.SecurityEvent;
using DigiTekShop.Contracts.Enums.Security;
using DigiTekShop.SharedKernel.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiTekShop.Contracts.Interfaces.Identity.Auth
{
    public interface ISecurityEventService
    {
        Task<Result<SecurityEventDto>> RecordSecurityEventAsync(
            SecurityEventCreateDto request,
            CancellationToken ct = default);

       
        Task<Result<SecurityEventDto>> RecordSecurityEventAsync<T>(
            SecurityEventType type,
            T metadata,
            Guid? userId = null,
            string? ipAddress = null,
            string? userAgent = null,
            string? deviceId = null,
            CancellationToken ct = default);

        Task<Result<IEnumerable<SecurityEventDto>>> GetUnresolvedEventsAsync(
            int limit = 100,
            CancellationToken ct = default);

        Task<Result<IEnumerable<SecurityEventDto>>> GetUserSecurityEventsAsync(
            Guid userId,
            int limit = 50,
            CancellationToken ct = default);

        Task<Result<IEnumerable<SecurityEventDto>>> GetSecurityEventsFromIpAsync(
            string ipAddress,
            TimeSpan timeWindow,
            CancellationToken ct = default);

        Task<Result<bool>> ResolveSecurityEventAsync(
            SecurityEventResolveDto request,
            CancellationToken ct = default);

        Task<Result<SecurityEventStatsDto>> GetSecurityEventStatsAsync(
            TimeSpan timeWindow,
            CancellationToken ct = default);

        Task<Result<int>> CleanupOldEventsAsync(
            TimeSpan olderThan,
            CancellationToken ct = default);
    }
}
