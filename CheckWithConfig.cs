using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace KNN
{
    class CheckWithConfig
    {
        public bool SprawdzDelimeter(Configuration config, string delimiter)
        {
            if (config.Separator != delimiter)
            {
                MessageBox.Show("Znak separatora nie jest zgodny ze znakiem podanym w pliku konfiguracyjnym," +
                                " powinien być: " + config.Separator + ", a jest: " + delimiter, "Błąd walidacji");
                return false;
            }
            return true;
        }
        public bool SprawdzLiczbeLinii(Configuration config, int liczbaLinii)
        {
            if (config.LiczbaLinii != liczbaLinii)
            {
                MessageBox.Show("Liczba linii nie jest zgodna z liczbą podaną w pliku konfiguracyjnym," +
                                " powinno być: " + config.LiczbaLinii + ", a jest: " + liczbaLinii, "Błąd walidacji");
                return false;
            }

            return true;
        }
        public bool SprawdzLiczbeKolumn(Configuration config, int liczbaKolumn)
        {
            if (config.LiczbaKolumn != liczbaKolumn)
            {
                MessageBox.Show("Liczba linii nie jest zgodna z liczbą podaną w pliku konfiguracyjnym," +
                                " powinno być: " + config.LiczbaKolumn + ", a jest: " + liczbaKolumn, "Błąd walidacji");
                return false;
            }
            return true;
        }
        public bool SprawdzZakresAtrybutow(Configuration config, List<List<string>> zakresAtrybutow)
        {
            try
            {
                for (int i = 0; i < config.LiczbaKolumn; i++)
                {
                    for (int j = 0; j < zakresAtrybutow[i].Count; j++)
                    {
                        if (!config.ZakresAtrybutow[i].Contains(zakresAtrybutow[i][j]))
                        {
                            MessageBox.Show("Atrybut nie należy do zakresu podanego w pliku konfiguracyjnym, " +
                                            "chodzi o zakres: " + (i + 1) + " i wartość: " + zakresAtrybutow[i][j], "Błąd walidacji");
                            return false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }
        public bool SprawdzWartosciMinimalne(Configuration config, List<double> wartosciMinimalne)
        {
            int i = 0;
            try
            {
                foreach (var wartoscMinimalna in config.WartosciMinimalneWKolumnie)
                {
                    if (wartoscMinimalna != wartosciMinimalne[i])
                    {
                        MessageBox.Show("Wartości minimalne nie są zgodne z wartościami podanymi w pliku konfiguracyjnym, " +
                                        "powinno być: " + wartoscMinimalna + ", a jest: " + wartosciMinimalne[i], "Błąd walidacji");
                        return false;
                    }
                    i++;
                }
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }
        public bool SprawdzWartosciMaksymalne(Configuration config, List<double> wartosciMaksymalne)
        {
            int i = 0;
            try
            {
                foreach (var wartoscMaksymalna in config.WartosciMaksymalneWKolumnie)
                {
                    if (wartoscMaksymalna != wartosciMaksymalne[i])
                    {
                        MessageBox.Show("Wartości maksymalne nie są zgodne z wartościami podanymi w pliku konfiguracyjnym, " +
                                        "powinno być: " + wartoscMaksymalna + ", a jest: " + wartosciMaksymalne[i], "Błąd walidacji");
                        return false;
                    }
                    i++;
                }
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }
        public bool SprawdzTypyDanych(Configuration config, List<string> typyDanych)
        {
            int i = 0;
            try
            {
                foreach (var typ in config.TypyDanych)
                {
                    if (typ != typyDanych[i])
                    {
                        MessageBox.Show("Typ danych nie jest zgodny z typem podanym w pliku konfiguracyjnym, " +
                                        "powinno być: " + typ + ", a jest: " + typyDanych[i] + " w miejscu: " + i, "Błąd walidacji");
                    }
                    i++;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return false;
            }
            return true;
        }
        public bool SprawdzZnakZapytania(Configuration config, string znakZapytania)
        {
            if (config.ZnakZapytania != znakZapytania)
            {
                MessageBox.Show(
                    "Wynik zmiany znaku zapytania nie jest taki sam jak wynik zapisany w pliku konfiguracyjnym," +
                    " powinno być: " + config.ZnakZapytania + ", a jest " + znakZapytania);
                return false;
            }
            return true;
        }
        public bool SprawdzKolumneDecyzji(Configuration config, int kolumna)
        {
            if (config.Decyzja != kolumna)
            {
                MessageBox.Show("Kolumna decyzji podana w pliku konfiguracyjnym jest inna niż kolumna podana," +
                                " powinno być: " + config.Decyzja + ", a jest " + kolumna);
                return false;
            }
            return true;
        }
    }
}
