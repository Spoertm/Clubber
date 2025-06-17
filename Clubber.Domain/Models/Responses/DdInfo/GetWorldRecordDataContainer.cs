namespace Clubber.Domain.Models.Responses.DdInfo;

public record GetWorldRecordDataContainer
{
	public required List<GetWorldRecordHolder> WorldRecordHolders { get; init; }

	public required List<GetWorldRecord> WorldRecords { get; init; }
}
