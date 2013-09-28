using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using EdayRoom.Models;
using OfficeOpenXml;

namespace EdayRoom.Controllers
{
    public class ExcelController : Controller
    {

        public static DataTable Pivot(IDataReader dataValues, string keyColumn, string pivotNameColumn, string pivotValueColumn)
        {
            DataTable tmp = new DataTable();
            DataRow r;
            string LastKey = "//dummy//";
            int i, pValIndex, pNameIndex;                           
            string s;
            bool FirstRow = true;

            // Add non-pivot columns to the data table:

            pValIndex = dataValues.GetOrdinal(pivotValueColumn);
            pNameIndex = dataValues.GetOrdinal(pivotNameColumn);

            for (i = 0; i <= dataValues.FieldCount - 1; i++)
                if (i != pValIndex && i != pNameIndex)
                    tmp.Columns.Add(dataValues.GetName(i), dataValues.GetFieldType(i));

            r = tmp.NewRow();

            // now, fill up the table with the data:
            while (dataValues.Read())
            {
                // see if we need to start a new row
                if (dataValues[keyColumn].ToString() != LastKey)
                {
                    // if this isn't the very first row, we need to add the last one to the table
                    if (!FirstRow)
                        tmp.Rows.Add(r);
                    r = tmp.NewRow();
                    FirstRow = false;
                    // Add all non-pivot column values to the new row:
                    for (i = 0; i <= dataValues.FieldCount - 3; i++)
                        r[i] = dataValues[tmp.Columns[i].ColumnName];
                    LastKey = dataValues[keyColumn].ToString();
                }
                // assign the pivot values to the proper column; add new columns if needed:
                s = dataValues[pNameIndex].ToString();
                if (!tmp.Columns.Contains(s))
                    tmp.Columns.Add(s, dataValues.GetFieldType(pValIndex));
                r[s] = dataValues[pValIndex];
            }

            // add that final row to the datatable:
            tmp.Rows.Add(r);

            // Close the DataReader
            dataValues.Close();

            // and that's it!
            return tmp;
        }

