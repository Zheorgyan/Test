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
    [DisplayName("Билеты")]
    [ListForm(Title = "Журнал продажи билетов", FormType = typeof(TicketListWindow))]
    [AllowEdit("ROLE_KASSIR,ROLE_ADMIN")]
    public partial class Ticket : DataObjectBase {

        [Display(Name ="Номер билета", Order =1)]
        [ReadOnly(true)]
        [Visibility(FieldVisibility.List)]
        public int Id { get; set; }

        [Display(Name = "Дата", Order = 10)]
        [Required]
        public DateTime TicketDate { get; set; }

        [Display(Name = "Рейс", Order = 20)]
        [Required]
        public virtual BusRoute BusRoute { get; set; }

        [Display(Name = "Номер места", Order = 30)]
        [Required]
        public int PlaceNumber { get; set; }

        [Display(Name = "Цена, р.", Order = 40)]
        [Required]
        public decimal Price { get; set; }

        public int PassengerId { get; set; }

        public int KassirId { get; set; }        

        public int BusRouteId { get; set; }

        [Display(Name = "Кассир", Order = 50)]
        [Required]
        public virtual Kassir Kassir { get; set; }

        [Display(Name = "Пассажир", Order = 60)]
        [Required]
        public virtual Passenger Passenger { get; set; }

        public override void InitNewObject(DbContext dataContext) {
            base.InitNewObject(dataContext);
            TicketDate = DateTime.Today;
        }
    }
}
