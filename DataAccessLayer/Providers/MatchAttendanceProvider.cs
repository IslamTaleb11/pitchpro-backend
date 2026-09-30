using DataAccessLayer.DTOs.MatchAttendance;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Providers
{
    public class MatchAttendanceProvider
    {
        public static async Task<bool> CallUpPlayerBelongsToClub(int? matchCallUpPlayerId, int clubId)
        {
            const string sql = @"SELECT TOP 1 1
                                 FROM match_callup_players
                                 INNER JOIN matches ON match_callup_players.match_id = matches.id
                                 WHERE match_callup_players.id = @MatchCallUpPlayerID AND matches.club_id = @ClubID;";

            using (var connection = new SqlConnection(DataAccessSettings.connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@MatchCallUpPlayerID", SqlDbType.Int).Value = matchCallUpPlayerId;
                command.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return result != null;
            }
        }

        // Inserts a single attendance row for the given call-up player. The foreign
        // key on match_callup_players_id rejects ids that don't exist. Returns the new id.
        public static async Task<int> MarkAttendance(MatchAttendanceSaveDTO dto)
        {
            int newId = -1;

            const string sql = @"INSERT INTO matches_attendance (match_callup_players_id, status, recorded_at)
                                 VALUES (@MatchCallUpPlayerID, @Status, @RecordedAt);
                                 SELECT CAST(scope_identity() AS int);";

            using (var connection = new SqlConnection(DataAccessSettings.connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@MatchCallUpPlayerID", SqlDbType.Int).Value = dto.MatchCallUpPlayerID;
                command.Parameters.Add("@Status", SqlDbType.Bit).Value = dto.Status;
                command.Parameters.Add("@RecordedAt", SqlDbType.DateTime).Value = dto.RecordedAt;

                try
                {
                    await connection.OpenAsync();
                    var result = await command.ExecuteScalarAsync();
                    if (result != null)
                        newId = Convert.ToInt32(result);
                }
                catch (SqlException ex)
                {
                    throw new Exception("Database error occurred while recording match attendance.", ex);
                }
            }

            return newId;
        }

        // Deletes every attendance row whose match_callup_players_id is in the
        // supplied list and returns the number of rows removed.
        public static async Task<int> DeleteByCallUpPlayerIds(List<int?> matchCallUpPlayerIds)
        {
            if (matchCallUpPlayerIds == null || matchCallUpPlayerIds.Count == 0)
                return 0;

            var ids = matchCallUpPlayerIds.Distinct().ToList();
            var parameters = new List<SqlParameter>();
            var inClause = new List<string>();

            for (int i = 0; i < ids.Count; i++)
            {
                string paramName = $"@Id{i}";
                inClause.Add(paramName);
                parameters.Add(new SqlParameter(paramName, SqlDbType.Int) { Value = ids[i] });
            }

            string sql = $@"DELETE FROM matches_attendance
                             WHERE match_callup_players_id IN ({string.Join(", ", inClause)});";

            using (var connection = new SqlConnection(DataAccessSettings.connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddRange(parameters.ToArray());

                try
                {
                    await connection.OpenAsync();
                    int rows = await command.ExecuteNonQueryAsync();
                    return rows;
                }
                catch (SqlException ex)
                {
                    throw new Exception("Database error occurred while resetting match attendance.", ex);
                }
            }
        }


        public static async Task<int> IsAlreadyExist(int? matchCallUpPlayerID, int clubID)
        {
            const string sql = @"
                                SELECT ma.id
                                FROM matches_attendance ma
                                INNER JOIN match_callup_players mcp
                                    ON ma.match_callup_players_id = mcp.id
                                INNER JOIN matches m
                                    ON mcp.match_id = m.id
                                WHERE m.club_id = @ClubID
                                  AND ma.match_callup_players_id = @MatchCallUpPlayerID;";

            using var connection = new SqlConnection(DataAccessSettings.connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@MatchCallUpPlayerID", SqlDbType.Int).Value = matchCallUpPlayerID;
            command.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubID;

            try
            {
                await connection.OpenAsync();

                object? result = await command.ExecuteScalarAsync();

                return result != null && result != DBNull.Value
                    ? Convert.ToInt32(result)
                    : -1;
            }
            catch (SqlException ex)
            {
                throw new Exception("A database error occurred while checking the match attendance.", ex);
            }
        }

        public static async Task<int> UpdateAttendance(MatchAttendanceSaveDTO dto)
        {
            const string sql = @"UPDATE matches_attendance
                                 SET status = @Status, recorded_at = @RecordedAt
                                 WHERE match_callup_players_id = @MatchCallUpPlayerID;";

            using var connection = new SqlConnection(DataAccessSettings.connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@MatchCallUpPlayerID", SqlDbType.Int).Value = dto.MatchCallUpPlayerID;
            command.Parameters.Add("@Status", SqlDbType.Bit).Value = dto.Status;
            command.Parameters.Add("@RecordedAt", SqlDbType.DateTime).Value = dto.RecordedAt;

            try
            {
                await connection.OpenAsync();
                return await command.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                throw new Exception("Database error occurred while updating match attendance.", ex);
            }
        }

        // Returns all called-up players for a match with their attendance status.
        // Club and category scoping is enforced through the matches join.
        public static async Task<List<MatchAttendanceByMatchResponseDTO>> GetAttendanceByMatch(
            int matchId, int categoryId, int clubId)
        {
            try
            {
                var list = new List<MatchAttendanceByMatchResponseDTO>();

                const string sql = @"
                    SELECT
                        pl.id AS playerId,
                        CONCAT(pr.first_name, ' ', pr.second_name, ' ', pr.last_name) AS playerName,
                        pl.jersey_number AS jerseyNumber,
                        pp.name AS positionName,
                        pl.photo AS playerImage,
                        ma.status AS attended
                    FROM matches_attendance ma
                    INNER JOIN match_callup_players mcp ON ma.match_callup_players_id = mcp.id
                    INNER JOIN players pl ON mcp.player_id = pl.id
                    INNER JOIN persons pr ON pl.person_id = pr.id
                    INNER JOIN primary_positions pp ON pl.primary_position_id = pp.id
                    INNER JOIN matches m ON mcp.match_id = m.id
                    WHERE mcp.match_id = @MatchID
                      AND m.category_id = @CategoryID
                      AND m.club_id = @ClubID
                    ORDER BY TRY_CAST(pl.jersey_number AS INT), pl.jersey_number;";

                using (var conn = new SqlConnection(DataAccessSettings.connectionString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@MatchID", SqlDbType.Int).Value = matchId;
                    cmd.Parameters.Add("@CategoryID", SqlDbType.Int).Value = categoryId;
                    cmd.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;

                    await conn.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new MatchAttendanceByMatchResponseDTO
                            {
                                PlayerID = reader.GetInt32(reader.GetOrdinal("playerId")),
                                PlayerName = reader.GetString(reader.GetOrdinal("playerName")),
                                JerseyNumber = reader.GetString(reader.GetOrdinal("jerseyNumber")),
                                PositionName = reader.GetString(reader.GetOrdinal("positionName")),
                                PlayerImage = reader.IsDBNull(reader.GetOrdinal("playerImage"))
                                    ? string.Empty
                                    : reader.GetString(reader.GetOrdinal("playerImage")),
                                Attended = reader.GetBoolean(reader.GetOrdinal("attended"))
                            });
                        }
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                throw new Exception("Database error while fetching match attendance data.", ex);
            }
        }
    }
}