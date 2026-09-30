using DataAccessLayer.DTOs.MatchCallUpPlayer;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Threading.Tasks;

namespace DataAccessLayer.Providers
{
    public class MatchCallUpPlayerProvider
    {
        // True when the given match exists in the given category for the club.
        public static async Task<bool> MatchBelongsToCategory(int matchId, int categoryId, int clubId)
        {
            const string sql = @"SELECT TOP 1 1
                                 FROM matches
                                 WHERE id = @MatchID AND category_id = @CategoryID AND club_id = @ClubID;";

            using (var connection = new SqlConnection(DataAccessSettings.connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@MatchID", SqlDbType.Int).Value = matchId;
                command.Parameters.Add("@CategoryID", SqlDbType.Int).Value = categoryId;
                command.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return result != null;
            }
        }

        // True when the given player is linked to the given category for the club.
        public static async Task<bool> PlayerBelongsToCategory(int playerId, int categoryId, int clubId)
        {
            const string sql = @"SELECT TOP 1 1
                                 FROM players_categories pc
                                 INNER JOIN players pl ON pc.player_id = pl.id
                                 INNER JOIN persons pr ON pl.person_id = pr.id
                                 WHERE pc.player_id = @PlayerID AND pc.category_id = @CategoryID AND pr.club_id = @ClubID;";

            using (var connection = new SqlConnection(DataAccessSettings.connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@PlayerID", SqlDbType.Int).Value = playerId;
                command.Parameters.Add("@CategoryID", SqlDbType.Int).Value = categoryId;
                command.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return result != null;
            }
        }

        // True when the player is already in the call-up for the match.
        public static async Task<bool> PairingExists(int matchId, int playerId)
        {
            const string sql = @"SELECT TOP 1 1
                                 FROM match_callup_players
                                 WHERE match_id = @MatchID AND player_id = @PlayerID;";

            using (var connection = new SqlConnection(DataAccessSettings.connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@MatchID", SqlDbType.Int).Value = matchId;
                command.Parameters.Add("@PlayerID", SqlDbType.Int).Value = playerId;

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return result != null;
            }
        }

        // Inserts a match_id / player_id pairing into match_callup_players and
        // returns the new identity, or -1 on failure. Club scoping is guaranteed
        // upstream by the MatchBelongsToCategory / PlayerBelongsToCategory checks.
        public static async Task<int> AddNew(MatchCallUpPlayerSaveDTO dto, MatchCallUpPlayerResponseDTO responseDTO)
        {
            int newId = -1;

            const string sql = @"INSERT INTO match_callup_players (match_id, player_id)
                                 VALUES (@MatchID, @PlayerID);
                                 SELECT CAST(scope_identity() AS int);";

            using (var connection = new SqlConnection(DataAccessSettings.connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@MatchID", SqlDbType.Int).Value = dto.MatchID;
                command.Parameters.Add("@PlayerID", SqlDbType.Int).Value = dto.PlayerID;

                try
                {
                    await connection.OpenAsync();

                    var result = await command.ExecuteScalarAsync();
                    if (result != null)
                    {
                        newId = Convert.ToInt32(result);

                        responseDTO.ID = newId;
                        responseDTO.MatchID = dto.MatchID;
                        responseDTO.PlayerID = dto.PlayerID;
                    }
                }
                catch (SqlException ex)
                {
                    throw new Exception("Database error occurred while adding the player to the match call-up.", ex);
                }
            }

            return newId;
        }

        // Deletes every match_callup_players row for the given match, but only
        // when the match belongs to the supplied club (enforced by the INNER JOIN
        // on matches). A foreign match_id produces no join rows, so nothing is
        // deleted and 0 is returned rather than touching another club's data.
        // Deletes every match_callup_players row for the given match (and any
        // matches_attendance rows that reference them), but only when the match
        // belongs to the supplied club (enforced by the INNER JOIN on matches).
        // A foreign match_id produces no join rows, so nothing is deleted and 0
        // is returned rather than touching another club's data.
        // Returns the number of call-up player rows removed.
        public static async Task<int> DeleteByMatch(int matchId, int clubId)
        {
            const string sql = @"DELETE ma
                                 FROM matches_attendance ma
                                 INNER JOIN match_callup_players mcp ON ma.match_callup_players_id = mcp.id
                                 INNER JOIN matches m ON mcp.match_id = m.id AND m.club_id = @ClubID
                                 WHERE mcp.match_id = @MatchID;

                                 DELETE mcp
                                 FROM match_callup_players mcp
                                 INNER JOIN matches m ON mcp.match_id = m.id AND m.club_id = @ClubID
                                 WHERE mcp.match_id = @MatchID;";

            using (var connection = new SqlConnection(DataAccessSettings.connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@MatchID", SqlDbType.Int).Value = matchId;
                command.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;

                try
                {
                    await connection.OpenAsync();
                    int rows = await command.ExecuteNonQueryAsync();
                    return rows;
                }
                catch (SqlException ex)
                {
                    throw new Exception("Database error occurred while resetting the match call-up.", ex);
                }
            }
        }

        // Counts how many players are called up for a match, scoped to the club
        // for security: the match is only joined (and therefore only its call-up
        // rows are counted) when it belongs to the current club. A match_id that
        // belongs to another club produces no join rows, so the count is 0 rather
        // than leaking another club's data. match_callup_players has no club_id
        // column of its own, so club scoping is enforced through the matches join.
        public static async Task<int> CountByMatch(int matchId, int clubId)
        {
            const string sql = @"SELECT COUNT(*)
                                 FROM match_callup_players mcp
                                 INNER JOIN matches m ON mcp.match_id = m.id AND m.club_id = @ClubID
                                 WHERE mcp.match_id = @MatchID;";

            using (var connection = new SqlConnection(DataAccessSettings.connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@MatchID", SqlDbType.Int).Value = matchId;
                command.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }

        // Gets the MatchCallUpPlayerID by player_id and match_id. Club scoping is
        // enforced through the matches join in the provided SQL, so a player_id that
        // belongs to a different club produces no result (null) rather than leaking
        // another club's call-up data.
        // True when the match has is_completed = 1 for the given club.
        public static async Task<bool> MatchIsCompleted(int matchId, int clubId)
        {
            try
            {
                const string sql = @"SELECT TOP 1 1
                                     FROM matches
                                     WHERE id = @MatchID AND club_id = @ClubID AND is_completed = 1;";

                using (var connection = new SqlConnection(DataAccessSettings.connectionString))
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@MatchID", SqlDbType.Int).Value = matchId;
                    command.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;

                    await connection.OpenAsync();
                    var result = await command.ExecuteScalarAsync();
                    return result != null;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error while checking if the match is completed.", ex);
            }
        }

        public static async Task<int?> GetCallUpPlayerId(int matchId, int playerId, int clubId)
        {
            const string sql = @"SELECT mcp.id
                                 FROM match_callup_players mcp
                                 INNER JOIN matches m ON mcp.match_id = m.id AND m.club_id = @ClubID
                                 WHERE mcp.match_id = @MatchID AND mcp.player_id = @PlayerID;";

            using (var connection = new SqlConnection(DataAccessSettings.connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@MatchID", SqlDbType.Int).Value = matchId;
                command.Parameters.Add("@PlayerID", SqlDbType.Int).Value = playerId;
                command.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return result != null ? Convert.ToInt32(result) : (int?)null;
            }
        }



    }
}
