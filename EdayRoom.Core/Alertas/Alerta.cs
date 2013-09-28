using System;
using System.Collections.Generic;
using System.Data.Objects.DataClasses;
using System.Linq;
using System.Text;
using EdayRoom.Models;
using EdayRoom.Security;

//TODO: Unificar el codigo de los inicializadores

namespace EdayRoom.Core
{
    public class Alerta
    {
        public Modulo Modulo { get; set; }
        public List<MensajeAlerta> Mensajes { get; set; }
        public int Id { get; private set; }
        public DateTime Fecha { get; set; }
        public bool Activa { get; set; }
        public string NombreAlerta { get; set; }
        public Usuario Usuario { get; set; }
        public Alerta(MovilizacionAlerta a)
        {
            Modulo = Modulo.Movilizacion;
            InicializarAlerta(a.MovilizacionAlertaMessages,a);
        }


        public Alerta(ParticipacionAlerta a)
        {
            Modulo = Modulo.Participacion;
            InicializarAlerta(a.ParticipacionAlertaMessages, a);

        }

        public Alerta(TotalizacionAlerta a)
        {
            Modulo = Modulo.Totalizacion;
            InicializarAlerta(a.TotalizacionAlertaMessages, a);

        }
        public Alerta(QuickCountAlerta a)
        {
            Modulo = Modulo.Totalizacion;
            InicializarAlerta(a.QuickCountAlertaMessages, a);
        }
        public Alerta(ExitPollAlerta a)
        {
            Modulo = Modulo.Totalizacion;
            InicializarAlerta(a.ExitPollAlertaMessages, a);
            
            
        }

        private void InicializarAlerta(IEnumerable<dynamic> mensajes, dynamic alerta)
        {
            Id = alerta.id;
            Fecha = alerta.fecha;
            Activa = alerta.activa;
            NombreAlerta = ((Models.Alerta) alerta.Alerta).name;
            Usuario = new Usuario(alerta.user);
            Mensajes = mensajes.OrderByDescending(m => m.fecha).Select(m => new MensajeAlerta(m)).ToList();
        }

    }
}
