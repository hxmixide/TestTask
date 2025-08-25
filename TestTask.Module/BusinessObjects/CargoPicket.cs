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
using DevExpress.XtraEditors; 
using DevExpress.Xpf.Core; 


namespace TestTask.Module.BusinessObjects
{
    /// <summary>
    /// Груз на пикете 
    /// </summary>
    [DefaultClassOptions] // Стандартные настройки для класса
    public class CargoPicket : BaseObject
    {
        #region Поля 
        private decimal _weight;       // Вес груза
        private Picket _picket;       // Ссылка на пикет, где размещен груз
        private Cargo _cargo;         // Ссылка на тип груза
        #endregion

        public CargoPicket(Session session) : base(session)
        {
        }

        public override void AfterConstruction()
        {
            base.AfterConstruction();
        }

        #region Методы работы с историей изменений

        // Обработчик сохранения объекта
        protected override void OnSaving()
        { 
        
            // Проверка вместимости пикета перед сохранением
            if (Picket != null && IsSingleExecuteOnSaving())
            {
                decimal newTotalWeight;

                if (Session.IsNewObject(this))
                {
                    // Для нового груза
                    newTotalWeight = Picket.Weight + this.Weight;
                }
                else
                {
                    // Для существующего груза
                    decimal oldWeight = (decimal)GetMemberValue(nameof(Weight));
                    newTotalWeight = Picket.Weight - oldWeight + this.Weight;
                }

                if (newTotalWeight > Picket.Capacity)
                {
                    throw new UserFriendlyException($"Невозможно сохранить: превышена вместимость. " +
                    $"максимальная вместимость: {Picket.Capacity}, " +
                    $"превышение вместимости на {newTotalWeight - Picket.Capacity}");

                }
            }

            // Логирование истории изменений (только при IsSingleExecuteOnSaving() == true)
            if (IsSingleExecuteOnSaving())
            {
                bool isNew = Session.IsNewObject(this);
                string actionType;
                decimal weightChange = 0;

                if (isNew)
                {
                    actionType = "Добавление груза на пикет";
                    weightChange = Weight;
                }
                else
                {
                    decimal oldWeight = (decimal)GetMemberValue(nameof(Weight));
                    weightChange = Weight - oldWeight;
                    actionType = "Изменение веса";
                }

                CreateHistoryRecord(actionType, weightChange);
            }

            base.OnSaving();
        }

        protected bool IsSingleExecute()
        {
            
            // При первой вызове Session.ObjectLayer == SecuredSessionObjectLayer (наследник SessionObjectLayer) или просто SessionObjectLayer - нет на службах, обработается на клиенте (иначе не логируются изменения в объектах)
            // При запуске из клиента
            if (false && Session?.ObjectLayer is SessionObjectLayer) { return true; }
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

        // Обработчик удаления объекта
        protected override void OnDeleting()
        {
            CreateHistoryRecord("Удаление груза с пикета", -Weight); // Записываем в историю факт удаления
            base.OnDeleting(); // Вызываем базовый метод
        }

        // Создает запись в истории изменений
        private void CreateHistoryRecord(string actionType, decimal weightChange)
        {
            if (Picket?.Site == null) return;

            // Рассчитываем актуальный вес площадки
            decimal actualWeight = Picket.Site.Weight;

            // Если это удаление - корректируем вес
            if (actionType.StartsWith("Удаление"))
            {
                actualWeight -= Weight; // Вычитаем вес удаляемого груза
            }

            var record = new HistoryRecord(Session)
            {
                ChangeDate = DateTime.Now,
                ActionType = $"{actionType}: {Cargo?.Name}",
                ChangedBy = SecuritySystem.CurrentUserName ?? "System",
                Site = Picket.Site,
                Warehouse = Picket.Site.Warehouse,
                CargoPicket = this,
                CurrentTotalWeight = actualWeight, // Используем скорректированный вес
                PicketInfo = $"Пикет {Picket.PicketNumber} (Площадка {Picket.Site.CalculatedSiteNumber})"
            };

            record.Save();
        }
        #endregion

        #region Свойства
        /// <summary>
        /// Вес груза (в килограммах)
        /// </summary>
        [ModelDefault("DisplayFormat", "# ##0.000")] // Формат отображения
        [ModelDefault("EditMask", "# ##0.000")] // Маска ввода
        [RuleRequiredField]
        public decimal Weight
        {
            get { return _weight; }
            set { SetPropertyValue(nameof(Weight), ref _weight, value); }
        }

        /// <summary>
        /// Пикет, на котором размещен груз
        /// </summary>
        [Association("Picket-CargoPickets")] // Связь с пикетом
        [RuleRequiredField]
        public Picket Picket
        {
            get { return _picket; }
            set { SetPropertyValue(nameof(Picket), ref _picket, value); }
        }

        /// <summary>
        /// Тип груза
        /// </summary>
        [Association("Cargo-CargoPickets")] // Связь с типом груза
        [RuleRequiredField]
        public Cargo Cargo
        {
            get { return _cargo; }
            set { SetPropertyValue(nameof(Cargo), ref _cargo, value); }
        }

        /// <summary>
        /// История изменений по данному грузу
        /// </summary>
        [Association("CargoPicket-HistoryRecords")] // Связь с историей изменений
        public XPCollection<HistoryRecord> HistoryRecords => GetCollection<HistoryRecord>(nameof(HistoryRecords));
        #endregion
    }
}