namespace Clubber.Domain.Models.Responses.DdInfo;

public sealed record GetWorldRecordDataContainer
{
	public required IReadOnlyCollection<GetWorldRecordHolder> WorldRecordHolders { get; init; }

	public required IReadOnlyCollection<GetWorldRecord> WorldRecords { get; init; }
}
