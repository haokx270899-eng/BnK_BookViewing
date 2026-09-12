namespace PropertyViewing.Domain.Entities;

public sealed class Property
{
    public int Id { get; set; }
    public required string Address { get; set; }
    public ICollection<Viewing> Viewings { get; } = new List<Viewing>();
}
