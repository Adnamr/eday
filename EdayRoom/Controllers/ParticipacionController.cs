using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using EdayRoom.API;
using EdayRoom.API.DataLoading;
using EdayRoom.API.DataTable;
using EdayRoom.Models;
using Newtonsoft.Json;

namespace EdayRoom.Controllers
{
    public class ParticipacionController : Controller
    {

        [Authorize(Roles = "participacion")]
        public ActionResult Index()
        {
            return View("Participacion");
        }

        [Authorize(Roles = "participacion")]
        public string GetStatistics()
        {
            var db = new edayRoomEntities();

            var user = db.users.Single(u => u.username == User.Identity.Name);
            var stats = new ParticipacionStats(user);
            return new JavaScriptSerializer().Serialize(stats);
        }

        [Authorize(Roles = "participacion")]
        public string GetContacts()
        {
            var db = new edayRoomEntities();
            var user = db.users.Single(u => u.username == User.Identity.Name);

            var contactos = (from m in db.Mesas 
                            from c in db.Centroes
                            from t in db.Testigoes
                            from asp in db.AsignacionParticipacions
                            where m.id == t.id_mesa &&
                                   m.id_centro == c.id &&
                                   t.activo &&
                                   asp.id_mesa == m.id && asp.id_user == user.id &&
                                   !m.cerrada
                             orderby m.nextContact ascending 
                             select new ParticipacionContact
                                        {
                                            Centro = m.Centro.Nombre,
                                            QuickCountActive = c.quickCountActive ?? false,
                                            CentroUniqueId = c.unique_id,
                                            Abierta = m.abierta,
                                            Cerrada = m.cerrada,
                                            Estado = m.Centro.unidadGeografica1,
                                            Municipio = m.Centro.unidadGeografica2,
                                            Parroquia = m.Centro.unidadGeografica3,
                                            IdTestigo = t.id,
                                            LastUpdate = m.lastContact,
                                            LastValue = m.participacion,
                                            Mesa = m.numero,
                                            NombreTestigo = t.nombre,
                                            Numero = t.numero,
                                            NextUpdate = m.nextContact,
                                            Votantes = m.votantes,
                                            PreviousValue = m.participacion,
                                            IdMesa = m.id,
                                            IsAlertBlocked = m.alertBlocked,
                                            BlockingAlert = m.Alerta == null ? new AlertaWrapper()
                                            {
                                                Fecha =  DateTime.Now,
                                                Blocking = false,
                                                CanCancel = false,
                                                Name = ""
                                            } : new AlertaWrapper()
                                                {
                                                    Fecha = m.blockDate?? DateTime.Now,
                                                    //FechaStr = ( m.blockDate?? DateTime.Now).ToString("hh:mm tt"),
                                                    Blocking = m.Alerta.blocking,
                                                    CanCancel = true,
                                                    Name = m.Alerta.name
                                                }


                                        }).ToList();

           
            var contactoTimelineCurrent = (from c in contactos where !c.IsAlertBlocked select c).Take(5).ToList();
            var contactoTimelineAlertas = (from c in contactos where c.IsAlertBlocked select c).ToList();

            

            return new JavaScriptSerializer().Serialize(new
                                                            {
                                                                proximos = contactoTimelineCurrent,
                                                                alertas = contactoTimelineAlertas
                                                            });
        }


        #region updates regulares

