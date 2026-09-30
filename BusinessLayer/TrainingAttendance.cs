using BusinessLayer.Exceptions;
using BusinessLayer.Helpers;
using DataAccessLayer.DTOs.TrainingAttendance;
using DataAccessLayer.Providers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;

namespace BusinessLayer
{
    public class TrainingAttendance
    {
        // Records attendance for one or more players in a training session.
        // Validates every player belongs to the club and that the training
        // session belongs to the club. Throws InvalidCallUpDataException on
        // any validation failure. Returns a dictionary of playerId -> attendanceId.
        public static async Task<Dictionary<int, int>> Save(TrainingAttendanceSaveRequestDTO request)
        {
            int clubId = GeneralSettings.ClubID;

            bool sessionBelongs = await TrainingAttendanceProvider.TrainingSessionBelongsToClub(
                request.TrainingSessionID, clubId);
            if (!sessionBelongs)
                throw new InvalidCallUpDataException(
                    "Training session not found or you don't have permission to mark attendance for it.");

            var attendanceIds = new Dictionary<int, int>();

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                foreach (var pair in request.PlayersAttendance)
                {
                    int playerId = pair.Key;
                    bool status = pair.Value;

                    bool playerBelongs = await TrainingAttendanceProvider.PlayerBelongsToClub(playerId, clubId);
                    if (!playerBelongs)
                        throw new InvalidCallUpDataException(
                            $"Player with ID {playerId} does not belong to this club.");

                    var dto = new TrainingAttendanceInsertDTO
                    {
                        PlayerID = playerId,
                        TrainingSessionID = request.TrainingSessionID,
                        Status = status,
                        RecordedAt = DateTime.UtcNow
                    };

                    int existingId = await TrainingAttendanceProvider.IsAlreadyExist(playerId, request.TrainingSessionID);
                    if (existingId != -1)
                    {
                        await TrainingAttendanceProvider.Update(dto);
                        attendanceIds[playerId] = existingId;
                    }
                    else
                    {
                        int newId = await TrainingAttendanceProvider.AddNew(dto);
                        if (newId == -1)
                            throw new InvalidCallUpDataException(
                                $"Failed to record attendance for player ID {playerId}.");
                        attendanceIds[playerId] = newId;
                    }
                }

                scope.Complete();
            }

            return attendanceIds;
        }
    }
}
