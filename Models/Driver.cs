using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WpfApp.Core;

namespace WpfApp.Models
{
    [DisplayName("Водители")]
    [AllowEdit("ROLE_DISPATCHER,ROLE_ADMIN")]
    public partial class Driver : DataObjectBase {
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

        [Display(Name = "Номер ВУ", Order = 60)]
        [Required]
        public string DriverLicence { get; set; }

        public virtual ICollection<BusDriver> BusDrivers { get; set; } = new HashSet<BusDriver>();

        public override string ToString() {
            return string.Format("{0} {1} {2}", Surname, Name, Fathername);
        }
    }
}