        [Authorize(Roles = "participacion")]
        public string UpdateParticipacion(ParticipacionContact contacto, int valor, int cola = 0)
        {
            var db = new edayRoomEntities();
            var user = db.users.Single(u => u.username == User.Identity.Name);

            //Reseteo los timelines
            var oldTimelines =
                    (from pt in db.ParticipacionTimelines
                     where
                         pt.activa && pt.id_mesa == contacto.IdMesa
                     select pt);
            foreach (var oldTimeline in oldTimelines)
            {
                oldTimeline.activa = false;
            }

            //Guardar valor en timeline
            var newDate = contacto.QuickCountActive ? DateTime.Now.AddMinutes(45) : DateTime.Now.AddMinutes(60);
            var newTimeline = new ParticipacionTimeline { id_mesa = contacto.IdMesa, fecha = newDate, activa = true };
            db.ParticipacionTimelines.AddObject(newTimeline);
            var newParticipacion = new Participacion
            {
                fecha = DateTime.Now,
                hora = DateTime.Now.Hour,
                min10 = DateTime.Now.Minute/10,
                min30 = DateTime.Now.Minute/30,
                conteo = valor,
                id_mesa = contacto.IdMesa,
                id_testigo = contacto.IdTestigo,
                cola = cola,
                active = true,
                id_parent = null,
                id_user = user.id
            };

            //Guardo los datos a nivel de mesa
            var mesa = db.Mesas.Single(m => m.id == contacto.IdMesa);
            mesa.participacion = valor;
            mesa.lastContact = DateTime.Now;
            mesa.nextContact = newDate;
            mesa.abierta = true;


            //Proyeccion de resutados a centro
            var centro = mesa.Centro;
            var mesasActivas = centro.Mesas1.Where(m => m.participacion != 0).ToArray();
            var votosRegistrados = mesasActivas.Sum(m => m.participacion);
            var votantesEnMesa = mesasActivas.Sum(m => m.votantes);
            var proyeccionCentro = centro.votantes * votosRegistrados / votantesEnMesa;


            centro.lastParticipacionContact = DateTime.Now;
            centro.participacionContada = votosRegistrados;
            centro.participacionProyectada = proyeccionCentro;


            //Participacion oldParticipacion = db.Participacions.Single(p => p.id == contacto.LastParticipacionId);
            //oldParticipacion.active = false;

            db.Participacions.AddObject(newParticipacion);

            IQueryable<ParticipacionAlerta> alertas = from a in db.ParticipacionAlertas
                                                      where a.activa && !a.Alerta.blocking
                                                            && a.id_mesa == contacto.IdMesa
                                                      select a;

            foreach (ParticipacionAlerta a in alertas)
            {
                a.activa = false;
            }

            db.SaveChanges();
            return new JavaScriptSerializer().Serialize("");
        }

        [Authorize(Roles = "participacion")]
        public string CerrarMesa(ParticipacionContact contacto)
        {
            var db = new edayRoomEntities();

            var mesa = db.Mesas.Single(m => m.id == contacto.IdMesa);
            mesa.abierta = false;
            mesa.cerrada = true;
            db.SaveChanges();
            return new JavaScriptSerializer().Serialize(db.SaveChanges());
        }

        [Authorize(Roles = "participacion-lider")]
        public string UpdateSingleParticipacion(int participacionId, int valor, int cola)
        {
            using (var db = new edayRoomEntities())
            {
                Participacion participacion = db.Participacions.SingleOrDefault(p => p.id == participacionId);


                if (participacion != null)
                {
                    var mesa = participacion.Mesa;
                    mesa.participacion = mesa.participacion == participacion.conteo ? valor : Math.Max(mesa.participacion, valor);
                    participacion.conteo = valor;
                    participacion.cola = cola;

                    var centro = mesa.Centro;
                    var mesasActivas = centro.Mesas1.Where(m => m.participacion != 0).ToArray();
                    var votosRegistrados = mesasActivas.Sum(m => m.participacion);
                    var votantesEnMesa = mesasActivas.Sum(m => m.votantes);
                    var proyeccionCentro = centro.votantes * votosRegistrados / votantesEnMesa;
                    centro.participacionContada = votosRegistrados;
                    centro.participacionProyectada = proyeccionCentro;

                }
                db.SaveChanges();
                return new JavaScriptSerializer().Serialize("");
            }
        }

