using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EdayRoom.Models;

namespace EdayRoom.Core
{
    public class Enlace
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Telefono { get; set; }
        public bool Activo { get; set; }

        public Enlace(Movilizador testigo)
        {
            Id = testigo.id;
            Nombre = testigo.nombre;
            Telefono = testigo.numero;
            Activo = testigo.activo;
        }
        public Enlace(TestigoExitPoll testigo)
        {
            Id = testigo.id;
            Nombre = testigo.nombre;
            Telefono = testigo.numero;
            Activo = testigo.activo;
        }
        public Enlace(Testigo testigo)
        {
            Id = testigo.id;
            Nombre = testigo.nombre;
            Telefono = testigo.numero;
            Activo = testigo.activo;
        }
    }

    public class EnlaceMovilizacion : Enlace
    {
        public int? IdCentro { get; set; }
        public EnlaceMovilizacion(Movilizador testigo)
            : base(testigo)
        {
            IdCentro = testigo.id_centro;
        }
    }
    public class EnlacePartipacion : Enlace
    {
        public int? IdMesa { get; set; }
        public EnlacePartipacion(Testigo testigo)
            : base(testigo)
        {

            IdMesa = testigo.id_mesa;
        }
    }
    public class EnlaceExitPoll : Enlace
    {
        public int? IdCentro { get; set; }
        public EnlaceExitPoll(TestigoExitPoll testigo)
            : base(testigo)
        {
            IdCentro = testigo.id_centro;
        }
    }



}
