using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using BookHorseBot.Functions;
using BookHorseBot.Models;
using Newtonsoft.Json;
using RedditSharp;
using RedditSharp.Things;

using static BookHorseBot.Configuration;
using static BookHorseBot.Models.Misc;
using static BookHorseBot.Models.Misc.Command.RequestType;
using Reddit = RedditSharp.Reddit;

namespace BookHorseBot
{
    class Program
    {
        public static HttpClient BotClient = new HttpClient();

        static readonly List<string> CommandList = new List<String>
        {
            "s:",    //StoryData Lookup - using Ids!
            "c:"     //Command - various!
        };

        static void Main()
        {
            bool dbug = Debugger.IsAttached;
            Console.Title = "BookHorseBot " + Constants.Version;
            List<string> ignoredUsers = C.Ignored.User;
            //Does all the dirty work of handling oAuth and tokens. Gives botclient authentication.
            AuthorizeFimFictionBot();
            Reddit reddit = AuthorizeRedditBot();
            string redditName = reddit.User.FullName;
            Console.WriteLine(dbug ? "Debug detected. Running on test subreddit!" : "Running on Main subreddit!");
            Subreddit subreddit = reddit.GetSubreddit(dbug ? "bronyvillers" : "mylittlepony");
            IEnumerable<Comment> commentStream =
                subreddit.CommentStream.Where(c => !ignoredUsers.Contains(c.AuthorName.ToLower())
                                                   && c.CreatedUTC >= DateTime.UtcNow.AddMinutes(-15) &&
                                                   c.AuthorName.ToLower() != redditName.ToLower()
                );
            

            foreach (Comment comment in commentStream)
            {
                //Look for { and }, if none are found, skip!
                MatchCollection matches = Regex.Matches(comment.Body, @"(?<=\{)[^}]*(?=\})", RegexOptions.None);
                if (matches.Count == 0)
                {
                    continue;
                }
                //Check to see if we already replied to this comment.
                Comment qualifiedComment = reddit.GetComment(new Uri(comment.Shortlink));
                if (qualifiedComment.Comments.All(x => x.AuthorName != redditName))
                {
                    string username = qualifiedComment.AuthorName.ToLower();
                    List<Command> commands = ExtractCommands(matches, username);
                    if (commands.Count > 0)
                    {
                        GetPostText(commands);
                        string postReplyBody = GeneratePostBody(commands);
                        comment.Reply(postReplyBody);
                        Console.WriteLine($"Reply posted to {comment.AuthorName}!");
                    }
                }
            }
        }