        [Authorize(Roles = "participacion")]
        public string UpdateLastParticipacion(ParticipacionContact contacto, int valor, int cola = 0)
        {
            throw new NotImplementedException();
            //var db = new edayRoomEntities();
            //Participacion oldParticipacion = (from pt in db.Participacions
            //                                  where pt.id == contacto.LastParticipacionId
            //                                  select pt).SingleOrDefault();
            //if (oldParticipacion != null)
            //{
            //    var mesa = oldParticipacion.Mesa;
            //    mesa.participacion = mesa.participacion == oldParticipacion.conteo ? valor : Math.Max(mesa.participacion, valor);
            //    var centro = mesa.Centro;
            //    var mesasActivas = centro.Mesas1.Where(m => m.participacion != 0).ToArray();
            //    var votosRegistrados = mesasActivas.Sum(m => m.participacion);
            //    var votantesEnMesa = mesasActivas.Sum(m => m.votantes);
            //    var proyeccionCentro = centro.votantes * votosRegistrados / votantesEnMesa;

            //    centro.participacionProyectada = proyeccionCentro;
            //    centro.participacionContada = votosRegistrados;

            //    oldParticipacion.conteo = valor;
            //    oldParticipacion.cola = cola;
            //    db.SaveChanges();
            //}


            //return new JavaScriptSerializer().Serialize("success");
        }

        [Authorize(Roles = "participacion")]
        public string AlertaParticipacion(ParticipacionContact contacto, int valor, string mensaje)
        {
            var db = new edayRoomEntities();
            var mesa = db.Mesas.Single(m => m.id == contacto.IdMesa);
            var user = db.users.Single(u => u.username == User.Identity.Name);
            #region Registro la alerta

            var alerta = new ParticipacionAlerta
                             {
                                 activa = true,
                                 comentario =
                                     string.Format("<li><b>{1}</b> - {0}</li>", mensaje, DateTime.Now.ToString("HH:mm")),
                                 fecha = DateTime.Now,
                                 id_mesa = contacto.IdMesa,
                                 id_testigo = contacto.IdTestigo,
                                 id_alerta = valor,
                                 id_usuario = user.id
                             };
           
            db.ParticipacionAlertas.AddObject(alerta);


            var messages = new List<ParticipacionAlertaMessage>();
            if (!string.IsNullOrWhiteSpace(mensaje))
            {
                var alertMessage = new ParticipacionAlertaMessage { fecha = DateTime.Now, mensaje = mensaje, id_usuario = user.id };
                alerta.ParticipacionAlertaMessages.Add(alertMessage);
                messages.Add(alertMessage);
            }

            #endregion

            #region  Retraso de Timeline

            Alerta objetoAlerta = db.Alertas.Single(a => a.id == valor);
            if (objetoAlerta.blocking){
                mesa.alertBlocked = true;
                mesa.blockingAlertId = objetoAlerta.id;
            }
            if (objetoAlerta.regresivo)
            {

                var oldTimelines =
                    (from pt in db.ParticipacionTimelines
                     where
                         pt.activa && pt.id_mesa == contacto.IdMesa
                     //pt.id == contacto.MovilizacionTimelineId
                     select pt);
                foreach (var oldTimeline in oldTimelines)
                {
                    oldTimeline.activa = false;
                }
                //ParticipacionTimeline oldTimeline =
                //    (from pt in db.ParticipacionTimelines where pt.id == contacto.ParticipacionTimelineId select pt).
                //        Single();
                //oldTimeline.activa = false;
                DateTime newDate = DateTime.Now.AddMinutes(objetoAlerta.tiempo);
                var newTimeline = new ParticipacionTimeline { id_mesa = contacto.IdMesa, fecha = newDate, activa = true };
                db.ParticipacionTimelines.AddObject(newTimeline);
            }

            #endregion

            db.SaveChanges();


            // VERIFICAR SI HAY QUE HACER TRIGGER DE ALGUNA ALERTA
            var existingAlerts = from a in db.ParticipacionAlertas
                                 where a.id_mesa == contacto.IdMesa &&
                                       a.id_alerta == valor && a.activa
                                 select a;
            var alertCount = existingAlerts.Count();
            var maxRepeats = existingAlerts.First().Alerta.maxRepeats ?? 0;
            if (maxRepeats != 0)
            {
                if (alertCount == maxRepeats)
                {
                    //LLEGUE AL LIMITE, hago el trigger de la alerta
                    var newAlerta = existingAlerts.First().Alerta.AlertaAsociada;
                    var alertaAuto = new ParticipacionAlerta
                                         {
                                             activa = true,
                                             comentario =
                                                 string.Format("<li><b>{1}</b> - {0}</li>",
                                                               "ALERTA GENERADA AUTOMATICAMENTE",
                                                               DateTime.Now.ToString("HH:mm")),
                                             fecha = DateTime.Now,
                                             id_mesa = contacto.IdMesa,
                                             id_testigo = contacto.IdTestigo,
                                             id_alerta = newAlerta.id,
                                             id_usuario = user.id
                                         };
                    db.ParticipacionAlertas.AddObject(alertaAuto);
                    if (newAlerta.blocking){
                        mesa.alertBlocked = true;
                        mesa.blockingAlertId = newAlerta.id;
                    }
                    var alertMessage = new ParticipacionAlertaMessage { fecha = DateTime.Now, mensaje = "Alerta generada por sistema", id_usuario = user.id };
                    alertaAuto.ParticipacionAlertaMessages.Add(alertMessage);
                }
            }
            db.SaveChanges();
            return new JavaScriptSerializer().Serialize("success");
        }

