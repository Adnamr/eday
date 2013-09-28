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
    public class TotalizacionController : Controller
    {
        // GET: /Totalizacions/
        [Authorize(Roles = "totalizacion")]
        public ActionResult Index()
        {
            return View("Totalizacion");
        }

        [Authorize(Roles = "totalizacion")]
        public ActionResult Totalizacion()
        {
            return View("Totalizacion");
        }

        [Authorize(Roles = "totalizacion")]
        public string GetContacts()
        {
            var db = new edayRoomEntities();
            var user = db.users.Single(u => u.username == User.Identity.Name);
            var contactos = (from c in db.Centroes
                                                   from m in db.Mesas
                                                   from t in db.Testigoes
                                                   from pt in db.TotalizacionTimelines
                             from asp in db.AsignacionTotalizacions
                             join a in db.TotalizacionAlertas.Include("Alerta") on
                             new { id_mesa = m.id, blocking = true, activa = true } equals
                             new { a.id_mesa, a.Alerta.blocking, a.activa } into alertas2
                             from pa in alertas2.DefaultIfEmpty()

                                                   where
                                                       c.id == m.id_centro &&
                                                       m.id == pt.id_mesa &&
                                                       asp.id_user == user.id && 
                                                       asp.id_mesa == m.id &&
                                                       pt.activa &&
                                                       t.id_mesa == m.id && 
                                                       m.cerrada &&
                                                       t.activo && !m.Totalizacions.Any()
                                                   orderby pt.fecha ascending
                                                   select new TotalizacionContact
                                                              {
                                                                  Centro = c.Nombre,
                                                                  Estado = c.unidadGeografica1,
                                                                  Municipio = c.unidadGeografica2,
                                                                  Parroquia = c.unidadGeografica3,
                                                                  QuickCountActive = c.quickCountActive ?? false,
                                                                  CentroUniqueId = c.unique_id,
                                                                  CentroId = c.id,
                                                                  MesaId = m.id,
                                                                  Mesa = m.numero,
                                                                  IdTestigo = t.id,
                                                                  LastUpdate = pt.fecha,
                                                                  NombreTestigo = t.nombre,
                                                                  Numero = t.numero,
                                                                  NextUpdate = pt.fecha,
                                                                  Votantes = c.votantes ?? 0,
                                                                  TotalizacionTimelineId = pt.id,
                                                                  LinqAlertaObj = pa == null ? null : pa.Alerta,
                                                                  LinqTotalizacionAlertaObj = pa,
                                                                  LinqTotalizacionAlertaMessagesObj = pa.TotalizacionAlertaMessages,
                                                                  IsAlertBlocked = pa != null,
                                                                  Valores =
                                                                      pt.Totalizacions.Select(epp => new CandidatoValue
                                                                                                         {
                                                                                                             CandidatoId = epp.id_candidato,
                                                                                                             Fecha =
                                                                                                                 epp.TotalizacionTimelines.FirstOrDefault() ==null
                                                                                                                     ? DateTime.MinValue : epp.TotalizacionTimelines.FirstOrDefault().fecha,
                                                                                                             Nombre =epp.RelacionCandidatoPartidoCoalicion.Candidato.nombre,
                                                                                                             TimelineId = pt.id,
                                                                                                             Valor = epp.valor
                                                                                                         }).AsQueryable()
                                                              }).ToList();

            List<TotalizacionContact> contactoTimelineCurrent = (from c in contactos where !c.IsAlertBlocked select c).Take(150).ToList();
            List<TotalizacionContact> contactoTimelineAlertas = (from c in contactos where c.IsAlertBlocked select c).OrderBy(c => c.BlockingAlert.Fecha).ToList();


            return new JavaScriptSerializer().Serialize(new
                                                            {
                                                                proximos = contactoTimelineCurrent,
                                                                alertas = contactoTimelineAlertas
                                                            });
        }

        //#region updates regulares
        [Authorize(Roles = "totalizacion")]
        public string UpdateTotalizacion(TotalizacionContact contacto, CandidatoValueIncoming[] valores)
        {
            var db = new edayRoomEntities();

            TotalizacionTimeline oldTimeline = (from pt in db.TotalizacionTimelines
                                                where pt.id == contacto.TotalizacionTimelineId
                                                select pt).Single();
            oldTimeline.activa = false;
            db.SaveChanges();

            foreach (CandidatoValueIncoming v in valores)
            {
                var ep = new Totalizacion
                             {
                                 id_candidato = int.Parse(v.name.Replace("candidato-", "")),
                                 id_mesa = contacto.MesaId,
                                 valor = v.value
                             };
                db.Totalizacions.AddObject(ep);
                ep.TotalizacionTimelines.Add(oldTimeline);
            }

            IQueryable<TotalizacionAlerta> alertas = from a in db.TotalizacionAlertas
                                                     where a.activa && !a.Alerta.blocking
                                                           && a.id_mesa == contacto.MesaId
                                                     select a;
            foreach (TotalizacionAlerta a in alertas)
            {
                a.activa = false;
            }

            var centro = db.Centroes.Single(c => c.id == contacto.CentroId);
            centro.totalizado = true;

            db.SaveChanges();
            //MatrizDeSustitucion.UpdateMatriz(contacto.CentroId);

            return new JavaScriptSerializer().Serialize("");
        }

        [Authorize(Roles = "totalizacion")]
        public string UpdateSingleTotalizacion(int totalizacionId, int value)
        {
            var db = new edayRoomEntities();

            Totalizacion totalizacion = (from t in db.Totalizacions
                                         where t.ID == totalizacionId
                                         select t).Single();

            totalizacion.valor = value;
            db.SaveChanges();

            MatrizDeSustitucion.UpdateMatriz(totalizacion.Mesa.Centro.id);

            return new JavaScriptSerializer().Serialize("");
        }

        [Authorize(Roles = "totalizacion")]
        public string GetCandidatos(int mesaId)
        {
            var db = new edayRoomEntities();
            var candidatos =
                db.RelacionCandidatoPartidoCoalicions.Select(c => new
                                                                      {
                                                                          c.id,
                                                                          c.Candidato.nombre,
                                                                          partido = c.Partido.nombre,
                                                                          coalicion = c.Coalicion.nombre,
                                                                          orden = c.orden,
                                                                          value = 0
                                                                      //c.Totalizacions.OrderByDescending(t => t.ID).
                                                                      //    FirstOrDefault(m => m.id_mesa == mesaId) ==
                                                                      //null
                                                                      //    ? 0
                                                                      //    : c.Totalizacions.OrderByDescending(t => t.ID)
                                                                      //          .FirstOrDefault(m => m.id_mesa == mesaId)
                                                                      //          .valor
                                                                      }).OrderBy(c => c.orden);
            return new JavaScriptSerializer().Serialize(candidatos);
        }

        [Authorize(Roles = "totalizacion")]
        public string AlertaTotalizacion(TotalizacionContact contacto, int valor, string mensaje)
        {
            var db = new edayRoomEntities();
            var user = db.users.Single(u => u.username == User.Identity.Name);

            #region Registro la alerta

            var alerta = new TotalizacionAlerta
                             {
                                 activa = true,
                                 fecha = DateTime.Now,
                                 id_mesa = contacto.MesaId,
                                 id_testigoTotalizacion = contacto.IdTestigo,
                                 id_alerta = valor,
                                 id_usuario = user.id
                             };
            db.TotalizacionAlertas.AddObject(alerta);


            var messages = new List<TotalizacionAlertaMessage>();
            if (!string.IsNullOrWhiteSpace(mensaje))
            {
                var alertMessage = new TotalizacionAlertaMessage { fecha = DateTime.Now, mensaje = mensaje , id_usuario = user.id};
                alerta.TotalizacionAlertaMessages.Add(alertMessage);
                messages.Add(alertMessage);
            }

            #endregion

            #region  Retraso de Timeline

            Alerta objetoAlerta = db.Alertas.Single(a => a.id == valor);

            if (objetoAlerta.regresivo)
            {
                var oldTimelines =
                  (from pt in db.TotalizacionTimelines
                   where
                       pt.activa && pt.id_mesa == contacto.MesaId
                   //pt.id == contacto.MovilizacionTimelineId
                   select pt);
                foreach (var oldTimeline in oldTimelines)
                {
                    oldTimeline.activa = false;
                }

                //TotalizacionTimeline oldTimeline =
                //    (from pt in db.TotalizacionTimelines where pt.id == contacto.TotalizacionTimelineId select pt).
                //        Single();
                //oldTimeline.activa = false;
                DateTime newDate = DateTime.Now.AddMinutes(objetoAlerta.tiempo);
                var newTimeline = new TotalizacionTimeline
                                      {
                                          id_mesa = contacto.MesaId,
                                          //id_parent = oldTimeline.id,
                                          fecha = newDate,
                                          activa = true
                                      };
                db.TotalizacionTimelines.AddObject(newTimeline);
            }

            #endregion

            db.SaveChanges();


            // VERIFICAR SI HAY QUE HACER TRIGGER DE ALGUNA ALERTA
            var existingAlerts = from a in db.TotalizacionAlertas
                                                            where a.id_mesa == contacto.MesaId &&
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
                    var alertaAuto = new TotalizacionAlerta
                                         {
                                             activa = true,
                                             fecha = DateTime.Now,
                                             id_mesa = contacto.MesaId,
                                             id_testigoTotalizacion = contacto.IdTestigo,
                                             id_alerta = newAlerta.id,
                                             id_usuario = user.id
                                         };
                    db.TotalizacionAlertas.AddObject(alertaAuto);

                    var alertMessage = new TotalizacionAlertaMessage { fecha = DateTime.Now, mensaje = "Alerta generada por sistema", id_usuario = user.id };
                    alertaAuto.TotalizacionAlertaMessages.Add(alertMessage);
                }
            }
            db.SaveChanges();

            //var newContacto = TotalizacionContact.GetTotalizacionContact(contacto.IdTestigo);
            return new JavaScriptSerializer().Serialize("");
        }

        [Authorize(Roles = "totalizacion")]
        public string ActualizarAlertaTotalizacion(TotalizacionContact contacto, string mensaje)
        {
            var db = new edayRoomEntities();
            var user = db.users.Single(u => u.username == User.Identity.Name);
            TotalizacionAlerta alerta = (from a in db.TotalizacionAlertas
                                         where a.id == contacto.BlockingAlert.Id
                                         select a).Single();

            if (!string.IsNullOrWhiteSpace(mensaje))
            {
                var alertMessage = new TotalizacionAlertaMessage { fecha = DateTime.Now, mensaje = mensaje, id_usuario=user.id };
                alerta.TotalizacionAlertaMessages.Add(alertMessage);
            }

            db.SaveChanges();
            IQueryable<TotalizacionContactAlertMessage> mensajesAlerta = (from a in alerta.TotalizacionAlertaMessages
                                                                          orderby a.fecha descending
                                                                          select new TotalizacionContactAlertMessage
                                                                                     {
                                                                                         Id = a.id,
                                                                                         Fecha = a.fecha,
                                                                                         Message = a.mensaje
                                                                                     }).AsQueryable();

            return new JavaScriptSerializer().Serialize("");
        }

        [Authorize(Roles = "totalizacion")]
        public string CancelarAlertaTotalizacion(TotalizacionContact contacto, string mensaje)
        {
            var db = new edayRoomEntities();
            TotalizacionAlerta alerta = (from a in db.TotalizacionAlertas
                                         where a.id == contacto.BlockingAlert.Id
                                         select a).Single();

            alerta.activa = false;
            db.SaveChanges();
            return new JavaScriptSerializer().Serialize("");
        }

        #region Administration

        [Authorize(Roles = "totalizacion-lider")]
        public ActionResult Admin()
        {
            return View();
        }

        [Authorize(Roles = "totalizacion-lider")]
        public ActionResult AdminCentro(int idCentro)
        {
            var db = new edayRoomEntities();
            var user = db.users.Single(u => u.username == User.Identity.Name);
            Centro centro = (from c in db.Centroes
                             where c.id == idCentro
                             select c).Single();
            List<Mesa> mesas = (from m in centro.Mesas1
                                where user.admin || m.AsignacionTotalizacions.Any(at => at.id_user == user.id)

                                select m).ToList();

            ViewData["centro"] = centro;
            ViewData["mesas"] = mesas;
            return View("AdminCentro");
        }

        [Authorize(Roles = "totalizacion-lider")]
        public ActionResult AdminMesa(int idMesa)
        {
            var db = new edayRoomEntities();

            Mesa mesa = (from m in db.Mesas
                         where m.id == idMesa
                         select m).Single();
            Centro centro = (from c in db.Centroes
                             where c.id == mesa.id_centro
                             select c).Single();
            bool totalizacionDone = mesa.Totalizacions.Any();
            Testigo testigo = mesa.Testigoes.FirstOrDefault(t => t.activo);

            ViewData["centro"] = centro;
            ViewData["mesa"] = mesa;
            ViewData["totalizacionDone"] = totalizacionDone;
            ViewData["contact"] = (from pt in db.TotalizacionTimelines
                                   where mesa.id == pt.id_mesa &&
                                         pt.activa
                                   select new TotalizacionContact
                                              {
                                                  Centro = mesa.Centro.Nombre,
                                                  CentroId = mesa.Centro.id,
                                                  LastUpdate = pt.fecha,
                                                  NextUpdate = pt.fecha,
                                                  Votantes = mesa.votantes,
                                                  IdTestigo = testigo.id,
                                                  Mesa = mesa.numero,
                                                  MesaId = mesa.id,
                                                  TotalizacionTimelineId = pt.id,
                                                  NombreTestigo = testigo.nombre,
                                                  Numero = testigo.numero
                                              }).SingleOrDefault();
            if (totalizacionDone)
            {
                ViewData["totales"] =
                    mesa.Totalizacions.OrderBy(t => t.RelacionCandidatoPartidoCoalicion.Candidato.nombre).ThenBy(
                        t => t.RelacionCandidatoPartidoCoalicion.Partido.nombre).ToList();
            }

            return View();
        }

        [Authorize(Roles = "totalizacion-lider")]
        public string GetCentros(int iDisplayStart, int iDisplayLength, int sEcho, int iSortCol_0, string sSortDir_0, string sSearch)
        {
            var dth = new DataTableHelper();
            var db = new edayRoomEntities();
            var user = db.users.Single(u => u.username == User.Identity.Name);
            var centros = from c in db.Centroes
                          where
                          
                              (user.admin ||
                               c.Mesas1.Any(m => m.AsignacionTotalizacions.Any(ap => ap.id_user == user.id)))
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
                            centros = centros.OrderByDescending(c => c.Mesas1.Sum(m => m.Totalizacions.Sum(t => t.valor)));
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
                                                        c.Mesas1.Any(m=>m.Totalizacions.Any())?
                                                            c.Mesas1.Sum(m => m.Totalizacions.Any() ? 
                                                            m.Totalizacions.Sum(p => p.valor) : 0)+"":"<span>-</span>",
                                                        c.Mesas1.All(m=>m.Totalizacions.Any())?
                                                        "<div class='mws-ic-16 ic-accept tableIcon16'></div>" :
                                                        "<div class='mws-ic-16 ic-cross tableIcon16'></div>" 
                                                    }))
            {
                dth.aaData.Add(l);
            }
            return new JavaScriptSerializer().Serialize(dth);
        }

        [Authorize(Roles = "totalizacion-lider")]
        public ActionResult GetChartData(int? idCentro, string tag1, string tag2, string estados, string municipios, bool quickCountOnly=false)
        {
            var db = new edayRoomEntities();

            var tag1Filter = string.IsNullOrWhiteSpace(tag1) ? null : tag1;
            var tag2Filter = string.IsNullOrWhiteSpace(tag2) ? null : tag2;
            var estadosFilter = string.IsNullOrWhiteSpace(estados) ? null : estados;
            var municipiosFilter = string.IsNullOrWhiteSpace(municipios) ? null : municipios;


            var data = db.GetTotalizacion(idCentro, null, tag1Filter, tag2Filter, estadosFilter, municipiosFilter, quickCountOnly, null).ToList();
            var totalVotos = (int)data.Sum(d => d.votos??1);
            var totalProyecciones = (int)data.Sum(d => d.proyeccion??1);
            var candidatos =
                data.Select(d=>new CandidatoCountChart
                                    {
                                        Nombre = d.nombre,
                                        Votos = d.votos??1,
                                        Proyeccion = d.proyeccion??1,
                                        Color = d.color,
                                        PorcentajeVotos = (float) Math.Round((d.votos??1.0)*100/(totalVotos==0?1:totalVotos),2),
                                        PorcentajeProyeccion = (float)Math.Round((d.proyeccion ?? 1.0) * 100 / (totalProyecciones == 0 ? 1 : totalProyecciones), 2)
                                    }).ToList();


            return Json(candidatos, JsonRequestBehavior.AllowGet);
        }
       
        [Authorize(Roles = "totalizacion-lider")]
        public JsonResult GetTotalizedPercentage()
        {
            var db = new edayRoomEntities();
            int mesas = db.Mesas.Count();
            int totalizaciones = db.Mesas.Count(m => m.Totalizacions.Any());
            double pct = mesas == 0 ? 0 : Math.Round((float)totalizaciones * 100 / mesas, 2);
            

            

            return Json(new { pct, mesas, totalizaciones }, JsonRequestBehavior.AllowGet);
        }
     
        [Authorize(Roles = "totalizacion-lider")]
        public ActionResult Usuarios()
        {
            var db = new edayRoomEntities();
            var user = db.users.Single(u => u.username == User.Identity.Name);
            var group = user.grupo;
            var users = db.users.Where(u => u.grupo == group && u.totalizacion);
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
                    db.users.Where(u => u.totalizacion && u.grupo == user.grupo && u.id != user.id && !u.paused).ToList();
                var groupCount = usersInGroup.Count;
                var assignedTotalizacion= db.AsignacionTotalizacions.Where(ap => ap.id_user == user.id).ToList();
                var roundRobin = 0;
                foreach (var ap in assignedTotalizacion)
                {
                    db.AsignacionTotalizacions.AddObject(new AsignacionTotalizacion
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

                var assignedTotalizacion =
                    db.AsignacionTotalizacions.Where(ap => ap.id_original_user == user.id && ap.isReplacement).ToList();
                foreach (var ap in assignedTotalizacion)
                {
                    db.AsignacionTotalizacions.DeleteObject(ap);
                }
            }


            db.SaveChanges();
            return user.paused.ToString(CultureInfo.InvariantCulture);
        }
    }
}