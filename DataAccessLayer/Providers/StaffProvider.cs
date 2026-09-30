using DataAccessLayer.DTOs.Club;
using DataAccessLayer.DTOs.Staff;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Providers
{
    public class StaffProvider
    {
        public static int AddNew(StaffRegistrationSaveDTO saveDTO, StaffRegistrationResponseDTO staffRegistrationResponseDTO)
        {
            // We use 'int' as the return type to capture the newly created ID (Identity)
            int newId = -1;

            // 1. T-SQL Query with Output clause to get the new ID
            string sql = @"INSERT INTO Staff (photo, phone_number, address, staff_medical_dossier_id, primary_role_id, 
            role_classification_id, user_id) 
                           VALUES (@Photo, @PhoneNumber, @Address, @StaffMedicalDossierID, @PrimaryRoleID, @RoleClassificationID, 
                            @UserID);
                           SELECT CAST(scope_identity() AS int);";

            // 2. Wrap in 'using' blocks to ensure connections are closed automatically
            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@Photo", SqlDbType.NVarChar, -1).Value = saveDTO.Photo;

                    command.Parameters.Add("@PhoneNumber", SqlDbType.NVarChar, 20).Value = saveDTO.PhoneNumber;

                    command.Parameters.Add("@Address", SqlDbType.NVarChar, 500).Value = saveDTO.Address;

                    command.Parameters.Add("@StaffMedicalDossierID", SqlDbType.Int).Value = saveDTO.StaffMedicalDossierID;

                    command.Parameters.Add("@PrimaryRoleID", SqlDbType.Int).Value = saveDTO.PrimaryRoleID;

                    command.Parameters.Add("@RoleClassificationID", SqlDbType.Int).Value = saveDTO.RoleClassificationID;

                    // The glue for your 1:1 relationship
                    command.Parameters.Add("@UserID", SqlDbType.Int).Value = saveDTO.UserID;

                    try
                    {
                        connection.Open();

                        // 4. Execute and get the new Identity ID
                        var result = command.ExecuteScalar();
                        if (result != null)
                        {
                            newId = Convert.ToInt32(result);

                            staffRegistrationResponseDTO.ID = newId;
                            staffRegistrationResponseDTO.PhoneNumber = saveDTO.PhoneNumber;
                            staffRegistrationResponseDTO.Photo = saveDTO.Photo;
                            staffRegistrationResponseDTO.PrimaryRoleID = saveDTO.PrimaryRoleID;
                            staffRegistrationResponseDTO.RoleClassificationID = saveDTO.RoleClassificationID;
                            staffRegistrationResponseDTO.StaffMedicalDossierID = saveDTO.StaffMedicalDossierID;
                            staffRegistrationResponseDTO.UserID = saveDTO.UserID;
                        }
                    }
                    catch (SqlException ex)
                    {
                        // In a 3-tier app, we log the error and re-throw it 
                        // so the Business Layer can decide how to tell the user.
                        throw new Exception("Database error occurred while adding a new club.", ex);
                    }
                    finally
                    {
                        // The 'using' block handles this, but closing manually is extra safe.
                        connection.Close();
                    }
                }
            }

            return newId;
        }


        public static async Task<StaffDashboardCountsResponseDTO> GetDashboardCounts(int clubId)
        {
            var counts = new StaffDashboardCountsResponseDTO();

            try
            {
                using (SqlConnection conn = new SqlConnection(DataAccessSettings.connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetStaffDashboardCounts", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ClubID", clubId);

                        // Use OpenAsync to avoid blocking the thread
                        await conn.OpenAsync();

                        // Use ExecuteReaderAsync
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                counts.TotalActiveStaff = Convert.ToInt32(reader["TotalActiveStaff"]);
                                counts.TotalCoachingStaff = Convert.ToInt32(reader["TotalCoachingStaff"]);
                                counts.TotalMedicalStaff = Convert.ToInt32(reader["TotalMedicalStaff"]);
                                counts.TotalFitnessStaff = Convert.ToInt32(reader["TotalFitnessStaff"]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }

            return counts;
        }


        public static async Task<int> CountActiveStaffByClubID(int clubID)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
                {
                    using (SqlCommand command = new SqlCommand(@"
                        SELECT COUNT(*)
                        FROM dbo.Staff s
                        INNER JOIN dbo.Users u ON u.id = s.user_id
                        INNER JOIN dbo.Persons p ON p.id = u.person_id
                        WHERE p.club_id = @ClubID
                          AND s.is_active = 1;", connection))
                    {
                        command.Parameters.AddWithValue("@ClubID", clubID);

                        await connection.OpenAsync();

                        var result = await command.ExecuteScalarAsync();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("A database error occurred while counting active staff members.", ex);
            }
        }


        public static async Task<List<StaffGetAllResponse>> GetAll(int pageNumber, int rowsPerPage, int clubID)
        {
            var staffList = new List<StaffGetAllResponse>();

            try
            {
                using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
                {
                    using (SqlCommand command = new SqlCommand("sp_GetAllStaffListByClub", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@ClubID", clubID);
                        command.Parameters.AddWithValue("@PageNumber", pageNumber);
                        command.Parameters.AddWithValue("@PageSize", rowsPerPage);

                        await connection.OpenAsync();

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                staffList.Add(new StaffGetAllResponse
                                {
                                    ID = ReadStaffId(reader),
                                    Photo = reader["photo"] != DBNull.Value ? reader["photo"].ToString() : null,
                                    FullName = reader["FullName"].ToString(),
                                    Email = reader["email"].ToString(),
                                    PrimaryRoleName = reader["PrimaryRoleName"].ToString(),
                                    RoleClassificationName = reader["RoleClassificationName"].ToString(),
                                    // Splitting the string into an array for your Response DTO
                                    CategoriesNames = reader["CategoriesNames"] != DBNull.Value
                                        ? reader["CategoriesNames"].ToString().Split(", ")
                                        : Array.Empty<string>()
                                });
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                // Log ex.Message here (e.g., using Serilog or NLog)
                // Throwing it keeps the call stack so you know exactly where it failed
                throw new Exception("A database error occurred while fetching the staff list.", ex);
            }
            catch (Exception ex)
            {
                // Catches mapping errors or split errors
                throw new Exception("An unexpected error occurred in the Staff Provider.", ex);
            }

            return staffList;
        }


        public static async Task<List<StaffGetAllResponse>> GetStaffByFilters(int pageNumber, int rowsPerPage, int clubID, int? primaryRoleID, int? roleClassificationID, int[] categoryIDs)
        {
            var staffList = new List<StaffGetAllResponse>();

            try
            {
                using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
                {
                    using (SqlCommand command = new SqlCommand("sp_GetStaffWithFilters", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@ClubID", clubID);
                        command.Parameters.AddWithValue("@PageNumber", pageNumber);
                        command.Parameters.AddWithValue("@PageSize", rowsPerPage);

                        if (primaryRoleID.HasValue)
                            command.Parameters.AddWithValue("@PrimaryRoleID", primaryRoleID.Value);
                        else
                            command.Parameters.AddWithValue("@PrimaryRoleID", DBNull.Value);

                        if (roleClassificationID.HasValue)
                            command.Parameters.AddWithValue("@RoleClassificationID", roleClassificationID.Value);
                        else
                            command.Parameters.AddWithValue("@RoleClassificationID", DBNull.Value);

                        // Build the TVP for category IDs
                        DataTable categoryTable = new DataTable();
                        categoryTable.Columns.Add("ItemValue", typeof(int));

                        if (categoryIDs != null && categoryIDs.Length > 0)
                        {
                            foreach (int id in categoryIDs)
                            {
                                categoryTable.Rows.Add(id);
                            }
                        }

                        SqlParameter tvpParam = command.Parameters.AddWithValue("@CategoriesIDs", categoryTable);
                        tvpParam.SqlDbType = SqlDbType.Structured;
                        tvpParam.TypeName = "dbo.IntegerList";

                        await connection.OpenAsync();

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                staffList.Add(new StaffGetAllResponse
                                {
                                    ID = ReadStaffId(reader),
                                    Photo = reader["photo"] != DBNull.Value ? reader["photo"].ToString() : null,
                                    FullName = reader["FullName"].ToString(),
                                    Email = reader["email"].ToString(),
                                    PrimaryRoleName = reader["PrimaryRoleName"].ToString(),
                                    RoleClassificationName = reader["RoleClassificationName"].ToString(),
                                    CategoriesNames = reader["CategoriesNames"] != DBNull.Value
                                        ? reader["CategoriesNames"].ToString().Split(", ")
                                        : Array.Empty<string>()
                                });
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("A database error occurred while fetching the filtered staff list.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("An unexpected error occurred in the Staff Provider.", ex);
            }

            return staffList;
        }

        public static async Task<StaffGetByIdResponse> GetById(int staffId, int clubID)
        {
            StaffGetByIdResponse response = null;

            try
            {
                using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
                {
                    // We scope by club through the person link (the Staff table has no club_id column;
                    // the club is derived via user -> person -> club, matching AddNew / SoftDelete).
                    using (SqlCommand command = new SqlCommand(@"
                        SELECT
                            s.id                        AS id,
                            s.photo                     AS photo,
                            p.first_name                AS FirstName,
                            p.second_name               AS SecondName,
                            p.last_name                 AS LastName,
                            p.gender                    AS Gender,
                            p.birth_date                AS BirthDate,
                            u.email                     AS Email,
                            s.phone_number              AS PhoneNumber,
                            s.address                   AS Address,
                            s.primary_role_id           AS PrimaryRoleID,
                            s.role_classification_id    AS RoleClassificationID,
                            s.staff_medical_dossier_id  AS StaffMedicalDossierID,
                            d.blood_type_id             AS BloodTypeID,
                            d.allergies                 AS Allergies,
                            d.medical_notes             AS MedicalNotes
                        FROM dbo.Staff s
                        INNER JOIN dbo.Users u ON u.id = s.user_id
                        INNER JOIN dbo.Persons p ON p.id = u.person_id
                        LEFT JOIN dbo.Staff_Medical_Dossiers d ON d.id = s.staff_medical_dossier_id
                        WHERE s.id = @StaffID AND p.club_id = @ClubID", connection))
                    {
                        command.Parameters.AddWithValue("@StaffID", staffId);
                        command.Parameters.AddWithValue("@ClubID", clubID);

                        await connection.OpenAsync();

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                response = new StaffGetByIdResponse();
                                response.ID = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0;
                                response.Photo = reader["photo"] != DBNull.Value ? reader["photo"].ToString() : null;
                                response.FirstName = reader["FirstName"] != DBNull.Value ? reader["FirstName"].ToString() : null;
                                response.SecondName = reader["SecondName"] != DBNull.Value ? reader["SecondName"].ToString() : null;
                                response.LastName = reader["LastName"] != DBNull.Value ? reader["LastName"].ToString() : null;
                                response.Gender = reader["Gender"] != DBNull.Value ? (bool?)Convert.ToBoolean(reader["Gender"]) : null;
                                response.BirthDate = reader["BirthDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["BirthDate"]) : null;
                                response.Email = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : null;
                                response.PhoneNumber = reader["PhoneNumber"] != DBNull.Value ? reader["PhoneNumber"].ToString() : null;
                                response.Address = reader["Address"] != DBNull.Value ? reader["Address"].ToString() : null;
                                response.PrimaryRoleID = reader["PrimaryRoleID"] != DBNull.Value ? Convert.ToInt32(reader["PrimaryRoleID"]) : 0;
                                response.RoleClassificationID = reader["RoleClassificationID"] != DBNull.Value ? Convert.ToInt32(reader["RoleClassificationID"]) : 0;
                                response.StaffMedicalDossierID = reader["StaffMedicalDossierID"] != DBNull.Value ? (int?)Convert.ToInt32(reader["StaffMedicalDossierID"]) : null;
                                response.BloodTypeID = reader["BloodTypeID"] != DBNull.Value ? (int?)Convert.ToInt32(reader["BloodTypeID"]) : null;
                                response.Allergies = reader["Allergies"] != DBNull.Value ? reader["Allergies"].ToString() : null;
                                response.MedicalNotes = reader["MedicalNotes"] != DBNull.Value ? reader["MedicalNotes"].ToString() : null;
                            }
                        }
                    }
                }

                // Staff member not found for this club -> return null so the API can respond 404.
                if (response == null)
                {
                    return null;
                }

                // Categories attached to this staff member
                using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
                {
                    using (SqlCommand command = new SqlCommand(@"
                        SELECT category_id
                        FROM dbo.Staff_Categories
                        Inner join staff on staff_categories.staff_id = staff.id
                        Inner join users on staff.user_id = users.id
                        Inner join persons on users.person_id = persons.id
                        Inner join clubs on persons.club_id = clubs.id
                        WHERE Staff_Categories.staff_id = @StaffID AND clubs.id = @ClubID", connection))
                    {
                        command.Parameters.AddWithValue("@StaffID", staffId);
                        command.Parameters.AddWithValue("@ClubID", clubID);

                        await connection.OpenAsync();

                        var categories = new List<int>();
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                if (reader["category_id"] != DBNull.Value)
                                {
                                    categories.Add(Convert.ToInt32(reader["category_id"]));
                                }
                            }
                        }
                        response.CategoriesIDs = categories.ToArray();
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new Exception($"A database error occurred while fetching the staff member");
            }
            catch (Exception ex)
            {
                throw new Exception($"An unexpected error occurred");
            }

            return response;
        }

        public static async Task<bool> Update(StaffEditSaveDTO saveDTO, int clubId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
                {
                    await connection.OpenAsync();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Resolve the linked person / user / medical-dossier ids for this staff member.
                            // Club scoping is done through the person link, since Staff has no club_id column.
                            int personId = 0, userId = 0, dossierId = 0;
                            using (var linkCmd = new SqlCommand(@"
                                SELECT u.id AS UserID, u.person_id AS PersonID, s.staff_medical_dossier_id AS DossierID
                                FROM dbo.Staff s
                                INNER JOIN dbo.Users u ON u.id = s.user_id
                                INNER JOIN dbo.Persons p ON p.id = u.person_id
                                WHERE s.id = @StaffID AND p.club_id = @ClubID", connection, transaction))
                            {
                                linkCmd.Parameters.AddWithValue("@StaffID", saveDTO.ID);
                                linkCmd.Parameters.AddWithValue("@ClubID", clubId);

                                using (var reader = await linkCmd.ExecuteReaderAsync())
                                {
                                    if (await reader.ReadAsync())
                                    {
                                        userId = reader["UserID"] != DBNull.Value ? Convert.ToInt32(reader["UserID"]) : 0;
                                        personId = reader["PersonID"] != DBNull.Value ? Convert.ToInt32(reader["PersonID"]) : 0;
                                        dossierId = reader["DossierID"] != DBNull.Value ? Convert.ToInt32(reader["DossierID"]) : 0;
                                    }
                                }
                            }

                            // Staff member does not exist for this club.
                            if (personId == 0 || userId == 0)
                            {
                                transaction.Rollback();
                                return false;
                            }

                            using (var cmd = new SqlCommand(@"
                                UPDATE dbo.Persons
                                SET first_name = @FirstName, second_name = @SecondName, last_name = @LastName,
                                    gender = @Gender, birth_date = @BirthDate
                                WHERE id = @PersonID AND club_id = @ClubID", connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@FirstName", (object)saveDTO.FirstName ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@SecondName", (object)saveDTO.SecondName ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@LastName", (object)saveDTO.LastName ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Gender", saveDTO.Gender);
                                cmd.Parameters.AddWithValue("@BirthDate", saveDTO.BirthDate);
                                cmd.Parameters.AddWithValue("@PersonID", personId);
                                cmd.Parameters.AddWithValue("@ClubID", clubId);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            using (var cmd = new SqlCommand("UPDATE dbo.Users SET email = @Email WHERE id = @UserID", connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@Email", (object)saveDTO.Email ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@UserID", userId);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // Only overwrite the photo when a new one was uploaded (saveDTO.Photo not null).
                            string staffUpdateSql = saveDTO.Photo != null
                                ? @"UPDATE dbo.Staff
                                    SET photo = @Photo, phone_number = @PhoneNumber, address = @Address,
                                        primary_role_id = @PrimaryRoleID, role_classification_id = @RoleClassificationID
                                    WHERE id = @StaffID"
                                : @"UPDATE dbo.Staff
                                    SET phone_number = @PhoneNumber, address = @Address,
                                        primary_role_id = @PrimaryRoleID, role_classification_id = @RoleClassificationID
                                    WHERE id = @StaffID";

                            using (var cmd = new SqlCommand(staffUpdateSql, connection, transaction))
                            {
                                if (saveDTO.Photo != null)
                                    cmd.Parameters.AddWithValue("@Photo", saveDTO.Photo);
                                cmd.Parameters.AddWithValue("@PhoneNumber", (object)saveDTO.PhoneNumber ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Address", (object)saveDTO.Address ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@PrimaryRoleID", saveDTO.PrimaryRoleID);
                                cmd.Parameters.AddWithValue("@RoleClassificationID", saveDTO.RoleClassificationID);
                                cmd.Parameters.AddWithValue("@StaffID", saveDTO.ID);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            if (dossierId > 0)
                            {
                                using (var cmd = new SqlCommand(@"
                                    UPDATE dbo.Staff_Medical_Dossiers
                                    SET blood_type_id = @BloodTypeID, allergies = @Allergies, medical_notes = @MedicalNotes
                                    WHERE id = @DossierID", connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@BloodTypeID", saveDTO.BloodTypeID);
                                    cmd.Parameters.AddWithValue("@Allergies", (object)saveDTO.Allergies ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@MedicalNotes", (object)saveDTO.MedicalNotes ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DossierID", dossierId);
                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }

                            // Replace the staff's category assignments.
                            using (var delCmd = new SqlCommand(@"DELETE sc
                            FROM dbo.Staff_Categories sc
                            INNER JOIN staff ON sc.staff_id = staff.id
                            INNER JOIN users ON staff.user_id = users.id
                            INNER JOIN persons ON users.person_id = persons.id
                            WHERE sc.staff_id = @StaffID AND persons.club_id = @ClubID", connection, transaction))
                            {
                                delCmd.Parameters.AddWithValue("@StaffID", saveDTO.ID);
                                delCmd.Parameters.AddWithValue("@ClubID", clubId);
                                await delCmd.ExecuteNonQueryAsync();
                            }

                            if (saveDTO.CategoriesIDs != null)
                            {
                                foreach (var categoryId in saveDTO.CategoriesIDs)
                                {
                                    using (var insCmd = new SqlCommand("INSERT INTO dbo.Staff_Categories (staff_id, category_id) VALUES (@StaffID, @CategoryID)", connection, transaction))
                                    {
                                        insCmd.Parameters.AddWithValue("@StaffID", saveDTO.ID);
                                        insCmd.Parameters.AddWithValue("@CategoryID", categoryId);
                                        await insCmd.ExecuteNonQueryAsync();
                                    }
                                }
                            }

                            transaction.Commit();
                            return true;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (SqlException ex) when (ex.Number == 547) // 547 is the SQL code for Foreign Key violation
            {
                throw new Exception("The selected role or category is no longer available.", ex);
            }
            catch (SqlException ex)
            {
                throw new Exception($"A database error occurred while updating the staff member: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"An unexpected error occurred while updating the staff member: {ex.Message}", ex);
            }
        }

        // Small helper so the list mappers can safely read the staff id only when the
        // stored procedure actually returns an "id" column.
        private static bool HasColumn(SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        // Reads the staff id from whichever column name the stored procedure happens to
        // use. Returns 0 when no id column is present (the previous behaviour's fallback).
        private static int ReadStaffId(SqlDataReader reader)
        {
            string[] candidates = { "id", "ID", "StaffID", "staff_id", "staffId", "Staff_id", "StaffId" };
            foreach (string name in candidates)
            {
                if (HasColumn(reader, name) && reader[name] != DBNull.Value)
                {
                    return Convert.ToInt32(reader[name]);
                }
            }
            return 0;
        }

        public static async Task<bool> SoftDelete(int staffId, int clubId)
        {

            try
            {
                using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
                {
                    await connection.OpenAsync();

                    // 1. Ensure the is_active column exists. This MUST run in its own batch:
                    //    ALTER TABLE (DDL) and the UPDATE that references the new column (DML)
                    //    cannot share a batch, because SQL Server compiles the whole batch up
                    //    front and fails to bind the not-yet-existing column — so the ALTER
                    //    never executes and the column is never created.
                    using (var ensureCmd = new SqlCommand(@"
                        IF COL_LENGTH('dbo.Staff', 'is_active') IS NULL
                        BEGIN
                            ALTER TABLE dbo.Staff ADD is_active BIT NOT NULL CONSTRAINT DF_Staff_is_active DEFAULT (1);
                        END;", connection))
                    {
                        await ensureCmd.ExecuteNonQueryAsync();
                    }

                    // 2. Perform the soft delete now that the column is guaranteed to exist.
                    using (var cmd = new SqlCommand(@"
                        UPDATE s
                        SET s.is_active = 0
                        FROM dbo.Staff s
                        INNER JOIN dbo.Users u ON u.id = s.user_id
                        INNER JOIN dbo.Persons p ON p.id = u.person_id
                        INNER JOIN dbo.Clubs c ON c.id = p.club_id
                        WHERE s.id = @StaffID
                          AND c.id = @ClubID
                          AND s.is_active = 1;", connection))
                    {
                        cmd.Parameters.AddWithValue("@StaffID", staffId);
                        cmd.Parameters.AddWithValue("@ClubID", clubId);

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("A database error occurred while deleting the staff member.", ex);
            }
        }

    }
}