        [Authorize(Roles = "participacion")]
        public string ActualizarAlertaParticipacion(ParticipacionContact contacto, string mensaje)
        {
            var db = new edayRoomEntities();
            var user = db.users.Single(u => u.username == User.Identity.Name);
            ParticipacionAlerta alerta = (from a in db.ParticipacionAlertas
                                          where a.id == contacto.BlockingAlert.Id
                                          select a).Single();

            if (!string.IsNullOrWhiteSpace(mensaje))
            {
                var alertMessage = new ParticipacionAlertaMessage { fecha = DateTime.Now, mensaje = mensaje, id_usuario = user.id };
                alerta.ParticipacionAlertaMessages.Add(alertMessage);
            }

            db.SaveChanges();

            return new JavaScriptSerializer().Serialize("success");
        }

        [Authorize(Roles = "participacion")]
        public string CancelarAlertaParticipacion(ParticipacionContact contacto, string mensaje)
        {
            var db = new edayRoomEntities();
            var mesa = db.Mesas.Single(m => m.id == contacto.IdMesa);
            var alerta = (from a in db.ParticipacionAlertas
                                          where 
                                          a.Alerta.id == mesa.blockingAlertId &&
                                          a.activa
                                          select a).Single();
 
            mesa.alertBlocked = false;
            mesa.blockingAlertId = null;
            alerta.activa = false;
            alerta.comentario =
                string.Format("<li> <b>{1}</b> - {0} </li>", "Alerta Cancelada", DateTime.Now.ToString("HH:mm")) +
                alerta.comentario;
            db.SaveChanges();

            return new JavaScriptSerializer().Serialize("success");
        }

        #endregion

        #region Administracion

        [Authorize(Roles = "participacion-lider")]
        public ActionResult Admin()
        {
            return View("Admin");
        }

        [Authorize(Roles = "participacion-lider")]
        public ActionResult AdminCentro(int idCentro)
        {
            var db = new edayRoomEntities();
            user user = db.users.Single(u => u.username == User.Identity.Name);
            Centro centro = (from c in db.Centroes
                             where c.id == idCentro
                             select c).Single();
            List<Mesa> mesas = (from m in centro.Mesas1
                                where (user.admin ||
                                      (user.leader && m.Centro.grupo == user.grupo) ||
                                    m.AsignacionParticipacions.Any(ap => ap.id_user == user.id))
                                select m).ToList();

            ViewData["centro"] = centro;
            ViewData["mesas"] = mesas;
            return View("AdminCentro");
        }

