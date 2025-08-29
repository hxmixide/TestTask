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
using DisplayNameAttribute = System.ComponentModel.DisplayNameAttribute;

namespace TestTask.Module.BusinessObjects
{
    /// <summary>
    /// Пикет
    /// </summary>
    [DefaultClassOptions] // Стандартные настройки класса
    [DisplayName("Пикеты")]
    [DefaultProperty(nameof(FullPicketNumber))] // Свойство по умолчанию для отображения
    public class Picket : BaseObject
    {
        // Регион для приватных полей объекта
        #region Поля
        private decimal _capacity; // Приватное поле для вместимости пикета
        private Site _site; // Приватное поле для связи с площадкой
        private string _picketNumber; // Приватное поле для номера пикета
        private Warehouse _warehouse; // Приватное поле для связи со складом
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

        public string GetFullPicketNumber()
        {
            if (string.IsNullOrEmpty(PicketNumber)) return string.Empty;

            string warehousePrefix = Warehouse?.WarehouseNumber?.Trim();
            if (string.IsNullOrEmpty(warehousePrefix)) return PicketNumber;

            return $"{warehousePrefix}{PicketNumber}";
        }
        #endregion

        #region Свойства
        [VisibleInListView(false)]
        [RuleRequiredField(DefaultContexts.Save)] // Поле обязательно для заполнения при сохранении
        [Size(SizeAttribute.DefaultStringMappingFieldSize)] // Стандартный размер строки в БД
        public string PicketNumber
        {
            get { return _picketNumber; } // Возвращает значение поля
            set
            {
                if (SetPropertyValue(nameof(PicketNumber), ref _picketNumber, value)) // Устанавливает новое значение
                {
                    // При изменении номера пикета обновляем имя площадки
                    if (Site != null && !Session.IsObjectsLoading) // Избегаем двойного обращения к коллекции пикетов
                    {
                        var temp = Site.CalculatedSiteNumber;
                    }

                    // Вызываем изменение вычисляемого свойства
                    OnChanged(nameof(FullPicketNumber));
                }
            }
        }

        // Вычисляемое свойство для отображения полного номера
        [VisibleInListView(true)]
        [VisibleInDetailView(false)]
        [VisibleInLookupListView(false)]
        [RuleUniqueValue(DefaultContexts.Save)] // Значение должно быть уникальным
        [ModelDefault("DisplayName", "Номер пикета")]
        public string FullPicketNumber
        {
            get { return GetFullPicketNumber(); }
        }

        [ModelDefault("DisplayFormat", "# ##0.000")] // Формат отображения числа
        [ModelDefault("EditMask", "# ##0.000")] // Маска ввода
        [ModelDefault("AllowEdit", "false")] // Запрет редактирования

        public decimal Capacity
        {
            get { return _capacity; } // Возвращает значение вместимости
            set { SetPropertyValue(nameof(Capacity), ref _capacity, 5000); } // Устанавливает фиксированное значение 5000
        }

        [Association("Site-Pickets")]
        public Site Site
        {
            get { return _site; }
            set
            {
                // Проверяем соответствие складов
                if (value != null && Warehouse != null && value.Warehouse != null)
                {
                    if (Warehouse.WarehouseNumber != value.Warehouse.WarehouseNumber)
                    {
                        // Очищаем поле площадки и выводим ошибку
                        SetPropertyValue(nameof(Site), ref _site, null);
                        throw new Exception($"Ошибка: Пикет принадлежит складу '{Warehouse.WarehouseNumber}', " +
                                          $"а площадка принадлежит складу '{value.Warehouse.WarehouseNumber}'. " +
                                          $"Присвоение невозможно.");
                    }
                }

                // Если проверка пройдена, устанавливаем значение
                SetPropertyValue(nameof(Site), ref _site, value);
            }
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

        [Association("Warehouse-Pickets")] // Связь со складом
        [DisplayName("Номер склада")]
        public Warehouse Warehouse
        {
            get { return _warehouse; }
            set
            {
                if (SetPropertyValue(nameof(Warehouse), ref _warehouse, value))
                {
                    // При изменении склада обновляем вычисляемое свойство
                    if (!Session.IsObjectsLoading)
                    {
                        OnChanged(nameof(FullPicketNumber));
                    }
                }
            }
        }
        #endregion

    }
}
