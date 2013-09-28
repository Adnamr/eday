using System;

namespace EdayRoom.API
{
    public class EdayRoomUser
    {
        public String Name { get; set; }
        public String Username { get; set; }
        public String Password { get; set; }
        public String Group { get; set; }
        public bool Admin { get; set; }
        public bool Leader { get; set; }
        public bool Supervisor { get; set; }

        public bool Participacion { get; set; }
        public bool Movilizacion { get; set; }
        public bool ExitPolls { get; set; }
        public bool Totalizacion { get; set; }
        public bool QuickCount { get; set; }
        public bool Dashboard { get; set; }
        public bool Alertas { get; set; }

        //public String salt { get; set; }
        //public String name { get; set; }
    }
}