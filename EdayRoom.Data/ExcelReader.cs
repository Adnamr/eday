using System.Data;
using System.IO;
using OfficeOpenXml;

namespace EdayRoom.Models
{
    class ExcelReader
    {

        public static DataTable GetDataTableFromExcel(string path)
        {
            using (var pck = new ExcelPackage())
            {
                using (var stream = File.OpenRead(path))
                {
                    pck.Load(stream);
                }
                var ws = pck.Workbook.Worksheets[0];
                var tbl = new DataTable();
                var cells = ws.Cells[1, 1, 1, ws.Dimension.End.Column];
                for (var i = cells.Start.Column; i <= cells.End.Column; i++)
                {
                    tbl.Columns.Add(cells[1, i].Value.ToString());
                }
                for (var rowNum = 2; rowNum <= ws.Dimension.End.Row; rowNum++)
                {
                    var wsRow = ws.Cells[rowNum, 1, rowNum, ws.Dimension.End.Column];
                    var row = tbl.NewRow();
                    for (var cellIndex = 1; cellIndex <= ws.Dimension.End.Column; cellIndex++)
                    {
                        row[cellIndex - 1] = wsRow[rowNum, cellIndex].Value;
                    }
                    tbl.Rows.Add(row);
                }
                return tbl;
            }
        }
    }
}
