using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using EdayRoom.API;
using EdayRoom.API.DataTable;
using EdayRoom.Models;

namespace EdayRoom.Controllers
{
    public class AlertasController : Controller
    {

        #region ADMINISTRACION

        [Authorize(Roles = "admin")]
        public ActionResult Index()
        {
            var db = new edayRoomEntities();
            IQueryable<Alerta> alertas = from a in db.Alertas
                                         select a;
            ViewData["alertas"] = alertas.ToList();
            return View("Alertas");
        }

        [Authorize(Roles = "admin")]
        public string CrearAlerta(Alerta alerta)
        {
            var db = new edayRoomEntities();
            db.Alertas.AddObject(alerta);
            db.SaveChanges();
            return new JavaScriptSerializer().Serialize("");
        }

        [Authorize]
        public string GetAllAlertas(int iDisplayStart, int iDisplayLength, int sEcho, int iSortCol_0, string sSortDir_0,
                                    string sSearch)
        {
            var dth = new DataTableHelper();
            var db = new edayRoomEntities();
            IQueryable<Alerta> alertas = from a in db.Alertas
                                         select a;

            dth.iTotalRecords = alertas.Count();
            if (!string.IsNullOrEmpty(sSearch))
            {
                alertas = alertas.Where(a => a.name.ToLower().Contains(sSearch.ToLower()));
            }
            dth.iTotalDisplayRecords = alertas.Count();

            #region ORDER ALERTAS

            switch (sSortDir_0)
            {
                case "asc":
                    switch (iSortCol_0)
                    {
                        case 0:
                        case 4:
                            alertas = alertas.OrderBy(a => a.name);
                            break;
                        case 1:
                            alertas = alertas.OrderBy(a => a.blocking);
                            break;
                        case 2:
                            alertas = alertas.OrderBy(a => a.regresivo);
                            break;
                        case 3:
                            alertas = alertas.OrderBy(a => a.tiempo);
                            break;
                    }
                    break;
                case "desc":
                    switch (iSortCol_0)
                    {
                        case 0:
                        case 4:
                            alertas = alertas.OrderByDescending(a => a.name);
                            break;
                        case 1:
                            alertas = alertas.OrderByDescending(a => a.blocking);
                            break;
                        case 2:
                            alertas = alertas.OrderByDescending(a => a.regresivo);
                            break;
                        case 3:
                            alertas = alertas.OrderByDescending(a => a.tiempo);
                            break;
                    }

                    break;
            }

            #endregion

            dth.sEcho = sEcho;
            var alist = alertas.Select(a => new
                                                {
                                                    a.id,
                                                    a.name,
                                                    a.blocking,
                                                    a.regresivo,
                                                    a.tiempo,
                                                    a.canceledBy,
                                                    a.maxRepeats,
                                                    triggerAlerta =
                                                a.AlertaAsociada == null ? null : a.AlertaAsociada.name
                                                }).Skip(iDisplayStart).Take(iDisplayLength).ToList();

            dth.aaData = new List<List<string>>();
            foreach (var l in alist.Select(a => new List<string>
                                                    {
                                                        a.name.Trim(),
                                                        a.canceledBy,
                                                        (a.blocking
                                                             ? "<div class='mws-ic-16 ic-accept tableIcon16'></div>"
                                                             : "<div class='mws-ic-16 ic-cross tableIcon16'></div>"),
                                                        (a.regresivo
                                                             ? "<div class='mws-ic-16 ic-accept tableIcon16'></div>"
                                                             : "<div class='mws-ic-16 ic-cross tableIcon16'></div>"),
                                                        a.tiempo.ToString(CultureInfo.InvariantCulture),
                                                        a.maxRepeats + "",
                                                        a.triggerAlerta ?? "NA",
                                                        string.Format(
                                                            "<div class='mws-ic-16 ic-edit editAlerta  tableIcon16 clickable' alerta-id='{0}' style='float:left;'></div><div class='mws-ic-16 ic-trash deleteAlerta tableIcon16 clickable'  alerta-id='{0}' style='float:left; margin-left: 10px;'></div>",
                                                            a.id)
                                                    })
                )
            {
                dth.aaData.Add(l);
            }

            return new JavaScriptSerializer().Serialize(dth);
        }

