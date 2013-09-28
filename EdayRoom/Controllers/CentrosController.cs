using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using EdayRoom.API;
using EdayRoom.API.DataTable;
using EdayRoom.Core.Centros;
using EdayRoom.Models;
using EdayRoom.Core;

namespace EdayRoom.Controllers
{
    public class CentrosController : Controller
    {
        //
        // GET: /Centros/
        private static readonly ProgressClass Progress = new ProgressClass();

        [Authorize(Roles = "admin")]
        public ActionResult Index()
        {
            return View("Setup");
        }

        [Authorize(Roles = "admin")]
        public string LoadCentros()
        {
            var fileUpload = Request.Files[0];
            if (fileUpload != null)
            {
                string filename = Server.MapPath("~/temp/" + fileUpload.FileName);
                fileUpload.SaveAs(filename);
                CentroDeVotacion.LoadBatch(filename);
                return new JavaScriptSerializer().Serialize(new {centros = 0, mesas = 0, testigos = 0});
            }
            return "";
        }

        [Authorize(Roles = "admin")]
        public string GetAllCentrosPaginate(int iDisplayStart, int iDisplayLength, int sEcho, int iSortCol_0,
                                            string sSortDir_0, string sSearch)
        {
            var dth = new DataTableHelper();
            var db = new edayRoomEntities();
            var centros = from c in db.Centroes 
                          orderby c.Nombre
                          select c;

            dth.iTotalRecords = centros.Count();

            #region Filtros

            sSearch = sSearch.ToLower();
            if (!string.IsNullOrWhiteSpace(sSearch))
            {
                centros = centros.Where(c =>
                                        c.unique_id.ToLower().Contains(sSearch) ||
                                        c.Nombre.ToLower().Contains(sSearch) ||
                                        c.unidadGeografica1.ToLower().Contains(sSearch) ||
                                        c.unidadGeografica2.ToLower().Contains(sSearch) ||
                                        c.unidadGeografica3.ToLower().Contains(sSearch) ||
                                        c.unidadGeografica4.ToLower().Contains(sSearch) ||
                                        c.unidadGeografica5.ToLower().Contains(sSearch) ||
                                        c.unidadGeografica6.ToLower().Contains(sSearch) ||
                                        c.unidadGeografica7.ToLower().Contains(sSearch) ||
                                        c.unidadGeografica8.ToLower().Contains(sSearch)).OrderBy(c=>c.Nombre);
            }

            #endregion

            dth.iTotalDisplayRecords = centros.Count();

            #region Sorting de registros

            switch (sSortDir_0)
            {
                case "asc":
                    switch (iSortCol_0)
                    {
                        case 0:
                            centros = centros.OrderBy(i => i.unique_id);
                            break;
                        case 1:
                            centros = centros.OrderBy(i => i.Nombre);
                            break;
                        case 2:
                            centros = centros.OrderBy(i => i.unidadGeografica1)
                                .ThenBy(i => i.unidadGeografica2)
                                .ThenBy(i => i.unidadGeografica3)
                                .ThenBy(i => i.unidadGeografica4)
                                .ThenBy(i => i.unidadGeografica5)
                                .ThenBy(i => i.unidadGeografica6)
                                .ThenBy(i => i.unidadGeografica7)
                                .ThenBy(i => i.unidadGeografica8);
                            break;
                        //case 3:
                        //    centros = centros.OrderBy(i => i.m.uniqueId);
                        //    break;
                        //case 4:
                        //    centros = centros.OrderBy(i => i.m.votantes);
                        //    break;
                        //case 5:
                        //    centros = centros.OrderBy(i => i.t1.nombre);
                        //    break;
                        //case 6:
                        //    centros = centros.OrderBy(i => i.t2.nombre);
                        //    break;
                    }
                    break;
                case "desc":
                    switch (iSortCol_0)
                    {
                        case 0:
                            centros = centros.OrderByDescending(i => i.unique_id);
                            break;

                        case 1:
                            centros = centros.OrderByDescending(i => i.Nombre);
                            break;
                        case 2:
                            centros = centros.OrderByDescending(i => i.unidadGeografica1)
                                .ThenByDescending(i => i.unidadGeografica2)
                                .ThenByDescending(i => i.unidadGeografica3)
                                .ThenByDescending(i => i.unidadGeografica4)
                                .ThenByDescending(i => i.unidadGeografica5)
                                .ThenByDescending(i => i.unidadGeografica6)
                                .ThenByDescending(i => i.unidadGeografica7)
                                .ThenByDescending(i => i.unidadGeografica8);
                            break;
                        //case 3:
                        //    centros = centros.OrderByDescending(i => i.m.uniqueId);
                        //    break;
                        //case 4:
                        //    centros = centros.OrderByDescending(i => i.m.votantes);
                        //    break;
                        //case 5:
                        //    centros = centros.OrderByDescending(i => i.t1.nombre);
                        //    break;
                        //case 6:
                        //    centros = centros.OrderByDescending(i => i.t2.nombre);
                        //    break;
                    }
                    break;
            }

            #endregion

            dth.sEcho = sEcho;

            var centroList = centros.ToList();

            dth.aaData = centroList.Select(c => new List<string>
                                                 {
                                                     c.unique_id,
                                                     string.Format("<a href='/Centros/Edit/{1}' target='_blank'>{0}</a>",c.Nombre,c.id),
                                                     (c.unidadGeografica1 ?? "") + " " +
                                                     (c.unidadGeografica2 ?? "") + " " +
                                                     (c.unidadGeografica3 ?? "") + " " +
                                                     (c.unidadGeografica4 ?? "") + " " +
                                                     (c.unidadGeografica5 ?? "") + " " +
                                                     (c.unidadGeografica6 ?? "") + " " +
                                                     (c.unidadGeografica7 ?? "") + " " +
                                                     (c.unidadGeografica8 ?? "")
                                                 }).Skip(iDisplayStart).Take(iDisplayLength).ToList();


            return
                new
                    JavaScriptSerializer().Serialize(dth);
        }

        [Authorize(Roles = "admin,supervisor")]
        public ActionResult Edit(int id)
        {
            var vc = new CentroDeVotacion(id);
            ViewData["centro"] = vc;
            return View("Edit");
        }

        [Authorize(Roles = "admin,supervisor")]
        public string EditTestigoMovilizacion(int tid, string name, string number)
        {
            var db = new edayRoomEntities();
            var testigo = db.Movilizadors.SingleOrDefault(m => m.id == tid);
            if(testigo != null)
            {
                testigo.nombre = name;
                testigo.numero = number;
                db.SaveChanges();
                return "Testigo editado";
            }
            return "Error";
        }

    }
}