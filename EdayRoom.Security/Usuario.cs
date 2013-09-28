using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EdayRoom.Models;

namespace EdayRoom.Security
{
    //TODO: Finalizar construccion completa de la clase
    public class Usuario
    {
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string NombreCompleto { get { return Nombre + " " + Apellido; } }
        public Usuario(int id)
        {
            var db = new edayRoomEntities();
            var user = db.users.SingleOrDefault(u => u.id == id);
            InitializeFromEntity(user);
        }

        public Usuario(user u)
        {
            InitializeFromEntity(u);
        }

        private void InitializeFromEntity(user u)
        {
            Nombre = u.nombre;
            Apellido = u.apellido;
            
        }
    }
}
