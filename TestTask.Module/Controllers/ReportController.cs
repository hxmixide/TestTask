using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;
using OfficeOpenXml;
using System;
using System.IO;
using System.Linq;
using TestTask.Module.BusinessObjects;

namespace TestTask.Module.Controllers
{
    public partial class ReportController : ViewController<ListView>
    {
        private SimpleAction exportToExcelAction;

        public ReportController()
        {
            InitializeComponent();

            exportToExcelAction = new SimpleAction(this, "ExportWarehousesToExcel", PredefinedCategory.Reports)
            {
                Caption = "Экспорт в Excel",
                ToolTip = "Экспорт данных по складам и площадкам в Excel",
                ImageName = "Action_Export_ToExcel"
            };
            exportToExcelAction.Execute += ExportToExcelAction_Execute;
        }

        private void ExportToExcelAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Отчет по складам");
                var reportDateTime = DateTime.Now;

                // Заголовок отчета
                worksheet.Cells[1, 1].Value = "Отчет по складам, площадкам и пикетам";
                worksheet.Cells[1, 1, 1, 4].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.Font.Size = 14;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                worksheet.Cells[2, 1].Value = $"Дата формирования: {reportDateTime:dd.MM.yyyy HH:mm:ss}";
                worksheet.Cells[2, 1, 2, 4].Merge = true;
                worksheet.Cells[2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                worksheet.Cells[2, 1].Style.Font.Italic = true;

                // Заголовки столбцов
                int headerRow = 4;
                worksheet.Cells[headerRow, 1].Value = "Номер склада";
                worksheet.Cells[headerRow, 2].Value = "Площадка";
                worksheet.Cells[headerRow, 3].Value = "Пикет";
                worksheet.Cells[headerRow, 4].Value = "Вес";

                // Форматирование заголовков
                using (var range = worksheet.Cells[headerRow, 1, headerRow, 4])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Font.Size = 12;
                }

                int dataRow = headerRow + 1;
                var warehouses = ObjectSpace.GetObjects<Warehouse>();
                decimal totalAllWeight = 0;

                foreach (var warehouse in warehouses)
                {
                    bool isFirstRow = true;
                    decimal warehouseTotal = 0;

                    // Обрабатываем площадки склада
                    foreach (var site in warehouse.Sites)
                    {
                        worksheet.Cells[dataRow, 1].Value = isFirstRow ? warehouse.WarehouseNumber : "";
                        worksheet.Cells[dataRow, 2].Value = site.CalculatedSiteNumber;
                        worksheet.Cells[dataRow, 3].Value = ""; // Пусто для площадки
                        worksheet.Cells[dataRow, 4].Value = site.Weight;
                        worksheet.Cells[dataRow, 4].Style.Numberformat.Format = "#,##0.000";

                        warehouseTotal += site.Weight;
                        isFirstRow = false;
                        dataRow++;

                        // Обрабатываем пикеты площадки
                        foreach (var picket in site.Pickets)
                        {
                            worksheet.Cells[dataRow, 1].Value = "";
                            worksheet.Cells[dataRow, 2].Value = "";
                            worksheet.Cells[dataRow, 3].Value = picket.FullPicketNumber;
                            worksheet.Cells[dataRow, 4].Value = picket.Weight;
                            worksheet.Cells[dataRow, 4].Style.Numberformat.Format = "#,##0.000";

                            dataRow++;
                        }
                    }

                    // Обрабатываем пикеты без площадок (привязанные напрямую к складу)
                    var picketsWithoutSite = warehouse.Pickets.Where(p => p.Site == null).ToList();
                    if (picketsWithoutSite.Any())
                    {
                        foreach (var picket in picketsWithoutSite)
                        {
                            worksheet.Cells[dataRow, 1].Value = isFirstRow ? warehouse.WarehouseNumber : "";
                            worksheet.Cells[dataRow, 2].Value = "Без площадки";
                            worksheet.Cells[dataRow, 3].Value = picket.FullPicketNumber;
                            worksheet.Cells[dataRow, 4].Value = picket.Weight;
                            worksheet.Cells[dataRow, 4].Style.Numberformat.Format = "#,##0.000";

                            warehouseTotal += picket.Weight;
                            isFirstRow = false;
                            dataRow++;
                        }
                    }

                    // Итог по складу
                    if (warehouse.Sites.Count > 0 || picketsWithoutSite.Any())
                    {
                        worksheet.Cells[dataRow, 2].Value = "Итого по складу:";
                        worksheet.Cells[dataRow, 2].Style.Font.Bold = true;
                        worksheet.Cells[dataRow, 4].Value = warehouseTotal;
                        worksheet.Cells[dataRow, 4].Style.Numberformat.Format = "#,##0.000";
                        worksheet.Cells[dataRow, 4].Style.Font.Bold = true;

                        totalAllWeight += warehouseTotal;
                        dataRow += 2; // Пропуск строки перед следующим складом
                    }
                }

                // Общий итог по всем складам
                if (warehouses.Any(w => w.Sites.Count > 0 || w.Pickets.Any(p => p.Site == null)))
                {
                    worksheet.Cells[dataRow, 1].Value = "ИТОГО:";
                    worksheet.Cells[dataRow, 1].Style.Font.Bold = true;
                    worksheet.Cells[dataRow, 4].Value = totalAllWeight;
                    worksheet.Cells[dataRow, 4].Style.Numberformat.Format = "#,##0.000";
                    worksheet.Cells[dataRow, 4].Style.Font.Bold = true;

                    // Объединение ячеек для заголовка
                    worksheet.Cells[dataRow, 1, dataRow, 3].Merge = true;
                    dataRow++;
                }

                // Автоподбор ширины столбцов
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // Границы для данных
                using (var range = worksheet.Cells[headerRow, 1, dataRow - 1, 4])
                {
                    range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                }

                // Сохранение файла
                var fileName = $"Отчет_по_складам_{reportDateTime:yyyyMMdd_HHmmss}.xlsx";
                var fileData = package.GetAsByteArray();
                var filePath = Path.Combine(Path.GetTempPath(), fileName);
                File.WriteAllBytes(filePath, fileData);

                // Открытие файла
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            exportToExcelAction.Active["ViewType"] = View is ListView && View.ObjectTypeInfo.Type == typeof(Warehouse);
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();
        }
    }
}