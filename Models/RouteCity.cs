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
    [DisplayName("Остановка")]
    [AllowEdit("ROLE_DISPATCHER,ROLE_ADMIN")]
    public partial class RouteCity : DataObjectBase {
        public int Id { get; set; }
        public int RouteId { get; set; }
        public int CityId { get; set; }

        [Display(Name = "Маршрут", Order = 10)]
        [ReadOnly(true)]
        [Required]
        [Visibility(FieldVisibility.Form)]
        public virtual Route Route { get; set; }

        [Display(Name ="Порядок следования", Order =20)]
        [Required]
        [SortDirection(ListSortDirection.Descending)]
        public int RouteOrder { get; set; }

        [Display(Name = "Населенный пункт", Order = 30)]
        [Required]
        public virtual City City { get; set; }

    }
}
