using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using EdayRoom.API.DataReader;
using EdayRoom.Models;

namespace EdayRoom.Controllers
{
    public class DiagnosticController : Controller
    {
        [Authorize]
        public ViewResult Index()
        {
            var db = new edayRoomEntities();
            ViewData["centros"] = db.Centroes.Count();
            ViewData["mesas"] = db.Mesas.Count();
            ViewData["testigos"] = db.Testigoes.Count();
            ViewData["testigosExitPoll"] = db.TestigoExitPolls.Count();
            ViewData["movilizadores"] = db.Movilizadors.Count();
            ViewData["alertas"] = db.Alertas.Count();
            ViewData["candidatos"] = db.Candidatoes.Count();

            ViewData["timelineParticipacion"] = db.ParticipacionTimelines.Count();
            ViewData["timelineMovilizacion"] = db.MovilizacionTimelines.Count();
            ViewData["timelineExitPoll"] = db.ExitPollTimelines.Count();
            ViewData["timelineQuickCount"] = db.QuickCountTimelines.Count();
            ViewData["timelineTotalizacion"] = db.TotalizacionTimelines.Count();

                        ViewData["asignacionParticipacion"] = db.AsignacionParticipacions.Count();
            ViewData["asignacionMovilizacion"] = db.AsignacionMovilizacions.Count();
            ViewData["asignacionExitPoll"] = db.AsignacionExitPolls.Count();
            ViewData["asignacionQuickCount"] = db.AsignacionQuickCounts.Count();
            ViewData["asignacionTotalizacion"] = db.AsignacionTotalizacions.Count();



            ViewData["usuarios"] = db.users.Count();
            return View();
        }

