using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace TestTask.Module.BusinessObjects
{
    /// <summary>
    /// Площадка 
    /// </summary>
    [DefaultClassOptions] // Стандартные настройки для класса
    [DefaultProperty(nameof(CalculatedSiteNumber))] // Свойство по умолчанию для отображения
    public class Site : BaseObject
    {
        #region Поля      
        private Warehouse _warehouse; // Склад, к которому относится площадка
        #endregion

        // Конструктор класса
        public Site(Session session) : base(session)
        {
        }

        // Метод инициализации после создания объекта
        public override void AfterConstruction()
        {
            base.AfterConstruction();
        }

        #region Методы работы с историей изменений
        /// <summary>
        /// Обработчик сохранения объекта
        /// </summary>
        protected override void OnSaving()
        {

            base.OnSaving();

            if (!IsSingleExecuteOnSaving())
            {
               
                return;
            }

            if (Session.IsNewObject(this))
            {
                RecordHistory("Добавление площадки");
               
                return;
            }
        }

        protected bool IsSingleExecute()
        {
       
            // При запуске из клиента
            if (false) { return true; } 
            // При запуске из службы
            else if (!false && (!(Session?.ObjectLayer is SessionObjectLayer sessionObjectLayer) || sessionObjectLayer.ParentSession == null)) { return true; }
            else return false;
        }

        /// <summary>
        /// Исключить повторное выполнение в OnSaving()
        /// </summary>
        protected bool IsSingleExecuteOnSaving()
        {
            return IsSingleExecute() // Исключить повторное выполнение
                   && !Session.IsObjectMarkedDeleted(this); // Если объект не помечен как удаленный
        }

        /// <summary>
        /// Обработчик удаления объекта
        /// </summary>
        protected override void OnDeleting()
        {
            // Удаляем все связанные пикеты
            foreach (var picket in Pickets.ToList())
            {
                picket.Site = null;
                picket.Save();
            }

            // Записываем в историю факт удаления
            RecordHistory("Удаление площадки");
            base.OnDeleting();
        }



        /// <summary>
        /// Создает запись в истории изменений
        /// </summary>   
        private void RecordHistory(string action)
        {
            var history = new HistoryRecord(Session)
            {
                ChangeDate = DateTime.Now, // Текущая дата и время
                ActionType = action, // Описание действия
                ChangedBy = SecuritySystem.CurrentUserName ?? "System", // Пользователь или система
                Site = this, // Ссылка на текущую площадку
                Warehouse = Warehouse, // Ссылка на склад
            };

            history.Save(); // Сохраняем запись
        }

        /// <summary>
        /// Обновляет номер площадки на основе номеров пикетов
        /// </summary>
        public string CalculateSiteNumber()
        {
            if (Pickets.Count == 0)
                return null; // Возвращаем null, если нет пикетов

            var picketNumbers = Pickets
                .Select(p => int.TryParse(p.FullPicketNumber, out var num) ? num : (int?)null)
                .Where(n => n.HasValue)
                .Select(n => n.Value)
                .OrderBy(n => n)
                .ToList();

            if (!picketNumbers.Any())
            {
                return null;
            }

            // Проверяем последовательность для любого количества пикетов
            bool isSequential = true;
            for (int i = 1; i < picketNumbers.Count; i++)
            {
                if (picketNumbers[i] != picketNumbers[i - 1] + 1)
                {
                    isSequential = false;
                    break;
                }
            }
           
            // Если пикеты не последовательные - выбрасываем исключение
            if (picketNumbers.Count > 1 && !isSequential)
            {
                foreach (var picket in Pickets.ToList())
                {
                    picket.Site = null;
                }
                throw new ArgumentException("Пикеты не могут разрываться в пределах одной площадки или пересекаться");
            
            }

            // Возвращаем результат в зависимости от количества пикетов
            return picketNumbers.Count == 1
                ? picketNumbers.First().ToString()
                : $"{picketNumbers.First()}-{picketNumbers.Last()}";

            
        }
        #endregion

        #region Свойства 
        public string CalculatedSiteNumber
        {
            get { return CalculateSiteNumber(); }
        }

        [RuleRequiredField(DefaultContexts.Save)]
        [Association("Warehouse-Sites")] // Связь со складом
        public Warehouse Warehouse
        {
            get { return _warehouse; }
            set
            {
                // Запрещаем изменение после создания объекта
                if (!Session.IsNewObject(this) && !IsLoading && !IsSaving && !IsDeleted && _warehouse != null)
                {
                    throw new UserFriendlyException("Склад нельзя изменить после создания объекта");
                }

                string oldWh = _warehouse?.WarehouseNumber; // Записываем старое значение склада
                bool IsEdit = SetPropertyValue(nameof(Warehouse), ref _warehouse, value); 

                if (Session.IsNewObject(this))
                {
                    return;
                }
                else if (IsEdit && !IsLoading && !IsSaving && !IsDeleted)
                {
                    RecordHistory($"Перенос со склада '{oldWh}' на склад '{_warehouse?.WarehouseNumber}'"); // Отображение в истории
                }
            }
        }

        [Association("Site-Pickets")] // Связь с пикетами
        public XPCollection<Picket> Pickets
        {
            get { return GetCollection<Picket>(nameof(Pickets)); }
        }

        [ModelDefault("DisplayFormat", "# ##0.000")] // Формат отображения
        [ModelDefault("EditMask", "# ##0.000")] // Маска ввода
        public decimal Capacity
        {
            get { return Pickets.Sum(s => s.Capacity); } // Суммарная вместимость всех пикетов
        }

        [ModelDefault("DisplayFormat", "# ##0.000")]
        [ModelDefault("EditMask", "# ##0.000")]
        public decimal Weight
        {
            get { return Pickets.Sum(s => s.Weight); } // Суммарный вес всех пикетов
        }

        [Association("Site-HistoryRecords")] // Связь с историей изменений
        public XPCollection<HistoryRecord> HistoryRecords
        {
            get { return GetCollection<HistoryRecord>(nameof(HistoryRecords)); }
        }
        #endregion
    }
}