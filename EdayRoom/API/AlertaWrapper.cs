using System;
using System.Collections.Generic;

namespace EdayRoom.API
{
    public class    AlertaWrapper
    {
        private DateTime _fecha;
        public String FechaStr { get; set; }

        public DateTime Fecha
        {
            get { return _fecha; }
            set
            {
                _fecha = value;
                FechaStr = value.ToString("dd/MM/yyyy HH:mm");
            }
        }

        public int Id { get; set; }

        public string Name { get; set; }
        public bool Blocking { get; set; }
        public bool Regresivo { get; set; }
        public int Tiempo { get; set; }
        public IEnumerable<AlertaMessageWrapper> Messages { get; set; }
        public bool CanCancel { get; set; }


        //private void GetMessages(int id)
        //{
        //    var db = new edayRoomEntities();
        //    Messages = from m in db.ParticipacionAlertaMessages
        //               where m.alerta_id == id
        //               orderby m.fecha descending
        //               select new AlertaMessageWrapper
        //                          {
        //                              Fecha = m.fecha,
        //                              Message = m.mensaje
        //                          };


        //}
    }
}