        [Authorize(Roles = "participacion-lider")]
        public ActionResult AdminMesa(int idMesa)
        {
            var db = new edayRoomEntities();
            Mesa mesa = (from m in db.Mesas
                         where m.id == idMesa
                         select m).Single();
            IOrderedEnumerable<Participacion> participacion = from p in mesa.Participacions
                                                              where p.id_testigo != null
                                                              orderby p.fecha descending
                                                              select p;

            Participacion lastParticipacion = participacion.FirstOrDefault();

            Debug.Assert(lastParticipacion != null, "lastParticipacion != null");
            var contact = new ParticipacionContact
                              {
                                  Centro = mesa.Centro.Nombre,
                                  IdMesa = mesa.id,
                                  IdTestigo = mesa.Testigoes.First(t => t.activo).id,
                                  Mesa = mesa.numero,
                                  NombreTestigo = mesa.Testigoes.First(t => t.activo).nombre,
                                  Numero = mesa.Testigoes.First(t => t.activo).numero,
                                  Votantes = mesa.votantes,
                                  LastUpdate = mesa.lastContact,
                                  LastValue = mesa.participacion
                              };
            ViewData["mesa"] = mesa;
            ViewData["participacion"] = participacion.ToList();
            ViewData["contact"] = contact;
            return View("AdminMesa");
        }

        [Authorize(Roles = "participacion-lider, dashboard")]
        public string GetChartData(int? idCentro = null, int? idMesa = null)
        {
            var db = new edayRoomEntities();
            var user = db.users.Single(u => u.username == User.Identity.Name);
            int interval = 30;
            if (idCentro != null)
            {
                interval = 5;
            }
            if (idMesa != null)
            {
                interval = 1;
            }
            var data = db.GetParticipacion(idCentro, idMesa, interval, user.id).ToList();
            var datos = new List<object>();
            var valorPrevio = 0;
            foreach (var d in data)
            {
                datos.Add(new
                                    {
                                        value = d.conteo - valorPrevio,
                                        participacion = d.conteo, d.cola,
                                        date = d.fecha,
                                        year = ((DateTime)d.fecha).Year,
                                        month = ((DateTime)d.fecha).Month - 1,
                                        day = ((DateTime)d.fecha).Day,
                                        hour = ((DateTime)d.fecha).Hour,
                                        min = ((DateTime)d.fecha).Minute
                                    });
                valorPrevio = (int)d.conteo;
            }

            return JsonConvert.SerializeObject(new
                            {
                                participacion = datos
                            }, Formatting.Indented);
        }

