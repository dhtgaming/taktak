using System;
using System.Collections.Generic;
using System.Text;

namespace KNN
{
    public class Configuration
    {
        public string Separator { get; set; }
        public int LiczbaLinii { get; set; }
        public int LiczbaKolumn { get; set; }
        public List<List<string>> ZakresAtrybutow { get; set; }
        public List<double> WartosciMinimalneWKolumnie { get; set; }
        public List<double> WartosciMaksymalneWKolumnie { get; set; }
        public List<string> TypyDanych { get; set; }
        public string ZnakZapytania { get; set; }
        public int Decyzja { get; set; }
    }
}
