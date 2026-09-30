using DataAccessLayer.DTOs.Person;
using DataAccessLayer.DTOs.StaffMedicalDossier;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Providers
{
    public class StaffMedicalDossierProvider
    {
        public static int AddNew(StaffMedicalDossierRegistrationSaveDTO dto)
        {
            // We use 'int' as the return type to capture the newly created ID (Identity)
            int newId = -1;

            // 1. T-SQL Query with Output clause to get the new ID
            string sql = @"INSERT INTO staff_medical_dossiers (blood_type_id, allergies, medical_notes) 
                           VALUES (@BloodTypeID, @Allergies, @MedicalNotes);
                           SELECT CAST(scope_identity() AS int);";

            // 2. Wrap in 'using' blocks to ensure connections are closed automatically
            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    // 3. Define parameters with explicit SqlDbTypes to prevent SQL Injection
                    command.Parameters.Add("@BloodTypeID", SqlDbType.Int).Value = dto.BloodTypeID;
                    command.Parameters.Add("@Allergies", SqlDbType.NVarChar, 255).Value =
                        (object)dto.Allergies ?? DBNull.Value;

                    command.Parameters.Add("@MedicalNotes", SqlDbType.NText).Value =
                        (object)dto.MedicalNotes ?? DBNull.Value;

                    try
                    {
                        connection.Open();

                        // 4. Execute and get the new Identity ID
                        var result = command.ExecuteScalar();
                        if (result != null)
                        {
                            newId = Convert.ToInt32(result);
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


        public static bool IsMedicalDossierLinkedToStaff(int medicalDossierID)
        {
            bool exists = false;

            string sql = @"SELECT TOP 1 1 FROM staff_medical_dossiers
            inner join staff on staff_medical_dossiers.id = staff.staff_medical_dossier_id
            where staff_medical_dossiers.id = @MedicalDossierID";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    try
                    {
                        command.Parameters.AddWithValue("@MedicalDossierID", medicalDossierID);
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
