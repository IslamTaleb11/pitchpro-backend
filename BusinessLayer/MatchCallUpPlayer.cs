using BusinessLayer.Exceptions;
using BusinessLayer.Helpers;
using DataAccessLayer.DTOs.MatchCallUpPlayer;
using DataAccessLayer.DTOs.Player;
using DataAccessLayer.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessLayer
{
    // Adds one or more players to a match call-up in a single request. Any
    // problem with the match or a player is a hard failure: an InvalidCallUpDataException
    // is thrown immediately and the request aborts (it does not skip and continue).
    // A player already in the call-up is the only case that is silently skipped.
    // Returns the number of players actually inserted.
    //
    // The per-match call-up count is cached (keyed by club + match). The cache
    // for a match is dropped whenever players are called up for it (see AddRange),
    // so the next count read recomputes the fresh total.
    public class MatchCallUpPlayer
    {
        // Maximum number of players that can be called up for a single match.
        // Mirrors a football match-day squad cap (11 starters + 12 subs).
        private const int MaxCallUpPlayers = 23;

        private static IMemoryCache GetCache() =>
            AppServicesHelper.ServiceProvider.GetRequiredService<IMemoryCache>();

        // Tracks the count cache keys per club so they can be invalidated.
        private readonly static ConcurrentDictionary<int, HashSet<string>> _countCacheKeys = new();
        private static readonly object _countCacheLock = new();

        private static string _getCountCacheKey(int matchId)
        {
            return $"matchCallUpCount_{GeneralSettings.ClubID}_{matchId}";
        }

        public static async Task<int> AddRange(int matchId, int categoryId, IEnumerable<int> playerIds)
        {
            // A call-up mutates this match's call-up count, so drop the cached
            // total up front. Whether the insert succeeds or fails partway, the
            // next count read will recompute the correct value.
            _RemoveCountCache(matchId);

            int clubId = GeneralSettings.ClubID;

            bool matchOk = await MatchCallUpPlayerProvider.MatchBelongsToCategory(matchId, categoryId, clubId);
            if (!matchOk)
                throw new InvalidCallUpDataException("The selected match does not belong to this category.");

            // Reject outright when the call-up is already at (or past) the cap, so
            // we never start inserting into a full squad.
            bool matchCompleted = await MatchCallUpPlayerProvider.MatchIsCompleted(matchId, clubId);
            if (matchCompleted)
                throw new InvalidCallUpDataException("Cannot call up players for a completed match.");

            int currentCount = await CountByMatch(matchId);
            if (currentCount >= MaxCallUpPlayers)
                throw new InvalidCallUpDataException(
                    $"The match call-up is full. A maximum of {MaxCallUpPlayers} players can be called up.");

            int added = 0;

            foreach (int playerId in playerIds.Distinct())
            {
                // Invalid id -> fail the whole request.
                if (playerId <= 0)
                    throw new InvalidCallUpDataException("Invalid player id.");

                // Player not in the category -> fail the whole request.
                bool playerOk = await MatchCallUpPlayerProvider.PlayerBelongsToCategory(playerId, categoryId, clubId);
                if (!playerOk)
                    throw new InvalidCallUpDataException("The selected player does not belong to this category.");

                // Already in the call-up -> skip, don't fail.
                bool alreadyCalledUp = await MatchCallUpPlayerProvider.PairingExists(matchId, playerId);
                if (alreadyCalledUp)
                    continue;

                // Guard against exceeding the cap mid-batch: if adding this player
                // would push the squad over the limit, stop before inserting.
                if (currentCount + added >= MaxCallUpPlayers)
                    throw new InvalidCallUpDataException(
                        $"The match call-up is full. A maximum of {MaxCallUpPlayers} players can be called up.");

                var saveDto = new MatchCallUpPlayerSaveDTO
                {
                    MatchID = matchId,
                    PlayerID = playerId
                };
                var responseDto = new MatchCallUpPlayerResponseDTO();

                int newId = await MatchCallUpPlayerProvider.AddNew(saveDto, responseDto);
                if (newId > 0)
                    added++;
            }

            return added;
        }

        // Deletes every player pairing for a match within the current club and
        // returns the number of rows removed. Club scoping is enforced in the
        // provider's SQL join, so a match_id that belongs to another club deletes
        // nothing and returns 0. The cached call-up count for the match is
        // invalidated so the next read recomputes the fresh (empty) total.
        public static async Task<int> ResetByMatch(int matchId)
        {
            _RemoveCountCache(matchId);

            int clubId = GeneralSettings.ClubID;

            int deleted = await MatchCallUpPlayerProvider.DeleteByMatch(matchId, clubId);

            return deleted;
        }

        // Returns how many players are called up for a match within the current
        // club (club scoping comes from the auth token via GeneralSettings.ClubID).
        // Result is cached per club + match; invalidated by AddRange.
        public static async Task<int> CountByMatch(int matchId)
        {
            string key = _getCountCacheKey(matchId);

            if (GetCache().TryGetValue(key, out int cachedCount))
                return cachedCount;

            int count = await MatchCallUpPlayerProvider.CountByMatch(matchId, GeneralSettings.ClubID);

            GetCache().Set(key, count);

            lock (_countCacheLock)
            {
                var keys = _countCacheKeys.GetOrAdd(GeneralSettings.ClubID, _ => new HashSet<string>());
                keys.Add(key);
            }

            return count;
        }

        // Returns every player in the given category who is available for a call-up
        // — i.e. has no active injury (either no injury records, or all have
        // is_active = 0). Also indicates whether each player is already called up
        // for the specified match. Club scoping comes from the auth token.
        public static async Task<List<AvailablePlayerByCategoryResponseDTO>> GetAvailablePlayersByCategory(int categoryId, int matchId)
        {
            return await PlayerProvider.GetAvailablePlayersByCategory(categoryId, GeneralSettings.ClubID, matchId);
        }

        // Drops the cached call-up count for a single match within the current club.
        private static void _RemoveCountCache(int matchId)
        {
            string key = _getCountCacheKey(matchId);
            GetCache().Remove(key);

            lock (_countCacheLock)
            {
                if (_countCacheKeys.TryGetValue(GeneralSettings.ClubID, out var keys))
                    keys.Remove(key);
            }
        }
    }
}
