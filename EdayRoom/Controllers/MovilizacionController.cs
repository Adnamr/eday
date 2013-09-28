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

namespace EdayRoom.Controllers
{
    public class MovilizacionController : Controller
    {
        //
        // GET: /Movilizacion/
        [Authorize(Roles = "movilizacion")]
        public ActionResult Index()
        {
            return View("Movilizacion");
        }
        [Authorize(Roles = "movilizacion")]
        public string GetStatistics()
        {
            var db = new edayRoomEntities();
            var user = db.users.Single(u => u.username == User.Identity.Name);
            var stats = new MovilizacionStats(user);
            return new JavaScriptSerializer().Serialize(stats);
        }

        [Authorize(Roles = "movilizacion")]
        public string GetContacts()
        {
            var db = new edayRoomEntities();
            
            var user = db.users.Single(u => u.username == User.Identity.Name);
            var contactos = (from c in db.Centroes
                             from t in db.Movilizadors
                             from pt in db.MovilizacionTimelines
                             from am in db.AsignacionMovilizacions
                             from p in db.Movilizacions
                             join a in db.MovilizacionAlertas.Include("Alerta") on
                             new { id_centro = c.id, blocking = true, activa = true } equals
                             new { a.id_centro, blocking = a.Alerta.blocking, a.activa }
                             into alertas2
                             from pa in alertas2.DefaultIfEmpty()
                             where
                                   p.id_movilizador == t.id &&
                                   c.id == pt.id_centro &&
                                   p.id_centro == c.id &&
                                   pt.activa && t.activo &&
                                   am.id_user == user.id &&
                                   am.id_centro == c.id && 
                                   p.active && c.movilizacion
                             orderby pt.fecha ascending
                             select new MovilizacionContact
                                        {
                                            Centro = c.Nombre,
                                            CentroUniqueId = c.unique_id,
                                            QuickCountActive = c.quickCountActive ?? false,

                                            Estado = c.unidadGeografica1,
                                            Municipio = c.unidadGeografica2,
                                            Parroquia = c.unidadGeografica3,

                                            IdTestigo = t.id,
                                            LastUpdate = p.fecha,
                                            LastValue = p.conteo,
                                            MovilizacionIdParent = p.id_parent,
                                            LastMovilizacionId = p.id,
                                            NombreTestigo = t.nombre,
                                            Numero = t.numero,
                                            NextUpdate = pt.fecha,
                                            Votantes = c.votantes ?? 0,
                                            MovilizacionTimelineId = pt.id,
                                            PreviousValue = p.Previo == null ? 0 : p.Previo.conteo,
                                            IdCentro = c.id,
                                            LinqAlertaObj = pa == null ? null : pa.Alerta,
                                            LinqMovilizacionAlertaObj = pa,
                                            LinqMovilizacionAlertaMessagesObj = pa.MovilizacionAlertaMessages,
                                            IsAlertBlocked = pa != null
                                        }).ToList();

            var contactoTimelineCurrent = (from c in contactos where !c.IsAlertBlocked select c).Take(3).ToList();
            var contactoTimelineAlertas = (from c in contactos where c.IsAlertBlocked select c).OrderBy(c => c.BlockingAlert.Fecha).ToList();


            return new JavaScriptSerializer().Serialize(new
                                                            {
                                                                proximos = contactoTimelineCurrent,
                                                                alertas = contactoTimelineAlertas
                                                            });
        }

        #region updates regulares

