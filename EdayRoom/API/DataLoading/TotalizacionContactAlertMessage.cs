using System;

namespace EdayRoom.API.DataLoading
{
    [Serializable]
    public class TotalizacionContactAlertMessage
    {
        private DateTime _fecha;
        public int Id { get; set; }
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