        [Authorize(Roles = "admin")]
        public string DeleteAlerta(int id)
        {
            var db = new edayRoomEntities();
            Alerta alerta = (from a in db.Alertas
                             where a.id == id
                             select a).SingleOrDefault();
            if (alerta != null)
            {
                db.Alertas.DeleteObject(alerta);
            }
            db.SaveChanges();
            return "Deleted";
        }

        [Authorize]
        public string GetAlerta(int id)
        {
            var db = new edayRoomEntities();
            var alerta = (from a in db.Alertas
                          where a.id == id
                          select
                              new
                                  {
                                      a.id,
                                      a.name,
                                      a.blocking,
                                      a.regresivo,
                                      a.tiempo,
                                      a.canceledBy,
                                      a.maxRepeats,
                                      a.asocAlerta
                                  }).SingleOrDefault();
            return new JavaScriptSerializer().Serialize(alerta);
        }

        [Authorize(Roles = "admin")]
        public string EditAlerta(Alerta alerta)
        {
            if (ModelState.IsValid)
            {
                var db = new edayRoomEntities();
                db.Alertas.Attach(alerta);
                db.ObjectStateManager.ChangeObjectState(alerta, EntityState.Modified);
                db.SaveChanges();
            }
            return "";
        }

        [Authorize]
        public string GetAlertas()
        {
            var db = new edayRoomEntities();
            var alertas = from a in db.Alertas
                          orderby a.name
                          select
                              new
                                  {
                                      a.id,
                                      a.name,
                                      a.blocking,
                                      a.regresivo,
                                      a.tiempo,
                                      a.canceledBy,
                                      a.maxRepeats,
                                      a.asocAlerta
                                  };
            return new JavaScriptSerializer().Serialize(alertas);
        }

        private class AlertaModulo
        {
            public string modulo;
            public int id;
            public string name;
            public DateTime fecha;
            public string centro;
            public int mesa = -1;
            public int id_usuario;
            public bool active;
            public string nombreUsuario;
            public string estado;
            public string municipio;
            public string parroquia;
            public string unique_id;
            public bool quickCount;
        }

