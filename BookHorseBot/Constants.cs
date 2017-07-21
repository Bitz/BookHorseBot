using System.Reflection;

namespace BookHorseBot
{
    public class Constants
    {
        public static string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public static string Footer 
            => "\r\n \r\n" +
            $"This is a bot | [Report problems](/message/compose/?to=BitzLeon&subject=Bookhorsebot running BHB {Version}) | [Source](https://github.com/Bitz/BookHorseBot) | [Info](https://bitz.rocks/bookhorsebot/)";

        public static string NoResults
            => "\r\n [](/twisad) \r\n" +
            "[I'm sorry, I looked everywhere but I couldn't find that fanfic...](https://www.youtube.com/watch?v=BmRAGl1BOiQ)";
        
        public static string NotAllowed
            => "\r\n [](/twirage) \r\n" +
               "  Please don't link mature rated fanfics in this sub! It isn't allowed!";

        public static string StoryQueryUrl(string query)
        {
            string url = "https://www.fimfiction.net/api/v2/stories" +
                         $"{query}" +
                         "sort=-relevance" +
                         "&page[size]=1" +
                         "&fields[user]=name,meta" +
                         "&fields[story]=title,short_description,date_published,total_num_views,num_words,rating,completion_status,tags,content_rating,author" +
                         "&fields[story_tag]=name,type" +
                         "&include=characters,tags,author";
            return url;
        }
    }
}
