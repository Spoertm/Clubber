namespace Clubber.Domain.Models.Responses.DdInfo;

public record GetWorldRecord
{
	public required DateTime DateTime { get; init; }

	public required GetWorldRecordEntry Entry { get; init; }

	public required GameVersion? GameVersion { get; init; }

	public required TimeSpan WorldRecordDuration { get; init; }

	public required double? WorldRecordImprovement { get; init; }
}