        private IQueryable<AlertaModulo> GetUserAlertas()
        {
            var db = new edayRoomEntities();
            var user = db.users.Single(u => u.username == User.Identity.Name);

            var alertas =
                //Agrega Participacion
                from pa in db.ParticipacionAlertas
                where pa.activa && pa.Alerta.blocking
                select new AlertaModulo
                           {
                               modulo = "Participación",
                               id = pa.id,
                               name = pa.Alerta.name,
                               fecha = pa.fecha,
                               centro = pa.Mesa.Centro.Nombre,
                               estado = pa.Mesa.Centro.unidadGeografica1,
                               municipio = pa.Mesa.Centro.unidadGeografica2,
                               parroquia = pa.Mesa.Centro.unidadGeografica3,
                               mesa = pa.Mesa.numero,
                               id_usuario = pa.id_usuario,
                               active = pa.activa,
                               nombreUsuario = pa.user.username,
                               unique_id = pa.Mesa.Centro.unique_id,
                               quickCount = pa.Mesa.Centro.quickCountActive??false
                           };
            //Agrega movilizacion
            alertas = alertas.Union(
                from pa in db.MovilizacionAlertas
                where pa.activa && pa.Alerta.blocking
                select new AlertaModulo
                           {
                               modulo = "Movilización",
                               id = pa.id,
                               name = pa.Alerta.name,
                               fecha = pa.fecha,
                               centro = pa.Centro.Nombre,
                               estado = pa.Centro.unidadGeografica1,
                               municipio = pa.Centro.unidadGeografica2,
                               parroquia = pa.Centro.unidadGeografica3,
                               mesa = -1,
                               id_usuario = pa.id_usuario,
                               active = pa.activa,
                               nombreUsuario = pa.user.username,
                               unique_id = pa.Centro.unique_id,
                               quickCount = pa.Centro.quickCountActive ?? false
                           });
            //Agrega exitpoll
            alertas = alertas.Union(
                from pa in db.ExitPollAlertas
                where pa.activa && pa.Alerta.blocking
                select new AlertaModulo
                           {
                               modulo = "Exit Poll",
                               id = pa.id,
                               name = pa.Alerta.name,
                               fecha = pa.fecha,
                               centro = pa.Centro.Nombre,
                               estado = pa.Centro.unidadGeografica1,
                               municipio = pa.Centro.unidadGeografica2,
                               parroquia = pa.Centro.unidadGeografica3,
                               mesa = -1,
                               id_usuario = pa.id_usuario,
                               active = pa.activa,
                               nombreUsuario = pa.user.username,
                               unique_id = pa.Centro.unique_id,
                               quickCount = pa.Centro.quickCountActive ?? false
                           });
            //Agrega quickcount
            alertas = alertas.Union(
                from pa in db.QuickCountAlertas
                where pa.activa && pa.Alerta.blocking
                select new AlertaModulo
                           {
                               modulo = "Conteo Rapido",
                               id = pa.id,
                               name = pa.Alerta.name,
                               fecha = pa.fecha,
                               centro = pa.Centro.Nombre,
                               estado = pa.Centro.unidadGeografica1,
                               municipio = pa.Centro.unidadGeografica2,
                               parroquia = pa.Centro.unidadGeografica3,
                               mesa = -1,
                               id_usuario = pa.id_usuario,
                               active = pa.activa,
                               nombreUsuario = pa.user.username,
                               unique_id = pa.Centro.unique_id,
                               quickCount = pa.Centro.quickCountActive ?? false
                           }
                );
            //Agrega totalización
            alertas = alertas.Union(
                from pa in db.TotalizacionAlertas
                where pa.activa && pa.Alerta.blocking
                select new AlertaModulo
                           {
                               modulo = "Totalización",
                               id = pa.id,
                               name = pa.Alerta.name,
                               fecha = pa.fecha,
                               centro = pa.Mesa.Centro.Nombre,
                               estado = pa.Mesa.Centro.unidadGeografica1,
                               municipio = pa.Mesa.Centro.unidadGeografica2,
                               parroquia = pa.Mesa.Centro.unidadGeografica3,
                               mesa = pa.Mesa.numero,
                               id_usuario = pa.id_usuario,
                               active = pa.activa,
                               nombreUsuario = pa.user.username,
                               unique_id = pa.Mesa.Centro.unique_id,
                               quickCount = pa.Mesa.Centro.quickCountActive ?? false

                           });

            if (user.admin || user.supervisor)
            {
                //do nothing
            }
            else if (user.leader)
            {
                var users = db.users.Where(u => u.grupo == user.grupo).Select(u => u.id).ToList();
                alertas = alertas.Where(a => users.Contains(a.id_usuario));
            }
            else
            {
                alertas = alertas.Where(a => a.id_usuario == user.id);
            }
            //var al = alertas.ToList();
            return alertas;
        }

        [Authorize]
        public string GetActiveAlertas()
        {
            var al = GetUserAlertas();
            return new JavaScriptSerializer().Serialize(new
                                                            {
                                                                count = al.Count(),
                                                                alertas = al.OrderByDescending(a => a.fecha).Take(6),
                                                                fecha = DateTime.Now
                                                            });
        }

        #endregion

