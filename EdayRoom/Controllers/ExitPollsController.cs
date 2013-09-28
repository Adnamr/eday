using System;
using System.Collections.Generic;
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
    public class ExitPollsController : Controller
    {
        //
        // GET: /ExitPolls/
        [Authorize(Roles = "exitpolls")]
        public ActionResult Index()
        {
            return View("ExitPolls");
        }

        [Authorize(Roles = "exitpolls")]
        public string GetStatistics()
        {
            var stats = new ExitPollStats();
            return new JavaScriptSerializer().Serialize(stats);
        }

        [Authorize(Roles = "exitpolls")]
        public string GetContacts()
        {
            var db = new edayRoomEntities();
            var user = db.users.Single(u => u.username == User.Identity.Name);

            List<ExitPollContact> contactos = (from c in db.Centroes
                                               from t in db.TestigoExitPolls
                                               from pt in db.ExitPollTimelines
                                               from asp in db.AsignacionExitPolls
                                               join a in db.ExitPollAlertas.Include("Alerta") on
                                               new { id_centro = c.id, blocking = true, activa = true } equals
                                               new { a.id_centro, a.Alerta.blocking, a.activa } into alertas2
                                               from pa in alertas2.DefaultIfEmpty()

                                               where
                                                   c.id == pt.id_centro &&
                                                   pt.activa &&
                                                   pt.id_testigoExitPoll == t.id &&
                                                   asp.id_user == user.id && asp.id_centro == c.id &&
                                                   t.activo
                                               orderby pt.fecha ascending
                                               select new ExitPollContact
                                                          {
                                                              Centro = c.Nombre,
                                                              CentroId = c.id,
                                                              Estado = c.unidadGeografica1,
                                                              Municipio = c.unidadGeografica2,
                                                              Parroquia = c.unidadGeografica3,
                                                              QuickCountActive = c.quickCountActive ?? false,

                                                              IdTestigo = t.id,
                                                              LastUpdate = pt.fecha,
                                                              ExitPollIdParent = pt.id_parent,
                                                              NombreTestigo = t.nombre,
                                                              Numero = t.numero,
                                                              NextUpdate = pt.fecha,
                                                              Votantes = c.votantes ?? 0,
                                                              ExitPollTimelineId = pt.id,

                                                              LinqAlertaObj = pa == null ? null : pa.Alerta,
                                                              LinqExitrPollAlertaObj = pa,
                                                              LinqExitPollAlertaMessagesObj = pa.ExitPollAlertaMessages,
                                                              IsAlertBlocked = pa != null,

                                                              Valores = pt.ExitPolls.Select(epp => new CandidatoValue
                                                                                                       {
                                                                                                           CandidatoId =
                                                                                                               epp.
                                                                                                               id_candidato,
                                                                                                           Fecha =
                                                                                                               epp.fecha,
                                                                                                           Nombre =
                                                                                                               epp.
                                                                                                               Candidato
                                                                                                               .nombre,
                                                                                                           TimelineId =
                                                                                                               pt.id,
                                                                                                           Valor =
                                                                                                               epp.valor
                                                                                                       }).AsQueryable()
                                                          }).ToList();

            List<ExitPollContact> contactoTimelineCurrent =
                (from c in contactos where !c.IsAlertBlocked select c).Take(3).ToList();
            List<ExitPollContact> contactoTimelineAlertas =
                (from c in contactos where c.IsAlertBlocked select c).OrderBy(
                    c => c.BlockingAlert.Fecha).ToList();


            return new JavaScriptSerializer().Serialize(new
                                                            {
                                                                proximos = contactoTimelineCurrent,
                                                                alertas = contactoTimelineAlertas
                                                            });
        }

        #region updates regulares

        [Authorize(Roles = "exitpolls")]
        public string UpdateExitPoll(ExitPollContact contacto, CandidatoValueIncoming[] valores)
        {
            var db = new edayRoomEntities();
            var user = db.users.Single(u => u.username == User.Identity.Name);

            ExitPollTimeline oldTimeline = (from pt in db.ExitPollTimelines
                                            where pt.id == contacto.ExitPollTimelineId
                                            select pt).Single();
            oldTimeline.activa = false;
            db.SaveChanges();

            DateTime newDate = contacto.QuickCountActive ? DateTime.Now.AddMinutes(45) : DateTime.Now.AddMinutes(60);
            var newTimeline = new ExitPollTimeline
                                  {
                                      id_centro = contacto.CentroId,
                                      fecha = newDate,
                                      id_testigoExitPoll = contacto.IdTestigo,
                                      activa = true,
                                      id_parent = oldTimeline.id

                                  };
            db.ExitPollTimelines.AddObject(newTimeline);
            db.SaveChanges();

            foreach (CandidatoValueIncoming v in valores)
            {
                var ep = new ExitPoll
                             {
                                 id_candidato = int.Parse(v.name.Replace("candidato-", "")),
                                 id_centro = contacto.CentroId,
                                 id_testigoExitPoll = contacto.IdTestigo,
                                 id_timeline = newTimeline.id,
                                 valor = v.value,
                                 fecha = DateTime.Now,
                                 id_user = user.id
                             };
                db.ExitPolls.AddObject(ep);
            }

            IQueryable<ExitPollAlerta> alertas = from a in db.ExitPollAlertas
                                                 where a.activa && !a.Alerta.blocking
                                                       && a.id_centro == contacto.CentroId
                                                 select a;
            foreach (ExitPollAlerta a in alertas)
            {
                a.activa = false;
            }

            db.SaveChanges();
            //var newContacto = ExitPollsContact.GetExitPollsContact(contacto.IdTestigo);


            return new JavaScriptSerializer().Serialize(contacto);
        }

        [Authorize(Roles = "exitpolls")]
        public string UpdateSingleExitPoll(int exitpollId, CandidatoValueIncoming[] valores)
        {
            var db = new edayRoomEntities();

            ExitPollTimeline oldTimeline = (from pt in db.ExitPollTimelines
                                            where pt.id == exitpollId
                                            select pt).Single();

            db.SaveChanges();


            foreach (CandidatoValueIncoming v in valores)
            {
                int idCandidato = int.Parse(v.name.Replace("candidato-", ""));
                int valor = v.value;
                ExitPoll
                    ep = oldTimeline.ExitPolls.Single(e => e.id_candidato == idCandidato);
                ep.valor = valor;
            }

            db.SaveChanges();


            return new JavaScriptSerializer().Serialize("");
        }

        [Authorize(Roles = "exitpolls")]
        public string GetCandidatos(int centroId)
        {
            var db = new edayRoomEntities();
            IQueryable<CandidatoValue> candidato =
                from c in db.Candidatoes
                let ep =
                    c.ExitPolls.Where(ep => ep.id_centro == centroId).OrderByDescending(ep => ep.fecha).FirstOrDefault()
                select new CandidatoValue
                           {
                               CandidatoId = c.id,
                               Fecha = ep == null ? DateTime.MinValue : ep.fecha,
                               Nombre = c.nombre,
                               TimelineId = ep == null ? 0 : ep.id_timeline,
                               Valor = ep == null ? 0 : ep.valor
                           };
            return new JavaScriptSerializer().Serialize(candidato);
        }

        [Authorize(Roles = "exitpolls")]
        public string AlertaExitPolls(ExitPollContact contacto, int valor, string mensaje)
        {
            var db = new edayRoomEntities();
            var user = db.users.Single(u => u.username == User.Identity.Name);

            #region Registro la alerta

            var alerta = new ExitPollAlerta
                             {
                                 activa = true,
                                 fecha = DateTime.Now,
                                 id_centro = contacto.CentroId,
                                 id_testigoExitPoll = contacto.IdTestigo,
                                 id_alerta = valor,
                                 id_usuario = user.id
                             };
            db.ExitPollAlertas.AddObject(alerta);


            var messages = new List<ExitPollAlertaMessage>();
            if (!string.IsNullOrWhiteSpace(mensaje))
            {
                var alertMessage = new ExitPollAlertaMessage { fecha = DateTime.Now, mensaje = mensaje, id_usuario = user.id };
                alerta.ExitPollAlertaMessages.Add(alertMessage);
                messages.Add(alertMessage);
            }

            #endregion

            #region  Retraso de Timeline

            Alerta objetoAlerta = db.Alertas.Single(a => a.id == valor);

            if (objetoAlerta.regresivo)
            {
                ExitPollTimeline oldTimeline =
                    (from pt in db.ExitPollTimelines where pt.id == contacto.ExitPollTimelineId select pt).
                        Single();
                oldTimeline.activa = false;
                DateTime newDate = DateTime.Now.AddMinutes(objetoAlerta.tiempo);
                var newTimeline = new ExitPollTimeline
                                      {
                                          id_centro = contacto.CentroId,
                                          id_testigoExitPoll = contacto.IdTestigo,
                                          id_parent = oldTimeline.id,
                                          fecha = newDate,
                                          activa = true
                                      };
                db.ExitPollTimelines.AddObject(newTimeline);
            }

            #endregion

            db.SaveChanges();


            // VERIFICAR SI HAY QUE HACER TRIGGER DE ALGUNA ALERTA
            IQueryable<ExitPollAlerta> existingAlerts = from a in db.ExitPollAlertas
                                                        where a.id_centro == contacto.CentroId &&
                                                              a.id_alerta == valor && a.activa
                                                        select a;
            int alertCount = existingAlerts.Count();
            int maxRepeats = existingAlerts.First().Alerta.maxRepeats ?? 0;
            if (maxRepeats != 0)
            {
                if (alertCount == maxRepeats)
                {
                    //LLEGUE AL LIMITE, hago el trigger de la alerta
                    Alerta newAlerta = existingAlerts.First().Alerta.AlertaAsociada;
                    var alertaAuto = new ExitPollAlerta
                                         {
                                             activa = true,
                                             fecha = DateTime.Now,
                                             id_centro = contacto.CentroId,
                                             id_testigoExitPoll = contacto.IdTestigo,
                                             id_alerta = newAlerta.id,
                                             id_usuario = user.id
                                         };
                    db.ExitPollAlertas.AddObject(alertaAuto);

                    var alertMessage = new ExitPollAlertaMessage { fecha = DateTime.Now, mensaje = "Alerta generada por sistema", id_usuario = user.id};
                    alertaAuto.ExitPollAlertaMessages.Add(alertMessage);
                }
            }
            db.SaveChanges();
            //var newContacto = ExitPollContact.GetExitPollContact(contacto.IdTestigo);
            return new JavaScriptSerializer().Serialize("");
        }

        [Authorize(Roles = "exitpolls")]
        public string ActualizarAlertaExitPolls(ExitPollContact contacto, string mensaje)
        {
            var db = new edayRoomEntities();
            var user = db.users.Single(u => u.username == User.Identity.Name);
            ExitPollAlerta alerta = (from a in db.ExitPollAlertas
                                     where a.id == contacto.BlockingAlert.Id
                                     select a).Single();

            if (!string.IsNullOrWhiteSpace(mensaje))
            {
                var alertMessage = new ExitPollAlertaMessage { fecha = DateTime.Now, mensaje = mensaje, id_usuario = user.id};
                alerta.ExitPollAlertaMessages.Add(alertMessage);
            }

            db.SaveChanges();
            IQueryable<ExitPollContactAlertMessage> mensajesAlerta = (from a in alerta.ExitPollAlertaMessages
                                                                      orderby a.fecha descending
                                                                      select new ExitPollContactAlertMessage
                                                                                 {
                                                                                     Id = a.id,
                                                                                     Fecha = a.fecha,
                                                                                     Message = a.mensaje
                                                                                 }).AsQueryable();

            return new JavaScriptSerializer().Serialize("");
        }

        [Authorize(Roles = "exitpolls")]
        public string CancelarAlertaExitPolls(ExitPollContact contacto, string mensaje)
        {
            var db = new edayRoomEntities();
            ExitPollAlerta alerta = (from a in db.ExitPollAlertas
                                     where a.id == contacto.BlockingAlert.Id
                                     select a).Single();

            alerta.activa = false;
            db.SaveChanges();
            return new JavaScriptSerializer().Serialize("");
        }

        #endregion

        #region Administracion

        [Authorize(Roles = "exitpolls-lider")]
        public ActionResult Admin()
        {
            return View("Admin");
        }

        [Authorize(Roles = "exitpolls-lider")]
        public ActionResult AdminCentro(int idCentro)
        {
            var db = new edayRoomEntities();
            Centro centro = (from c in db.Centroes
                             where c.id == idCentro
                             select c).Single();
            IOrderedQueryable<ExitPollTimeline> timelines =
                db.ExitPollTimelines.Where(ep => ep.id_centro == idCentro && ep.ExitPolls.Any()).OrderByDescending(
                    ep => ep.fecha);
            ViewData["timelines"] = timelines.ToList();
            ViewData["candidatos"] = db.Candidatoes.OrderBy(c => c.nombre).ToList();
            ViewData["centro"] = centro;

            return View("AdminCentro");
        }

        [Authorize(Roles = "exitpolls-lider")]
        public ActionResult GetChartData(int? idCentro = null)
        {
            var db = new edayRoomEntities();
            IQueryable<ExitPoll> exitpolls = db.ExitPolls.Where(p => p.id_testigoExitPoll != null);
            if (idCentro != null)
            {
                exitpolls = exitpolls.Where(p => p.id_centro == (int)idCentro);
            }
            DateTime minDate = exitpolls.Min(p => p.fecha) ;
            minDate = minDate.AddMinutes(minDate.Minute * -1);
            DateTime currentDate = minDate;
            DateTime maxDate = exitpolls.Max(p => p.fecha);


            var dateArray = new List<DateTime> { minDate };
            while (currentDate <= maxDate)
            {
                currentDate = currentDate.AddMinutes(60);
                dateArray.Add(currentDate);
            }

            var candidatosCount = new List<CandidatoCountChart>();
            List<Candidato> candidatos = db.Candidatoes.OrderBy(c => c.nombre).ToList();
            foreach (DateTime date in dateArray)
            {
                IQueryable<ExitPoll> validExitPolls = exitpolls.Where(p => p.fecha <= date);
                if (validExitPolls.Any())
                {
                    foreach (Candidato c in candidatos)
                    {
                        CandidatoCountChart obj = candidatosCount.SingleOrDefault(cc => cc.Nombre == c.nombre);
                        if (obj == null)
                        {
                            obj = new CandidatoCountChart
                                      {
                                          Nombre = c.nombre,
                                          Porcentajes = new List<DateValuePair>()
                                      };
                            candidatosCount.Add(obj);
                        }
                        IQueryable<ExitPoll> test = validExitPolls.Where(v => v.id_candidato == c.id);
                        obj.Porcentajes.Add(
                            new DateValuePair
                                {
                                    year = date.Year,
                                    month = date.Month - 1,
                                    day = date.Day,
                                    hour = date.Hour,
                                    min = date.Minute,
                                    value = test.GroupBy(v => v.id_centro).Sum(g => g.Max(m => m.valor))
                                });
                    }
                }
            }


            return Json(new
                            {
                                candidatos = candidatosCount,
                                fechas = dateArray
                            }, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "exitpolls-lider")]
        public string GetCentros(int iDisplayStart, int iDisplayLength, int sEcho, int iSortCol_0, string sSortDir_0,
                                 string sSearch)
        {
            var dth = new DataTableHelper();
            var db = new edayRoomEntities();
            user user = db.users.Single(u => u.username == User.Identity.Name);
            IQueryable<Centro> centros = from c in db.Centroes
                                         where
                                             c.quickCountActive ?? false &&
                                             (user.admin ||
                                              c.AsignacionExitPolls.Any(ap => ap.id_user == user.id))
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
                            centros = centros.OrderBy(c => c.Mesas1.Sum(m => m.Totalizacions.Sum(t => t.valor)));
                            break;
                        case 6:
                            centros = centros.OrderBy(c => c.Mesas1.Any(m => m.Totalizacions.Any()));
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
                            centros =
                                centros.OrderByDescending(c => c.Mesas1.Sum(m => m.Totalizacions.Sum(t => t.valor)));
                            break;
                        case 6:
                            centros = centros.OrderByDescending(c => c.Mesas1.Any(m => m.Totalizacions.Any()));
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
                                                        c.Mesas1.Any(m => m.Totalizacions.Any())
                                                            ? c.Mesas1.Sum(m => m.Totalizacions.Any()
                                                                                    ? m.Totalizacions.Sum(
                                                                                        p => p.valor)
                                                                                    : 0) + ""
                                                            : "<span>-</span>",
                                                        c.Mesas1.Any(m => m.Totalizacions.Any())
                                                            ? "<div class='mws-ic-16 ic-accept tableIcon16'></div>"
                                                            : "<div class='mws-ic-16 ic-cross tableIcon16'></div>"
                                                    }))
            {
                dth.aaData.Add(l);
            }
            return new JavaScriptSerializer().Serialize(dth);
        }

        [Authorize(Roles = "exitpolls-lider")]
        public ActionResult Usuarios()
        {
            var db = new edayRoomEntities();
            var user = db.users.Single(u => u.username == User.Identity.Name);
            var group = user.grupo;
            var users = db.users.Where(u => u.grupo == group && u.exitpolls);
            ViewData["users"] = users.ToList();
            ViewData["candidatosCount"] = db.Candidatoes.Count();
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
                var usersInGroup =
                    db.users.Where(u => u.exitpolls && u.grupo == user.grupo && u.id != user.id && !u.paused).ToList
                        ();
                var groupCount = usersInGroup.Count;
                var assignedExitPoll = db.AsignacionExitPolls.Where(ap => ap.id_user == user.id).ToList();
                var roundRobin = 0;
                foreach (var ap in assignedExitPoll)
                {
                    db.AsignacionExitPolls.AddObject(new AsignacionExitPoll
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

                var assignedExitPoll =
                    db.AsignacionExitPolls.Where(ap => ap.id_original_user == user.id && ap.isReplacement).ToList();
                foreach (var ap in assignedExitPoll)
                {
                    db.AsignacionExitPolls.DeleteObject(ap);
                }
            }


            db.SaveChanges();
            return user.paused.ToString(CultureInfo.InvariantCulture);
        }
    }
}