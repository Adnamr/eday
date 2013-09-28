using System.Linq;
using EdayRoom.Models;

namespace EdayRoom.API.DataLoading
{
    public class ParticipacionStats 
    {

        private readonly int _participacion;

        public ParticipacionStats(user user)
        {
            var db = new edayRoomEntities();
            int conteo = user.admin
                             ? db.Participacions.Where(p => p.active).Sum(p => p.conteo)
                             : (from p in db.Participacions
                                from ap in db.AsignacionParticipacions
                                where p.active && p.id_mesa == ap.id_mesa
                                      && ap.id_user == user.id
                                select p).Sum(p => p.conteo);

        /*    _centros = 
                user.admin? 
                db.Centroes.Count():
                (from m in db.Mesas
                    from ap in db.AsignacionParticipacions
                    where m.id == ap.id_mesa
                    select m.id_centro).Distinct().Count();
            _mesas = user.admin ?
                db.Mesas.Count() :
                (from m in db.Mesas
                 from ap in db.AsignacionParticipacions
                 where m.id == ap.id_mesa
                 select m).Count();*/

            _participacion = conteo;
            //_movilizacion = db.Movilizacions.Where(p => p.active).Sum(p => p.conteo);
        }

       

        public int Participacion
        {
            get { return _participacion; }
        }

     
    }
}