        [Authorize]
        public void AddMessageToAlert(int idAlerta, string modulo, string message)
        {
            var db = new edayRoomEntities();
            var user = db.users.Single(u => u.username == User.Identity.Name);

            switch (modulo)
            {
                case "Participación":
                case "Participaci&#243;n":
                    db.ParticipacionAlertaMessages.AddObject(new ParticipacionAlertaMessage
                                                                 {
                                                                     id_usuario = user.id,
                                                                     mensaje = message,
                                                                     fecha = DateTime.Now,
                                                                     alerta_id = idAlerta
                                                                 });
                    break;
                case "Movilización":
                case "Movilizaci&#243;n":
                    db.MovilizacionAlertaMessages.AddObject(new MovilizacionAlertaMessage()
                                                                {
                                                                    id_usuario = user.id,
                                                                    mensaje = message,
                                                                    fecha = DateTime.Now,
                                                                    alerta_id = idAlerta
                                                                });

                    break;
                case "Exit Poll":
                    db.ExitPollAlertaMessages.AddObject(new ExitPollAlertaMessage()
                                                            {
                                                                id_usuario = user.id,
                                                                mensaje = message,
                                                                fecha = DateTime.Now,
                                                                alerta_id = idAlerta
                                                            });

                    break;
                case "Conteo Rapido":
                    db.QuickCountAlertaMessages.AddObject(new QuickCountAlertaMessage()
                                                              {
                                                                  id_usuario = user.id,
                                                                  mensaje = message,
                                                                  fecha = DateTime.Now,
                                                                  alerta_id = idAlerta
                                                              });

                    break;
                case "Totalización":
                    db.TotalizacionAlertaMessages.AddObject(new TotalizacionAlertaMessage()
                                                                {
                                                                    id_usuario = user.id,
                                                                    mensaje = message,
                                                                    fecha = DateTime.Now,
                                                                    alerta_id = idAlerta
                                                                });

                    break;
            }
            db.SaveChanges();
        }


        [Authorize(Roles = "admin,supervisor,leader")]
        public ActionResult ListAlertas(string modulo)
        {
            ViewData["modulo"] = modulo;
            return View("ListAlertas");
        }

