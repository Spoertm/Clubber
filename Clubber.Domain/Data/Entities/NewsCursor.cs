namespace Clubber.Domain.Data.Entities;

public sealed class NewsCursor
{
    public int Id { get; set; }

    public DateTimeOffset LastCheckedAt { get; set; }
}