        [Authorize(Roles = "participacion-lider")]
        public string GetCentros(int iDisplayStart, int iDisplayLength, int sEcho, int iSortCol_0, string sSortDir_0,
                                 string sSearch)
        {
            var dth = new DataTableHelper();
            var db = new edayRoomEntities();
            user user = db.users.Single(u => u.username == User.Identity.Name);
            IQueryable<Centro> centros = from c in db.Centroes
                                         where
                                             (user.admin ||
                                                (user.leader && c.grupo == user.grupo) ||

                                              c.Mesas1.Any(
                                                  m => m.AsignacionParticipacions.Any(ap => ap.id_user == user.id)))
                                         select c;

            dth.iTotalRecords = centros.Count();
            if (!string.IsNullOrEmpty(sSearch))
            {
                centros = centros.Where(c =>
                                        c.Nombre.ToLower().Contains(sSearch.ToLower()) ||
                                        (c.unidadGeografica1 ?? "").ToLower().Contains(sSearch.ToLower()) ||
                                        (c.unidadGeografica2 ?? "").ToLower().Contains(sSearch.ToLower()) ||
                                        (c.unidadGeografica3 ?? "").ToLower().Contains(sSearch.ToLower()) ||
                                        (c.unidadGeografica4 ?? "").ToLower().Contains(sSearch.ToLower()) ||
                                        (c.unidadGeografica5 ?? "").ToLower().Contains(sSearch.ToLower()) ||
                                        (c.unidadGeografica6 ?? "").ToLower().Contains(sSearch.ToLower()) ||
                                        (c.unidadGeografica7 ?? "").ToLower().Contains(sSearch.ToLower()) ||
                                        (c.unidadGeografica8 ?? "").ToLower().Contains(sSearch.ToLower()) ||
                                        c.Direccion.ToLower().Contains(sSearch.ToLower())
                    );
            }
            dth.iTotalDisplayRecords = centros.Count();

            #region ORDER ALERTAS

            switch (sSortDir_0)
            {
                case "asc":
                    switch (iSortCol_0)
                    {
                        case 0:
                            centros = centros.OrderBy(c => c.Nombre);
                            break;
                        case 1:
                            centros = centros.OrderBy(c => c.unidadGeografica1);
                            break;
                        case 2:
                            centros = centros.OrderBy(c => c.unidadGeografica2);
                            break;
                        case 3:
                            centros = centros.OrderBy(c => c.unidadGeografica3);
                            break;
                        case 4:
                            centros = centros.OrderBy(c => c.votantes);
                            break;
                        case 5:
                            centros = centros.OrderBy(c => c.Mesas1.Sum(m => m.Participacions.Max(p => p.conteo)));
                            break;
                    }
                    break;
                case "desc":
                    switch (iSortCol_0)
                    {
                        case 0:
                            centros = centros.OrderByDescending(c => c.Nombre);
                            break;
                        case 1:
                            centros = centros.OrderByDescending(c => c.unidadGeografica1);
                            break;
                        case 2:
                            centros = centros.OrderByDescending(c => c.unidadGeografica2);
                            break;
                        case 3:
                            centros = centros.OrderByDescending(c => c.unidadGeografica3);
                            break;
                        case 4:
                            centros = centros.OrderByDescending(c => c.votantes);
                            break;
                        case 5:
                            centros = centros.OrderByDescending(c => c.Mesas1.Sum(m => m.Participacions.Max(p => p.conteo)));
                            break;
                    }

                    break;
            }

            #endregion

            dth.sEcho = sEcho;
            List<Centro> clist = centros.Select(c => c).Skip(iDisplayStart).Take(iDisplayLength).ToList();

            dth.aaData = new List<List<string>>();

            foreach (var l in clist.Select(c => new List<string>
                                                    {
                                                        string.Format(
                                                            "<a href='#centro={0}' centro-id='{0}' " +
                                                            "class='centro-link'>{1}</a>",
                                                            c.id, c.Nombre),
                                                        c.unidadGeografica1,
                                                        c.unidadGeografica2,
                                                        c.unidadGeografica3,
                                                        c.votantes + "",
                                                        c.Mesas1.Sum(m => m.Participacions.Max(p => p.conteo)) + ""
                                                    }))
            {
                dth.aaData.Add(l);
            }
            return new JavaScriptSerializer().Serialize(dth);
        }

        [Authorize(Roles = "participacion-lider")]
        public ActionResult Usuarios()
        {
            var db = new edayRoomEntities();
            user user = db.users.Single(u => u.username == User.Identity.Name);
            var group = user.grupo;
            var users = db.users.Where(u => u.grupo == group && u.participacion);
            ViewData["users"] = users.ToList();
            return View();
        }

        #endregion

        public string TogglePauseUser(int userId)
        {
            var db = new edayRoomEntities();
            var user = db.users.Single(u => u.id == userId);
            user.paused = !user.paused;

            if (user.paused)
            {
                var usersInGroup = db.users.Where(u => u.participacion && u.grupo == user.grupo && u.id != user.id && !u.paused).ToList();
                var groupCount = usersInGroup.Count;
                var assignedParticipacion = db.AsignacionParticipacions.Where(ap => ap.id_user == user.id).ToList();
                var roundRobin = 0;
                foreach (var ap in assignedParticipacion)
                {
                    db.AsignacionParticipacions.AddObject(new AsignacionParticipacion
                                                              {
                                                                  id_mesa = ap.id_mesa,
                                                                  id_original_user = ap.id_original_user ?? user.id,
                                                                  isReplacement = true,
                                                                  id_user = usersInGroup[roundRobin % groupCount].id
                                                              });
                    roundRobin++;
                }
            }
            else
            {

                var assignedParticipacion = db.AsignacionParticipacions.Where(ap => ap.id_original_user == user.id && ap.isReplacement).ToList();
                foreach (var ap in assignedParticipacion)
                {
                    db.AsignacionParticipacions.DeleteObject(ap);
                }
            }


            db.SaveChanges();
            return user.paused.ToString(CultureInfo.InvariantCulture);
        }
    }
}