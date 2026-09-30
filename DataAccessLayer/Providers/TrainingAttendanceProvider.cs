using DataAccessLayer.DTOs.TrainingAttendance;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DataAccessLayer.Providers
{
    public class TrainingAttendanceProvider
    {
        public static async Task<int> AddNew(TrainingAttendanceInsertDTO dto)
        {
            try
            {
                const string sql = @"INSERT INTO training_attendance (player_id, training_session_id, status, recorded_at)
                                     VALUES (@PlayerID, @TrainingSessionID, @Status, @RecordedAt);
                                     SELECT CAST(scope_identity() AS int);";

                using (var conn = new SqlConnection(DataAccessSettings.connectionString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@PlayerID", SqlDbType.Int).Value = dto.PlayerID;
                    cmd.Parameters.Add("@TrainingSessionID", SqlDbType.Int).Value = dto.TrainingSessionID;
                    cmd.Parameters.Add("@Status", SqlDbType.Bit).Value = dto.Status;
                    cmd.Parameters.Add("@RecordedAt", SqlDbType.DateTime).Value = dto.RecordedAt;

                    await conn.OpenAsync();
                    var result = await cmd.ExecuteScalarAsync();
                    return result != null ? Convert.ToInt32(result) : -1;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error while recording training attendance.", ex);
            }
        }

        public static async Task<int> IsAlreadyExist(int playerId, int trainingSessionId)
        {
            const string sql = @"SELECT id
                                 FROM training_attendance
                                 WHERE player_id = @PlayerID AND training_session_id = @TrainingSessionID;";

            using var connection = new SqlConnection(DataAccessSettings.connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@PlayerID", SqlDbType.Int).Value = playerId;
            command.Parameters.Add("@TrainingSessionID", SqlDbType.Int).Value = trainingSessionId;

            try
            {
                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return result != null && result != DBNull.Value ? Convert.ToInt32(result) : -1;
            }
            catch (SqlException ex)
            {
                throw new Exception("A database error occurred while checking the training attendance.", ex);
            }
        }

        public static async Task<int> Update(TrainingAttendanceInsertDTO dto)
        {
            const string sql = @"UPDATE training_attendance
                                 SET status = @Status, recorded_at = @RecordedAt
                                 WHERE player_id = @PlayerID AND training_session_id = @TrainingSessionID;";

            using var connection = new SqlConnection(DataAccessSettings.connectionString);
            using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@PlayerID", SqlDbType.Int).Value = dto.PlayerID;
            command.Parameters.Add("@TrainingSessionID", SqlDbType.Int).Value = dto.TrainingSessionID;
            command.Parameters.Add("@Status", SqlDbType.Bit).Value = dto.Status;
            command.Parameters.Add("@RecordedAt", SqlDbType.DateTime).Value = dto.RecordedAt;

            try
            {
                await connection.OpenAsync();
                return await command.ExecuteNonQueryAsync();
            }
            catch (SqlException ex)
            {
                throw new Exception("Database error occurred while updating training attendance.", ex);
            }
        }

        // True when the given player belongs to the club (via persons table).
        public static async Task<bool> PlayerBelongsToClub(int playerId, int clubId)
        {
            try
            {
                const string sql = @"SELECT TOP 1 1
                                     FROM players pl
                                     INNER JOIN persons pr ON pl.person_id = pr.id
                                     WHERE pl.id = @PlayerID AND pr.club_id = @ClubID;";

                using (var conn = new SqlConnection(DataAccessSettings.connectionString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@PlayerID", SqlDbType.Int).Value = playerId;
                    cmd.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;

                    await conn.OpenAsync();
                    var result = await cmd.ExecuteScalarAsync();
                    return result != null;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error while checking player club membership.", ex);
            }
        }

        // True when the training session belongs to the given club.
        public static async Task<bool> TrainingSessionBelongsToClub(int trainingSessionId, int clubId)
        {
            try
            {
                const string sql = @"SELECT TOP 1 1
                                     FROM training_sessions
                                     WHERE id = @ID AND club_id = @ClubID;";

                using (var conn = new SqlConnection(DataAccessSettings.connectionString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@ID", SqlDbType.Int).Value = trainingSessionId;
                    cmd.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;

                    await conn.OpenAsync();
                    var result = await cmd.ExecuteScalarAsync();
                    return result != null;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Database error while checking training session club membership.", ex);
            }
        }
    }
}
