using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfApp.Core;

namespace WpfApp.Reports
{
    [EditorForm(Title = "Загрузка рейсов за период", Width = 350)]
    class RoutesByPeriod : HtmlReportBase {

        [Display(Name ="Начало периода", Order =1)]
        [Required]
        public DateTime PeriodStart { get; set; }

        [Display(Name = "Конец периода", Order = 2)]
        [Required]
        public DateTime PeriodEnd { get; set; }

        public RoutesByPeriod() {
            PeriodStart = DateTime.Today.AddMonths(-3);
            PeriodEnd = DateTime.Today;
        }

        protected override string GetReportTitle() {
            return string.Format("Отчет по загрузке рейсов за период с {0} по {1}", PeriodStart.ToString("dd.MM.yyyy"), PeriodEnd.ToString("dd.MM.yyyy"));
        }

        protected override IEnumerable<string> GetReportMainHeader() {
            return new string[] { GetReportTitle() };
        }

        protected override IEnumerable<string> GetDataHeader() {
            return new string[0];
        }

        int totalQuantity = 0;

        protected override IEnumerable GetDataItems() {
            var data = (DataContext as Models.DataContext).Ticket
                .Where(t => t.TicketDate >= PeriodStart && t.TicketDate <= PeriodEnd)
                .ToList()
                .GroupBy(t => new { t.BusRoute.Route.StartPoint, t.BusRoute.Route.EndPoint })
                .OrderBy(g => g.Key.StartPoint)
                .Select(t => new ReportItem() {
                    DepartPoint = t.Key.StartPoint,
                    ArrivePoint = t.Key.EndPoint,
                    Quantity = t.Count()
                }).ToList();
            totalQuantity = data.Sum(t => t.Quantity);
            return data;
        }

        protected override IEnumerable<string> GetDataFooter() {
            return new string[] {
                string.Format("Всего продано билетов: {0} шт.", totalQuantity)
            };
        }


        class ReportItem {
            [Display(Name = "Пункт отправления", Order = 1)]
            public string DepartPoint { get; set; }

            [Display(Name = "Пункт назначения", Order = 2)]
            public string ArrivePoint { get; set; }
                       
            [Display(Name = "Продано билетов, шт.", Order = 3)]
            public int Quantity { get; set; }
        }
    }
}
