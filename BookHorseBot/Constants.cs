namespace BookHorseBot
{
    public class Constants
    {
        public static string Footer 
            => "\r\n \r\n" +
            "This is a bot | [Report problems](/message/compose/?to=BitzLeon&subject=Bookhorsebot running BHB 0.0.2)";

        public static string NoResults
            => "[](/twisad) \r\n" +
            "[I'm sorry, I looked everywhere but I couldn't find that fanfic...](https://www.youtube.com/watch?v=BmRAGl1BOiQ)";
        
        public static string NotAllowed
            => "[](/twirage) \r\n  Please don't link mature rated fanfics in this sub! It isn't allowed!";

        public static string RegexBbCode(string tag)
        {
            return $@"\[{tag}\]((?:.|\n)+?)\[\/{tag}\]";
        }
    }
}
