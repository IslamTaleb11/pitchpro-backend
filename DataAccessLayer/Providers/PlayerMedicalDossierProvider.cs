using DataAccessLayer.DTOs.PlayerMedicalDossier;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DataAccessLayer.Providers
{
    public class PlayerMedicalDossierProvider
    {
        public static int AddNew(PlayerMedicalDossierRegistrationSaveDTO dto)
        {
            int newId = -1;

            string sql = @"INSERT INTO players_medical_dossiers (blood_type_id, allergies, medical_notes, player_id)
                           VALUES (@BloodTypeID, @Allergies, @MedicalNotes, @PlayerID);
                           SELECT CAST(scope_identity() AS int);";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@BloodTypeID", SqlDbType.Int).Value = dto.BloodTypeID;
                    command.Parameters.Add("@Allergies", SqlDbType.NVarChar, 1000).Value =
                        (object?)dto.Allergies ?? DBNull.Value;
                    command.Parameters.Add("@MedicalNotes", SqlDbType.NVarChar, -1).Value =
                        (object?)dto.MedicalNotes ?? DBNull.Value;
                    command.Parameters.Add("@PlayerID", SqlDbType.Int).Value = dto.PlayerID;

                    try
                    {
                        connection.Open();

                        var result = command.ExecuteScalar();
                        if (result != null)
                        {
                            newId = Convert.ToInt32(result);
                        }
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while adding the player medical dossier.", ex);
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }

            return newId;
        }
    }
}
