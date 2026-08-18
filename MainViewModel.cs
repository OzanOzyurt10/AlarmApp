using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using AlarmApp.Paketler;
using System.Reflection;

namespace AlarmApp
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ParametreBase> Parametreler { get; set; } = new ObservableCollection<ParametreBase>();
        public ObservableCollection<string> SistemLoglari { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<BirlesikMantiksalKural> KombinasyonListesi { get; set; } = new ObservableCollection<BirlesikMantiksalKural>();

        private Stack<List<ParametreBase>> _mementoHistory = new Stack<List<ParametreBase>>();

        public ObservableCollection<AlarmKartModel> TekParametreliAlarmKartlari { get; } = new ObservableCollection<AlarmKartModel>();

        public ObservableCollection<AlarmKartModel> CokParametreliAlarmKartlari { get; } = new ObservableCollection<AlarmKartModel>();

        public bool AlarmVarMi
        {
            get
            {
                if (AlarmGorunumu == AlarmGorunumTipi.TekParametreli)
                    return TekParametreliAlarmKartlari.Count > 0;

                if (AlarmGorunumu == AlarmGorunumTipi.CokParametreli)
                    return CokParametreliAlarmKartlari.Count > 0;

                return TekParametreliAlarmKartlari.Count > 0 ||
                       CokParametreliAlarmKartlari.Count > 0;
            }
        }
        public static bool IsLoading { get; set; } = true;

        private PaketManager _paketManager = new PaketManager();



        public ObservableCollection<Uydu> Uydular { get; } = new ObservableCollection<Uydu>();
        public IEnumerable<ParametreBase> TumParametreler
        {
            get
            {
                return Uydular.SelectMany(u => u.Parametreler);
            }
        }



        private Uydu _seciliUydu;

        public Uydu SeciliUydu
        {
            get => _seciliUydu;
            set
            {
                _seciliUydu = value;
                OnPropertyChanged();

                if (_seciliUydu != null)
                {
                    YukleSeciliUydu(_seciliUydu.UyduId);
                    AlarmKartlariniGuncelle();
                }
            }
        }

        public enum AlarmGorunumTipi
        {
            TekParametreli,
            CokParametreli,
            TumunuGoster
        }

        private AlarmGorunumTipi _alarmGorunumu =
            AlarmGorunumTipi.TumunuGoster;

        public AlarmGorunumTipi AlarmGorunumu
        {
            get => _alarmGorunumu;

            set
            {
                _alarmGorunumu = value;

                OnPropertyChanged();

                OnPropertyChanged(nameof(TekAlarmlarGorunur));
                OnPropertyChanged(nameof(CokAlarmlarGorunur));
                OnPropertyChanged(nameof(AlarmVarMi));
            }
        }

        public bool TekAlarmlarGorunur =>
            AlarmGorunumu == AlarmGorunumTipi.TekParametreli ||
            AlarmGorunumu == AlarmGorunumTipi.TumunuGoster;

        public bool CokAlarmlarGorunur =>
            AlarmGorunumu == AlarmGorunumTipi.CokParametreli ||
            AlarmGorunumu == AlarmGorunumTipi.TumunuGoster;

        private void YukleSeciliUydu(int uyduId)
        {
            Parametreler.Clear();

            if (uyduId == Uydu.TumuId)
            {
                foreach (var u in _paketManager.Uydular.Values)
                {
                    foreach (var p in u.Parametreler)
                    {
                        Parametreler.Add(p);
                    }
                }

                UpdateAlarmCount();
                return;
            }

            if (!_paketManager.Uydular.TryGetValue(uyduId, out var uydu))
                return;

            foreach (var p in uydu.Parametreler)
            {
                Parametreler.Add(p);
            }

            UpdateAlarmCount();
        }

        public void AlarmKartlariniGuncelle()
        {
            TekParametreliAlarmKartlari.Clear();
            CokParametreliAlarmKartlari.Clear();

            //---------------------------------------
            // TEK PARAMETRELİ ALARMLAR
            //---------------------------------------

            var parametreler =
                _paketManager.Uydular
                             .SelectMany(x => x.Value.Parametreler);

            if (SeciliUydu != null && !SeciliUydu.TumunuTemsilEdiyorMu)
            {
                parametreler =
                    parametreler.Where(p =>
                        p.UyduId == SeciliUydu.UyduId);
            }

            var tekliKartlar = new List<AlarmKartModel>();

            foreach (var p in parametreler)
            {
                if (!p.AlarmAktif)
                    continue;
                tekliKartlar.Add(
                    new AlarmKartModel
                    {
                        MantiksalMi = false,

                        AlarmTuru = "Tek Parametreli",

                        UyduId = p.UyduId,

                        PaketAdi = p.PaketAdi,

                        ParametreAdi = p.PropertyAdi,

                        Baslik = p.Ad,

                        Aciklama = p.AlarmKurali,

                        AnlikDeger =
                            p.AnlikDegerObje?.ToString(),

                        Aktif = p.UyariTetiklendi,

                        Kritiklik = p.Kritiklik
                    });
            }

            // Halihazırda tetiklenmiş olan alarmlar en üstte, ardından en
            // kritik seviyeden en düşüğe doğru sıralanır. Böylece kullanıcı
            // en acil durumu her zaman ilk bakışta görür.
            foreach (var kart in tekliKartlar
                .OrderByDescending(k => k.Aktif)
                .ThenByDescending(k => k.Kritiklik)
                .ThenBy(k => k.Baslik))
            {
                TekParametreliAlarmKartlari.Add(kart);
            }


            //---------------------------------------
            // ÇOK PARAMETRELİ ALARMLAR
            //---------------------------------------

            var cokluKartlar = new List<AlarmKartModel>();

            foreach (var kural in KombinasyonListesi)
            {
                if (kural.IcerilenKosullar == null ||
                    !kural.IcerilenKosullar.Any())
                {
                    continue;
                }


                bool seciliUyduBelirli =
                    SeciliUydu != null && !SeciliUydu.TumunuTemsilEdiyorMu;

                if (seciliUyduBelirli)
                {
                    bool buUyduyaAitMi =
                        kural.IcerilenKosullar
                             .Any(k =>
                                 k.UyduId == SeciliUydu.UyduId ||
                                 (k.MatematikselMi &&
                                  k.Uydu2Id == SeciliUydu.UyduId));

                    if (!buUyduyaAitMi)
                        continue;
                }

                cokluKartlar.Add(
                     new AlarmKartModel
                     {
                         MantiksalMi = true,

                         AlarmTuru = "Çok Parametreli",

                         UyduId =
                             seciliUyduBelirli
                             ? SeciliUydu.UyduId
                             : kural.IcerilenKosullar
                                  .First()
                                  .UyduId,

                         Baslik = "Çok Parametreli Alarm",

                         Aciklama = kural.BirlesikMetin,

                         AnlikDeger = "",

                         Aktif = kural.IsTriggered,

                         MantiksalKural = kural,

                         Kritiklik = kural.Kritiklik
                     });
            }

            
            foreach (var kart in cokluKartlar
                .OrderByDescending(k => k.Aktif)
                .ThenByDescending(k => k.Kritiklik)
                .ThenBy(k => k.Baslik))
            {
                CokParametreliAlarmKartlari.Add(kart);
            }

            OnPropertyChanged(nameof(AlarmVarMi));
        }



        private bool _isDirty;
        public bool IsDirty
        {
            get => _isDirty;
            set { _isDirty = value; OnPropertyChanged(); }
        }

        private int _aktifAlarmSayisi;
        public int AktifAlarmSayisi
        {
            get => _aktifAlarmSayisi;
            set { _aktifAlarmSayisi = value; OnPropertyChanged(); }
        }

        private bool _sesliAlarmAktif;
        public bool SesliAlarmAktif
        {
            get => _sesliAlarmAktif;
            set { _sesliAlarmAktif = value; OnPropertyChanged(); }
        }

        // ComboBox'ta "Tümü" seçeneği olarak gösterilen özel Uydu öğesi.
        private readonly Uydu _tumuUydusu = new Uydu(Uydu.TumuId);

        public MainViewModel(bool loadData)
        {
            Uydular.Add(_tumuUydusu);

            if (loadData)
            {
                LoadInitialTelemetryData();
            }
        }

        public MainViewModel() : this(true)
        {
        }

        private void LoadInitialTelemetryData()
        {
            IsLoading = true;


            try
            {
                VeritabaniYardimcisi.VeritabaniniHazirla();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"VERİTABANI BAŞLATMA HATASI:\n\n{ex.GetType().Name}: {ex.Message}\n\nInnerException: {ex.InnerException?.Message}\n\n{ex.StackTrace}",
                    "Tanı - DB Init Hatası");
            }



            var kayitliPaketler =
                VeritabaniYardimcisi.PaketleriGetir();

            foreach (var kayit in kayitliPaketler)
            {
                var paket = _paketManager.GetOrCreatePaket(
                    kayit.UyduId,
                    kayit.PaketAdi);

                if (paket == null)
                    continue;

                var uydu = _paketManager.Uydular[kayit.UyduId];

                if (!Uydular.Any(u => u.UyduId == kayit.UyduId))
                {
                    Uydular.Add(uydu);
                }

                var parametreler =
                    _paketManager.ParametreleriOlustur(paket);

                foreach (var p in parametreler)
                {
                    if (uydu.Parametreler.Any(x =>
                        x.PaketAdi == p.PaketAdi &&
                        x.PropertyAdi == p.PropertyAdi))
                    {
                        continue;
                    }

                    p.OnLogUretildi += Cihaz_OnLogUretildi;
                    p.PropertyChanged += Parametre_PropertyChanged;

                    uydu.Parametreler.Add(p);
                }
            }



            DbKayitliParametreleriYukle();



            var eskiLoglar =
                VeritabaniYardimcisi.EskiLoglariGetir();

            foreach (var log in eskiLoglar)
            {
                SistemLoglari.Add(log);
            }


            SeciliUydu = _tumuUydusu;



            if (SeciliUydu != null)
            {
                YukleSeciliUydu(SeciliUydu.UyduId);
            }



            foreach (var p in TumParametreler)
            {
                if (p.AlarmAktif && p.EsikGirildiMi)
                {
                    p.ControlEtMetodu();
                }
                else
                {
                    p.UyariTetiklendi = false;
                }
            }



            PushToHistory();

            UpdateAlarmCount();



            var dbKurallari =
                VeritabaniYardimcisi.MantiksalKurallariGetir();

            foreach (var k in dbKurallari)
            {
                KombinasyonListesi.Add(k);
            }



            IsLoading = false;

            MantiksalKurallariDenetle();


            IsDirty = false;


            Cihaz_OnLogUretildi(
                "SİSTEM",
                "Veritabanı bağlantısı kuruldu ve arayüz yüklendi.");
        }

        private void DbKayitliParametreleriYukle()
        {
            var dbKayitlari =
                VeritabaniYardimcisi.AyarlariGetir();

            foreach (var kayit in dbKayitlari)
            {
                var uydu =
                    Uydular.FirstOrDefault(
                        u => u.UyduId == kayit.UyduId);

                if (uydu == null)
                    continue;


                var parametre =
                    uydu.Parametreler.FirstOrDefault(p =>
                        p.PaketAdi == kayit.PaketAdi &&
                        p.PropertyAdi == kayit.PropertyAdi);

                if (parametre == null)
                    continue;



                if (!string.IsNullOrWhiteSpace(kayit.Karsilastirma))
                {
                    parametre.Karsilastirma =
                        (KarsilastirmaTuru)Enum.Parse(
                            typeof(KarsilastirmaTuru),
                            kayit.Karsilastirma);
                }



                parametre.EsikDegerObje =
                    string.IsNullOrWhiteSpace(kayit.EsikDeger)
                        ? null
                        : kayit.EsikDeger;

                parametre.MaxEsikDegerObje =
                    string.IsNullOrWhiteSpace(kayit.MaxEsikDeger)
                        ? null
                        : kayit.MaxEsikDeger;



                parametre.AlarmAktif = kayit.AlarmAktif;

                parametre.Kritiklik =
                    !string.IsNullOrWhiteSpace(kayit.Kritiklik) &&
                    Enum.TryParse(kayit.Kritiklik, out AlarmKritiklik kritiklik)
                        ? kritiklik
                        : AlarmKritiklik.Orta;



                parametre.EsikGirildiMi =
                    !string.IsNullOrWhiteSpace(kayit.EsikDeger);



                parametre.OnLogUretildi += Cihaz_OnLogUretildi;

                parametre.PropertyChanged += Parametre_PropertyChanged;
            }
        }

        private void Parametre_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ParametreBase.UyariTetiklendi))
            {
                UpdateAlarmCount();
                MantiksalKurallariDenetle();
            }
            else if (!IsLoading)
            {
                IsDirty = true;
            }
        }

        private void Cihaz_OnLogUretildi(string parametreAdi, string mesaj)
        {
            string zamanFormati =
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            var parametre =
                TumParametreler.FirstOrDefault(p =>
                    p.Ad == parametreAdi);

            if (parametre != null)
            {
                LogOlustur(
                    parametre.UyduId,
                    parametreAdi,
                    mesaj,
                    zamanFormati);
            }
            else
            {
                LogOlustur(
                    null,
                    parametreAdi,
                    mesaj,
                    zamanFormati);
            }
        }

        private void LogOlustur(
            int? uyduId,
            string parametreAdi,
            string mesaj,
            string zamanFormati)
        {
            string logSatiri;

            if (uyduId > 0)
            {
                logSatiri =
                    $"[{zamanFormati}] [UYDU-{uyduId}] " +
                    $"TELEMETRİ ({parametreAdi}): {mesaj}";
            }
            else
            {
                logSatiri =
                    $"[{zamanFormati}] [SİSTEM] " +
                    $"{parametreAdi}: {mesaj}";
            }

            VeritabaniYardimcisi.LogYaz(
                uyduId ?? 0,
                parametreAdi,
                mesaj,
                zamanFormati);

            Application.Current.Dispatcher.Invoke(() =>
            {
                SistemLoglari.Insert(0, logSatiri);
            });
        }

        public void UpdateAlarmCount()
        {
            IEnumerable<ParametreBase> parametreler =
                _paketManager.Uydular.SelectMany(x => x.Value.Parametreler);

            if (SeciliUydu != null && !SeciliUydu.TumunuTemsilEdiyorMu)
            {
                parametreler = parametreler.Where(p => p.UyduId == SeciliUydu.UyduId);
            }

            int tekParametreli = parametreler.Count(p => p.UyariTetiklendi);

            int cokParametreli;

            if (SeciliUydu != null && !SeciliUydu.TumunuTemsilEdiyorMu)
            {
                cokParametreli = KombinasyonListesi.Count(k =>
                    k.IsTriggered &&
                    k.IcerilenKosullar.Any(c =>
                        c.UyduId == SeciliUydu.UyduId ||
                        (c.MatematikselMi && c.Uydu2Id == SeciliUydu.UyduId)));
            }
            else
            {
                cokParametreli = KombinasyonListesi.Count(k => k.IsTriggered);
            }

            AktifAlarmSayisi = tekParametreli + cokParametreli;
        }

        private void PushToHistory()
        {
            var snapshot = Parametreler.Select(p => p.Clone()).ToList();
            _mementoHistory.Push(snapshot);
        }

        public void SaveSettings()
        {
            var result = MessageBox.Show(
                "Mevcut telemetri kısıt ayarlarını uygulamak ve veritabanına kaydetmek istediğinize emin misiniz?",
                "Ayarları Uygula",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            PushToHistory();

            foreach (var p in Parametreler)
            {
                p.AlarmAktif = p.EsikGirildiMi;
            }

            var kaydedilecekParametreler =
                Parametreler
                    .Where(p => p.EsikGirildiMi)
                    .ToList();

            VeritabaniYardimcisi.AyarlariKaydet(
                kaydedilecekParametreler);

            foreach (var p in Parametreler)
            {
                p.ControlEtMetodu();
            }

            AlarmKartlariniGuncelle();

            IsDirty = false;

            UpdateAlarmCount();

            MessageBox.Show(
                "Ayarlar veritabanına kaydedildi ve başarıyla uygulandı.",
                "Bilgi",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            if (AktifAlarmSayisi > 0 &&
                SesliAlarmAktif)
            {
                System.Media.SystemSounds.Exclamation.Play();
            }
        }

        public void UndoSettings()
        {
            if (_mementoHistory.Count > 1)
            {
                IsLoading = true;
                _mementoHistory.Pop();
                var previousState = _mementoHistory.Peek();

                for (int i = 0; i < Parametreler.Count; i++)
                {
                    Parametreler[i].EsikDegerObje = previousState[i].EsikDegerObje;
                    Parametreler[i].MaxEsikDegerObje = previousState[i].MaxEsikDegerObje;
                    Parametreler[i].Karsilastirma = previousState[i].Karsilastirma;
                    Parametreler[i].AnlikDegerObje = previousState[i].AnlikDegerObje;
                }

                VeritabaniYardimcisi.AyarlariKaydet(Parametreler);

                IsLoading = false;
                foreach (var p in Parametreler) p.ControlEtMetodu();

                IsDirty = false;
                UpdateAlarmCount();
            }
            else
            {
                MessageBox.Show("Geri alınabilecek daha eski bir konfigürasyon geçmişi yok.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public void MantiksalKurallariDenetle()
        {
            if (KombinasyonListesi == null || !KombinasyonListesi.Any()) return;

            foreach (var kural in KombinasyonListesi)
            {
                if (kural.IcerilenKosullar == null || kural.IcerilenKosullar.Count == 0) continue;

                List<bool> alarmDurumlari = new List<bool>();

                foreach (var kosul in kural.IcerilenKosullar)
                {
                    alarmDurumlari.Add(KosulSaglaniyorMu(kosul));
                }

                bool kuralSonucu = false;

                if (kural.Baglac == "VE")
                {
                    kuralSonucu = alarmDurumlari.All(d => d == true);
                }
                else if (kural.Baglac == "VEYA")
                {
                    kuralSonucu = alarmDurumlari.Any(d => d == true);
                }
                else if (kural.Baglac == "YADA")
                {
                    kuralSonucu = alarmDurumlari.Count(d => d == true) == 1;
                }
                else if (kural.Baglac == "TEK")
                {
                    kuralSonucu = alarmDurumlari.Count > 0 && alarmDurumlari[0];
                }

                if (kuralSonucu)
                {
                    if (!kural.IsTriggered)
                    {
                        Cihaz_OnLogUretildi("ALARM", $"Kombinasyon İhlali Gerçekleşti: {kural.BirlesikMetin}");
                        kural.IsTriggered = true;

                        if (SesliAlarmAktif)
                        {
                            System.Media.SystemSounds.Exclamation.Play();
                        }
                    }
                }
                else
                {
                    if (kural.IsTriggered)
                    {
                        Cihaz_OnLogUretildi("BİRLEŞİK DURUM", $"Kombinasyon Normale Döndü: {kural.BirlesikMetin}");
                        kural.IsTriggered = false;
                    }
                }
            }

            AlarmKartlariniGuncelle();
            UpdateAlarmCount();
        }

        private ParametreBase BulParametre(int uyduId, string paketAdi, string parametreAdi)
        {
            if (!_paketManager.Uydular.TryGetValue(uyduId, out var uydu))
                return null;

            return uydu.Parametreler
                        .FirstOrDefault(p => p.PaketAdi == paketAdi && p.Ad == parametreAdi);
        }

        private bool KosulSaglaniyorMu(MantiksalKosul kosul)
        {
            if (kosul.MatematikselMi)
                return MatematikselKosulSaglarMi(kosul);

            var parametre = BulParametre(kosul.UyduId, kosul.PaketAdi, kosul.ParametreAdi);

            return parametre != null && MantiksalKosuluSaglarMi(parametre, kosul);
        }

        private bool MatematikselKosulSaglarMi(MantiksalKosul kosul)
        {
            var parametre1 = BulParametre(kosul.UyduId, kosul.PaketAdi, kosul.ParametreAdi);
            var parametre2 = BulParametre(kosul.Uydu2Id, kosul.Paket2Adi, kosul.Parametre2Adi);

            if (parametre1?.AnlikDegerObje == null || parametre2?.AnlikDegerObje == null)
                return false;

            try
            {
                double deger1 = Convert.ToDouble(parametre1.AnlikDegerObje);
                double deger2 = Convert.ToDouble(parametre2.AnlikDegerObje);

                double sonuc;

                switch (kosul.MatematikOperatoru)
                {
                    case MatematikOperatoru.Topla:
                        sonuc = deger1 + deger2;
                        break;

                    case MatematikOperatoru.Cikar:
                        sonuc = deger1 - deger2;
                        break;

                    case MatematikOperatoru.Carp:
                        sonuc = deger1 * deger2;
                        break;

                    case MatematikOperatoru.Bol:
                        if (deger2 == 0)
                            return false;
                        sonuc = deger1 / deger2;
                        break;

                    case MatematikOperatoru.MutlakFark:
                        sonuc = Math.Abs(deger1 - deger2);
                        break;

                    default:
                        return false;
                }

                double esik = Convert.ToDouble(kosul.EsikDeger);

                switch (kosul.Karsilastirma)
                {
                    case KarsilastirmaTuru.Buyuktur:
                        return sonuc > esik;

                    case KarsilastirmaTuru.Kucuktur:
                        return sonuc < esik;

                    case KarsilastirmaTuru.Esittir:
                        return sonuc == esik;

                    case KarsilastirmaTuru.BuyukEsittir:
                        return sonuc >= esik;

                    case KarsilastirmaTuru.KucukEsittir:
                        return sonuc <= esik;

                    case KarsilastirmaTuru.Arasindadir:
                        {
                            double max = Convert.ToDouble(kosul.MaxEsikDeger);
                            return sonuc >= esik && sonuc <= max;
                        }

                    case KarsilastirmaTuru.Disindadir:
                        {
                            double max = Convert.ToDouble(kosul.MaxEsikDeger);
                            return sonuc < esik || sonuc > max;
                        }

                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private bool MantiksalKosuluSaglarMi(ParametreBase parametre, MantiksalKosul kosul)
        {
            if (parametre == null || parametre.AnlikDegerObje == null)
                return false;

            try
            {
                if (parametre.TipAdi == "Boolean")
                {
                    bool anlikBool = Convert.ToBoolean(parametre.AnlikDegerObje);
                    bool esikBool = Convert.ToBoolean(kosul.EsikDeger);

                    return anlikBool == esikBool;
                }


                double anlik = Convert.ToDouble(parametre.AnlikDegerObje);
                double min = Convert.ToDouble(kosul.EsikDeger);

                if (kosul.MutlakDegerMi)
                    anlik = Math.Abs(anlik);

                switch (kosul.Karsilastirma)
                {
                    case KarsilastirmaTuru.Buyuktur:
                        return anlik > min;

                    case KarsilastirmaTuru.Kucuktur:
                        return anlik < min;

                    case KarsilastirmaTuru.Esittir:
                        return anlik == min;

                    case KarsilastirmaTuru.BuyukEsittir:
                        return anlik >= min;

                    case KarsilastirmaTuru.KucukEsittir:
                        return anlik <= min;

                    case KarsilastirmaTuru.Arasindadir:
                        {
                            double max = Convert.ToDouble(kosul.MaxEsikDeger);
                            return anlik >= min && anlik <= max;
                        }

                    case KarsilastirmaTuru.Disindadir:
                        {
                            double max = Convert.ToDouble(kosul.MaxEsikDeger);
                            return anlik < min || anlik > max;
                        }

                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public void VeriGeldi(int uyduId, string paketAdi, string[] parcalar)
        {

            var paket = _paketManager.GetOrCreatePaket(uyduId, paketAdi);
            var uydu = _paketManager.Uydular[uyduId];

            VeritabaniYardimcisi.UyduKaydet(uyduId);
            VeritabaniYardimcisi.PaketKaydet(uyduId, paketAdi);



            if (!Uydular.Any(u => u.UyduId == uyduId))
            {
                Uydular.Add(uydu);

                VeritabaniYardimcisi.UyduKaydet(uyduId);

                if (SeciliUydu == null)
                    SeciliUydu = uydu;
            }


            if (!uydu.Parametreler.Any(p => p.PaketAdi == paketAdi))
            {
                foreach (var p in _paketManager.ParametreleriOlustur(paket))
                {
                    p.OnLogUretildi += Cihaz_OnLogUretildi;
                    p.PropertyChanged += Parametre_PropertyChanged;

                    uydu.Parametreler.Add(p);
                }
            }


            var parametreler = uydu.Parametreler.Where(p => p.PaketAdi == paketAdi).ToList();

            int adet = Math.Min(parametreler.Count, parcalar.Length - 2);

            for (int i = 0; i < adet; i++)
            {
                parametreler[i].AnlikDegerObje = parcalar[i + 2].Trim();
            }


            if (SeciliUydu != null &&
                (SeciliUydu.UyduId == uyduId || SeciliUydu.TumunuTemsilEdiyorMu))
            {
                YukleSeciliUydu(SeciliUydu.UyduId);
            }

            OnPropertyChanged(nameof(TumParametreler));

            UpdateAlarmCount();

            AlarmKartlariniGuncelle();


        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string p = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}