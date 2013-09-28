using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using EdayRoom.API;
using EdayRoom.API.DataTable;
using EdayRoom.Security;
using EdayRoom.Models;
using OfficeOpenXml;

namespace EdayRoom.Controllers
{
    public class UsuariosController : Controller
    {
        //
        // GET: /Usuarios/

        [Authorize]
        public ActionResult Index()
        {
            return View();
        }

        [Authorize]
        public ActionResult Setup()
        {
            return View("SetupUsuarios");
        }

        [Authorize]
        public string GetAllUsuariosPaginate(int iDisplayStart, int iDisplayLength, int sEcho, int iSortCol_0,
                                             string sSortDir_0, string sSearch)
        {
            var dth = new DataTableHelper();
            var db = new edayRoomEntities();
            IQueryable<user> usuarios = from u in db.users
                                        select u;

            dth.iTotalRecords = usuarios.Count();

            #region Filtros

            sSearch = sSearch.ToLower();
            if (!string.IsNullOrWhiteSpace(sSearch))
            {
                usuarios = usuarios.Where(u =>
                                          u.username.ToLower().Contains(sSearch) ||
                                          u.nombre.ToLower().Contains(sSearch) ||
                                          u.apellido.ToLower().Contains(sSearch));
            }

            #endregion

            dth.iTotalDisplayRecords = usuarios.Count();
            usuarios = usuarios.OrderBy(u => u.username);

            #region Sorting de registros

            switch (sSortDir_0)
            {
                case "asc":
                    switch (iSortCol_0)
                    {
                        case 0:
                            usuarios = usuarios.OrderBy(u => u.username);
                            break;
                        case 1:
                            usuarios = usuarios.OrderBy(u => u.nombre);
                            break;
                        case 2:
                            usuarios = usuarios.OrderBy(u => u.apellido);
                            break;
                        case 3:
                            usuarios = usuarios.OrderBy(u => u.grupo);
                            break;
                        case 4:
                            usuarios = usuarios.OrderBy(u => u.admin);
                            break;
                        case 5:
                            usuarios = usuarios.OrderBy(u => u.participacion);
                            break;
                        case 6:
                            usuarios = usuarios.OrderBy(u => u.movilizacion);
                            break;
                        case 7:
                            usuarios = usuarios.OrderBy(u => u.exitpolls);
                            break;
                        case 8:
                            usuarios = usuarios.OrderBy(u => u.quickcount);
                            break;
                        case 9:
                            usuarios = usuarios.OrderBy(u => u.totalizacion);
                            break;
                    }
                    break;
                case "desc":
                    switch (iSortCol_0)
                    {
                        case 0:
                            usuarios = usuarios.OrderByDescending(u => u.username);
                            break;
                        case 1:
                            usuarios = usuarios.OrderByDescending(u => u.nombre);
                            break;
                        case 2:
                            usuarios = usuarios.OrderByDescending(u => u.apellido);
                            break;
                        case 3:
                            usuarios = usuarios.OrderByDescending(u => u.grupo);
                            break;
                        case 4:
                            usuarios = usuarios.OrderByDescending(u => u.admin);
                            break;
                        case 5:
                            usuarios = usuarios.OrderByDescending(u => u.participacion);
                            break;
                        case 6:
                            usuarios = usuarios.OrderByDescending(u => u.movilizacion);
                            break;
                        case 7:
                            usuarios = usuarios.OrderByDescending(u => u.exitpolls);
                            break;
                        case 8:
                            usuarios = usuarios.OrderByDescending(u => u.quickcount);
                            break;
                        case 9:
                            usuarios = usuarios.OrderByDescending(u => u.totalizacion);
                            break;
                    }
                    break;
            }

            #endregion

            dth.sEcho = sEcho;

            var ulist = usuarios.Select(u => new
                                                 {
                                                     u.username,
                                                     u.nombre,
                                                     u.apellido,
                                                     u.grupo,
                                                     admin = u.admin ? "si" : "no",
                                                     leader = u.leader ? "si" : "no",
                                                     participacion = u.participacion ? "si" : "no",
                                                     movilizacion = u.movilizacion ? "si" : "no",
                                                     exitpolls = u.exitpolls ? "si" : "no",
                                                     quickcount = u.quickcount ? "si" : "no",
                                                     totalizacion = u.totalizacion ? "si" : "no",
                                                     dashboard = u.dashboard ? "si" : "no"
                                                 }).ToList();
            List<List<string>> users = ulist.Select(u => new List<string>
                                                             {
                                                                 u.username,
                                                                 u.nombre,
                                                                 u.apellido,
                                                                 u.grupo,
                                                                 u.admin,
                                                                 u.leader,
                                                                 u.participacion,
                                                                 u.movilizacion,
                                                                 u.exitpolls,
                                                                 u.quickcount,
                                                                 u.totalizacion,
                                                                 u.dashboard
                                                             }).ToList();
            dth.aaData = users.Skip(iDisplayStart).Take(iDisplayLength).ToList();


            return new JavaScriptSerializer().Serialize(dth);
        }

