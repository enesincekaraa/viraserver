using Vira.Shared.Base;

namespace Vira.Domain.Entities;

public enum RequestStatus { Open = 0, Assigned = 1, Resolved = 2, Rejected = 3 }
public class Request : AuditableEntity<Guid>
{

    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public Guid? CategoryId { get; private set; }
    public RequestStatus Status { get; private set; } = RequestStatus.Open;

    public Guid CreatedByUserId { get; private set; }
    public Guid? AssignedToUserId { get; private set; }

    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    private Request() { }

    public Request(string title, Guid createdByUserId, double latitude, double longitude, string? description = null, Guid? categoryId = null)
    {
        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        CategoryId = categoryId;
        CreatedByUserId = createdByUserId;
        Latitude = latitude;
        Longitude = longitude;
        CreatedAt = DateTime.UtcNow;
    }

    public void AssignToUser(Guid? userId)
    {
        AssignedToUserId = userId;
        Status = RequestStatus.Assigned;
        UpdatedAt = DateTime.UtcNow;
    }
    public void Resolve()
    {
        Status = RequestStatus.Resolved;
        UpdatedAt = DateTime.UtcNow;
    }
    public void Reject()
    {
        Status = RequestStatus.Rejected;
        UpdatedAt = DateTime.UtcNow;
    }

}
