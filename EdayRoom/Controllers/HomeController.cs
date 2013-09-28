using System;
using System.Linq;
using System.Web.Mvc;
using EdayRoom.Models;

namespace EdayRoom.Controllers
{
    public class HomeController : Controller
    {
        [Authorize]
        public ActionResult Index()
        {
            var db = new edayRoomEntities();

            user user = db.users.SingleOrDefault(u => u.username == User.Identity.Name);

            if (user.dashboard)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            if (user.participacion)
            {
                return RedirectToAction("Index", "Participacion");
            }

            if (user.movilizacion)
            {
                return RedirectToAction("Index", "Movilizacion");
            }
            if (user.exitpolls)
            {
                return RedirectToAction("Index", "ExitPolls");
            }
            if (user.quickcount)
            {
                return RedirectToAction("Index", "QuickCount");
            }
            if (user.totalizacion)
            {
                return RedirectToAction("Index", "Totalizacion");
            }
            if (user.alertas)
            {
                return RedirectToAction("ListAlertas", "Alertas");
            }
            throw new Exception("No actions available for this user");
        }
    }
}