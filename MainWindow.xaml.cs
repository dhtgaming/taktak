using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Path = System.IO.Path;

namespace KNN
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Paragraph _globalSaveParagraph = new();
        private readonly List<List<string>> _globalnaListaZapisu = new();

        private string _globalDelimiter;
        private string _plikWejsciowy;
        private string _globalFilename;
        private string _wczytanyConfig;

        private bool _poNormalizacji = false;
        private bool _czyZignorowane = false;
        private bool _podstawSrednia = false;
        private bool _normOstatniaKolumna = false;
        private bool _czyWczytanoDataSet = false;

        private int _dodanyWiersz = 0;
        private int _liczbaLiniiPoZmianach = 0;
        private int _parametrK = 0;

        private double _parametrPMinkowskiego = 0;

        public MainWindow()
        {
            InitializeComponent();
            cbMetryki.Items.Add("Manhattan");
            cbMetryki.Items.Add("Euklidesowa");
            cbMetryki.Items.Add("Czebyszewa");
            cbMetryki.Items.Add("Minkowskiego");
            cbMetryki.Items.Add("Z logarytmem");
            cbMetryki.SelectionChanged += new SelectionChangedEventHandler(MinkowskiegoWybrana);
        }

        #region FUNKCJE ZADANIA 1
        private void Button_Browse(object sender, RoutedEventArgs e)
        {
            _liczbaLiniiPoZmianach = 0;
            _poNormalizacji = false;
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".dat";
            dlg.Filter = "Dat files (.dat)|*.dat|Data files (.data)|*.data|Txt files (.txt)|*.txt|Json files (.json)|*.json";
            bool? result = dlg.ShowDialog();
            List<List<string>> pomocPrzyOtwieraniuJson = new List<List<string>>();
            StreamWriter zapiszJson = new StreamWriter(@"jsonpomoc.txt");
            string text = "";

            if (result == true)
            {
                if (dlg.FileName.Contains("json"))
                {
                    var plik = File.ReadAllText(dlg.FileName);
                    pomocPrzyOtwieraniuJson = JsonConvert.DeserializeObject<List<List<string>>>(plik);
                    int liczbaWierszy = pomocPrzyOtwieraniuJson.Count;
                    int i = 1;
                    foreach (var lista in pomocPrzyOtwieraniuJson)
                    {
                        text = string.Join(" ", lista);
                        zapiszJson.Write(text);
                        if (i != liczbaWierszy)
                            zapiszJson.Write("\n");
                        i++;
                    }

                    _globalFilename = "jsonpomoc.txt";
                    _plikWejsciowy = "jsonpomoc.txt";
                }
                else
                {
                    _globalFilename = dlg.FileName;
                    _plikWejsciowy = dlg.FileName;
                }
                zapiszJson.Flush();
                zapiszJson.Close();
                FileNameTextBox.Text = dlg.FileName;
                _czyZignorowane = false;
                _dodanyWiersz = 0;
                DecyzjaOZnakuZapytania(_plikWejsciowy);

                _czyWczytanoDataSet = true;
            }


        }

        private void Button_Open(object sender, RoutedEventArgs e)
        {
            _globalSaveParagraph.Inlines.Clear();
            Paragraph paragraph = new Paragraph();
            try
            {
                paragraph.Inlines.Add(File.ReadAllText(_plikWejsciowy));
                _globalSaveParagraph.Inlines.Add(File.ReadAllText(_plikWejsciowy));
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Wystąpił błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            FlowDocument document = new FlowDocument(paragraph);
            FlowDocReader.Document = document;
            paragraph.FontFamily = new FontFamily("Arial");
            paragraph.FontSize = 15.0;
            paragraph.FontStretch = FontStretches.UltraExpanded;
        }

        private void Button_Sprawdz(object sender, RoutedEventArgs e)
        {
            bool warunek = false;
            _globalFilename = FileNameTextBox.Text;
            //Sprawdzenie wczytywanych plikow z zapisanym configiem
            int j;
            int i = 0;
            bool config = true;
            string test = "";
            if (string.IsNullOrEmpty(_wczytanyConfig))
            {
                MessageBox.Show("Nie wczytano pliku konfiguracyjnego");
                return;
            }
            try
            {
                test = File.ReadAllText(_wczytanyConfig);
            }
            catch (Exception exception)
            {
                MessageBox.Show("Brak pliku konfiguracyjnego", "Wystąpił błąd");
                config = false;
            }

            List<Configuration> listaKonfiguracji = new List<Configuration>();
            foreach (var row in test.Split('\n'))
            {
                if (!string.IsNullOrEmpty(row))
                {
                    Configuration xd = JsonConvert.DeserializeObject<Configuration>(row);
                    listaKonfiguracji.Add(xd);
                    i++;
                }
            }

            Configuration doSprawdzenia = new Configuration();

            if (Path.GetFileName(_globalFilename) == @"australian.dat")
            {
                doSprawdzenia = listaKonfiguracji[0];
                warunek = true;
            }
            if (Path.GetFileName(_globalFilename) == @"breast-cancer-wisconsin.data")
            {
                doSprawdzenia = listaKonfiguracji[1];
                warunek = true;
            }
            if (Path.GetFileName(_globalFilename) == @"crx.data")
            {
                doSprawdzenia = listaKonfiguracji[2];
                warunek = true;
            }
            if (Path.GetFileName(_globalFilename) == @"iris.data")
            {
                doSprawdzenia = listaKonfiguracji[3];
                warunek = true;
            }

            if (!warunek)
                MessageBox.Show("Sprawdzanie configu można wykonać tylko na nie naruszonych wcześniej 3 repozytoriach", "Wystąpił błąd");

            string delimiter = ",";
            bool cbDone = true;
            bool txNotEmpty = true;
            string delimeterPomoc = ",";

            if (cbPrzecinek.IsChecked == true)
            {
                delimiter = ",";
                tbZnak.Text = ",";
                delimeterPomoc = ",";
            }
            if (cbSpacja.IsChecked == true)
            {
                delimiter = " ";
                tbZnak.Text = "s";
                delimeterPomoc = "s";
            }
            if (cbSpacja.IsChecked == false && cbPrzecinek.IsChecked == false && string.IsNullOrWhiteSpace(tbZnak.Text))
            {
                MessageBox.Show("Proszę wybrać separator", "Wystąpił błąd");
                cbDone = false;
            }
            if (cbSpacja.IsChecked == false && cbPrzecinek.IsChecked == false)
            {
                if (tbZnak.Text == "s")
                {
                    delimiter = " ";
                    cbSpacja.IsChecked = true;
                }
            }
            if (cbPrzecinek.IsChecked == true && cbSpacja.IsChecked == true)
            {
                MessageBox.Show("Proszę zostawić tylko jeden wybrany separator");
                cbDone = false;
            }
            if (string.IsNullOrWhiteSpace(FileNameTextBox.Text))
            {
                MessageBox.Show("Brak ścieżki do pliku", "Wystąpił błąd");
                txNotEmpty = false;
            }
            if (string.IsNullOrWhiteSpace(tbZnak.Text))
            {
                MessageBox.Show("Brak ustawionego separatora", "Wystąpił błąd");
                txNotEmpty = false;
            }

            if (cbDone && txNotEmpty && warunek && config)
            {
                var liczbaLinii = File.ReadLines(_globalFilename).Where(line => line != "").Count() - _liczbaLiniiPoZmianach;
                string znakZapytania = "usuwanie";
                if (_podstawSrednia)
                {
                    liczbaLinii = File.ReadLines(_globalFilename).Count();
                    znakZapytania = "srednia";
                }

                if (Path.GetFileName(_globalFilename) == "australian.dat" || Path.GetFileName(_globalFilename) == "iris.data")
                    znakZapytania = "brak";
                var pierwszaLinia = File.ReadLines(_globalFilename).First();
                int liczbaKolumn = 0;
                if (pierwszaLinia != null)
                {
                    liczbaKolumn = pierwszaLinia.Split(delimiter).Length;
                }

                string[,] tablicaString = new string[liczbaLinii, liczbaKolumn];
                string[,] tablicaStringCrx = new string[liczbaLinii, liczbaKolumn];
                double[,] tablicaDouble = new double[liczbaLinii, liczbaKolumn];

                tablicaStringCrx = DoTablicyString(_globalFilename, liczbaLinii, liczbaKolumn, delimiter, false);
                //Tylko dla crx.data, inicjalizacja tablicy do atrybutow
                List<List<string>> znalezione = new List<List<string>>();
                for (int k = 0; k < liczbaKolumn; k++)
                {
                    znalezione.Add(new List<string>());
                }
                if (doSprawdzenia == listaKonfiguracji[2])
                {
                    for (int k = 0; k < liczbaKolumn; k++)
                    {
                        if (k == 0 || k == 3 || k == 4 || k == 5 || k == 6 || k == 8 || k == 9 || k == 11 || k == 12 ||
                            k == 15)
                        {
                            for (int l = 0; l < liczbaLinii; l++)
                            {
                                if (tablicaStringCrx[l, k] == "?")
                                    continue;
                                if (!znalezione[k].Contains(tablicaStringCrx[l, k]))
                                {
                                    znalezione[k].Add(tablicaStringCrx[l, k]);
                                }
                            }
                        }
                    }
                }
                tablicaString = DoTablicyString(_globalFilename, liczbaLinii, liczbaKolumn, delimiter);
                tablicaDouble = DoTablicyDouble(tablicaString, liczbaLinii, liczbaKolumn, _globalFilename);
                //Sprawdanie min/max
                List<double> pomocMax = new List<double>();
                for (int q = 0; q < liczbaKolumn; q++)
                {
                    pomocMax.Add(ZnajdzMinMax(tablicaDouble, liczbaLinii, liczbaKolumn)[1, q]);
                }

                List<double> pomocMin = new List<double>();
                for (int q = 0; q < liczbaKolumn; q++)
                {
                    pomocMin.Add(ZnajdzMinMax(tablicaDouble, liczbaLinii, liczbaKolumn)[0, q]);
                }

                var typyDanych = File.ReadLines(_globalFilename).First();
                List<string> typ = new List<string>();
                typ = typyDanych.Split(delimiter).ToList();

                //Tylko dla crx.data
                if (doSprawdzenia == listaKonfiguracji[2])
                {
                    List<int> wezMinMax = new List<int>();

                    wezMinMax.Add(1);
                    wezMinMax.Add(2);
                    wezMinMax.Add(7);
                    wezMinMax.Add(10);
                    wezMinMax.Add(13);
                    wezMinMax.Add(14);

                    foreach (var number in wezMinMax)
                    {
                        znalezione[number].Clear();
                        znalezione[number].Add(pomocMin[number].ToString(CultureInfo.CurrentCulture));
                        znalezione[number].Add(pomocMax[number].ToString(CultureInfo.CurrentCulture));
                    }
                }
                //Sprawdzanie min i max dla datasetow bez liter
                if (doSprawdzenia == listaKonfiguracji[0] || doSprawdzenia == listaKonfiguracji[1])
                {
                    int v = 0;
                    foreach (var item in znalezione)
                    {
                        item.Add(pomocMin[v].ToString(CultureInfo.CurrentCulture));
                        item.Add(pomocMax[v].ToString(CultureInfo.CurrentCulture));
                        v++;
                    }
                }
                //Sprawdzanie typow danych
                List<string> typyDanychPomoc = new List<string>();
                typyDanychPomoc = SprawdzTypDanych(_globalFilename, delimiter, liczbaLinii, liczbaKolumn);
                //Końcowe sprawdzanie configu
                CheckWithConfig validate = new CheckWithConfig();
                var sprawdzSeparator = validate.SprawdzDelimeter(doSprawdzenia, delimeterPomoc);
                var sprawdzLiczbeLinii = validate.SprawdzLiczbeLinii(doSprawdzenia, liczbaLinii);
                var sprawdzLiczbeKolumn = validate.SprawdzLiczbeKolumn(doSprawdzenia, liczbaKolumn);
                var sprawdzZakresAtrybutow = validate.SprawdzZakresAtrybutow(doSprawdzenia, znalezione);
                var sprawdzWartosciMinimalne = validate.SprawdzWartosciMinimalne(doSprawdzenia, pomocMin);
                var sprawdzWartosciMaksymalne = validate.SprawdzWartosciMaksymalne(doSprawdzenia, pomocMax);
                var sprawdzTypyDanych = validate.SprawdzTypyDanych(doSprawdzenia, typyDanychPomoc);
                var sprawdzZnakZapytania = validate.SprawdzZnakZapytania(doSprawdzenia, znakZapytania);
                var sprawdzKolumneDecyzji = validate.SprawdzKolumneDecyzji(doSprawdzenia, liczbaKolumn);

                if (sprawdzSeparator && sprawdzLiczbeLinii && sprawdzLiczbeKolumn && sprawdzZakresAtrybutow
                    && sprawdzWartosciMinimalne && sprawdzWartosciMaksymalne && sprawdzTypyDanych && sprawdzZnakZapytania && sprawdzKolumneDecyzji)
                    MessageBox.Show("Plik sprawdzony pomyślnie", "Sprawdzanie zakończone");
                else
                    MessageBox.Show("Wystąpiły błędy w sprawdzaniu pliku konfiguracyjnego", "Wystąpił błąd");
            }
        }

        private void Button_PrzegladajConfig(object sender, RoutedEventArgs e)
        {
            string filename = _wczytanyConfig;
            if (string.IsNullOrEmpty(filename))
            {
                MessageBox.Show("Nie wczytano pliku konfiguracyjnego");
                return;
            }

            Paragraph paragraph = new Paragraph();
            try
            {
                paragraph.Inlines.Add(File.ReadAllText(filename));
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Wystąpił błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            FlowDocument document = new FlowDocument(paragraph);
            FlowDocReader.Document = document;
            paragraph.FontFamily = new FontFamily("Arial");
            paragraph.FontSize = 18.0;
            paragraph.FontStretch = FontStretches.UltraExpanded;
        }

        private void Button_Stworz(object sender, RoutedEventArgs e)
        {

            //Inicjalizacja ogólnych zmiennych
            string delimiter;
            int liczbaLinii;
            int liczbaKolumn = 0;
            string pierwszaLinia;


            //Tworzenie 1 pliku konfiguracyjnego
            List<double> pomocMax1 = new List<double>();
            List<double> pomocMin1 = new List<double>();
            string filename1 = "australian.dat";
            DecyzjaOZnakuZapytania(filename1);
            delimiter = " ";
            liczbaLinii = File.ReadLines(filename1).Count();
            pierwszaLinia = File.ReadLines(filename1).First();
            if (pierwszaLinia != null)
            {
                liczbaKolumn = pierwszaLinia.Split(delimiter).Length;
            }
            string[,] tablicaString1 = new string[liczbaLinii, liczbaKolumn];
            double[,] tablicaDouble1 = new double[liczbaLinii, liczbaKolumn];
            tablicaString1 = DoTablicyString(filename1, liczbaLinii, liczbaKolumn, delimiter);
            tablicaDouble1 = DoTablicyDouble(tablicaString1, liczbaLinii, liczbaKolumn, filename1);
            for (int q = 0; q < liczbaKolumn; q++)
            {
                pomocMax1.Add(ZnajdzMinMax(tablicaDouble1, liczbaLinii, liczbaKolumn)[1, q]);
            }
            List<string> typyDanychPomoc1 = new List<string>();
            typyDanychPomoc1 = SprawdzTypDanych(filename1, delimiter, liczbaLinii, liczbaKolumn);
            for (int q = 0; q < liczbaKolumn; q++)
            {
                pomocMin1.Add(ZnajdzMinMax(tablicaDouble1, liczbaLinii, liczbaKolumn)[0, q]);
            }
            Configuration config1 = new Configuration()
            {
                Separator = "s",
                LiczbaLinii = File.ReadLines(filename1).Count(),
                LiczbaKolumn = File.ReadLines(filename1).First().Split(delimiter).Length,
                ZakresAtrybutow = new List<List<string>>()
                {
                    new List<string>(){$"{pomocMin1[0]}", $"{pomocMax1[0]}"},
                    new List<string>(){$"{pomocMin1[1]}", $"{pomocMax1[1]}"},
                    new List<string>(){$"{pomocMin1[2]}", $"{pomocMax1[2]}"},
                    new List<string>(){$"{pomocMin1[3]}", $"{pomocMax1[3]}"},
                    new List<string>(){$"{pomocMin1[4]}", $"{pomocMax1[4]}"},
                    new List<string>(){$"{pomocMin1[5]}", $"{pomocMax1[5]}"},
                    new List<string>(){$"{pomocMin1[6]}", $"{pomocMax1[6]}"},
                    new List<string>(){$"{pomocMin1[7]}", $"{pomocMax1[7]}"},
                    new List<string>(){$"{pomocMin1[8]}", $"{pomocMax1[8]}"},
                    new List<string>(){$"{pomocMin1[9]}", $"{pomocMax1[9]}"},
                    new List<string>(){$"{pomocMin1[10]}", $"{pomocMax1[10]}"},
                    new List<string>(){$"{pomocMin1[11]}", $"{pomocMax1[11]}"},
                    new List<string>(){$"{pomocMin1[12]}", $"{pomocMax1[12]}"},
                    new List<string>(){$"{pomocMin1[13]}", $"{pomocMax1[13]}"},
                    new List<string>(){$"{pomocMin1[14]}", $"{pomocMax1[14]}"},
                },
                WartosciMinimalneWKolumnie = pomocMin1,
                WartosciMaksymalneWKolumnie = pomocMax1,
                TypyDanych = typyDanychPomoc1,
                ZnakZapytania = "brak",
                Decyzja = liczbaKolumn,
            };

            //Tworzenie 2 pliku konfiguracyjnego
            List<double> pomocMax2 = new List<double>();
            List<double> pomocMin2 = new List<double>();
            string filename2 = "breast-cancer-wisconsin.data";
            DecyzjaOZnakuZapytania(filename2);
            string delimeter2 = ",";
            liczbaLinii = File.ReadLines(filename2).Count() - _liczbaLiniiPoZmianach;
            string coZaZnakZapytania = "usuwanie";
            if (_podstawSrednia)
            {
                liczbaLinii = File.ReadLines(filename2).Count();
                coZaZnakZapytania = "srednia";
            }
            pierwszaLinia = File.ReadLines(filename2).First();
            if (pierwszaLinia != null)
            {
                liczbaKolumn = pierwszaLinia.Split(delimeter2).Length;
            }
            string[,] tablicaString2 = new string[liczbaLinii, liczbaKolumn];
            double[,] tablicaDouble2 = new double[liczbaLinii, liczbaKolumn];
            tablicaString2 = DoTablicyString(filename2, liczbaLinii, liczbaKolumn, delimeter2);
            tablicaDouble2 = DoTablicyDouble(tablicaString2, liczbaLinii, liczbaKolumn, filename2);
            for (int q = 0; q < liczbaKolumn; q++)
            {
                pomocMax2.Add(ZnajdzMinMax(tablicaDouble2, liczbaLinii, liczbaKolumn)[1, q]);
            }
            for (int q = 0; q < liczbaKolumn; q++)
            {
                pomocMin2.Add(ZnajdzMinMax(tablicaDouble2, liczbaLinii, liczbaKolumn)[0, q]);
            }
            List<string> typyDanychPomoc2 = new List<string>();
            typyDanychPomoc2 = SprawdzTypDanych(filename2, delimeter2, liczbaLinii, liczbaKolumn);
            Configuration config2 = new Configuration()
            {
                Separator = delimeter2,
                LiczbaLinii = liczbaLinii,
                LiczbaKolumn = File.ReadLines(filename2).First().Split(delimeter2).Length,
                ZakresAtrybutow = new List<List<string>>()
                {
                    new List<string>(){$"{pomocMin2[0]}", $"{pomocMax2[0]}"},
                    new List<string>(){$"{pomocMin2[1]}", $"{pomocMax2[1]}"},
                    new List<string>(){$"{pomocMin2[2]}", $"{pomocMax2[2]}"},
                    new List<string>(){$"{pomocMin2[3]}", $"{pomocMax2[3]}"},
                    new List<string>(){$"{pomocMin2[4]}", $"{pomocMax2[4]}"},
                    new List<string>(){$"{pomocMin2[5]}", $"{pomocMax2[5]}"},
                    new List<string>(){$"{pomocMin2[6]}", $"{pomocMax2[6]}"},
                    new List<string>(){$"{pomocMin2[7]}", $"{pomocMax2[7]}"},
                    new List<string>(){$"{pomocMin2[8]}", $"{pomocMax2[8]}"},
                    new List<string>(){$"{pomocMin2[9]}", $"{pomocMax2[9]}"},
                    new List<string>(){$"{pomocMin2[10]}", $"{pomocMax2[10]}"},
                },
                WartosciMinimalneWKolumnie = pomocMin2,
                WartosciMaksymalneWKolumnie = pomocMax2,
                TypyDanych = typyDanychPomoc2,
                ZnakZapytania = coZaZnakZapytania,
                Decyzja = liczbaKolumn,
            };

            //Tworzenie 3 pliku konfiguracyjnego
            List<double> pomocMax3 = new List<double>();
            List<double> pomocMin3 = new List<double>();
            string filename3 = "crx.data";
            DecyzjaOZnakuZapytania(filename3);
            string delimeter3 = ",";
            liczbaLinii = File.ReadLines(filename3).Count() - _liczbaLiniiPoZmianach;
            coZaZnakZapytania = "usuwanie";
            if (_podstawSrednia)
            {
                liczbaLinii = File.ReadLines(filename3).Count();
                coZaZnakZapytania = "srednia";
            }

            pierwszaLinia = File.ReadLines(filename3).First();
            if (pierwszaLinia != null)
            {
                liczbaKolumn = pierwszaLinia.Split(delimeter3).Length;
            }
            string[,] tablicaString3 = new string[liczbaLinii, liczbaKolumn];
            double[,] tablicaDouble3 = new double[liczbaLinii, liczbaKolumn];
            tablicaString3 = DoTablicyString(filename3, liczbaLinii, liczbaKolumn, delimeter3);
            tablicaDouble3 = DoTablicyDouble(tablicaString3, liczbaLinii, liczbaKolumn, filename3);
            for (int q = 0; q < liczbaKolumn; q++)
            {
                pomocMax3.Add(ZnajdzMinMax(tablicaDouble3, liczbaLinii, liczbaKolumn)[1, q]);
            }
            for (int q = 0; q < liczbaKolumn; q++)
            {
                pomocMin3.Add(ZnajdzMinMax(tablicaDouble3, liczbaLinii, liczbaKolumn)[0, q]);
            }
            List<string> typyDanychPomoc3 = new List<string>();
            typyDanychPomoc3 = SprawdzTypDanych(filename3, delimeter3, liczbaLinii, liczbaKolumn);
            Configuration config3 = new Configuration()
            {
                Separator = delimeter3,
                LiczbaLinii = liczbaLinii,
                LiczbaKolumn = File.ReadLines(filename3).First().Split(delimeter3).Length,
                ZakresAtrybutow = new List<List<string>>()
                {
                    new List<string>(){"b", "a"},
                    new List<string>(){$"{pomocMin3[1]}", $"{pomocMax3[1]}"},
                    new List<string>(){$"{pomocMin3[2]}", $"{pomocMax3[2]}"},
                    new List<string>(){"u", "y", "l"},
                    new List<string>(){"g", "p", "gg"},
                    new List<string>(){"w", "q", "m", "r", "cc", "k", "c", "d", "x", "i", "e", "aa", "ff", "j"},
                    new List<string>(){"v", "h", "bb", "ff", "j", "z", "o", "dd", "n"},
                    new List<string>(){$"{pomocMin3[7]}", $"{pomocMax3[7]}"},
                    new List<string>(){"t", "f"},
                    new List<string>(){"t", "f"},
                    new List<string>(){$"{pomocMin3[10]}", $"{pomocMax3[10]}"},
                    new List<string>(){"f", "t"},
                    new List<string>(){"g", "s", "p"},
                    new List<string>(){$"{pomocMin3[13]}", $"{pomocMax3[13]}"},
                    new List<string>(){$"{pomocMin3[14]}", $"{pomocMax3[14]}"},
                    new List<string>(){"+", "-"}
                },
                WartosciMinimalneWKolumnie = pomocMin3,
                WartosciMaksymalneWKolumnie = pomocMax3,
                TypyDanych = typyDanychPomoc3,
                ZnakZapytania = coZaZnakZapytania,
                Decyzja = liczbaKolumn,
            };

            //Tworzenie 4 pliku konfiguracyjnego
            List<double> pomocMax4 = new List<double>();
            List<double> pomocMin4 = new List<double>();
            string filename4 = "iris.data";
            DecyzjaOZnakuZapytania(filename4);
            delimiter = ",";
            liczbaLinii = File.ReadLines(filename4).Count();
            pierwszaLinia = File.ReadLines(filename4).First();
            if (pierwszaLinia != null)
            {
                liczbaKolumn = pierwszaLinia.Split(delimiter).Length;
            }
            string[,] tablicaString4 = new string[liczbaLinii, liczbaKolumn];
            double[,] tablicaDouble4 = new double[liczbaLinii, liczbaKolumn];
            tablicaString4 = DoTablicyString(filename4, liczbaLinii, liczbaKolumn, delimiter);
            tablicaDouble4 = DoTablicyDouble(tablicaString4, liczbaLinii, liczbaKolumn, filename4);
            for (int q = 0; q < liczbaKolumn; q++)
            {
                pomocMax1.Add(ZnajdzMinMax(tablicaDouble4, liczbaLinii, liczbaKolumn)[1, q]);
            }
            List<string> typyDanychPomoc4 = new List<string>();
            typyDanychPomoc4 = SprawdzTypDanych(filename4, delimiter, liczbaLinii, liczbaKolumn);
            for (int q = 0; q < liczbaKolumn; q++)
            {
                pomocMin1.Add(ZnajdzMinMax(tablicaDouble4, liczbaLinii, liczbaKolumn)[0, q]);
            }
            Configuration config4 = new Configuration()
            {
                Separator = ",",
                LiczbaLinii = File.ReadLines(filename4).Count(),
                LiczbaKolumn = File.ReadLines(filename4).First().Split(delimiter).Length,
                ZakresAtrybutow = new List<List<string>>()
                {
                    new List<string>(){$"{pomocMin1[0]}", $"{pomocMax1[0]}"},
                    new List<string>(){$"{pomocMin1[1]}", $"{pomocMax1[1]}"},
                    new List<string>(){$"{pomocMin1[2]}", $"{pomocMax1[2]}"},
                    new List<string>(){$"{pomocMin1[3]}", $"{pomocMax1[3]}"},
                    new List<string>(){"Iris-setosa", "Iris-versicolor", "Iris-virginica"}
                },
                WartosciMinimalneWKolumnie = pomocMin4,
                WartosciMaksymalneWKolumnie = pomocMax4,
                TypyDanych = typyDanychPomoc4,
                ZnakZapytania = "brak",
                Decyzja = liczbaKolumn,
            };


            string config1Json = JsonConvert.SerializeObject(config1);
            string config2Json = JsonConvert.SerializeObject(config2);
            string config3Json = JsonConvert.SerializeObject(config3);
            string config4Json = JsonConvert.SerializeObject(config4);

            SaveFileDialog saveFile = new SaveFileDialog();
            saveFile.Filter = "Json File|*.json";
            saveFile.Title = "Zapisz config";

            bool? result = saveFile.ShowDialog();
            if (result == true)
            {
                StreamWriter configJson = new StreamWriter(saveFile.FileName);
                configJson.Write(config1Json);
                configJson.WriteLine("\n" + config2Json);
                configJson.WriteLine(config3Json);
                configJson.WriteLine(config4Json);
                configJson.Flush();
                configJson.Close();
            }
        }

        private void Button_Wczytaj_Config(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Json File|*.json";

            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                _wczytanyConfig = dlg.FileName;
            }
        }

        private void Zapisz_Txt(object sender, RoutedEventArgs e)
        {
            var text = String.Empty;
            MessageBox.Show("Zapisywanie aktualnego stanu do pliku TXT", "Informacja");
            SaveFileDialog saveFile = new SaveFileDialog();


            saveFile.Filter = "Txt file|*.txt";
            saveFile.Title = "Zapisz do pliku TXT";

            bool? result = saveFile.ShowDialog();
            StreamWriter aktualnyStan = new StreamWriter(saveFile.FileName, false);
            try
            {
                text = string.Join(String.Empty,
                    _globalSaveParagraph.Inlines.Select(line => line.ContentStart.GetTextInRun(LogicalDirection.Forward)));
                aktualnyStan.Write(text);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Wystąpił błąd");
            }

            aktualnyStan.Flush();
            aktualnyStan.Close();
        }

        private void Zapisz_Json(object sender, RoutedEventArgs e)
        {
            _globalnaListaZapisu.Clear();
            var text = String.Empty;
            MessageBox.Show("Zapisywanie aktualnego stanu do pliku JSON", "Informacja");
            SaveFileDialog saveFile = new SaveFileDialog();


            saveFile.Filter = "Json file|*.json";
            saveFile.Title = "Zapisz do pliku Json";

            bool? result = saveFile.ShowDialog();
            StreamWriter aktualnyStan = new StreamWriter(saveFile.FileName, false);
            try
            {
                text = string.Join(String.Empty,
                    _globalSaveParagraph.Inlines.Select(line => line.ContentStart.GetTextInRun(LogicalDirection.Forward)));
                int i = 0;
                foreach (var row in text.Split("\n"))
                {
                    int j = 0;
                    _globalnaListaZapisu.Add(new List<string>());
                    foreach (var col in row.Trim().Split(_globalDelimiter))
                    {
                        _globalnaListaZapisu[i].Add(col);
                        j++;
                    }
                    i++;
                }

                var JsonSave = JsonConvert.SerializeObject(_globalnaListaZapisu);
                aktualnyStan.Write(JsonSave);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Wystąpił błąd");
            }

            aktualnyStan.Flush();
            aktualnyStan.Close();
        }

        private void Button_Dodaj_Wiersz(object sender, RoutedEventArgs e)
        {

            bool tbDone = true;
            bool format = false;
            if (string.IsNullOrWhiteSpace(tbDodajWiersz.Text))
            {
                MessageBox.Show("Wartość nowego wiersza nie może być pusta", "Wystąpił błąd");
                tbDone = false;
            }

            var delimiter = ",";

            if (Path.GetFileName(_globalFilename) == @"australian.dat")
                delimiter = " ";
            if (Path.GetFileName(_globalFilename) == @"breast-cancer-wisconsin.data")
                delimiter = ",";
            if (Path.GetFileName(_globalFilename) == @"crx.data")
                delimiter = ",";
            if (_poNormalizacji)
                delimiter = " ";

            int liczbaKolumn = 0;
            if (!string.IsNullOrWhiteSpace(_globalFilename))
            {
                var pierwszaLinia = File.ReadLines(_globalFilename).First();
                liczbaKolumn = pierwszaLinia.Split(delimiter).Length;
            }

            if (tbDodajWiersz.Text.Split(delimiter).Length == liczbaKolumn)
                format = true;
            if (!format)
                MessageBox.Show("Nie poprawny format nowego wiersza");
            if (tbDone && format)
            {
                try
                {
                    using (StreamWriter w = File.AppendText(_globalFilename))
                    {
                        w.WriteLine(tbDodajWiersz.Text);
                    }
                    _globalSaveParagraph.Inlines.Add(tbDodajWiersz.Text);
                    _globalDelimiter = delimiter;
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "Wystąpił błąd");
                    throw;
                }
                MessageBox.Show("Pomyślnie dodano nowy wiersz o wartości: " + tbDodajWiersz.Text);
            }
            if (!_poNormalizacji)
                Button_Open(sender, e);
            _dodanyWiersz++;
        }

        private void Button_Ignoruj_Kolumny(object sender, RoutedEventArgs e)
        {
            bool problem = false;

            if (string.IsNullOrWhiteSpace(tbNumerKolumny.Text))
            {
                MessageBox.Show("Nie podano żadnych kolumn do zignorowania", "Wystąpił błąd");
                problem = true;
            }

            if (string.IsNullOrWhiteSpace(_globalFilename))
            {
                MessageBox.Show("Należy podać w jakim pliku będziemy ignorowali kolumny");
                problem = true;
            }
            List<string> listaKolumnString = tbNumerKolumny.Text.Split(",").ToList();
            if (listaKolumnString.Contains("0"))
            {
                problem = true;
                MessageBox.Show("Uwaga! Podano wartość 0. Ignorowane kolumny powinny zaczynać się od 1");
            }
            if (!problem)
            {
                bool bezproblemu = true;
                List<int> listaKolumn = new List<int>();
                foreach (var kolumna in listaKolumnString)
                {
                    try
                    {
                        int liczba = int.Parse(kolumna);
                        liczba -= 1;
                        listaKolumn.Add(liczba);
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(exception.Message, "Wystąpił błąd");
                        bezproblemu = false;
                    }
                }
                if (!bezproblemu)
                {
                    listaKolumn.Clear();
                }
                else
                {
                    string s = String.Join(";", listaKolumn);
                    if (_czyZignorowane)
                        MessageBox.Show(
                            "Kolumny zostały już zignorowane, jeśli chcesz zrobić to jeszcze raz ponownie otwórz plik", "Wystąpił błąd");
                    else
                        ZmienKolumnyWPliku(listaKolumn, _globalFilename);
                }
                if (listaKolumn.Count == 0)
                {
                    MessageBox.Show("Lista ignorowanych kolumn jest pusta", "Wystąpił błąd");
                }
            }
            _globalSaveParagraph.Inlines.Clear();
            Paragraph paragraph = new Paragraph();
            try
            {
                paragraph.Inlines.Add(File.ReadAllText(_globalFilename));
                _globalSaveParagraph.Inlines.Add(File.ReadAllText(_globalFilename));
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Wystąpił błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            FlowDocument document = new FlowDocument(paragraph);
            FlowDocReader.Document = document;
            paragraph.FontFamily = new FontFamily("Arial");
            paragraph.FontSize = 15.0;
            paragraph.FontStretch = FontStretches.UltraExpanded;
        }

        public void ZmienKolumnyWPliku(List<int> lista, string plikDoZmiany)
        {
            string delimiter = ",";
            bool cbDone = true;
            bool txNotEmpty = true;

            if (cbPrzecinek.IsChecked == true)
            {
                delimiter = ",";
                tbZnak.Text = ",";
            }
            if (cbSpacja.IsChecked == true)
            {
                delimiter = " ";
                tbZnak.Text = "s";
            }
            if (cbSpacja.IsChecked == false && cbPrzecinek.IsChecked == false && string.IsNullOrWhiteSpace(tbZnak.Text))
            {
                MessageBox.Show("Proszę wybrać separator", "Wystąpił błąd");
                cbDone = false;
            }
            if (cbSpacja.IsChecked == false && cbPrzecinek.IsChecked == false)
            {
                if (tbZnak.Text == "s")
                {
                    delimiter = " ";
                    cbSpacja.IsChecked = true;
                }
            }
            if (cbPrzecinek.IsChecked == true && cbSpacja.IsChecked == true)
            {
                MessageBox.Show("Proszę zostawić tylko jeden wybrany separator");
                cbDone = false;
            }
            if (string.IsNullOrWhiteSpace(FileNameTextBox.Text))
            {
                MessageBox.Show("Brak ścieżki do pliku", "Wystąpił błąd");
                txNotEmpty = false;
            }
            if (string.IsNullOrWhiteSpace(tbZnak.Text))
            {
                MessageBox.Show("Brak ustawionego separatora", "Wystąpił błąd");
                txNotEmpty = false;
            }

            if (_poNormalizacji)
                delimiter = " ";

            bool mozliwe = true;
            int j;
            int i = 0;
            int pomoc = 0;
            var liczbaLinii = File.ReadLines(plikDoZmiany).Count() + _dodanyWiersz;
            if (!_poNormalizacji)
                liczbaLinii -= _liczbaLiniiPoZmianach;
            var pierwszaLinia = File.ReadLines(plikDoZmiany).First();
            int liczbaKolumn = 0;
            if (pierwszaLinia != null)
            {
                liczbaKolumn = pierwszaLinia.Split(delimiter).Length;
            }

            string[,] daneString = new string[liczbaLinii, liczbaKolumn];
            double[,] daneDouble = new double[liczbaLinii, liczbaKolumn];
            bool problem = false;
            if (cbDone && txNotEmpty)
            {
                daneString = DoTablicyString(plikDoZmiany, liczbaLinii, liczbaKolumn, delimiter);

                StreamWriter sw = new StreamWriter(@"pomoc.txt", false);

                for (int k = 0; k < liczbaLinii; k++)
                {
                    for (int l = 0; l < liczbaKolumn; l++)
                    {
                        if (lista.Contains(l))
                        {
                            continue;
                        }
                        sw.Write(daneString[k, l] + " ");
                    }
                    sw.Write("\n");
                }
                sw.Flush();
                sw.Close();
                _globalFilename = "pomoc.txt";
                _globalDelimiter = delimiter;
                _czyZignorowane = true;
            }
        }

        public double[,] Normalizacja(double[,] doNormalizacji, double[] zakres, double[,] minMax, int liczbaLinii, int liczbaKolumn)
        {

            //Wzor na normalizacje: liczba_znormalizowana = (kolumna[x] - minimum) / zakres
            //Zakres kolumny: zakres = max - min
            for (int kolumna = 0; kolumna < liczbaKolumn; kolumna++)
            {
                for (int wiersz = 0; wiersz < liczbaLinii; wiersz++)
                {
                    doNormalizacji[wiersz, kolumna] =
                        (doNormalizacji[wiersz, kolumna] - minMax[0, kolumna]) / zakres[kolumna];
                }
            }
            return doNormalizacji;
        }

        public double[] ObliczZakres(double[,] minMaxTable, int liczbaKolumn)
        {
            double[] zakresTable = new double[liczbaKolumn];

            for (int kolumna = 0; kolumna < liczbaKolumn; kolumna++)
            {
                double pomoc = minMaxTable[1, kolumna];
                double pomoc1 = minMaxTable[0, kolumna];
                double zakres = minMaxTable[1, kolumna] - minMaxTable[0, kolumna];

                zakresTable[kolumna] = zakres;
            }
            return zakresTable;
        }

        public double[,] ZnajdzMinMax(double[,] tablica, int liczbaLinii, int liczbaKolumn)
        {
            double[,] minMaxTable = new double[2, liczbaKolumn];

            #region ZnajdzMin
            double min;
            for (int kolumna = 0; kolumna < liczbaKolumn; kolumna++)
            {
                min = double.MaxValue;
                for (int wiersz = 0; wiersz < liczbaLinii; wiersz++)
                {
                    double tempMin = tablica[wiersz, kolumna];
                    if (min > tempMin)
                        min = tempMin;
                }
                minMaxTable[0, kolumna] = min;
            }
            #endregion

            #region ZnajdzMax
            double max;
            for (int kolumna = 0; kolumna < liczbaKolumn; kolumna++)
            {
                max = 0;
                for (int wiersz = 0; wiersz < liczbaLinii; wiersz++)
                {
                    double tempMax = tablica[wiersz, kolumna];
                    if (max < tempMax)
                        max = tempMax;
                }
                minMaxTable[1, kolumna] = max;
            }
            #endregion

            return minMaxTable;
        }

        public string ZamianaLiter(string litera, int kolumna, string aktualnaWartosc)
        {
            string wynik = aktualnaWartosc;

            if (kolumna == 0)
            {
                switch (litera)
                {
                    case "a":
                        wynik = "0";
                        break;
                    case "b":
                        wynik = "1";
                        break;
                }
            }
            if (kolumna == 3)
            {
                switch (litera)
                {
                    case "u":
                        wynik = "1";
                        break;
                    case "y":
                        wynik = "2";
                        break;
                    case "l":
                        wynik = "3";
                        break;
                    case "t":
                        wynik = "4";
                        break;
                }
            }
            if (kolumna == 4)
            {
                switch (litera)
                {
                    case "g":
                        wynik = "1";
                        break;
                    case "p":
                        wynik = "2";
                        break;
                    case "gg":
                        wynik = "3";
                        break;
                    case "Iris-setosa":
                        wynik = "1";
                        break;
                    case "Iris-versicolor":
                        wynik = "2";
                        break;
                    case "Iris-virginica":
                        wynik = "3";
                        break;
                }
            }
            if (kolumna == 5)
            {
                switch (litera)
                {
                    case "c":
                        wynik = "1";
                        break;
                    case "d":
                        wynik = "2";
                        break;
                    case "cc":
                        wynik = "3";
                        break;
                    case "i":
                        wynik = "4";
                        break;
                    case "j":
                        wynik = "5";
                        break;
                    case "k":
                        wynik = "6";
                        break;
                    case "m":
                        wynik = "7";
                        break;
                    case "r":
                        wynik = "8";
                        break;
                    case "q":
                        wynik = "9";
                        break;
                    case "w":
                        wynik = "10";
                        break;
                    case "x":
                        wynik = "11";
                        break;
                    case "e":
                        wynik = "12";
                        break;
                    case "aa":
                        wynik = "13";
                        break;
                    case "ff":
                        wynik = "14";
                        break;
                }
            }
            if (kolumna == 6)
            {
                switch (litera)
                {
                    case "v":
                        wynik = "1";
                        break;
                    case "h":
                        wynik = "2";
                        break;
                    case "bb":
                        wynik = "3";
                        break;
                    case "j":
                        wynik = "4";
                        break;
                    case "n":
                        wynik = "5";
                        break;
                    case "z":
                        wynik = "6";
                        break;
                    case "dd":
                        wynik = "7";
                        break;
                    case "ff":
                        wynik = "8";
                        break;
                    case "o":
                        wynik = "9";
                        break;
                }
            }
            if (kolumna == 8)
            {
                switch (litera)
                {
                    case "t":
                        wynik = "1";
                        break;
                    case "f":
                        wynik = "0";
                        break;
                }
            }
            if (kolumna == 9)
            {
                switch (litera)
                {
                    case "t":
                        wynik = "1";
                        break;
                    case "f":
                        wynik = "0";
                        break;
                }
            }
            if (kolumna == 11)
            {
                switch (litera)
                {
                    case "t":
                        wynik = "1";
                        break;
                    case "f":
                        wynik = "0";
                        break;
                }
            }
            if (kolumna == 12)
            {
                switch (litera)
                {
                    case "g":
                        wynik = "1";
                        break;
                    case "p":
                        wynik = "2";
                        break;
                    case "s":
                        wynik = "3";
                        break;
                }
            }
            if (kolumna == 15)
            {
                switch (litera)
                {
                    case "+":
                        wynik = "1";
                        break;
                    case "-":
                        wynik = "0";
                        break;
                }
            }

            return wynik;
        }

        public string[,] DoTablicyString(string filename, int liczbaLinii, int liczbaKolumn, string delimiter, bool sredniaPotrzebna = true)
        {
            var plik = File.ReadAllText(filename);
            int i = 0;
            int j;
            string[,] daneString = new string[liczbaLinii, liczbaKolumn];
            bool problem = false;
            List<double> sredniaWKolumnie = new List<double>();
            if (sredniaPotrzebna)
                sredniaWKolumnie = ObliczSrednia(filename, delimiter, liczbaLinii, liczbaKolumn);
            foreach (var row in plik.Split('\n'))
            {
                if (!string.IsNullOrEmpty(row))
                {
                    if (row.Contains("?") && !_podstawSrednia)
                    {
                        continue;
                    }
                    j = 0;
                    foreach (var col in row.Trim().Split(delimiter))
                    {
                        if (col == "?" && _podstawSrednia && sredniaPotrzebna)
                        {
                            daneString[i, j] = sredniaWKolumnie[j].ToString(CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            try
                            {
                                daneString[i, j] = col;
                            }
                            catch (Exception exception)
                            {
                                MessageBox.Show(exception.Message, "Wystąpił błąd");
                                problem = true;
                            }
                        }
                        j++;
                    }
                    i++;
                }
                if (problem)
                    break;
            }
            return daneString;
        }

        public double[,] DoTablicyDouble(string[,] daneString, int liczbaLinii, int liczbaKolumn, string filename)
        {
            double[,] dane = new double[liczbaLinii, liczbaKolumn];
            int i = 0;
            for (int z = 0; z < liczbaLinii; z++)
            {
                for (int x = 0; x < liczbaKolumn; x++)
                {
                    daneString[z, x] = ZamianaLiter(daneString[z, x], x, daneString[z, x]);
                }
            }
            bool problem;
            List<double> pomocSrednia = new List<double>();
            for (int k = 0; k < liczbaLinii; k++)
            {
                problem = false;
                for (int l = 0; l < liczbaKolumn; l++)
                {
                    try
                    {
                        dane[k, l] = double.Parse(daneString[k, l].Trim(),
                            System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show(exception.Message, "Wystąpił błąd");
                        problem = true;
                    }
                }
                if (problem == true)
                    break;
            }
            return dane;
        }

        public void DecyzjaOZnakuZapytania(string filename)
        {
            _liczbaLiniiPoZmianach = 0;
            bool znakZapytania = false;
            var plik = File.ReadAllText(filename);
            foreach (var row in plik.Split('\n'))
            {
                if (!string.IsNullOrEmpty(row))
                {
                    if (row.Contains("?"))
                    {
                        _liczbaLiniiPoZmianach++;
                        znakZapytania = true;
                    }
                }
            }
            var decyzja = MessageBoxResult.Cancel;
            if (znakZapytania)
            {
                MessageBox.Show("Sprawdzasz plik: " + Path.GetFileName(filename) + ". Uwaga! Ten plik zawiera znaki zapytania, możesz usunąć linię z nimi lub podstawić średnią wartość");
                decyzja = MessageBox.Show("Tak - usuń linię || Nie - podstaw średnią", "Uwaga!", MessageBoxButton.YesNo);
                if (decyzja == MessageBoxResult.No)
                {
                    _liczbaLiniiPoZmianach = 0;
                    _podstawSrednia = true;
                }
            }
        }

        public List<double> ObliczSrednia(string filename, string delimiter, int liczbaLinii, int liczbaKolumn)
        {
            List<double> sredniaWKolumnie = new List<double>();
            if (_podstawSrednia)
            {
                int i = 0;
                int j;
                double suma;
                double srednia;
                int licznik = liczbaLinii;

                var plik = File.ReadAllText(filename);
                string[,] daneString = new string[liczbaLinii, liczbaKolumn];
                foreach (var row in plik.Split('\n'))
                {
                    if (!string.IsNullOrEmpty(row))
                    {
                        j = 0;
                        foreach (var col in row.Trim().Split(delimiter))
                        {
                            daneString[i, j] = col;
                            j++;
                        }
                        i++;
                    }
                }
                for (int z = 0; z < liczbaLinii; z++)
                {
                    for (int x = 0; x < liczbaKolumn; x++)
                    {
                        daneString[z, x] = ZamianaLiter(daneString[z, x], x, daneString[z, x]);
                    }
                }
                for (int k = 0; k < liczbaKolumn; k++)
                {
                    suma = 0;
                    for (int l = 0; l < liczbaLinii; l++)
                    {
                        if (daneString[l, k] == "?")
                        {
                            licznik--;
                            continue;
                        }

                        var pomoc = double.Parse(daneString[l, k].Trim(),
                            CultureInfo.InvariantCulture);
                        suma += pomoc;
                    }
                    srednia = suma / licznik;
                    sredniaWKolumnie.Add(srednia);
                }
            }
            return sredniaWKolumnie;
        }

        public List<string> SprawdzTypDanych(string filename, string delimiter, int liczbaLinii, int liczbaKolumn)
        {
            List<string> typyDanych = new List<string>();
            for (int i = 0; i < liczbaKolumn; i++)
            {
                typyDanych.Add("");
            }
            string[,] daneString = DoTablicyString(filename, liczbaLinii, liczbaKolumn, delimiter);
            for (int i = 0; i < liczbaKolumn; i++)
            {
                for (int j = 0; j < liczbaLinii; j++)
                {
                    var pomoc2 = daneString[j, i];
                    if (daneString[j, i] == "?")
                        continue;
                    if (pomoc2.Contains("."))
                    {
                        typyDanych[i] = "double";
                        break;
                    }
                    char pomoc = char.Parse(daneString[j, i][0].ToString());
                    if (pomoc >= (char)65 && pomoc <= (char)90 || pomoc >= (char)97 && pomoc <= (char)122 || pomoc == '+' || pomoc == '-')
                    {
                        typyDanych[i] = "string";
                        break;
                    }
                    typyDanych[i] = "int";
                }
            }
            return typyDanych;
        }

        #endregion

        #region FUNKCJE DO WPF
        private void TbParametrK_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            tb.Text = string.Empty;
            tb.GotFocus -= TbParametrK_GotFocus;
        }

        private void TbWierszDoKlasyfikacji_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            tb.Text = string.Empty;
            tb.GotFocus -= TbWierszDoKlasyfikacji_GotFocus;
        }

        #endregion

        private void Button_Klasyfikuj(object sender, RoutedEventArgs routedEventArgs)
        {
            int k;

            if (!_czyWczytanoDataSet)
            {
                MessageBox.Show("Nie wczytano datasetu");
                return;
            }

            if (string.IsNullOrWhiteSpace(tbParametrK.Text) || tbParametrK.Text == "Podaj parametr K (przedział: '-')")
            {
                MessageBox.Show("Nie podano parametru K do klasyfikacji");
                return;
            }

            if (string.IsNullOrWhiteSpace(tbWierszDoKlasyfikacji.Text) || tbWierszDoKlasyfikacji.Text == "Wpisz wiersz do klasyfikacji")
            {
                MessageBox.Show("Nie podano wiersza do klasyfikacji");
                return;
            }

            if (cbPierwszy.IsChecked == true && cbDrugi.IsChecked == true)
            {
                MessageBox.Show("Wybrano dwie metody klasyfikacji na raz, należy wybrać tylko jedną");
                return;
            }

            if (cbPierwszy.IsChecked == false && cbDrugi.IsChecked == false)
            {
                MessageBox.Show("Nie wybrano żadnej metody klasyfikacji");
                return;
            }

            try
            {
                k = int.Parse(tbParametrK.Text);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return;
            }

            if (k < 0)
            {
                MessageBox.Show("Parametr K nie może być mniejszy od 0");
                return;
            }

            
            _parametrK = k;

            List<List<string>> bazaDoKlasyfikacji = new();
            
            string file = File.ReadAllText(@"dane.txt");

            int max = 0;
            int i = 0;
            int j = 0;
            foreach (var row in file.Split("\n"))
            {
                j = 0;
                bazaDoKlasyfikacji.Add(new List<string>());
                foreach (var col in row.Trim().Split(" "))
                {
                    if (string.IsNullOrWhiteSpace(col))
                        continue;
                    bazaDoKlasyfikacji[i].Add(col);
                    j++;
                    if (max < j)
                        max = j;
                }
                i++;
            }

            if (k > bazaDoKlasyfikacji.Count)
            {
                MessageBox.Show("Parametr K nie może być większy od bazy próbek wzorcowych");
                return;
            }

            List<double> wartosciDoKlasyfikacji = new();
            wartosciDoKlasyfikacji = DaneDoKlasyfikacji(max - 1);

            if (wartosciDoKlasyfikacji.Count == 0)
            {
                MessageBox.Show("Brak wartosci do sprawdzenia");
                return;
            }

            int metryka = 0;
            if (cbMetryki.SelectedIndex == 0)
                metryka = 0;
            if (cbMetryki.SelectedIndex == 1)
                metryka = 1;
            if (cbMetryki.SelectedIndex == 2)
                metryka = 2;
            if (cbMetryki.SelectedIndex == 3)
                metryka = 3;
            if (cbMetryki.SelectedIndex == 4)
                metryka = 4;

            if (cbPierwszy.IsChecked == true)
            {
                MessageBox.Show("Decyzja obliczona pierwszą metodą: " + KNN_PierwszaMetoda(bazaDoKlasyfikacji, wartosciDoKlasyfikacji, _parametrK, metryka));
            }

            if (cbDrugi.IsChecked == true)
            {
                MessageBox.Show("Decyzja obliczona drugą metodą: " + KNN_DrugaMetoda(bazaDoKlasyfikacji, wartosciDoKlasyfikacji, _parametrK, metryka));
            }
        }

        public string KNN_PierwszaMetoda(List<List<string>> bazaDoKlasyfikacji, List<double> wierszDoKlasyfikacji, int parametrK,
            int metryka)
        {
            string koncowaDecyzja = "";
            List<double> listaOdleglosci = new();
            List<string> decyzje = new();
            int liczbaKolumn = 0;
            int max = 0;
            foreach (var lista in bazaDoKlasyfikacji)
            {
                liczbaKolumn = lista.Count;
                if (liczbaKolumn > max)
                    max = liczbaKolumn;
            }

            int liczbaWierszy = bazaDoKlasyfikacji.Count;
            foreach (var lista in bazaDoKlasyfikacji)
            {
                if (lista.Count == 0)
                    liczbaWierszy--;

            }

            liczbaKolumn = max;
            double[,] daneDouble = new double[liczbaWierszy, liczbaKolumn - 1];

            int i = 0;
            foreach (var row in bazaDoKlasyfikacji)
            {
                if(row.Count == 0)
                    continue;
                int j = 0;
                foreach (var col in row)
                {
                    if (j == row.Count - 1)
                    {
                        decyzje.Add(col);
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(col))
                        continue;
                    daneDouble[i, j] = double.Parse(col, CultureInfo.InvariantCulture);
                    j++;
                }
                i++;
            }

            if (metryka == 0)
                listaOdleglosci = MetrykaManhattan(liczbaWierszy, wierszDoKlasyfikacji, daneDouble,
                    liczbaKolumn - 1);
            if (metryka == 1)
                listaOdleglosci = MetrykaEuklidesowa(liczbaWierszy, wierszDoKlasyfikacji, daneDouble,
                    liczbaKolumn - 1);
            if (metryka == 2)
                listaOdleglosci = MetrykaCzebyszewa(liczbaWierszy, wierszDoKlasyfikacji, daneDouble,
                    liczbaKolumn - 1);
            if (metryka == 3)
                listaOdleglosci = MetrykaMinkowskiego(liczbaWierszy, wierszDoKlasyfikacji, daneDouble,
                    liczbaKolumn - 1);
            if (metryka == 4)
                listaOdleglosci = MetrykaZLogarytmem(liczbaWierszy, wierszDoKlasyfikacji, daneDouble,
                    liczbaKolumn - 1);

            
            List<double> znalezioneNajmniejsze = new();
            List<string> decyzjeKonkretne = new();
            for (int j = 0; j < parametrK; j++)
            {
                int indeks = 0;
                double min = Double.MaxValue;
                for (int k = 0; k < listaOdleglosci.Count; k++)
                {
                    if (listaOdleglosci[k] < min && !znalezioneNajmniejsze.Contains(listaOdleglosci[k]))
                    {
                        min = listaOdleglosci[k];
                        indeks = k;
                    }
                }
                decyzjeKonkretne.Add(decyzje[indeks]);
                znalezioneNajmniejsze.Add(min);
            }

            List<string> decyzjeDoZliczenia = new();
            foreach (var t in decyzjeKonkretne)
            {
                if(!decyzjeDoZliczenia.Contains(t))
                    decyzjeDoZliczenia.Add(t);
            }

            int count = 0;
            int pomoc = 0;
            List<int> liczbaDecyzji = new();
            foreach (var decyzja in decyzjeDoZliczenia)
            {
                count = decyzjeKonkretne.Count(x => x.Equals(decyzja));
                liczbaDecyzji.Add(count);
                if (count > pomoc)
                {
                    pomoc = count;
                    koncowaDecyzja = decyzja;
                }
            }

            IEnumerable<int> liczba = liczbaDecyzji.Distinct();
            if (liczba.Count() < liczbaDecyzji.Count)
                koncowaDecyzja = "Odmowa";

            return koncowaDecyzja;
        }

        public string KNN_DrugaMetoda(List<List<string>> bazaDoKlasyfikacji, List<double> wierszDoKlasyfikacji, int parametrK,
            int metryka)
        {
            string koncowaDecyzja = "";
            List<double> listaOdleglosci = new();
            List<string> decyzje = new();
            int liczbaKolumn = 0;
            int max = 0;
            foreach (var lista in bazaDoKlasyfikacji)
            {
                liczbaKolumn = lista.Count;
                if (liczbaKolumn > max)
                    max = liczbaKolumn;
            }

            int liczbaWierszy = bazaDoKlasyfikacji.Count;
            foreach (var lista in bazaDoKlasyfikacji)
            {
                if (lista.Count == 0)
                    liczbaWierszy--;

            }

            liczbaKolumn = max;
            double[,] daneDouble = new double[liczbaWierszy, liczbaKolumn - 1];

            int i = 0;
            foreach (var row in bazaDoKlasyfikacji)
            {
                if (row.Count == 0)
                    continue;
                int j = 0;
                foreach (var col in row)
                {
                    if (j == row.Count - 1)
                    {
                        decyzje.Add(col);
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(col))
                        continue;
                    daneDouble[i, j] = double.Parse(col, CultureInfo.InvariantCulture);
                    j++;
                }
                i++;
            }

            List<int> liczbaPoszczegolnychDecyzji = new();
            var result = decyzje.GroupBy(a => a).Select(x => new {key = x.Key, val = x.Count()});
            foreach (var decyzja in result)
            {
                liczbaPoszczegolnychDecyzji.Add(decyzja.val);
            }

            int pomocMax = 0;
            foreach (var poszczegolnaDecyzja in liczbaPoszczegolnychDecyzji)
            {
                if (poszczegolnaDecyzja > pomocMax)
                    pomocMax = poszczegolnaDecyzja;
            }

            if (parametrK > pomocMax)
            {
                MessageBox.Show("Parametr K nie może być większy niż ilość poszczegolnych decyzji");
                return "BŁĄD PARAMETRU K";
            }


            if (metryka == 0)
                listaOdleglosci = MetrykaManhattan(liczbaWierszy, wierszDoKlasyfikacji, daneDouble,
                    liczbaKolumn - 1);
            if (metryka == 1)
                listaOdleglosci = MetrykaEuklidesowa(liczbaWierszy, wierszDoKlasyfikacji, daneDouble,
                    liczbaKolumn - 1);
            if (metryka == 2)
                listaOdleglosci = MetrykaCzebyszewa(liczbaWierszy, wierszDoKlasyfikacji, daneDouble,
                    liczbaKolumn - 1);
            if (metryka == 3)
                listaOdleglosci = MetrykaMinkowskiego(liczbaWierszy, wierszDoKlasyfikacji, daneDouble,
                    liczbaKolumn - 1);
            if (metryka == 4)
                listaOdleglosci = MetrykaZLogarytmem(liczbaWierszy, wierszDoKlasyfikacji, daneDouble,
                    liczbaKolumn - 1);

            List<List<string>> Scalone = new();
            for (int j = 0; j < listaOdleglosci.Count; j++)
            {
                Scalone.Add(new List<string>());
                Scalone[j].Add(listaOdleglosci[j].ToString(CultureInfo.InvariantCulture));
                Scalone[j].Add(decyzje[j]);
            }

            int liczbaUnikalnychDecyzji = (from x in decyzje select x).Distinct().Count();

            int pomoc = 0;
            List<List<string>> podzialNaDecyzje = new();
            for (int j = 0; j < liczbaUnikalnychDecyzji; j++)
            {
                podzialNaDecyzje.Add(new List<string>());
            }
            podzialNaDecyzje[0].Add(decyzje[0]);
            List<string> dodaneWartosci = new();
            dodaneWartosci.Add(decyzje[0]);

            foreach (var decyzja in decyzje)
            {
                foreach (var lista in podzialNaDecyzje)
                {
                    if (lista.Count != 0)
                        continue;
                    if (!lista.Contains(decyzja) && !dodaneWartosci.Contains(decyzja))
                    {
                        lista.Add(decyzja);
                        dodaneWartosci.Add(decyzja);
                    }
                }

                if (dodaneWartosci.Count() == liczbaUnikalnychDecyzji)
                    break;
            }

            List<double> sumaOdleglosciDecyzji = new();


            List<double> znalezioneNajmniejsze = new();
            for (int z = 0; z < podzialNaDecyzje.Count; z++)
            {
                double suma = 0;
                znalezioneNajmniejsze.Clear();
                for (int j = 0; j < parametrK; j++)
                {
                    int indeks = 0;
                    double min = Double.MaxValue;
                    for (int k = 0; k < listaOdleglosci.Count; k++)
                    {
                        if (listaOdleglosci[k] < min && !znalezioneNajmniejsze.Contains(listaOdleglosci[k]) && Scalone[k][1] == podzialNaDecyzje[z][0])
                        {
                            min = listaOdleglosci[k];
                        }
                    }
                    znalezioneNajmniejsze.Add(min);
                }

                foreach (var minimum in znalezioneNajmniejsze)
                {
                    suma += minimum;
                }
                sumaOdleglosciDecyzji.Add(suma);
            }

            double temp = Double.MaxValue;
            int miejsce = 0;
            foreach (var minimum in sumaOdleglosciDecyzji)
            {
                if (minimum < temp)
                {
                    temp = minimum;
                    miejsce++;
                }
            }
            koncowaDecyzja = podzialNaDecyzje[miejsce - 1][0];

            IEnumerable<double> liczba = sumaOdleglosciDecyzji.Distinct();
            if (liczba.Count() < sumaOdleglosciDecyzji.Count)
                koncowaDecyzja = "Odmowa";
            
            return koncowaDecyzja;
        }

        private void Button_SprawdzDokladnosc(object sender, RoutedEventArgs routedEventArgs)
        {
            int k;

            if (string.IsNullOrWhiteSpace(tbParametrK.Text) || tbParametrK.Text == "Podaj parametr K (przedział: '-')")
            {
                MessageBox.Show("Nie podano parametru K do klasyfikacji");
                return;
            }

            if (!_czyWczytanoDataSet)
            {
                MessageBox.Show("Nie wczytano datasetu");
                return;
            }

            if (cbPierwszy.IsChecked == true && cbDrugi.IsChecked == true)
            {
                MessageBox.Show("Wybrano dwie metody klasyfikacji na raz, należy wybrać tylko jedną");
                return;
            }

            if (cbPierwszy.IsChecked == false && cbDrugi.IsChecked == false)
            {
                MessageBox.Show("Nie wybrano żadnej metody klasyfikacji");
                return;
            }

            try
            {
                k = int.Parse(tbParametrK.Text);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return;
            }

            if (k < 0)
            {
                MessageBox.Show("Parametr K nie może być mniejszy od 0");
                return;
            }
            _parametrK = k;

            List<List<string>> bazaDoKlasyfikacji = new();

            string file = File.ReadAllText(@"dane.txt");

            int max = 0;
            int i = 0;
            int j = 0;
            foreach (var row in file.Split("\n"))
            {
                j = 0;
                bazaDoKlasyfikacji.Add(new List<string>());
                foreach (var col in row.Trim().Split(" "))
                {
                    if (string.IsNullOrWhiteSpace(col))
                        continue;
                    bazaDoKlasyfikacji[i].Add(col);
                    j++;
                    if (max < j)
                        max = j;
                }
                i++;
            }

            List<double> wartosciDoKlasyfikacji = new();

            int metryka = 0;
            if (cbMetryki.SelectedIndex == 0)
                metryka = 0;
            if (cbMetryki.SelectedIndex == 1)
                metryka = 1;
            if (cbMetryki.SelectedIndex == 2)
                metryka = 2;
            if (cbMetryki.SelectedIndex == 3)
                metryka = 3;
            if (cbMetryki.SelectedIndex == 4)
                metryka = 4;

            List<List<double>> daneDouble = new();
            List<string> decyzje = new();
            int z = 0;
            foreach (var row in bazaDoKlasyfikacji)
            {
                if (row.Count == 0)
                    continue;
                int y = 0;
                daneDouble.Add(new List<double>());
                foreach (var col in row)
                {
                    if (y == row.Count - 1)
                    {
                        decyzje.Add(col);
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(col))
                        continue;
                    daneDouble[z].Add(double.Parse(col, CultureInfo.InvariantCulture));
                    y++;
                }
                z++;
            }

            if (k > bazaDoKlasyfikacji.Count)
            {
                MessageBox.Show("Parametr K nie może być większy od bazy próbek wzorcowych");
                return;
            }

            List<string> decyzjePoSprawdzeniu = new();

            if (cbPierwszy.IsChecked == true)
            {
                decyzjePoSprawdzeniu.Clear();
                for (int l = 0; l < daneDouble.Count; l++)
                {
                    try
                    {
                        decyzjePoSprawdzeniu.Add(KNN_PierwszaMetoda(bazaDoKlasyfikacji, daneDouble[l], _parametrK, metryka));
                    }
                    catch (Exception e)
                    {
                        decyzjePoSprawdzeniu.Add("Odmowa");
                    }
                }
            }

            if (cbDrugi.IsChecked == true)
            {
                decyzjePoSprawdzeniu.Clear();
                for (int l = 0; l < daneDouble.Count; l++)
                {
                    try
                    {
                        decyzjePoSprawdzeniu.Add(KNN_DrugaMetoda(bazaDoKlasyfikacji, daneDouble[l], _parametrK, metryka));
                    }
                    catch (Exception e)
                    {
                        decyzjePoSprawdzeniu.Add("Odmowa");
                    }
                }
            }


            string temp = "Odmowa";
            int odmowneDecyzje = decyzjePoSprawdzeniu.Where(x => x.Equals(temp)).Count();
            int t = 0;
            double pokrycie = bazaDoKlasyfikacji.Count - odmowneDecyzje;
            double pokrycieWProcentach = (pokrycie / bazaDoKlasyfikacji.Count) * 100;
            pokrycieWProcentach = Math.Round(pokrycieWProcentach, 1);
            double poprawnieSprawdzone = 0;
            foreach (var decyzjaPoSprawdzeniu in decyzjePoSprawdzeniu)
            {
                if (decyzjaPoSprawdzeniu == decyzje[t])
                {
                    poprawnieSprawdzone++;
                }

                t++;
            }

            double poprawnieSprawdzoneWProcentach = (poprawnieSprawdzone / pokrycie) * 100;
            poprawnieSprawdzoneWProcentach = Math.Round(poprawnieSprawdzoneWProcentach, 1);

            MessageBox.Show("Pokrycie wynosi: " + pokrycieWProcentach + "%, natomiast poprawnie zostało sprawdzonych: " 
                            + poprawnieSprawdzoneWProcentach + "%");
        }

        private void Button_Normalizuj(object sender, RoutedEventArgs e)
        {

            string delimiter = ",";
            bool cbDone = true;
            bool txNotEmpty = true;

            if (cbPrzecinek.IsChecked == true)
            {
                delimiter = ",";
                tbZnak.Text = ",";
            }
            if (cbSpacja.IsChecked == true)
            {
                delimiter = " ";
                tbZnak.Text = "s";
            }
            if (cbSpacja.IsChecked == false && cbPrzecinek.IsChecked == false && string.IsNullOrWhiteSpace(tbZnak.Text))
            {
                MessageBox.Show("Proszę wybrać separator", "Wystąpił błąd");
                cbDone = false;
            }
            if (cbSpacja.IsChecked == false && cbPrzecinek.IsChecked == false)
            {
                if (tbZnak.Text == "s")
                {
                    delimiter = " ";
                    cbSpacja.IsChecked = true;
                }
            }
            if (cbPrzecinek.IsChecked == true && cbSpacja.IsChecked == true)
            {
                MessageBox.Show("Proszę zostawić tylko jeden wybrany separator");
                cbDone = false;
            }
            if (string.IsNullOrWhiteSpace(FileNameTextBox.Text))
            {
                MessageBox.Show("Brak ścieżki do pliku", "Wystąpił błąd");
                txNotEmpty = false;
            }
            if (string.IsNullOrWhiteSpace(tbZnak.Text))
            {
                MessageBox.Show("Brak ustawionego separatora", "Wystąpił błąd");
                txNotEmpty = false;
            }


            var liczbaLinii = File.ReadLines(_plikWejsciowy).Count() - _liczbaLiniiPoZmianach;
            if (_podstawSrednia)
                liczbaLinii = File.ReadLines(_plikWejsciowy).Count();
            var pierwszaLinia = File.ReadLines(_plikWejsciowy).First();
            int liczbaKolumn = 0;
            if (pierwszaLinia != null)
            {
                liczbaKolumn = pierwszaLinia.Split(delimiter).Length;
            }
            MessageBoxResult decyzja;
            List<int> wylaczone = new List<int>();
            decyzja = MessageBox.Show(
                "Czy chcesz znormalizować ostatnią kolumnę?\n"
                + "Tak -> normalizuj || Nie -> zostaw", "Uwaga!", MessageBoxButton.YesNo);
           
            if (decyzja == MessageBoxResult.No)
            {
                _normOstatniaKolumna = true;
                wylaczone.Add(liczbaKolumn - 1);
            }

            if (!string.IsNullOrEmpty(tbWylaczZNormalizacji.Text))
            {
                foreach (var wylaczoneKolumny in tbWylaczZNormalizacji.Text.Split(",").ToList())
                {
                    wylaczone.Add(int.Parse(wylaczoneKolumny) - 1);
                }
            }

            foreach (var number in wylaczone)
            {
                if (number > liczbaKolumn)
                {
                    MessageBox.Show("Nie można zignorować podanych kolumn. Wpisane numery kolumn wykraczają poza zakres");
                    return;
                }
            }
            string[,] tablicaString = new string[liczbaLinii, liczbaKolumn];
            string[,] czystaTablicaString = new string[liczbaLinii, liczbaKolumn];
            double[,] tablicaDouble = new double[liczbaLinii, liczbaKolumn];
            if (cbDone && txNotEmpty)
            {
                tablicaString = DoTablicyString(_plikWejsciowy, liczbaLinii, liczbaKolumn, delimiter);
                czystaTablicaString = DoTablicyString(_plikWejsciowy, liczbaLinii, liczbaKolumn, delimiter);
                tablicaDouble = DoTablicyDouble(tablicaString, liczbaLinii, liczbaKolumn, _plikWejsciowy);

                double[,] pomocMinMax = new double[2, liczbaKolumn];
                pomocMinMax = ZnajdzMinMax(tablicaDouble, liczbaLinii, liczbaKolumn);

                double[] pomocZakres = new double[liczbaKolumn];
                pomocZakres = ObliczZakres(pomocMinMax, liczbaKolumn);

                tablicaDouble = Normalizacja(tablicaDouble, pomocZakres, pomocMinMax, liczbaLinii, liczbaKolumn);
                string[,] tablicaStringDoIgnorowania = new string[liczbaLinii, liczbaKolumn];
                for (int i = 0; i < liczbaKolumn; i++)
                {
                    for (int j = 0; j < liczbaLinii; j++)
                    {
                        if (wylaczone.Contains(i))
                        {
                            tablicaStringDoIgnorowania[j, i] = czystaTablicaString[j, i];
                            continue;
                        }
                        tablicaStringDoIgnorowania[j, i] = tablicaDouble[j, i].ToString(CultureInfo.InvariantCulture);
                    }
                }

                var normalizacjaPlik = "dane.txt";
                var sw = new StreamWriter(normalizacjaPlik);
                for (int k = 0; k < liczbaLinii; k++)
                {
                    for (int l = 0; l < liczbaKolumn; l++)
                    {
                        if (l == liczbaKolumn - 1)
                            sw.Write(tablicaStringDoIgnorowania[k, l]);
                        else
                            sw.Write(tablicaStringDoIgnorowania[k, l] + " ");
                    }
                    sw.Write("\n");
                }
                sw.Flush();
                sw.Close();
                _globalFilename = normalizacjaPlik;
                _globalSaveParagraph.Inlines.Clear();
                _globalDelimiter = delimiter;
                _globalSaveParagraph.Inlines.Add(File.ReadAllText(normalizacjaPlik));
                FlowDocument document = new FlowDocument(_globalSaveParagraph);
                FlowDocReader.Document = document;
                _globalSaveParagraph.FontFamily = new FontFamily("Arial");
                _globalSaveParagraph.FontSize = 15.0;
                _globalSaveParagraph.FontStretch = FontStretches.UltraExpanded;
                _poNormalizacji = true;
                if (_czyZignorowane)
                {
                    _czyZignorowane = false;
                    Button_Ignoruj_Kolumny(sender, e);
                }
            }
        }

        private void MinkowskiegoWybrana(object sender, SelectionChangedEventArgs e)
        {
            if (cbMetryki.SelectedIndex == 3)
            {
                MessageBox.Show("Przy metryce Minkowskiego należy podać dodatkowy parametr P");
                string parametrP = Interaction.InputBox("Podaj parametr P", "Metryka Minkowskiego", "1");
                try
                {
                    _parametrPMinkowskiego = double.Parse(parametrP, CultureInfo.InvariantCulture);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "Wystąpił błąd");
                    MinkowskiegoWybrana(sender, e);
                }

                if (_parametrPMinkowskiego < 0)
                {
                    MessageBox.Show("Parametr P Minkowskiego musi być więskzy od 0");
                    MinkowskiegoWybrana(sender, e);
                }
            }
        }

        public List<double> DaneDoKlasyfikacji(int liczbaKolumn)
        {
            var doKlasyfikacjiString = tbWierszDoKlasyfikacji.Text.Split(" ").ToList();
            List<double> empty = new();

            if (doKlasyfikacjiString.Count() != liczbaKolumn)
            {
                MessageBox.Show("Podano nie prawidłową liczbę elementów do klasyfikacji, nie można tych danych podać do klasyfikacji");
                return empty;
            }

            List<double> doKlasyfikacjiDouble = new();
            foreach (var wartosc in doKlasyfikacjiString)
            {
                try
                {
                    doKlasyfikacjiDouble.Add(double.Parse(wartosc, CultureInfo.InvariantCulture));
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                    return empty;
                }
            }

            var decyzja = MessageBoxResult.Cancel;
            decyzja = MessageBox.Show("Czy chcesz znormalizować wpisany wiersz do klasyfikacji?", "Podejmij decyzję", MessageBoxButton.YesNo);
            if (decyzja == MessageBoxResult.Yes)
            {
                double min = Double.MaxValue;
                double max = 0;
                List<double> doKlasyfikacjiDoubleZnormalizowane = new();
                foreach (var number in doKlasyfikacjiDouble)
                {
                    if (min > number)
                        min = number;
                    if (max < number)
                        max = number;
                }

                double zakres = max - min;
                foreach (var number in doKlasyfikacjiDouble)
                {
                    doKlasyfikacjiDoubleZnormalizowane.Add((number - min) / zakres);
                }

                tbWierszDoKlasyfikacji.Text = string.Join(" ", doKlasyfikacjiDoubleZnormalizowane);
                return doKlasyfikacjiDoubleZnormalizowane;
                
            }
            return doKlasyfikacjiDouble;
        }

        #region METRYKI

        public List<double> MetrykaManhattan(int liczbaAtrybutow, List<double> wartosciDoKlasyfikacji, double[,] daneWzorcowe, int liczbaKolumn)
        {

            List<double> odleglosci = new();
            for (int i = 0; i < liczbaAtrybutow; i++)
            {
                double suma = 0;
                for (int j = 0; j < liczbaKolumn; j++)
                {
                    suma += Math.Abs(wartosciDoKlasyfikacji[j] - daneWzorcowe[i, j]);
                }
                odleglosci.Add(suma);
            }

            return odleglosci;
        }

        public List<double> MetrykaEuklidesowa(int liczbaAtrybutow, List<double> wartosciDoKlasyfikacji, double[,] daneWzorcowe, int liczbaKolumn)
        {

            List<double> odleglosci = new();
            for (int i = 0; i < liczbaAtrybutow; i++)
            {
                double suma = 0;
                for (int j = 0; j < liczbaKolumn; j++)
                {
                    suma += Math.Pow(wartosciDoKlasyfikacji[j] - daneWzorcowe[i, j], 2);
                }
                suma = Math.Sqrt(suma);
                odleglosci.Add(suma);
            }

            return odleglosci;
        }

        public List<double> MetrykaCzebyszewa(int liczbaAtrybutow, List<double> wartosciDoKlasyfikacji, double[,] daneWzorcowe, int liczbaKolumn)
        {
            List<double> odleglosci = new();
            List<double> doSzukaniaMax = new();
            
            for (int i = 0; i < liczbaAtrybutow; i++)
            {
                double suma = 0;
                double max = 0;
                doSzukaniaMax.Clear();

                for (int j = 0; j < liczbaKolumn; j++)
                {
                    doSzukaniaMax.Add(Math.Abs(wartosciDoKlasyfikacji[j] - daneWzorcowe[i, j]));
                }

                foreach (var number in doSzukaniaMax)
                {
                    if (number > max)
                    {
                        max = number;
                    }
                }
                odleglosci.Add(max);
            }

            return odleglosci;
        }

        public List<double> MetrykaMinkowskiego(int liczbaAtrybutow, List<double> wartosciDoKlasyfikacji, double[,] daneWzorcowe, int liczbaKolumn)
        {
            List<double> odleglosci = new();
            for (int i = 0; i < liczbaAtrybutow; i++)
            {
                double suma = 0;
                for (int j = 0; j < liczbaKolumn; j++)
                {
                    suma += Math.Pow(Math.Abs(wartosciDoKlasyfikacji[j] - daneWzorcowe[i, j]), _parametrPMinkowskiego);
                }

                suma = Math.Pow(suma, 1 / _parametrPMinkowskiego);
                odleglosci.Add(suma);
            }

            return odleglosci;
        }

        public List<double> MetrykaZLogarytmem(int liczbaAtrybutow, List<double> wartosciDoKlasyfikacji, double[,] daneWzorcowe, int liczbaKolumn)
        {
            List<double> odleglosci = new();
            for (int i = 0; i < liczbaAtrybutow; i++)
            {
                double suma = 0;
                for (int j = 0; j < liczbaKolumn; j++)
                {
                    suma += Math.Abs(Math.Log(wartosciDoKlasyfikacji[j]) - Math.Log(daneWzorcowe[i, j]));
                }
                odleglosci.Add(suma);
            }

            return odleglosci;
        }

        #endregion
    }
}