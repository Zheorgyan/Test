using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WpfApp.Core;

namespace WpfApp.Models
{
    [DisplayName("Автобусы")]
    [AllowEdit("ROLE_DISPATCHER,ROLE_ADMIN")]
    [EditorForm(FormType = typeof(BusEditWindow))]
    public partial class Bus : DataObjectBase {
        public int Id { get; set; }

        [Display(Name ="Марка", Order =10)]
        [Required]
        public string Model { get; set; }

        [Display(Name = "Гос. номер", Order = 20)]
        [Required]
        public string RegNumber { get; set; }

        [Display(Name = "Кол-во мест", Order = 30)]
        [Required]
        public int PassengetCount { get; set; }

        [Display(Name = "Номер страховки", Order = 40)]
        [Required]
        public string InsuranceNumber { get; set; }

        public virtual ICollection<BusRoute> BusRoutes { get; set; } = new HashSet<BusRoute>();

        [Display(Name ="Закрепленные водители", Order =100)]
        public virtual ICollection<BusDriver> BusDrivers { get; set; } = new HashSet<BusDriver>();

        public override string ToString() {
            return string.Format("{0} {1}", Model, RegNumber);
        }
    }
}