        [Authorize(Roles = "movilizacion")]
        public string UpdateMovilizacion(MovilizacionContact contacto, int valor, int cola = 0)
        {
            var db = new edayRoomEntities();
            var user = db.users.Single(u => u.username == User.Identity.Name);
            //Reseteo los timelines activos
            var oldTimelines =
                    (from pt in db.MovilizacionTimelines
                     where
                         pt.activa && pt.id_centro == contacto.IdCentro
                     select pt);
            foreach (var oldTimeline in oldTimelines)
            {
                oldTimeline.activa = false;
            }

            var newDate = contacto.QuickCountActive ? DateTime.Now.AddMinutes(45) : DateTime.Now.AddMinutes(60); 
           
            //Actualizo los timelines
            var newTimeline = new MovilizacionTimeline { id_centro = contacto.IdCentro, fecha = newDate, activa = true };
            db.MovilizacionTimelines.AddObject(newTimeline);
            var newMovilizacion = new Movilizacion
                                      {
                                          fecha = DateTime.Now,
                                          hora = DateTime.Now.Hour,
                                          min10 = DateTime.Now.Minute / 10,
                                          min30 = DateTime.Now.Minute / 30,

                                          conteo = valor,
                                          id_centro = contacto.IdCentro,
                                          id_movilizador = contacto.IdTestigo,
                                          active = true,
                                          id_parent = contacto.LastMovilizacionId,
                                          id_user = user.id
                                      };



            //Actualizo los datos a nivel de centro
            var centro = db.Centroes.Single(c => c.id == contacto.IdCentro);
            centro.movilizacionCount = valor;
            centro.lastMovilizacionContact = DateTime.Now;
            centro.nextMovilizacionContact = newDate;


            Movilizacion oldMovilizacion = db.Movilizacions.Single(p => p.id == contacto.LastMovilizacionId);
            oldMovilizacion.active = false;

            db.Movilizacions.AddObject(newMovilizacion);

            //Desactivo las alertas que esten pendientes en el centro
            var alertas = from a in db.MovilizacionAlertas
                                                     where a.activa && !a.Alerta.blocking
                                                           && a.id_centro == contacto.IdCentro
                                                     select a;

            foreach (var a in alertas)
            {
                a.activa = false;
            }

            db.SaveChanges();
            return new JavaScriptSerializer().Serialize("");
        }

        [Authorize(Roles = "movilizacion")]
        public string UpdateSingleMovilizacion(int movilizacionId, int valor)
        {
            using (var db = new edayRoomEntities())
            {
                var movilizacion = db.Movilizacions.SingleOrDefault(p => p.id == movilizacionId);
                if (movilizacion != null)
                {
                    movilizacion.conteo = valor;
                }
                db.SaveChanges();
                return new JavaScriptSerializer().Serialize("");
            }
        }

        [Authorize(Roles = "movilizacion")]
        public string UpdateLastMovilizacion(MovilizacionContact contacto, int valor, int cola = 0)
        {
            var db = new edayRoomEntities();
            Movilizacion oldMovilizacion = (from pt in db.Movilizacions
                                            where pt.id == contacto.LastMovilizacionId
                                            select pt).SingleOrDefault();
            if (oldMovilizacion != null)
            {
                oldMovilizacion.conteo = valor;
                db.SaveChanges();
            }

            return new JavaScriptSerializer().Serialize("");
        }