        [Authorize]
        public ActionResult DetailAlerta(int idAlerta, string modulo)
        {
            ViewData["idAlerta"] = idAlerta;
            ViewData["modulo"] = modulo;
            string centro = null, mesa = null, alerta = null;
            int idCentro = -1, idMesa = -1;
            DateTime? fecha = DateTime.Now;
            var messages = new List<AlertaMessageWrapper>();
            var db = new edayRoomEntities();
            Centro centroObj = new Centro();

            switch (modulo)
            {
                case "Participación":
                    {
                        var alertaObj = db.ParticipacionAlertas.SingleOrDefault(a => a.id == idAlerta);
                        if (alertaObj != null)
                        {
                            centro = alertaObj.Mesa.Centro.Nombre;
                            centroObj = alertaObj.Mesa.Centro;
                            idCentro = alertaObj.Mesa.Centro.id;
                            mesa = alertaObj.Mesa.numero.ToString(CultureInfo.InvariantCulture);
                            idMesa = alertaObj.Mesa.id;
                            alerta = alertaObj.Alerta.name;
                            fecha = alertaObj.fecha;
                            messages = alertaObj.ParticipacionAlertaMessages.Select(pa => new AlertaMessageWrapper
                                                                                              {
                                                                                                  User =
                                                                                                      pa.user.nombre +
                                                                                                      " " +
                                                                                                      pa.user.apellido,
                                                                                                  Fecha = pa.fecha,
                                                                                                  Message = pa.mensaje
                                                                                              }).OrderBy(pa => pa.Fecha)
                                .ToList();
                        }
                    }
                    break;
                case "Movilización":
                    {
                        var alertaObj = db.MovilizacionAlertas.SingleOrDefault(a => a.id == idAlerta);
                        if (alertaObj != null)
                        {
                            centro = alertaObj.Centro.Nombre;
                            centroObj = alertaObj.Centro;
                            idCentro = alertaObj.Centro.id;
                            alerta = alertaObj.Alerta.name;
                            fecha = alertaObj.fecha;
                            messages = alertaObj.MovilizacionAlertaMessages.Select(pa => new AlertaMessageWrapper
                                                                                             {
                                                                                                 User =
                                                                                                     pa.user.nombre +
                                                                                                     " " +
                                                                                                     pa.user.apellido,
                                                                                                 Fecha = pa.fecha,
                                                                                                 Message = pa.mensaje
                                                                                             }).OrderBy(pa => pa.Fecha).
                                ToList();

                        }
                    }
                    break;
                case "Exit Poll":
                    {
                        var alertaObj = db.ExitPollAlertas.SingleOrDefault(a => a.id == idAlerta);
                        if (alertaObj != null)
                        {
                            centro = alertaObj.Centro.Nombre;
                            centroObj = alertaObj.Centro;

                            idCentro = alertaObj.Centro.id;
                            alerta = alertaObj.Alerta.name;
                            fecha = alertaObj.fecha;
                            messages = alertaObj.ExitPollAlertaMessages.Select(pa => new AlertaMessageWrapper
                                                                                         {
                                                                                             User =
                                                                                                 pa.user.nombre + " " +
                                                                                                 pa.user.apellido,
                                                                                             Fecha = pa.fecha,
                                                                                             Message = pa.mensaje
                                                                                         }).OrderBy(pa => pa.Fecha).
                                ToList();

                        }
                    }
                    break;
                case "Conteo Rapido":
                    {
                        var alertaObj = db.QuickCountAlertas.SingleOrDefault(a => a.id == idAlerta);
                        if (alertaObj != null)
                        {
                            centro = alertaObj.Centro.Nombre;
                            centroObj = alertaObj.Centro;

                            idCentro = alertaObj.Centro.id;
                            alerta = alertaObj.Alerta.name;
                            fecha = alertaObj.fecha;
                            messages = alertaObj.QuickCountAlertaMessages.Select(pa => new AlertaMessageWrapper
                                                                                           {
                                                                                               User =
                                                                                                   pa.user.nombre + " " +
                                                                                                   pa.user.apellido,
                                                                                               Fecha = pa.fecha,
                                                                                               Message = pa.mensaje
                                                                                           }).OrderBy(pa => pa.Fecha).
                                ToList();

                        }
                    }
                    break;
                case "Totalización":
                    {
                        var alertaObj = db.TotalizacionAlertas.SingleOrDefault(a => a.id == idAlerta);
                        if (alertaObj != null)
                        {
                            centro = alertaObj.Mesa.Centro.Nombre;
                            centroObj = alertaObj.Mesa.Centro;

                            idCentro = alertaObj.Mesa.Centro.id;
                            mesa = alertaObj.Mesa.numero.ToString(CultureInfo.InvariantCulture);
                            idMesa = alertaObj.Mesa.id;
                            alerta = alertaObj.Alerta.name;
                            fecha = alertaObj.fecha;
                            messages = alertaObj.TotalizacionAlertaMessages.Select(pa => new AlertaMessageWrapper
                                                                                             {
                                                                                                 User =
                                                                                                     pa.user.nombre +
                                                                                                     " " +
                                                                                                     pa.user.apellido,
                                                                                                 Fecha = pa.fecha,
                                                                                                 Message = pa.mensaje
                                                                                             }).OrderBy(pa => pa.Fecha).
                                ToList();

                        }
                    }
                    break;

            }


            ViewData["centro"] = centro;
            ViewData["centroObj"] = centroObj;
            ViewData["idCentro"] = idCentro;

            ViewData["mesa"] = mesa;
            ViewData["idMesa"] = idMesa;

            ViewData["alerta"] = alerta;
            ViewData["idAlerta"] = idAlerta;

            ViewData["fecha"] = fecha;
            ViewData["messages"] = messages;

            return View("DetailAlerta");
        }

