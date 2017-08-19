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
            "[I'm sorry, I looked everywhere but I couldn't find that fanfic...](https://www.youtube.com/watch?v=BmRAGl1BOiQ)\r\n\r\n";


        public static string OptOut
            => "\r\n [](/twisad) \r\n" +
            "[Okay, I won't bother you anymore...](https://www.youtube.com/watch?v=BmRAGl1BOiQ)  \r\n \r\n" +
            "To opt back in, just reply to any post I make with \"{C:Start}\"...";
        
        public static string NotAllowed
            => "\r\n [](/twirage) \r\n" +
               "Please don't link mature rated fanfics in this sub! It isn't allowed! \r\n\r\n";

        public static string FimFictionUrl => "https://www.fimfiction.net/api/v2"; //No trailing slash

        public static string StoryQueryUrl(string query)
        {
            string url = $"{FimFictionUrl}/stories" +
                         $"{query}" +
                         "sort=-relevance" +
                         "&page[size]=1" +
                         "&fields[user]=name,meta" +
                         "&fields[story]=title,short_description,date_published,total_num_views,num_words,num_likes,num_dislikes,completion_status,tags,content_rating,author" +
                         "&fields[story_tag]=name,type" +
                         "&include=characters,tags,author";
            return url;
        }
    }
}
