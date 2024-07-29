using Clubber.Domain.Models;

namespace Clubber.Domain.Helpers;

public static class CollectionUtils
{
	public static CollectionChange<T> DetermineCollectionChanges<T>(
		IReadOnlyCollection<T> sourceCollection,
		IReadOnlyCollection<T> referenceCollection,
		IReadOnlyCollection<T> itemsToRetain)
	{
		T[] itemsToAdd = itemsToRetain
			.Where(item => !sourceCollection.Contains(item) && referenceCollection.Contains(item))
			.ToArray();

		T[] itemsToRemove = sourceCollection
			.Intersect(referenceCollection)
			.Except(itemsToRetain)
			.ToArray();

		return new(itemsToAdd, itemsToRemove);
	}

	public static MilestoneInfo<TKey> GetNextMileStone<TKey>(
		int currentTime,
		IReadOnlyDictionary<int, TKey> milestones)
	{
		(int score, _) = milestones.FirstOrDefault(m => m.Key <= currentTime / 10_000);
		if (score == milestones.Keys.Max())
		{
			return default;
		}

		(int nextScore, TKey nextMilestoneId) = milestones.Last(m => m.Key > currentTime / 10_000);
		decimal timeUntilNextMilestone = nextScore - currentTime / 10_000M;
		return new(timeUntilNextMilestone, nextMilestoneId);
	}
}
