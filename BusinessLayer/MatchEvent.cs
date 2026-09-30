using BusinessLayer.Exceptions;
using DataAccessLayer.DTOs.MatchEvent;
using DataAccessLayer.Providers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessLayer
{
    public class MatchEvent
    {
        public static async Task<int> Save(MatchEventRequestSaveDTO dto)
        {
            int clubId = GeneralSettings.ClubID;

            bool belongs = await MatchEventProvider.MatchAttendanceBelongsToClub(dto.MatchAttendanceID, clubId);
            if (!belongs)
                throw new InvalidCallUpDataException("The selected match attendance does not belong to this club.");

            return await MatchEventProvider.Save(dto);
        }

        public static async Task Delete(MatchEventDeleteRequestDTO request)
        {
            int clubId = GeneralSettings.ClubID;

            bool belongs = await MatchEventProvider.EventBelongsToClub(request.EventID, clubId, request.MatchID);
            if (!belongs)
                throw new InvalidCallUpDataException("The match event does not belong to this club or match.");

            await MatchEventProvider.Delete(request.EventID);
        }

        public static async Task<List<MatchEventByMatchResponseDTO>> GetByMatch(int matchId)
        {
            int clubId = GeneralSettings.ClubID;

            return await MatchEventProvider.GetByMatch(matchId, clubId);
        }
    }
}