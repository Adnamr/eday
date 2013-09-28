using EdayRoom.Models;

namespace EdayRoom.Core
{
    public class CentroSettings
    {

        
        public bool QuickCountActive { get; set; }
        public bool ExitPollActive { get; set; }
        public bool MovilizacionActive { get; set; }
        public bool Totalizado { get; set; }

       
        public CentroSettings(Centro centroDeVotacion)
        {
            ExitPollActive = centroDeVotacion.exitPollActive ?? false;
            QuickCountActive = centroDeVotacion.quickCountActive ?? false;
            Totalizado = centroDeVotacion.totalizado;
            MovilizacionActive = centroDeVotacion.movilizacion;

        }


    }
}