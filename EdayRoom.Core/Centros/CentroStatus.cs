using System;
using EdayRoom.Models;

namespace EdayRoom.Core
{
    public class CentroStatus
    {
        public int Movilizacion { get; set; }
        public int? MovilizacionTarget { get; set; }
        public int Participacion { get; set; }
        public int ParticipacionProyectada { get; set; }
        public int ParticipacionTarget { get; set; }
        public DateTime? LastMovilizacionUpdate { get; set; }
        public DateTime? LastParticipacionUpdate { get; set; }
        public DateTime? NextMovilizacionUpdate { get; set; }

        public CentroStatus(Centro c)
        {
            Movilizacion = c.movilizacionCount??0;
            MovilizacionTarget = c.movilizacionTarget;
            Participacion = c.participacionContada??0;
            ParticipacionProyectada = c.participacionProyectada??0;
            ParticipacionTarget = c.participacionTarget;
            
            LastMovilizacionUpdate = c.lastMovilizacionContact;
            LastParticipacionUpdate = c.lastParticipacionContact;
            NextMovilizacionUpdate = c.nextMovilizacionContact;
        }

    }
}