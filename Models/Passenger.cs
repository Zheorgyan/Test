using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WpfApp.Core;

namespace WpfApp.Models
{
    [DisplayName("Пассажиры")]
    [AllowEdit("ROLE_KASSIR,ROLE_ADMIN")]
    public partial class Passenger : DataObjectBase {
        public int Id { get; set; }

        [Display(Name = "Фамилия", Order = 10)]
        [Required]
        public string Surname { get; set; }

        [Display(Name = "Имя", Order = 20)]
        [Required]
        public string Name { get; set; }

        [Display(Name = "Отчество", Order = 30)]
        public string Fathername { get; set; }

        [Display(Name = "Номер паспорта", Order = 50)]
        [Required]
        public string PassportNumber { get; set; }

        [Display(Name = "Серия паспорта", Order = 40)]
        [Required]
        public string PassportSeria { get; set; }

        public virtual ICollection<Ticket> Tickets { get; set; } = new HashSet<Ticket>();

        public override string ToString() {
            return string.Format("{0} {1} {2} паспорт {3} {4}", Surname, Name, Fathername, PassportSeria, PassportNumber);
        }
    }
}
