using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Helpers
{
    public class AppServicesHelper
    {
        // This will hold the "Brain" of the application's services
        public static IServiceProvider ServiceProvider { get; set; }
    }
}
