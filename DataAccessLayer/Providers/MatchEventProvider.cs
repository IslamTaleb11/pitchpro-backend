using DataAccessLayer.DTOs.MatchEvent;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DataAccessLayer.Providers
{
    public class MatchEventProvider
    {
        public static async Task<bool> MatchAttendanceBelongsToClub(int? matchAttendanceId, int clubId)
        {
            const string sql = @"SELECT TOP 1 1
                                   FROM matches_attendance ma
                                   INNER JOIN match_callup_players mcp ON ma.match_callup_players_id = mcp.id
                                   INNER JOIN matches m ON mcp.match_id = m.id
                                   WHERE ma.id = @MatchAttendanceID AND m.club_id = @ClubID;";

            using (var connection = new SqlConnection(DataAccessSettings.connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@MatchAttendanceID", SqlDbType.Int).Value = matchAttendanceId;
                command.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return result != null;
            }
        }

        public static async Task<int> Save(MatchEventRequestSaveDTO dto)
        {
            int newId = -1;

            const string sql = @"INSERT INTO match_events (match_attendance_id, event_type_id, event_at)
                                   VALUES (@MatchAttendanceID, @EventTypeID, @EventAt);
                                   SELECT CAST(scope_identity() AS int);";

            using (var connection = new SqlConnection(DataAccessSettings.connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@MatchAttendanceID", SqlDbType.Int).Value = dto.MatchAttendanceID;
                command.Parameters.Add("@EventTypeID", SqlDbType.TinyInt).Value = dto.EventTypeID;
                command.Parameters.Add("@EventAt", SqlDbType.TinyInt).Value = dto.EventAt;

                try
                {
                    await connection.OpenAsync();
                    var result = await command.ExecuteScalarAsync();
                    if (result != null)
                        newId = Convert.ToInt32(result);
                }
                catch (SqlException ex)
                {
                    throw new Exception("Database error occurred while saving match event.", ex);
                }
            }

            return newId;
        }

        public static async Task<bool> EventBelongsToClub(int eventId, int clubId, int matchId)
        {
            const string sql = @"SELECT TOP 1 1
                                   FROM match_events me
                                   INNER JOIN matches_attendance ma ON me.match_attendance_id = ma.id
                                   INNER JOIN match_callup_players mcp ON ma.match_callup_players_id = mcp.id
                                   INNER JOIN matches m ON mcp.match_id = m.id
                                   WHERE me.id = @EventID AND m.club_id = @ClubID AND m.id = @MatchID;";

            using (var connection = new SqlConnection(DataAccessSettings.connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@EventID", SqlDbType.Int).Value = eventId;
                command.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;
                command.Parameters.Add("@MatchID", SqlDbType.Int).Value = matchId;

                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return result != null;
            }
        }

        public static async Task Delete(int eventId)
        {
            const string sql = @"DELETE FROM match_events WHERE id = @EventID;";

            using (var connection = new SqlConnection(DataAccessSettings.connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@EventID", SqlDbType.Int).Value = eventId;

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }

        public static async Task<List<MatchEventByMatchResponseDTO>> GetByMatch(int matchId, int clubId)
        {
            var list = new List<MatchEventByMatchResponseDTO>();

            const string sql = @"SELECT match_events.id as eventId, CONCAT(persons.first_name, persons.last_name) as name, event_types.name as eventName, match_events.event_at
                                   FROM match_events
                                   INNER JOIN matches_attendance ON match_events.match_attendance_id = matches_attendance.id
                                   INNER JOIN match_callup_players ON matches_attendance.match_callup_players_id = match_callup_players.id
                                   INNER JOIN players ON match_callup_players.player_id = players.id
                                   INNER JOIN event_types ON match_events.event_type_id = event_types.id
                                   INNER JOIN persons ON players.person_id = persons.id
                                   INNER JOIN matches ON match_callup_players.match_id = matches.id
                                   WHERE matches.id = @MatchID AND matches.club_id = @ClubID;";

            try
            {
                using (var connection = new SqlConnection(DataAccessSettings.connectionString))
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@MatchID", SqlDbType.Int).Value = matchId;
                    command.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        int eventIdColumn = reader.GetOrdinal("eventId");
                        int nameColumn = reader.GetOrdinal("name");
                        int eventNameColumn = reader.GetOrdinal("eventName");
                        int eventAtColumn = reader.GetOrdinal("event_at");

                        while (await reader.ReadAsync())
                        {
                            list.Add(new MatchEventByMatchResponseDTO
                            {
                                EventID = reader.GetInt32(eventIdColumn),
                                Name = reader.GetString(nameColumn),
                                EventName = reader.GetString(eventNameColumn),
                                EventAt = reader.GetByte(eventAtColumn)
                            });
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Database error occurred while retrieving match events.", ex);
            }

            return list;
        }
    }
}