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

using Reddit = RedditSharp.Reddit;

namespace BookHorseBot
{
    class Program
    {
        public static HttpClient BotClient = new HttpClient();

        static readonly List<string> CommandList = new List<String>
        {
            "S:" //StoryData Lookup - using Ids!
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
                                                   && c.CreatedUTC >= DateTime.UtcNow.AddMinutes(-15)
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
                    List<Command> commands = ExtractCommands(matches);
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

        private static List<Command> ExtractCommands(MatchCollection matches)
        {
            List<Command> list = new List<Command>();
            foreach (Match match in matches)
            {
                Command c = new Command();
                //Check to see if our request is a valid URL
                if (Uri.IsWellFormedUriString(match.Value, UriKind.RelativeOrAbsolute))
                {
                    if (new Uri(match.Value).Host.Contains("fimfiction.net"))
                    {
                        c.Request = match.Value;
                        c.Type = Command.RequestType.SearchUrl;
                    }
                }
                else
                {
                    c.Request = Regex.Replace(match.Value.Trim(), @"[^a-zA-Z0-9 :]", "");
                }
                list.Add(c);
            }
            return list;
        }

        private static string GeneratePostBody(List<Command> command)
        {
            string template = "";
            foreach (Command r in command.Where(t => t.Response.GetType() == typeof(StoryData.Story)))
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
                    string u = GetUsername(root);
                    template += "\r\n [](/twibeam) \r\n" +
                                $"#[{story.attributes.title}]({story.meta.url})\r\n" +
                                $"*by [{u}](https://www.fimfiction.net/user/{story.relationships.author.data.id}/{u}) " +
                                $"| {story.attributes.date_published:dd MMM yyyy} " +
                                $"| {Utils.FormatNumber(story.attributes.total_num_views)} Views" +
                                $"| {Utils.FormatNumber(story.attributes.num_words)} Words " +
                                $"| Status: `{Utils.UppercaseFirst(story.attributes.completion_status)}` " +
                                $"| Rating: `{(double) story.attributes.rating}%`*\r\n\r\n" +
                                $"{story.attributes.short_description}" +
                                "\r\n\r\n" +
                                $"**Tags**: {GenerateTags(root)}";
                }
                template += "[](//sp)" +
                            "\r\n \r\n" +
                            "-----";
            }

            template += Constants.Footer;

            return template;
        }

        private static string GetUsername(StoryData.Story s)
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
                if (CommandList.Any(x => command.StartsWith(x)))
                {
                    string commandHeader = command.Split(':').First().ToLower();
                    string commandBody = command.Substring(command.IndexOf(':') + 1,
                        command.Length - 2);
                    switch (commandHeader)
                    {
                        case "s":
                        case "story":
                        {
                                c.Type = Command.RequestType.SearchId;
                                c.Response = StoryIdLookup(commandBody);
                        }
                            break;
                    }
                }
                else
                {
                    if (c.Type == Command.RequestType.SearchUrl)
                    {
                        var s = c.Request.Split('/').ToList();
                        int index = s.FindIndex(x => x == "story") + 1;
                        string id = s[index];
                        c.Response = StoryIdLookup(id);
                    }
                    else
                    {
                        string queryUrl = Constants.StoryQueryUrl($"?query={command}&");
                        string res =
                            BotClient.GetStringAsync(queryUrl)
                                .Result;
                        StoryData.Story searchResult = JsonConvert.DeserializeObject<StoryData.Story>(res);
                        c.Type = Command.RequestType.SearchName;
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
