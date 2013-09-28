using System.Linq;
using EdayRoom.Models;

namespace EdayRoom.API.Settings
{
    public class SettingsHandler
    {
        public bool EnableDashboard = true;
        public bool EnableExitPolls = true;
        public bool EnableMovilizacion = true;
        public bool EnableParticipacion = true;
        public bool EnableQuickCount = true;
        public bool EnableTotalizacion = true;

        public SettingsHandler()
        {
            using (var db = new edayRoomEntities())
            {
                Setting s = db.Settings.First();
                EnableDashboard = s.enableDashboard;
                EnableExitPolls = s.enableExitPoll;
                EnableMovilizacion = s.enableMovilizacion;
                EnableParticipacion = s.enableParticipacion;
                EnableQuickCount = s.enableQuickCount;
                EnableTotalizacion = s.enableTotalizacion;
            }
        }
    }
}