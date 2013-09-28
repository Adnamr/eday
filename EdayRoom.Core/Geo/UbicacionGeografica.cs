using EdayRoom.Models;

namespace EdayRoom.Core
{

    public class UbicacionGeografica
    {
        public string Adm1 { get; set; }
        public string Adm2 { get; set; }
        public string Adm3 { get; set; }
        public string Adm4 { get; set; }
        public string Adm5 { get; set; }
        public string Adm6 { get; set; }
        public string Adm7 { get; set; }
        public string Adm8 { get; set; }

        public string Address { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public UbicacionGeografica()
        {
        }
        public UbicacionGeografica(Centro c)
        {
            Adm1 = c.unidadGeografica1;
            Adm2 = c.unidadGeografica2;
            Adm3 = c.unidadGeografica3;
            Adm4 = c.unidadGeografica4;
            Adm5 = c.unidadGeografica5;
            Adm6 = c.unidadGeografica6;
            Adm7 = c.unidadGeografica7;
            Adm8 = c.unidadGeografica8;

            Address = c.Direccion;
            //TODO: Agregar latitud y longitud
        }

    }
}

