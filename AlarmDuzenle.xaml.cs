using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AlarmApp
{
   
    public partial class AlarmDuzenle : Window
    {
        private readonly bool _isBoolean;
        public AlarmKritiklik SeciliKritiklik { get; private set; }

        public KarsilastirmaTuru SeciliKarsilastirma { get; private set; }

        public string GirilenEsikDeger { get; private set; } = "";

        public string GirilenMaxEsikDeger { get; private set; } = "";

        public string SeciliBaglac { get; private set; } = "";

        private readonly bool _esikAlanlariGosterilsin;

        // Çoklu (birleşik) alarm düzenlemede, her bir koşul satırı için
        // oluşturulan kontrolleri ve ilgili MantiksalKosul referansını tutar.
        private class KosulSatiri
        {
            public MantiksalKosul Kosul;
            public bool IsBoolean;
            public ComboBox CmbKarsilastirma;
            public TextBox TxtEsik;
            public ComboBox CmbBooleanEsik;
            public TextBlock LblMax;
            public TextBox TxtMax;
        }

        private readonly bool _cokluKosulModu;
        private readonly List<KosulSatiri> _kosulSatirlari = new List<KosulSatiri>();

        public AlarmDuzenle(
            string altBaslik,
            AlarmKritiklik mevcutKritiklik,
            bool esikAlanlariGosterilsin = false,
            KarsilastirmaTuru mevcutKarsilastirma = KarsilastirmaTuru.Buyuktur,
            string mevcutEsikDeger = "",
            string mevcutMaxEsikDeger = "",
            bool isBoolean = false)
        {
            InitializeComponent();
            _isBoolean = isBoolean;
            txtAltBaslik.Text = altBaslik;

            _esikAlanlariGosterilsin = esikAlanlariGosterilsin;

            cmbKritiklik.Items.Add(new ComboBoxItem { Content = "ℹ  DÜŞÜK", Tag = AlarmKritiklik.Dusuk });
            cmbKritiklik.Items.Add(new ComboBoxItem { Content = "◆  ORTA", Tag = AlarmKritiklik.Orta });
            cmbKritiklik.Items.Add(new ComboBoxItem { Content = "▲  YÜKSEK", Tag = AlarmKritiklik.Yuksek });
            cmbKritiklik.Items.Add(new ComboBoxItem { Content = "⛔  KRİTİK", Tag = AlarmKritiklik.Kritik });
            cmbKritiklik.SelectedIndex = (int)mevcutKritiklik;

            if (esikAlanlariGosterilsin)
            {
                pnlEsikAyarlari.Visibility = Visibility.Visible;

                if (_isBoolean)
                {
                    cmbKarsilastirma.Items.Add(new ComboBoxItem
                    {
                        Content = "=   Eşittir",
                        Tag = KarsilastirmaTuru.Esittir
                    });
                }
                else
                {
                    cmbKarsilastirma.Items.Add(new ComboBoxItem { Content = ">   Büyüktür", Tag = KarsilastirmaTuru.Buyuktur });
                    cmbKarsilastirma.Items.Add(new ComboBoxItem { Content = "<   Küçüktür", Tag = KarsilastirmaTuru.Kucuktur });
                    cmbKarsilastirma.Items.Add(new ComboBoxItem { Content = "=   Eşittir", Tag = KarsilastirmaTuru.Esittir });
                    cmbKarsilastirma.Items.Add(new ComboBoxItem { Content = ">=  Büyük Eşittir", Tag = KarsilastirmaTuru.BuyukEsittir });
                    cmbKarsilastirma.Items.Add(new ComboBoxItem { Content = "<=  Küçük Eşittir", Tag = KarsilastirmaTuru.KucukEsittir });
                    cmbKarsilastirma.Items.Add(new ComboBoxItem { Content = "Arasında", Tag = KarsilastirmaTuru.Arasindadir });
                    cmbKarsilastirma.Items.Add(new ComboBoxItem { Content = "Dışında", Tag = KarsilastirmaTuru.Disindadir });
                }
                int secilecekIndex = 0;

                for (int i = 0; i < cmbKarsilastirma.Items.Count; i++)
                {
                    var item = (ComboBoxItem)cmbKarsilastirma.Items[i];

                    if ((KarsilastirmaTuru)item.Tag == mevcutKarsilastirma)
                    {
                        secilecekIndex = i;
                        break;
                    }
                }

                cmbKarsilastirma.SelectionChanged += (s, e) => MaxEsikGorunurlukGuncelle();
                cmbKarsilastirma.SelectedIndex = secilecekIndex;

                if (_isBoolean)
                {
                    txtEsikDeger.Visibility = Visibility.Collapsed;
                    cmbBooleanEsik.Visibility = Visibility.Visible;

                    cmbBooleanEsik.SelectedIndex =
                        mevcutEsikDeger == "True" ? 0 : 1;

                    txtMaxEsikBaslik.Visibility = Visibility.Collapsed;
                    txtMaxEsikDeger.Visibility = Visibility.Collapsed;
                }
                else
                {
                    txtEsikDeger.Text = mevcutEsikDeger;
                    txtMaxEsikDeger.Text = mevcutMaxEsikDeger;

                    MaxEsikGorunurlukGuncelle();
                }
            }
            else
            {
                pnlEsikAyarlari.Visibility = Visibility.Collapsed;
            }
        }

        // Çoklu (birleşik/mantıksal kural) alarm düzenleme. Kritiklik seviyesinin
        // yanında, kurala dahil her koşulun karşılaştırma türü ve eşik değer(ler)i
        // de ayrı ayrı düzenlenebilir hale gelir.
        public AlarmDuzenle(
            string altBaslik,
            AlarmKritiklik mevcutKritiklik,
            List<MantiksalKosul> kosullar,
            string mevcutBaglac,
            Func<MantiksalKosul, string> etiketSecici,
            Func<MantiksalKosul, bool> isBooleanSecici)
        {
            InitializeComponent();
            _cokluKosulModu = true;
            txtAltBaslik.Text = altBaslik;

            cmbKritiklik.Items.Add(new ComboBoxItem { Content = "ℹ  DÜŞÜK", Tag = AlarmKritiklik.Dusuk });
            cmbKritiklik.Items.Add(new ComboBoxItem { Content = "◆  ORTA", Tag = AlarmKritiklik.Orta });
            cmbKritiklik.Items.Add(new ComboBoxItem { Content = "▲  YÜKSEK", Tag = AlarmKritiklik.Yuksek });
            cmbKritiklik.Items.Add(new ComboBoxItem { Content = "⛔  KRİTİK", Tag = AlarmKritiklik.Kritik });
            cmbKritiklik.SelectedIndex = (int)mevcutKritiklik;

            pnlEsikAyarlari.Visibility = Visibility.Collapsed;
            pnlKosullar.Visibility = Visibility.Visible;

            if (kosullar == null)
                return;

            // Bağlaç seçimi yalnızca birden fazla koşul olduğunda anlamlıdır.
            if (kosullar.Count > 1)
            {
                pnlBaglac.Visibility = Visibility.Visible;

                cmbBaglac.Items.Add(new ComboBoxItem { Content = "VE  (tüm koşullar sağlanmalı)", Tag = "VE" });
                cmbBaglac.Items.Add(new ComboBoxItem { Content = "VEYA  (koşullardan en az biri sağlanmalı)", Tag = "VEYA" });
                cmbBaglac.Items.Add(new ComboBoxItem { Content = "YADA  (koşullardan sadece biri sağlanmalı)", Tag = "YADA" });

                int secilecekBaglacIndex = 0;

                for (int i = 0; i < cmbBaglac.Items.Count; i++)
                {
                    var item = (ComboBoxItem)cmbBaglac.Items[i];

                    if ((string)item.Tag == mevcutBaglac)
                    {
                        secilecekBaglacIndex = i;
                        break;
                    }
                }

                cmbBaglac.SelectedIndex = secilecekBaglacIndex;
            }

            foreach (var kosul in kosullar)
            {
                bool isBoolean = isBooleanSecici != null && isBooleanSecici(kosul);
                string etiket = etiketSecici != null ? etiketSecici(kosul) : kosul.SolTarafMetni;

                var satir = KosulSatiriOlustur(kosul, etiket, isBoolean);
                _kosulSatirlari.Add(satir);
            }
        }

        private KosulSatiri KosulSatiriOlustur(MantiksalKosul kosul, string etiket, bool isBoolean)
        {
            var satir = new KosulSatiri { Kosul = kosul, IsBoolean = isBoolean };

            var kapsayici = new StackPanel { Margin = new Thickness(0, 0, 0, 16) };

            kapsayici.Children.Add(new TextBlock
            {
                Text = etiket,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12.5,
                Margin = new Thickness(0, 0, 0, 8),
                TextWrapping = TextWrapping.Wrap
            });

            var cmbKarsilastirma = new ComboBox
            {
                Style = (Style)Application.Current.Resources["DarkComboBox"]
            };

            if (isBoolean)
            {
                cmbKarsilastirma.Items.Add(new ComboBoxItem { Content = "=   Eşittir", Tag = KarsilastirmaTuru.Esittir });
            }
            else
            {
                cmbKarsilastirma.Items.Add(new ComboBoxItem { Content = ">   Büyüktür", Tag = KarsilastirmaTuru.Buyuktur });
                cmbKarsilastirma.Items.Add(new ComboBoxItem { Content = "<   Küçüktür", Tag = KarsilastirmaTuru.Kucuktur });
                cmbKarsilastirma.Items.Add(new ComboBoxItem { Content = "=   Eşittir", Tag = KarsilastirmaTuru.Esittir });
                cmbKarsilastirma.Items.Add(new ComboBoxItem { Content = ">=  Büyük Eşittir", Tag = KarsilastirmaTuru.BuyukEsittir });
                cmbKarsilastirma.Items.Add(new ComboBoxItem { Content = "<=  Küçük Eşittir", Tag = KarsilastirmaTuru.KucukEsittir });
                cmbKarsilastirma.Items.Add(new ComboBoxItem { Content = "Arasında", Tag = KarsilastirmaTuru.Arasindadir });
                cmbKarsilastirma.Items.Add(new ComboBoxItem { Content = "Dışında", Tag = KarsilastirmaTuru.Disindadir });
            }

            int secilecekIndex = 0;

            for (int i = 0; i < cmbKarsilastirma.Items.Count; i++)
            {
                var item = (ComboBoxItem)cmbKarsilastirma.Items[i];

                if ((KarsilastirmaTuru)item.Tag == kosul.Karsilastirma)
                {
                    secilecekIndex = i;
                    break;
                }
            }

            kapsayici.Children.Add(cmbKarsilastirma);
            satir.CmbKarsilastirma = cmbKarsilastirma;

            var lblMax = new TextBlock
            {
                Text = "MAKS. EŞİK DEĞER",
                Foreground = (Brush)new BrushConverter().ConvertFromString("#64748B"),
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 14, 0, 6)
            };
            var txtMax = new TextBox { Text = kosul.MaxEsikDeger };

            if (isBoolean)
            {
                var cmbBoolean = new ComboBox
                {
                    Style = (Style)Application.Current.Resources["DarkComboBox"],
                    Height = 34,
                    Margin = new Thickness(0, 14, 0, 0)
                };
                cmbBoolean.Items.Add(new ComboBoxItem { Content = "True" });
                cmbBoolean.Items.Add(new ComboBoxItem { Content = "False" });
                cmbBoolean.SelectedIndex = kosul.EsikDeger == "True" ? 0 : 1;

                kapsayici.Children.Add(cmbBoolean);
                satir.CmbBooleanEsik = cmbBoolean;
            }
            else
            {
                var txtEsik = new TextBox
                {
                    Text = kosul.EsikDeger,
                    Margin = new Thickness(0, 14, 0, 0)
                };

                kapsayici.Children.Add(txtEsik);
                satir.TxtEsik = txtEsik;

                kapsayici.Children.Add(lblMax);
                kapsayici.Children.Add(txtMax);
                satir.LblMax = lblMax;
                satir.TxtMax = txtMax;

                bool maxGerekli =
                    kosul.Karsilastirma == KarsilastirmaTuru.Arasindadir ||
                    kosul.Karsilastirma == KarsilastirmaTuru.Disindadir;

                lblMax.Visibility = maxGerekli ? Visibility.Visible : Visibility.Collapsed;
                txtMax.Visibility = maxGerekli ? Visibility.Visible : Visibility.Collapsed;

                cmbKarsilastirma.SelectionChanged += (s, e) =>
                {
                    var secili = cmbKarsilastirma.SelectedItem as ComboBoxItem;
                    var tur = secili != null ? (KarsilastirmaTuru)secili.Tag : KarsilastirmaTuru.Buyuktur;

                    bool gerekli =
                        tur == KarsilastirmaTuru.Arasindadir ||
                        tur == KarsilastirmaTuru.Disindadir;

                    lblMax.Visibility = gerekli ? Visibility.Visible : Visibility.Collapsed;
                    txtMax.Visibility = gerekli ? Visibility.Visible : Visibility.Collapsed;
                };
            }

            cmbKarsilastirma.SelectedIndex = secilecekIndex;

            pnlKosullar.Children.Add(kapsayici);

            return satir;
        }

        private void MaxEsikGorunurlukGuncelle()
        {
            var secili = cmbKarsilastirma.SelectedItem as ComboBoxItem;

            var tur = secili != null
                ? (KarsilastirmaTuru)secili.Tag
                : KarsilastirmaTuru.Buyuktur;

            bool gerekli =
                tur == KarsilastirmaTuru.Arasindadir ||
                tur == KarsilastirmaTuru.Disindadir;

            txtMaxEsikBaslik.Visibility = gerekli ? Visibility.Visible : Visibility.Collapsed;
            txtMaxEsikDeger.Visibility = gerekli ? Visibility.Visible : Visibility.Collapsed;
        }

        private void btnKaydet_Click(object sender, RoutedEventArgs e)
        {
            var kritiklikItem = cmbKritiklik.SelectedItem as ComboBoxItem;

            SeciliKritiklik = kritiklikItem != null
                ? (AlarmKritiklik)kritiklikItem.Tag
                : AlarmKritiklik.Orta;

            if (_cokluKosulModu)
            {
                if (cmbBaglac.Visibility == Visibility.Visible)
                {
                    var baglacItem = cmbBaglac.SelectedItem as ComboBoxItem;
                    SeciliBaglac = baglacItem != null ? (string)baglacItem.Tag : "VE";
                }

                foreach (var satir in _kosulSatirlari)
                {
                    if (satir.IsBoolean)
                    {
                        if (satir.CmbBooleanEsik.SelectedItem == null)
                        {
                            MessageBox.Show(
                                "Lütfen tüm koşullar için True veya False seçiniz.",
                                "Eksik Bilgi",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);

                            return;
                        }
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(satir.TxtEsik.Text))
                        {
                            MessageBox.Show(
                                "Lütfen tüm koşullar için bir eşik değer giriniz.",
                                "Eksik Bilgi",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);

                            return;
                        }

                        var secili = satir.CmbKarsilastirma.SelectedItem as ComboBoxItem;
                        var tur = secili != null ? (KarsilastirmaTuru)secili.Tag : KarsilastirmaTuru.Buyuktur;

                        bool maxGerekli =
                            tur == KarsilastirmaTuru.Arasindadir ||
                            tur == KarsilastirmaTuru.Disindadir;

                        if (maxGerekli && string.IsNullOrWhiteSpace(satir.TxtMax.Text))
                        {
                            MessageBox.Show(
                                "Lütfen maksimum eşik değerini de giriniz.",
                                "Eksik Bilgi",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);

                            return;
                        }
                    }
                }

                foreach (var satir in _kosulSatirlari)
                {
                    var secili = satir.CmbKarsilastirma.SelectedItem as ComboBoxItem;

                    satir.Kosul.Karsilastirma = secili != null
                        ? (KarsilastirmaTuru)secili.Tag
                        : KarsilastirmaTuru.Buyuktur;

                    if (satir.IsBoolean)
                    {
                        satir.Kosul.EsikDeger =
                            ((ComboBoxItem)satir.CmbBooleanEsik.SelectedItem).Content.ToString();

                        satir.Kosul.MaxEsikDeger = "";
                    }
                    else
                    {
                        bool maxGerekli =
                            satir.Kosul.Karsilastirma == KarsilastirmaTuru.Arasindadir ||
                            satir.Kosul.Karsilastirma == KarsilastirmaTuru.Disindadir;

                        satir.Kosul.EsikDeger = satir.TxtEsik.Text.Trim();
                        satir.Kosul.MaxEsikDeger = maxGerekli ? satir.TxtMax.Text.Trim() : "";
                    }
                }

                DialogResult = true;
                Close();
                return;
            }

            if (_esikAlanlariGosterilsin)
            {
                var karsilastirmaItem = cmbKarsilastirma.SelectedItem as ComboBoxItem;

                SeciliKarsilastirma = karsilastirmaItem != null
                    ? (KarsilastirmaTuru)karsilastirmaItem.Tag
                    : KarsilastirmaTuru.Buyuktur;

                if (_isBoolean)
                {
                    if (cmbBooleanEsik.SelectedItem == null)
                    {
                        MessageBox.Show(
                            "Lütfen True veya False seçiniz.",
                            "Eksik Bilgi",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return;
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(txtEsikDeger.Text))
                    {
                        MessageBox.Show(
                            "Lütfen bir eşik değer giriniz.",
                            "Eksik Bilgi",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return;
                    }
                }

                bool maxGerekli =
                    SeciliKarsilastirma == KarsilastirmaTuru.Arasindadir ||
                    SeciliKarsilastirma == KarsilastirmaTuru.Disindadir;

                if (maxGerekli && string.IsNullOrWhiteSpace(txtMaxEsikDeger.Text))
                {
                    MessageBox.Show(
                        "Lütfen maksimum eşik değerini de giriniz.",
                        "Eksik Bilgi",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }
                if (_isBoolean)
                {
                    GirilenEsikDeger =
                        ((ComboBoxItem)cmbBooleanEsik.SelectedItem).Content.ToString();

                    GirilenMaxEsikDeger = "";
                }
                else
                {
                    GirilenEsikDeger = txtEsikDeger.Text.Trim();
                    GirilenMaxEsikDeger = maxGerekli ? txtMaxEsikDeger.Text.Trim() : "";
                }
            }

            DialogResult = true;
            Close();
        }

        private void btnIptal_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
