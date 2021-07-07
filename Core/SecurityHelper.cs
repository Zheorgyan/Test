using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp.Core {

    /// <summary>
    /// Вспомогательный класс для определения правил разграничения доступа к объектам, задаваемым с помощью атрибутов.
    /// </summary>
    static class SecurityHelper {

        public static bool? IsReadAllowed(Type type) {
            var attr = ReflectionHelper.GetAttribute<AllowReadAttribute>(type);
            if (attr == null) {
                return null;
            }
            return IsContainsCurrentRole(attr.Roles);
        }

        public static bool? IsEditAllowed(Type type) {
            var attr = ReflectionHelper.GetAttribute<AllowEditAttribute>(type);
            if (attr == null) {
                bool? t = IsReadAllowed(type);
                if (t == false) {
                    return false;
                }
                return null;
            }
            return IsContainsCurrentRole(attr.Roles);
        }

        public static bool? IsCreateAllowed(Type type) {
            var attr = ReflectionHelper.GetAttribute<AllowCreateAttribute>(type);
            if (attr == null) {
                bool? t = IsEditAllowed(type);
                if (t == false) {
                    return false;
                }
                return null;
            }
            return IsContainsCurrentRole(attr.Roles);
        }

        public static bool? IsEditOrCreateAllowed(Type type) {
            bool? isCreateAllowed = IsCreateAllowed(type);
            if (isCreateAllowed != null) {
                return isCreateAllowed;
            }
            return IsEditAllowed(type);
        }

        public static bool? IsDeleteAllowed(Type type) {
            var attr = ReflectionHelper.GetAttribute<AllowDeleteAttribute>(type); ;
            if (attr == null) {
                bool? t = IsEditAllowed(type);
                if (t == false) {
                    return false;
                }
                t = IsReadAllowed(type);
                if (t == false) {
                    return false;
                }
                return null;
            }
            return IsContainsCurrentRole(attr.Roles);
        }

        public static bool? IsReadAllowed(PropertyInfo property) {
            var attr = ReflectionHelper.GetAttribute<AllowReadAttribute>(property);
            if (attr == null) {
                bool? t = IsReadAllowed(property.DeclaringType);
                if (t != null) {
                    return t;
                }
                return null;
            }
            return IsContainsCurrentRole(attr.Roles);
        }

        public static bool? IsEditAllowed(PropertyInfo property) {
            var attr = ReflectionHelper.GetAttribute<AllowEditAttribute>(property);
            if (attr == null) {
                bool? t = IsReadAllowed(property);
                if (t == false) {
                    return false;
                }
                t = IsEditAllowed(property.DeclaringType);
                if (t != null) {
                    return t;
                }
                return null;
            }
            return IsContainsCurrentRole(attr.Roles);
        }

        static bool IsContainsCurrentRole(string roles) {
            if (roles == null) {
                return false;
            }
            string[] userRoles = App.AuthenticationProvider.Roles;
            string[] attrRoles = roles.Split(',');
            foreach(var i in userRoles) {
                foreach(var j in attrRoles) {
                    if (i.ToLower() == j.ToLower()) {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
