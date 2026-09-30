using DataAccessLayer.DTOs.Schedule;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DataAccessLayer.Providers
{
    public class ScheduleProvider
    {
        public static async Task<List<UpcomingScheduleItemDTO>> GetUpcomingSchedule(
            int clubId, 
            int pageNumber, 
            int rowsPerPage,
            string eventClassification = null,
            string category = null)
        {
            var list = new List<UpcomingScheduleItemDTO>();

            try
            {
                using (var conn = new SqlConnection(DataAccessSettings.connectionString))
                {
                    using (var cmd = new SqlCommand("sp_GetUpcomingSchedule", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("@ClubId",             SqlDbType.Int).Value          = clubId;
                        cmd.Parameters.Add("@PageNumber",         SqlDbType.Int).Value          = pageNumber;
                        cmd.Parameters.Add("@RowsPerPage",        SqlDbType.Int).Value          = rowsPerPage;
                        cmd.Parameters.Add("@EventClassification", SqlDbType.NVarChar, 50).Value = (object)eventClassification ?? DBNull.Value;
                        cmd.Parameters.Add("@Category",            SqlDbType.NVarChar, 100).Value = (object)category ?? DBNull.Value;

                        await conn.OpenAsync();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            int eventTypeCol = reader.GetOrdinal("EventType");
                            int eventIdCol   = reader.GetOrdinal("EventId");
                            int eventDateCol = reader.GetOrdinal("EventDate");
                            int locationCol  = reader.GetOrdinal("Location");
                            int titleCol     = reader.GetOrdinal("Title");
                            int startTimeCol = reader.GetOrdinal("StartTime");
                            int endTimeCol   = reader.GetOrdinal("EndTime");
                            int categoryIDCol = reader.GetOrdinal("CategoryID");
                            int categoryNameCol = reader.GetOrdinal("CategoryName");
                            int isHomeCol    = reader.GetOrdinal("IsHome");

                            while (await reader.ReadAsync())
                            {
                                list.Add(new UpcomingScheduleItemDTO
                                {
                                    EventType     = reader.GetString(eventTypeCol),
                                    EventId       = reader.GetInt32(eventIdCol),
                                    EventDate     = reader.GetDateTime(eventDateCol),
                                    Location      = reader.IsDBNull(locationCol) ? "—" : reader.GetString(locationCol),
                                    Title         = reader.GetString(titleCol),
                                    StartTime     = reader.IsDBNull(startTimeCol) ? null : reader.GetTimeSpan(startTimeCol),
                                    EndTime       = reader.IsDBNull(endTimeCol) ? null : reader.GetTimeSpan(endTimeCol),
                                    CategoryID    = reader.GetInt32(categoryIDCol),
                                    CategoryName  = reader.IsDBNull(categoryNameCol) ? "—" : reader.GetString(categoryNameCol),
                                    IsHome        = reader.IsDBNull(isHomeCol) ? null : reader.GetBoolean(isHomeCol),
                                });
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Database error occurred while fetching the upcoming schedule.", ex);
            }

            return list;
        }
    }
}
