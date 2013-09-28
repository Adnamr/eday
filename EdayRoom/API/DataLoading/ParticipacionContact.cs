using System;
using System.Collections.Generic;
using System.Data.Objects.DataClasses;
using System.Linq;
using System.Web.Script.Serialization;
using EdayRoom.Models;

namespace EdayRoom.API.DataLoading
{
    public class ParticipationContactAlertMessage
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

    //TODO: Reescribir utilizando las clases en CentroDeVotacion
    public class ParticipacionContact
    {
        public IQueryable<ParticipationContactAlertMessage> MensajesAlerta;
        private IEnumerable<AlertaWrapper> _alertas = new List<AlertaWrapper>();
        private AlertaWrapper _blockingAlert;
        private DateTime? _fechaAlerta;
        private DateTime? _lastUpdate;
        private DateTime? _nextUpdate;
        public bool Abierta, Cerrada;
        public int IdTestigo { get; set; }
        public string NombreTestigo { get; set; }
        public string Numero { get; set; }

        //CENTRO
        public string Centro { get; set; }
        public string CentroUniqueId { get; set; }
        public string Estado { get; set; }
        public string Municipio { get; set; }
        public string Parroquia { get; set; }
        public bool QuickCountActive { get; set; }
        //MESA
        public int IdMesa { get; set; }
        public int Mesa { get; set; }
        public int Votantes { get; set; }


        //Participacion Stats
        public string LastUpdateStr { get; set; }
        public DateTime? LastUpdate
        {
            get { return _lastUpdate; }
            set
            {
                LastUpdateStr = value == null ? "-" : ((DateTime)value).ToString("hh:mm tt");
                _lastUpdate = value;
            }
        }
        public string NextUpdateStr { get; set; }
        public DateTime? NextUpdate
        {
            get { return _nextUpdate; }
            set
            {
                if (value != null)
                {
                    TimeSpan ts = (DateTime)value - DateTime.Now;
                    SecondsToCall = Math.Floor(ts.TotalSeconds);
                }
                NextUpdateStr = value == null ? "-" : ((DateTime)value).ToString("hh:mm tt");
                _nextUpdate = value;
            }
        }
        public double SecondsToCall { get; set; }
        public int LastValue { get; set; }
        public int PreviousValue { get; set; }

        //ALERTAS

        public IEnumerable<AlertaWrapper> Alertas
        {
            get { return _alertas; }
            set { _alertas = value; }
        }

        public bool IsAlertBlocked { get; set; }



        public AlertaWrapper BlockingAlert
        {
            get;
            set;
        }


    }
}