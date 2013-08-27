using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Linq;
using System.Configuration;

using HtmlAgilityPack;
using ExtendedLibrary;


namespace GetFilmInfoFromKinopoisk
{
    // Kinopoisk search link.
    // "http://www.kinopoisk.ru/s/type/film/find/ + movieName + /m_act%5Byear%5D/ + movieYear";

    class Program
    {
        #region Private fields

        private readonly String Host = ConfigurationManager.AppSettings["Host"];
        private readonly String SearchLinkTemplate = ConfigurationManager.AppSettings["SearchLinkTemplate"];
        private readonly String FilmLinkTemplate = ConfigurationManager.AppSettings["FilmLinkTemplate"];
        private readonly String OutputFileName = ConfigurationManager.AppSettings["OutputFileName"];
        private readonly String FilmYear = ConfigurationManager.AppSettings["FilmYear"];

        private readonly String[] filmNames = ConfigurationManager.AppSettings["FilmList"]
            .Split(new String[] {", "}, StringSplitOptions.RemoveEmptyEntries);

        #endregion


        static void Main(String[] args)
        {
            var program = new Program();
            program.GetFilmsInfo();
        }


        public void GetFilmsInfo()
        {
            Console.WriteLine("Start getting info...\n");
            var filmList = new List<Film>();

            foreach (var filmName in filmNames)
            {
                var film = new Film() { Name = filmName };

                // Get html page from kinoppoisk.
                String searchString = String.Format(SearchLinkTemplate, filmName, FilmYear);
                var html = ExtendedHtmlHelpers.GetResponseHtml(searchString, Host);
                if (String.IsNullOrEmpty(html))
                {
                    Console.WriteLine("Can't find info for " + filmName);
                    continue;
                }

                // Get film URL.
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(html);
                var filmUrl = htmlDocument.DocumentNode
                                          .SelectNodes("//div[@class='info']/p[@class='name']/a[@href]")
                                          .First()
                                          .Attributes["href"].Value;

                // Get film ID.
                var result = Regex.Match(filmUrl, @"film\/(\d)+").Value.Substring(5);
                int id;
                Int32.TryParse(result, out id);
                film.Id = id;

                // Get film properties.
                html = ExtendedHtmlHelpers.GetResponseHtml(String.Format(FilmLinkTemplate, id), Host);
                htmlDocument.LoadHtml(html);

                try
                {
                    film.NameEng = HttpUtility.HtmlDecode(GetInnerText(htmlDocument, "//div[@id='headerFilm']/span[@itemprop='alternativeHeadline']"));
                    film.Year = HttpUtility.HtmlDecode(GetInnerText(htmlDocument, "//table[@class='info']//a[@title='']"));
                    film.DatePremierWorld = Convert.ToDateTime(
                        HttpUtility.HtmlDecode(
                            GetInnerText(htmlDocument, "//td[@id='div_world_prem_td2']//a[@href]")));
                    film.DateDVD = Convert.ToDateTime(
                        HttpUtility.HtmlDecode(
                            GetInnerText(htmlDocument, "//td[@class='calendar dvd']//a[@href]")));
                    Console.WriteLine(film.ToString());
                }
                catch (Exception)
                {
                    Console.WriteLine("\tDidn't get a full info for film " + filmName);
                }
                filmList.Add(film);
            }
            // Save result.
            var films = filmList.OrderBy(x => x.DatePremierWorld);
            ExtendedIOHelpers.SaveAsJsonToFile(OutputFileName, films);
            ExtendedIOHelpers.SaveToFile("films.csv", Encoding.Default, ToUsualView(films));
        }

        private String GetInnerText(HtmlDocument htmlDocument, String xpath)
        {
            return htmlDocument.DocumentNode
                               .SelectNodes(xpath)
                               .First()
                               .InnerText;
        }

        private String ToUsualView(IEnumerable<Film> films)
        {
            StringBuilder builder = new StringBuilder();
            foreach (var film in films)
            {
                builder.Append(film.Name + ";" + film.DatePremierWorld.ToString("dd MMMM yyyy") + ";" + film.DateDVD.ToString("dd MMMM yyyy") + Environment.NewLine);
            }
            return builder.ToString();
        }
    }
}
