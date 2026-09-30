using DataAccessLayer.DTOs.Match;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DataAccessLayer.Providers
{
    public class MatchProvider
    {
        public static async Task<int> CountMatchesByClubID(int clubID)
        {
            const string sql = @"SELECT COUNT(*)
                                 FROM matches
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

        public static int AddNew(MatchRegistrationSaveDTO dto, MatchRegistrationResponseDTO responseDTO)
        {
            int newId = -1;

            string sql = @"INSERT INTO matches (club_id, category_id, opponent_name, date, kickoff_time, end_time, is_completed, is_home, stadium_name)
                           VALUES (@ClubID, @CategoryID, @OpponentName, @Date, @KickoffTime, @EndTime, @IsCompleted, @IsHome, @StadiumName);
                           SELECT CAST(scope_identity() AS int);";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@ClubID",       SqlDbType.Int).Value          = dto.ClubID;
                    command.Parameters.Add("@CategoryID",   SqlDbType.Int).Value          = dto.CategoryID;
                    command.Parameters.Add("@OpponentName", SqlDbType.NVarChar, 255).Value = dto.OpponentName;
                    command.Parameters.Add("@Date",         SqlDbType.Date).Value         = dto.Date;
                    command.Parameters.Add("@KickoffTime",  SqlDbType.Time).Value         = dto.KickoffTime;
                    command.Parameters.Add("@EndTime",      SqlDbType.Time).Value         = dto.EndTime;
                    command.Parameters.Add("@IsCompleted",  SqlDbType.Bit).Value          = dto.IsCompleted;
                    command.Parameters.Add("@IsHome",       SqlDbType.Bit).Value          = dto.IsHome;
                    command.Parameters.Add("@StadiumName",  SqlDbType.NVarChar, 255).Value = (object)dto.StadiumName ?? DBNull.Value;

                    try
                    {
                        connection.Open();

                        var result = command.ExecuteScalar();
                        if (result != null)
                        {
                            newId = Convert.ToInt32(result);

                            responseDTO.ID           = newId;
                            responseDTO.ClubID       = dto.ClubID;
                            responseDTO.CategoryID   = dto.CategoryID;
                            responseDTO.OpponentName = dto.OpponentName;
                            responseDTO.Date         = dto.Date;
                            responseDTO.KickoffTime  = dto.KickoffTime;
                            responseDTO.IsCompleted  = dto.IsCompleted;
                            responseDTO.IsHome       = dto.IsHome;
                        }
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while adding a new match.", ex);
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }

            return newId;
        }

        // Returns the nearest upcoming match for a category within a club, or null
        // when the category has no upcoming match. "Upcoming" = not completed and
        // dated today or later. Ordered by soonest date/kickoff first.
        public static async Task<MatchCallUpDTO> GetUpcomingMatchByCategory(int categoryId, int clubId)
        {
            string sql = @"SELECT TOP 1 m.id, m.club_id, m.category_id, m.opponent_name, m.date,
                                  m.kickoff_time, m.end_time, m.is_completed, m.is_home, m.stadium_name,
                                  c.name AS club_name
                           FROM matches m
                           INNER JOIN clubs c ON c.id = m.club_id
                           WHERE m.club_id = @ClubID
                             AND m.category_id = @CategoryID
                             AND m.is_completed = 0
                             AND CAST(m.date AS DATE) >= CAST(GETDATE() AS DATE)
                           ORDER BY m.date ASC, m.kickoff_time ASC;";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@ClubID",     SqlDbType.Int).Value = clubId;
                    command.Parameters.Add("@CategoryID", SqlDbType.Int).Value = categoryId;

                    await connection.OpenAsync();

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new MatchCallUpDTO
                            {
                                ID           = Convert.ToInt32(reader["id"]),
                                ClubID       = Convert.ToInt32(reader["club_id"]),
                                CategoryID   = Convert.ToInt32(reader["category_id"]),
                                OpponentName = reader["opponent_name"] != DBNull.Value ? reader["opponent_name"].ToString() : null,
                                Date         = Convert.ToDateTime(reader["date"]),
                                KickoffTime  = reader["kickoff_time"] != DBNull.Value ? (TimeSpan)reader["kickoff_time"] : TimeSpan.Zero,
                                EndTime      = reader["end_time"] != DBNull.Value ? (TimeSpan)reader["end_time"] : TimeSpan.Zero,
                                IsCompleted  = reader["is_completed"] != DBNull.Value && Convert.ToBoolean(reader["is_completed"]),
                                IsHome       = reader["is_home"] != DBNull.Value && Convert.ToBoolean(reader["is_home"]),
                                StadiumName  = reader["stadium_name"] != DBNull.Value ? reader["stadium_name"].ToString() : null,
                                ClubName     = reader["club_name"] != DBNull.Value ? reader["club_name"].ToString() : null
                            };
                        }
                    }
                }
            }

            // No upcoming match for this category in this club.
            return null;
        }

        // Returns all non-completed matches for a category within a club.
        public static async Task<List<MatchIncompleteByCategoryResponseDTO>> GetIncompleteMatchesByCategory(int categoryId, int clubId)
        {
            var list = new List<MatchIncompleteByCategoryResponseDTO>();

            string sql = @"SELECT m.id, m.club_id, m.category_id, m.opponent_name, m.date,
                                  m.kickoff_time, m.end_time, m.is_completed, m.is_home, m.stadium_name,
                                  c.name AS club_name
                           FROM matches m
                           INNER JOIN clubs c ON c.id = m.club_id
                           WHERE m.club_id = @ClubID
                             AND m.category_id = @CategoryID
                             AND m.is_completed = 0
                           ORDER BY m.date DESC, m.kickoff_time DESC;";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@ClubID",     SqlDbType.Int).Value = clubId;
                    command.Parameters.Add("@CategoryID", SqlDbType.Int).Value = categoryId;

                    await connection.OpenAsync();

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            list.Add(new MatchIncompleteByCategoryResponseDTO
                            {
                                ID           = Convert.ToInt32(reader["id"]),
                                ClubID       = Convert.ToInt32(reader["club_id"]),
                                CategoryID   = Convert.ToInt32(reader["category_id"]),
                                OpponentName = reader["opponent_name"] != DBNull.Value ? reader["opponent_name"].ToString() : null,
                                Date         = Convert.ToDateTime(reader["date"]),
                                KickoffTime  = reader["kickoff_time"] != DBNull.Value ? (TimeSpan)reader["kickoff_time"] : TimeSpan.Zero,
                                EndTime      = reader["end_time"] != DBNull.Value ? (TimeSpan)reader["end_time"] : TimeSpan.Zero,
                                IsCompleted  = reader["is_completed"] != DBNull.Value && Convert.ToBoolean(reader["is_completed"]),
                                IsHome       = reader["is_home"] != DBNull.Value && Convert.ToBoolean(reader["is_home"]),
                                StadiumName  = reader["stadium_name"] != DBNull.Value ? reader["stadium_name"].ToString() : null,
                                ClubName     = reader["club_name"] != DBNull.Value ? reader["club_name"].ToString() : null
                            });
                        }
                    }
                }
            }

            return list;
        }

        // Returns paginated completed matches for a category within a club, plus
        // the total count of completed matches for that category.
        public static async Task<(List<MatchIncompleteByCategoryResponseDTO> Data, int TotalCount)> GetCompletedByCategory(
            int categoryId, int clubId, int page, int pageSize)
        {
            try
            {
                var list = new List<MatchIncompleteByCategoryResponseDTO>();

                string countSql = @"SELECT COUNT(*)
                                    FROM matches
                                    WHERE club_id = @ClubID
                                      AND category_id = @CategoryID
                                      AND is_completed = 1;";

                string dataSql = @"SELECT m.id, m.club_id, m.category_id, m.opponent_name, m.date,
                                          m.kickoff_time, m.end_time, m.is_completed, m.is_home, m.stadium_name,
                                          c.name AS club_name, m.club_score , m.opponent_score
                                   FROM matches m
                                   INNER JOIN clubs c ON c.id = m.club_id
                                   WHERE m.club_id = @ClubID
                                     AND m.category_id = @CategoryID
                                     AND m.is_completed = 1
                                   ORDER BY m.date DESC, m.kickoff_time DESC
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
                                    list.Add(new MatchIncompleteByCategoryResponseDTO
                                    {
                                        ID = Convert.ToInt32(reader["id"]),
                                        ClubID = Convert.ToInt32(reader["club_id"]),
                                        CategoryID = Convert.ToInt32(reader["category_id"]),
                                        OpponentName = reader["opponent_name"] != DBNull.Value ? reader["opponent_name"].ToString() : null,
                                        Date = Convert.ToDateTime(reader["date"]),
                                        KickoffTime = reader["kickoff_time"] != DBNull.Value ? (TimeSpan)reader["kickoff_time"] : TimeSpan.Zero,
                                        EndTime = reader["end_time"] != DBNull.Value ? (TimeSpan)reader["end_time"] : TimeSpan.Zero,
                                        IsCompleted = reader["is_completed"] != DBNull.Value && Convert.ToBoolean(reader["is_completed"]),
                                        IsHome = reader["is_home"] != DBNull.Value && Convert.ToBoolean(reader["is_home"]),
                                        StadiumName = reader["stadium_name"] != DBNull.Value ? reader["stadium_name"].ToString() : null,
                                        ClubName = reader["club_name"] != DBNull.Value ? reader["club_name"].ToString() : null,
                                        ClubScore = Convert.ToInt32(reader["club_score"]),
                                        OpponentScore = Convert.ToInt32(reader["opponent_score"])
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
                throw new Exception("Database error while fetching completed matches.", ex);
            }
        }

        // True when a match with the given id exists for the club.
        public static async Task<bool> Exists(int matchId, int clubId)
        {
            try
            {
                const string sql = @"SELECT TOP 1 1
                                     FROM matches
                                     WHERE id = @ID AND club_id = @ClubID;";

                using (var conn = new SqlConnection(DataAccessSettings.connectionString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@ID", SqlDbType.Int).Value = matchId;
                    cmd.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;

                    await conn.OpenAsync();
                    var result = await cmd.ExecuteScalarAsync();
                    return result != null;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error while checking if the match exists.", ex);
            }
        }

        // Updates the club and opponent scores for a match, scoped to the club.
        public static async Task SetResult(int matchId, int clubId, MatchResultSaveDTO dto)
        {
            try
            {
                const string sql = @"UPDATE matches
                                     SET club_score = @ClubScore,
                                         opponent_score = @OpponentScore
                                     WHERE id = @ID AND club_id = @ClubID;";

                using (var conn = new SqlConnection(DataAccessSettings.connectionString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@ID", SqlDbType.Int).Value = matchId;
                    cmd.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;
                    cmd.Parameters.Add("@ClubScore", SqlDbType.Int).Value = dto.ClubScore;
                    cmd.Parameters.Add("@OpponentScore", SqlDbType.Int).Value = dto.OpponentScore;

                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error while updating the match result.", ex);
            }
        }

        // Sets is_completed = 1 for a match, scoped to the club.
        public static async Task SetCompleted(int matchId, int clubId)
        {
            try
            {
                const string sql = @"UPDATE matches
                                     SET is_completed = 1
                                     WHERE id = @ID AND club_id = @ClubID;";

                using (var conn = new SqlConnection(DataAccessSettings.connectionString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@ID", SqlDbType.Int).Value = matchId;
                    cmd.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;

                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error while completing the match.", ex);
            }
        }

        public static async Task<bool> Update(MatchEditSaveDTO dto)
        {
            string sql = @"UPDATE matches 
                           SET club_id = @ClubID, 
                               category_id = @CategoryID, 
                               opponent_name = @OpponentName, 
                               date = @Date, 
                               kickoff_time = @KickoffTime, 
                               end_time = @EndTime, 
                               is_completed = @IsCompleted, 
                               is_home = @IsHome, 
                               stadium_name = @StadiumName
                           WHERE id = @ID AND club_id = @ClubID;";

            using (var conn = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@ID",            SqlDbType.Int).Value          = dto.ID;
                    cmd.Parameters.Add("@ClubID",        SqlDbType.Int).Value          = dto.ClubID;
                    cmd.Parameters.Add("@CategoryID",    SqlDbType.Int).Value          = dto.CategoryID;
                    cmd.Parameters.Add("@OpponentName",  SqlDbType.NVarChar, 255).Value = dto.OpponentName;
                    cmd.Parameters.Add("@Date",          SqlDbType.Date).Value         = dto.Date;
                    cmd.Parameters.Add("@KickoffTime",   SqlDbType.Time).Value         = dto.KickoffTime;
                    cmd.Parameters.Add("@EndTime",       SqlDbType.Time).Value         = dto.EndTime;
                    cmd.Parameters.Add("@IsCompleted",   SqlDbType.Bit).Value          = dto.IsCompleted;
                    cmd.Parameters.Add("@IsHome",        SqlDbType.Bit).Value          = dto.IsHome;
                    cmd.Parameters.Add("@StadiumName",   SqlDbType.NVarChar, 255).Value = (object)dto.StadiumName ?? DBNull.Value;

                    await conn.OpenAsync();
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }
    }
}
