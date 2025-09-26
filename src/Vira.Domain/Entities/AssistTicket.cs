using Vira.Shared.Base;

namespace Vira.Domain.Entities;

public enum AssistType { Grocery = 1, Medicine = 2, Visit = 3 }   // ihtiyaca göre arttır
public enum AssistStatus { Open = 0, Assigned = 1, Resolved = 2, Canceled = 3 }

public sealed class AssistTicket : AuditableEntity<Guid>
{
    public AssistType Type { get; private set; }
    public AssistStatus Status { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string ElderFullName { get; private set; } = default!;
    public string? ElderPhone { get; private set; }
    public string Address { get; private set; } = default!;
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public DateTime? ScheduledAtUtc { get; private set; }
    public string? Notes { get; private set; }

    private AssistTicket() { }

    public AssistTicket(Guid createdBy, AssistType type,
        string elderFullName, string? elderPhone, string address,
        double latitude, double longitude, DateTime? scheduledAtUtc, string? notes)
    {
        Id = Guid.NewGuid();
        CreatedByUserId = createdBy;
        Type = type;
        Status = AssistStatus.Open;

        ElderFullName = elderFullName;
        ElderPhone = elderPhone;
        Address = address;
        Latitude = latitude;
        Longitude = longitude;
        ScheduledAtUtc = scheduledAtUtc;
        Notes = notes;

        CreatedAt = DateTime.UtcNow;
    }

    public void Assign(Guid userId)
    {
        AssignedToUserId = userId;
        Status = AssistStatus.Assigned;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(AssistStatus status, string? reason = null)
    {
        Status = status;
        if (!string.IsNullOrWhiteSpace(reason))
            Notes = string.IsNullOrWhiteSpace(Notes) ? reason : $"{Notes}\n{reason}";
        UpdatedAt = DateTime.UtcNow;
    }
}
