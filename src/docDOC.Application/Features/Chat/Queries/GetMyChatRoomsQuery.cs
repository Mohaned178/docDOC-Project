using docDOC.Application.Interfaces;
using docDOC.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace docDOC.Application.Features.Chat.Queries;

public sealed record GetMyChatRoomsQuery() : IRequest<IEnumerable<ChatRoomListItem>>;

public sealed record OtherUserInfo(int Id, string FirstName, string LastName, string Role);
public sealed record ChatRoomListItem(int Id, OtherUserInfo OtherUser, bool IsActive, DateTimeOffset UpdatedAt);

public sealed class GetMyChatRoomsQueryHandler : IRequestHandler<GetMyChatRoomsQuery, IEnumerable<ChatRoomListItem>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetMyChatRoomsQueryHandler> _logger;

    public GetMyChatRoomsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<GetMyChatRoomsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<IEnumerable<ChatRoomListItem>> Handle(GetMyChatRoomsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        var userType = _currentUserService.UserType;
        _logger.LogInformation("Fetching chat rooms for user {UserId}", userId);

        var rooms = await _unitOfWork.ChatRooms.GetMyRoomsAsync(userId, cancellationToken);

        var result = new List<ChatRoomListItem>();
        foreach (var c in rooms)
        {
            OtherUserInfo otherUser;
            if (userType == "Patient")
            {
                var doctor = await _unitOfWork.Doctors.GetByIdAsync(c.DoctorId, cancellationToken);
                otherUser = doctor != null
                    ? new OtherUserInfo(doctor.Id, doctor.FirstName, doctor.LastName, "Doctor")
                    : new OtherUserInfo(c.DoctorId, "Unknown", "User", "Doctor");
            }
            else
            {
                var patient = await _unitOfWork.Patients.GetByIdAsync(c.PatientId, cancellationToken);
                otherUser = patient != null
                    ? new OtherUserInfo(patient.Id, patient.FirstName, patient.LastName, "Patient")
                    : new OtherUserInfo(c.PatientId, "Unknown", "User", "Patient");
            }

            result.Add(new ChatRoomListItem(c.Id, otherUser, c.IsActive, c.UpdatedAt));
        }

        return result;
    }
}
