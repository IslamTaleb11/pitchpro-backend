using DataAccessLayer.DTOs.Category;
using DataAccessLayer.DTOs.StaffCategory;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessLayer.Providers
{
    public class CategoryProvider
    {
        public static async Task<bool> Delete(int categoryID, int clubID)
        {
            string sql = @"UPDATE Categories 
               SET IsDeleted = 1 
               WHERE id = @CategoryID AND club_id = @ClubID";
            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@CategoryID", SqlDbType.Int).Value = categoryID;
                    command.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubID;

                    try
                    {
                        await connection.OpenAsync();
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while deleting the category.", ex);
                    }
                }
            }
        }

        public static async Task<bool> Update(CategoryEditSaveDTO saveDTO)
        {
            string sql = @"UPDATE Categories
                           SET name = @Name,
                               min_age = @MinAge,
                               max_age = @MaxAge,
                               capacity = @Capacity,
                               registration_fee = @RegistrationFee
                           WHERE id = @ID AND club_id = @ClubID AND isDeleted = 0";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@ID", SqlDbType.Int).Value = saveDTO.ID;
                    command.Parameters.Add("@Name", SqlDbType.NVarChar, 255).Value = saveDTO.Name;
                    command.Parameters.Add("@MinAge", SqlDbType.Int).Value = saveDTO.MinAge;
                    command.Parameters.Add("@MaxAge", SqlDbType.Int).Value = saveDTO.MaxAge;
                    command.Parameters.Add("@Capacity", SqlDbType.Int).Value = saveDTO.Capacity;
                    command.Parameters.Add("@RegistrationFee", SqlDbType.Decimal).Value = saveDTO.RegistrationFee;
                    command.Parameters.Add("@ClubID", SqlDbType.Int).Value = saveDTO.ClubID;

                    try
                    {
                        await connection.OpenAsync();
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while updating the category.", ex);
                    }
                }
            }
        }

        public static async Task<int> AddNew(CategoryAddSaveDTO saveDTO, CategoryAddResponseDTO responseDTO)
        {
            int newId = -1;

            string sql = @"INSERT INTO Categories (name, min_age, max_age, capacity, registration_fee, club_id)
                           VALUES (@Name, @MinAge, @MaxAge, @Capacity, @RegistrationFee, @ClubID);
                           SELECT CAST(scope_identity() AS int);";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@Name", SqlDbType.NVarChar, 255).Value = saveDTO.Name;
                    command.Parameters.Add("@MinAge", SqlDbType.Int).Value = saveDTO.MinAge;
                    command.Parameters.Add("@MaxAge", SqlDbType.Int).Value = saveDTO.MaxAge;
                    command.Parameters.Add("@Capacity", SqlDbType.Int).Value = saveDTO.Capacity;
                    command.Parameters.Add("@RegistrationFee", SqlDbType.Decimal).Value = saveDTO.RegistrationFee;
                    command.Parameters.Add("@ClubID", SqlDbType.Int).Value = saveDTO.ClubID;

                    try
                    {
                        await connection.OpenAsync();

                        var result = await command.ExecuteScalarAsync();
                        if (result != null)
                        {
                            newId = Convert.ToInt32(result);

                            responseDTO.ID = newId;
                            responseDTO.Name = saveDTO.Name;
                            responseDTO.MinAge = saveDTO.MinAge;
                            responseDTO.MaxAge = saveDTO.MaxAge;
                            responseDTO.Capacity = saveDTO.Capacity;
                            responseDTO.RegistrationFee = saveDTO.RegistrationFee;
                            responseDTO.ClubID = saveDTO.ClubID;
                        }
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while adding a new category.", ex);
                    }
                }
            }

            return newId;
        }

        public static async Task CheckCategoriesExistence(int[] categoriesIDs, int clubID)
        {
            if (categoriesIDs == null || categoriesIDs.Length == 0) return;

            using (SqlConnection conn = new SqlConnection(DataAccessSettings.connectionString))
            {
                var parameterNames = categoriesIDs.Select((id, index) => "@cat" + index).ToArray();

                string query = $"SELECT COUNT(*) FROM Categories WHERE club_id = @ClubID AND id IN ({string.Join(",", parameterNames)})" +
                    $"and isDeleted = 0";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ClubID", clubID);

                    for (int i = 0; i < categoriesIDs.Length; i++)
                    {
                        cmd.Parameters.AddWithValue(parameterNames[i], categoriesIDs[i]);
                    }

                    await conn.OpenAsync();
                    int foundCount = (int)await cmd.ExecuteScalarAsync();

                    if (foundCount != categoriesIDs.Length)
                    {
                        throw new Exception();
                    }
                }
            }
        }

        public static async Task<int> CountCategoriesByClubID(int clubID)
        {
            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(@"SELECT COUNT(*)
                                  FROM Categories
                                  WHERE club_id = @ClubID AND isDeleted = 0", connection))
                {
                    command.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubID;

                    try
                    {
                        await connection.OpenAsync();
                        var result = await command.ExecuteScalarAsync();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while counting categories.", ex);
                    }
                }
            }
        }

        public static async Task<List<CategoryGetAllLookupResponse>> GetAllCategories(int clubID)
        {
            var list = new List<CategoryGetAllLookupResponse>();

            try
            {
                using (var conn = new SqlConnection(DataAccessSettings.connectionString))
                {
                    const string sql = @"SELECT id, name
                                         FROM Categories
                                         WHERE club_id = @ClubID and isDeleted = 0
                                         ORDER BY id";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ClubID", clubID);

                        await conn.OpenAsync();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            int idColumn = reader.GetOrdinal("id");
                            int nameColumn = reader.GetOrdinal("name");

                            while (await reader.ReadAsync())
                            {
                                list.Add(new CategoryGetAllLookupResponse
                                {
                                    ID = reader.GetInt32(idColumn),
                                    Name = reader.GetString(nameColumn),
                                });
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Database error while fetching categories for the specified club.", ex);
            }

            return list;
        }

        public static async Task<List<CategoryFilterResponseDTO>> GetCategoriesWithFilter(
            int clubID,
            int minAge,
            int maxAge,
            int? capacity,
            int registrationFeeMin,
            int registrationFeeMax,
            int minPlayers,
            int maxPlayers,
            int pageNumber,
            int pageSize)
        {
            var list = new List<CategoryFilterResponseDTO>();

            try
            {
                using (var conn = new SqlConnection(DataAccessSettings.connectionString))
                {
                    using (var cmd = new SqlCommand("sp_GetCategoriesWithFilter", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@ClubID", clubID);
                        cmd.Parameters.AddWithValue("@MinAge", minAge);
                        cmd.Parameters.AddWithValue("@MaxAge", maxAge);

                        if (capacity.HasValue)
                            cmd.Parameters.AddWithValue("@Capacity", capacity.Value);
                        else
                            cmd.Parameters.AddWithValue("@Capacity", DBNull.Value);

                        cmd.Parameters.AddWithValue("@RegistrationFeeMin", registrationFeeMin);
                        cmd.Parameters.AddWithValue("@RegistrationFeeMax", registrationFeeMax);
                        cmd.Parameters.AddWithValue("@MinPlayers", minPlayers);
                        cmd.Parameters.AddWithValue("@MaxPlayers", maxPlayers);
                        cmd.Parameters.AddWithValue("@PageNumber", pageNumber);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        await conn.OpenAsync();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            int nameColumn = reader.GetOrdinal("name");
                            int minAgeColumn = reader.GetOrdinal("min_age");
                            int maxAgeColumn = reader.GetOrdinal("max_age");
                            int capacityColumn = reader.GetOrdinal("capacity");
                            int registrationFeeColumn = reader.GetOrdinal("registration_fee");
                            int totalPlayersColumn = reader.GetOrdinal("TotalPlayers");

                            while (await reader.ReadAsync())
                            {
                                list.Add(new CategoryFilterResponseDTO
                                {
                                    Name = reader.GetString(nameColumn),
                                    MinAge = (int)reader.GetInt16(minAgeColumn),
                                    MaxAge = (int)reader.GetInt16(maxAgeColumn),
                                    Capacity = (int)reader.GetInt16(capacityColumn),
                                    RegistrationFee = reader.GetDecimal(registrationFeeColumn),
                                    TotalPlayers = reader.GetInt32(totalPlayersColumn)
                                });
                            }
                        }
                    }
                }
            }
            catch (SqlException)
            {
                throw new Exception("An error occurred while fetching the categories.");
            }

            return list;
        }

        public static async Task<List<CategoryGetAllResponseDTO>> GetAllCategories(int clubID, int pageNumber, int rowsPerPage)
        {
            var list = new List<CategoryGetAllResponseDTO>();

            int offset = (pageNumber - 1) * rowsPerPage;

            try
            {
                using (var conn = new SqlConnection(DataAccessSettings.connectionString))
                {
                    const string sql = @"SELECT id, name, min_age, max_age, capacity, registration_fee, club_id
                                         FROM Categories
                                         WHERE club_id = @ClubID
                                         and isDeleted = 0
                                         ORDER BY id
                                         OFFSET @Offset ROWS
                                         FETCH NEXT @RowsPerPage ROWS ONLY";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@ClubID", clubID);
                        cmd.Parameters.AddWithValue("@Offset", offset);
                        cmd.Parameters.AddWithValue("@RowsPerPage", rowsPerPage);

                        await conn.OpenAsync();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            int idColumn = reader.GetOrdinal("id");
                            int nameColumn = reader.GetOrdinal("name");
                            int minAgeColumn = reader.GetOrdinal("min_age");
                            int maxAgeColumn = reader.GetOrdinal("max_age");
                            int capacityColumn = reader.GetOrdinal("capacity");
                            int registrationFeeColumn = reader.GetOrdinal("registration_fee");
                            int clubIdColumn = reader.GetOrdinal("club_id");

                            while (await reader.ReadAsync())
                            {
                                list.Add(new CategoryGetAllResponseDTO
                                {
                                    ID = reader.GetInt32(idColumn),
                                    Name = reader.GetString(nameColumn),
                                    MinAge = (int)reader.GetInt16(minAgeColumn),
                                    MaxAge = (int)reader.GetInt16(maxAgeColumn),
                                    Capacity = (int)reader.GetInt16(capacityColumn),
                                    RegistrationFee = reader.GetDecimal(registrationFeeColumn),
                                    ClubID = reader.GetInt32(clubIdColumn)
                                });
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Database error while fetching categories for the specified club.", ex);
            }

            return list;
        }


    }
}
