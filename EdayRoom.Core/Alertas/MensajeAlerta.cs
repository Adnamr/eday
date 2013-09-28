using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EdayRoom.Models;

namespace EdayRoom.Core
{
    public class MensajeAlerta
    {
        private int _idAlerta;
        public int Id { get; private set; }
        public int? IdUsuario { get; private set; }
        public string Mensaje { get; set; }
        public DateTime Fecha { get; set; }
        public MensajeAlerta(MovilizacionAlertaMessage m)
        {
            InitializarMensaje(m.mensaje,m.fecha,m.id_usuario,m.id,m.alerta_id);
        }
        public MensajeAlerta(ParticipacionAlertaMessage m)
        {
            InitializarMensaje(m.mensaje, m.fecha, m.id_usuario, m.id, m.alerta_id);

        }
        public MensajeAlerta(TotalizacionAlertaMessage m)
        {
            InitializarMensaje(m.mensaje, m.fecha, m.id_usuario, m.id, m.alerta_id);

        }
        public MensajeAlerta(ExitPollAlertaMessage m)
        {
            InitializarMensaje(m.mensaje, m.fecha, m.id_usuario, m.id, m.alerta_id);
            
        }
        public MensajeAlerta(QuickCountAlertaMessage m)
        {
            InitializarMensaje(m.mensaje, m.fecha, m.id_usuario, m.id, m.alerta_id);
            
        }

        private void InitializarMensaje(string mensaje, DateTime fecha, int? idUsuario, int id, int idAlerta)
        {
            _idAlerta = idAlerta;
            Id = id;
            IdUsuario = idUsuario;
            Fecha = fecha;
            Mensaje = mensaje;
        }
    }
}
