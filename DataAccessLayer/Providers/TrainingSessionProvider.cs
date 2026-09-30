using DataAccessLayer.DTOs.TrainingSession;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DataAccessLayer.Providers
{
    public class TrainingSessionProvider
    {
        public static async Task<int> CountSessionsByClubID(int clubID)
        {
            const string sql = @"SELECT COUNT(*)
                                 FROM training_sessions
                                 WHERE club_id = @ClubID;";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubID;

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return result != null ? Convert.ToInt32(result) : 0;
            }
        }

        public static int AddNew(TrainingSessionRegistrationSaveDTO dto, TrainingSessionRegistrationResponseDTO responseDTO)
        {
            int newId = -1;

            string sql = @"INSERT INTO training_sessions (club_id, category_id, date, start_time, end_time, location, session_type_id)
                           VALUES (@ClubID, @CategoryID, @Date, @StartTime, @EndTime, @Location, @SessionTypeID);
                           SELECT CAST(scope_identity() AS int);";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@ClubID",        SqlDbType.Int).Value           = dto.ClubID;
                    command.Parameters.Add("@CategoryID",    SqlDbType.Int).Value           = dto.CategoryID;
                    command.Parameters.Add("@Date",          SqlDbType.Date).Value          = dto.Date;
                    command.Parameters.Add("@StartTime",     SqlDbType.Time).Value          = dto.StartTime;
                    command.Parameters.Add("@EndTime",       SqlDbType.Time).Value          = dto.EndTime;
                    command.Parameters.Add("@Location",      SqlDbType.NVarChar, -1).Value  = dto.Location;
                    command.Parameters.Add("@SessionTypeID", SqlDbType.Int).Value           = dto.SessionTypeID;

                    try
                    {
                        connection.Open();

                        var result = command.ExecuteScalar();
                        if (result != null)
                        {
                            newId = Convert.ToInt32(result);

                            responseDTO.ID            = newId;
                            responseDTO.ClubID        = dto.ClubID;
                            responseDTO.CategoryID    = dto.CategoryID;
                            responseDTO.Date          = dto.Date;
                            responseDTO.StartTime     = dto.StartTime;
                            responseDTO.EndTime       = dto.EndTime;
                            responseDTO.Location      = dto.Location;
                            responseDTO.SessionTypeID = dto.SessionTypeID;
                        }
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while adding a new training session.", ex);
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }

            return newId;
        }

        public static async Task<bool> Update(TrainingSessionEditSaveDTO dto)
        {
            string sql = @"UPDATE training_sessions 
                           SET club_id = @ClubID, 
                               category_id = @CategoryID, 
                               date = @Date, 
                               start_time = @StartTime, 
                               end_time = @EndTime, 
                               location = @Location, 
                               session_type_id = @SessionTypeID
                           WHERE id = @ID AND club_id = @ClubID;";

            using (var conn = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@ID",            SqlDbType.Int).Value          = dto.ID;
                    cmd.Parameters.Add("@ClubID",        SqlDbType.Int).Value          = dto.ClubID;
                    cmd.Parameters.Add("@CategoryID",    SqlDbType.Int).Value          = dto.CategoryID;
                    cmd.Parameters.Add("@Date",          SqlDbType.Date).Value         = dto.Date;
                    cmd.Parameters.Add("@StartTime",     SqlDbType.Time).Value         = dto.StartTime;
                    cmd.Parameters.Add("@EndTime",       SqlDbType.Time).Value         = dto.EndTime;
                    cmd.Parameters.Add("@Location",      SqlDbType.NVarChar, -1).Value  = dto.Location;
                    cmd.Parameters.Add("@SessionTypeID", SqlDbType.Int).Value           = dto.SessionTypeID;

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }
        // Returns paginated completed training sessions for a category within a
        // club, plus the total count of completed sessions for that category.
        public static async Task<(List<TrainingSessionByCategoryResponseDTO> Data, int TotalCount)> GetCompletedByCategory(
            int categoryId, int clubId, int page, int pageSize)
        {
            try
            {
                var list = new List<TrainingSessionByCategoryResponseDTO>();

                string countSql = @"SELECT COUNT(*)
                                    FROM training_sessions
                                    WHERE club_id = @ClubID
                                      AND category_id = @CategoryID
                                      AND is_completed = 1;";

                string dataSql = @"SELECT ts.id, ts.club_id, ts.category_id, ts.date,
                                          ts.start_time, ts.end_time, ts.location,
                                          st.name AS session_type_name
                                   FROM training_sessions ts
                                   INNER JOIN training_session_types st ON ts.session_type_id = st.id
                                   WHERE ts.club_id = @ClubID
                                     AND ts.category_id = @CategoryID
                                     AND ts.is_completed = 1
                                   ORDER BY ts.date DESC, ts.start_time DESC
                                   OFFSET @Offset ROWS
                                   FETCH NEXT @PageSize ROWS ONLY;";

                using (var conn = new SqlConnection(DataAccessSettings.connectionString))
                {
                    await conn.OpenAsync();

                    using (var countCmd = new SqlCommand(countSql, conn))
                    {
                        countCmd.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;
                        countCmd.Parameters.Add("@CategoryID", SqlDbType.Int).Value = categoryId;

                        var totalResult = await countCmd.ExecuteScalarAsync();
                        int totalCount = totalResult != null ? Convert.ToInt32(totalResult) : 0;

                        using (var dataCmd = new SqlCommand(dataSql, conn))
                        {
                            dataCmd.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;
                            dataCmd.Parameters.Add("@CategoryID", SqlDbType.Int).Value = categoryId;
                            dataCmd.Parameters.Add("@Offset", SqlDbType.Int).Value = (page - 1) * pageSize;
                            dataCmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;

                            using (var reader = await dataCmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    list.Add(new TrainingSessionByCategoryResponseDTO
                                    {
                                        ID = Convert.ToInt32(reader["id"]),
                                        ClubID = Convert.ToInt32(reader["club_id"]),
                                        CategoryID = Convert.ToInt32(reader["category_id"]),
                                        Date = Convert.ToDateTime(reader["date"]),
                                        StartTime = reader["start_time"] != DBNull.Value ? (TimeSpan)reader["start_time"] : TimeSpan.Zero,
                                        EndTime = reader["end_time"] != DBNull.Value ? (TimeSpan)reader["end_time"] : TimeSpan.Zero,
                                        Location = reader["location"] != DBNull.Value ? reader["location"].ToString() : null,
                                        SessionTypeName = reader["session_type_name"] != DBNull.Value ? reader["session_type_name"].ToString() : null
                                    });
                                }
                            }
                        }

                        return (list, totalCount);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error while fetching completed training sessions.", ex);
            }
        }

        // Sets is_completed = 1 for the given training session.
        // Returns true only when the session exists and belongs to the club.
        public static async Task<bool> SetCompleted(int trainingSessionId, int clubId)
        {
            const string sql = @"UPDATE training_sessions
                                 SET is_completed = 1
                                 WHERE id = @ID AND club_id = @ClubID;";

            using var conn = new SqlConnection(DataAccessSettings.connectionString);
            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.Add("@ID", SqlDbType.Int).Value = trainingSessionId;
            cmd.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;

            try
            {
                await conn.OpenAsync();
                int rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (SqlException ex)
            {
                throw new Exception("Database error while completing training session.", ex);
            }
        }

        // Returns all players in a category who have no active injury (including
        // players with no injury records at all). Club scoped.
        public static async Task<List<TrainingSessionPlayerByCategoryResponseDTO>> GetPlayersByCategory(int categoryId, int clubId, int trainingSessionId)
        {
            try
            {
                var list = new List<TrainingSessionPlayerByCategoryResponseDTO>();

                const string sql = @"
                    SELECT
                        pl.id AS playerId,
                        CONCAT(pr.first_name, ' ', pr.second_name, ' ', pr.last_name) AS playerName,
                        pl.jersey_number AS jerseyNumber,
                        pp.name AS positionName,
                        pl.photo AS playerImage,
                        CAST(
                            CASE
                                WHEN EXISTS (
                                    SELECT 1
                                    FROM training_attendance ta
                                    WHERE ta.player_id = pl.id
                                      AND ta.training_session_id = @TrainingSessionID
                                      AND ta.status = 1
                                )
                                THEN 1
                                ELSE 0
                            END
                        AS BIT) AS isAttended
                    FROM players_categories pc
                    INNER JOIN players pl ON pc.player_id = pl.id
                    INNER JOIN categories c ON pc.category_id = c.id
                    INNER JOIN players_medical_dossiers md ON md.player_id = pl.id
                    LEFT JOIN player_injuries pi ON md.id = pi.player_medical_dossier_id AND pi.is_active = 1
                    INNER JOIN clubs cl ON c.club_id = cl.id
                    INNER JOIN persons pr ON pl.person_id = pr.id
                    INNER JOIN primary_positions pp ON pl.primary_position_id = pp.id
                    WHERE pc.category_id = @CategoryID
                      AND cl.id = @ClubID
                      AND pi.id IS NULL
                    ORDER BY TRY_CAST(pl.jersey_number AS INT), pl.jersey_number;";

                using (var conn = new SqlConnection(DataAccessSettings.connectionString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@CategoryID", SqlDbType.Int).Value = categoryId;
                    cmd.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;
                    cmd.Parameters.Add("@TrainingSessionID", SqlDbType.Int).Value = trainingSessionId;

                    await conn.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new TrainingSessionPlayerByCategoryResponseDTO
                            {
                                PlayerID = Convert.ToInt32(reader["playerId"]),
                                PlayerName = reader.GetString(reader.GetOrdinal("playerName")),
                                JerseyNumber = reader.GetString(reader.GetOrdinal("jerseyNumber")),
                                PositionName = reader.GetString(reader.GetOrdinal("positionName")),
                                PlayerImage = reader.IsDBNull(reader.GetOrdinal("playerImage"))
                                    ? string.Empty
                                    : reader.GetString(reader.GetOrdinal("playerImage")),
                                IsAttended = reader.GetBoolean(reader.GetOrdinal("isAttended"))
                            });
                        }
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                throw new Exception("Database error while fetching training session players by category.", ex);
            }
        }

        // Returns all non-completed training sessions for a category within a club.
        public static async Task<List<TrainingSessionByCategoryResponseDTO>> GetByCategory(int categoryId, int clubId)
        {
            try
            {
                var list = new List<TrainingSessionByCategoryResponseDTO>();

                const string sql = @"SELECT ts.id, ts.club_id, ts.category_id, ts.date,
                                            ts.start_time, ts.end_time, ts.location,
                                            st.name AS session_type_name
                                     FROM training_sessions ts
                                     INNER JOIN training_session_types st ON ts.session_type_id = st.id
                                     WHERE ts.club_id = @ClubID
                                       AND ts.category_id = @CategoryID
                                       AND ts.is_completed = 0
                                     ORDER BY ts.date DESC, ts.start_time DESC;";

                using (var conn = new SqlConnection(DataAccessSettings.connectionString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;
                    cmd.Parameters.Add("@CategoryID", SqlDbType.Int).Value = categoryId;

                    await conn.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new TrainingSessionByCategoryResponseDTO
                            {
                                ID = Convert.ToInt32(reader["id"]),
                                ClubID = Convert.ToInt32(reader["club_id"]),
                                CategoryID = Convert.ToInt32(reader["category_id"]),
                                Date = Convert.ToDateTime(reader["date"]),
                                StartTime = reader["start_time"] != DBNull.Value ? (TimeSpan)reader["start_time"] : TimeSpan.Zero,
                                EndTime = reader["end_time"] != DBNull.Value ? (TimeSpan)reader["end_time"] : TimeSpan.Zero,
                                Location = reader["location"] != DBNull.Value ? reader["location"].ToString() : null,
                                SessionTypeName = reader["session_type_name"] != DBNull.Value ? reader["session_type_name"].ToString() : null
                            });
                        }
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                throw new Exception("Database error while fetching training sessions by category.", ex);
            }
        }
    }
}
