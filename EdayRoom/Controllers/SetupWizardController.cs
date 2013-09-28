using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using EdayRoom.API;
using EdayRoom.Security;
using EdayRoom.Models;
using OfficeOpenXml;

namespace EdayRoom.Controllers
{
    public class SetupWizardController : Controller
    {
        [Authorize]
        public ActionResult Index()
        {
            return RedirectToAction("UsersStep");
        }

        #region USER SETUP STEP

        [Authorize]
        public ActionResult UsersStep()
        {
            var db = new edayRoomEntities();
            IOrderedQueryable<UserFile> userFiles = from uf in db.UserFiles
                                                    orderby uf.fileName descending
                                                    select uf;
            ViewData["userFiles"] = userFiles.ToList();
            return View("UsersStep");
        }

        [Authorize]
        [HttpPost]
        public ActionResult SetupUsers()
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
                ExcelWorksheet worksheet = package.Workbook.Worksheets[1];
                worksheet.Cells[1, 3].Value = "usuario";
                worksheet.Cells[1, 4].Value = "password";
                for (int i = 2; i <= worksheet.Dimension.End.Row; i++)
                {
                    var nombre = (string) worksheet.Cells[i, 1].Value;
                    var apellido = (string) worksheet.Cells[i, 2].Value;
                    var u = new EdayRoomUser
                                {
                                    Name =
                                        nombre + " " +
                                        apellido,
                                    Password = PasswordManagement.GenerateRandomPassword(6),
                                };
                    u.Username = u.Name.Replace(" ", ".").ToLower();
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
                    string passwordHash = PasswordManagement.GeneratePasswordHash(u.Password, out salt);

                    db.users.AddObject(new user
                                           {
                                               nombre = nombre,
                                               apellido = apellido,
                                               username = u.Username,
                                               salt = salt,
                                               hash = passwordHash
                                           });
                }

                db.UserFiles.AddObject(new UserFile
                                           {
                                               fileName = fiOut.Name
                                           });

                db.SaveChanges();

                package.SaveAs(fiOut);
            }
            var errores = new List<string> {"error " + fileUpload.FileName};

            return GetAllUsers(errores, users);
        }

        [Authorize]
        public ActionResult GetAllUsers(List<string> errores = null, List<EdayRoomUser> users = null)
        {
            ViewData["errores"] = errores;
            ViewData["users"] = users;
            return View("GetAllUsers");
        }

        [Authorize]
        public string GetAllUserFiles()
        {
            var db = new edayRoomEntities();
            IOrderedQueryable<UserFile> userFiles = from uf in db.UserFiles
                                                    orderby uf.fileName descending
                                                    select uf;
            return new JavaScriptSerializer().Serialize(userFiles);
        }

        #endregion
    }
}