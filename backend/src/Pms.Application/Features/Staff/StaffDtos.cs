using Pms.Domain.Enums;

namespace Pms.Application.Features.Staff;

public record StaffDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string? Email,
    string? Phone,
    StaffRole Role,
    string? Department,
    DateOnly HireDate,
    StaffStatus Status);

public record CreateStaffRequest(
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    StaffRole Role,
    string? Department,
    DateOnly HireDate,
    StaffStatus Status);

public record UpdateStaffRequest(
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    StaffRole Role,
    string? Department,
    DateOnly HireDate,
    StaffStatus Status);

public record ScheduleDto(
    Guid Id,
    Guid StaffId,
    string StaffName,
    DateOnly Date,
    TimeOnly ShiftStart,
    TimeOnly ShiftEnd,
    string? Notes);

public record CreateScheduleRequest(
    Guid StaffId,
    DateOnly Date,
    TimeOnly ShiftStart,
    TimeOnly ShiftEnd,
    string? Notes);
