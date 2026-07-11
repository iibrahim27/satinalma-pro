namespace SatinalmaPro.Shared.Models;

public sealed class NotificationInboxEntry
{
    public string DocId { get; set; } = "";
    public string EventCode { get; set; } = "";
    public string Category { get; set; } = "";
    public string Type { get; set; } = "";
    public string Tip { get; set; } = "";
    public string Priority { get; set; } = "";
    public string Title { get; set; } = "";
    public string Baslik { get; set; } = "";
    public string Message { get; set; } = "";
    public string Mesaj { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string TalepId { get; set; } = "";
    public string DeepLink { get; set; } = "";
    public string DesktopRoute { get; set; } = "";
    public string Module { get; set; } = "";
    public string Screen { get; set; } = "";
    public string Action { get; set; } = "";
    public string HedefRol { get; set; } = "";
    public string TargetRole { get; set; } = "";
    public string HedefUid { get; set; } = "";
    public string TargetUid { get; set; } = "";
    public string OlusturanUid { get; set; } = "";
    public string CreatedBy { get; set; } = "";
    public bool IsRead { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? DismissedAt { get; set; }
    public DateTime? CreatedAt { get; set; }

    public bool IsDismissed => DismissedAt.HasValue || IsArchived;
}
