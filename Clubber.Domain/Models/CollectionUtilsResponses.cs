namespace Clubber.Domain.Models;

public record struct CollectionChange<T>(T[] ItemsToAdd, T[] ItemsToRemove);

public record struct MilestoneInfo<TKey>(decimal TimeUntilNextMilestone, TKey NextMilestoneId);
