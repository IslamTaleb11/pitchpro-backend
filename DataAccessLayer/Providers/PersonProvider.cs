using DataAccessLayer.DTOs.Club;
using DataAccessLayer.DTOs.Person;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Providers
{
    public class PersonProvider
    {

        public static int AddNew(PersonRegistrationSaveDTO personRegistrationSaveDTO, PersonRegistrationResponseDTO personRegistrationResponseDTO)
        {
            // We use 'int' as the return type to capture the newly created ID (Identity)
            int newId = -1;

            // 1. T-SQL Query with Output clause to get the new ID
            string sql = @"INSERT INTO Persons (first_name, second_name, last_name, gender, birth_date, club_id) 
                           VALUES (@FirstName, @SecondName, @LastName, @Gender, @BirthDate, @ClubID);
                           SELECT CAST(scope_identity() AS int);";

            // 2. Wrap in 'using' blocks to ensure connections are closed automatically
            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    // 3. Define parameters with explicit SqlDbTypes to prevent SQL Injection
                    command.Parameters.Add("@FirstName", SqlDbType.NVarChar, 255).Value = personRegistrationSaveDTO.FirstName;
                    command.Parameters.Add("@SecondName", SqlDbType.NVarChar, 255).Value =
                        (object)personRegistrationSaveDTO.SecondName ?? DBNull.Value; 
                    command.Parameters.Add("@LastName", SqlDbType.NVarChar, 255).Value = personRegistrationSaveDTO.LastName;
                    command.Parameters.Add("@Gender", SqlDbType.Bit).Value = personRegistrationSaveDTO.Gender;
                    command.Parameters.Add("@BirthDate", SqlDbType.Date).Value = personRegistrationSaveDTO.BirthDate;
                    command.Parameters.Add("@ClubID", SqlDbType.Int).Value = personRegistrationSaveDTO.ClubID;

                    try
                    {
                        connection.Open();

                        // 4. Execute and get the new Identity ID
                        var result = command.ExecuteScalar();
                        if (result != null)
                        {
                            newId = Convert.ToInt32(result);

                            personRegistrationResponseDTO.ID = newId;
                        }
                    }
                    catch (SqlException ex)
                    {
                        // In a 3-tier app, we log the error and re-throw it 
                        // so the Business Layer can decide how to tell the user.
                        throw new Exception("Database error occurred while adding a new person.", ex);
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


        public static bool DoesPersonExist(int id)
        {
            bool exists = false;

            using (SqlConnection conn = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_DoesPersonExist", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ID", id);

                    // Setup the Output Parameter
                    SqlParameter outputParam = new SqlParameter("@Exists", SqlDbType.Bit)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outputParam);

                    conn.Open();
                    cmd.ExecuteNonQuery();

                    exists = (bool)outputParam.Value;
                }
            }
            return exists;
        }


        public static bool DoesPersonHaveUser(int PersonID)
        {
            bool exists = false;

            string sql = @"SELECT TOP 1 1 FROM Users
            WHERE person_id = @PersonID";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@PersonID", PersonID);
                        connection.Open();
                        object result = command.ExecuteScalar();

                        exists = (result != null);
                    }
                    catch (SqlException ex)
                    {
                        // In production, log 'ex' here
                        throw new Exception("Database error occurred while retrieving data.", ex);
                    }
                }
            }

            return exists;
        }

    }
}
