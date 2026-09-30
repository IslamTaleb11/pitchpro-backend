using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using BusinessLayer.Helpers;
using Microsoft.AspNetCore.Http;

namespace BusinessLayer
{
    public static class GeneralSettings
    {
        public static int ClubID
        {
            get
            {
                // Resolve the registered IHttpContextAccessor from the DI container.
                var accessor = AppServicesHelper.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
                var claim = accessor.HttpContext?.User?.FindFirst("clubId")?.Value;
                if (int.TryParse(claim, out var id))
                    return id;
                return 0; // fallback if claim missing
            }
        }
    }
}
