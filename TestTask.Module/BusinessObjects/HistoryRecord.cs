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
    /// История изменений
    /// </summary>
    [DefaultClassOptions] // Стандартные настройки для класса
    [DeferredDeletion(false)] // удаление происходит сразу
    public class HistoryRecord : XPObject
    {

        #region Поля 
        private DateTime _changeDate;    // Дата и время изменения
        private string _actionType;     // Действие
        private string _changedBy;      // Пользователь
        private Site _site;             // Связанная площадка
        private Warehouse _warehouse;   // Связанный склад
        #endregion

        public HistoryRecord(Session session) : base(session)
        {
        }

        public override void AfterConstruction()
        {
            base.AfterConstruction();
        }

        #region Свойства

        // Дата и время изменения
        [ModelDefault("DisplayFormat", "dd.MM.yyyy HH:mm:ss")] // Формат отображения даты
        [ModelDefault("EditMask", "dd.MM.yyyy HH:mm:ss")]     // Маска ввода даты
        public DateTime ChangeDate
        {
            get { return _changeDate; }
            set { SetPropertyValue(nameof(ChangeDate), ref _changeDate, value); }
        }

        //Тип выполненного действия
        [Size(100)] // Ограничение длины строки в БД
        public string ActionType
        {
            get { return _actionType; }
            set { SetPropertyValue(nameof(ActionType), ref _actionType, value); }
        }


        // Пользователь, выполнивший изменение
        [Size(SizeAttribute.DefaultStringMappingFieldSize)] // Стандартная длина строки
        public string ChangedBy
        {
            get { return _changedBy; }
            set { SetPropertyValue(nameof(ChangedBy), ref _changedBy, value); }
        }

        // Площадка, к которой относится запись истории
        [Association("Site-HistoryRecords")] // Связь с площадкой
        public Site Site
        {
            get { return _site; }
            set { SetPropertyValue(nameof(Site), ref _site, value); }
        }

        // Склад, к которому относится запись истории
        [Association("Warehouse-HistoryRecords")] // Связь со складом
        public Warehouse Warehouse
        {
            get { return _warehouse; }
            set { SetPropertyValue(nameof(Warehouse), ref _warehouse, value); }
        }

        // Груз на пикете, к которому относится запись истории
        [Association("CargoPicket-HistoryRecords")] // Связь с грузом на пикете
        public CargoPicket CargoPicket { get; set; }

        // Текущий общий вес (может быть null)
        public decimal? CurrentTotalWeight { get; set; }

        // Информация о пикете
        public string PicketInfo { get; set; }
        #endregion
    }
}