using System;
using System.Collections.Generic;
using System.Data.Objects.DataClasses;
using System.Linq;
using System.Web.Script.Serialization;
using EdayRoom.Models;

namespace EdayRoom.API.DataLoading
{
    public class ExitPollContactAlertMessage
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
    public class ExitPollContact
    {
        public IQueryable<ExitPollContactAlertMessage> MensajesAlerta;
        public IQueryable<CandidatoValue> Valores;

        private DateTime? _fechaAlerta;
        private DateTime? _lastUpdate;
        private DateTime? _nextUpdate;

        public int? ExitPollIdParent { get; set; }


        public int IdTestigo { get; set; }
        public string NombreTestigo { get; set; }
        public string Numero { get; set; }

        //CENTRO
        public string Centro { get; set; }
        public int CentroId { get; set; }
        public int Votantes { get; set; }
        public string Estado { get; set; }
        public string Municipio { get; set; }
        public string Parroquia { get; set; }
        public bool QuickCountActive { get; set; }

        //ExitPoll Stats
        public int ExitPollTimelineId { get; set; }
        public string LastUpdateStr { get; set; }

        public DateTime? LastUpdate
        {
            get { return _lastUpdate; }
            set
            {
                LastUpdateStr = value == null ? "-" : ((DateTime) value).ToString("hh:mm tt");
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
                    TimeSpan ts = (DateTime) value - DateTime.Now;
                    SecondsToCall = Math.Floor(ts.TotalSeconds);
                }
                NextUpdateStr = value == null ? "-" : ((DateTime) value).ToString("hh:mm tt");
                _nextUpdate = value;
            }
        }


        public double SecondsToCall { get; set; }

        //public IQueryable<ExitPollCandidatoValue> LastValues { get; set; }
        //public IQueryable<ExitPollCandidatoValue> PreviousValues { get; set; }

        public int LastExitPollId { get; set; }


       
        public IEnumerable<AlertaWrapper> Alertas { get; set; }
        public bool IsAlertBlocked { get; set; }

        private AlertaWrapper _blockingAlert;
        [ScriptIgnore]
        public Alerta LinqAlertaObj;
        [ScriptIgnore]
        public ExitPollAlerta LinqExitrPollAlertaObj
        {
            set
            {
                BlockingAlert = value == null
                                    ? null
                                    : new AlertaWrapper
                                    {
                                        Fecha = value.fecha,
                                        Id = value.id,
                                        Name = LinqAlertaObj.name,
                                        Blocking = LinqAlertaObj.blocking,
                                        Regresivo = LinqAlertaObj.regresivo,
                                        Tiempo = LinqAlertaObj.tiempo,
                                        CanCancel =
                                            //TODO: ARREGLAR ESTA PARTE
                                            LinqAlertaObj.canceledBy == "all"
                                    };
            }
        }
        [ScriptIgnore]
        public EntityCollection<ExitPollAlertaMessage> LinqExitPollAlertaMessagesObj
        {
            set
            {
                if (value != null)
                {
                    BlockingAlert.Messages = value.Select(a => new AlertaMessageWrapper
                    {
                        Fecha = a.fecha,
                        Message = a.mensaje,
                        User = a.user.nombre + " " + a.user.apellido
                    });

                }
            }
        }

        public AlertaWrapper BlockingAlert
        {
            get { return _blockingAlert; }
            set { _blockingAlert = value;
                IsAlertBlocked = value != null;
            }
        }

        public bool ActiveAlert { get; set; }
        public int IdAlerta { get; set; }
        public string TipoAlerta { get; set; }

        public DateTime? FechaAlerta
        {
            get { return _fechaAlerta; }
            set
            {
                FechaAlertaStr = value == null ? "-" : ((DateTime) value).ToString("dd/MM/yyyy hh:mm tt");
                _fechaAlerta = value;
            }
        }

        public string FechaAlertaStr { get; set; }
    }
}