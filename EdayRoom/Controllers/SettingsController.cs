using System.Data;
using System.Linq;
using System.Web.Mvc;
using EdayRoom.Models;

namespace EdayRoom.Controllers
{
    public class SettingsController : Controller
    {
        //
        // GET: /Settings/

        [Authorize]
        public ActionResult Index()
        {
            using (var db = new edayRoomEntities())
            {
                return View("Settings", db.Settings.First());
            }
        }

        [Authorize]
        public ActionResult UpdateSettings(Setting setting)
        {
            using (var db = new edayRoomEntities())
            {
                db.Settings.Attach(setting);
                db.ObjectStateManager.ChangeObjectState(setting, EntityState.Modified);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}