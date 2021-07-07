using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace WpfApp.Core {

    /// <summary>
    /// Базовый класс для отчета, формируемого в формате HTML
    /// </summary>
    abstract class HtmlReportBase : DataObjectBase, IDisposable {

        DbContext dbContext;
        protected DbContext DataContext {
            get {
                if (dbContext == null) {
                    dbContext = App.DataContextFactory.CreateDbContext();
                }
                return dbContext;
            }
        }

        public void ShowPreview() {
            string html = GetHtml();
            string fileName = CreateTempFile(html);
            var process = System.Diagnostics.Process.Start(fileName);
        }

        private string GetHtml() {
            StringBuilder sb = new StringBuilder();
            sb.Append("<html>");
            sb.AppendFormat("<head><title>{0}</title>", GetReportTitle());
            sb.Append("<meta http-equiv=\"content-type\" content=\"text/html; charset=utf-8\" /></head>");
            sb.Append("<body>");

            foreach (string h1 in GetReportMainHeader()) {
                sb.AppendFormat("<h1>{0}</h1>", h1);
            }
            foreach (string h2 in GetReportSubHeader()) {
                sb.AppendFormat("<h2>{0}</h2>", h2);
            }

            IEnumerable<string> dataHeader = GetDataHeader();
            if (dataHeader != null && dataHeader.Any()) {
                sb.Append("<p>");
                foreach (string s in dataHeader) {
                    sb.AppendFormat("{0}<br/>", s);
                }
                sb.Append("</p>");
            }

            IEnumerable<object> dataItems = GetDataItems().Cast<object>();
            if (dataItems != null && dataItems.Any()) {
                PropertyInfo[] properties = ReflectionHelper.GetVisibleProperties(dataItems.First());
                sb.AppendFormat("<table border='1' style='border-collapse: collapse; width: 100%'>{0}", FormatTableHeader(properties));
                foreach (object dataItem in dataItems) {
                    sb.Append(FormatTableRow(properties, dataItem));
                }
                sb.AppendFormat("</table>");
            }

            IEnumerable<string> dataFooter = GetDataFooter();
            if (dataFooter != null && dataFooter.Any()) {
                sb.Append("<p>");
                foreach (string s in dataFooter) {
                    sb.AppendFormat("{0}<br/>", s);
                }
                sb.Append("</p>");
            }

            sb.Append("</body>");
            sb.Append("</html>");
            return sb.ToString();
        }

        private string FormatTableHeader(PropertyInfo[] props) {
            StringBuilder sb = new StringBuilder();
            sb.Append("<tr style='text-align: center'>");

            foreach (var prop in props) {
                string propName = ReflectionHelper.GetPropertyName(prop);
                sb.AppendFormat("<td style='padding: 2px;'>{0}</td>", propName);
            }

            sb.Append("</tr>");
            return sb.ToString();
        }

        private string FormatTableRow(PropertyInfo[] props, object dataItem) {
            StringBuilder sb = new StringBuilder();
            sb.Append("<tr>");

            foreach (var prop in props) {
                object value = prop.GetValue(dataItem);
                sb.AppendFormat("<td style='padding: 2px;'>{0}</td>", FormatValue(prop, dataItem, value));
            }

            sb.Append("</tr>");
            return sb.ToString();
        }

        private string CreateTempFile(string html) {
            string fileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".html");
            File.WriteAllText(fileName, html, Encoding.UTF8);
            return fileName;
        }

        protected abstract string GetReportTitle();
        protected abstract IEnumerable<string> GetReportMainHeader();
        protected virtual IEnumerable<string> GetReportSubHeader() {
            return new string[0];
        }
        protected abstract IEnumerable<string> GetDataHeader();    
        protected abstract IEnumerable GetDataItems();
        protected abstract IEnumerable<string> GetDataFooter();
        protected virtual string FormatValue(PropertyInfo property, object dataItem, object value) {
            return (value != null) ? value.ToString() : "";
        }

        public sealed override void BeforeAdd(DbContext dataContext) {
            throw new NotSupportedException();
        }

        public sealed override void BeforeDelete(DbContext dataContext) {
            throw new NotSupportedException();
        }

        public sealed override bool IsNew => true;

        public void Dispose() {
            if (dbContext != null) {
                dbContext.Dispose();
            }
        }
    }
}
