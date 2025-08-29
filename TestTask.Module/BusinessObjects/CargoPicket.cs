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
    [DefaultClassOptions]
    public class CargoPicket : BaseObject
    {
        #region Поля 
        private decimal _weight;
        private Picket _picket;
        private Cargo _cargo;
        #endregion

        public CargoPicket(Session session) : base(session)
        {
        }

        public override void AfterConstruction()
        {
            base.AfterConstruction();
        }

        #region Методы работы с историей изменений

        protected override void OnSaving()
        {
            if (Picket != null && IsSingleExecuteOnSaving())
            {
                decimal totalWeightInSession = Picket.CargoPickets
                    .Where(cp => !Session.IsNewObject(cp) || cp == this)
                    .Sum(cp => cp.Weight);

                if (!Session.IsNewObject(this))
                {
                    decimal oldWeight = (decimal)GetMemberValue(nameof(Weight));
                    totalWeightInSession = totalWeightInSession - oldWeight + this.Weight;
                }

                if (totalWeightInSession > Picket.Capacity)
                {
                    throw new UserFriendlyException($"Невозможно сохранить: превышена вместимость. " +
                    $"Максимальная вместимость: {Picket.Capacity}, " +
                    $"превышение вместимости на: {totalWeightInSession - Picket.Capacity}");
                }
            }

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
            if (Session?.ObjectLayer is SessionObjectLayer) { return true; }
            else if (!(Session?.ObjectLayer is SessionObjectLayer sessionObjectLayer) || sessionObjectLayer.ParentSession == null) { return true; }
            else return false;
        }

        protected bool IsSingleExecuteOnSaving()
        {
            return IsSingleExecute() && !Session.IsObjectMarkedDeleted(this);
        }

        protected override void OnDeleting()
        {
            CreateHistoryRecord("Удаление груза с пикета", -Weight);
            base.OnDeleting();
        }

        private void CreateHistoryRecord(string actionType, decimal weightChange)
        {
            if (Picket?.Site == null) return;

            decimal actualWeight = Picket.Site.Weight;

            if (actionType.StartsWith("Удаление"))
            {
                actualWeight -= Weight;
            }

            var record = new HistoryRecord(Session)
            {
                ChangeDate = DateTime.Now,
                ActionType = $"{actionType}: {Cargo?.Name}",
                ChangedBy = SecuritySystem.CurrentUserName ?? "System",
                Site = Picket.Site,
                Warehouse = Picket.Site.Warehouse,
                CargoPicket = this,
                CurrentTotalWeight = actualWeight,
                PicketInfo = $"Пикет {Picket.FullPicketNumber} (Площадка {Picket.Site.CalculatedSiteNumber})"
            };

            record.Save();
        }
        #endregion

        #region Свойства
        [ModelDefault("DisplayFormat", "# ##0.000")]
        [ModelDefault("EditMask", "# ##0.000")]
        [RuleRequiredField]
        public decimal Weight
        {
            get { return _weight; }
            set { SetPropertyValue(nameof(Weight), ref _weight, value); }
        }

        [Association("Picket-CargoPickets")]
        [RuleRequiredField]
        public Picket Picket
        {
            get { return _picket; }
            set { SetPropertyValue(nameof(Picket), ref _picket, value); }
        }

        // Вычисляемое свойство для отображения полного номера пикета
        [VisibleInListView(false)]
        [VisibleInDetailView(false)]
        [VisibleInLookupListView(false)]
        [ModelDefault("DisplayFormat", "{0}")]
        [ModelDefault("AllowEdit", "False")]
        public string FullPicketNumber
        {
            get { return Picket?.FullPicketNumber ?? string.Empty; }
        }

        [Association("Cargo-CargoPickets")]
        [RuleRequiredField]
        public Cargo Cargo
        {
            get { return _cargo; }
            set { SetPropertyValue(nameof(Cargo), ref _cargo, value); }
        }

        [Association("CargoPicket-HistoryRecords")]
        public XPCollection<HistoryRecord> HistoryRecords => GetCollection<HistoryRecord>(nameof(HistoryRecords));
        #endregion
    }
}