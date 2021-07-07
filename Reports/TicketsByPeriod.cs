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
    [EditorForm(Title = "Продажа билетов за период", Width = 350)]
    class TicketsByPeriod : HtmlReportBase {

        [Display(Name ="Начало периода", Order =1)]
        [Required]
        public DateTime PeriodStart { get; set; }

        [Display(Name = "Конец периода", Order = 2)]
        [Required]
        public DateTime PeriodEnd { get; set; }

        public TicketsByPeriod() {
            PeriodStart = DateTime.Today.AddMonths(-3);
            PeriodEnd = DateTime.Today;
        }

        protected override string GetReportTitle() {
            return string.Format("Отчет по продажам билетов за период с {0} по {1}", PeriodStart.ToString("dd.MM.yyyy"), PeriodEnd.ToString("dd.MM.yyyy"));
        }

        protected override IEnumerable<string> GetReportMainHeader() {
            return new string[] { GetReportTitle() };
        }

        protected override IEnumerable<string> GetDataHeader() {
            return new string[0];
        }

        int totalQuantity = 0;
        decimal totalPrice = 0;

        protected override IEnumerable GetDataItems() {
            var data = (DataContext as Models.DataContext).Ticket
                .Where(t => t.TicketDate >= PeriodStart && t.TicketDate <= PeriodEnd)
                .ToList()
                .GroupBy(t => new { t.BusRoute.Route, t.Price })
                .OrderBy(g => g.Key.Route)
                .Select(t => new ReportItem() {
                    Route = t.Key.Route.ToString(),
                    Price = t.Key.Price,
                    Quantity = t.Count()
                }).ToList();
            totalQuantity = data.Sum(t => t.Quantity);
            totalPrice = data.Sum(t => t.TotalPrice);
            return data;
        }

        protected override IEnumerable<string> GetDataFooter() {
            return new string[] {
                string.Format("Всего продано билетов: {0} шт.", totalQuantity),
                string.Format("Выручка за период: {0} руб.", totalPrice)
            };
        }


        class ReportItem {
            [Display(Name ="Маршрут", Order =1)]
            public string Route { get; set; }

            [Display(Name = "Цена билета, р.", Order = 2)]
            public decimal Price { get; set; }

            [Display(Name = "Продано билетов, шт.", Order = 3)]
            public int Quantity { get; set; }

            [Display(Name = "Общая стоимость билетов, р.", Order = 4)]
            public decimal TotalPrice {
                get {
                    return Quantity * Price;
                }
            }
        }
    }
}
