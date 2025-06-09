namespace Clubber.Domain.Models;

public readonly record struct CollectionChange<T>(IReadOnlyList<T> ItemsToAdd, IReadOnlyList<T> ItemsToRemove);

public record struct MilestoneInfo<TKey>(decimal TimeUntilNextMilestone, TKey NextMilestoneId);
