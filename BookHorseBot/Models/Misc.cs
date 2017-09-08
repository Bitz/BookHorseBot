

namespace BookHorseBot.Models
{
    class Misc
    {

        public class FfAuthorization
        {
            public string access_token { get; set; }
            public string token_type { get; set; }
        }


        //Will eventually group each request with their respective responses to help group data
        public class Command
        {
            public string Request { get; set; }
            public string Username { get; set; }
            public RequestType Type { get; set; }
            public RequestResult Result { get; set; } = RequestResult.NothingHappened;
            public object Response { get; set; } = null;
            
            public enum RequestResult
            {
                Success,        //All good! 
                Fail,           //Some error or exception occured when performing the command.
                NotFound,       //Search or request returned no results
                NothingHappened //Nothing happened means the application didn't do anything....Yet
            }

            public enum RequestType
            {
                SearchName,    //{My Little Dashie}
                SearchId,      //{S:1888}
                SearchUrl,      //{https://www.fimfiction.net/story/1888/my-little-dashie}
            }
        }
    }
}
