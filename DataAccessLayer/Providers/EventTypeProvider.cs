using DataAccessLayer.DTOs.EventType;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DataAccessLayer.Providers
{
    public class EventTypeProvider
    {
        public static async Task<List<EventTypeGetAllResponseDTO>> GetAllEventTypes()
        {
            var list = new List<EventTypeGetAllResponseDTO>();

            try
            {
                using (var conn = new SqlConnection(DataAccessSettings.connectionString))
                {
                    const string sql = "SELECT id, name FROM event_types";
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        await conn.OpenAsync();
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            int idColumn = reader.GetOrdinal("id");
                            int nameColumn = reader.GetOrdinal("name");

                            while (await reader.ReadAsync())
                            {
                                list.Add(new EventTypeGetAllResponseDTO
                                {
                                    ID = reader.GetInt32(idColumn),
                                    Name = reader.GetString(nameColumn)
                                });
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Database connectivity issue while loading event types.", ex);
            }

            return list;
        }
    }
}