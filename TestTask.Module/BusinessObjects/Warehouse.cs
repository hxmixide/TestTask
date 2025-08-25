
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
    /// Склад 
    /// </summary>
    [DefaultClassOptions] // Стандартные настройки класса
    [DefaultProperty(nameof(WarehouseNumber))] // Свойство по умолчанию для отображения
    public class Warehouse : BaseObject 
    {
        // Регион для приватных полей объекта
        #region Поля
        private string _warehouseNumber; // Приватное поле для номера склада
        #endregion

        // Конструктор класса, принимающий сессию XPO
        public Warehouse(Session session) : base(session)
        {
        }

        // Метод, вызываемый после создания объекта
        public override void AfterConstruction()
        {
            base.AfterConstruction();
        }

        #region Свойства
        [RuleRequiredField(DefaultContexts.Save)] // Поле обязательно для заполнения при сохранении
        [RuleUniqueValue(DefaultContexts.Save)] // Значение должно быть уникальным
        [Size(20)] // Ограничение на размер строки в БД
        public string WarehouseNumber
        {
            get { return _warehouseNumber; } // возвращает значение поля
            set { SetPropertyValue(nameof(WarehouseNumber), ref _warehouseNumber, value); } // Устанавливает новое значение value для _warehouseNumber
        }

        [Association("Warehouse-Sites")] // Связь между Warehouse и Site
        public XPCollection<Site> Sites // Коллекция для работы со связанными объектами
        {
            get { return GetCollection<Site>(nameof(Sites)); } // Возвращает коллекцию связанных данных и передает имя свойства связи
        }

        [Association("Warehouse-HistoryRecords")] // Связь с историей изменений
        public XPCollection<HistoryRecord> HistoryRecords // Коллекция для работы со связанными объектами
        {
            get { return GetCollection<HistoryRecord>(nameof(HistoryRecords)); } // Возвращает коллекцию связанных данных и передает имя свойства связи
        }
        #endregion
    }
} 