        [Authorize]
        public string LoadUsuarios()
        {
            HttpPostedFileBase fileUpload = Request.Files[0];
            var users = new List<EdayRoomUser>();

            if (fileUpload != null)
            {
                fileUpload.SaveAs(Server.MapPath("~/temp/" + fileUpload.FileName));
                var fi = new FileInfo(Server.MapPath("~/temp/" + fileUpload.FileName));
                var fiOut = new FileInfo(
                    Path.Combine(
                        Server.MapPath(ConfigurationManager.AppSettings["userFilesDirectory"]),
                        string.Format("{0}_" + fileUpload.FileName,
                                      DateTime.Now.ToString("yyyy_MM_dd_hh_mm_ss"))));
                var db = new edayRoomEntities();
                List<string> existingUsers = (from u in db.users
                                              select u.username).ToList();
                var package = new ExcelPackage(fi);
                var passwordPackage = new ExcelPackage();

                ExcelWorksheet worksheet = package.Workbook.Worksheets[1];
                passwordPackage.Workbook.Worksheets.Add("Passwords");
                ExcelWorksheet passwordWorksheet = passwordPackage.Workbook.Worksheets["Passwords"];
                passwordWorksheet.Cells[1, 1].Value = "Nombre";
                passwordWorksheet.Cells[1, 3].Value = "Apellido";
                passwordWorksheet.Cells[1, 3].Value = "usuario";
                passwordWorksheet.Cells[1, 4].Value = "password";

                for (int i = 2; i <= worksheet.Dimension.End.Row; i++)
                {
                    var nombre = (string) worksheet.Cells[i, 1].Value;
                    string apellido = worksheet.Cells[i, 2].Value == null ? "" : worksheet.Cells[i, 2].Value.ToString();
                    string grupo = (string) worksheet.Cells[i, 3].Value ?? "";
                    bool admin = ((string) worksheet.Cells[i, 4].Value).ToLower() == "si";
                    bool supervisor = ((string)worksheet.Cells[i, 5].Value).ToLower() == "si";
                    bool leader = ((string)worksheet.Cells[i, 6].Value).ToLower() == "si";
                    bool participacion = ((string) worksheet.Cells[i, 7].Value).ToLower() == "si";
                    bool movilizacion = ((string) worksheet.Cells[i, 8].Value).ToLower() == "si";
                    bool exitpolls = ((string) worksheet.Cells[i, 9].Value).ToLower() == "si";
                    bool quickcount = ((string) worksheet.Cells[i, 10].Value).ToLower() == "si";
                    bool totalizacion = ((string) worksheet.Cells[i, 11].Value).ToLower() == "si";
                    bool dashboard = ((string)worksheet.Cells[i, 12].Value).ToLower() == "si";
                    bool alertas = ((string)worksheet.Cells[i, 13].Value).ToLower() == "si";
                    var password = (worksheet.Cells[i, 14].Value == null ? "" : worksheet.Cells[i, 14].Value.ToString());
                    var u = new EdayRoomUser
                                {
                                    Name = nombre + " " + apellido,
                                    Password = 
                                        string.IsNullOrWhiteSpace(password)
                                            ? PasswordManagement.GenerateRandomPassword(6)
                                            : password,
                                    Group = grupo,
                                    Admin = admin,
                                    Leader = leader,
                                    Participacion = participacion,
                                    Movilizacion = movilizacion,
                                    ExitPolls = exitpolls,
                                    QuickCount = quickcount,
                                    Totalizacion = totalizacion,
                                    Dashboard = dashboard
                                };
                    u.Username = nombre.Trim().Split(' ').First().ToLower() + "." + apellido.Trim().Split(' ').First().ToLower();
                    byte[] b = Encoding.GetEncoding(1251).GetBytes(u.Username);
                    u.Username = Encoding.ASCII.GetString(b);
                    int uid = 1;
                    if (existingUsers.Any(us => us == u.Username))
                    {
                        string original = u.Username;
                        while (true)
                        {
                            u.Username = original + "." + uid;
                            if (existingUsers.All(us => us != u.Username))
                            {
                                break;
                            }
                            uid++;
                        }
                    }
                    existingUsers.Add(u.Username);
                    worksheet.Cells[i, 3].Value = u.Username;
                    worksheet.Cells[i, 4].Value = u.Password;
                    users.Add(u);
                    string salt = "";
                    string passwordHash = u.Password;//PasswordManagement.GeneratePasswordHash(u.Password, out salt);

                    db.users.AddObject(new user
                                           {
                                               nombre = nombre,
                                               apellido = apellido,
                                               username = u.Username,
                                               salt = salt,
                                               hash = passwordHash,
                                               grupo = grupo,
                                               admin = admin,
                                               leader = leader,
                                               participacion = participacion,
                                               movilizacion = movilizacion,
                                               exitpolls = exitpolls,
                                               quickcount = quickcount,
                                               totalizacion = totalizacion,
                                               dashboard = dashboard,
                                               supervisor = supervisor,
                                               alertas = alertas
                                           });
                }

                db.UserFiles.AddObject(new UserFile
                                           {
                                               fileName = fiOut.Name
                                           });

                db.SaveChanges();

                package.SaveAs(fiOut);


                var errores = new List<string> {"error " + fileUpload.FileName};
            }
            return "YEAH";
        }
    }
}