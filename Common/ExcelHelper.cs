using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Data;
using System.IO;

namespace Common
{
    /// <summary>
    /// Excel帮助类
    /// </summary>
    public class ExcelHelper
    {
        private static readonly int EXCEL03_MaxRow = 65535;

        /// <summary>
        /// 根据数据流读物Excel表格数据
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static DataTable GetExcel(Stream stream)
        {
            IWorkbook hs = CreateWorkbook(stream);

            return ExportToDataTable(hs.GetSheetAt(0));
        }

        private static IWorkbook CreateWorkbook(Stream stream)
        {
            try
            {
                return new XSSFWorkbook(stream); //07
            }
            catch
            {
                return new HSSFWorkbook(stream); //03
            }
        }

        /// <summary>
        /// 把Sheet中的数据转换为DataTable
        /// </summary>
        /// <param name="sheet"></param>
        /// <returns></returns>
        private static DataTable ExportToDataTable(ISheet sheet)
        {
            DataTable dataTable = new DataTable();

            //默认，第一行是表格字段
            IRow headRow = sheet.GetRow(0);

            //设置datatable字段
            for (int i = headRow.FirstCellNum, len = headRow.LastCellNum; i < len; i++)
            {
                dataTable.Columns.Add(headRow.Cells[i].StringCellValue.Trim());
            }

            //遍历数据行
            for (int i = (sheet.FirstRowNum + 1), len = sheet.LastRowNum + 1; i < len; i++)
            {
                IRow tempRow = sheet.GetRow(i);

                if (tempRow != null)
                {
                    DataRow dataRow = dataTable.NewRow();

                    //遍历一行的每一个单元格
                    for (int r = 0, j = headRow.FirstCellNum, len2 = headRow.LastCellNum; j < len2; j++, r++)
                    {
                        ICell cell = tempRow.GetCell(j);

                        if (cell != null)
                        {
                            switch (cell.CellType)
                            {
                                case CellType.String:
                                    dataRow[r] = cell.StringCellValue.Trim();
                                    break;

                                case CellType.Numeric:
                                    if (DateUtil.IsCellDateFormatted(cell))
                                        dataRow[r] = cell.DateCellValue;
                                    else
                                        dataRow[r] = cell.NumericCellValue;
                                    break;

                                case CellType.Boolean:
                                    dataRow[r] = cell.BooleanCellValue;
                                    break;

                                default:
                                    break;
                            }
                        }
                    }

                    foreach (var item in dataRow.ItemArray)
                    {
                        if (item != System.DBNull.Value)
                        {
                            dataTable.Rows.Add(dataRow);
                            break;
                        }
                    }
                }
            }

            return dataTable;
        }

        /// <summary>
        /// 将DataTable转换为excel2003格式。
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="sheetName"></param>
        /// <returns></returns>
        public static byte[] DataTableToExcelByte(DataTable dt, string sheetName)
        {
            IWorkbook book = new XSSFWorkbook();

            if (dt.Rows.Count < EXCEL03_MaxRow)
                DataWrite2Sheet(dt, 0, dt.Rows.Count - 1, book, sheetName);
            else
            {
                int page = dt.Rows.Count / EXCEL03_MaxRow;

                for (int i = 0; i < page; i++)
                {
                    int start = i * EXCEL03_MaxRow;

                    int end = (i * EXCEL03_MaxRow) + EXCEL03_MaxRow - 1;

                    DataWrite2Sheet(dt, start, end, book, sheetName + i.ToString());
                }
                int lastPageItemCount = dt.Rows.Count % EXCEL03_MaxRow;

                DataWrite2Sheet(dt, dt.Rows.Count - lastPageItemCount, lastPageItemCount, book, sheetName + page.ToString());
            }
            MemoryStream ms = new MemoryStream();

            book.Write(ms);

            return ms.ToArray();
        }

        private static void DataWrite2Sheet(DataTable dt, int startRow, int endRow, IWorkbook book, string sheetName)
        {
            ISheet sheet = book.CreateSheet(sheetName);

            IRow header = sheet.CreateRow(0);

            for (int i = 0; i < dt.Columns.Count; i++)
            {
                ICell cell = header.CreateCell(i);

                string val = dt.Columns[i].Caption ?? dt.Columns[i].ColumnName;

                cell.SetCellValue(val);
            }

            int rowIndex = 1;

            for (int i = startRow; i <= endRow; i++)
            {
                DataRow dtRow = dt.Rows[i];

                IRow excelRow = sheet.CreateRow(rowIndex++);

                for (int j = 0; j < dtRow.ItemArray.Length; j++)
                {
                    excelRow.CreateCell(j).SetCellValue(dtRow[j].ToString());
                }
            }
        }
    }
}
