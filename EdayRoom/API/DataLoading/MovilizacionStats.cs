using System;
using System.Linq;
using EdayRoom.Models;

namespace EdayRoom.API.DataLoading
{
    public class MovilizacionStats
    {
        private readonly int _centros;

        private readonly int _mesas;
        private readonly int _movilizacion;

      

        public MovilizacionStats(user user)
        {
            var db = new edayRoomEntities();
            _centros = db.AsignacionMovilizacions.Count(am=>am.id_user == user.id);
            _mesas = (from m in db.Mesas
                         from c in db.Centroes
                         from am in db.AsignacionMovilizacions
                         where m.id_centro == c.id
                         && am.id_centro == c.id
                         && am.id_user == user.id
                         select m
                          ).Count();
            _movilizacion = db.AsignacionMovilizacions.Where(m => m.id_user == user.id).Sum(c => c.Centro.Movilizacions.Max(m=>m.conteo));
        }

        public int Centros
        {
            get { return _centros; }
        }

        public int Mesas
        {
            get { return _mesas; }
        }

       

        public int Movilizacion
        {
            get { return _movilizacion; }
        }
    }
}