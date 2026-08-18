using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AlarmApp
{
    public partial class MantikIslemleri : Window
    {
        private ObservableCollection<Uydu> _mevcutUydular;
        private MainViewModel _viewModel;
        public ObservableCollection<BirlesikMantiksalKural> KombinasyonListesi { get; set; } = new ObservableCollection<BirlesikMantiksalKural>();

        // Pencere açılır açılmaz sağdaki listeye otomatik dolmasın diye,
        // veri tabanından/önceki oturumdan gelen senaryolar burada bekletilir.
        // Kullanıcı "Veritabanından Çek" butonuna basınca (ya da kaydederken,
        // veri kaybı olmasın diye güvenlik amacıyla) KombinasyonListesi'ne aktarılır.
        private readonly ObservableCollection<BirlesikMantiksalKural> _eskiKurallarKaynak;
        private bool _veritabaniSenaryolariYuklendi = false;

        public MantikIslemleri(ObservableCollection<Uydu> uydular, ObservableCollection<BirlesikMantiksalKural> eskiKurallar, MainViewModel viewModel)
        {
            InitializeComponent();
            _mevcutUydular = uydular;
            _viewModel = viewModel;
            _eskiKurallarKaynak = eskiKurallar;

            lstKombinasyonlar.ItemsSource = KombinasyonListesi;
            lstKombinasyonlar.SelectionMode = SelectionMode.Multiple;

            DoldurUyduComboBox();
            DoldurUyduComboBox2();

            KosulOnizlemesiniGuncelle();

        }

        // Sağdaki "Oluşturulan Alarm Senaryoları" listesini veri tabanından
        // (ve halihazırda aktif olan tek parametreli alarmlardan) doldurur.
        // Birden fazla kez çağrılsa bile senaryolar tekrar eklenmez.
        private void VeritabaniSenaryolariniYukle()
        {
            if (_veritabaniSenaryolariYuklendi)
                return;

            if (_eskiKurallarKaynak != null)
            {
                foreach (var kural in _eskiKurallarKaynak)
                {
                    KombinasyonListesi.Add(new BirlesikMantiksalKural
                    {
                        Baglac = kural.Baglac,
                        BirlesikMetin = kural.BirlesikMetin,
                        IcerilenKosullar = kural.IcerilenKosullar.ToList(),
                        IsTriggered = kural.IsTriggered,
                        Kritiklik = kural.Kritiklik
                    });
                }
            }

            // Halihazırda aktif olan tek parametreli alarmları da sağdaki
            // "Oluşturulan Alarm Senaryoları" listesine ekle. Böylece yeni
            // oluşturulan matematiksel (veya herhangi bir) alarm, mevcut
            // alarmlarla aynı listeden VE/VEYA/YADA ile birleştirilebilir.
            MevcutTekliAlarmlariListeyeEkle();

            _veritabaniSenaryolariYuklendi = true;
        }

        private void btnVeritabanindanCek_Click(object sender, RoutedEventArgs e)
        {
            VeritabaniSenaryolariniYukle();

            pnlSenaryoYerTutucu.Visibility = Visibility.Collapsed;

            btnVeritabanindanCek.IsEnabled = false;
            btnVeritabanindanCek.Content = "✓  Senaryolar Yüklendi";
        }

        // ================= ADIM ADIM CANLI ÖNİZLEME =================
        // Kullanıcı "Koşul Oluştur" bölümündeki alanları doldurdukça
        // (uydu/paket/parametre, kritiklik, operatör, eşik değeri,
        // mutlak değer, matematiksel işlem vb.) bu metot çağrılır ve
        // sağdaki "Alarm Önizlemesi" kutusu anlık olarak güncellenir.
        private void KosulOnizlemesiniGuncelle()
        {
            if (txtOnizleme == null)
                return;

            var item = cmbSolVeri?.SelectedItem as ComboBoxItem;
            var parametre = item?.Tag as ParametreBase;

            if (parametre == null)
            {
                txtOnizleme.Text = "Yukarıdaki adımları tamamladığınızda oluşacak alarm kuralı burada özetlenecektir.";
                return;
            }

            // ----- SOL TARAF (tekli parametre ya da matematiksel ifade) -----
            string solTaraf;
            bool matematikselMi = chkMatematik?.IsChecked == true;

            if (matematikselMi)
            {
                var item2 = cmbSolVeri2?.SelectedItem as ComboBoxItem;
                var parametre2 = item2?.Tag as ParametreBase;
                var secilenMatOperator = cmbMatOperator?.SelectedItem as ComboBoxItem;
                string matTag = secilenMatOperator?.Tag?.ToString();
                string matSimge = secilenMatOperator?.Content?.ToString() ?? "?";

                if (parametre2 == null)
                {
                    solTaraf = $"Uydu {parametre.UyduId} · {parametre.Ad}  (+ ikinci parametre bekleniyor…)";
                }
                else if (matTag == "MutlakFark")
                {
                    solTaraf = $"|Uydu {parametre.UyduId}.{parametre.Ad} − Uydu {parametre2.UyduId}.{parametre2.Ad}|";
                }
                else
                {
                    solTaraf = $"(Uydu {parametre.UyduId}.{parametre.Ad} {matSimge} Uydu {parametre2.UyduId}.{parametre2.Ad})";
                }
            }
            else
            {
                bool mutlakMi = chkMutlakDeger?.IsChecked == true;
                solTaraf = mutlakMi
                    ? $"|Uydu {parametre.UyduId}.{parametre.Ad}|"
                    : $"Uydu {parametre.UyduId}.{parametre.Ad}";
            }

            // ----- KARŞILAŞTIRMA OPERATÖRÜ -----
            var secilenOperator = cmbKarsilastirma?.SelectedItem as ComboBoxItem;
            string opMetni = secilenOperator?.Content?.ToString() ?? "…";
            string opTag = secilenOperator?.Tag?.ToString();

            // ----- EŞİK / DURUM DEĞERİ -----
            string esikMetni;
            if (parametre.TipAdi == "Boolean")
            {
                var secBool = cmbBoolean?.SelectedItem as ComboBoxItem;
                esikMetni = secBool?.Content?.ToString() ?? "…";
            }
            else if (opTag == "Arasindadir" || opTag == "Disindadir")
            {
                string min = string.IsNullOrWhiteSpace(txtEsik?.Text) ? "…" : txtEsik.Text.Trim();
                string max = string.IsNullOrWhiteSpace(txtMaxEsik?.Text) ? "…" : txtMaxEsik.Text.Trim();
                esikMetni = $"{min} - {max}";
            }
            else
            {
                esikMetni = string.IsNullOrWhiteSpace(txtEsik?.Text) ? "…" : txtEsik.Text.Trim();
            }

            // ----- KRİTİKLİK -----
            var secilenKritiklik = cmbKritiklik?.SelectedItem as ComboBoxItem;
            string kritiklikHam = secilenKritiklik?.Content?.ToString() ?? "Orta";
            var parcalar = kritiklikHam.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string kritiklikSade = parcalar.Length > 0 ? parcalar[parcalar.Length - 1] : kritiklikHam;

            txtOnizleme.Text = $"Eğer {solTaraf} {opMetni} {esikMetni} ise {kritiklikSade.ToUpper()} alarm oluşturulacak.";
        }

        private void MevcutTekliAlarmlariListeyeEkle()
        {
            if (_mevcutUydular == null)
                return;

            // Zaten bir kombinasyonun içinde yer alan (tek parametreli)
            // koşulları tekrar eklememek için önce onları not al.
            var kombinasyondakiParametreler = new HashSet<(int, string, string)>(
                KombinasyonListesi
                    .SelectMany(k => k.IcerilenKosullar)
                    .Where(k => !k.MatematikselMi)
                    .Select(k => (k.UyduId, k.PaketAdi, k.ParametreAdi)));

            foreach (var uydu in _mevcutUydular)
            {
                foreach (var p in uydu.Parametreler.Where(p => p.AlarmAktif))
                {
                    if (kombinasyondakiParametreler.Contains((p.UyduId, p.PaketAdi, p.Ad)))
                        continue;

                    var mevcutKosul = new MantiksalKosul
                    {
                        UyduId = p.UyduId,
                        PaketAdi = p.PaketAdi,
                        ParametreAdi = p.Ad,
                        Karsilastirma = p.Karsilastirma,
                        EsikDeger = p.EsikDegerObje?.ToString() ?? "",
                        MaxEsikDeger = p.MaxEsikDegerObje?.ToString() ?? "",
                        MatematikselMi = false,
                        MevcutAlarmMi = true,
                        Kritiklik = p.Kritiklik
                    };

                    KombinasyonListesi.Add(new BirlesikMantiksalKural
                    {
                        Baglac = "TEK",
                        IcerilenKosullar = new List<MantiksalKosul> { mevcutKosul },
                        BirlesikMetin = mevcutKosul.KosulMetni,
                        Kritiklik = p.Kritiklik
                    });
                }
            }
        }

        private void DoldurUyduComboBox()
        {
            cmbUydu.Items.Clear();

            foreach (var uydu in _mevcutUydular.OrderBy(u => u.UyduId))
            {
                cmbUydu.Items.Add(uydu.UyduId);
            }

            if (cmbUydu.Items.Count > 0)
            {
                cmbUydu.SelectedIndex = 0;
            }
        }


        private void cmbUydu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cmbPaket.Items.Clear();
            cmbSolVeri.Items.Clear();

            if (cmbUydu.SelectedItem == null)
                return;

            int seciliUyduId = (int)cmbUydu.SelectedItem;

            var uydu = _mevcutUydular.FirstOrDefault(u => u.UyduId == seciliUyduId);

            if (uydu == null)
                return;

            var paketler = uydu.Paketler.Values
                .OrderBy(p => p.PaketAdi);

            foreach (var paket in paketler)
            {
                cmbPaket.Items.Add(paket.PaketAdi);
            }

            if (cmbPaket.Items.Count > 0)
            {
                cmbPaket.SelectedIndex = 0;
            }

            KosulOnizlemesiniGuncelle();
        }

        private void cmbPaket_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cmbSolVeri.Items.Clear();

            if (cmbUydu.SelectedItem == null ||
                cmbPaket.SelectedItem == null)
                return;

            int seciliUyduId = (int)cmbUydu.SelectedItem;
            string seciliPaket = cmbPaket.SelectedItem.ToString();

            var uydu = _mevcutUydular.FirstOrDefault(u => u.UyduId == seciliUyduId);

            if (uydu == null)
                return;

            var parametreler = uydu.Parametreler.Where(p => p.PaketAdi == seciliPaket).OrderBy(p => p.Ad);

            foreach (var p in parametreler)
            {
                cmbSolVeri.Items.Add(new ComboBoxItem
                {
                    Content = p.Ad,
                    Tag = p
                });
            }

            if (cmbSolVeri.Items.Count > 0)
            {
                cmbSolVeri.SelectedIndex = 0;
            }

            KosulOnizlemesiniGuncelle();
        }

        private void cmbSolVeri_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = cmbSolVeri.SelectedItem as ComboBoxItem;

            if (item == null)
                return;

            var parametre = item.Tag as ParametreBase;

            if (parametre == null)
                return;

            bool booleanMi = parametre.TipAdi == "Boolean";

            lblEsik.Visibility = booleanMi
                ? Visibility.Collapsed
                : Visibility.Visible;

            txtEsik.Visibility = booleanMi
                ? Visibility.Collapsed
                : Visibility.Visible;

            lblEsik.Visibility = booleanMi ? Visibility.Collapsed : Visibility.Visible;
            txtEsik.Visibility = booleanMi ? Visibility.Collapsed : Visibility.Visible;

            lblBoolean.Visibility = booleanMi ? Visibility.Visible : Visibility.Collapsed;
            cmbBoolean.Visibility = booleanMi ? Visibility.Visible : Visibility.Collapsed;

            chkMutlakDeger.Visibility = booleanMi ? Visibility.Collapsed : Visibility.Visible;
            if (booleanMi)
                chkMutlakDeger.IsChecked = false;

            if (booleanMi)
                cmbBoolean.SelectedIndex = 0;

            if (parametre == null)
                return;

            cmbKarsilastirma.Items.Clear();

            if (parametre.TipAdi == "Boolean")
            {
                cmbKarsilastirma.Items.Add(new ComboBoxItem
                {
                    Content = "==",
                    Tag = "Esittir"
                });
            }
            else
            {
                cmbKarsilastirma.Items.Add(new ComboBoxItem
                {
                    Content = ">",
                    Tag = "Buyuktur"
                });

                cmbKarsilastirma.Items.Add(new ComboBoxItem
                {
                    Content = "<",
                    Tag = "Kucuktur"
                });

                cmbKarsilastirma.Items.Add(new ComboBoxItem
                {
                    Content = "==",
                    Tag = "Esittir"
                });

                cmbKarsilastirma.Items.Add(new ComboBoxItem
                {
                    Content = ">=",
                    Tag = "BuyukEsittir"
                });

                cmbKarsilastirma.Items.Add(new ComboBoxItem
                {
                    Content = "<=",
                    Tag = "KucukEsittir"
                });

                cmbKarsilastirma.Items.Add(new ComboBoxItem
                {
                    Content = "Arasında",
                    Tag = "Arasindadir"
                });

                cmbKarsilastirma.Items.Add(new ComboBoxItem
                {
                    Content = "Dışında",
                    Tag = "Disindadir"
                });
            }

            cmbKarsilastirma.SelectedIndex = 0;

            KosulOnizlemesiniGuncelle();

        }


        private void DoldurUyduComboBox2()
        {
            cmbUydu2.Items.Clear();

            foreach (var uydu in _mevcutUydular.OrderBy(u => u.UyduId))
            {
                cmbUydu2.Items.Add(uydu.UyduId);
            }

            if (cmbUydu2.Items.Count > 0)
            {
                cmbUydu2.SelectedIndex = 0;
            }
        }

        private void chkMatematik_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool acikMi = chkMatematik.IsChecked == true;

            pnlMatematik.Visibility = acikMi ? Visibility.Visible : Visibility.Collapsed;

            // Mutlak değer seçeneği tek parametreli koşullar içindir; matematik
            // işlemi açıkken bunun yerine "| |  (Mutlak Fark)" işlemi kullanılır.
            chkMutlakDeger.IsEnabled = !acikMi;
            if (acikMi)
                chkMutlakDeger.IsChecked = false;

            if (acikMi && cmbMatOperator.SelectedItem == null)
            {
                cmbMatOperator.SelectedIndex = 0;
            }

            KosulOnizlemesiniGuncelle();
        }

        private void cmbUydu2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cmbPaket2.Items.Clear();
            cmbSolVeri2.Items.Clear();

            if (cmbUydu2.SelectedItem == null)
                return;

            int seciliUyduId = (int)cmbUydu2.SelectedItem;

            var uydu = _mevcutUydular.FirstOrDefault(u => u.UyduId == seciliUyduId);

            if (uydu == null)
                return;

            var paketler = uydu.Paketler.Values
                .OrderBy(p => p.PaketAdi);

            foreach (var paket in paketler)
            {
                cmbPaket2.Items.Add(paket.PaketAdi);
            }

            if (cmbPaket2.Items.Count > 0)
            {
                cmbPaket2.SelectedIndex = 0;
            }

            KosulOnizlemesiniGuncelle();
        }

        private void cmbPaket2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cmbSolVeri2.Items.Clear();

            if (cmbUydu2.SelectedItem == null ||
                cmbPaket2.SelectedItem == null)
                return;

            int seciliUyduId = (int)cmbUydu2.SelectedItem;
            string seciliPaket = cmbPaket2.SelectedItem.ToString();

            var uydu = _mevcutUydular.FirstOrDefault(u => u.UyduId == seciliUyduId);

            if (uydu == null)
                return;

            var parametreler = uydu.Parametreler.Where(p => p.PaketAdi == seciliPaket).OrderBy(p => p.Ad);

            foreach (var p in parametreler)
            {
                cmbSolVeri2.Items.Add(new ComboBoxItem
                {
                    Content = p.Ad,
                    Tag = p
                });
            }

            if (cmbSolVeri2.Items.Count > 0)
            {
                cmbSolVeri2.SelectedIndex = 0;
            }

            KosulOnizlemesiniGuncelle();
        }

        private void cmbSolVeri2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            KosulOnizlemesiniGuncelle();
        }

        private void cmbMatOperator_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            KosulOnizlemesiniGuncelle();
        }

        private void cmbBoolean_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            KosulOnizlemesiniGuncelle();
        }

        private void cmbKritiklik_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            KosulOnizlemesiniGuncelle();
        }

        private void chkMutlakDeger_CheckedChanged(object sender, RoutedEventArgs e)
        {
            KosulOnizlemesiniGuncelle();
        }

        private void txtEsik_TextChanged(object sender, TextChangedEventArgs e)
        {
            KosulOnizlemesiniGuncelle();
        }

        private void txtMaxEsik_TextChanged(object sender, TextChangedEventArgs e)
        {
            KosulOnizlemesiniGuncelle();
        }


        private void cmbKarsilastirma_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lblMaxEsik == null ||
                txtMaxEsik == null ||
                lblEsik == null)
                return;


            var secilen =
                cmbKarsilastirma.SelectedItem
                as ComboBoxItem;


            if (secilen == null)
                return;


            string tag =
                secilen.Tag?.ToString();


            bool ciftEsik =
                tag == "Arasindadir" ||
                tag == "Disindadir";


            if (ciftEsik)
            {

                lblEsik.Text =
                    "Minimum Eşik Değeri";


                lblMaxEsik.Text =
                    "Maksimum Eşik Değeri";


                lblMaxEsik.Visibility =
                    Visibility.Visible;

                txtMaxEsik.Visibility =
                    Visibility.Visible;
            }
            else
            {

                lblEsik.Text =
                    "Eşik Değeri";


                lblMaxEsik.Visibility =
                    Visibility.Collapsed;

                txtMaxEsik.Visibility =
                    Visibility.Collapsed;
            }

            KosulOnizlemesiniGuncelle();
        }








        public void btnBagla_Click(object sender, RoutedEventArgs e)
        {
            var seciliSenaryolar = lstKombinasyonlar.SelectedItems.Cast<BirlesikMantiksalKural>().ToList();

            if (seciliSenaryolar.Count < 2)
            {
                MessageBox.Show(
                    "Birleştirmek için sağdaki listeden en az 2 alarm senaryosu seçilmeli.",
                    "Sistem",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string baglac = (sender as Button)?.Tag.ToString();

            // Seçilen senaryolardan farklı bir bağlaçla oluşturulmuş
            // olanlar varsa kullanıcıyı bilgilendir; birleştirme sonrasında
            // tüm koşullar tek bir bağlaç altında değerlendirilecek.
            if (seciliSenaryolar.Any(k => k.Baglac != baglac))
            {
                var onay = MessageBox.Show(
                    $"Seçtiğiniz senaryolardan bazıları farklı bir bağlaç ile oluşturulmuş. " +
                    $"Birleştirildiğinde tüm koşullar tek bir '{baglac}' bağlacı altında değerlendirilecek. Devam edilsin mi?",
                    "Bağlaç Uyarısı",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (onay != MessageBoxResult.Yes)
                    return;
            }

            var birlesenKosullar = new List<MantiksalKosul>();
            foreach (var senaryo in seciliSenaryolar)
            {
                birlesenKosullar.AddRange(senaryo.IcerilenKosullar);
            }

            string birlesikMetin = "(" + string.Join(
                $" {baglac} ",
                seciliSenaryolar.Select(k => k.BirlesikMetin)) + ")";

            // Birleştirilen senaryolardan en kritik (en yüksek) olan seviye,
            // yeni oluşan birleşik kuralın önem derecesi olarak alınır.
            var enYuksekKritiklik = seciliSenaryolar
                .Select(k => k.Kritiklik)
                .DefaultIfEmpty(AlarmKritiklik.Orta)
                .Max();

            KombinasyonListesi.Add(new BirlesikMantiksalKural
            {
                Baglac = baglac,
                IcerilenKosullar = birlesenKosullar,
                BirlesikMetin = birlesikMetin,
                Kritiklik = enYuksekKritiklik
            });

            // Birleştirilen senaryolar artık yeni kuralın içinde yer
            // aldığından, ayrı bir senaryo olarak listede tekrar gösterilmez.
            foreach (var senaryo in seciliSenaryolar)
            {
                KombinasyonListesi.Remove(senaryo);
            }

            lstKombinasyonlar.UnselectAll();
        }

        public void btnKombinasyonSil_Click(object sender, RoutedEventArgs e)
        {
            var kural = (sender as Button)?.Tag as BirlesikMantiksalKural;
            if (kural != null) KombinasyonListesi.Remove(kural);
        }


        public void btnKaydet_Click(
            object sender,
            RoutedEventArgs e)
        {
            // Kullanıcı sağdaki "Veritabanından Çek" butonuna hiç basmadan
            // kaydetmeye çalışırsa, veri tabanındaki mevcut senaryoların
            // silinmemesi için burada arka planda çekilir.
            VeritabaniSenaryolariniYukle();

            if (KombinasyonListesi.Count == 0)
            {
                MessageBox.Show(
                    "Lütfen en az bir alarm koşulu ekleyin.",
                    "Eksik Bilgi",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            // Tek başına duran, matematiksel olmayan basit koşullar
            // doğrudan "tek parametreli" alarm olarak uygulanır; geri kalan
            // her şey (matematiksel koşullar ve VE/VEYA/YADA ile
            // birleştirilmiş senaryolar) çok parametreli kural olarak
            // kaydedilir.
            var tekliUygulanacaklar = KombinasyonListesi
                .Where(k =>
                    k.Baglac == "TEK" &&
                    k.IcerilenKosullar.Count == 1 &&
                    !k.IcerilenKosullar[0].MatematikselMi)
                .ToList();

            var kaydedilecekKombinasyonlar = KombinasyonListesi
                .Except(tekliUygulanacaklar)
                .ToList();


            // TEK PARAMETRELİ ALARMLARI DOĞRUDAN UYGULA
            foreach (var kural in tekliUygulanacaklar)
            {
                var kosul = kural.IcerilenKosullar[0];

                var uydu =
                    _mevcutUydular.FirstOrDefault(
                        u => u.UyduId == kosul.UyduId);

                var parametre =
                    uydu?.Parametreler.FirstOrDefault(
                        p =>
                            p.PaketAdi == kosul.PaketAdi &&
                            p.Ad == kosul.ParametreAdi);

                if (parametre == null)
                    continue;

                parametre.Karsilastirma =
                    kosul.Karsilastirma;

                parametre.EsikDegerObje =
                    kosul.EsikDeger;

                parametre.MaxEsikDegerObje =
                    kosul.MaxEsikDeger;

                parametre.EsikGirildiMi =
                    true;

                parametre.AlarmAktif =
                    true;

                parametre.Kritiklik =
                    kosul.Kritiklik;

                parametre.ControlEtMetodu();

                VeritabaniYardimcisi.AyarlariKaydet(
                    new List<ParametreBase>
                    {
                        parametre
                    });
            }

            // Uygulanan tekli alarmlar artık kendi kartlarında (Tek
            // Parametreli) görüneceğinden, ayrı bir senaryo olarak listede
            // tutulmazlar.
            foreach (var kural in tekliUygulanacaklar)
            {
                KombinasyonListesi.Remove(kural);
            }


            // ÇOK PARAMETRELİ / MATEMATİKSEL SENARYOLARI KAYDET
            if (kaydedilecekKombinasyonlar.Count > 0)
            {
                // Bir senaryonun içine alınan, daha önce tek başına aktif
                // olan alarmları "tekli alarm" olarak tekrar gösterilmemesi
                // için pasif hale getir; artık çok parametreli senaryonun
                // bir parçasılar.
                var etkilenenParametreler = new List<ParametreBase>();

                foreach (var kural in kaydedilecekKombinasyonlar)
                {
                    foreach (var kosul in kural.IcerilenKosullar.Where(
                        k => k.MevcutAlarmMi && !k.MatematikselMi))
                    {
                        var uydu = _mevcutUydular.FirstOrDefault(
                            u => u.UyduId == kosul.UyduId);

                        var parametre = uydu?.Parametreler.FirstOrDefault(
                            p => p.PaketAdi == kosul.PaketAdi &&
                                 p.Ad == kosul.ParametreAdi);

                        if (parametre != null && parametre.AlarmAktif)
                        {
                            parametre.AlarmAktif = false;
                            etkilenenParametreler.Add(parametre);
                        }
                    }
                }

                if (etkilenenParametreler.Count > 0)
                {
                    VeritabaniYardimcisi.AyarlariKaydet(
                        etkilenenParametreler);
                }
            }

            VeritabaniYardimcisi.MantiksalKurallariKaydet(
                kaydedilecekKombinasyonlar);

            _viewModel.MantiksalKurallariDenetle();


            _viewModel.AlarmKartlariniGuncelle();

            DialogResult = true;

            Close();
        }
        public void btnIptal_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        public void btnKosulEkle_Click(object sender, RoutedEventArgs e)
        {
            var item =
                cmbSolVeri.SelectedItem as ComboBoxItem;

            if (item == null)
            {
                MessageBox.Show("Lütfen bir parametre seçin.");
                return;
            }

            var parametre =
                item.Tag as ParametreBase;

            if (parametre == null)
                return;


            // MATEMATİKSEL İŞLEM (İKİNCİ PARAMETRE)
            bool matematikselMi = chkMatematik.IsChecked == true;
            ParametreBase parametre2 = null;
            MatematikOperatoru matOperator = default;

            if (matematikselMi)
            {
                var item2 = cmbSolVeri2.SelectedItem as ComboBoxItem;

                if (item2 == null)
                {
                    MessageBox.Show("Lütfen ikinci parametreyi seçin.");
                    return;
                }

                parametre2 = item2.Tag as ParametreBase;

                if (parametre2 == null)
                    return;

                var secilenMatOperator = cmbMatOperator.SelectedItem as ComboBoxItem;

                if (secilenMatOperator == null)
                {
                    MessageBox.Show("Lütfen matematiksel işlemi seçin.");
                    return;
                }

                matOperator = (MatematikOperatoru)Enum.Parse(
                    typeof(MatematikOperatoru),
                    secilenMatOperator.Tag.ToString());

                if (parametre2.UyduId == parametre.UyduId &&
                    parametre2.PaketAdi == parametre.PaketAdi &&
                    parametre2.Ad == parametre.Ad)
                {
                    MessageBox.Show("İkinci parametre birinci parametre ile aynı olamaz.");
                    return;
                }
            }


            var secilenOperator =
                cmbKarsilastirma.SelectedItem as ComboBoxItem;

            if (secilenOperator == null)
            {
                MessageBox.Show("Lütfen karşılaştırma seçin.");
                return;
            }


            KarsilastirmaTuru tur =
                (KarsilastirmaTuru)Enum.Parse(
                    typeof(KarsilastirmaTuru),
                    secilenOperator.Tag.ToString());


            string esikDegeri;


            // BOOLEAN
            if (parametre.TipAdi == "Boolean")
            {
                var seciliBoolean =
                    cmbBoolean.SelectedItem as ComboBoxItem;

                if (seciliBoolean == null)
                {
                    MessageBox.Show(
                        "Lütfen True veya False seçin.");

                    return;
                }

                esikDegeri =
                    seciliBoolean.Content.ToString();
            }


            // NORMAL DEĞER
            else
            {
                if (string.IsNullOrWhiteSpace(txtEsik.Text))
                {
                    MessageBox.Show(
                        "Lütfen eşik değeri girin.");

                    return;
                }

                esikDegeri =
                    txtEsik.Text.Trim();
            }


            string maxEsikDegeri = "";


            // MUTLAK DEĞER (tek parametreli koşullarda)
            bool mutlakDegerMi = !matematikselMi && chkMutlakDeger.IsChecked == true;


            // ÇİFT EŞİK GEREKEN DURUMLAR
            if (tur == KarsilastirmaTuru.Arasindadir ||
                tur == KarsilastirmaTuru.Disindadir)
            {
                if (string.IsNullOrWhiteSpace(
                    txtMaxEsik.Text))
                {
                    MessageBox.Show(
                        "Lütfen maksimum eşik değerini girin.");

                    return;
                }

                maxEsikDegeri =
                    txtMaxEsik.Text.Trim();
            }


            // AYNI KOŞULU AYNI LİSTEYE TEKRAR EKLEME
            var tumMevcutKosullar = KombinasyonListesi
                .SelectMany(k => k.IcerilenKosullar);

            bool zatenVarMi = matematikselMi
                ? tumMevcutKosullar.Any(k =>
                    k.MatematikselMi &&
                    k.UyduId == parametre.UyduId &&
                    k.PaketAdi == parametre.PaketAdi &&
                    k.ParametreAdi == parametre.Ad &&
                    k.Uydu2Id == parametre2.UyduId &&
                    k.Paket2Adi == parametre2.PaketAdi &&
                    k.Parametre2Adi == parametre2.Ad &&
                    k.MatematikOperatoru == matOperator)
                : tumMevcutKosullar.Any(k =>
                    !k.MatematikselMi &&
                    k.UyduId == parametre.UyduId &&
                    k.PaketAdi == parametre.PaketAdi &&
                    k.ParametreAdi == parametre.Ad &&
                    k.Karsilastirma == tur &&
                    k.EsikDeger == esikDegeri &&
                    k.MaxEsikDeger == maxEsikDegeri &&
                    k.MutlakDegerMi == mutlakDegerMi);


            if (zatenVarMi)
            {
                MessageBox.Show(
                    "Bu koşul (aynı parametre, aynı karşılaştırma ve aynı eşik değeriyle) zaten koşul listesine eklenmiş.");

                return;
            }


            var secilenKritiklik = cmbKritiklik.SelectedItem as ComboBoxItem;

            var kritiklik = secilenKritiklik?.Tag != null
                ? (AlarmKritiklik)Enum.Parse(typeof(AlarmKritiklik), secilenKritiklik.Tag.ToString())
                : AlarmKritiklik.Orta;

            var yeniKosul =
                new MantiksalKosul
                {
                    UyduId = parametre.UyduId,

                    PaketAdi = parametre.PaketAdi,

                    ParametreAdi = parametre.Ad,

                    Karsilastirma = tur,

                    EsikDeger = esikDegeri,

                    MaxEsikDeger = maxEsikDegeri,

                    MatematikselMi = matematikselMi,

                    Uydu2Id = matematikselMi ? parametre2.UyduId : 0,

                    Paket2Adi = matematikselMi ? parametre2.PaketAdi : null,

                    Parametre2Adi = matematikselMi ? parametre2.Ad : null,

                    MatematikOperatoru = matematikselMi ? matOperator : default,

                    MutlakDegerMi = mutlakDegerMi,

                    Kritiklik = kritiklik,
                };


            // Koşul, "Koşul Oluştur" bölümünde birikmek yerine doğrudan
            // sağdaki "Oluşturulan Alarm Senaryoları" listesine, tek başına
            // uygulanabilir bir senaryo olarak eklenir. Buradan diğer
            // senaryolarla VE / VEYA / YADA ile birleştirilebilir, ya da tek
            // başına seçiliyken "Uygula" ile doğrudan alarm haline getirilebilir.
            KombinasyonListesi.Add(new BirlesikMantiksalKural
            {
                Baglac = "TEK",
                IcerilenKosullar = new List<MantiksalKosul> { yeniKosul },
                BirlesikMetin = yeniKosul.KosulMetni,
                Kritiklik = kritiklik
            });




            txtEsik.Clear();
            txtMaxEsik.Clear();
            chkMutlakDeger.IsChecked = false;
            cmbKritiklik.SelectedIndex = 1;

            KosulOnizlemesiniGuncelle();
        }




    }



    public class MantiksalKosul
    {
        public string ParametreAdi { get; set; }

        public KarsilastirmaTuru Karsilastirma { get; set; }

        public string EsikDeger { get; set; }

        public string MaxEsikDeger { get; set; }

        public int UyduId { get; set; }

        public string PaketAdi { get; set; }

        // Matematiksel (çift parametreli) koşul bilgileri.
        // Örn: Uydu1.Hiz - Uydu2.Nem > 0
        public bool MatematikselMi { get; set; }

        public int Uydu2Id { get; set; }

        public string Paket2Adi { get; set; }

        public string Parametre2Adi { get; set; }

        public MatematikOperatoru MatematikOperatoru { get; set; }

        // Tek parametreli (matematiksel olmayan) koşullarda, karşılaştırmadan
        // önce parametrenin mutlak değerinin alınıp alınmayacağı. Örn:
        // |Uydu1.Paket1.Sicaklik| > 5
        public bool MutlakDegerMi { get; set; }

        // Bu koşul, pencere açıldığında halihazırda aktif olan tek
        // parametreli bir alarmdan otomatik olarak mı eklendi? (Mevcut
        // alarmları başka alarmlarla birleştirebilme özelliği için)
        public bool MevcutAlarmMi { get; set; }

        // Alarmın önem derecesi. Kullanıcı koşulu eklerken seçer.
        public AlarmKritiklik Kritiklik { get; set; } = AlarmKritiklik.Orta;

        // Sadece görüntüleme/yardımcı amaçlı; System.Text.Json soyut (abstract)
        // tipleri deserialize edemediğinden veritabanına yazılmaz.
        [JsonIgnore]
        public AlarmIfadesi Ifade { get; set; }


        private static string OperatorSimgesi(KarsilastirmaTuru tur)
        {
            switch (tur)
            {
                case KarsilastirmaTuru.Buyuktur: return ">";
                case KarsilastirmaTuru.Kucuktur: return "<";
                case KarsilastirmaTuru.Esittir: return "==";
                case KarsilastirmaTuru.BuyukEsittir: return ">=";
                case KarsilastirmaTuru.KucukEsittir: return "<=";
                case KarsilastirmaTuru.Arasindadir: return "Arasında";
                case KarsilastirmaTuru.Disindadir: return "Dışında";
                default: return "?";
            }
        }

        private static string MatematikOperatoruSimgesi(MatematikOperatoru op)
        {
            switch (op)
            {
                case MatematikOperatoru.Topla: return "+";
                case MatematikOperatoru.Cikar: return "-";
                case MatematikOperatoru.Carp: return "×";
                case MatematikOperatoru.Bol: return "÷";
                case MatematikOperatoru.MutlakFark: return "|Δ|";
                default: return "?";
            }
        }

        // Koşulun karşılaştırma/eşik kısmı olmadan sadece sol tarafını (parametre
        // ya da matematiksel ifade) gösterir. Çoklu alarm düzenleme penceresinde
        // her koşul satırının başlığı olarak kullanılır.
        [JsonIgnore]
        public string SolTarafMetni
        {
            get
            {
                if (MatematikselMi)
                {
                    return MatematikOperatoru == MatematikOperatoru.MutlakFark
                        ? $"|Uydu{UyduId}.{ParametreAdi} - Uydu{Uydu2Id}.{Parametre2Adi}|"
                        : $"(Uydu{UyduId}.{ParametreAdi} {MatematikOperatoruSimgesi(MatematikOperatoru)} Uydu{Uydu2Id}.{Parametre2Adi})";
                }

                return MutlakDegerMi
                    ? $"|Uydu{UyduId}.{ParametreAdi}|"
                    : $"Uydu{UyduId}.{ParametreAdi}";
            }
        }

        public string KosulMetni
        {
            get
            {
                string op = OperatorSimgesi(Karsilastirma);

                string solTaraf = SolTarafMetni;

                string on = MevcutAlarmMi ? "🔔 " : "";

                if (Karsilastirma == KarsilastirmaTuru.Arasindadir ||
                    Karsilastirma == KarsilastirmaTuru.Disindadir)
                {
                    return $"{on}[{solTaraf} {op} {EsikDeger} - {MaxEsikDeger}]";
                }

                return $"{on}[{solTaraf} {op} {EsikDeger}]";
            }
        }

    }

    public class BirlesikMantiksalKural
    {
        public string Baglac { get; set; }
        public List<MantiksalKosul> IcerilenKosullar { get; set; } = new List<MantiksalKosul>();
        public string BirlesikMetin { get; set; }

        public bool IsTriggered { get; set; }

        // Alarmın önem derecesi. Birden fazla koşul birleştirildiğinde
        // (VE/VEYA/YADA) en yüksek seviye esas alınır.
        public AlarmKritiklik Kritiklik { get; set; } = AlarmKritiklik.Orta;

        [JsonIgnore]
        public string DurumMetni
        {
            get
            {
                return IsTriggered ? "🟢 AKTİF" : "⚪ PASİF";
            }
        }
        [JsonIgnore]
        public Brush DurumRengi
        {
            get
            {
                return IsTriggered ? Brushes.LimeGreen : Brushes.Gray;
            }
        }

        [JsonIgnore]
        public string KritiklikMetni
        {
            get
            {
                switch (Kritiklik)
                {
                    case AlarmKritiklik.Dusuk: return "DÜŞÜK";
                    case AlarmKritiklik.Orta: return "ORTA";
                    case AlarmKritiklik.Yuksek: return "YÜKSEK";
                    case AlarmKritiklik.Kritik: return "KRİTİK";
                    default: return "ORTA";
                }
            }
        }

        [JsonIgnore]
        public Brush KritiklikRengi
        {
            get
            {
                switch (Kritiklik)
                {
                    case AlarmKritiklik.Dusuk: return new SolidColorBrush(Color.FromRgb(0x0E, 0xA5, 0xE9));
                    case AlarmKritiklik.Orta: return new SolidColorBrush(Color.FromRgb(0xEA, 0xB3, 0x08));
                    case AlarmKritiklik.Yuksek: return new SolidColorBrush(Color.FromRgb(0xF9, 0x73, 0x16));
                    case AlarmKritiklik.Kritik: return new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26));
                    default: return new SolidColorBrush(Color.FromRgb(0xEA, 0xB3, 0x08));
                }
            }
        }
    }
}