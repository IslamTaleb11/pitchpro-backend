using DataAccessLayer.DTOs.Player;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DataAccessLayer.Providers
{
    public class PlayerProvider
    {
        public static int AddNew(PlayerRegistrationSaveDTO saveDTO, PlayerRegistrationResponseDTO responseDTO)
        {
            int newId = -1;

            string sql = @"INSERT INTO players (photo, primary_position_id, secondary_position_id, 
                               prefered_foot_id, jersey_number, person_id, address)
                           VALUES (@Photo, @PrimaryPositionID, @SecondaryPositionID,
                               @PreferredFootID, @JerseyNumber, @PersonID, @Address);
                           SELECT CAST(scope_identity() AS int);";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@Photo", SqlDbType.NVarChar, -1).Value = saveDTO.Photo;
                    command.Parameters.Add("@PrimaryPositionID", SqlDbType.Int).Value = saveDTO.PrimaryPositionID;
                    command.Parameters.Add("@SecondaryPositionID", SqlDbType.Int).Value =
                        saveDTO.SecondaryPositionID.HasValue ? (object)saveDTO.SecondaryPositionID.Value : saveDTO.PrimaryPositionID;
                    command.Parameters.Add("@PreferredFootID", SqlDbType.Int).Value = saveDTO.PreferredFootID;
                    command.Parameters.Add("@JerseyNumber", SqlDbType.VarChar, 10).Value = saveDTO.JerseyNumber;
                    command.Parameters.Add("@PersonID", SqlDbType.Int).Value = saveDTO.PersonID;
                    command.Parameters.Add("@Address", SqlDbType.NVarChar, 1000).Value = saveDTO.Address;

                    try
                    {
                        connection.Open();

                        var result = command.ExecuteScalar();
                        if (result != null)
                        {
                            newId = Convert.ToInt32(result);

                            responseDTO.ID = newId;
                            responseDTO.Photo = saveDTO.Photo;

                            responseDTO.PrimaryPositionID = (int)saveDTO.PrimaryPositionID;
                            responseDTO.SecondaryPositionID = saveDTO.SecondaryPositionID ?? 0;
                            responseDTO.PreferredFootID = (int)saveDTO.PreferredFootID;
                            responseDTO.JerseyNumber = saveDTO.JerseyNumber;
                            responseDTO.PersonID = (int)saveDTO.PersonID;
                        }
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while adding a new player.", ex);
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }

            return newId;
        }

        // Links a player to a category by inserting into players_categories.
        // Called inside the player-registration transaction right after the
        // players row is created, so it participates in the same commit/rollback.
        public static async Task AddPlayerCategory(int playerId, int categoryId)
        {
            string sql = @"INSERT INTO players_categories (player_id, category_id)
                           VALUES (@PlayerID, @CategoryID);";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@PlayerID", SqlDbType.Int).Value = playerId;
                    command.Parameters.Add("@CategoryID", SqlDbType.Int).Value = categoryId;

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while linking the player to a category.", ex);
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
        }

        // Returns every player belonging to the given category within a club,
        // exposing their full name and jersey number. Club scoping always comes
        // from the auth token via GeneralSettings.ClubID, matching every other read.
        public static async Task<int> CountActivePlayersByClubID(int clubID)
        {
            await EnsureIsActiveColumnAsync();

            const string sql = @"SELECT COUNT(*)
                                 FROM players pl
                                 INNER JOIN persons pr ON pl.person_id = pr.id
                                 WHERE pr.club_id = @ClubID
                                   AND pl.is_active = 1;";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
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
                    throw new Exception("Database error occurred while counting active players.", ex);
                }
            }
        }


        public static async Task<List<PlayerByCategoryResponseDTO>> GetPlayersByCategory(int categoryId, int clubId)
        {
            var list = new List<PlayerByCategoryResponseDTO>();

            await EnsureIsActiveColumnAsync();

            string sql = @"SELECT pl.id AS playerId,
                                  CONCAT(pr.first_name, ' ', pr.second_name, ' ', pr.last_name) AS fullName,
                                  pl.jersey_number,
                                  md.id AS medicalDossierId
                           FROM players_categories pc
                           INNER JOIN players pl ON pc.player_id = pl.id
                           INNER JOIN persons pr ON pl.person_id = pr.id
                           INNER JOIN players_medical_dossiers md ON pl.id = md.player_id
                           WHERE pc.category_id = @CategoryID
                             AND pr.club_id = @ClubID
                             AND pl.is_active = 1;";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@CategoryID", SqlDbType.Int).Value = categoryId;
                    command.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;

                    try
                    {
                        await connection.OpenAsync();

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            int playerIdColumn = reader.GetOrdinal("playerId");
                            int fullNameColumn = reader.GetOrdinal("fullName");
                            int jerseyColumn = reader.GetOrdinal("jersey_number");
                            int medicalDossierIdColumn = reader.GetOrdinal("medicalDossierId");

                            while (await reader.ReadAsync())
                            {
                                list.Add(new PlayerByCategoryResponseDTO
                                {
                                    PlayerID = reader.GetInt32(playerIdColumn),
                                    FullName = reader.GetString(fullNameColumn),
                                    JerseyNumber = reader.GetString(jerseyColumn),
                                    MedicalDossierID = reader.GetInt32(medicalDossierIdColumn)
                                });
                            }
                        }
                    }
                    catch (SqlException ex)
                    {
                        throw new Exception("Database error occurred while loading players by category.", ex);
                    }
                }
            }

            return list;
        }


        // Returns every player in the given category that currently has NO active
        // injury, for building a call-up / lineup. Selects only the flattened
        // person name, jersey number, and primary position name. "No injuries"
        // means there is no row in player_injuries for the player's medical
        // dossier with is_active = 1. Club scoping always comes from the auth
        // token via GeneralSettings.ClubID, matching every other read.
        public static async Task<List<MatchCallUpPlayerByCategoryResponseDTO>> GetMatchCallUpPlayersByCategory(int categoryId, int clubId, int matchId)
        {
            var list = new List<MatchCallUpPlayerByCategoryResponseDTO>();

            await EnsureIsActiveColumnAsync();

            string sql = @"
        SELECT
    players.id AS playerId,
    CONCAT(persons.first_name, ' ', persons.second_name, ' ', persons.last_name) AS fullName,
    players.jersey_number AS jerseyNumber,
    primary_positions.name AS positionName,
    players.photo AS playerImage,
    categories.name AS categoryName,
    CAST(
        CASE
            WHEN EXISTS
            (
                SELECT 1
                FROM matches_attendance ma
                WHERE ma.match_callup_players_id = match_callup_players.id AND ma.status = 1
            )
            THEN 1
            ELSE 0
        END
        AS BIT)
    AS isCalled, 
    CAST(
        CASE
            WHEN EXISTS
            (
                SELECT 1
                FROM matches_attendance ma
                WHERE ma.match_callup_players_id = match_callup_players.id AND ma.status = 0
            )
            THEN 1
            ELSE 0
        END
        AS BIT)
    AS isAbsent,

	(
    SELECT TOP (1) ma.id
    FROM matches_attendance ma
    WHERE ma.match_callup_players_id = match_callup_players.id
	) AS matchAttendanceID
FROM match_callup_players
INNER JOIN players
    ON match_callup_players.player_id = players.id
INNER JOIN persons
    ON players.person_id = persons.id
INNER JOIN primary_positions
    ON players.primary_position_id = primary_positions.id
INNER JOIN players_categories
    ON players_categories.player_id = players.id
INNER JOIN categories
    ON players_categories.category_id = categories.id
INNER JOIN matches
    ON match_callup_players.match_id = matches.id
WHERE match_callup_players.match_id = @MatchID
  AND matches.club_id = @ClubID
  AND categories.id = @CategoryID
  AND players.is_active = 1
ORDER BY TRY_CAST(players.jersey_number AS INT),
         players.jersey_number;";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@CategoryID", SqlDbType.Int).Value = categoryId;
                command.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;
                command.Parameters.Add("@MatchID", SqlDbType.Int).Value = matchId;

                try
                {
                    await connection.OpenAsync();

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        int playerIdColumn = reader.GetOrdinal("playerId");
                        int fullNameColumn = reader.GetOrdinal("fullName");
                        int playerImageColumn = reader.GetOrdinal("playerImage");
                        int jerseyNumberColumn = reader.GetOrdinal("jerseyNumber");
                        int positionNameColumn = reader.GetOrdinal("positionName");
                        int isCalledColumn = reader.GetOrdinal("isCalled");
                        int isAbsentColumn = reader.GetOrdinal("isAbsent");
                        int matchAttendanceIdColumn = reader.GetOrdinal("matchAttendanceID");

                        while (await reader.ReadAsync())
                        {
                            list.Add(new MatchCallUpPlayerByCategoryResponseDTO
                            {
                                PlayerID = reader.GetInt32(playerIdColumn),
                                PlayerName = reader.GetString(fullNameColumn),
                                PlayerImage = reader.IsDBNull(playerImageColumn)
                                    ? string.Empty
                                    : reader.GetString(playerImageColumn),
                                JerseyNumber = reader.GetString(jerseyNumberColumn),
                                PositionName = reader.GetString(positionNameColumn),
                                IsAlreadyAttended = reader.GetBoolean(isCalledColumn),
                                IsAbsent = reader.GetBoolean(isAbsentColumn),
                                MatchAttendanceID = reader.IsDBNull(matchAttendanceIdColumn)
                                    ? null
                                    : reader.GetInt32(matchAttendanceIdColumn)
                            });
                        }
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine("the error is " + ex.Message);
                    throw new Exception("Database error occurred while loading match call-up players by category.", ex);
                }
            }

            return list;
        }

        // Returns every player in the given category that currently has NO active
        // injury — i.e. either no row exists in player_injuries for that player
        // at all, or every existing injury has is_active = 0. Also indicates
        // whether each player is already called up for the given match. Club
        // scoping comes from the auth token via GeneralSettings.ClubID.
        public static async Task<List<AvailablePlayerByCategoryResponseDTO>> GetAvailablePlayersByCategory(int categoryId, int clubId, int matchId)
        {
            var list = new List<AvailablePlayerByCategoryResponseDTO>();

            await EnsureIsActiveColumnAsync();

            string sql = @"
                SELECT pl.id AS playerId,
                       CONCAT(pr.first_name, ' ', pr.second_name, ' ', pr.last_name) AS fullName,
                       pl.jersey_number AS jerseyNumber,
                       pp.name AS positionName,
                       CAST(
                           CASE
                               WHEN EXISTS (
                                   SELECT 1
                                   FROM match_callup_players mcp
                                   WHERE mcp.match_id = @MatchID AND mcp.player_id = pl.id
                               )
                               THEN 1
                               ELSE 0
                           END
                       AS BIT) AS isCalled
                FROM players_categories pc
                INNER JOIN players pl ON pc.player_id = pl.id
                INNER JOIN persons pr ON pl.person_id = pr.id
                INNER JOIN primary_positions pp ON pl.primary_position_id = pp.id
                LEFT JOIN players_medical_dossiers md ON pl.id = md.player_id
                LEFT JOIN player_injuries pi ON md.id = pi.player_medical_dossier_id AND pi.is_active = 1
                WHERE pc.category_id = @CategoryID
                  AND pr.club_id = @ClubID
                  AND pi.id IS NULL
                  AND pl.is_active = 1
                ORDER BY TRY_CAST(pl.jersey_number AS INT), pl.jersey_number;";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@CategoryID", SqlDbType.Int).Value = categoryId;
                command.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;
                command.Parameters.Add("@MatchID", SqlDbType.Int).Value = matchId;

                try
                {
                    await connection.OpenAsync();

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        int playerIdColumn = reader.GetOrdinal("playerId");
                        int fullNameColumn = reader.GetOrdinal("fullName");
                        int jerseyNumberColumn = reader.GetOrdinal("jerseyNumber");
                        int positionNameColumn = reader.GetOrdinal("positionName");
                        int isCalledColumn = reader.GetOrdinal("isCalled");

                        while (await reader.ReadAsync())
                        {
                            list.Add(new AvailablePlayerByCategoryResponseDTO
                            {
                                PlayerID = reader.GetInt32(playerIdColumn),
                                PlayerName = reader.GetString(fullNameColumn),
                                JerseyNumber = reader.GetString(jerseyNumberColumn),
                                PositionName = reader.GetString(positionNameColumn),
                                IsCalled = reader.GetBoolean(isCalledColumn)
                            });
                        }
                    }
                }
                catch (SqlException ex)
                {
                    throw new Exception("Database error occurred while loading available players by category.", ex);
                }
            }

            return list;
        }

        // Returns the full detail of a single player for a given club, used to
        // prefill the update form. Club scoping comes from the auth token via
        // GeneralSettings.ClubID, matching every other read.
        public static async Task<PlayerGetByIdResponseDTO> GetById(int playerId, int clubId)
        {
            PlayerGetByIdResponseDTO response = null;

            string sql = @"
                SELECT pl.id                    AS playerId,
                       pr.first_name            AS firstName,
                       pr.second_name           AS secondName,
                       pr.last_name             AS lastName,
                       pr.gender                AS gender,
                       pr.birth_date            AS birthDate,
                       pl.photo                 AS photo,
                       pl.primary_position_id   AS primaryPositionID,
                       pl.secondary_position_id AS secondaryPositionID,
                       pl.prefered_foot_id      AS preferredFootID,
                       pl.jersey_number         AS jerseyNumber,
                       pl.address               AS address,
                       pc.category_id           AS categoryID,
                       md.blood_type_id         AS bloodTypeID,
                       md.allergies             AS allergies,
                       md.medical_notes         AS medicalNotes,
                       ap.phone                 AS phone,
                       ap.email                 AS email,
                       mp.guardian_full_name    AS guardianFullName,
                       mp.guardian_phone        AS guardianPhone
                FROM players pl
                INNER JOIN persons pr ON pl.person_id = pr.id
                LEFT JOIN players_categories pc ON pc.player_id = pl.id
                LEFT JOIN players_medical_dossiers md ON md.player_id = pl.id
                LEFT JOIN adult_players_details ap ON ap.player_id = pl.id
                LEFT JOIN minor_players_details mp ON mp.player_id = pl.id
                WHERE pl.id = @PlayerID
                  AND pr.club_id = @ClubID;";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@PlayerID", SqlDbType.Int).Value = playerId;
                command.Parameters.Add("@ClubID", SqlDbType.Int).Value = clubId;

                try
                {
                    await connection.OpenAsync();

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var birthDate = reader.GetDateTime(reader.GetOrdinal("birthDate"));
                            response = new PlayerGetByIdResponseDTO
                            {
                                ID = reader.GetInt32(reader.GetOrdinal("playerId")),
                                FirstName = reader.IsDBNull(reader.GetOrdinal("firstName")) ? null : reader.GetString(reader.GetOrdinal("firstName")),
                                SecondName = reader.IsDBNull(reader.GetOrdinal("secondName")) ? null : reader.GetString(reader.GetOrdinal("secondName")),
                                LastName = reader.IsDBNull(reader.GetOrdinal("lastName")) ? null : reader.GetString(reader.GetOrdinal("lastName")),
                                Gender = reader.GetBoolean(reader.GetOrdinal("gender")),
                                BirthDate = birthDate,
                                Photo = reader.IsDBNull(reader.GetOrdinal("photo")) ? null : reader.GetString(reader.GetOrdinal("photo")),
                                PrimaryPositionID = reader.IsDBNull(reader.GetOrdinal("primaryPositionID")) ? 0 : reader.GetInt32(reader.GetOrdinal("primaryPositionID")),
                                SecondaryPositionID = reader.IsDBNull(reader.GetOrdinal("secondaryPositionID")) ? null : reader.GetInt32(reader.GetOrdinal("secondaryPositionID")),
                                PreferredFootID = reader.IsDBNull(reader.GetOrdinal("preferredFootID")) ? 0 : reader.GetInt32(reader.GetOrdinal("preferredFootID")),
                                JerseyNumber = reader.IsDBNull(reader.GetOrdinal("jerseyNumber")) ? null : reader.GetString(reader.GetOrdinal("jerseyNumber")),
                                Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                                CategoryID = reader.IsDBNull(reader.GetOrdinal("categoryID")) ? 0 : reader.GetInt32(reader.GetOrdinal("categoryID")),
                                BloodTypeID = reader.IsDBNull(reader.GetOrdinal("bloodTypeID")) ? 0 : reader.GetInt32(reader.GetOrdinal("bloodTypeID")),
                                Allergies = reader.IsDBNull(reader.GetOrdinal("allergies")) ? null : reader.GetString(reader.GetOrdinal("allergies")),
                                MedicalNotes = reader.IsDBNull(reader.GetOrdinal("medicalNotes")) ? null : reader.GetString(reader.GetOrdinal("medicalNotes")),
                                Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString(reader.GetOrdinal("phone")),
                                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString(reader.GetOrdinal("email")),
                                GuardianFullName = reader.IsDBNull(reader.GetOrdinal("guardianFullName")) ? null : reader.GetString(reader.GetOrdinal("guardianFullName")),
                                GuardianPhone = reader.IsDBNull(reader.GetOrdinal("guardianPhone")) ? null : reader.GetString(reader.GetOrdinal("guardianPhone")),
                                IsMinor = IsMinor(birthDate)
                            };
                        }
                    }
                }
                catch (SqlException ex)
                {
                    throw new Exception("Database error occurred while loading the player.", ex);
                }
            }

            return response;
        }

        private static bool IsMinor(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age)) age--;
            return age < 18;
        }

        // Updates a player and all of its linked records (person, category link,
        // medical dossier, adult/minor details) inside one transaction. When
        // saveDTO.Photo is null the existing photo is kept. Club scoping comes
        // from the auth token via GeneralSettings.ClubID.
        public static async Task<bool> Update(PlayerUpdateSaveDTO saveDTO, int clubId)
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
                            int personId = 0;
                            int dossierId = 0;

                            using (var linkCmd = new SqlCommand(@"
                                SELECT pl.person_id AS PersonID, md.id AS DossierID
                                FROM players pl
                                INNER JOIN persons pr ON pl.person_id = pr.id
                                LEFT JOIN players_medical_dossiers md ON md.player_id = pl.id
                                WHERE pl.id = @PlayerID
                                  AND pr.club_id = @ClubID;", connection, transaction))
                            {
                                linkCmd.Parameters.AddWithValue("@PlayerID", saveDTO.ID);
                                linkCmd.Parameters.AddWithValue("@ClubID", clubId);

                                using (var reader = await linkCmd.ExecuteReaderAsync())
                                {
                                    if (await reader.ReadAsync())
                                    {
                                        personId = reader["PersonID"] != DBNull.Value ? Convert.ToInt32(reader["PersonID"]) : 0;
                                        dossierId = reader["DossierID"] != DBNull.Value ? Convert.ToInt32(reader["DossierID"]) : 0;
                                    }
                                }
                            }

                            // Player does not exist for this club.
                            if (personId == 0)
                            {
                                transaction.Rollback();
                                return false;
                            }

                            using (var cmd = new SqlCommand(@"
                                UPDATE persons
                                SET first_name = @FirstName, second_name = @SecondName, last_name = @LastName,
                                    gender = @Gender, birth_date = @BirthDate
                                WHERE id = @PersonID AND club_id = @ClubID;", connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@FirstName", saveDTO.FirstName);
                                cmd.Parameters.AddWithValue("@SecondName", (object)saveDTO.SecondName ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@LastName", saveDTO.LastName);
                                cmd.Parameters.AddWithValue("@Gender", saveDTO.Gender);
                                cmd.Parameters.AddWithValue("@BirthDate", saveDTO.BirthDate);
                                cmd.Parameters.AddWithValue("@PersonID", personId);
                                cmd.Parameters.AddWithValue("@ClubID", clubId);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // Gender and BirthDate come from the resolved person row below.
                            string playerUpdateSql = saveDTO.Photo != null
                                ? @"UPDATE players
                                    SET photo = @Photo, primary_position_id = @PrimaryPositionID,
                                        secondary_position_id = @SecondaryPositionID, prefered_foot_id = @PreferredFootID,
                                        jersey_number = @JerseyNumber, address = @Address
                                    WHERE id = @PlayerID;"
                                : @"UPDATE players
                                    SET primary_position_id = @PrimaryPositionID,
                                        secondary_position_id = @SecondaryPositionID, prefered_foot_id = @PreferredFootID,
                                        jersey_number = @JerseyNumber, address = @Address
                                    WHERE id = @PlayerID;";

                            using (var cmd = new SqlCommand(playerUpdateSql, connection, transaction))
                            {
                                if (saveDTO.Photo != null)
                                    cmd.Parameters.AddWithValue("@Photo", saveDTO.Photo);
                                cmd.Parameters.AddWithValue("@PrimaryPositionID", saveDTO.PrimaryPositionID);
                                cmd.Parameters.AddWithValue("@SecondaryPositionID", (object)saveDTO.SecondaryPositionID ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@PreferredFootID", saveDTO.PreferredFootID);
                                cmd.Parameters.AddWithValue("@JerseyNumber", saveDTO.JerseyNumber);
                                cmd.Parameters.AddWithValue("@Address", saveDTO.Address);
                                cmd.Parameters.AddWithValue("@PlayerID", saveDTO.ID);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // Replace the player's category assignment.
                            using (var delCmd = new SqlCommand(@"
                                DELETE FROM players_categories
                                WHERE player_id = @PlayerID;", connection, transaction))
                            {
                                delCmd.Parameters.AddWithValue("@PlayerID", saveDTO.ID);
                                await delCmd.ExecuteNonQueryAsync();
                            }

                            using (var insCmd = new SqlCommand(@"
                                INSERT INTO players_categories (player_id, category_id)
                                VALUES (@PlayerID, @CategoryID);", connection, transaction))
                            {
                                insCmd.Parameters.AddWithValue("@PlayerID", saveDTO.ID);
                                insCmd.Parameters.AddWithValue("@CategoryID", saveDTO.CategoryID);
                                await insCmd.ExecuteNonQueryAsync();
                            }

                            if (dossierId > 0)
                            {
                                using (var cmd = new SqlCommand(@"
                                    UPDATE players_medical_dossiers
                                    SET blood_type_id = @BloodTypeID, allergies = @Allergies, medical_notes = @MedicalNotes
                                    WHERE id = @DossierID;", connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@BloodTypeID", saveDTO.BloodTypeID);
                                    cmd.Parameters.AddWithValue("@Allergies", (object)saveDTO.Allergies ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@MedicalNotes", (object)saveDTO.MedicalNotes ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@DossierID", dossierId);
                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }
                            else
                            {
                                using (var insCmd = new SqlCommand(@"
                                    INSERT INTO players_medical_dossiers (blood_type_id, allergies, medical_notes, player_id)
                                    VALUES (@BloodTypeID, @Allergies, @MedicalNotes, @PlayerID);", connection, transaction))
                                {
                                    insCmd.Parameters.AddWithValue("@BloodTypeID", saveDTO.BloodTypeID);
                                    insCmd.Parameters.AddWithValue("@Allergies", (object)saveDTO.Allergies ?? DBNull.Value);
                                    insCmd.Parameters.AddWithValue("@MedicalNotes", (object)saveDTO.MedicalNotes ?? DBNull.Value);
                                    insCmd.Parameters.AddWithValue("@PlayerID", saveDTO.ID);
                                    await insCmd.ExecuteNonQueryAsync();
                                }
                            }

                            // Replace adult/minor details (age may have changed, so clear both
                            // and re-insert whichever type now applies).
                            using (var delAdult = new SqlCommand(@"DELETE FROM adult_players_details WHERE player_id = @PlayerID;", connection, transaction))
                            {
                                delAdult.Parameters.AddWithValue("@PlayerID", saveDTO.ID);
                                await delAdult.ExecuteNonQueryAsync();
                            }

                            using (var delMinor = new SqlCommand(@"DELETE FROM minor_players_details WHERE player_id = @PlayerID;", connection, transaction))
                            {
                                delMinor.Parameters.AddWithValue("@PlayerID", saveDTO.ID);
                                await delMinor.ExecuteNonQueryAsync();
                            }

                            if (saveDTO.IsMinor)
                            {
                                using (var insCmd = new SqlCommand(@"
                                    INSERT INTO minor_players_details (guardian_full_name, guardian_phone, player_id)
                                    VALUES (@GuardianFullName, @GuardianPhone, @PlayerID);", connection, transaction))
                                {
                                    insCmd.Parameters.AddWithValue("@GuardianFullName", (object)saveDTO.GuardianFullName ?? DBNull.Value);
                                    insCmd.Parameters.AddWithValue("@GuardianPhone", (object)saveDTO.GuardianPhone ?? DBNull.Value);
                                    insCmd.Parameters.AddWithValue("@PlayerID", saveDTO.ID);
                                    await insCmd.ExecuteNonQueryAsync();
                                }
                            }
                            else
                            {
                                using (var insCmd = new SqlCommand(@"
                                    INSERT INTO adult_players_details (phone, email, player_id)
                                    VALUES (@Phone, @Email, @PlayerID);", connection, transaction))
                                {
                                    insCmd.Parameters.AddWithValue("@Phone", (object)saveDTO.Phone ?? DBNull.Value);
                                    insCmd.Parameters.AddWithValue("@Email", (object)saveDTO.Email ?? DBNull.Value);
                                    insCmd.Parameters.AddWithValue("@PlayerID", saveDTO.ID);
                                    await insCmd.ExecuteNonQueryAsync();
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
            catch (SqlException ex) when (ex.Number == 547)
            {
                Console.WriteLine(ex.Message);

                throw new Exception("The selected position, foot preference, or category is no longer available.", ex);
            }
            catch (SqlException ex)
            {
                Console.WriteLine(ex.Message);
                throw new Exception($"A database error occurred while updating the player: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw new Exception($"An unexpected error occurred while updating the player: {ex.Message}", ex);
            }
        }

        // Ensures the players.is_active soft-delete column exists before any read
        // references it. Same guard SoftDelete performs on delete, so squad reads
        // can never fail with "Invalid column name 'is_active'" on a fresh schema.
        private static async Task EnsureIsActiveColumnAsync()
        {
            const string sql = @"
                IF COL_LENGTH('dbo.players', 'is_active') IS NULL
                BEGIN
                    ALTER TABLE dbo.players ADD is_active BIT NOT NULL CONSTRAINT DF_players_is_active DEFAULT (1);
                END;";

            using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }

        // Soft-deletes a player by flipping its is_active flag. The column is
        // added on demand (matching the Staff soft-delete pattern). Deactivated
        // players disappear from every squad list while their historical records
        // (call-ups, attendance, injuries) are preserved. Club scoping comes from
        // the auth token via GeneralSettings.ClubID.
        public static async Task<bool> SoftDelete(int playerId, int clubId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(DataAccessSettings.connectionString))
                {
                    await connection.OpenAsync();

                    using (var ensureCmd = new SqlCommand(@"
                        IF COL_LENGTH('dbo.players', 'is_active') IS NULL
                        BEGIN
                            ALTER TABLE dbo.players ADD is_active BIT NOT NULL CONSTRAINT DF_players_is_active DEFAULT (1);
                        END;", connection))
                    {
                        await ensureCmd.ExecuteNonQueryAsync();
                    }

                    using (var cmd = new SqlCommand(@"
                        UPDATE pl
                        SET pl.is_active = 0
                        FROM dbo.players pl
                        INNER JOIN dbo.persons pr ON pl.person_id = pr.id
                        INNER JOIN dbo.clubs c ON c.id = pr.club_id
                        WHERE pl.id = @PlayerID
                          AND c.id = @ClubID
                          AND pl.is_active = 1;", connection))
                    {
                        cmd.Parameters.AddWithValue("@PlayerID", playerId);
                        cmd.Parameters.AddWithValue("@ClubID", clubId);

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("A database error occurred while deleting the player.", ex);
            }
        }
    }
}