        public string AssignParticipacion()
        {
            var db = new edayRoomEntities();
            var asignaciones = new List<AsignacionParticipacion>();
            var totalizacion = new List<AsignacionTotalizacion>();
            var quickcount = new List<AsignacionQuickCount>();

            var centrosAsignados = new List<int>();
            var groups = db.Centroes.Where(c => !string.IsNullOrEmpty(c.grupo)).Select(c => c.grupo).Distinct();
            foreach (var g in groups)
            {
                var mesas =
                    (from m in db.Mesas
                     from c in db.Centroes
                     where m.id_centro == c.id && c.grupo == g
                     select m).ToList();
                var mesasCount = mesas.Count();
                if (mesasCount == 0)
                    continue;

                var users = db.users.Where(u => u.grupo == g && u.participacion).ToList();

                var leaderCount = users.Count(u => u.leader && u.participacion);
                var normalCount = users.Count(u => !u.leader && u.participacion);
                var totalUser = 4 * leaderCount + 5 * normalCount;
                var mesasfraction = Math.Ceiling((float)mesasCount / totalUser);
                var normalFraction = mesasfraction * 5;
                var leaderFraction = mesasfraction * 4;
                int assigned = 0;

                #region asignacin de usuarios
                int iteracion = 1;
                bool mesasDisponibles = true;
                while (mesasDisponibles)
                {
                    foreach (var user in users)
                    {
                        if (assigned >= mesasCount)
                        {
                            mesasDisponibles = false;
                            break;
                        }
                        if (user.leader && iteracion % 5 != 0)
                        {
                            asignaciones.Add(new AsignacionParticipacion()
                            {
                                id_mesa = mesas[(assigned)].id,
                                id_user = user.id

                            });
                            totalizacion.Add(new AsignacionTotalizacion()
                            {
                                id_mesa = mesas[(assigned)].id,
                                id_user = user.id

                            });
                            if (mesas[(assigned)].Centro.quickCountActive ?? false)
                            {
                                if (!centrosAsignados.Contains(mesas[(assigned)].id_centro))
                                {
                                    centrosAsignados.Add(mesas[(assigned)].id_centro);
                                    quickcount.Add(new AsignacionQuickCount()
                                    {
                                        id_centro = mesas[(assigned)].id_centro,
                                        id_user = user.id

                                    });
                                }
                            }
                            assigned++;
                        }
                        else
                        {
                            if (!user.leader)
                            {
                                asignaciones.Add(new AsignacionParticipacion()
                                {
                                    id_mesa = mesas[(assigned)].id,
                                    id_user = user.id

                                });
                                totalizacion.Add(new AsignacionTotalizacion()
                                {
                                    id_mesa = mesas[(assigned)].id,
                                    id_user = user.id

                                });
                                if (mesas[(assigned)].Centro.quickCountActive ?? false)
                                {
                                    if (!centrosAsignados.Contains(mesas[(assigned)].id_centro))
                                    {
                                        centrosAsignados.Add(mesas[(assigned)].id_centro);
                                        quickcount.Add(new AsignacionQuickCount()
                                        {
                                            id_centro = mesas[(assigned)].id_centro,
                                            id_user = user.id

                                        });
                                    }
                                }
                                assigned++;
                            }
                        }
                    }

                    iteracion++;
                }
                #endregion

                //foreach (var user in users)
                //{
                //    var cota = user.leader ? leaderFraction : normalFraction;
                //    for (int i = 0; i < cota; i++)
                //    {
                //        if (assigned >= mesasCount)
                //            break;
                //        asignaciones.Add(new AsignacionParticipacion()
                //        {
                //            id_mesa = mesas[(assigned)].id,
                //            id_user = user.id

                //        });
                //        totalizacion.Add(new AsignacionTotalizacion()
                //        {
                //            id_mesa = mesas[(assigned)].id,
                //            id_user = user.id

                //        });
                //        if (mesas[(assigned)].Centro.quickCountActive??false)
                //        {
                //            if (!centrosAsignados.Contains(mesas[(assigned)].id_centro))
                //            {
                //                centrosAsignados.Add(mesas[(assigned)].id_centro);
                //                quickcount.Add(new AsignacionQuickCount()
                //                                   {
                //                                       id_centro = mesas[(assigned)].id_centro,
                //                                       id_user = user.id

                //                                   });
                //            }
                //        }
                //        assigned++;
                //    }
                //}
            }

            using (
             var con = new SqlConnection(ConfigurationManager.ConnectionStrings["dbConnSimple"].ConnectionString)
             )
            {
                con.Open();

                #region Asignar Participacion

                using (var tran = con.BeginTransaction())
                {
                    var bc = new SqlBulkCopy(con,
                                             SqlBulkCopyOptions.CheckConstraints |
                                             SqlBulkCopyOptions.FireTriggers |
                                             SqlBulkCopyOptions.KeepNulls, tran) { BatchSize = 1000, DestinationTableName = "AsignacionParticipacion" };

                    bc.WriteToServer(asignaciones.AsDataReader());

                    tran.Commit();
                }

                #endregion

                #region Asignar Totalizacion

                using (var tran = con.BeginTransaction())
                {
                    var bc = new SqlBulkCopy(con,
                                             SqlBulkCopyOptions.CheckConstraints |
                                             SqlBulkCopyOptions.FireTriggers |
                                             SqlBulkCopyOptions.KeepNulls, tran) { BatchSize = 1000, DestinationTableName = "AsignacionTotalizacion" };

                    bc.WriteToServer(totalizacion.AsDataReader());

                    tran.Commit();
                }

                #endregion

                #region Asignar QuickCount

                using (var tran = con.BeginTransaction())
                {
                    var bc = new SqlBulkCopy(con,
                                             SqlBulkCopyOptions.CheckConstraints |
                                             SqlBulkCopyOptions.FireTriggers |
                                             SqlBulkCopyOptions.KeepNulls, tran) { BatchSize = 1000, DestinationTableName = "AsignacionQuickCount" };

                    bc.WriteToServer(quickcount.AsDataReader());

                    tran.Commit();
                }

                #endregion


                con.Close();
            }

            return new JavaScriptSerializer().Serialize(asignaciones.Select(a => new { a.id_mesa, a.id_user }));

        }

