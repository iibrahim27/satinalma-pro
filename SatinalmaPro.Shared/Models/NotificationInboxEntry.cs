namespace SatinalmaPro.Shared.Models;

public sealed class NotificationInboxEntry
{
    public string DocId { get; set; } = "";
    public string EventCode { get; set; } = "";
    public string Category { get; set; } = "";
    public string Type { get; set; } = "";
    public string Priority { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string DeepLink { get; set; } = "";
    public string DesktopRoute { get; set; } = "";
    public string Module { get; set; } = "";
    public string Screen { get; set; } = "";
    public string Action { get; set; } = "";
    public bool IsRead { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? DismissedAt { get; set; }
    public DateTime? CreatedAt { get; set; }

    public bool IsDismissed => DismissedAt.HasValue || IsArchived;
}
