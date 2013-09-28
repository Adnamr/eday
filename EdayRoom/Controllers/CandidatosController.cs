using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using EdayRoom.API.DataTable;
using EdayRoom.Models;

namespace EdayRoom.Controllers
{
    public class CandidatosController : Controller
    {
        //
        // GET: /Candidatos/
        [Authorize]
        public ActionResult Index()
        {
            return View("Candidatos");
        }

        [Authorize]
        public string GetAllCandidatos(int iDisplayStart, int iDisplayLength, int sEcho, int iSortCol_0,
                                       string sSortDir_0, string sSearch)
        {
            var dth = new DataTableHelper();
            var db = new edayRoomEntities();
            IQueryable<RelacionCandidatoPartidoCoalicion> candidatos = from c in db.RelacionCandidatoPartidoCoalicions
                                                                       select c;

            dth.iTotalRecords = candidatos.Count();
            if (!string.IsNullOrEmpty(sSearch))
            {
                candidatos = candidatos.Where(c => c.Candidato.nombre.ToLower().Contains(sSearch.ToLower()));
            }
            dth.iTotalDisplayRecords = candidatos.Count();

            #region ORDER ALERTAS

            switch (sSortDir_0)
            {
                case "asc":
                    switch (iSortCol_0)
                    {
                        case 0:
                        case 3:
                            candidatos = candidatos.OrderBy(c => c.Candidato.nombre);
                            break;
                        case 1:
                            candidatos = candidatos.OrderBy(c => c.Coalicion.nombre);
                            break;
                        case 2:
                            candidatos = candidatos.OrderBy(c => c.Partido.nombre);
                            break;
                    }
                    break;
                case "desc":
                    switch (iSortCol_0)
                    {
                        case 0:
                        case 3:
                            candidatos = candidatos.OrderByDescending(c => c.Candidato.nombre);
                            break;
                        case 1:
                            candidatos = candidatos.OrderByDescending(c => c.Coalicion.nombre);
                            break;
                        case 2:
                            candidatos = candidatos.OrderByDescending(c => c.Partido.nombre);
                            break;
                    }

                    break;
            }

            #endregion

            dth.sEcho = sEcho;
            var clist = candidatos.Select(c => new
                                                   {
                                                       c.id,
                                                       candidato = c.Candidato.nombre,
                                                       color = c.Candidato.color,
                                                       partido = c.Partido.nombre,
                                                       colorpartido = c.Partido.color,
                                                       coalicion = c.Coalicion.nombre,
                                                       colorcoalicion = c.Coalicion.color
                                                   }).Skip(iDisplayStart).Take(iDisplayLength).ToList();

            dth.aaData = new List<List<string>>();

            foreach (var l in clist.Select(c => new List<string>
                                                    {
                                                        (string.IsNullOrEmpty(c.color)?"":"<div class='swatch' style='background-color:#"+c.color+"'></div>") + c.candidato.Trim() ,
                                                        (string.IsNullOrEmpty(c.colorcoalicion)?"":"<div class='swatch' style='background-color:#"+c.colorcoalicion+"'></div>")+c.coalicion,
                                                        (string.IsNullOrEmpty(c.colorpartido)?"":"<div class='swatch' style='background-color:#"+c.colorpartido+"'></div>") + c.partido,
                                                        string.Format(
                                                            "<div class='mws-ic-16 ic-edit editCandidato  tableIcon16 clickable' candidato-id='{0}' style='float:left;'></div><div class='mws-ic-16 ic-trash deleteCandidato tableIcon16 clickable'  candidato-id='{0}' style='float:left; margin-left: 10px;'></div>",
                                                            c.id)
                                                    })
                )
            {
                dth.aaData.Add(l);
            }

            return new JavaScriptSerializer().Serialize(dth);
        }

        [Authorize]
        public string CrearCandidato(string nombre, string coalicion, string partido, string color, string colorpartido, string colorcoalicion)
        {
            var db = new edayRoomEntities();
            var rel = new RelacionCandidatoPartidoCoalicion();
            db.RelacionCandidatoPartidoCoalicions.AddObject(rel);
            Candidato candidato = db.Candidatoes.SingleOrDefault(c => c.nombre.ToLower() == nombre.ToLower());
            if (candidato == null)
            {
                candidato = new Candidato {nombre = nombre, color = color};
                db.Candidatoes.AddObject(candidato);
            }
            Partido partidoObj = db.Partidoes.SingleOrDefault(c => c.nombre.ToLower() == partido.ToLower());
            if (partidoObj == null)
            {
                partidoObj = new Partido {nombre = partido, color=colorpartido};
                db.Partidoes.AddObject(partidoObj);
            }

            Coalicion coalicionObj = db.Coalicions.SingleOrDefault(c => c.nombre.ToLower() == coalicion.ToLower());
            if (coalicionObj == null)
            {
                coalicionObj = new Coalicion {nombre = coalicion, color = colorcoalicion};
                db.Coalicions.AddObject(coalicionObj);
            }

            partidoObj.RelacionCandidatoPartidoCoalicions.Add(rel);
            coalicionObj.RelacionCandidatoPartidoCoalicions.Add(rel);
            candidato.RelacionCandidatoPartidoCoalicions.Add(rel);

            db.SaveChanges();
            return new JavaScriptSerializer().Serialize("");
        }

        [Authorize]
        public string DeleteCandidato(int id)
        {
            var db = new edayRoomEntities();
            Candidato candidato = (from c in db.Candidatoes
                                   where c.id == id
                                   select c).SingleOrDefault();
            if (candidato != null)
            {
                db.Candidatoes.DeleteObject(candidato);
            }
            db.SaveChanges();
            return "Deleted";
        }

