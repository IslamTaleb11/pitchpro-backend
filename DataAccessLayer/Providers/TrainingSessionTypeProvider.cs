using DataAccessLayer.DTOs.TrainingSessionType;
using Microsoft.Data.SqlClient;

namespace DataAccessLayer.Providers
{
    public class TrainingSessionTypeProvider
    {
        public static async Task<List<TrainingSessionTypeGetAllResponseDTO>> GetAllSessionTypes()
        {
            var list = new List<TrainingSessionTypeGetAllResponseDTO>();

            try
            {
                using (var conn = new SqlConnection(DataAccessSettings.connectionString))
                {
                    const string sql = "SELECT id, name FROM training_session_types";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        await conn.OpenAsync();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            int idColumn   = reader.GetOrdinal("id");
                            int nameColumn = reader.GetOrdinal("name");

                            while (await reader.ReadAsync())
                            {
                                list.Add(new TrainingSessionTypeGetAllResponseDTO
                                {
                                    ID   = reader.GetInt32(idColumn),
                                    Name = reader.GetString(nameColumn)
                                });
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Database connectivity issue while loading training session types.", ex);
            }

            return list;
        }
    }
}
