using docDOC.Application.Interfaces;
using docDOC.Domain.Entities;
using docDOC.Domain.Enums;
using docDOC.Domain.Exceptions;
using docDOC.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace docDOC.Application.Features.Chat.Commands;

public sealed record CreateOrGetChatRoomCommand(int OtherUserId, string OtherUserType) : IRequest<ChatRoomResult>;

public sealed class CreateOrGetChatRoomCommandHandler : IRequestHandler<CreateOrGetChatRoomCommand, ChatRoomResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateOrGetChatRoomCommandHandler> _logger;

    public CreateOrGetChatRoomCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<CreateOrGetChatRoomCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ChatRoomResult> Handle(CreateOrGetChatRoomCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        var currentUserType = _currentUserService.UserType;

int patientId, doctorId;
        if (currentUserType == "Patient")
        {
            patientId = currentUserId;
            doctorId = request.OtherUserId;
        }
        else
        {
            patientId = request.OtherUserId;
            doctorId = currentUserId;
        }

        _logger.LogInformation("Attempting to create or get chat room for Patient {PatientId} and Doctor {DoctorId}", patientId, doctorId);

var hasAppointment = await _unitOfWork.ChatRooms.HasQualifyingAppointmentAsync(patientId, doctorId, cancellationToken);
        if (!hasAppointment)
        {
            _logger.LogWarning("Chat room creation rejected: No qualifying appointment between Patient {PatientId} and Doctor {DoctorId}", patientId, doctorId);
            throw new ForbiddenException("You must have a confirmed or completed appointment to chat with this doctor.");
        }

        var chatRoom = await _unitOfWork.ChatRooms.GetByPairAsync(patientId, doctorId, cancellationToken);
        bool isNew = false;

        if (chatRoom != null)
        {
            if (!chatRoom.IsActive)
            {
                _logger.LogInformation("Re-activating existing soft-closed chat room {RoomId}", chatRoom.Id);
                chatRoom.IsActive = true;
                chatRoom.UpdatedAt = DateTimeOffset.UtcNow;
                _unitOfWork.ChatRooms.Update(chatRoom);
            }
            else
            {
                _logger.LogInformation("Returning existing active chat room {RoomId}", chatRoom.Id);
            }
        }
        else
        {
            _logger.LogInformation("Creating new chat room for Patient {PatientId} and Doctor {DoctorId}", patientId, doctorId);
            chatRoom = new ChatRoom
            {
                PatientId = patientId,
                DoctorId = doctorId,
                IsActive = true,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await _unitOfWork.ChatRooms.AddAsync(chatRoom, cancellationToken);
            isNew = true;
        }

return new ChatRoomResult(
            chatRoom.Id,
            chatRoom.PatientId,
            chatRoom.DoctorId,
            chatRoom.IsActive,
            chatRoom.CreatedAt,
            isNew);
    }
}

public sealed record ChatRoomResult(
    int Id,
    int PatientId,
    int DoctorId,
    bool IsActive,
    DateTimeOffset CreatedAt,
    bool IsNew);