        [Authorize]
        public string GetTableAlertas(int iDisplayStart, int iDisplayLength, int sEcho, int iSortCol_0, string sSortDir_0, string sSearch, string modulo, bool quickCountOnly)
        {
            var dth = new DataTableHelper();
            //TODO: Optimizar esta consulta
            var alertas = GetUserAlertas();

            if (!string.IsNullOrEmpty(sSearch))
            {

                alertas = alertas.Where(a =>
                                        a.name.ToLower().Contains(sSearch.ToLower()) ||
                                        a.modulo.ToLower().Contains(sSearch.ToLower()) ||
                                        a.centro.ToLower().Contains(sSearch.ToLower()) ||
                                        a.estado.ToLower().Contains(sSearch.ToLower()) ||
                                        a.municipio.ToLower().Contains(sSearch.ToLower()) ||
                                        a.parroquia.ToLower().Contains(sSearch.ToLower()) ||
                                        a.nombreUsuario.ToLower().Contains(sSearch.ToLower())
                    );
            }
            if(quickCountOnly)
            {
                alertas = alertas.Where(a => a.quickCount);
            }
            if(!string.IsNullOrEmpty(modulo))
            {
                switch(modulo)
                {
                    case "participacion":
                        alertas = alertas.Where(a=>a.modulo == "Participación");
                        break;
                    case "movilizacion":
                        alertas = alertas.Where(a=>a.modulo == "Movilización");
                        break;
                    case "quickcount":
                        alertas = alertas.Where(a => a.modulo == "Conteo Rapido");
                        break;
                    case "totalizacion":
                        alertas = alertas.Where(a=>a.modulo == "Totalización");
                        break;
                    case "exitpoll":
                        alertas = alertas.Where(a => a.modulo == "Exit Poll");
                        break;

                }

            }

            //TODO: Arreglar el timeout que da este count
            dth.iTotalRecords = 10000;// alertas.Count();
            
            dth.iTotalDisplayRecords = dth.iTotalRecords;
            dth.sEcho = sEcho;
            
            var clist = alertas.Select(c => c).OrderBy(c=>c.fecha).Skip(iDisplayStart).Take(iDisplayLength).ToList();
            dth.aaData = new List<List<string>>();
            foreach (var l in clist.Select(c => new List<string>
                                                    {
                                                        string.Format(
                                                            "<a href='/Alertas/DetailAlerta?idAlerta={0}&modulo={2}' alerta-id='{0}' " +
                                                            "class='alerta-link'>{1}</a>",
                                                            c.id, c.centro, c.modulo),
                                                        c.unique_id,     
                                                        c.mesa == -1
                                                            ? "-"
                                                            : c.mesa.ToString(CultureInfo.InvariantCulture),
                                                        c.modulo,
                                                        c.name,
                                                        c.estado,c.municipio,c.parroquia,
                                                        c.fecha.ToString("yyyy/MM/dd @ HH:mm"),
                                                       // c.active? "Si":"No",
                                                        c.nombreUsuario    
                                                    }))
            {
                dth.aaData.Add(l);
            }
            return new JavaScriptSerializer().Serialize(dth);

        }

        public ActionResult CancelAlert(int idAlerta, string modulo)
        {
            var db = new edayRoomEntities();

            switch (modulo)
            {
                case "Participación":
                    db.ParticipacionAlertas.Single(pa => pa.id == idAlerta).activa = false;
                    break;
                case "Movilización":
                    db.MovilizacionAlertas.Single(pa => pa.id == idAlerta).activa = false;
                    break;
                case "Exit Poll":
                    db.ExitPollAlertas.Single(pa => pa.id == idAlerta).activa = false;
                    break;
                case "QuickCount":
                    db.QuickCountAlertas.Single(pa => pa.id == idAlerta).activa = false;
                    break;
                case "Totalización":
                    db.TotalizacionAlertas.Single(pa => pa.id == idAlerta).activa = false;
                    break;
            }
            db.SaveChanges();
            return RedirectToAction("ListAlertas");
        }
    }
}