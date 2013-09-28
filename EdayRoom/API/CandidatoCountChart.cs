using System.Collections.Generic;

namespace EdayRoom.API
{
    public class CandidatoCountChart
    {
        public List<DateValuePair> Porcentajes;
        public double Votos;
        public float PorcentajeVotos;
        public double Proyeccion;
        public float PorcentajeProyeccion;
        public string Color;
        public string Nombre { get; set; }
    }

    public class DateValuePair
    {
        public int day, hour, min;
        public int month;
        public double value;
        public int year;
    }
}