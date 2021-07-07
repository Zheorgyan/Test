using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp.Core {

    /// <summary>
    /// Базовый класс атрибута, определяющего списка ролей пользователя
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    abstract class AllowAccessAttributeBase : Attribute {
        public string Roles { get; private set; }
        public AllowAccessAttributeBase(string roles) {
            Roles = roles;
        }
    }

    /// <summary>
    /// Атрибут определяющий роли, имеющие доступ на чтение объекта
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    class AllowReadAttribute : AllowAccessAttributeBase {
        public AllowReadAttribute(string roles) : base(roles) { }
    }

    /// <summary>
    /// Атрибут определяющий роли, имеющие доступ на создание объекта
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    class AllowCreateAttribute : AllowAccessAttributeBase {
        public AllowCreateAttribute(string roles) : base(roles) { }
    }

    /// <summary>
    /// Атрибут определяющий роли, имеющие доступ на редактирование объекта
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    class AllowEditAttribute : AllowAccessAttributeBase {
        public AllowEditAttribute(string roles) : base(roles) { }
    }

    /// <summary>
    /// Атрибут определяющий роли, имеющие доступ на удаление объекта
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    class AllowDeleteAttribute : AllowAccessAttributeBase {
        public AllowDeleteAttribute(string roles) : base(roles) { }
    }

    /// <summary>
    /// Базовый класс атрибута, определяющего параметры окна связанного с сущностью
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    abstract class FormAttributeBase : Attribute {
        public Type FormType { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Title { get; set; }
        public bool CenterToParent { get; set; }
    }

    /// <summary>
    /// Атрибут определяющий параметры окна редактирования объекта
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    class EditorFormAttribute : FormAttributeBase { }

    /// <summary>
    /// Атрибут определяющий параметры окна списка объектов
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    class ListFormAttribute : FormAttributeBase { }


    /// <summary>
    /// Атрибут определяющий направление сортировки списка объектов по полю
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    class SortDirectionAttribute : Attribute {
        public ListSortDirection Direction { get; private set; }
        public SortDirectionAttribute(ListSortDirection direction) {
            Direction = direction;
        }
    }

    /// <summary>
    /// Атрибут определяющий видимость поля объекта (в списке, в окне редактора или везде)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    class VisibilityAttribute : Attribute {
        public FieldVisibility Visibility { get; private set; }
        public VisibilityAttribute(FieldVisibility visibility) {
            Visibility = visibility;
        }
    }

    enum FieldVisibility {
        None = 0,
        Form = 1,
        List = 2,
        Both = 3,
    }
}
