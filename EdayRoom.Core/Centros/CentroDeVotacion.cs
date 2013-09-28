using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using EdayRoom.API.DataReader;
using EdayRoom.Models;
using OfficeOpenXml;

namespace EdayRoom.Core.Centros
{
    public class CentroDeVotacion
    {
        public int Id { get; private set; }
        public string Nombre { get; set; }
        public string UniqueId { get; set; }
        public UbicacionGeografica Ubicacion { get; set; }
        public int Votantes { get; set; }
        public int CantidadMesas { get; set; }
        public int Cuadernos { get; set; }
        public string Grupo;
        public string GrupoMovilizacion;
        public string GrupoQuickCount;
        public string Tag1;
        public string Tag2;
        public List<string> Tags;

        public List<EnlaceMovilizacion> EnlacesMovilizacion { get; set; }
        public List<EnlaceExitPoll> EnlacesExitPoll { get; set; }
        public List<Alerta> Alertas { get; set; }

        public List<Mesa> Mesas { get; set; }

        public CentroSettings Settings{ get; private set; }



        public CentroDeVotacion(int id)
        {
            Id = id;
            var db = new edayRoomEntities();
            InitializeFromEntity(db.Centroes.SingleOrDefault(c => c.id == id));
        }

        private void InitializeFromEntity(Centro c)
        {
            Nombre = c.Nombre;
            UniqueId = c.unique_id;

            Votantes = c.votantes ?? 0;
            CantidadMesas = c.mesas;
            Cuadernos = c.cuadernos ?? 0;
            
            Grupo = c.grupo;
            GrupoMovilizacion = c.grupoMovilizacion;
            GrupoQuickCount = c.qcGroup;
            Tag1 = c.tag1;
            Tag2 = c.tag2;
            
            Ubicacion = new UbicacionGeografica(c);
            Settings= new CentroSettings(c);

            EnlacesMovilizacion = c.Movilizadors.Select(m => new EnlaceMovilizacion(m)).ToList();
            EnlacesExitPoll = c.TestigoExitPolls.Select(m => new EnlaceExitPoll(m)).ToList();

            Alertas = new List<Alerta>();
            Alertas.AddRange(c.ExitPollAlertas.Select(a => new Alerta(a)));
            Alertas.AddRange(c.MovilizacionAlertas.Select(a => new Alerta(a)));
            Alertas.AddRange(c.QuickCountAlertas.Select(a => new Alerta(a)));

            Mesas = c.Mesas1.Select(m => new Mesa(m)).ToList();
        }  

        public static int LoadBatch(string file)
        {
            var fi = new FileInfo(file);
            var package = new ExcelPackage(fi);
            var worksheet = package.Workbook.Worksheets[1];
            var centros = new List<Centro>();
            for (var i = 2; i <= worksheet.Dimension.End.Row; i++)
            {
                var mesas = worksheet.Cells[i, 4].Value == null ? "0" : worksheet.Cells[i, 4].Value.ToString();
                var votantes = worksheet.Cells[i, 5].Value == null ? "0" : worksheet.Cells[i, 5].Value.ToString();
                var centro = new Centro
                    {
                        unique_id = worksheet.Cells[i, 1].Value == null
                                        ? ""
                                        : worksheet.Cells[i, 1].Value.ToString(),
                        Nombre = (string)worksheet.Cells[i, 2].Value,
                        Direccion = (string)worksheet.Cells[i, 3].Value,
                        mesas = int.Parse(mesas),
                        votantes = int.Parse(votantes),
                        grupo = (string)worksheet.Cells[i, 6].Value,
                        quickCountActive = ((string)worksheet.Cells[i, 8].Value).Equals("si",
                                                                                        StringComparison.
                                                                                            InvariantCultureIgnoreCase),
                        exitPollActive = ((string)worksheet.Cells[i, 9].Value).Equals("si",
                                                                                      StringComparison.
                                                                                          InvariantCultureIgnoreCase),
                        unidadGeografica1 = (string)worksheet.Cells[i, 10].Value,
                        unidadGeografica2 = (string)worksheet.Cells[i, 11].Value,
                        unidadGeografica3 = (string)worksheet.Cells[i, 12].Value,
                        unidadGeografica4 = (string)worksheet.Cells[i, 13].Value,
                        unidadGeografica5 = (string)worksheet.Cells[i, 14].Value,
                        unidadGeografica6 = (string)worksheet.Cells[i, 15].Value,
                        unidadGeografica7 = (string)worksheet.Cells[i, 16].Value,
                        unidadGeografica8 = (string)worksheet.Cells[i, 17].Value,
                        qcGroup = (string)worksheet.Cells[i, 18].Value,
                        tag1 = (string)worksheet.Cells[i, 19].Value,
                        tag2 = (string)worksheet.Cells[i, 20].Value,
                        movilizacion = ((string)worksheet.Cells[i, 21].Value).Equals("si",
                                                                                     StringComparison.
                                                                                         InvariantCultureIgnoreCase)
                    };
                centros.Add(centro);
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
                                             SqlBulkCopyOptions.KeepNulls, tran) { BatchSize = 1000, DestinationTableName = "centro" };

                    bc.WriteToServer(centros.AsDataReader());

                    tran.Commit();
                }
                con.Close();
            }
            return 1;
        }


    }
}