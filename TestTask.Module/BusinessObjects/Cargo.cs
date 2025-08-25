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
using System.Data.SqlClient;

namespace TestTask.Module.BusinessObjects
{   /// <summary>
    /// Груз
    /// </summary>
    [DefaultClassOptions]
    [DefaultProperty(nameof(Name))]
    public class Cargo : BaseObject
    {
 
        #region Поля
        private string _name; // Приватное поле, хранящее значение наименование груза
        #endregion 

        public Cargo(Session session) : base(session)
        {
        }
        public override void AfterConstruction()
        {
            base.AfterConstruction();

        }

        #region Свойства
        [RuleRequiredField(DefaultContexts.Save)] // Поле обязательно к заполнению
        [RuleUniqueValue(DefaultContexts.Save)] // Значения поля должны быть уникальными
        [Size(50)]
        public string Name // Публичное свойство, которое предоставляет доступ к _name
        {
            get { return _name; } // Возвращает значение _name
            set { SetPropertyValue(nameof(Name), ref _name, value); } // Устанавливает новое значение value для _name
        }

        [Association("Cargo-CargoPickets")] // Связь между объектами один-ко-многим
        public XPCollection<CargoPicket> CargoPickets // Коллекия, которая отслеживает изменения и загружает связанные данные между CargoPicket и Cargo
        { get { return GetCollection<CargoPicket>(nameof(CargoPickets)); } } // Возвращает коллекцию связанных данных и передает имя свойства связи
        #endregion

    }
}

