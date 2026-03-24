namespace Clubber.Domain.Models;

#pragma warning disable SA1649 // File name should match first type name
public readonly record struct CollectionChange<T>(IReadOnlyList<T> ItemsToAdd, IReadOnlyList<T> ItemsToRemove);
#pragma warning restore SA1649 // File name should match first type name

public record struct MilestoneInfo<TKey>(decimal TimeUntilNextMilestone, TKey NextMilestoneId);