        public string AssignMovilizacion()
        {
            var db = new edayRoomEntities();
            var asignaciones = new List<AsignacionMovilizacion>();

            var groups = db.Centroes.Where(c => !string.IsNullOrEmpty(c.grupoMovilizacion) && c.movilizacion).Select(c => c.grupoMovilizacion).Distinct();
            foreach (var g in groups)
            {
                var centros =
                    (
                     from c in db.Centroes
                     where c.grupoMovilizacion == g
                     && c.movilizacion
                     select c).ToList();
                var centrosCount = centros.Count();
                if (centrosCount == 0)
                    continue;

                var users = db.users.Where(u => u.grupo == g && u.movilizacion).ToList();

                var leaderCount = users.Count(u => u.leader && u.movilizacion);
                var normalCount = users.Count(u => !u.leader && u.movilizacion);
                var totalUser = 4 * leaderCount + 5 * normalCount;
                var centrosfraction = Math.Ceiling((float)centrosCount / totalUser);
                var normalFraction = centrosfraction * 5;
                var leaderFraction = centrosfraction * 4;
                int assigned = 0;

                int iteracion = 1;
                bool centrosDisponibles = true;
                while (centrosDisponibles)
                {
                    foreach (var user in users)
                    {
                        if (assigned >= centrosCount)
                        {
                            centrosDisponibles = false;
                            break;
                        }
                        if (user.leader && iteracion%5 != 0)
                        {
                            asignaciones.Add(new AsignacionMovilizacion()
                                                 {
                                                     id_centro = centros[(assigned)].id,
                                                     id_user = user.id

                                                 });
                            assigned++;
                        }
                        else
                        {
                            if (!user.leader)
                            {
                                asignaciones.Add(new AsignacionMovilizacion()
                                                     {
                                                         id_centro = centros[(assigned)].id,
                                                         id_user = user.id

                                                     });
                                assigned++;
                            }
                        }
                    }

                    iteracion++;
                }

                //foreach (var user in users)
                //{
                //    var cota = user.leader ? leaderFraction : normalFraction;
                //    for (int i = 0; i < cota; i++)
                //    {
                //        if (assigned >= centrosCount)
                //            break;
                //        asignaciones.Add(new AsignacionMovilizacion()
                //        {
                //            id_centro = centros[(assigned)].id,
                //            id_user = user.id

                //        });
                //        assigned++;
                //    }
                //}
            }

            using (
             var con = new SqlConnection(ConfigurationManager.ConnectionStrings["dbConnSimple"].ConnectionString)
             )
            {
                con.Open();

                #region Asignacion Movilizacion Timeline

                using (var tran = con.BeginTransaction())
                {
                    var bc = new SqlBulkCopy(con,
                                             SqlBulkCopyOptions.CheckConstraints |
                                             SqlBulkCopyOptions.FireTriggers |
                                             SqlBulkCopyOptions.KeepNulls, tran)
                                             {
                                                 BatchSize = 1000,
                                                 DestinationTableName = "AsignacionMovilizacion"
                                             };

                    bc.WriteToServer(asignaciones.AsDataReader());

                    tran.Commit();
                }

                #endregion





                con.Close();
            }

            return new JavaScriptSerializer().Serialize(asignaciones.Select(a => new { a.id_centro, a.id_user }));

        }

