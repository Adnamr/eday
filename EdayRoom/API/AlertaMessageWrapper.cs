using System;

namespace EdayRoom.API
{
    public class AlertaMessageWrapper
    {
        private DateTime _fecha;
        public String FechaStr { get; set; }

        public DateTime Fecha
        {
            get { return _fecha; }
            set
            {
                _fecha = value;
                FechaStr = value.ToString("hh:mm:ss tt");
            }
        }

        public string User = "";
        public String Message { get; set; }
    }
}