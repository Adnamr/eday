using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using EdayRoom.API.DataReader;
using EdayRoom.API.DataTable;
using EdayRoom.Models;
using OfficeOpenXml;
using System;

namespace EdayRoom.Controllers
{
    public class MesasController : Controller
    {
        [Authorize]
        public ActionResult Index()
        {
            return View("Setup");
        }

        [Authorize(Roles = "admin,supervisor")]
        public string LoadMesas()
        {
            HttpPostedFileBase fileUpload = Request.Files[0];

            if (fileUpload != null)
            {
                fileUpload.SaveAs(Server.MapPath("~/temp/" + fileUpload.FileName));
                var fi = new FileInfo(Server.MapPath("~/temp/" + fileUpload.FileName));
                var package = new ExcelPackage(fi);
                ExcelWorksheet worksheet = package.Workbook.Worksheets[1];

                var db = new edayRoomEntities();
                var centros = (db.Centroes.Select(cc => new {cc.id, cc.unique_id})).ToDictionary(t => t.unique_id);

                var mesas = new List<Mesa>();
                for (int i = 2; i <= worksheet.Dimension.End.Row; i++)
                {
                    double numero = worksheet.Cells[i, 2].Value == null ? 0 : (double) worksheet.Cells[i, 2].Value;
                    double votantes = worksheet.Cells[i, 3].Value == null ? 0 : (double) worksheet.Cells[i, 3].Value;
                    string centroId = worksheet.Cells[i, 1].Value == null ? "" : worksheet.Cells[i, 1].Value.ToString();

                    if (centros.ContainsKey(centroId))
                    {
                        int idcentro = centros[centroId].id;
                        var mesa = new Mesa
                                       {
                                           uniqueId = centroId + "-" + numero,
                                           numero = (int) numero,
                                           votantes = (int) votantes,
                                           id_centro = idcentro,
                                           lastContact = DateTime.Now
                                       };
                        mesas.Add(mesa);
                    }
                }

                using (
                    var con = new SqlConnection(ConfigurationManager.ConnectionStrings["dbConnSimple"].ConnectionString)
                    )
                {
                    con.Open();
                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        var bc = new SqlBulkCopy(con,
                                                 SqlBulkCopyOptions.CheckConstraints |
                                                 SqlBulkCopyOptions.FireTriggers |
                                                 SqlBulkCopyOptions.KeepNulls, tran)
                                     {BatchSize = 1000, DestinationTableName = "mesa"};

                        bc.WriteToServer(mesas.AsDataReader());

                        tran.Commit();
                    }
                    con.Close();
                }

                return new JavaScriptSerializer().Serialize("");
            }
            return "";
        }

        [Authorize(Roles = "admin,supervisor")]
        public ActionResult Edit(int id)
        {
            ViewData["mesa"] = new EdayRoom.Core.Mesa(id);
            return View("Edit");
        }

        [Authorize(Roles = "admin,supervisor")]
        public string EditTestigoParticipacion(int tid, string name, string number)
        {
            var db = new edayRoomEntities();
            var testigo = db.Testigoes.SingleOrDefault(t => t.id == tid);

            if (testigo != null)
            {
                testigo.numero = number;
                testigo.nombre = name;
                db.SaveChanges();
                return "Testigo Editado";
            }
            return "Falla editando";
        }

        [Authorize(Roles = "admin,supervisor")]
        public string GetAllMesasPaginate(int iDisplayStart, int iDisplayLength, int sEcho, int iSortCol_0,
                                            string sSortDir_0, string sSearch)
        {
            var dth = new DataTableHelper();
            var db = new edayRoomEntities();
            var mesas = from c in db.Centroes
                        from m in db.Mesas
                        where m.id_centro == c.id
                        orderby c.Nombre
                        select new{c, m};

            dth.iTotalRecords = mesas.Count();

            #region Filtros

            sSearch = sSearch.ToLower();
            if (!string.IsNullOrWhiteSpace(sSearch))
            {
                mesas = mesas.Where(c =>
                                    c.c.unique_id.ToLower().Contains(sSearch) ||
                                    c.m.uniqueId.ToLower().Contains(sSearch) ||
                                    c.c.Nombre.ToLower().Contains(sSearch) ||
                                    c.c.unidadGeografica1.ToLower().Contains(sSearch) ||
                                    c.c.unidadGeografica2.ToLower().Contains(sSearch) ||
                                    c.c.unidadGeografica3.ToLower().Contains(sSearch));
            }

            #endregion

            dth.iTotalDisplayRecords = mesas.Count();

            #region Sorting de registros

            switch (sSortDir_0)
            {
                case "asc":
                    switch (iSortCol_0)
                    {
                        case 0:
                            mesas = mesas.OrderBy(c => c.c.unique_id);
                            break;
                        case 1:
                            mesas = mesas.OrderBy(c => c.c.Nombre);
                            break;
                        case 2:
                            mesas = mesas.OrderBy(c => c.c.unidadGeografica1)
                                .ThenBy(c => c.c.unidadGeografica2)
                                .ThenBy(c => c.c.unidadGeografica3)
                                .ThenBy(c => c.c.unidadGeografica4)
                                .ThenBy(c => c.c.unidadGeografica5)
                                .ThenBy(c => c.c.unidadGeografica6)
                                .ThenBy(c => c.c.unidadGeografica7)
                                .ThenBy(c => c.c.unidadGeografica8);
                            break;

                    }
                    break;
                case "desc":
                    switch (iSortCol_0)
                    {
                        case 0:
                            mesas = mesas.OrderByDescending(c => c.c.unique_id);
                            break;

                        case 1:
                            mesas = mesas.OrderByDescending(c => c.c.Nombre);
                            break;
                        case 2:
                            mesas = mesas.OrderByDescending(c => c.c.unidadGeografica1)
                                .ThenByDescending(c => c.c.unidadGeografica2)
                                .ThenByDescending(c => c.c.unidadGeografica3)
                                .ThenByDescending(c => c.c.unidadGeografica4)
                                .ThenByDescending(c => c.c.unidadGeografica5)
                                .ThenByDescending(c => c.c.unidadGeografica6)
                                .ThenByDescending(c => c.c.unidadGeografica7)
                                .ThenByDescending(c => c.c.unidadGeografica8);
                            break;
                    }
                    break;
            }

            #endregion

            dth.sEcho = sEcho;

            var mesasList = mesas.ToList();

            dth.aaData = mesasList.Select(c => new List<string>
                                                 {
                                                     c.c.unique_id,
                                                     string.Format("<a href='/Centros/Edit/{1}' target='_blank'>{0}</a>",c.c.Nombre,c.c.id),
                                                     (c.c.unidadGeografica1 ?? "") + " " +
                                                     (c.c.unidadGeografica2 ?? "") + " " +
                                                     (c.c.unidadGeografica3 ?? "") + " " +
                                                     (c.c.unidadGeografica4 ?? "") + " " +
                                                     (c.c.unidadGeografica5 ?? "") + " " +
                                                     (c.c.unidadGeografica6 ?? "") + " " +
                                                     (c.c.unidadGeografica7 ?? "") + " " +
                                                     (c.c.unidadGeografica8 ?? ""),
                                                     string.Format("<a href='/Mesas/Edit/{1}' target='_blank'>{0}</a>",c.m.uniqueId,c.m.id),
                                                     c.m.numero.ToString(),
                                                     c.m.votantes.ToString()

                                                 }).Skip(iDisplayStart).Take(iDisplayLength).ToList();


            return
                new
                    JavaScriptSerializer().Serialize(dth);
        }

    }
}