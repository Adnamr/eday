using System;

namespace EdayRoom.API.DataLoading
{
    public class CandidatoValue
    {
        private DateTime _fecha;
        public int CandidatoId { get; set; }
        public int TimelineId { get; set; }
        public int Valor { get; set; }
        public string Nombre { get; set; }
        public string Message { get; set; }
        public string Hora { get; set; }

        public DateTime Fecha
        {
            get { return _fecha; }
            set
            {
                Hora = value.ToString("hh:mm tt");
                _fecha = value;
            }
        }
    }
}