using DataAccessLayer.DTOs.Dashboard;
using DataAccessLayer.Providers;
using BusinessLayer.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLayer
{
    public static class Dashboard
    {
        private static IMemoryCache GetCache() => AppServicesHelper.ServiceProvider.GetRequiredService<IMemoryCache>();

        private static string _getCacheKey()
        {
            return $"dashboardCounts_{GeneralSettings.ClubID}";
        }

        public async static Task<DashboardCountsResponseDTO> GetCounts()
        {
            string cacheKey = _getCacheKey();

            if (!GetCache().TryGetValue(cacheKey, out DashboardCountsResponseDTO counts))
            {
                counts = await DashboardProvider.GetCounts(GeneralSettings.ClubID);
                GetCache().Set(cacheKey, counts);
            }

            return counts;
        }

        public static void ClearCache()
        {
            GetCache().Remove(_getCacheKey());
        }
    }
}
