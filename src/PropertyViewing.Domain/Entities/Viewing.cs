namespace PropertyViewing.Domain.Entities;

public sealed class Viewing
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public int UserId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public Property? Property { get; set; }
    public User? User { get; set; }
}
