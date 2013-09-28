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

namespace EdayRoom.Controllers
{
    public class TestigosController : Controller
    {
        //
        // GET: /Testigos/

        [Authorize]
        public ActionResult Index()
        {
            return View("Setup");
        }

        [Authorize]
        public string LoadTestigos(string type)
        {
            HttpPostedFileBase fileUpload = Request.Files[0];

            if (fileUpload != null)
            {
                fileUpload.SaveAs(Server.MapPath("~/temp/" + fileUpload.FileName));
                var fi = new FileInfo(Server.MapPath("~/temp/" + fileUpload.FileName));
                var package = new ExcelPackage(fi);
                ExcelWorksheet worksheet = package.Workbook.Worksheets[1];

                var db = new edayRoomEntities();
                var centros = (db.Centroes.Select(cc => new { cc.id, cc.unique_id })).ToDictionary(t => t.unique_id);
                var mesas = (db.Mesas.Select(cc => new { cc.id, cc.uniqueId })).ToDictionary(t => t.uniqueId);

                var testigosParticipacion = new List<Testigo>();
                
                  Dictionary<int, int> existingTestigos =
                                    db.Testigoes.Select(t => t.id_mesa).Distinct().ToDictionary(t => t);
                                var movilizador = new List<Movilizador>();
                                Dictionary<int?, int?> existingMovilizacion =
                                    db.Movilizadors.Select(t => t.id_centro).Distinct().ToDictionary(t => t);
                                var testigosExitPoll = new List<TestigoExitPoll>();
                                Dictionary<int?, int?> existingTestigosExitpoll =
                                    db.TestigoExitPolls.Select(t => t.id_centro).Distinct().ToDictionary(t => t);
   
                               for (int i = 2; i < worksheet.Dimension.End.Row; i++)
                                {
                                    string nombre = worksheet.Cells[i, 1].Value == null ? "-" : worksheet.Cells[i, 1].Value.ToString();
                                    string numero = worksheet.Cells[i, 2].Value == null ? "-" : worksheet.Cells[i, 2].Value.ToString();
                                    string id = worksheet.Cells[i, 3].Value == null ? "-" : worksheet.Cells[i, 3].Value.ToString();

                                    bool exists = false;
                                    switch (type)
                                    {
                                        case "participacion":
                                            if (mesas.ContainsKey(id))
                                            {
                                
                                                exists = existingTestigos.ContainsKey(mesas[id].id);
                                                if (!exists)
                                                {
                                                    existingTestigos.Add(mesas[id].id, 0);
                                                }
                                                testigosParticipacion.Add(new Testigo
                                                {
                                                    nombre = nombre,
                                                    numero = numero,
                                                    activo = !exists,
                                                    id_mesa = mesas[id].id
                                                });
                                            }
                                            break;
                                        case "movilizacion":
                                            if (centros.ContainsKey(id))
                                            {
                                                exists = existingMovilizacion.ContainsKey(centros[id].id);
                                                if (!exists)
                                                {
                                                    existingMovilizacion.Add(centros[id].id, 0);
                                                }

                                                movilizador.Add(new Movilizador
                                                {
                                                    nombre = nombre,
                                                    numero = numero,
                                                    activo = !exists,
                                                    id_centro = centros[id].id
                                                });
                                            }


                                            break;
                                        case "exitpoll":
                                            if (centros.ContainsKey(id))
                                            {
                                                exists = existingTestigosExitpoll.ContainsKey(centros[id].id);
                                                if (!exists)
                                                {
                                                    existingTestigosExitpoll.Add(centros[id].id, 0);
                                                }
                                                testigosExitPoll.Add(new TestigoExitPoll
                                                {
                                                    nombre = nombre,
                                                    numero = numero,
                                                    activo = !exists,
                                                    id_centro = centros[id].id
                                                });
                                            }


                                            break;
                                    }
                                }

                using (
                    var con = new SqlConnection(ConfigurationManager.ConnectionStrings["dbConnSimple"].ConnectionString)
                    )
                {
                    con.Open();
                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        string table = "";
                        switch (type)
                        {
                            case "participacion":
                                table = "testigo";
                                break;
                            case "movilizacion":
                                table = "movilizador";
                                break;
                            case "exitpoll":
                                table = "testigoexitpoll";
                                break;
                        }

                        var bc = new SqlBulkCopy(con,
                                                 SqlBulkCopyOptions.CheckConstraints |
                                                 SqlBulkCopyOptions.FireTriggers |
                                                 SqlBulkCopyOptions.KeepNulls, tran) { BatchSize = 1000, DestinationTableName = table };
                        switch (type)
                        {
                            case "participacion":
                                bc.WriteToServer(testigosParticipacion.AsDataReader());
                                break;
                            case "movilizacion":
                                bc.WriteToServer(movilizador.AsDataReader());
                                break;
                            case "exitpoll":
                                bc.WriteToServer(testigosExitPoll.AsDataReader());
                                break;
                        }

                        tran.Commit();
                    }
                    con.Close();
                }

                return new JavaScriptSerializer().Serialize("");
                
            }
                  
            return "";
        }


        [Authorize]
        public string GetTestigosParticipacion(int iDisplayStart, int iDisplayLength, int sEcho, int iSortCol_0,
                                            string sSortDir_0, string id_centro)
        {
            var dth = new DataTableHelper();
            var db = new edayRoomEntities();
            var centros =   from c in db.Centroes
                     from t in db.PadronElectoralParticipacions
                     where c.unique_id == t.id_centro &&
                            id_centro == c.unique_id
                     select new {c, t};

            dth.iTotalRecords = centros.Count();

            dth.iTotalDisplayRecords = centros.Count();

           dth.sEcho = sEcho;

            var centroList = centros.ToList();

            dth.aaData = centroList.Select(c => new List<string>
                                                 {
                                                     c.c.unique_id,
                                                     c.c.Nombre,
                                                     c.c.unidadGeografica1,
                                                     c.c.unidadGeografica2,
                                                     c.c.unidadGeografica3,
                                                     c.t.id_mesa,
                                                     c.t.nombre,
                                                     c.t.telefono
                                                }).Skip(iDisplayStart).Take(iDisplayLength).ToList();


            return
                new
                    JavaScriptSerializer().Serialize(dth);
        }
        [Authorize]
        public ActionResult ListaTestigosParticipacion(string sSearch)
        {
                
            return View();
        }


    }
}