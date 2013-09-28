using System;
using System.Collections.Generic;
using System.Linq;
using EdayRoom.Models;

namespace EdayRoom.API
{
    public class TestigoDeMesa
    {
        public int Id { get; set; }
        public int IdMesa { get; set; }
        public string Nombre { get; set; }
        public string Numero { get; set; }
        public bool Activo { get; set; }

        public bool Save()
        {
            using (var db = new edayRoomEntities())
            {
                var testigo = new Testigo
                                  {
                                      id_mesa = IdMesa,
                                      nombre = Nombre,
                                      numero = Numero,
                                      activo = Activo
                                  };

                db.Testigoes.AddObject(testigo);
                db.SaveChanges();
                Id = testigo.id;
                return true;
            }
        }
    }

    public class MesaDeVotacion
    {
        public MesaDeVotacion()
        {
            Testigos = new List<TestigoDeMesa>();
        }

        public string UniqueId { get; set; }
        public int Id { get; set; }
        public int IdCentro { get; set; }
        public int Numero { get; set; }
        public int Votantes { get; set; }
        public int Cuadernos { get; set; }
        public List<TestigoDeMesa> Testigos { get; set; }

        public bool Save()
        {
            using (var db = new edayRoomEntities())
            {
                bool returnValue = true;
                Mesa mesa = (from m in db.Mesas
                             where m.id_centro == IdCentro
                                   && m.uniqueId == UniqueId
                             select m).SingleOrDefault();
                if (mesa == null)
                {
                    mesa = new Mesa
                               {
                                   id_centro = IdCentro,
                                   numero = Numero,
                                   uniqueId = UniqueId,
                                   votantes = Votantes
                               };
                    db.Mesas.AddObject(mesa);
                    mesa.ParticipacionTimelines.Add(new ParticipacionTimeline
                                                        {
                                                            activa = true,
                                                            fecha = DateTime.Now,
                                                        });

                    mesa.Participacions.Add(new Participacion
                                                {
                                                    conteo = 0,
                                                    active = true,
                                                    cola = 0,
                                                    fecha = DateTime.Now,
                                                    id_testigo = null
                                                });

                    db.SaveChanges();
                }
                else
                {
                    returnValue = false;
                }

                Id = mesa.id;

                return returnValue;
            }
        }
    }

    //public class CentroDeVotacion
    //{
    //    #region Campos del centro

    //    public int Id { get; set; }
    //    public string UniqueId { get; set; }
    //    public string Nombre { get; set; }
    //    public string Direccion { get; set; }
    //    public int Cuadernos { get; set; }
    //    public int Mesas { get; set; }
    //    public int Votantes { get; set; }

    //    //Unidades Geograficas. A menor numero mayor la extension de la unidad (UG1 = pais, UG2= estado... etc)
    //    public string UG1 { get; set; }
    //    public string UG2 { get; set; }
    //    public string UG3 { get; set; }
    //    public string UG4 { get; set; }
    //    public string UG5 { get; set; }
    //    public string UG6 { get; set; }
    //    public string UG7 { get; set; }
    //    public string UG8 { get; set; }
    //    public List<MesaDeVotacion> ListaMesas { get; set; }

    //    #endregion

    //    public CentroDeVotacion()
    //    {
    //        ListaMesas = new List<MesaDeVotacion>();
    //    }

    //    /// <summary>
    //    /// Registra un centro en la BBDD
    //    /// </summary>
    //    /// <returns>True: se registro un nuevo centro. False: Ya existia en centro en la BBDD</returns>
    //    public bool Save()
    //    {
    //        using (var db = new edayRoomEntities())
    //        {
    //            Centro centro = (from c in db.Centroes
    //                             where c.unique_id == UniqueId
    //                             select c).SingleOrDefault();
    //            if (centro == null)
    //            {
    //                centro = new Centro
    //                             {
    //                                 unique_id = UniqueId,
    //                                 Nombre = Nombre,
    //                                 unidadGeografica1 = UG1,
    //                                 unidadGeografica2 = UG2,
    //                                 unidadGeografica3 = UG3,
    //                                 unidadGeografica4 = UG4,
    //                                 unidadGeografica5 = UG5,
    //                                 unidadGeografica6 = UG6,
    //                                 unidadGeografica7 = UG7,
    //                                 unidadGeografica8 = UG8,
    //                                 Direccion = Direccion
    //                             };
    //                db.Centroes.AddObject(centro);
    //                db.SaveChanges();
    //            }
    //            else
    //            {
    //                Id = centro.id;
    //                return false;
    //            }
    //            Id = centro.id;
    //            return true;
    //        }
    //    }

    //    /// <summary>
    //    /// Agrega una mesa a un centro de votacion 
    //    /// </summary>
    //    /// <param name="uniqueId">Id unico del cliente</param>
    //    /// <param name="numero">Numero de la mesa en el centro</param>
    //    /// <param name="votantes">Numero de votantes inscritos en la mesa</param>
    //    /// <param name="cuadernos">Numero de cuadernos registrados</param>
    //    /// <returns>True si se registro una nueva mesa, false si la mesa ya estaba en la bbdd</returns>
    //    public int AddMesa(string uniqueId, int numero, int votantes, int cuadernos, ref int mesasCount)
    //    {
    //        using (var db = new edayRoomEntities())
    //        {
    //            var mesa = new MesaDeVotacion
    //                           {
    //                               UniqueId = uniqueId,
    //                               Numero = numero,
    //                               Votantes = votantes,
    //                               Cuadernos = cuadernos,
    //                               IdCentro = Id
    //                           };
    //            if (mesa.Save())
    //            {
    //                Centro centro = (from c in db.Centroes
    //                                 where c.id == Id
    //                                 select c).Single();

    //                centro.votantes += votantes;
    //                Votantes = centro.votantes ?? 0;

    //                centro.mesas++;
    //                Mesas = centro.mesas;

    //                centro.cuadernos += cuadernos;
    //                Cuadernos = centro.cuadernos ?? 0;
    //                mesasCount++;
    //                db.SaveChanges();
    //            }

    //            if (ListaMesas.All(m => m.UniqueId != mesa.UniqueId))
    //            {
    //                ListaMesas.Add(mesa);
    //            }
    //            return mesa.Id;
    //        }
    //    }

    //    /// <summary>
    //    /// Agrega un testigo a una mesa asociada a este centro.
    //    /// </summary>
    //    /// <param name="idMesa">Mesa del testigo</param>
    //    /// <param name="nombre">Nombre del testigo</param>
    //    /// <param name="numero">Numero de contacto</param>
    //    /// <param name="activo">Esta activo?</param>
    //    /// <returns></returns>
    //    public bool AddTestigo(int idMesa, string nombre, string numero, bool activo)
    //    {
    //        var testigo = new TestigoDeMesa
    //                          {
    //                              Activo = activo,
    //                              IdMesa = idMesa,
    //                              Nombre = nombre,
    //                              Numero = numero
    //                          };
    //        testigo.Save();
    //        ListaMesas.Find(m => m.Id == idMesa).Testigos.Add(testigo);
    //        return true;
    //    }

    //    public static string GetUbicacion(Centro c)
    //    {
    //        return string.Format("{0} {1} {2} {3} {4} {5} {6} {7}",
    //                             c.unidadGeografica1,
    //                             c.unidadGeografica2,
    //                             c.unidadGeografica3,
    //                             c.unidadGeografica4,
    //                             c.unidadGeografica5,
    //                             c.unidadGeografica6,
    //                             c.unidadGeografica7,
    //                             c.unidadGeografica8);
    //    }
    //}
}