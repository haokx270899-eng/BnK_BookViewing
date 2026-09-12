namespace PropertyViewing.Domain.Entities;

public sealed class User
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Email { get; set; }
    public ICollection<Viewing> Viewings { get; } = new List<Viewing>();
}
