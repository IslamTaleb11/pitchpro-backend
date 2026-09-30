using DataAccessLayer.DTOs.Match;
using DataAccessLayer.Providers;

namespace BusinessLayer
{
    // Business-layer model for the "match call up" feature: given a category,
    // determine whether that category has an upcoming match for the current club
    // and expose the match details. Club scoping always comes from the auth token
    // via GeneralSettings.ClubID, matching every other read in the project.
    public class MatchCallUp
    {
        public int ID { get; set; }
        public int ClubID { get; set; }
        public int CategoryID { get; set; }
        public string OpponentName { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan KickoffTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsHome { get; set; }
        public string StadiumName { get; set; }
        public string ClubName { get; set; }

        // True when this instance represents a real, persisted upcoming match.
        public bool HasUpcomingMatch => ID > 0;

        private static MatchCallUp _FromDTO(MatchCallUpDTO dto)
        {
            if (dto == null)
            {
                return null;
            }

            return new MatchCallUp
            {
                ID           = dto.ID,
                ClubID       = dto.ClubID,
                CategoryID   = dto.CategoryID,
                OpponentName = dto.OpponentName,
                Date         = dto.Date,
                KickoffTime  = dto.KickoffTime,
                EndTime      = dto.EndTime,
                IsCompleted  = dto.IsCompleted,
                IsHome       = dto.IsHome,
                StadiumName  = dto.StadiumName,
                ClubName     = dto.ClubName
            };
        }

        // Returns paginated completed matches for the given category within the
        // current club.
        public static async Task<(List<MatchIncompleteByCategoryResponseDTO> Data, int TotalCount)> GetCompletedByCategoryAsync(
            int categoryId, int page, int pageSize)
        {
            return await MatchProvider.GetCompletedByCategory(categoryId, GeneralSettings.ClubID, page, pageSize);
        }

        // Returns all non-completed matches for the given category within the
        // current club.
        public static async Task<List<MatchIncompleteByCategoryResponseDTO>> GetIncompleteByCategoryAsync(int categoryId)
        {
            return await MatchProvider.GetIncompleteMatchesByCategory(categoryId, GeneralSettings.ClubID);
        }

        // Returns the nearest upcoming match for the given category within the
        // current club, or null when the category has no upcoming match.
        public static async Task<MatchCallUp> GetUpcomingByCategoryAsync(int categoryId)
        {
            MatchCallUpDTO dto = await MatchProvider.GetUpcomingMatchByCategory(categoryId, GeneralSettings.ClubID);
            return _FromDTO(dto);
        }

        // Convenience check: does the given category have an upcoming match for
        // the current club?
        public static async Task<bool> HasUpcomingMatchAsync(int categoryId)
        {
            MatchCallUp match = await GetUpcomingByCategoryAsync(categoryId);
            return match != null && match.HasUpcomingMatch;
        }
    }
}
