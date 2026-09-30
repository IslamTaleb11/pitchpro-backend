using Azure.Core;
using BusinessLayer.Exceptions;
using BusinessLayer.Helpers;
using DataAccessLayer.DTOs.MatchAttendance;
using DataAccessLayer.Providers;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace BusinessLayer
{
    // Records a player's attendance for a match. The call-up player id is validated
    // to belong to the current club (via the match it links to) before any row is
    // written, so one club cannot record attendance for another's players. All
    // attendance rows for a single Mark call are committed atomically inside a
    // transaction, so a failure mid-way rolls back every insert. recorded_at is
    // set server-side (UTC) so timestamps are consistent regardless of client clock.
    public class MatchAttendance
    {
        private static async Task<int> Mark(int? matchCallUpPlayerId, bool status)
        {
            if (matchCallUpPlayerId == null || matchCallUpPlayerId <= 0)
                throw new InvalidCallUpDataException("Invalid call-up player id.");

            int clubId = GeneralSettings.ClubID;

            bool belongs = await MatchAttendanceProvider.CallUpPlayerBelongsToClub(matchCallUpPlayerId, clubId);
            if (!belongs)
                throw new InvalidCallUpDataException("The selected call-up player does not belong to this club.");

            var saveDto = new MatchAttendanceSaveDTO
            {
                MatchCallUpPlayerID = matchCallUpPlayerId,
                Status = status,
                RecordedAt = DateTime.UtcNow
            };

            int existingId = await MatchAttendanceProvider.IsAlreadyExist(matchCallUpPlayerId, clubId);
            if (existingId != -1)
            {
                await MatchAttendanceProvider.UpdateAttendance(saveDto);
                return existingId;
            }

            return await MatchAttendanceProvider.MarkAttendance(saveDto);
        }

        public static async Task<Dictionary<int, int?>> GetCallUpPlayerId(Dictionary<int, bool> PlayersAttendance, int MatchID)
        {
            Dictionary<int, int?> PlayersIds_MatchCallUpIds = new Dictionary<int, int?>();

            foreach (var pair in PlayersAttendance)
            {
                int? matchCallUpPlayerID = await MatchCallUpPlayerProvider.GetCallUpPlayerId(MatchID, pair.Key, GeneralSettings.ClubID);

                if (matchCallUpPlayerID != null)
                {
                    PlayersIds_MatchCallUpIds.Add(pair.Key, matchCallUpPlayerID);
                    continue;
                }
                throw new InvalidCallUpDataException($"Player with ID {pair.Key} is not in the call-up for this match.");
            }

            return PlayersIds_MatchCallUpIds;
        }
    
        
        public static async Task<Dictionary<int, int>> Mark(Dictionary<int, int?> PlayersIds_MatchCallUpIds, Dictionary<int, bool> PlayersAttendance)
        {
            var attendanceIds = new Dictionary<int, int>();

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                foreach (var pair in PlayersIds_MatchCallUpIds)
                {
                    int attendanceId = await Mark(pair.Value, PlayersAttendance[pair.Key]);
                    if (attendanceId == -1)
                    {
                        throw new InvalidCallUpDataException("Failed to record attendance for a player.");
                    }
                    attendanceIds[pair.Key] = attendanceId;
                }

                scope.Complete();
            }

            return attendanceIds;
        }

        // Builds a dictionary of { playerId -> matchCallUpPlayerId } for every
        // player in the given list. Throws if any player is not in the call-up for
        // the match. Club scoping is enforced via GeneralSettings.ClubID in the
        // provider's SQL join, so only the current club's call-up data is visible.
        public static async Task<List<int?>> GetCallUpPlayerIdByPlayerIds(int matchId, List<int> playerIds)
        {
            List<int?> matchCallUpPlayerIds = new List<int?>();
            int clubId = GeneralSettings.ClubID;

            foreach (int playerId in playerIds)
            {
                int? matchCallUpPlayerId = await MatchCallUpPlayerProvider.GetCallUpPlayerId(matchId, playerId, clubId);
                if (matchCallUpPlayerId == null)
                    throw new InvalidCallUpDataException($"Player with ID {playerId} is not in the call-up for this match.");
                matchCallUpPlayerIds.Add(matchCallUpPlayerId);
            }

            return matchCallUpPlayerIds;
        }

        // Returns all called-up players for a match with their attendance status.
        // Club and category scoping is enforced server-side.
        public static async Task<List<MatchAttendanceByMatchResponseDTO>> GetAttendanceByMatchAsync(
            int matchId, int categoryId)
        {
            int clubId = GeneralSettings.ClubID;

            bool exists = await MatchProvider.Exists(matchId, clubId);
            if (!exists)
                throw new KeyNotFoundException("Match not found or you don't have permission to access it.");

            return await MatchAttendanceProvider.GetAttendanceByMatch(matchId, categoryId, clubId);
        }

        // Resets attendance for the given players in a match. First resolves every
        // playerId to its match_callup_players_id (building the dictionary), then
        // deletes all attendance rows for those ids in a single transaction.
        // Returns the number of rows deleted.
        public static async Task<int> Reset(int matchId, List<int> playerIds)
        {
            var matchCallUpPlayerIds = await GetCallUpPlayerIdByPlayerIds(matchId, playerIds);

            if (matchCallUpPlayerIds.Count == 0)
                return 0;

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                int deleted = await MatchAttendanceProvider.DeleteByCallUpPlayerIds(matchCallUpPlayerIds);
                scope.Complete();
                return deleted;
            }
        }
    }
}
