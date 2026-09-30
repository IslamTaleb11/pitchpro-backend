using DataAccessLayer.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;


namespace BusinessLayer
{
    public static class ClubSubscription
    {
        private static IMemoryCache _cache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(1);

        public static void InitializeCache(IMemoryCache cache)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public static IMemoryCache GetCacheInstance()
        {
            return _cache;
        }

        public static async Task<string> GetCurrentSubscriptionName(int clubID)
        {
            return await ClubSubscriptionProvider.GetCurrentSubscriptionName(clubID);
        }

        private static string _getClubExpiryDate_CacheKey(int clubID)
        {
            return $"club_expiry_{clubID}";
        }

        // Evicts the cached expiry date for a club so the next read comes fresh from the
        // database (e.g. right after the plan changes).
        public static void RemoveClubSubscriptionExpiryDateCache(int clubID)
        {
            _cache.Remove(_getClubExpiryDate_CacheKey(clubID));
        }

        // Switches a club from the Premium plan to the Free plan and clears the now-stale
        // cached expiry date.
        public static async Task<bool> DowngradeToFreePlan(int clubID)
        {
            if (clubID <= 0)
            {
                throw new ArgumentException("Invalid Club ID provided.", nameof(clubID));
            }

            bool success = await ClubSubscriptionProvider.DowngradeToFreePlan(clubID, "Free");

            if (success)
            {
                RemoveClubSubscriptionExpiryDateCache(clubID);
            }

            return success;
        }

        public static async Task<DateTime?> GetClubSubscriptionExpiryDate(int clubID)
        {
            if (clubID <= 0)
            {
                throw new ArgumentException("Invalid Club ID provided.", nameof(clubID));
            }

            string cacheKey = _getClubExpiryDate_CacheKey(clubID);

            if (!_cache.TryGetValue(cacheKey, out DateTime? expiryDate))
            {
                // Cache miss -> Fetch directly from the Data Access Layer
                expiryDate = await ClubSubscriptionProvider.GetClubSubscriptionExpiryDate(clubID);

                // Set configuration properties for the cache entity
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(CacheDuration);

                // Save payload value back to memory cache pool
                _cache.Set(cacheKey, expiryDate, cacheOptions);
            }

            return expiryDate;
        }


        public static async Task<int> GetClubSubscriptionRemainingDays(int clubID)
        {
            DateTime? startDateTime = await ClubSubscriptionProvider.GetClubSubscriptionStartDate(clubID);
            bool isStartDateInFuture = startDateTime.HasValue && startDateTime.Value > DateTime.UtcNow;

            DateTime? expiryDate = await GetClubSubscriptionExpiryDate(clubID);

            DateTime currentDateTime = DateTime.UtcNow;

            if (isStartDateInFuture)
            {
                TimeSpan? addedDays = startDateTime - currentDateTime;
                TimeSpan? difference = expiryDate.Value - startDateTime.Value;

                double? totalCombinedDays = difference.Value.TotalDays + addedDays.GetValueOrDefault().TotalDays;

                return totalCombinedDays.HasValue && totalCombinedDays.Value > 0
                ? (int)Math.Ceiling(totalCombinedDays.Value)
                : 0;
            }

            TimeSpan diff = expiryDate.Value - currentDateTime;

            return diff.Days > 0 ? (int)Math.Ceiling(diff.TotalDays) : 0;
        }
    }
}