        private static List<Command> ExtractCommands(MatchCollection matches, string username)
        {
            List<Command> list = new List<Command>();
            foreach (Match match in matches)
            {
                Command c = new Command {Username = username};
                //Check to see if our request is a valid URL
                Uri uriResult;
                if (Uri.TryCreate(match.Value, UriKind.Absolute, out uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                {
                    if (new Uri(match.Value).Host.Contains("fimfiction.net"))
                    {
                        c.Request = match.Value;
                        c.Type = SearchUrl;
                    }
                }
                else
                {
                    MatchCollection parenthesisMatches = Regex.Matches(match.Value.Trim(), @"(?<=\()[^)]*(?=\))", RegexOptions.None);
                    //True- linking with a url using markup. Use the url, and fallback to the textinside
                    if (parenthesisMatches.Count == 1)
                    {
                        if (Uri.TryCreate(parenthesisMatches[0].Value, UriKind.Absolute, out uriResult) && 
                            (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps) &&
                            uriResult.Host.Contains("fimfiction.net"))
                        {
                                c.Request = parenthesisMatches[0].Value;
                                c.Type = SearchUrl;
                        }
                        else
                        {
                            MatchCollection sBracketMatches = Regex.Matches(match.Value.Trim(), @"(?<=\[)[^]]*(?=\])", RegexOptions.None);
                            if (sBracketMatches.Count == 1)
                            {
                                c.Request = Uri.EscapeDataString(sBracketMatches[0].Value.Trim());
                                c.Type = SearchName;
                            }
                        }
                    }
                    else
                    {
                        c.Request = Uri.EscapeDataString(match.Value.Trim());
                        c.Type = SearchName;
                    }
                }
                list.Add(c);
            }
            return list;
        }

        private static string GeneratePostBody(List<Command> commands)
        {
            string template = "";
            foreach (Command r in commands)
            {
                if (r.Type == SearchId || r.Type == SearchName || r.Type == SearchUrl)
                {
                var root = (StoryData.Story) r.Response;
                if (root.data.Length == 0)
                {
                    template += Constants.NoResults;
                    continue;
                }
                StoryData.Datum story = root.data.First();
                if (story.attributes.content_rating == "mature")
                {
                    template += Constants.NotAllowed;
                }
                else
                {
                    string u = GetAuthorUsername(root);
                    template += "\r\n [](/twibeam) \r\n" +
                                $"#[{story.attributes.title}]({story.meta.url})\r\n" +
                                $"*by [{u}](https://www.fimfiction.net/user/{story.relationships.author.data.id}/{u}) " +
                                $"| {story.attributes.date_published:dd MMM yyyy} " +
                                $"| {Utils.FormatNumber(story.attributes.total_num_views)} Views" +
                                $"| {Utils.FormatNumber(story.attributes.num_words)} Words " +
                                $"| Status: `{Utils.UppercaseFirst(story.attributes.completion_status)}` " +
                                $"| Rating: `\U0001F44D {story.attributes.num_likes} | \U0001F44E {story.attributes.num_dislikes}`*\r\n\r\n" +
                                $"{story.attributes.short_description}" +
                                "\r\n\r\n" +
                                $"**Tags**: {GenerateTags(root)}";
                }
                template += "[](//sp)" +
                            "\r\n \r\n" +
                            "-----";
                }

                if (r.Type == OptOut)
                {
                    template += Constants.OptOut;
                }
            }

            template += Constants.Footer;

            return template;
        }

        private static string GetAuthorUsername(StoryData.Story s)
        {
            string authorId = s.data.First().relationships.author.data.id;

            var authorName = s.included.First(x => x.id == authorId && x.type == "user").attributes.name;
            return authorName;
        }

        private static string GenerateTags(StoryData.Story relationshipsTags)
        {
            List<string> tagIds =
                relationshipsTags.data.First().relationships.tags.data.Select(datum1 => datum1.id).ToList();
            List<string> tagNames = new List<string>();
            foreach (string tagId in tagIds)
            {
                string tagName =
                    relationshipsTags.included.First(x => x.id == tagId && x.type == "story_tag").attributes.name;
                tagNames.Add(tagName);
            }

            string builtTagLineContent = string.Join("`, `", tagNames);
            builtTagLineContent = "`" + builtTagLineContent + "`";
            return builtTagLineContent;
        }

        private static void GetPostText(List<Command> sanitizedNames)
        {
            foreach (Command c in sanitizedNames)
            {
                string command = c.Request;
                if (CommandList.Any(x => command.StartsWith(x.ToLower())))
                {
                    string commandHeader = command.Split(':').First().ToLower();
                    string commandBody = command.Substring(command.IndexOf(':') + 1,
                        command.Length - 2);
                    switch (commandHeader)
                    {
                        case "s":
                        case "story":
                        {
                                c.Type = SearchId;
                                c.Response = StoryIdLookup(commandBody);
                        }
                            break;
                        case "c":
                        case "command":
                        {
                            if (commandBody.ToLower() == "stop")
                            {
                                C.Ignored.User.Add(c.Username);
                                Save.Config();
                                c.Type = OptOut;
                                c.Result = Command.RequestResult.Success;
                            }
                        }
                            break;
                    }
                }
                else
                {
                    if (c.Type == SearchUrl)
                    {
                        var s = c.Request.Split('/').ToList();
                        int index = s.FindIndex(x => x == "story") + 1;
                        string id = s[index];
                        c.Response = StoryIdLookup(id);
                    }
                    else
                    {
                        command = command.Replace(":", " ");
                        string queryUrl = Constants.StoryQueryUrl($"?query={command}&");
                        string res =
                            BotClient.GetStringAsync(queryUrl)
                                .Result;
                        StoryData.Story searchResult = JsonConvert.DeserializeObject<StoryData.Story>(res);
                        c.Type = SearchName;
                        c.Response = searchResult;
                    }
                }
            }
        }

        private static StoryData.Story StoryIdLookup(string id)
        {
            string queryUrl = Constants.StoryQueryUrl($"/{id}?");
            string res = BotClient.GetStringAsync(queryUrl).Result;
            StoryData.StorySingle searchResult =
                JsonConvert.DeserializeObject<StoryData.StorySingle>(res);
            StoryData.Story r = new StoryData.Story
            {
                data = new StoryData.Datum[1]
            };
            r.data[0] = searchResult.data;
            r.included = searchResult.included;
            return r;
        }

        #region Webclient Authorizations
        private static Reddit AuthorizeRedditBot()
        {
            BotWebAgent webAgent = new BotWebAgent(C.Reddit.Username,
                C.Reddit.Password,
                C.Reddit.ClientId,
                C.Reddit.ClientSecret,
                "https://google.com");

            Reddit reddit = new Reddit(webAgent, true);
            reddit.LogIn(C.Reddit.Username, C.Reddit.Password);
            string redditName = reddit.User.FullName;
            if (redditName.ToLower() == C.Reddit.Username.ToLower())
            {
                Console.WriteLine("Logged in!");
            }
            return reddit;
        }

        private static void AuthorizeFimFictionBot()
        {
            if (string.IsNullOrEmpty(C.FimFiction.Token))
            {
                string receiveStream = Get.FimFictionGetAuthToken(BotClient);
                FfAuthorization authorization = JsonConvert.DeserializeObject<FfAuthorization>(receiveStream);
                C.FimFiction.Token = authorization.access_token;
                Save.Config();
            }

            BotClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            BotClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                C.FimFiction.Token);
        }
        #endregion
    }
}
