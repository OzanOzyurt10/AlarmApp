using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;

namespace AlarmApp
{
    
    public static class VeritabaniYardimcisi
    {
        public static void VeritabaniniHazirla()
        {
            using (var db = new AlarmDbContext())
            {
                
                db.Database.Initialize(force: false);
            }
        }

        public static void PaketKaydet(int uyduId, string paketAdi)
        {
            using (var db = new AlarmDbContext())
            {
                bool zatenVar = db.Paketler.Any(p => p.UyduId == uyduId && p.PaketAdi == paketAdi);
                if (!zatenVar)
                {
                    db.Paketler.Add(new PaketKaydiEntity { UyduId = uyduId, PaketAdi = paketAdi });
                    db.SaveChanges();
                }
            }
        }

        public static List<(int UyduId, string PaketAdi)> PaketleriGetir()
        {
            using (var db = new AlarmDbContext())
            {
                var kayitlar = db.Paketler.OrderBy(p => p.UyduId).ThenBy(p => p.PaketAdi).Select(p => new { p.UyduId, p.PaketAdi }).ToList();

                return kayitlar.Select(p => (p.UyduId, p.PaketAdi)).ToList();
            }
        }

        public static void LogYaz(int uyduId, string parametreAdi, string mesaj, string zaman)
        {
            using (var db = new AlarmDbContext())
            {
                db.SistemLoglari.Add(new SistemLogKaydiEntity
                {
                    UyduId = uyduId,
                    Zaman = zaman,
                    ParametreAdi = parametreAdi,
                    Mesaj = mesaj
                });

                db.SaveChanges();
            }
        }

        public static List<string> EskiLoglariGetir()
        {
            using (var db = new AlarmDbContext())
            {
                var kayitlar = db.SistemLoglari.OrderByDescending(l => l.Id).Take(200).Select(l => new { l.UyduId, l.Zaman, l.ParametreAdi, l.Mesaj }).ToList();

                return kayitlar
                    .Select(l => $"[{l.Zaman}] [UYDU-{l.UyduId}] TELEMETRİ ({l.ParametreAdi}): {l.Mesaj}")
                    .ToList();
            }
        }

        public static List<ParametreKayitModel> AyarlariGetir()
        {
            using (var db = new AlarmDbContext())
            {
                return db.Ayarlar
                    .ToList()
                    .Select(a => new ParametreKayitModel
                    {
                        UyduId = a.UyduId,
                        PaketAdi = a.PaketAdi,
                        PropertyAdi = a.PropertyAdi,
                        TipAdi = a.TipAdi,
                        Karsilastirma = a.Karsilastirma,
                        EsikDeger = a.EsikDeger,
                        MaxEsikDeger = a.MaxEsikDeger,
                        AlarmAktif = a.AlarmAktif,
                        Kritiklik = string.IsNullOrWhiteSpace(a.Kritiklik) ? "Orta" : a.Kritiklik
                    })
                    .ToList();
            }
        }

        public static void AlarmSil(int uyduId, string paketAdi, string propertyAdi)
        {
            using (var db = new AlarmDbContext())
            {
                var kayit = db.Ayarlar.Find(uyduId, paketAdi, propertyAdi);
                if (kayit != null)
                {
                    db.Ayarlar.Remove(kayit);
                    db.SaveChanges();
                }
            }
        }

