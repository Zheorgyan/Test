using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WpfApp.Core;
using Microsoft.EntityFrameworkCore;

namespace WpfApp.Models
{
    [DisplayName("Водитель автобуса")]
    [AllowEdit("ROLE_DISPATCHER,ROLE_ADMIN")]
    public partial class BusDriver : DataObjectBase {
        public int Id { get; set; }
        public int DriverId { get; set; }
        public int BusId { get; set; }
        public DateTime RouteDate { get; set; }

        [Display(Name ="Автобус", Order =10)]
        [Required]
        [ReadOnly(true)]
        [Visibility(FieldVisibility.Form)]
        public virtual Bus Bus { get; set; }

        [Display(Name = "Водитель", Order = 20)]
        [Required]
        public virtual Driver Driver { get; set; }

        public override void InitNewObject(DbContext dataContext) {
            base.InitNewObject(dataContext);
            RouteDate = DateTime.Today;
        }
    }
}