        public string AssignExitPoll()
        {
            var db = new edayRoomEntities();
            var asignaciones = new List<AsignacionExitPoll>();

            var groups = db.Centroes.Where(c => !string.IsNullOrEmpty(c.grupo)).Select(c => c.grupo).Distinct();
            foreach (var g in groups)
            {
                var centros =
                    (
                     from c in db.Centroes
                     where c.grupo == g && (c.exitPollActive ?? false)
                     select c).ToList();
                var centrosCount = centros.Count();
                if (centrosCount == 0)
                    continue;

                var users = db.users.Where(u => u.grupo == g && u.exitpolls).ToList();

                var leaderCount = users.Count(u => u.leader && u.exitpolls);
                var normalCount = users.Count(u => !u.leader && u.exitpolls);
                var totalUser = 4 * leaderCount + 5 * normalCount;
                var centrosfraction = Math.Ceiling((float)centrosCount / totalUser);
                var normalFraction = centrosfraction * 5;
                var leaderFraction = centrosfraction * 4;
                int assigned = 0;
                foreach (var user in users)
                {
                    var cota = user.leader ? leaderFraction : normalFraction;
                    for (int i = 0; i < cota; i++)
                    {
                        if (assigned >= centrosCount)
                        {
                            int salida = assigned;
                            var a1 = salida;
                            
                            break;
                        }
                        asignaciones.Add(new AsignacionExitPoll()
                        {
                            id_centro = centros[(assigned   )].id,
                            id_user = user.id

                        });
                        assigned++;
                    }
                }
            }

            using (
             var con = new SqlConnection(ConfigurationManager.ConnectionStrings["dbConnSimple"].ConnectionString)
             )
            {
                con.Open();

                #region Asignacion Movilizacion Timeline

                using (var tran = con.BeginTransaction())
                {
                    var bc = new SqlBulkCopy(con,
                                             SqlBulkCopyOptions.CheckConstraints |
                                             SqlBulkCopyOptions.FireTriggers |
                                             SqlBulkCopyOptions.KeepNulls, tran)
                    {
                        BatchSize = 1000,
                        DestinationTableName = "AsignacionExitPoll"
                    };

                    bc.WriteToServer(asignaciones.AsDataReader());

                    tran.Commit();
                }

                #endregion





                con.Close();
            }

            return new JavaScriptSerializer().Serialize(asignaciones.Select(a => new { a.id_centro, a.id_user }));

        }

        public string AssignQuickCount()
        {
            var db = new edayRoomEntities();
            var asignaciones = new List<AsignacionQuickCount>();

            var groups = db.Centroes.Where(c => !string.IsNullOrEmpty(c.grupo)).Select(c => c.grupo).Distinct();
            foreach (var g in groups)
            {
                var centros =
                    (
                     from c in db.Centroes
                     where c.grupo == g && (c.quickCountActive ?? false)
                     select c).ToList();
                var centrosCount = centros.Count();
                if (centrosCount == 0)
                    continue;

                var users = db.users.Where(u => u.grupo == g && u.quickcount).ToList();

                var leaderCount = users.Count(u => u.leader && u.quickcount);
                var normalCount = users.Count(u => !u.leader && u.quickcount);
                var totalUser = 4 * leaderCount + 5 * normalCount;
                var centrosfraction = Math.Ceiling((float)centrosCount / totalUser);
                var normalFraction = centrosfraction * 5;
                var leaderFraction = centrosfraction * 4;
                int assigned = 0;
                foreach (var user in users)
                {
                    var cota = user.leader ? leaderFraction : normalFraction;
                    for (int i = 0; i < cota; i++)
                    {
                        if (assigned >= centrosCount)
                            break;
                        asignaciones.Add(new AsignacionQuickCount()
                        {
                            id_centro = centros[(assigned)].id,
                            id_user = user.id

                        });
                        assigned++;
                    }
                }
            }

            using (
             var con = new SqlConnection(ConfigurationManager.ConnectionStrings["dbConnSimple"].ConnectionString)
             )
            {
                con.Open();

                #region Asignacion Movilizacion Timeline

                using (var tran = con.BeginTransaction())
                {
                    var bc = new SqlBulkCopy(con,
                                             SqlBulkCopyOptions.CheckConstraints |
                                             SqlBulkCopyOptions.FireTriggers |
                                             SqlBulkCopyOptions.KeepNulls, tran)
                    {
                        BatchSize = 1000,
                        DestinationTableName = "AsignacionQuickCount"
                    };

                    bc.WriteToServer(asignaciones.AsDataReader());

                    tran.Commit();
                }

                #endregion





                con.Close();
            }

            return new JavaScriptSerializer().Serialize(asignaciones.Select(a => new { a.id_centro, a.id_user }));

        }
                
