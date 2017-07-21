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
            string redditName = C.Reddit.Username;
            bool dbug = Debugger.IsAttached;
            Console.Title = "BookHorseBot " + Constants.Version;
            List<string> ignoredUsers = C.Ignored.User;
            //Does all the dirty work of handling oAuth and tokens. Gives botclient authentication.
            AuthorizeFimFictionBot();
            Reddit reddit = AuthorizeRedditBot();
            redditName = reddit.User.FullName;
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
                    List<string> commands = ExtractCommands(matches);
                    if (commands.Count > 0)
                    {
                        List<StoryData.Story> stories = GetPostText(commands);
                        string postReplyBody = GeneratePostBody(stories);
                        comment.Reply(postReplyBody);
                        Console.WriteLine($"Reply posted to {comment.AuthorName}!");
                    }
                }
            }
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

        private static List<string> ExtractCommands(MatchCollection matches)
        {
            List<string> list = new List<string>();
            foreach (Match match in matches)
            {
                list.Add(Regex.Replace(match.Value.Trim(), @"[^a-zA-Z0-9 :]", ""));
            }
            return list;
        }

        private static string GeneratePostBody(List<StoryData.Story> rootList)
        {
            string template = "";
            foreach (var root in rootList)
            {
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
            if (rootList.All(x => x.data.Length == 0) || rootList.Count == 0)
            {
                template = Constants.NoResults;
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

        private static List<StoryData.Story> GetPostText(List<string> sanitizedNames)
        {
            List<StoryData.Story> resultCollection = new List<StoryData.Story>();
            foreach (var sanitizedName in sanitizedNames)
            {
                if (CommandList.Any(x => sanitizedName.StartsWith(x)))
                {
                    string commandBody = sanitizedName.Substring(sanitizedName.IndexOf(':') + 1,
                        sanitizedName.Length - 2);
                    switch (sanitizedName.Split(':').First().ToLower())
                    {
                        case "s":
                        case "story":
                        {
                            StoryData.Story r = StoryIdLookup(commandBody);
                            resultCollection.Add(r);
                        }
                            break;
                    }
                }
                else
                {
                    if (Uri.IsWellFormedUriString(sanitizedName, UriKind.RelativeOrAbsolute))
                    {
                        Uri myUri = new Uri(sanitizedName);
                        if (myUri.Host.Contains("fimfiction.net"))
                        {
                            var s = myUri.ToString().Split('/').ToList();
                            int index = s.FindIndex(x => x == "story") + 1;
                            string id = s[index];
                            StoryData.Story r = StoryIdLookup(id);
                            resultCollection.Add(r);
                        }
                        else
                        {
                            //Pass a null to tell the user that nothing was found.
                            StoryData.Story r = new StoryData.Story();
                            resultCollection.Add(r);
                        }
                    }
                    else
                    {
                        string queryUrl = Constants.StoryQueryUrl($"?query={sanitizedName}&");
                        string res =
                            BotClient.GetStringAsync(queryUrl)
                                .Result;
                        StoryData.Story searchResult = JsonConvert.DeserializeObject<StoryData.Story>(res);
                        resultCollection.Add(searchResult);
                    }
                }
            }
            return resultCollection;
        }

        private static StoryData.Story StoryIdLookup(string ID)
        {
            string queryUrl = Constants.StoryQueryUrl($"/{ID}?");
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
    }
}
