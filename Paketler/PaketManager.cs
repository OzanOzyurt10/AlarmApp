using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AlarmApp.Paketler
{
    public class PaketManager
    {
        private readonly Dictionary<string, PaketBase> _paketler = new Dictionary<string, PaketBase>();
        private readonly Dictionary<int, Uydu> _uydular = new Dictionary<int, Uydu>();
        public IReadOnlyDictionary<int, Uydu> Uydular => _uydular;

        public PaketManager()
        {
            PaketleriYukle();


        }

        private void PaketleriYukle()
        {
            var tipler = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(PaketBase).IsAssignableFrom(t) && !t.IsAbstract);

            foreach (var tip in tipler)
            {
                PaketBase paket = (PaketBase)Activator.CreateInstance(tip);

                _paketler[paket.PaketAdi] = paket;
            }
        }

        public IEnumerable<PaketBase> TumPaketler()
        {
            return _paketler.Values;
        }

        public object GetValue(string paketAdi, string propertyAdi)
        {
            PaketBase paket;

            if (!_paketler.TryGetValue(paketAdi, out paket))
                return null;

            PropertyInfo property = paket.GetType().GetProperty(propertyAdi);

            if (property == null)
                return null;

            return property.GetValue(paket);
        }

        public bool SetValue(string paketAdi, string propertyAdi, object value)
        {
            PaketBase paket;

            if (!_paketler.TryGetValue(paketAdi, out paket))
                return false;

            PropertyInfo property = paket.GetType().GetProperty(propertyAdi);

            if (property == null)
                return false;

            object donusenDeger = Convert.ChangeType(value, property.PropertyType);

            property.SetValue(paket, donusenDeger);

            return true;
        }

        public List<ParametreBase> ParametreleriOlustur(PaketBase paket)
        {
            List<ParametreBase> liste = new List<ParametreBase>();

            PropertyInfo[] propertyler = paket.GetType().GetProperties();

            foreach (PropertyInfo property in propertyler)
            {
                if (property.Name == nameof(PaketBase.PaketAdi) ||property.Name == nameof(PaketBase.UyduId))
                {
                    continue;
                }

                Type genericType = typeof(Parametre<>).MakeGenericType(property.PropertyType);

                ParametreBase parametre = (ParametreBase)Activator.CreateInstance(genericType);

                parametre.Ad = paket.PaketAdi + "." + property.Name;
                parametre.PaketAdi = paket.PaketAdi;
                parametre.PropertyAdi = property.Name;
                parametre.TipAdi = property.PropertyType.Name;

              
                parametre.UyduId = paket.UyduId;

                parametre.Paket = paket;
                parametre.Property = property;

                liste.Add(parametre);
            }

            return liste;
        }

        public PaketBase GetOrCreatePaket(int uyduId, string paketAdi)
        {
            // Uydu yoksa oluştur
            if (!_uydular.TryGetValue(uyduId, out Uydu uydu))
            {
                uydu = new Uydu(uyduId);
                _uydular.Add(uyduId, uydu);
            }

            // Paket varsa geri döndür
            if (uydu.Paketler.TryGetValue(paketAdi, out PaketBase paket))
            {
                return paket;
            }

            // Paket yoksa oluştur
            Type tip = Assembly.GetExecutingAssembly()
                .GetTypes()
                .FirstOrDefault(t =>
                    typeof(PaketBase).IsAssignableFrom(t) &&
                    !t.IsAbstract &&
                    ((PaketBase)Activator.CreateInstance(t)).PaketAdi == paketAdi);

            if (tip == null)
                return null;

            paket = (PaketBase)Activator.CreateInstance(tip);
            paket.UyduId = uyduId;

            uydu.Paketler.Add(paketAdi, paket);

            return paket;
        }
    }
}