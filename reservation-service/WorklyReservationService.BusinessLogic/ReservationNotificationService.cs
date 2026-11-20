using Microsoft.Extensions.Logging;
using WorklyReservationService.Domain;
using WorklyReservationService.IBusinessLogic.Notifications;
using WorklyReservationService.IDataAccess;
using WorklyReservationService.IDataAccess.External;

namespace WorklyReservationService.BusinessLogic;

public sealed class ReservationNotificationService
{
    private readonly IAreaUsersClient _areaUsers;
    private readonly IAreaNotificationRecipientRepository _areaRecipients;
    private readonly IReservationNotificationPublisher _publisher;
    private readonly ILogger<ReservationNotificationService> _logger;

    public ReservationNotificationService(
        IAreaUsersClient areaUsers,
        IAreaNotificationRecipientRepository areaRecipients,
        IReservationNotificationPublisher publisher,
        ILogger<ReservationNotificationService> logger)
    {
        _areaUsers = areaUsers;
        _areaRecipients = areaRecipients;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task PublishReservationEventAsync(
        string action,
        Reservation reservation,
        UserResponse actor,
        RoomResponse room,
        MainServiceAuthContext authentication,
        IReadOnlyList<ReservationNotificationChange>? changes = null,
        CancellationToken cancellationToken = default)
    {
        if (actor.AreaId is null || actor.AreaId == Guid.Empty)
        {
            _logger.LogInformation(
                "Skipping reservation notification for {ReservationId} because actor has no area.",
                reservation.Id);
            return;
        }

        IReadOnlyList<AreaNotificationRecipient> recipients = await ResolveAreaRecipientsAsync(
                actor.AreaId.Value,
                authentication,
                cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<string> emails = recipients
            .Where(recipient => recipient.EmailSubscribed)
            .Select(recipient => recipient.Email.Trim().ToLowerInvariant())
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (emails.Count == 0)
        {
            _logger.LogInformation(
                "Skipping reservation notification for {ReservationId} because no subscribed recipients were found for area {AreaId}.",
                reservation.Id,
                actor.AreaId);
            return;
        }

        string actorName = ResolveActorName(actor);

        var message = new ReservationNotificationMessage
        {
            Emails = emails,
            EmailSubscribed = true,
            Action = action,
            ActionAt = DateTime.UtcNow,
            Reservation = new ReservationNotificationReservation
            {
                Id = reservation.Id.ToString(),
                Space = new ReservationNotificationSpace
                {
                    Name = room.Name,
                },
                StartAt = reservation.StartUtc,
                EndAt = reservation.EndUtc,
            },
            User = new ReservationNotificationUser
            {
                Id = actor.Id.ToString(),
                Name = actorName,
                Email = actor.Email,
            },
            Changes = changes,
        };

        await _publisher.PublishAsync(message, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<AreaNotificationRecipient>> ResolveAreaRecipientsAsync(
        Guid areaId,
        MainServiceAuthContext authentication,
        CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<AreaUserResponse> users = await _areaUsers
                .GetByAreaIdAsync(areaId, authentication, cancellationToken)
                .ConfigureAwait(false);

            DateTime updatedAtUtc = DateTime.UtcNow;
            var recipients = users
                .Select(user => new AreaNotificationRecipient
                {
                    AreaId = areaId,
                    UserId = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    EmailSubscribed = user.EmailSubscribed,
                    UpdatedAtUtc = updatedAtUtc,
                })
                .ToList();

            await _areaRecipients
                .ReplaceForAreaAsync(areaId, recipients, cancellationToken)
                .ConfigureAwait(false);

            return recipients;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to fetch area users for area {AreaId}; using cached recipients.",
                areaId);
        }

        return await _areaRecipients
            .GetByAreaIdAsync(areaId, cancellationToken)
            .ConfigureAwait(false);
    }

    private static string ResolveActorName(UserResponse actor)
    {
        if (!string.IsNullOrWhiteSpace(actor.Name))
        {
            return actor.Name.Trim();
        }

        int atIndex = actor.Email.IndexOf('@');
        if (atIndex > 0)
        {
            return actor.Email[..atIndex];
        }

        return actor.Email;
    }
}
