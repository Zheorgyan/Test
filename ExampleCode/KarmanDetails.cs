
using System.ComponentModel.DataAnnotations;


namespace Entities
{
    public class KarmanDetails
    {
        [Display(Name = "Диаметр")]
        public double DIAMETER { get; set; }//--Диаметр
        [Display(Name = "Толщина")]
        public double THICKNESS { get; set; }//--Толщина
        [Display(Name = "Марка стали")]
        public string STAL { get; set; }//--Марка стали
        [Display(Name = "НТД")]
        public string GOST { get; set; }//--НТД
        [Display(Name = "Количество труб")]
        public int COUNT_PIPES { get; set; }//--Количество труб
        public string MNEMO_NAME { get; set; }//--мнемосхема
        public string STACK_NAME { get; set; }//--штабель
        public int POCKET_NUM { get; set; }//--карман
    }

    public class PipeNumbers
    {
        public string PipeNumber { get; set; }//--Номера труб
    }
}