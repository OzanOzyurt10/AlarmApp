using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlarmApp
{


    [Table("Ayarlar")]
    public class AyarKaydiEntity
    {
        
        public int UyduId { get; set; }
        public string PaketAdi { get; set; }
        public string PropertyAdi { get; set; }

        public string TipAdi { get; set; }
        public string Karsilastirma { get; set; }
        public string EsikDeger { get; set; }
        public string MaxEsikDeger { get; set; }
        public bool AlarmAktif { get; set; }
        public string Kritiklik { get; set; } = "Orta";
    }

    [Table("SistemLoglari")]
    public class SistemLogKaydiEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int UyduId { get; set; }
        public string Zaman { get; set; }
        public string ParametreAdi { get; set; }
        public string Mesaj { get; set; }
    }

    [Table("MantiksalKurallar")]
    public class MantiksalKuralKaydiEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string KuralJson { get; set; }
    }

    [Table("Uydular")]
    public class UyduKaydiEntity
    {
        public int UyduId { get; set; }
    }

    [Table("Paketler")]
    public class PaketKaydiEntity
    {
        // Bileşik anahtar (UyduId, PaketAdi) -> AlarmDbContext.OnModelCreating içinde tanımlanır.
        public int UyduId { get; set; }
        public string PaketAdi { get; set; }
    }
}
