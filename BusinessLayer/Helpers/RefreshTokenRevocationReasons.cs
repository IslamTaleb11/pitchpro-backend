using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Helpers
{
    public class RefreshTokenRevocationReasons
    {
        public enum enRefreshTokenRevocationReasons
        {
            Logout = 1,

            RefreshTokenRotated = 2,

            PasswordChanged = 3,

            TokenReuseDetected = 4,

            AdministratorRevokedSession = 5,

            AccountDisabled = 6,

            UserDeleted = 7
        }
    }
}
