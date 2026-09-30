using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Providers;
using Microsoft.AspNetCore.Http;

namespace BusinessLayer.Services
{
public class PlanLimitService
    {
        private const int FreePlanStaffLimit = 3;
        private const int FreePlanPlayerLimit = 20;
        private const int FreePlanCategoryLimit = 3;
        private const int FreePlanCallUpLimit = 20;
        private const int FreePlanInjuryLimit = 3;
        private const int FreePlanMatchLimit = 3;
        private const int FreePlanTrainingLimit = 3;

        private readonly bool _isPremium;

        public PlanLimitService(IHttpContextAccessor httpContextAccessor)
        {
            string currentPlan = httpContextAccessor.HttpContext?.User?.FindFirst("plan")?.Value;
            _isPremium = string.Equals(currentPlan, "Premium", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<bool> CanCreateStaff()
        {
            if (_isPremium) return true;

            int activeStaffCount = await StaffProvider.CountActiveStaffByClubID(GeneralSettings.ClubID);
            return activeStaffCount < FreePlanStaffLimit;
        }

        public async Task<bool> CanCreatePlayer()
        {
            if (_isPremium) return true;

            int activePlayerCount = await PlayerProvider.CountActivePlayersByClubID(GeneralSettings.ClubID);
            return activePlayerCount < FreePlanPlayerLimit;
        }

        public async Task<bool> CanCreateCategory()
        {
            if (_isPremium) return true;

            int categoryCount = await CategoryProvider.CountCategoriesByClubID(GeneralSettings.ClubID);
            return categoryCount < FreePlanCategoryLimit;
        }

        public async Task<bool> CanAddCallUpPlayers(int matchId, int playersToAdd)
        {
            if (_isPremium) return true;

            int currentCount = await MatchCallUpPlayerProvider.CountByMatch(matchId, GeneralSettings.ClubID);
            return currentCount + playersToAdd <= FreePlanCallUpLimit;
        }

        public async Task<bool> CanCreateInjury(int playerMedicalDossierId)
        {
            if (_isPremium) return true;

            int activeInjuryCount = await PlayerInjuryProvider.CountActiveInjuriesByDossier(playerMedicalDossierId);
            return activeInjuryCount < FreePlanInjuryLimit;
        }

        public async Task<bool> CanCreateMatch()
        {
            if (_isPremium) return true;

            int matchCount = await MatchProvider.CountMatchesByClubID(GeneralSettings.ClubID);
            return matchCount < FreePlanMatchLimit;
        }

        public async Task<bool> CanCreateTrainingSession()
        {
            if (_isPremium) return true;

            int sessionCount = await TrainingSessionProvider.CountSessionsByClubID(GeneralSettings.ClubID);
            return sessionCount < FreePlanTrainingLimit;
        }
    }
}