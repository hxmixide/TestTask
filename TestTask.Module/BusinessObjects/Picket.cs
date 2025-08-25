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
    /// Пикет
    /// </summary>
    [DefaultClassOptions] // Стандартные настройки класса
    [DefaultProperty(nameof(PicketNumber))] // Свойство по умолчанию для отображения
    public class Picket : BaseObject
    {
        // Регион для приватных полей объекта
        #region Поля
        private decimal _capacity; // Приватное поле для вместимости пикета
        private Site _site; // Приватное поле для связи с площадкой
        private string _picketNumber; // Приватное поле для номера пикета
        #endregion

        // Конструктор класса, принимающий сессию XPO
        public Picket(Session session) : base(session)
        {
        }

        // Метод, вызываемый после создания объекта
        public override void AfterConstruction()
        {
            base.AfterConstruction();
        }

        #region Методы
        // Метод, вызываемый при изменении свойств объекта
       
        protected override void OnDeleting()
        {
            // Сохраняем ссылку на площадку перед удалением
            var site = Site;

            base.OnDeleting();

            // После удаления обновляем номер площадки
            if (site != null)
            {
                var temp = site.CalculatedSiteNumber;
            }
        }
        #endregion

        #region Свойства
        [RuleRequiredField(DefaultContexts.Save)] // Поле обязательно для заполнения при сохранении
        [RuleUniqueValue(DefaultContexts.Save)] // Значение должно быть уникальным
        [Size(SizeAttribute.DefaultStringMappingFieldSize)] // Стандартный размер строки в БД
        public string PicketNumber
        {
            get { return _picketNumber; } // Возвращает значение поля
            set
            {
                if (SetPropertyValue(nameof(PicketNumber), ref _picketNumber, value)) // Устанавливает новое значение
                {
                    // При изменении номера пикета обновляем имя площадки
                    if (Site != null)
                    {
                        var temp = Site.CalculatedSiteNumber;
                    }
                }
            }
        }

        [ModelDefault("DisplayFormat", "# ##0.000")] // Формат отображения числа
        [ModelDefault("EditMask", "# ##0.000")] // Маска ввода
        public decimal Capacity
        {
            get { return _capacity; } // Возвращает значение вместимости
            set { SetPropertyValue(nameof(Capacity), ref _capacity, 5000); } // Устанавливает фиксированное значение 5000
        }

        [Association("Site-Pickets")] // Связь между Site и Picket
        
        public Site Site
        {
            get { return _site; } // Возвращает связанную площадку
            set { SetPropertyValue(nameof(Site), ref _site, value); } // Устанавливает новую площадку
        }

        [Association("Picket-CargoPickets")] // Связь между Picket и CargoPicket
        public XPCollection<CargoPicket> CargoPickets // Коллекция грузов на пикете
        {
            get { return GetCollection<CargoPicket>(nameof(CargoPickets)); } // Возвращает коллекцию связанных данных
        }

        [ModelDefault("DisplayFormat", "# ##0.000")] // Формат отображения числа
        [ModelDefault("EditMask", "# ##0.000")] // Маска ввода
        public decimal Weight
        {
            get { return CargoPickets.Sum(s => s.Weight); } // Возвращает суммарный вес всех грузов на пикете
        }
        #endregion

    }
}