        public string AssignTotalizacion()
        {
            var db = new edayRoomEntities();
            var asignaciones = new List<AsignacionTotalizacion>();

            var groups = db.Centroes.Where(c => !string.IsNullOrEmpty(c.grupo)).Select(c => c.grupo).Distinct();
            foreach (var g in groups)
            {
                var mesas =
                    (from m in db.Mesas
                     from c in db.Centroes
                     where m.id_centro == c.id && c.grupo == g
                     select m).ToList();
                var mesasCount = mesas.Count();
                if (mesasCount == 0)
                    continue;

                var users = db.users.Where(u => u.grupo == g && u.totalizacion).ToList();

                var leaderCount = users.Count(u => u.leader && u.totalizacion);
                var normalCount = users.Count(u => !u.leader && u.totalizacion);
                var totalUser = 4 * leaderCount + 5 * normalCount;
                var mesasfraction = Math.Ceiling((float)mesasCount / totalUser);
                var normalFraction = mesasfraction * 5;
                var leaderFraction = mesasfraction * 4;
                int assigned = 0;
                foreach (var user in users)
                {
                    var cota = user.leader ? leaderFraction : normalFraction;
                    for (int i = 0; i < cota; i++)
                    {
                        if (assigned >= mesasCount)
                            break;
                        asignaciones.Add(new AsignacionTotalizacion()
                        {
                            id_mesa = mesas[(assigned)].id,
                            id_user = user.id

                        });
                        assigned++;
                    }
                }
            }

            using (
             var con = new SqlConnection(ConfigurationManager.ConnectionStrings["dbConnSimple"].ConnectionString)
             )
            {
                con.Open();

                #region Asignacion Totalizacion

                using (var tran = con.BeginTransaction())
                {
                    var bc = new SqlBulkCopy(con,
                                             SqlBulkCopyOptions.CheckConstraints |
                                             SqlBulkCopyOptions.FireTriggers |
                                             SqlBulkCopyOptions.KeepNulls, tran) { BatchSize = 1000, DestinationTableName = "AsignacionTotalizacion" };

                    bc.WriteToServer(asignaciones.AsDataReader());

                    tran.Commit();
                }

                #endregion





                con.Close();
            }

            return new JavaScriptSerializer().Serialize(asignaciones.Select(a => new { a.id_mesa, a.id_user }));

        }

