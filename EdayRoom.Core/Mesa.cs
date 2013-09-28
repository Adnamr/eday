using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EdayRoom.Core.Centros;
using EdayRoom.Models;

namespace EdayRoom.Core
{
    public class MesaStatus
    {
        public bool Abierta { get; set; }
        public bool Cerrada { get; set; }
        public bool Bloqueada { get; set; }
        public DateTime? FechaDeBloqueo { get; set; }
        public DateTime? UltimoContacto { get; set; }
        public DateTime? ProximoContacto { get; set; }
        public bool Totalizada { get; set; }
        public int Participacion { get; set; }
    }
    public class Mesa
    {
        public int Id { get; private set; }
        public string UniqueId { get; private set; }
        public MesaStatus Status { get; set; }
        public int IdCentro { get; private set; }
        private CentroDeVotacion _centro = null;
        public CentroDeVotacion Centro
        {
            get { return _centro ?? (_centro = new CentroDeVotacion(IdCentro)); }
            set { _centro = value;}
        }
        public int Numero { get; set; }
        public int Votantes { get; set; }
        
        public List<EnlacePartipacion   > Enlaces { get; set; }
        public List<Alerta> Alertas { get; set; }


        public Mesa(int id)
        {
            var db = new edayRoomEntities();
            var mesa = db.Mesas.SingleOrDefault(m => m.id == id);
            if (mesa != null)
            {
                InitializeFromEntity(mesa);
            }
            else
            {
                throw new Exception("No hay una mesa con id = " + id);
            }
        }
        public Mesa(Models.Mesa m)
        {
            InitializeFromEntity(m);

        }
        private void InitializeFromEntity(Models.Mesa m)
        {

            Id = m.id;
            UniqueId = m.uniqueId;
            IdCentro = m.id_centro;
            Numero = m.numero;
            Votantes = m.votantes;

            Status = new MesaStatus()
                {
                    Abierta = m.abierta,
                    Cerrada = m.cerrada,
                    Bloqueada = m.alertBlocked,
                    FechaDeBloqueo = m.blockDate,
                    UltimoContacto = m.lastContact,
                    ProximoContacto = m.nextContact,
                    Totalizada = m.totalizada,
                    Participacion = m.participacion
                };
            Enlaces = m.Testigoes.Select(t => new EnlacePartipacion(t)).ToList();

            Alertas = new List<Alerta>();
            Alertas.AddRange(m.ParticipacionAlertas.Select(a => new Alerta(a)));
            Alertas.AddRange(m.TotalizacionAlertas.Select(a => new Alerta(a)));
        }

    }
}