        [Authorize(Roles = "movilizacion")]
        public string AlertaMovilizacion(MovilizacionContact contacto, int valor, string mensaje)
        {
            var db = new edayRoomEntities();
            var centro = db.Centroes.Single(c => c.id == contacto.IdCentro);
            var user = db.users.Single(u => u.username == User.Identity.Name);
            #region Registro la alerta

            var alerta = new MovilizacionAlerta
                             {
                                 activa = true,
                                 fecha = DateTime.Now,
                                 id_centro = contacto.IdCentro,
                                 id_movilizador = contacto.IdTestigo,
                                 id_alerta = valor,
                                 id_usuario = user.id
                             };
            db.MovilizacionAlertas.AddObject(alerta);


            var messages = new List<MovilizacionAlertaMessage>();
            if (!string.IsNullOrWhiteSpace(mensaje))
            {
                var alertMessage = new MovilizacionAlertaMessage { fecha = DateTime.Now, mensaje = mensaje, id_usuario = user.id };
                alerta.MovilizacionAlertaMessages.Add(alertMessage);
                messages.Add(alertMessage);
            }

            #endregion

            #region  Retraso de Timeline

            Alerta objetoAlerta = db.Alertas.Single(a => a.id == valor);
            if (objetoAlerta.blocking){
                centro.alertBlocked = true;
                centro.blockingAlertId = objetoAlerta.id;
            }
            if (objetoAlerta.regresivo)
            {

                //MovilizacionTimeline oldTimeline =
                //    (from pt in db.MovilizacionTimelines where 
                //         pt.id == contacto.MovilizacionTimelineId select pt).
                //        Single();

                //oldTimeline.activa = false;

                //Cancelo todos los timelines activos y resolvemos este peo
                var oldTimelines =
                    (from pt in db.MovilizacionTimelines
                     where
                         pt.activa && pt.id_centro == contacto.IdCentro
                         //pt.id == contacto.MovilizacionTimelineId
                     select pt);
                foreach (var oldTimeline in oldTimelines)
                {
                    oldTimeline.activa = false;
                }

                DateTime newDate = DateTime.Now.AddMinutes(objetoAlerta.tiempo);
                var newTimeline = new MovilizacionTimeline { id_centro = contacto.IdCentro, fecha = newDate, activa = true };
                db.MovilizacionTimelines.AddObject(newTimeline);
            }

            #endregion

            db.SaveChanges();


            // VERIFICAR SI HAY QUE HACER TRIGGER DE ALGUNA ALERTA
            IQueryable<MovilizacionAlerta> existingAlerts = from a in db.MovilizacionAlertas
                                                            where a.id_centro == contacto.IdCentro &&
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

                    var alertaAuto = new MovilizacionAlerta
                                         {
                                             activa = true,
                                             fecha = DateTime.Now,
                                             id_centro = contacto.IdCentro,
                                             id_movilizador = contacto.IdTestigo,
                                             id_alerta = newAlerta.id,
                                             id_usuario = user.id
                                         };
                    db.MovilizacionAlertas.AddObject(alertaAuto);

                    if (newAlerta.blocking){
                        centro.alertBlocked = true;
                        centro.blockingAlertId = newAlerta.id;
                    }

                    var alertMessage = new MovilizacionAlertaMessage { fecha = DateTime.Now, mensaje = "Alerta generada por sistema", id_usuario=user.id };
                    alertaAuto.MovilizacionAlertaMessages.Add(alertMessage);
                }
            }
            db.SaveChanges();
            return new JavaScriptSerializer().Serialize("");
        }

        [Authorize(Roles = "movilizacion")]
        public string ActualizarAlertaMovilizacion(MovilizacionContact contacto, string mensaje)
        {
            var db = new edayRoomEntities();
            var user = db.users.Single(u => u.username == User.Identity.Name); 
            MovilizacionAlerta alerta = (from a in db.MovilizacionAlertas
                                         where a.id == contacto.BlockingAlert.Id
                                         select a).Single();

            if (!string.IsNullOrWhiteSpace(mensaje))
            {
                var alertMessage = new MovilizacionAlertaMessage { fecha = DateTime.Now, mensaje = mensaje, id_usuario = user.id};
                alerta.MovilizacionAlertaMessages.Add(alertMessage);
            }

            db.SaveChanges();
            IQueryable<MovilizacionContactAlertMessage> mensajesAlerta = (from a in alerta.MovilizacionAlertaMessages
                                                                          orderby a.fecha descending
                                                                          select new MovilizacionContactAlertMessage
                                                                                     {
                                                                                         Id = a.id,
                                                                                         Fecha = a.fecha,
                                                                                         Message = a.mensaje
                                                                                     }).AsQueryable();
            return new JavaScriptSerializer().Serialize("");
        }

        [Authorize(Roles = "movilizacion")]
        public string CancelarAlertaMovilizacion(MovilizacionContact contacto, string mensaje)
        {
            var db = new edayRoomEntities();
            var centro = db.Centroes.Single(c=> c.id == contacto.IdCentro);
            var alerta = (from a in db.MovilizacionAlertas
                                         where a.id == contacto.BlockingAlert.Id
                                         select a).Single();
            centro.alertBlocked = false;
            centro.blockingAlertId = null;
            alerta.activa = false;
            db.SaveChanges();

            return new JavaScriptSerializer().Serialize("");
        }

        #endregion

        #region admin section

        [Authorize(Roles = "movilizacion-lider")]
        public ActionResult Admin()
        {
            return View("Admin");
        }

        [Authorize(Roles = "movilizacion")]
        public ActionResult AdminCentro(int idCentro)
        {
            var db = new edayRoomEntities();
            var centro = (from c in db.Centroes
                          where c.id == idCentro
                          select c).Single();
            var movilizacion = from p in centro.Movilizacions
                               orderby p.fecha descending
                               select p;

            MovilizacionTimeline timelineMovilizacion = (from tp in db.MovilizacionTimelines
                                                           where tp.activa &&
                                                                 tp.id_centro== idCentro
                                                           select tp).Single();

            Movilizacion lastMovilizacion= movilizacion.FirstOrDefault();

            Debug.Assert(lastMovilizacion != null, "lastMovilizacion != null");
            var contact = new MovilizacionContact
            {
                Centro = centro.Nombre,
                IdCentro= centro.id,
                IdTestigo = centro.Movilizadors.First(t => t.activo).id,
                NombreTestigo = centro.Movilizadors.First(t => t.activo).nombre,
                Numero = centro.Movilizadors.First(t => t.activo).numero,
                Votantes = centro.votantes??0,
                LastUpdate = lastMovilizacion.fecha,
                LastValue = lastMovilizacion.conteo,
                LastMovilizacionId = lastMovilizacion.id,
                MovilizacionTimelineId = timelineMovilizacion.id
            };
            ViewData["centro"] = centro;
            ViewData["movilizacion"] = movilizacion.ToList();
            ViewData["contact"] = contact;
            return View("AdminCentro");
        }

        [Authorize(Roles = "movilizacion")]
        public ActionResult GetChartData(int? idCentro = null)
        {
            var db = new edayRoomEntities();
            var interval = idCentro==null?15:1;
            var user = db.users.Single(u => u.username == User.Identity.Name);
            var data = db.GetMovilizacion(idCentro, interval, user.id).ToList();
            //var participaciones = db.Participacions.GroupBy(p => p.fecha).Select(p => new { fecha = p.Key, conteo = p.Sum(c => c.conteo), cola = p.Sum(c => c.cola) });
            return Json(new
                            {
                                movilizacion =
                            data.Select(
                                p =>
                                new
                                    {
                                        value = p.value,
                                        year = ((DateTime)p.fecha).Year,
                                        month = ((DateTime)p.fecha).Month - 1,
                                        day = ((DateTime)p.fecha).Day,
                                        hour = ((DateTime)p.fecha).Hour,
                                        min = ((DateTime)p.fecha).Minute
                                    }),
                            }, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "movilizacion-lider")]
        public string GetCentros(int iDisplayStart, int iDisplayLength, int sEcho, int iSortCol_0, string sSortDir_0,
                                 string sSearch)
        {
            var dth = new DataTableHelper();
            var db = new edayRoomEntities();
            user user = db.users.Single(u => u.username == User.Identity.Name);
            IQueryable<Centro> centros = from c in db.Centroes
                                         where
                                             (user.admin || (user.leader && user.grupo == c.grupoMovilizacion) ||

                                              c.AsignacionMovilizacions.Any(ap => ap.id_user == user.id))
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
                            centros = centros.OrderBy(c=>c.Movilizacions.Max(p => p.conteo));
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
                            centros = centros.OrderByDescending(c=>c.Movilizacions.Max(p => p.conteo));
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
                                                        c.Movilizacions.Max(p => p.conteo) + ""
                                                    }))
            {
                dth.aaData.Add(l);
            }
            return new JavaScriptSerializer().Serialize(dth);
        }

        [Authorize(Roles = "movilizacion-lider")]
        public ActionResult Usuarios()
        {
            var db = new edayRoomEntities();
            user user = db.users.Single(u => u.username == User.Identity.Name);
            var group = user.grupo;
            var users = db.users.Where(u => u.grupo == group && u.movilizacion);
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
                var usersInGroup = db.users.Where(u => u.movilizacion && u.grupo == user.grupo && u.id != user.id && !u.paused).ToList();
                var groupCount = usersInGroup.Count;
                var assignedMovilizacion = db.AsignacionMovilizacions.Where(ap => ap.id_user == user.id).ToList();
                var roundRobin = 0;
                foreach (var ap in assignedMovilizacion)
                {
                    db.AsignacionMovilizacions.AddObject(new AsignacionMovilizacion
                    {
                        id_centro = ap.id_centro,
                        id_original_user = ap.id_original_user ?? user.id,
                        isReplacement = true,
                        id_user = usersInGroup[roundRobin % groupCount].id
                    });
                    roundRobin++;
                }
            }
            else
            {

                var assignedMovilizacion = db.AsignacionMovilizacions.Where(ap => ap.id_original_user == user.id && ap.isReplacement).ToList();
                foreach (var ap in assignedMovilizacion)
                {
                    db.AsignacionMovilizacions.DeleteObject(ap);
                }
            }


            db.SaveChanges();
            return user.paused.ToString(CultureInfo.InvariantCulture);
        }
    }
}