        public static void AyarlariKaydet(IEnumerable<ParametreBase> parametreler)
        {
            using (var db = new AlarmDbContext())
            using (var trans = db.Database.BeginTransaction())
            {
                try
                {
                    foreach (var p in parametreler)
                    {
                        var mevcutKayit = db.Ayarlar.Find(p.UyduId, p.PaketAdi, p.PropertyAdi);

                        if (mevcutKayit == null)
                        {
                            db.Ayarlar.Add(new AyarKaydiEntity
                            {
                                UyduId = p.UyduId,
                                PaketAdi = p.PaketAdi,
                                PropertyAdi = p.PropertyAdi,
                                TipAdi = p.TipAdi,
                                Karsilastirma = p.Karsilastirma.ToString(),
                                EsikDeger = p.EsikDegerObje?.ToString() ?? "",
                                MaxEsikDeger = p.MaxEsikDegerObje?.ToString() ?? "",
                                AlarmAktif = p.AlarmAktif,
                                Kritiklik = p.Kritiklik.ToString()
                            });
                        }
                        else
                        {
                            mevcutKayit.TipAdi = p.TipAdi;
                            mevcutKayit.Karsilastirma = p.Karsilastirma.ToString();
                            mevcutKayit.EsikDeger = p.EsikDegerObje?.ToString() ?? "";
                            mevcutKayit.MaxEsikDeger = p.MaxEsikDegerObje?.ToString() ?? "";
                            mevcutKayit.AlarmAktif = p.AlarmAktif;
                            mevcutKayit.Kritiklik = p.Kritiklik.ToString();
                        }
                    }

                    db.SaveChanges();
                    trans.Commit();
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        public static void AyarlariTemizle(int uyduId)
        {
            using (var db = new AlarmDbContext())
            {
                var kayitlar = db.Ayarlar.Where(a => a.UyduId == uyduId).ToList();
                db.Ayarlar.RemoveRange(kayitlar);
                db.SaveChanges();
            }
        }

        public static void MantiksalKurallariKaydet(IEnumerable<BirlesikMantiksalKural> kurallar)
        {
            try
            {
                using (var db = new AlarmDbContext())
                using (var trans = db.Database.BeginTransaction())
                {
                    try
                    {
                        db.MantiksalKurallar.RemoveRange(db.MantiksalKurallar);

                        foreach (var kural in kurallar)
                        {
                            string jsonMetni = JsonSerializer.Serialize(kural);
                            db.MantiksalKurallar.Add(new MantiksalKuralKaydiEntity { KuralJson = jsonMetni });
                        }

                        db.SaveChanges();
                        trans.Commit();
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"DB Kural Kayıt Hatası: {ex.Message}");
            }
        }

        public static void UyduKaydet(int uyduId)
        {
            using (var db = new AlarmDbContext())
            {
                bool zatenVar = db.Uydular.Any(u => u.UyduId == uyduId);
                if (!zatenVar)
                {
                    db.Uydular.Add(new UyduKaydiEntity { UyduId = uyduId });
                    db.SaveChanges();
                }
            }
        }

        public static List<int> UydulariGetir()
        {
            using (var db = new AlarmDbContext())
            {
                return db.Uydular.OrderBy(u => u.UyduId) .Select(u => u.UyduId).ToList();
            }
        }

        public static List<BirlesikMantiksalKural> MantiksalKurallariGetir()
        {
            var kurallar = new List<BirlesikMantiksalKural>();
            try
            {
                using (var db = new AlarmDbContext())
                {
                    var jsonListesi = db.MantiksalKurallar
                        .Select(k => k.KuralJson)
                        .ToList();

                    foreach (var json in jsonListesi)
                    {
                        if (!string.IsNullOrEmpty(json))
                        {
                            var kural = JsonSerializer.Deserialize<BirlesikMantiksalKural>(json);
                            if (kural != null)
                            {
                                kurallar.Add(kural);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"DB Kural Okuma Sırasında Hata Oluştu: {ex.Message}\n{ex.StackTrace}");
            }

            return kurallar;
        }

        public static void AyarlariUygula(ObservableCollection<ParametreBase> parametreler)
        {
            var dbKayitlari = AyarlariGetir();

            foreach (var kayit in dbKayitlari)
            {
                ParametreBase parametre = parametreler.FirstOrDefault(p =>
                    p.UyduId == kayit.UyduId &&
                    p.PaketAdi == kayit.PaketAdi &&
                    p.PropertyAdi == kayit.PropertyAdi);

                if (parametre == null)
                    continue;

                parametre.Karsilastirma =
                    (KarsilastirmaTuru)Enum.Parse(typeof(KarsilastirmaTuru), kayit.Karsilastirma);

                parametre.EsikDegerObje = kayit.EsikDeger;
                parametre.MaxEsikDegerObje = kayit.MaxEsikDeger;
                parametre.AlarmAktif = kayit.AlarmAktif;

                parametre.Kritiklik =
                    !string.IsNullOrWhiteSpace(kayit.Kritiklik) &&
                    Enum.TryParse(kayit.Kritiklik, out AlarmKritiklik kritiklik)
                        ? kritiklik
                        : AlarmKritiklik.Orta;
            }
        }
    }

    public class ParametreKayitModel
    {
        public int UyduId { get; set; }
        public string PaketAdi { get; set; }
        public string PropertyAdi { get; set; }
        public string TipAdi { get; set; }
        public string Karsilastirma { get; set; }
        public string EsikDeger { get; set; }
        public bool AlarmAktif { get; set; }
        public string MaxEsikDeger { get; set; }
        public string Kritiklik { get; set; }
    }
}
