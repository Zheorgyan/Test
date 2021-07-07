using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WpfApp.Core;

namespace WpfApp.Models
{
    [DisplayName("Маршрут автобуса")]
    [AllowEdit("ROLE_DISPATCHER,ROLE_ADMIN")]
    public partial class BusRoute : DataObjectBase {
        public int Id { get; set; }
        public int BusId { get; set; }
        public int RouteId { get; set; }

        [Display(Name = "Маршрут", Order = 10)]
        [Required]
        public virtual Route Route { get; set; }

        [Display(Name ="Автобус", Order =20)]
        [Required]
        public virtual Bus Bus { get; set; }

        public virtual ICollection<Ticket> Tickets { get; set; } = new HashSet<Ticket>();

        public override string ToString() {
            return string.Format("{0}, автобус {1}", Route?.ToString(), Bus?.ToString());
        }

    }
}