        public ActionResult Centros()
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["dbConnSimple"].ConnectionString))
            {

                var da = new SqlDataAdapter("Select * from centro", conn);
                var dt = new DataTable();
                da.Fill(dt);
                var da2 = new SqlDataAdapter("Select * from mesa", conn);
                var dt2 = new DataTable();
                da2.Fill(dt2);
                var package = new ExcelPackage();

                package.Workbook.Worksheets.Add("Centros");
                var worksheet = package.Workbook.Worksheets[1];
                worksheet.Cells["A1"].LoadFromDataTable(dt, true);
                package.Workbook.Worksheets.Add("Mesas");
                var worksheet2 = package.Workbook.Worksheets[2];
                worksheet2.Cells["A1"].LoadFromDataTable(dt2, true);

                var stream = new MemoryStream();
                package.SaveAs(stream);

                string fileName = "centros" + DateTime.Now.ToString("yyyy.mm.dd.HH.MM.ss") + ".xlsx";
                const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                stream.Position = 0;
                return File(stream, contentType, fileName);
            }
        }

        public ActionResult ReporteAvance()
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["dbConnSimple"].ConnectionString))
            {
                var db = new edayRoomEntities();
                var metrics = db.GetGeneralMetricsForReport().First();
 
                
                var package = new ExcelPackage();
                



                package.Workbook.Worksheets.Add("E-day");
                package.Workbook.Worksheets.Add("Cruce");
                package.Workbook.Worksheets.Add("QC");
                package.Workbook.Worksheets.Add("Manuelita");

                var edayWorksheet = package.Workbook.Worksheets[1];

                edayWorksheet.Column(1).Width = 20;
                edayWorksheet.Column(2).Width = 43;
                edayWorksheet.Column(3).Width = 10;
                edayWorksheet.Column(4).Width = 5;

                edayWorksheet.Column(5).Width = 43;
                edayWorksheet.Column(6).Width = 10;

                edayWorksheet.Cells["B3"].Value = "Reporte por Hora";
                edayWorksheet.Cells["B3"].Style.Font.Bold = true;
                edayWorksheet.Cells["B3"].Style.Font.Size = 18;

                edayWorksheet.Cells["A1"].Value = DateTime.Now.ToString("dd/MM/yyyy hh:mm tt");

                edayWorksheet.Cells["B4"].Value = "Sala Situacional Integral 14-A";
                edayWorksheet.Cells["B4"].Style.Font.Bold = true;
                edayWorksheet.Cells["B4"].Style.Font.Size = 18;

                edayWorksheet.Cells["B6"].Value = "Indicadores generales";
                edayWorksheet.Cells["B6"].Style.Font.Bold = true;
                edayWorksheet.Cells["B6"].Style.Font.Size = 14;

                edayWorksheet.Cells["B8"].Value = "Porcentaje de contactabilidad";
                edayWorksheet.Cells["C8"].Value = Math.Round((decimal)metrics.mesasContactadas2hr, 2) + "%";
                edayWorksheet.Cells["B9"].Value = "Variación vs reporte previo";
                edayWorksheet.Cells["C9"].Value = "xx%";

                edayWorksheet.Cells["B11"].Value = "Mesas abiertas (sobre muestra)";
                edayWorksheet.Cells["C11"].Value = Math.Round((decimal)metrics.mesasAbiertas,2)+"%";

                edayWorksheet.Cells["B13"].Value = "Votantes contabilizados";
                edayWorksheet.Cells["C13"].Value = metrics.votantesContabilizados+"";
                edayWorksheet.Cells["B14"].Value = "Variación vs reporte previo";
                edayWorksheet.Cells["C14"].Value = "xx%";

                edayWorksheet.Cells["B16"].Value = "Proyección de votantes nacional";
                edayWorksheet.Cells["C16"].Value = metrics.proyeccionNacional+"";
                edayWorksheet.Cells["C16"].Style.Numberformat.Format = "#,#0.0";
                edayWorksheet.Cells["B17"].Value = "Variación vs reporte previo";
                edayWorksheet.Cells["C17"].Value = "xx%";


                edayWorksheet.Cells["B19"].Value = "Pareto de participación por tendencia";
                edayWorksheet.Cells["B20"].Value = "Centros opositores";
                edayWorksheet.Cells["C20"].Value = metrics.capriles+"%";
                edayWorksheet.Cells["B21"].Value = "Centros chavistas";
                edayWorksheet.Cells["C21"].Value = metrics.chavista + "%";
                edayWorksheet.Cells["B22"].Value = "Centros ni-ni";
                edayWorksheet.Cells["C22"].Value = metrics.nini + "%";

                edayWorksheet.Cells["B26"].Value = "Participación y Proyecciones (por estado)";
                edayWorksheet.Cells["B26"].Style.Font.Bold = true;
                edayWorksheet.Cells["B26"].Style.Font.Size = 14;
                

                //Inicio del calculo de Participacion y proyecciones
                var ugs = db.Centroes.Select(c => c.unidadGeografica1).Distinct();

                bool even = false;
                string colA, colB;
                int row = 28;
                foreach (var u in ugs) {
                    if(even){
                        colA = "E";
                        colB = "F";

                    }else{
                        colA = "B";
                        colB = "C";
                        
                    
                    }
                    even = !even;
                    var dataUG = db.GetMetricsReportForUG(u).First();

                    edayWorksheet.Cells[colA+row].Value = u;
                    edayWorksheet.Cells[colA+row].Style.Font.Bold = true;
                    edayWorksheet.Cells[colA+row].Style.Font.Size = 12;

                    edayWorksheet.Cells[colA+(row+2)].Value = "Mesas a contactar";
                    edayWorksheet.Cells[colB+(row+2)].Value = dataUG.mesas;

                    edayWorksheet.Cells[colA+(row+3)].Value = "Mesas contactadas";
                    edayWorksheet.Cells[colB+(row+3)].Value = dataUG.mesasContactadas;

                     edayWorksheet.Cells[colA+(row+5)].Value = "Votantes cuantificados";
                    edayWorksheet.Cells[colB+(row+5)].Value = dataUG.votantesCuantificados;
                     edayWorksheet.Cells[colA+(row+6)].Value = "Proyección de votantes regional";
                    edayWorksheet.Cells[colB+(row+6)].Value = dataUG.proyeccionRegional;
                     edayWorksheet.Cells[colA+(row+7)].Value = "Crecimiento vs reporte previo";
                    edayWorksheet.Cells[colB+(row+7)].Value = "xx%";

                     edayWorksheet.Cells[colA+(row+9)].Value = "Pareto de participación por tendencia";
                     edayWorksheet.Cells[colA+(row+10)].Value = "Centros opositores";
                    edayWorksheet.Cells[colB+(row+10)].Value = dataUG.capriles;
                     edayWorksheet.Cells[colA+(row+11)].Value = "Centros chavistas";
                    edayWorksheet.Cells[colB+(row+11)].Value = dataUG.chavista;
                     edayWorksheet.Cells[colA+(row+12)].Value = "Centros ni-ni";
                    edayWorksheet.Cells[colB+(row+12)].Value = dataUG.nini;

                     edayWorksheet.Cells[colA+(row+14)].Value = "Proyección de participación al cierre";
                    edayWorksheet.Cells[colB+(row+14)].Value = dataUG.proyeccionFin;
                     edayWorksheet.Cells[colA+(row+15)].Value = "Referencia 7O";
                    edayWorksheet.Cells[colB+(row+15)].Value = "xx%";
                     edayWorksheet.Cells[colA+(row+16)].Value = "Referencia 2007-2010";
                    edayWorksheet.Cells[colB+(row+16)].Value = "xx%";
                     edayWorksheet.Cells[colA+(row+17)].Value = "Participación óptima";
                    edayWorksheet.Cells[colB+(row+17)].Value = "xx%";







                    
                        if(!even){
                            row += 19;
                        }
                }

                var stream = new MemoryStream();
                package.SaveAs(stream);

                string fileName = "reporte" + DateTime.Now.ToString("yyyy.MM.dd.HH.mm.ss") + ".xlsx";
                const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                stream.Position = 0;
                return File(stream, contentType, fileName);
            }
        }
        public ActionResult ParticipacionMovilizacion()
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["dbConnSimple"].ConnectionString))
            {

                SqlCommand cmd = new SqlCommand();
                SqlDataAdapter da = new SqlDataAdapter();
                DataTable dt = new DataTable();
                cmd = new SqlCommand("GetParticipacionMovilizacionActual", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                da.SelectCommand = cmd;
                da.Fill(dt);
                var package = new ExcelPackage();

                package.Workbook.Worksheets.Add("Participacion y Movilizacion");
                var worksheet = package.Workbook.Worksheets[1];
                worksheet.Cells["A1"].LoadFromDataTable(dt, true);


                var da2 = new SqlDataAdapter(@"Select c.unique_id, m.uniqueid as id_mesa, m.votantes as votantes_en_mesa,  
                max(p.conteo) as participacion
                from mesa m, centro c, participacion p 
                where m.id = p.id_mesa and c.id = m.id_centro
		group by c.unique_id, m.uniqueid , m.votantes 
        order by c.unique_id, m.uniqueid , m.votantes ", conn);
                var dt2 = new DataTable();
                da2.Fill(dt2);
                package.Workbook.Worksheets.Add("Participacion por mesa");
                var worksheet2 = package.Workbook.Worksheets[2];
                worksheet2.Cells["A1"].LoadFromDataTable(dt2, true);


/*

                var da3 = new SqlDataAdapter(@"Select c.id, c.unique_id, c.unidadGeografica1 as estado,
c.unidadGeografica2 as municipio,
c.unidadGeografica3 as parroquia,
c.votantes as votantes_en_centro,
m.fecha, m.conteo as movilizacion
from movilizacion m, centro c
where c.id = m.id_centro
order by c.id, m.fecha", conn);
                var dt3 = new DataTable();
                da3.Fill(dt3);
                package.Workbook.Worksheets.Add("Movilizacion temporal");
                var worksheet3 = package.Workbook.Worksheets[3];
                worksheet3.Cells["A1"].LoadFromDataTable(dt3, true);
                */
                var stream = new MemoryStream();
                package.SaveAs(stream);

                string fileName = "participacion-movilizacion-" + DateTime.Now.ToString("yyyy.mm.dd.HH.MM.ss") + ".xlsx";
                const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                stream.Position = 0;
                return File(stream, contentType, fileName);
            }
        }

        //[OutputCache(Duration = 180, VaryByParam = "none")]
        public ActionResult Totalizacion()
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["dbConnSimple"].ConnectionString))
            {

                SqlDataAdapter da = new SqlDataAdapter(@"select c.id, c.unique_id, c.unidadGeografica1,
c.unidadGeografica2,
c.unidadGeografica3,
c.votantes as votantes_totales,
c.quickcountactive as conteo_rapido,c.qcGroup as grupos_conteo_rapido,
cc.nombre,
tpc.proyeccion
from totalizacionPorCandidato tpc 
join centro c  on tpc.id_centro = c.id 
join candidato cc On cc.nombre = tpc.nombre
order by c.id, cc.nombre
", conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                var package = new ExcelPackage();

                package.Workbook.Worksheets.Add("Totalizacion Proyectada");
                var worksheet = package.Workbook.Worksheets[1];
                worksheet.Cells["A1"].LoadFromDataTable(/*Pivot(*/dt/*.CreateDataReader(), "id", "nombre", "proyeccion")*/, true);


                var da2 = new SqlDataAdapter(@"select cc.unique_id as id_centro, m.uniqueid as id_mesa, c.nombre + ' ' + p.nombre as opcion, t.valor
from 
partido p,
candidato c,
relacioncandidatopartidocoalicion rcpc, 
centro cc, 
mesa m,
totalizacion t
where 
p.id = rcpc.id_partido and 
c.id = rcpc.id_candidato and
t.id_candidato = rcpc.id and
t.id_mesa = m.id and 
m.id_centro = cc.id", conn);
                var dt2 = new DataTable();



                da2.Fill(dt2);
                package.Workbook.Worksheets.Add("Totalizacion Por Mesa");
                var worksheet2 = package.Workbook.Worksheets[2];
                worksheet2.Cells["A1"].LoadFromDataTable(/*Pivot(*/dt2/*.CreateDataReader(), "id_mesa", "nombre", "votos")*/, true);


               

                var stream = new MemoryStream();
                package.SaveAs(stream);

                string fileName = "totalizacion-" + DateTime.Now.ToString("yyyy.mm.dd.HH.MM.ss") + ".xlsx";
                const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                stream.Position = 0;
                return File(stream, contentType, fileName);
            }
        }

        public ActionResult ProyeccionPorSustitucion()
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["dbConnSimple"].ConnectionString))
            {

                var da = new SqlDataAdapter(@"select * from mudvspsuv", conn);
                var dt = new DataTable();
                da.Fill(dt);
                var package = new ExcelPackage();

                package.Workbook.Worksheets.Add("Proyección por Sustitución");
                var worksheet = package.Workbook.Worksheets[1];
                worksheet.Cells["A1"].LoadFromDataTable(dt, true);


                var stream = new MemoryStream();
                package.SaveAs(stream);

                string fileName = "proyeccion_por_sustuticion-" + DateTime.Now.ToString("yyyy.mm.dd.HH.MM.ss") + ".xlsx";
                const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                stream.Position = 0;
                return File(stream, contentType, fileName);
            }
        }

    }
}
