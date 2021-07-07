using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp.Core {

    /// <summary>
    /// Базовый тип для редактируемы хобъектов
    /// </summary>
    public abstract class DataObjectBase : IComparable {

        [NotMapped]
        private Dictionary<string, object> ShadowCopy;

        [NotMapped]
        public string StringRepresentation {
            get {
                return ToString();
            }
        }

        [NotMapped]
        public virtual bool IsNew {
            get {
                int id = ReflectionHelper.GetObjectID(this);
                return id <= 0;
            }
        }

        public virtual void BeginEdit() {
            ShadowCopy = new Dictionary<string, object>();
            var properties = this.GetType().GetProperties();
            foreach (var property in properties) {
                ShadowCopy[property.Name] = property.GetValue(this);
            }
        }


        public virtual void CancelEdit() {
            var properties = this.GetType().GetProperties();
            foreach (var property in properties) {
                if (property.CanWrite) {
                    property.SetValue(this, ShadowCopy[property.Name]);
                }
            }
        }

        public virtual void InitNewObject(DbContext dataContext) {
        }


        public virtual void BeforeAdd(DbContext dataContext) {
        }


        public virtual void BeforeUpdate(DbContext dataContext) {
        }


        public virtual void BeforeDelete(DbContext dataContext) {
        }

        public virtual void OnPropertyChanged(DbContext dataContext, string propertyName) {
        }


        public virtual string Validate() {
            PropertyInfo[] properties = ReflectionHelper.GetVisibleProperties(this);
            foreach (PropertyInfo property in properties) {
                object value = property.GetValue(this);
                string name = ReflectionHelper.GetPropertyName(property);
                if (ReflectionHelper.IsPropertyRequired(property)) {
                    if (value == null
                        || (value is string && string.IsNullOrWhiteSpace(value.ToString()))) {
                        return string.Format("Необходимо заполнить поле '{0}'.", name);
                    }
                    if (value != null && value.GetType().IsValueType) {
                        Type type = value.GetType();
                        object defaultValue = Activator.CreateInstance(type);
                        if (value.Equals(defaultValue)) {
                            return string.Format("Необходимо заполнить поле '{0}'.", name);
                        }
                    }
                    if (value != null && value is IEnumerable && !((IEnumerable)value).Cast<object>().Any()) {
                        return string.Format("Необходимо заполнить список '{0}'.", name);
                    }
                }
                int maxLength = ReflectionHelper.GetPropertyTextMaxLength(property);
                if (maxLength > 0) {
                    if (value != null && value.ToString().Length > maxLength) {
                        return string.Format("Текст в поле '{0}' имеет слишком большую длину ({1}) при максимально допустимой {2}",
                            name, value.ToString().Length, maxLength);
                    }
                }
            }
            return "";
        }

        public int CompareTo(object obj) {
            if (obj == null) return 1;
            return string.Compare(this.ToString(), obj.ToString());
        }
    }
}
