namespace Vira.Contracts.Assists;

public static class AssistsDtos
{
    public sealed record CreateAssistRequest(
        int Type, string ElderFullName, string? ElderPhone,
        string Address, double Latitude, double Longitude,
        DateTime? ScheduledAtUtc, string? Notes);

    public sealed record AssistResponse(
        Guid Id, int Type, int Status, Guid CreatedByUserId,
        string ElderFullName, string? ElderPhone, string Address,
        double Latitude, double Longitude, Guid? AssignedToUserId,
        DateTime? ScheduledAtUtc, string? Notes, DateTime CreatedAtUtc);

    public sealed record AssistListItem(
        Guid Id, int Type, int Status, string ElderFullName,
        string Address, DateTime CreatedAtUtc, Guid? AssignedToUserId);

    public sealed record AssignAssistRequest(Guid AssignedToUserId);
    public sealed record ChangeStatusRequest(int Status, string? Reason);
}
