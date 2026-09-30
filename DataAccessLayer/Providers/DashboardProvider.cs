using DataAccessLayer.DTOs.Dashboard;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DataAccessLayer.Providers
{
    public class DashboardProvider
    {
        public static async Task<DashboardCountsResponseDTO> GetCounts(int clubId)
        {
            try
            {
                var counts = new DashboardCountsResponseDTO();

            string sql = @"
                SELECT
                    (SELECT COUNT(*) FROM Categories WHERE club_id = @ClubID AND isDeleted = 0) AS TotalCategories,
                    (SELECT COUNT(*)
                     FROM players_categories pc
                     INNER JOIN categories c ON pc.category_id = c.id
                     WHERE c.club_id = @ClubID AND c.isDeleted = 0) AS TotalPlayers,
                    (SELECT COUNT(*)
                     FROM Staff s
                     INNER JOIN Users u ON s.user_id = u.id
                     INNER JOIN Persons p ON u.person_id = p.id
                     WHERE p.club_id = @ClubID AND s.is_active = 1) AS TotalActiveStaff,
                    (SELECT COUNT(*) FROM Matches WHERE club_id = @ClubID) AS TotalMatches,
                    (SELECT COUNT(*) FROM Matches WHERE club_id = @ClubID AND is_completed = 1) AS TotalCompletedMatches,
                    (SELECT COUNT(*) FROM Matches WHERE club_id = @ClubID AND is_completed = 0 AND CAST(date AS DATE) >= CAST(GETDATE() AS DATE)) AS TotalUpcomingMatches,
                    (SELECT COUNT(*) FROM Training_Sessions WHERE club_id = @ClubID) AS TotalTrainingSessions,
                    (SELECT COUNT(*) FROM Training_Sessions WHERE club_id = @ClubID AND is_completed = 1) AS TotalCompletedTrainingSessions,
                    (SELECT COUNT(*) FROM Training_Sessions WHERE club_id = @ClubID AND is_completed = 0 AND CAST(date AS DATE) >= CAST(GETDATE() AS DATE)) AS TotalUpcomingTrainingSessions,
                    (SELECT COUNT(*)
                     FROM Staff s
                     INNER JOIN Users u ON s.user_id = u.id
                     INNER JOIN Persons p ON u.person_id = p.id
                     INNER JOIN staff_primary_roles pr ON s.primary_role_id = pr.id
                     WHERE p.club_id = @ClubID AND s.is_active = 1 AND pr.name = N'Coach') AS TotalCoachingStaff,
                    (SELECT COUNT(*)
                     FROM Staff s
                     INNER JOIN Users u ON s.user_id = u.id
                     INNER JOIN Persons p ON u.person_id = p.id
                     INNER JOIN staff_primary_roles pr ON s.primary_role_id = pr.id
                     WHERE p.club_id = @ClubID AND s.is_active = 1 AND pr.name = N'Medical') AS TotalMedicalStaff,
                    (SELECT COUNT(*)
                     FROM Staff s
                     INNER JOIN Users u ON s.user_id = u.id
                     INNER JOIN Persons p ON u.person_id = p.id
                     INNER JOIN staff_primary_roles pr ON s.primary_role_id = pr.id
                     WHERE p.club_id = @ClubID AND s.is_active = 1 AND pr.name = N'Fitness Coach') AS TotalFitnessStaff;";

            using (SqlConnection conn = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;

                    await conn.OpenAsync();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            if (reader["TotalCategories"] != DBNull.Value)
                                counts.TotalCategories = Convert.ToInt32(reader["TotalCategories"]);

                            if (reader["TotalPlayers"] != DBNull.Value)
                                counts.TotalPlayers = Convert.ToInt32(reader["TotalPlayers"]);

                            if (reader["TotalActiveStaff"] != DBNull.Value)
                                counts.TotalActiveStaff = Convert.ToInt32(reader["TotalActiveStaff"]);

                            if (reader["TotalMatches"] != DBNull.Value)
                                counts.TotalMatches = Convert.ToInt32(reader["TotalMatches"]);

                            if (reader["TotalCompletedMatches"] != DBNull.Value)
                                counts.TotalCompletedMatches = Convert.ToInt32(reader["TotalCompletedMatches"]);

                            if (reader["TotalUpcomingMatches"] != DBNull.Value)
                                counts.TotalUpcomingMatches = Convert.ToInt32(reader["TotalUpcomingMatches"]);

                            if (reader["TotalTrainingSessions"] != DBNull.Value)
                                counts.TotalTrainingSessions = Convert.ToInt32(reader["TotalTrainingSessions"]);

                            if (reader["TotalCompletedTrainingSessions"] != DBNull.Value)
                                counts.TotalCompletedTrainingSessions = Convert.ToInt32(reader["TotalCompletedTrainingSessions"]);

                            if (reader["TotalUpcomingTrainingSessions"] != DBNull.Value)
                                counts.TotalUpcomingTrainingSessions = Convert.ToInt32(reader["TotalUpcomingTrainingSessions"]);

                            if (reader["TotalCoachingStaff"] != DBNull.Value)
                                counts.TotalCoachingStaff = Convert.ToInt32(reader["TotalCoachingStaff"]);

                            if (reader["TotalMedicalStaff"] != DBNull.Value)
                                counts.TotalMedicalStaff = Convert.ToInt32(reader["TotalMedicalStaff"]);

                            if (reader["TotalFitnessStaff"] != DBNull.Value)
                                counts.TotalFitnessStaff = Convert.ToInt32(reader["TotalFitnessStaff"]);
                        }
                    }
                }
            }

            return counts;
            }
            catch (SqlException ex)
            {
                Console.WriteLine(ex.Message);
                throw new Exception("Database error while fetching dashboard counts.", ex);
            }
        }
    }
}
