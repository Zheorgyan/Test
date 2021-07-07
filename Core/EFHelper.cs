using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp.Core {

    /// <summary>
    /// Вспомогательный класс для работы с моделью данных контекста EntityFramework
    /// </summary>
    static class EFHelper {

        public static IQueryable<object> Set(this DbContext _context, Type t) {
            return (IQueryable<object>)_context.GetType().GetMethod("Set").MakeGenericMethod(t).Invoke(_context, null);
        }

        public static List<DataObjectBase> GetObjectCollection(DbContext dbContext, Type entityType, int referenceDepth = 3) {
            var items = new List<DataObjectBase>();
            IQueryable<object> dbQuery = dbContext.Set(entityType);
            if (referenceDepth > 0) {
                List<string> references = GetReferences(entityType, referenceDepth);
                references.AddRange(GetCollectionReferences(entityType));
                foreach (string reference in references) {
                    dbQuery = dbQuery.Include(reference);
                }
            }
            foreach (DataObjectBase item in dbQuery) {
                items.Add(item);
            }
            return items;
        }

        public static Type GetEntityTypeFromProxy(object proxy) {
            if (proxy is Microsoft.EntityFrameworkCore.Proxies.Internal.IProxyLazyLoader) {
                return proxy.GetType().BaseType;
            }
            return proxy.GetType();
        }

        static List<string> GetReferences(Type type, int maxDepth) {
            PropertyInfo[] props = ReflectionHelper.GetVisibleProperties(type);
            List<string> references = new List<string>();
            foreach (var prop in props) {
                if (prop.CanWrite && !ReflectionHelper.HasAttribute(prop, typeof(NotMappedAttribute))
                    && !prop.PropertyType.IsValueType && !prop.PropertyType.IsGenericType
                    && prop.PropertyType != typeof(string)) {
                    references.Add(prop.Name);
                    if (maxDepth > 0) {
                        List<string> subReferences = GetReferences(prop.PropertyType, maxDepth - 1);
                        foreach (string subref in subReferences) {
                            references.Add(prop.Name + "." + subref);
                        }
                    }
                }
            }
            return references;
        }

        static List<string> GetCollectionReferences(Type type) {
            PropertyInfo[] props = ReflectionHelper.GetVisibleProperties(type);
            List<string> references = new List<string>();
            foreach (var prop in props) {
                if (prop.CanWrite && !ReflectionHelper.HasAttribute(prop, typeof(NotMappedAttribute))
                    && !prop.PropertyType.IsValueType && prop.PropertyType.IsGenericType
                    && typeof(IEnumerable).IsAssignableFrom(prop.PropertyType)
                    && prop.PropertyType != typeof(string)) {
                    references.Add(prop.Name);
                }
            }
            return references;
        }
    }
}
