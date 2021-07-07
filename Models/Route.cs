using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WpfApp.Core;

namespace WpfApp.Models
{
    [DisplayName("Маршруты")]
    [AllowEdit("ROLE_DISPATCHER,ROLE_ADMIN")]
    [EditorForm(FormType =typeof(RouteEditWindow))]
    public partial class Route : DataObjectBase {
        public int Id { get; set; }

        [Display(Name = "Наименование", Order = 10)]
        [Required]
        public string Name { get; set; }

        [Display(Name = "Время отправления", Order = 20)]
        [DisplayFormat(DataFormatString ="HH:mm")]
        [Required]
        public DateTime DepartTime { get; set; }

        [Display(Name = "Время прибытия", Order = 30)]
        [DisplayFormat(DataFormatString = "HH:mm")]
        [Required]
        public DateTime ArriveTime { get; set; }

        [Display(Name = "Часов в пути", Order = 40)]
        [ReadOnly(true)]
        public decimal Hours {
            get {
                return (decimal)Math.Round((ArriveTime - DepartTime).TotalHours, 1);
            }
            set { }
        }

        [Display(Name = "Пункт отправления", Order = 50)]
        [Required]
        public string StartPoint { get; set; }

        [Display(Name = "Пункт прибытия", Order = 60)]
        [Required]
        public string EndPoint { get; set; }

        [Display(Name = "Остановки", Order = 100)]
        public virtual ICollection<RouteCity> RouteCity { get; set; } = new HashSet<RouteCity>();

        [Display(Name = "Автобусы", Order = 200)]
        public virtual ICollection<BusRoute> BusRoutes { get; set; } = new HashSet<BusRoute>();

        public override string ToString() {
            return string.Format("{0} - {1}, {2}", StartPoint, EndPoint, DepartTime.ToString("HH:mm"));
        }
    }
}
