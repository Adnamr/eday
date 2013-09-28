using System;
using System.Collections.Generic;
using System.Web.Mvc;
using EdayRoom.API;
using EdayRoom.Models;
using System.Linq;
using Newtonsoft.Json;

namespace EdayRoom.Controllers
{
    public class DashboardController : Controller
    {
        //
        // GET: /Dashboard/
        [Authorize]
        public ActionResult Index()
        {
            var db = new edayRoomEntities();
            ViewData["estados"] = db.Centroes.Select(c => c.unidadGeografica1).Distinct().OrderBy(c => c).ToList();
            ViewData["municipios"] = db.Centroes.Select(c => c.unidadGeografica2).Distinct().ToList();
            ViewData["parroquias"] = db.Centroes.Select(c => c.unidadGeografica3).Distinct().ToList();
            return View("Dashboard");
        }
        public ActionResult Map()
        {
            return View("Map");
        }




        public JsonResult GetMunicipios(string states)
        {
            var states2 = "," + states + ",";
            var db = new edayRoomEntities();
            return Json(db.Centroes.Where(c => states2.Contains("," + c.unidadGeografica1 + ",")).Select(c => c.unidadGeografica2).
                Distinct().OrderBy(c => c), JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetParroquoias(string municipios)
        {
            var municipios2 = "," + municipios + ",";
            var db = new edayRoomEntities();
            return Json(db.Centroes.Where(c => municipios2.Contains("," + c.unidadGeografica2 + ",")).Select(c => c.unidadGeografica3).
                Distinct().OrderBy(c => c), JsonRequestBehavior.AllowGet);
        }
        public string GetParticipacionMovilizacionData(string tag1, string tag2, string estados, string municipios)
        {

            var db = new edayRoomEntities();

            var tag1Filter = string.IsNullOrWhiteSpace(tag1)?null:tag1;
            var tag2Filter = string.IsNullOrWhiteSpace(tag2)?null:tag2;
            var estadosFilter = string.IsNullOrWhiteSpace(estados)?null:estados;
            var municipiosFilter = string.IsNullOrWhiteSpace(municipios)?null:municipios;

            return JsonConvert.SerializeObject(db.GetParticipacionMovilizacion(null, null,
                tag1Filter,tag2Filter,estadosFilter,municipiosFilter,30));
        }
        public string GetIndicadores(string tag1, string tag2, string estados, string municipios)
        {

            var db = new edayRoomEntities();

            var tag1Filter = string.IsNullOrWhiteSpace(tag1) ? null : tag1;
            var tag2Filter = string.IsNullOrWhiteSpace(tag2) ? null : tag2;
            var estadosFilter = string.IsNullOrWhiteSpace(estados) ? null : estados;
            var municipiosFilter = string.IsNullOrWhiteSpace(municipios) ? null : municipios;

            var muestra = db.GetMetricasMuestra(tag1Filter, tag2Filter, estadosFilter, municipiosFilter).First();
            var generales = db.GetMetricasGenerales(tag1Filter, tag2Filter, estadosFilter, municipiosFilter).First();
            var alarmas = db.GetMetricasAlarmas(tag1Filter, tag2Filter, estadosFilter, municipiosFilter).First();
            
            var participacion = db.GetMetricasParticipacion(tag1Filter, tag2Filter, estadosFilter, municipiosFilter).First();
            var movilizacion = db.GetMetricasMovilizacion(tag1Filter, tag2Filter, estadosFilter, municipiosFilter).First();
            var totalizacion = db.GetMetricasTotalizacion(tag1Filter, tag2Filter, estadosFilter, municipiosFilter).First();
            return JsonConvert.SerializeObject(
                new
                    {
                       muestra,generales,alarmas, participacion, movilizacion, totalizacion

                    });
        }
        public string GetAlertas(string tag1, string tag2, string estados, string municipios)
        {

            var db = new edayRoomEntities();

            var tag1Filter = string.IsNullOrWhiteSpace(tag1) ? null : tag1;
            var tag2Filter = string.IsNullOrWhiteSpace(tag2) ? null : tag2;
            var estadosFilter = string.IsNullOrWhiteSpace(estados) ? null : estados;
            var municipiosFilter = string.IsNullOrWhiteSpace(municipios) ? null : municipios;

            var totales = db.GetAlertasTotales(tag1Filter, tag2Filter, estadosFilter, municipiosFilter);
            var activas = db.GetAlertasActivas(tag1Filter, tag2Filter, estadosFilter, municipiosFilter);
            var estadosl = db.GetAlertasPorEstados();
            return JsonConvert.SerializeObject(
                new { totales, activas, estadosl });
        }
        public string GetMatrizCalique(string tag1, string tag2, string estados, string municipios)
        {

            var db = new edayRoomEntities();

            var tag1Filter = string.IsNullOrWhiteSpace(tag1) ? null : tag1;
            var tag2Filter = string.IsNullOrWhiteSpace(tag2) ? null : tag2;
            var estadosFilter = string.IsNullOrWhiteSpace(estados) ? null : estados;
            var municipiosFilter = string.IsNullOrWhiteSpace(municipios) ? null : municipios;

            var matriz = db.GetMatrizCalique(tag1Filter, tag2Filter, estadosFilter, municipiosFilter);

            return JsonConvert.SerializeObject(new { matriz });
        }
        public string GetMatrizSustitucion(string tag1, string tag2, string estados, string municipios)
        {

            var db = new edayRoomEntities();

            var tag1Filter = string.IsNullOrWhiteSpace(tag1) ? null : tag1;
            var tag2Filter = string.IsNullOrWhiteSpace(tag2) ? null : tag2;
            var estadosFilter = string.IsNullOrWhiteSpace(estados) ? null : estados;
            var municipiosFilter = string.IsNullOrWhiteSpace(municipios) ? null : municipios;


            var matriz = from m in db.mudVsPsuvs
                         from c in db.Centroes
                         where m.id_centro == c.unique_id
                               && (tag1Filter == null || tag1Filter.Contains(c.tag1))
                               && (tag2Filter == null || tag2Filter.Contains(c.tag2))
                               && (estadosFilter == null || estadosFilter.Contains(c.unidadGeografica1))
                               && (municipiosFilter == null || municipiosFilter.Contains(c.unidadGeografica2))
                         select m;


            var totalMud2012 = matriz.Sum(m => m.mud2012);
            var totalPsuv2012 = matriz.Sum(m => m.psuv2012);
            var totalAbstencion2012 = matriz.Sum(m => m.abstencion2012);
            var total2012 = totalMud2012 + totalPsuv2012 + totalAbstencion2012;

            var totalMud = matriz.Sum(m => m.mud_actual ?? m.mud2012);
            var totalPsuv = matriz.Sum(m => m.psuv_actual ?? m.psuv2012);
            var totalAbstencion = matriz.Sum(m => m.abstencion ?? m.abstencion2012);
            var total = totalMud + totalPsuv + totalAbstencion;

            return JsonConvert.SerializeObject(new
            {
                totalMud2012 = totalMud2012 * 100.0 / total2012,
                totalPsuv2012 = totalPsuv2012 * 100.0 / total2012,
                totalAbstencion2012 = totalAbstencion2012 * 100.0 / total2012,
                totalMud = totalMud * 100.0 / total,
                totalPsuv = totalPsuv * 100.0 / total,
                totalAbstencion = totalAbstencion * 100.0 / total
            });
        }
        public ActionResult GetQuickCountPorMuestras()
        {
            var db = new edayRoomEntities();

            var results = new List<KeyValuePair<string,List<GetTotalizacion_Result1>>>();
            var avances = new List<KeyValuePair<string,GetMetricasQuickCount_Result>>();
            foreach(var m in db.MuestrasQuickCounts)
            {
                string val = m.value;
                var avance = db.GetMetricasQuickCount(val).First();
                avances.Add(new KeyValuePair<string, GetMetricasQuickCount_Result>(val, avance));
                var data = db.GetTotalizacion(null, null, null, null, null, null, true, val).ToList();
                results.Add(new KeyValuePair<string, List<GetTotalizacion_Result1>>(val,data));
            }

            return Json(new{results,avances}, JsonRequestBehavior.AllowGet);
        }
        public string GetAlertasActivasPorTendencia(string tag1, string tag2, string estados, string municipios)
        {

            var db = new edayRoomEntities();

            var tag1Filter = string.IsNullOrWhiteSpace(tag1) ? null : tag1;
            var tag2Filter = string.IsNullOrWhiteSpace(tag2) ? null : tag2;
            var estadosFilter = string.IsNullOrWhiteSpace(estados) ? null : estados;
            var municipiosFilter = string.IsNullOrWhiteSpace(municipios) ? null : municipios;


            return JsonConvert.SerializeObject(db.GetAlertasActivasPorTendencia(tag1Filter, tag2Filter, estadosFilter, municipiosFilter));
        }
        public string GetTotalizacionPorTendencia()
        {

            var db = new edayRoomEntities();
            //db.GetTotalizacionPorTendencia().First().`

            return JsonConvert.SerializeObject(db.GetTotalizacionPorTendencia());
        }
        public string GetProyeccionPorParticipacion(string tag1, string tag2, string estados, string municipios)
        {
            var db = new edayRoomEntities();
            var tag1Filter = string.IsNullOrWhiteSpace(tag1) ? null : tag1;
            var tag2Filter = string.IsNullOrWhiteSpace(tag2) ? null : tag2;
            var estadosFilter = string.IsNullOrWhiteSpace(estados) ? null : estados;
            var municipiosFilter = string.IsNullOrWhiteSpace(municipios) ? null : municipios;

            var result = db.getProyeccionFromHistorico(tag1Filter, tag2Filter, estadosFilter, municipiosFilter).ToList();

            var muestras = result.Select(r => new { r.id_muestra, r.nombre }).Distinct();
            var resultGlobal = result.GroupBy(r=> new{r.id_muestra, r.nombre}).Select(r=> 
                new{r.Key.id_muestra,
                    r.Key.nombre, 
                    capr = r.Sum(i=>i.proyCapriles), 
                    chav = r.Sum(i=>i.proyChavismo),
                    otro = r.Sum(i => i.proyOtros),
                    abstencion = r.Sum(i=>i.abstencion)
                });



            //var nacional = result

            return JsonConvert.SerializeObject(
                new{
                    muestras = muestras,
                    global = resultGlobal,
                    regional = result
                }
                
                );
        }
        public ActionResult Historic(int id)
        {
            return View();
        }
    
    }
}