        [Authorize]
        public string InitializeTimelinesParticipacion()
        {
            var db = new edayRoomEntities();
            db.ClearParticipacion();
            var mesas = db.Mesas.Include("Testigoes").ToList();
            var tlParticipacion = new List<ParticipacionTimeline>();
            var participacionList = new List<Participacion>();

            foreach (var m in mesas)
            {
                tlParticipacion.Add(new ParticipacionTimeline
                                        {
                                            activa = true,
                                            id_mesa = m.id,
                                            fecha = DateTime.Now
                                        });
                var testigo = m.Testigoes.FirstOrDefault(t => t.activo);

                participacionList.Add(new Participacion
                                          {
                                              active = true,
                                              id_mesa = m.id,
                                              fecha = DateTime.Now,
                                              cola = 0,
                                              conteo = 0,
                                              id_parent = null,
                                              id_testigo = testigo == null ? null : (int?)testigo.id
                                              
                                              
                                          });
            }


            using (
                var con = new SqlConnection(ConfigurationManager.ConnectionStrings["dbConnSimple"].ConnectionString)
                )
            {
                con.Open();

                #region Participacion Timeline

                using (var tran = con.BeginTransaction())
                {
                    var bc = new SqlBulkCopy(con,
                                             SqlBulkCopyOptions.CheckConstraints |
                                             SqlBulkCopyOptions.FireTriggers |
                                             SqlBulkCopyOptions.KeepNulls, tran) { BatchSize = 1000, DestinationTableName = "participacionTimeline" };

                    bc.WriteToServer(tlParticipacion.AsDataReader());

                    tran.Commit();
                }

                #endregion

                #region Participacion Items

                using (var tran = con.BeginTransaction())
                {
                    var bc = new SqlBulkCopy(con,
                                             SqlBulkCopyOptions.CheckConstraints |
                                             SqlBulkCopyOptions.FireTriggers |
                                             SqlBulkCopyOptions.KeepNulls, tran) { BatchSize = 1000, DestinationTableName = "participacion" };

                    bc.WriteToServer(participacionList.AsDataReader());

                    tran.Commit();
                }

                #endregion



                con.Close();
            }



            return "todo inicializado, bien";
        }
        [Authorize]
        public string InitializeTimelinesMovilizacion()
        {
            var db = new edayRoomEntities();
            db.ClearMovilizacion();

            var centros = db.Centroes.Include("Movilizadors").Where(m=>m.movilizacion).ToList();


            var tlMovilizacion = new List<MovilizacionTimeline>();
            var movilizacionList = new List<Movilizacion>();



            foreach (var c in centros)
            {
                tlMovilizacion.Add(new MovilizacionTimeline
                                       {
                                           activa = true,
                                           id_centro = c.id,
                                           fecha = DateTime.Now
                                       });
                var testigo = c.Movilizadors.FirstOrDefault(t => t.activo);
                movilizacionList.Add(new Movilizacion
                                         {
                                             active = true,
                                             id_centro = c.id,
                                             fecha = DateTime.Now,
                                             conteo = 0,
                                             id_parent = null,
                                             
                                             id_movilizador = 
                                                testigo == null? null : (int?)testigo.id
                                         });

            }



            using (
                    var con = new SqlConnection(ConfigurationManager.ConnectionStrings["dbConnSimple"].ConnectionString)
                    )
            {
                con.Open();
                #region Movilizacion Timeline
                using (var tran = con.BeginTransaction())
                {
                    var bc = new SqlBulkCopy(con,
                                             SqlBulkCopyOptions.CheckConstraints |
                                             SqlBulkCopyOptions.FireTriggers |
                                             SqlBulkCopyOptions.KeepNulls, tran) { BatchSize = 1000, DestinationTableName = "movilizacionTimeline" };

                    bc.WriteToServer(tlMovilizacion.AsDataReader());

                    tran.Commit();
                }
                #endregion
                #region Movilizacion Items
                using (var tran = con.BeginTransaction())
                {
                    var bc = new SqlBulkCopy(con,
                                             SqlBulkCopyOptions.CheckConstraints |
                                             SqlBulkCopyOptions.FireTriggers |
                                             SqlBulkCopyOptions.KeepNulls, tran) { BatchSize = 1000, DestinationTableName = "movilizacion" };

                    bc.WriteToServer(movilizacionList.AsDataReader());

                    tran.Commit();
                }
                #endregion
                con.Close();
            }



            return "todo inicializado, bien";
        }
        [Authorize]
        public string InitializeTimelinesExitPoll()
        {
            var db = new edayRoomEntities();
            db.ClearExitPoll();
            var centros = db.Centroes.Include("Movilizadors").Include("TestigoExitPolls").Where(c => c.exitPollActive ?? false).ToList();


            var tlExitPoll = new List<ExitPollTimeline>();
            var exitPollList = new List<ExitPoll>();



            foreach (var c in centros)
            {

                if (c.exitPollActive ?? false)
                {
                    tlExitPoll.Add(new ExitPollTimeline
                    {
                        activa = true,
                        id_centro = c.id,
                        fecha = DateTime.Now,
                        id_testigoExitPoll = c.TestigoExitPolls.First(t => t.activo).id,
                        id_parent = null
                    });
                }
            }



            using (
                    var con = new SqlConnection(ConfigurationManager.ConnectionStrings["dbConnSimple"].ConnectionString)
                    )
            {
                con.Open();
                #region ExitPoll Timeline
                using (var tran = con.BeginTransaction())
                {
                    var bc = new SqlBulkCopy(con,
                                             SqlBulkCopyOptions.CheckConstraints |
                                             SqlBulkCopyOptions.FireTriggers |
                                             SqlBulkCopyOptions.KeepNulls, tran) { BatchSize = 1000, DestinationTableName = "exitpollTimeline" };

                    bc.WriteToServer(tlExitPoll.AsDataReader());

                    tran.Commit();
                }
                #endregion
                con.Close();
            }



            return "todo inicializado, bien";
        }
        [Authorize]
        public string InitializeTimelinesQuickCount()
        {
            var db = new edayRoomEntities();
            db.ClearQuickCount();
            var centros = db.Centroes.Include("Movilizadors").Include("TestigoExitPolls").Where(c => c.quickCountActive ?? false).ToList();
            var tlQuickCount = new List<QuickCountTimeline>();


            foreach (var c in centros)
            {

                if (c.quickCountActive ?? false)
                {
                    tlQuickCount.Add(new QuickCountTimeline
                                         {
                                             activa = true,
                                             id_centro = c.id,
                                             fecha = DateTime.Now,
                                             id_parent = null
                                         });
                }
            }



            using (
                    var con = new SqlConnection(ConfigurationManager.ConnectionStrings["dbConnSimple"].ConnectionString)
                    )
            {
                con.Open();

                #region QuickCount Timeline
                using (var tran = con.BeginTransaction())
                {
                    var bc = new SqlBulkCopy(con,
                                             SqlBulkCopyOptions.CheckConstraints |
                                             SqlBulkCopyOptions.FireTriggers |
                                             SqlBulkCopyOptions.KeepNulls, tran) { BatchSize = 1000, DestinationTableName = "quickcountTimeline" };

                    bc.WriteToServer(tlQuickCount.AsDataReader());

                    tran.Commit();
                }
                #endregion

                con.Close();
            }



            return "todo inicializado, bien";
        }
        [Authorize]
        public string InitializeTimelinesTotalizacion()
        {
            var db = new edayRoomEntities();
            db.ClearTotalizacion();
            var mesas = db.Mesas.Include("Testigoes").ToList();
            var centros = db.Centroes.Include("Movilizadors").Include("TestigoExitPolls").ToList();

            var tlTotalizacion = new List<TotalizacionTimeline>();
            foreach (var m in mesas)
            {
                tlTotalizacion.Add(new TotalizacionTimeline
                {
                    activa = true,
                    id_mesa = m.id,
                    fecha = DateTime.Now
                });
            }

            using (
                    var con = new SqlConnection(ConfigurationManager.ConnectionStrings["dbConnSimple"].ConnectionString)
                    )
            {
                con.Open();

                #region Totalizacion Timeline
                using (var tran = con.BeginTransaction())
                {
                    var bc = new SqlBulkCopy(con,
                                             SqlBulkCopyOptions.CheckConstraints |
                                             SqlBulkCopyOptions.FireTriggers |
                                             SqlBulkCopyOptions.KeepNulls, tran) { BatchSize = 1000, DestinationTableName = "totalizacionTimeline" };

                    bc.WriteToServer(tlTotalizacion.AsDataReader());

                    tran.Commit();
                }
                #endregion

                con.Close();
            }



            return "todo inicializado, bien";
        }

    }
}