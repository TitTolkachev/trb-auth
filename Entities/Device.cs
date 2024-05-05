namespace trb_auth.Entities;

public class Device
{
    public Guid Id { get; set; }
    public string? DeviceId { get; set; }
    public string? UserId { get; set; }
    public string? App { get; set; }
}