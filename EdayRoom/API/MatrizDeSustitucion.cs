using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using EdayRoom.Models;

namespace EdayRoom.API
{
    public class MatrizDeSustitucion
    {
        public static void UpdateMatriz(int idCentro)
        {
            var db = new edayRoomEntities();
            var centro = db.Centroes.Single(c => c.id == idCentro);
            var matriz = db.mudVsPsuvs.Single(c => c.id_centro == centro.unique_id);

            var mesas = (from m in db.Mesas
                         where m.id_centro == centro.id
                               && m.Totalizacions.Any()
                         select m);

            var votantesContados = mesas.Any()?mesas.Sum(m => m.votantes):0;
            var totales = (from m in db.Mesas
                          from t in db.Totalizacions
                          where m.id_centro == centro.id &&
                                t.id_mesa == m.id
                          select t).GroupBy(t=>t.RelacionCandidatoPartidoCoalicion.Candidato.nombre).
                          Select(k=>new{k.Key,valor = k.Sum(m=>m.valor)});
            int totalCapriles = totales.Any() ? totales.Single(t => t.Key.Contains("Capriles")).valor:0;
            int totalChavez = totales.Any() ? totales.Single(t => t.Key.Contains("Jaua")).valor:0;
            int abstencion = 0;
            totalCapriles = totalCapriles * (centro.votantes??0) / votantesContados;
            totalChavez = totalChavez*(centro.votantes??0)/votantesContados;
            abstencion = (centro.votantes ?? 0) - (totalCapriles + totalChavez);

            matriz.mud_actual = totalCapriles;
            matriz.psuv_actual = totalChavez;
            matriz.abstencion = abstencion;
            db.SaveChanges();
        }
    }
}