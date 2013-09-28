using System;
using System.Linq;
using EdayRoom.Models;

namespace EdayRoom.API.DataLoading
{
    public class ExitPollStats
    {
        private readonly int _ExitPoll;
        private readonly int _centros;

        private readonly int _mesas;

        private readonly int _participacion;

        public ExitPollStats()
        {
            var randomizer = new Random();
            var db = new edayRoomEntities();

            IQueryable<IGrouping<int, Participacion>> participacion = db.Participacions.GroupBy(p => p.id_mesa);
            int ExitPoll = 0;
            int conteo = Enumerable.Sum(participacion, p => p.OrderByDescending(g => g.fecha).First().conteo);

            _centros = db.Centroes.Count();
            _mesas = db.Mesas.Count();
            _participacion = conteo;
            _ExitPoll = ExitPoll;
        }

        public int Centros
        {
            get { return _centros; }
        }

        public int Mesas
        {
            get { return _mesas; }
        }

        public int Participacion
        {
            get { return _participacion; }
        }

        public int ExitPoll
        {
            get { return _ExitPoll; }
        }
    }
}