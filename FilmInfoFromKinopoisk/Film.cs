using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace GetFilmInfoFromKinopoisk
{
    public class Film
    {
        public Int32 Id { get; set; }

        public String Name { get; set; }

        public String NameEng { get; set; }

        public String Year { get; set; }

        [ScriptIgnore]
        public DateTime DatePremierWorld { get; set; }

        [ScriptIgnore]
        public DateTime DateDVD { get; set; }


        public override string ToString()
        {
            return String.Format("\t{0} ({1}) ({2})", Name, NameEng, Year);
        }
    }
}
