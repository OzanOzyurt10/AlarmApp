using System.Data.Entity;

namespace AlarmApp
{
   
    // Şema (tablo) oluşturma  artık ham SQL ile değil, EF6 Code-First
    
    public class AlarmDbContext : DbContext
    {
        
        public AlarmDbContext() : base("name=AlarmDbContext")
        {
        }

        public DbSet<AyarKaydiEntity> Ayarlar { get; set; }
        public DbSet<SistemLogKaydiEntity> SistemLoglari { get; set; }
        public DbSet<MantiksalKuralKaydiEntity> MantiksalKurallar { get; set; }
        public DbSet<UyduKaydiEntity> Uydular { get; set; }
        public DbSet<PaketKaydiEntity> Paketler { get; set; }

        static AlarmDbContext()
        {
            // Tablolar ham SQL ile DEĞİL, EF6 Code-First (Automatic) Migrations
            // ile oluşturuluyor/güncelleniyor. Ayrıntı: Migrations/Configuration.cs
            Database.SetInitializer(
                new MigrateDatabaseToLatestVersion<AlarmDbContext, AlarmApp.Migrations.Configuration>());
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AyarKaydiEntity>()
                .ToTable("Ayarlar")
                .HasKey(a => new { a.UyduId, a.PaketAdi, a.PropertyAdi });

            modelBuilder.Entity<SistemLogKaydiEntity>()
                .ToTable("SistemLoglari");

            modelBuilder.Entity<MantiksalKuralKaydiEntity>()
                .ToTable("MantiksalKurallar");

            modelBuilder.Entity<UyduKaydiEntity>()
                .ToTable("Uydular")
                .HasKey(u => u.UyduId);

            modelBuilder.Entity<PaketKaydiEntity>()
                .ToTable("Paketler")
                .HasKey(p => new { p.UyduId, p.PaketAdi });

            base.OnModelCreating(modelBuilder);
        }
    }
}
