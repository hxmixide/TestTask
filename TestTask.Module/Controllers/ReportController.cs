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
        private SimpleAction exportToExcelAction; // Кнопка экспорта в excel

        public ReportController()
        {
            InitializeComponent(); // Инициализация действий и кнопки 

            exportToExcelAction = new SimpleAction(this, "ExportWarehousesToExcel", PredefinedCategory.Reports) // Действие для кнопки
            {
                Caption = "Экспорт в Excel", // Текст кнопки
                ToolTip = "Экспорт данных по складам и площадкам в Excel", //Всплывающая подсказка
                ImageName = "Action_Export_ToExcel" // Иконка кнопки
            };
            exportToExcelAction.Execute += ExportToExcelAction_Execute; // Назначение события на кнопку
        }

        private void ExportToExcelAction_Execute(object sender, SimpleActionExecuteEventArgs e)
        {

            using (var package = new ExcelPackage()) // Создание excel документа
            {
                var worksheet = package.Workbook.Worksheets.Add("Отчет по складам"); // Лист
                var reportDateTime = DateTime.Now; // Дата создания отчета

                // Заголовок отчета
                worksheet.Cells[1, 1].Value = "Отчет по складам и площадкам";
                worksheet.Cells[1, 1, 1, 3].Merge = true; // Объединение ячеек
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.Font.Size = 14;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                worksheet.Cells[2, 1].Value = $"Дата формирования: {reportDateTime:dd.MM.yyyy HH:mm:ss}";
                worksheet.Cells[2, 1, 2, 3].Merge = true;
                worksheet.Cells[2, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                worksheet.Cells[2, 1].Style.Font.Italic = true;

                // Заголовки столбцов
                int headerRow = 4; // Заголовки столбцов будут начинаться с 4 строки
                worksheet.Cells[headerRow, 1].Value = "Номер склада";
                worksheet.Cells[headerRow, 2].Value = "Площадка";
                worksheet.Cells[headerRow, 3].Value = "Вес";
                

                // Форматирование заголовков
                using (var range = worksheet.Cells[headerRow, 1, headerRow, 3])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Font.Size = 12;
                }

                int dataRow = headerRow + 1; // Начало строк для данных
                var warehouses = ObjectSpace.GetObjects<Warehouse>(); // Получаем все склады
                decimal totalAllWeight = 0;

                foreach (var warehouse in warehouses)
                {
                    bool isFirstSite = true;
                    decimal warehouseTotal = 0;

                    foreach (var site in warehouse.Sites)
                    {
                        worksheet.Cells[dataRow, 1].Value = isFirstSite ? warehouse.WarehouseNumber : ""; // Номер склада (только у первой площадки)
                        worksheet.Cells[dataRow, 2].Value = site.CalculatedSiteNumber;
                        worksheet.Cells[dataRow, 3].Value = site.Weight;
                        worksheet.Cells[dataRow, 3].Style.Numberformat.Format = "#,##0.000";
                        

                        warehouseTotal += site.Weight;
                        isFirstSite = false;
                        dataRow++;
                    }

                    // Итог по складу
                    if (warehouse.Sites.Count > 0)
                    {
                        worksheet.Cells[dataRow, 2].Value = "Итого по складу:";
                        worksheet.Cells[dataRow, 2].Style.Font.Bold = true;
                        worksheet.Cells[dataRow, 3].Value = warehouseTotal;
                        worksheet.Cells[dataRow, 3].Style.Numberformat.Format = "#,##0.000";
                        worksheet.Cells[dataRow, 3].Style.Font.Bold = true;

                        totalAllWeight += warehouseTotal;   
                        dataRow += 2; // Пропуск строки перед следующим складом
                    }
                }

                // Общий итог по всем складам
                if (warehouses.Any(w => w.Sites.Count > 0))
                {
                    worksheet.Cells[dataRow, 1].Value = "ИТОГО:";
                    worksheet.Cells[dataRow, 1].Style.Font.Bold = true;
                    worksheet.Cells[dataRow, 3].Value = totalAllWeight;
                    worksheet.Cells[dataRow, 3].Style.Numberformat.Format = "#,##0.000";
                    worksheet.Cells[dataRow, 3].Style.Font.Bold = true;

                    // Объединение ячеек для заголовка
                    worksheet.Cells[dataRow, 1, dataRow, 2].Merge = true;
                    dataRow++;
                }

                // Автоподбор ширины столбцов
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // Границы для данных
                using (var range = worksheet.Cells[headerRow, 1, dataRow - 1, 3])
                {
                    // Тонкие границы
                    range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin; 
                    range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                }

                // Сохранение файла
                var fileName = $"Отчет_по_складам_{reportDateTime:yyyyMMdd_HHmmss}.xlsx";
                var fileData = package.GetAsByteArray(); // Excel файл в виде массива байтов
                var filePath = Path.Combine(Path.GetTempPath(), fileName); // Путь к временной папке
                File.WriteAllBytes(filePath, fileData); // Сохранение

                // Открытие файла
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true // Стандартная программа для .xlsx
                });
            }
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            exportToExcelAction.Active["ViewType"] = View is ListView && View.ObjectTypeInfo.Type == typeof(Warehouse); // Активация кнопки только для списка складов
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();
        }
    }
}