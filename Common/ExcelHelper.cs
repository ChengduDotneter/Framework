using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Data;
using System.IO;
using System.Text;

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

        /// <summary>
        /// DataTable导出到Excel的MemoryStream
        /// </summary>
        /// <param name="dtSource">源DataTable</param>
        /// <summary>
        /// DataTable导出到Excel的MemoryStream
        /// </summary>
        /// <param name="dtSource">源DataTable</param>
        public static MemoryStream Export(DataTable dtSource)
        {
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet();

            //取得列宽
            int[] arrColWidth = new int[dtSource.Columns.Count];

            foreach (DataColumn item in dtSource.Columns)
            {
                arrColWidth[item.Ordinal] = Encoding.GetEncoding(936).GetBytes(item.ColumnName.ToString()).Length;
            }

            for (int i = 0; i < dtSource.Rows.Count; i++)
            {
                for (int j = 0; j < dtSource.Columns.Count; j++)
                {
                    int intTemp = Encoding.GetEncoding(936).GetBytes(dtSource.Rows[i][j].ToString()).Length;

                    if (intTemp > 254)
                        intTemp = 254;

                    if (intTemp > arrColWidth[j])
                    {
                        arrColWidth[j] = intTemp;
                    }
                }
            }

            int rowIndex = 0;
            foreach (DataRow row in dtSource.Rows)
            {
                #region 新建表，填充表头，填充列头，样式

                if (rowIndex == 65535 || rowIndex == 0)
                {
                    if (rowIndex != 0)
                    {
                        sheet = workbook.CreateSheet();
                    }

                    #region 列头及样式

                    IRow headerRow = sheet.CreateRow(0);
                    ICellStyle headStyle = workbook.CreateCellStyle();
                    headStyle.Alignment = HorizontalAlignment.Center;
                    IFont font = workbook.CreateFont();
                    font.FontHeightInPoints = 10;
                    font.Boldweight = 700;
                    headStyle.SetFont(font);
                    foreach (DataColumn column in dtSource.Columns)
                    {
                        headerRow.CreateCell(column.Ordinal).SetCellValue(column.ColumnName);
                        headerRow.GetCell(column.Ordinal).CellStyle = headStyle;

                        //设置列宽
                        sheet.SetColumnWidth(column.Ordinal, (arrColWidth[column.Ordinal] + 1) * 256);
                    }

                    #endregion 列头及样式

                    rowIndex++;
                }

                #endregion 新建表，填充表头，填充列头，样式

                #region 填充内容

                IRow dataRow = sheet.CreateRow(rowIndex);
                ICellStyle rowStyle = workbook.CreateCellStyle();
                rowStyle.WrapText = true;
                foreach (DataColumn column in dtSource.Columns)
                {
                    ICell newCell = dataRow.CreateCell(column.Ordinal);
                    newCell.CellStyle = rowStyle;
                    string drValue = row[column].ToString();

                    newCell.SetCellValue(drValue);
                }

                #endregion 填充内容

                rowIndex++;
            }
            MemoryStream ms = new MemoryStream();
            workbook.Write(ms);
            return ms;
        }

        /// <summary>
        /// DataTable导出到Excel文件(07及以上版本excel文件)
        /// </summary>
        /// <param name="dtSource">源DataTable</param>
        /// <param name="strFileName">保存位置</param>
        public static void Export(DataTable dtSource, string strFileName)
        {
            if (string.IsNullOrWhiteSpace(strFileName))
                throw new DealException("请传入文件名");

            if (!strFileName.EndsWith(".xlsx"))
                throw new DealException("当前只支持07及以上版本excel文件");

            using (MemoryStream ms = Export(dtSource))
            {
                using (FileStream fs = new FileStream(strFileName, FileMode.Create, FileAccess.Write))
                {
                    byte[] data = ms.ToArray();
                    fs.Write(data, 0, data.Length);
                    fs.Flush();
                }
            }
        }
    }
}