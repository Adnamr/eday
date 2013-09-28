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
    public class QuickCountController : Controller
    {
        // GET: /Totalizacions/
        [Authorize(Roles = "quickcount")]
        public ActionResult Index()
        {
            
            return View("QuickCount");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="centroId"></param>
        /// <returns></returns>
        [Authorize(Roles = "quickcount")]
        public string GetMesas(int centroId)
        {
            var db = new edayRoomEntities();

            var mesas = from m in db.Mesas
                        from t in db.Testigoes
                        where m.id_centro == centroId
                              && m.id == t.id_mesa
                              && t.activo
                        orderby m.numero
                        select new
                                   {
                                       IdMesa = m.id,
                                       Numero = m.numero,
                                       IdTestigo = t.id,
                                       Nombre = t.nombre,
                                       Telefono = t.numero,
                                       Votantes = m.votantes
                                   };

            return new JavaScriptSerializer().Serialize(mesas);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "quickcount")]
        public string GetContacts()
        {
            var db = new edayRoomEntities();
            var user = db.users.Single(u => u.username == User.Identity.Name);
            var contactos = (from c in db.Centroes
                             from pt in db.QuickCountTimelines
                             from asp in db.AsignacionQuickCounts
                             join a in db.QuickCountAlertas.Include("Alerta") on
                             new { id_centro = c.id, blocking = true, activa = true } equals
                             new { a.id_centro, a.Alerta.blocking, a.activa } into alertas2
                             from pa in alertas2.DefaultIfEmpty()

                             where
                                 c.id == pt.id_centro &&
                                 pt.activa &&
                                 c.Mesas1.Any(m=>m.cerrada) &&
                                 asp.id_centro == c.id &&
                                 asp.id_user == user.id &&
                                 !c.Mesas1.Where(m=> m.cerrada).Any(m=>m.Totalizacions.Any()) &&
                                 //Esto es lo que diferencia al quickcount
                                 (c.quickCountActive ?? false)
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
                                            LastUpdate = pt.fecha,
                                            NextUpdate = pt.fecha,
                                            Votantes = c.votantes ?? 0,
                                            TotalizacionTimelineId = pt.id,
                                            LinqAlertaObj = pa == null ? null : pa.Alerta,
                                            LinqTotalizacionAlertaObjQC = pa,
                                            LinqTotalizacionAlertaMessagesObjQC = pa.QuickCountAlertaMessages,
                                            IsAlertBlocked = pa != null
                                        }).ToList();
                
            List<TotalizacionContact> contactoTimelineCurrent =
                (from c in contactos where !c.IsAlertBlocked select c).ToList();/*.Take(3)*/
            List<TotalizacionContact> contactoTimelineAlertas =
                (from c in contactos where c.IsAlertBlocked select c).OrderBy(
                    c => c.BlockingAlert.Fecha).ToList();


            return new JavaScriptSerializer().Serialize(new
                                                            {
                                                                proximos = contactoTimelineCurrent,
                                                                alertas = contactoTimelineAlertas
                                                            });
        }

        #region actualizaciones
        /// <summary>
        /// 
        /// </summary>
        /// <param name="totalizacionId"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [Authorize(Roles = "quickcount")]
        public string UpdateSingleQuickCount(int totalizacionId, int value)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contacto"></param>
        /// <param name="valores"></param>
        /// <returns></returns>
        [Authorize(Roles = "quickcount")]
        public string UpdateQuickCount(TotalizacionContact contacto, CandidatoValueIncoming[] valores)
        {
            var db = new edayRoomEntities();
            var user = db.users.Single(u => u.username == User.Identity.Name);

            QuickCountTimeline oldTimeline = (from pt in db.QuickCountTimelines
                                              where pt.id == contacto.TotalizacionTimelineId
                                              select pt).Single();
            oldTimeline.activa = false;
            db.SaveChanges();

            IQueryable<Totalizacion> totalizaciones = db.Totalizacions.Where(t => t.id_mesa == contacto.MesaId);
            foreach (CandidatoValueIncoming v in valores)
            {
                int idCandidato = int.Parse(v.name.Replace("candidato-", ""));
                Totalizacion totalizacion = totalizaciones.SingleOrDefault(t => t.id_candidato == idCandidato);
                if (totalizacion == null)
                {
                    totalizacion = new Totalizacion
                                       {
                                           id_candidato = idCandidato,
                                           id_mesa = contacto.MesaId,
                                           valor = v.value,
                                           id_user = user.id
                                       };
                    db.Totalizacions.AddObject(totalizacion);
                }
                totalizacion.QuickCountTimelines.Add(oldTimeline);
            }

            var centro = db.Centroes.Single(c => c.id == contacto.CentroId);
            centro.totalizado = true;
            IQueryable<QuickCountAlerta> alertas = from a in db.QuickCountAlertas
                                                   where a.activa && !a.Alerta.blocking
                                                         && a.id_centro == contacto.CentroId
                                                   select a;
            foreach (QuickCountAlerta a in alertas)
            {
                a.activa = false;
            }

            
            //contacto.CentroId

            db.SaveChanges();
            MatrizDeSustitucion.UpdateMatriz(contacto.CentroId);
            return new JavaScriptSerializer().Serialize("");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "quickcount")]
        public string GetCandidatos()
        {
            var db = new edayRoomEntities();
            var candidatos =
                db.RelacionCandidatoPartidoCoalicions.Select(c => new
                                                                      {
                                                                          c.id,
                                                                          c.Candidato.nombre,
                                                                          partido = c.Partido.nombre,
                                                                          coalicion = c.Coalicion.nombre,
                                                                          value = 0,
                                                                          c.orden
                                                                      }).OrderBy(c => c.orden);
            return new JavaScriptSerializer().Serialize(candidatos);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contacto"></param>
        /// <param name="valor"></param>
        /// <param name="mensaje"></param>
        /// <returns></returns>
        [Authorize(Roles = "quickcount")]
        public string AlertaQuickCount(TotalizacionContact contacto, int valor, string mensaje)
        {
            var db = new edayRoomEntities();
            var user = db.users.Single(u => u.username == User.Identity.Name);
            #region Registro la alerta

            var alerta = new QuickCountAlerta
                             {
                                 activa = true,
                                 fecha = DateTime.Now,
                                 id_centro = contacto.CentroId,
                                 id_alerta = valor,
                                 id_usuario = user.id
                             };
            db.QuickCountAlertas.AddObject(alerta);


            var messages = new List<QuickCountAlertaMessage>();
            if (!string.IsNullOrWhiteSpace(mensaje))
            {
                var alertMessage = new QuickCountAlertaMessage { fecha = DateTime.Now, mensaje = mensaje, id_usuario = user.id };
                alerta.QuickCountAlertaMessages.Add(alertMessage);
                messages.Add(alertMessage);
            }

            #endregion

            #region  Retraso de Timeline

            Alerta objetoAlerta = db.Alertas.Single(a => a.id == valor);

            if (objetoAlerta.regresivo)
            {
                var oldTimelines =
                   (from pt in db.QuickCountTimelines
                    where
                        pt.activa && pt.id_centro == contacto.CentroId
                    //pt.id == contacto.MovilizacionTimelineId
                    select pt);
                foreach (var oldTimeline in oldTimelines)
                {
                    oldTimeline.activa = false;
                }


                //QuickCountTimeline oldTimeline =
                //    (from pt in db.QuickCountTimelines where pt.id == contacto.TotalizacionTimelineId select pt).
                //        Single();
                //oldTimeline.activa = false;
                DateTime newDate = DateTime.Now.AddMinutes(objetoAlerta.tiempo);
                var newTimeline = new QuickCountTimeline
                                      {
                                          id_centro = contacto.CentroId,
                                          fecha = newDate,
                                          activa = true
                                      };
                db.QuickCountTimelines.AddObject(newTimeline);
            }

            #endregion

            db.SaveChanges();


            // VERIFICAR SI HAY QUE HACER TRIGGER DE ALGUNA ALERTA
            IQueryable<QuickCountAlerta> existingAlerts = from a in db.QuickCountAlertas
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
                    var alertaAuto = new QuickCountAlerta
                                         {
                                             activa = true,
                                             fecha = DateTime.Now,
                                             id_centro = contacto.CentroId,
                                             id_alerta = newAlerta.id,
                                             id_usuario = user.id
                                         };
                    db.QuickCountAlertas.AddObject(alertaAuto);

                    var alertMessage = new QuickCountAlertaMessage { fecha = DateTime.Now, mensaje = "Alerta generada por sistema", id_usuario = user.id };
                    alertaAuto.QuickCountAlertaMessages.Add(alertMessage);
                }
            }
            db.SaveChanges();

            //var newContacto = TotalizacionContact.GetTotalizacionContact(contacto.IdTestigo);
            return new JavaScriptSerializer().Serialize("");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contacto"></param>
        /// <param name="mensaje"></param>
        /// <returns></returns>
        [Authorize(Roles = "quickcount")]
        public string ActualizarAlertaQuickCount(TotalizacionContact contacto, string mensaje)
        {
            var db = new edayRoomEntities();
            var user = db.users.Single(u => u.username == User.Identity.Name);
            QuickCountAlerta alerta = (from a in db.QuickCountAlertas
                                       where a.id == contacto.BlockingAlert.Id
                                       select a).Single();

            if (!string.IsNullOrWhiteSpace(mensaje))
            {
                var alertMessage = new QuickCountAlertaMessage { fecha = DateTime.Now, mensaje = mensaje, id_usuario = user.id};
                alerta.QuickCountAlertaMessages.Add(alertMessage);
            }

            db.SaveChanges();
            IQueryable<TotalizacionContactAlertMessage> mensajesAlerta = (from a in alerta.QuickCountAlertaMessages
                                                                          orderby a.fecha descending
                                                                          select new TotalizacionContactAlertMessage
                                                                                     {
                                                                                         Id = a.id,
                                                                                         Fecha = a.fecha,
                                                                                         Message = a.mensaje
                                                                                     }).AsQueryable();

            return new JavaScriptSerializer().Serialize("");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contacto"></param>
        /// <param name="mensaje"></param>
        /// <returns></returns>
        [Authorize(Roles = "quickcount")]
        public string CancelarAlertaQuickCount(TotalizacionContact contacto, string mensaje)
        {
            var db = new edayRoomEntities();
            QuickCountAlerta alerta = (from a in db.QuickCountAlertas
                                       where a.id == contacto.BlockingAlert.Id
                                       select a).Single();

            alerta.activa = false;
            db.SaveChanges();
            return new JavaScriptSerializer().Serialize("");
        }

        #endregion
        #region Administration

        [Authorize(Roles = "quickcount-lider")]
        public ActionResult Admin()
        {
            return View();
        }

        [Authorize(Roles = "quickcount-lider")]
        public ActionResult AdminCentro(int idCentro)
        {
            var db = new edayRoomEntities();
            Centro centro = (from c in db.Centroes
                             where c.id == idCentro
                             select c).Single();

            ViewData["centro"] = centro;
            bool quickCountDone = centro.Mesas1.Any(m => m.Totalizacions.Any());
            ViewData["quickCountDone"] = quickCountDone;
            if (quickCountDone)
            {
                Mesa mesa = centro.Mesas1.FirstOrDefault(m => m.Totalizacions.Any(t => t.QuickCountTimelines.Any())) ??
                            centro.Mesas1.FirstOrDefault(m => m.Totalizacions.Any());
                ViewData["mesa"] = mesa;
                ViewData["totales"] =
                    mesa.Totalizacions.OrderBy(t => t.RelacionCandidatoPartidoCoalicion.Candidato.nombre).ThenBy(
                        t => t.RelacionCandidatoPartidoCoalicion.Partido.nombre).ToList();
            }
            else
            {
                ViewData["contact"] = (from c in db.Centroes
                                       from pt in db.QuickCountTimelines
                                       where
                                           c.id == idCentro &&
                                           c.id == pt.id_centro &&
                                           pt.activa && (c.quickCountActive ?? false)
                                       select new TotalizacionContact
                                                  {
                                                      Centro = c.Nombre,
                                                      CentroId = c.id,
                                                      LastUpdate = pt.fecha,
                                                      NextUpdate = pt.fecha,
                                                      Votantes = c.votantes ?? 0,
                                                      TotalizacionTimelineId = pt.id
                                                  }).SingleOrDefault();
            }

            return View();
        }

        [Authorize(Roles = "quickcount-lider")]
        public ActionResult GetChartData(string tag1, string tag2, string estados, string municipios)
        {

            var db = new edayRoomEntities();

            var tag1Filter = string.IsNullOrWhiteSpace(tag1) ? null : tag1;
            var tag2Filter = string.IsNullOrWhiteSpace(tag2) ? null : tag2;
            var estadosFilter = string.IsNullOrWhiteSpace(estados) ? null : estados;
            var municipiosFilter = string.IsNullOrWhiteSpace(municipios) ? null : municipios;


            var data = db.GetTotalizacion(null, null, tag1Filter, tag2Filter, estadosFilter, municipiosFilter, true, null).ToList();
            var totalVotos = (int)data.Sum(d => d.votos ?? 1);
            var totalProyecciones = (int)data.Sum(d => d.proyeccion ?? 1);
            var candidatos =
                data.Select(d => new CandidatoCountChart
                {
                    Nombre = d.nombre,
                    Votos = d.votos ?? 1,
                    Proyeccion = d.proyeccion ?? 1,
                    Color = d.color,
                    PorcentajeVotos = (float)Math.Round((d.votos ?? 1.0) * 100 / (totalVotos == 0 ? 1 : totalVotos), 2),
                    PorcentajeProyeccion = (float)Math.Round((d.proyeccion ?? 1.0) * 100 / (totalProyecciones == 0 ? 1 : totalProyecciones), 2)
                }).ToList();


            return Json(candidatos, JsonRequestBehavior.AllowGet);
            //var db = new edayRoomEntities();
            //IQueryable<Totalizacion> tots = db.Totalizacions.Where(t => t.QuickCountTimelines.Any());


            //IOrderedQueryable<RelacionCandidatoPartidoCoalicion> rel =
            //    db.RelacionCandidatoPartidoCoalicions.OrderBy(r => r.Candidato.nombre + r.Partido.nombre);
            //var candidatos = new List<CandidatoCountChart>();
            //foreach (RelacionCandidatoPartidoCoalicion r in rel)
            //{
            //    IQueryable<Totalizacion> candidatoTots = tots.Where(t => t.id_candidato == r.id);
            //    IQueryable<IGrouping<int, Totalizacion>> groupbyMesa = candidatoTots.GroupBy(t => t.id_mesa);

            //    int votos = groupbyMesa.Sum(t => t.Max(m => m.valor));
            //    var candidato = new CandidatoCountChart
            //                        {
            //                            Nombre = r.Candidato.nombre,
            //                            Votos = votos
            //                        };
            //    candidatos.Add(candidato);
            //}

            //return Json(candidatos, JsonRequestBehavior.AllowGet);
            //CandidatoCountChart
        }
        [Authorize(Roles = "quickcount-lider")]
        public JsonResult GetTotalizedPercentage()
        {
            var db = new edayRoomEntities();
            int centros = db.Centroes.Count(c => c.quickCountActive ?? false);
            int totalizaciones = db.Mesas.Count(m => m.Totalizacions.Any(t => t.QuickCountTimelines.Any()));
            return Json(new { pct = Math.Round((float)totalizaciones * 100 / centros, 2), centros, totalizaciones }, JsonRequestBehavior.AllowGet);
        }
        [Authorize(Roles = "quickcount-lider")]
        public string GetCentros(int iDisplayStart, int iDisplayLength, int sEcho, int iSortCol_0, string sSortDir_0, string sSearch)
        {
            var dth = new DataTableHelper();
            var db = new edayRoomEntities();
            user user = db.users.Single(u => u.username == User.Identity.Name);
            IQueryable<Centro> centros = from c in db.Centroes
                                         where
                                         c.quickCountActive ?? false &&
                                             (user.admin ||
                                              c.AsignacionQuickCounts.Any(ap => ap.id_user == user.id))
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
                                                        c.Mesas1.Any(m=>m.Totalizacions.Any())?
                                                        "<div class='mws-ic-16 ic-accept tableIcon16'></div>" :
                                                        "<div class='mws-ic-16 ic-cross tableIcon16'></div>" 
                                                    }))
            {
                dth.aaData.Add(l);
            }
            return new JavaScriptSerializer().Serialize(dth);
        }
      
        [Authorize(Roles = "quickcount-lider")]
        public ActionResult Usuarios()
        {
            var db = new edayRoomEntities();
            var user = db.users.Single(u => u.username == User.Identity.Name);
            var group = user.grupo;
            var users = db.users.Where(u => u.grupo == group && u.quickcount);
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
                    db.users.Where(u => u.quickcount && u.grupo == user.grupo && u.id != user.id && !u.paused).ToList();
                var groupCount = usersInGroup.Count;
                var assignedQuickCount = db.AsignacionQuickCounts.Where(ap => ap.id_user == user.id).ToList();
                var roundRobin = 0;
                foreach (var ap in assignedQuickCount)
                {
                    db.AsignacionQuickCounts.AddObject(new AsignacionQuickCount
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

                var assignedQuickCount =
                    db.AsignacionQuickCounts.Where(ap => ap.id_original_user == user.id && ap.isReplacement).ToList();
                foreach (var ap in assignedQuickCount)
                {
                    db.AsignacionQuickCounts.DeleteObject(ap);
                }
            }


            db.SaveChanges();
            return user.paused.ToString(CultureInfo.InvariantCulture);
        }
    }
}