        [Authorize]
        public string GetCandidato(int id)
        {
            var db = new edayRoomEntities();
            var candidato = (from c in db.RelacionCandidatoPartidoCoalicions
                             where c.id == id
                             select new
                                        {
                                            c.id,
                                            c.Candidato.nombre,
                                            c.Candidato.color,
                                            partido = c.Partido.nombre,
                                            colorpartido = c.Partido.color,
                                            coalicion = c.Coalicion.nombre,
                                            colorcoalicion = c.Coalicion.color
                                        }).SingleOrDefault();
            return new JavaScriptSerializer().Serialize(candidato);
        }

        [Authorize]
        public string EditCandidato(int relid, string nombre, string partido, string coalicion, string color, string colorpartido, string colorcoalicion)
        {
            var db = new edayRoomEntities();
            RelacionCandidatoPartidoCoalicion rel =
                db.RelacionCandidatoPartidoCoalicions.SingleOrDefault(r => r.id == relid);

            #region check candidato

            if (rel != null)
            {
                Candidato candidato = db.Candidatoes.SingleOrDefault(c => c.nombre.ToLower() == nombre.ToLower());
                //Chequeo si tengo que editar directamente al candidato o crear uno nuevo
                if (rel.Candidato.RelacionCandidatoPartidoCoalicions.Count() > 1)
                {
                    //Creo nuevo candidato
                    if (rel.Candidato.nombre != nombre || rel.Candidato.color != color)
                    {
                        if (candidato == null)
                        {
                            candidato = new Candidato {nombre = nombre};
                            db.Candidatoes.AddObject(candidato);
                        }
                        candidato.color = color;
                        rel.id_candidato = candidato.id;
                    }
                }
                else
                {
                    if (rel.Candidato.nombre.ToLower() != nombre.ToLower() || rel.Candidato.color != color)
                        if (candidato == null || rel.Candidato.nombre.ToLower() == nombre.ToLower())
                        {
                            //Edito candidato existente
                            rel.Candidato.nombre = nombre;
                            rel.Candidato.color = color;
                        }
                        else
                        {
                            db.Candidatoes.DeleteObject(rel.Candidato);
                            rel.id_candidato = candidato.id;
                        }
                }

                #endregion

                #region check partido

                Partido partidoObj = db.Partidoes.SingleOrDefault(p => p.nombre.ToLower().Trim() == partido.ToLower().Trim());
                //Chequeo si tengo que editar directamente al candidato o crear uno nuevo
                if (rel.Partido.RelacionCandidatoPartidoCoalicions.Count() > 1)
                {
                    //Creo nuevo candidato
                    if (rel.Partido.nombre != partido || rel.Partido.color != colorpartido)
                    {
                        if (partidoObj == null)
                        {
                            partidoObj = new Partido {nombre = partido};
                            db.Partidoes.AddObject(partidoObj);
                        }
                        partidoObj.color = colorpartido;
                        rel.id_partido = partidoObj.id;
                    }
                }
                else
                {
                    if (rel.Partido.nombre != partido || rel.Partido.color != colorpartido)
                    {
                        if (partidoObj == null || rel.Partido.nombre == partido)
                        {
                            rel.Partido.nombre = partido;
                            rel.Partido.color = colorpartido;
                        }
                        else
                        {
                            db.Partidoes.DeleteObject(rel.Partido);
                            rel.id_partido = partidoObj.id;
                        }
                    }
                }

                #endregion

                #region check coalicion

                //Chequeo si tengo que editar directamente al candidato o crear uno nuevo
                Coalicion coalicionObj = db.Coalicions.SingleOrDefault(c => c.nombre.ToLower() == coalicion.ToLower());

                if (rel.Coalicion.RelacionCandidatoPartidoCoalicions.Count() > 1)
                {
                    //Creo nuevo candidato
                    if (rel.Coalicion.nombre != coalicion || rel.Coalicion.color != colorcoalicion)
                    {
                        if (coalicionObj == null)
                        {
                            coalicionObj = new Coalicion {nombre = coalicion};
                            db.Coalicions.AddObject(coalicionObj);
                        }
                        coalicionObj.color = colorcoalicion;
                        rel.id_coalicion = coalicionObj.id;
                    }
                }
                else
                {
                    //Busco si el nuevo nombre tiene algun match
                    //Edito candidato existente
                    if (rel.Coalicion.nombre != coalicion || rel.Coalicion.color != colorcoalicion)
                    {
                        if (coalicionObj == null || rel.Coalicion.nombre == coalicion)
                        {
                            rel.Coalicion.nombre = coalicion;
                            rel.Coalicion.color = colorcoalicion;
                        }
                        else
                        {
                            db.Coalicions.DeleteObject(rel.Coalicion);
                            rel.id_coalicion = coalicionObj.id;
                        }
                    }
                }

                #endregion
            }
            db.SaveChanges();

            return "";
        }

        [Authorize]
        public JsonResult GetCandidatos()
        {
            var db = new edayRoomEntities();

            var partidos = from p in db.Partidoes
                           orderby p.nombre
                           select new { p.id, p.nombre, p.color };
            var coaliciones = from c in db.Coalicions
                              orderby c.nombre
                              select new {c.id, c.nombre,c.color};
            var candidato = from c in db.Candidatoes
                            orderby c.nombre
                            select new
                                       {
                                           c.id,
                                           c.nombre,
                                           c.color
                                       };
            return Json(new {candidato, partidos, coaliciones}, JsonRequestBehavior.AllowGet);
        }
    }
}