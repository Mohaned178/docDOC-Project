using docDOC.Domain.Entities;
using docDOC.Domain.Enums;
using docDOC.Domain.Interfaces;
using docDOC.Domain.Exceptions;
using MediatR;
using NetTopologySuite.Geometries;

namespace docDOC.Application.Features.Auth.Commands;

public record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Role,
    DateOnly? DateOfBirth,
    string? Gender,
    int? SpecialityId,
    string? Hospital,
    double? Latitude = null,
    double? Longitude = null
) : IRequest<RegisterUserDto>;

public record RegisterUserDto(int Id, string Email, string FirstName, string LastName, string Role);

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterUserDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public RegisterUserCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RegisterUserDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        bool isDoctor = request.Role.Equals("Doctor", StringComparison.OrdinalIgnoreCase);
        bool isPatient = request.Role.Equals("Patient", StringComparison.OrdinalIgnoreCase);

        if (!isDoctor && !isPatient)
            throw new ArgumentException("Invalid role");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        if (isPatient)
        {
            var exists = await _unitOfWork.Patients.GetByEmailAsync(request.Email, cancellationToken);
            if (exists != null) throw new ConflictException("Email already in use.");

            var patient = new Patient
            {
                Email = request.Email,
                PasswordHash = passwordHash,
                FirstName = request.FirstName,
                LastName = request.LastName,
                DateOfBirth = request.DateOfBirth ?? DateOnly.MinValue,
                Gender = Enum.TryParse<Gender>(request.Gender, true, out var g) ? g : Gender.Other,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _unitOfWork.Patients.AddAsync(patient, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new RegisterUserDto(patient.Id, patient.Email, patient.FirstName, patient.LastName, "Patient");
        }
        else
        {
            var exists = await _unitOfWork.Doctors.GetByEmailAsync(request.Email, cancellationToken);
            if (exists != null) throw new ConflictException("Email already in use.");

            var doctor = new Doctor
            {
                Email = request.Email,
                PasswordHash = passwordHash,
                FirstName = request.FirstName,
                LastName = request.LastName,
                SpecialityId = request.SpecialityId ?? throw new ArgumentException("SpecialityId is required for Doctor"),
                Hospital = request.Hospital,
                Location = (request.Latitude.HasValue && request.Longitude.HasValue)
                    ? new Point(request.Longitude.Value, request.Latitude.Value) { SRID = 4326 }
                    : null,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _unitOfWork.Doctors.AddAsync(doctor, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new RegisterUserDto(doctor.Id, doctor.Email, doctor.FirstName, doctor.LastName, "Doctor");
        }
    }
}
