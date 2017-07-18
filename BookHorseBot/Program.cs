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

        static List<string> commandList = new List<String>
        {
            "S:" //Story Lookup - using Ids!
        };

        static void Main()
        {
            Console.Title = "BookHorseBot " + Constants.Version;
            List<string> ignoredUsers = new List<string> {"nightmirrormoon"};
           
            //Does all the dirty work of handling oAuth and tokens. Gives botclient authentication.
            AuthorizeFimFictionBot();
            Reddit reddit = AuthorizeRedditBot();
            string redditName = reddit.User.FullName;

            bool dbug = Debugger.IsAttached;
            Console.WriteLine(dbug ? "Debug detected. Running on test subreddit!" : "Running on Main subreddit!");
            Subreddit subreddit = reddit.GetSubreddit(dbug ? "bronyvillers" : "mylittlepony");

            IEnumerable<Comment> commentStream =
                subreddit.CommentStream.Where(c => !ignoredUsers.Contains(c.AuthorName.ToLower())
                                                   && c.CreatedUTC >= DateTime.UtcNow.AddMinutes(-15)
                );

            foreach (Comment comment in commentStream)
            {
                if (!comment.Body.Contains("{") && !comment.Body.Contains("}"))
                {
                    continue;
                }
                Comment qualifiedComment = reddit.GetComment(new Uri(comment.Shortlink));
                if (qualifiedComment.Comments.All(x => x.AuthorName != redditName))
                {
                    List<string> list = ExtractStoryNames(comment);
                    if (list.Count > 0)
                    {
                        List<Story.Rootobject> stories = GetPostText(list);
                        string postReplyBody = GeneratePostBody(stories);
                        comment.Reply(postReplyBody);
                        Console.WriteLine($"Reply posted to {comment.AuthorName}!");
                    }
                }
            }
        }

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

        private static List<string> ExtractStoryNames(Comment comment)
        {
            List<string> list = new List<string>();
            MatchCollection matches = Regex.Matches(comment.Body, @"(?<=\{)[^}]*(?=\})", RegexOptions.None);
            foreach (Match match in matches)
            {
                list.Add(Regex.Replace(match.Value.Trim(), @"[^a-zA-Z0-9 :]", ""));
            }
            return list;
        }

        private static string GeneratePostBody(List<Story.Rootobject> rootList)
        {
            string template = "";
            foreach (var root in rootList)
            {
                if (root.data.Length == 0)
                {
                    template += Constants.NoResults;
                    continue;
                }
                Story.Datum story = root.data.First();
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

        //private static string GenerateDescription(string description)
        //{
        //    if (description.Length > 500)
        //    {
        //        description = description.Substring(0, 500);
        //    }
        //    if (description.Contains("[url=") && !description.Contains("[/url]"))
        //    {
        //        //If we stripped part of the body and didn't keep the closing tag for this.
        //        description = Regex.Replace(description, @"\[url=(.+?)\]", "");
        //    }
        //    //Bold
        //    description = Regex.Replace(description, Constants.RegexBbCode("b"), match => "**" + match.Groups[1] + "**",
        //        RegexOptions.Multiline & RegexOptions.IgnoreCase);
        //    //Italics
        //    description = Regex.Replace(description, Constants.RegexBbCode("i"), match => "*" + match.Groups[1] + "*",
        //        RegexOptions.Multiline & RegexOptions.IgnoreCase);
        //    //Strike-through
        //    description = Regex.Replace(description, Constants.RegexBbCode("s"), match => "~~" + match.Groups[1] + "~~",
        //        RegexOptions.Multiline & RegexOptions.IgnoreCase);
        //    //URL
        //    description = Regex.Replace(description, @"\[url=(.+?)\]((?:.|\n)+?)\[\/url\]",
        //        match => $"[{match.Groups[2]}]({match.Groups[1]})", RegexOptions.Multiline & RegexOptions.IgnoreCase);
        //    //Everything else
        //    description = Regex.Replace(description, @"[[\/\!]*?[^\[\]]*?](?!\()", "",
        //        RegexOptions.Multiline & RegexOptions.IgnoreCase);
        //    return description;
        //}

        private static string GetUsername(Story.Rootobject s)
        {
            string authorId = s.data.First().relationships.author.data.id;

            var authorName = s.included.First(x => x.id == authorId && x.type == "user").attributes.name;
            return authorName;
        }

        private static string GenerateTags(Story.Rootobject relationshipsTags)
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

        private static List<Story.Rootobject> GetPostText(List<string> sanitizedNames)
        {
            List<Story.Rootobject> resultCollection = new List<Story.Rootobject>();
            foreach (var sanitizedName in sanitizedNames)
            {
                if (commandList.Any(x => sanitizedName.StartsWith(x)))
                {
                    string commandBody = sanitizedName.Substring(2, sanitizedName.Length);
                    switch (sanitizedName.Substring(0, 2))
                    {
                        case "S:":
                        {
                            string queryUrl = Constants.StoryQueryUrl($"/{commandBody}?");
                                Story.Rootobject searchResult = JsonConvert.DeserializeObject<Story.Rootobject>(queryUrl);
                            resultCollection.Add(searchResult);
                        }
                            break;
                    }
                }
                else
                {
                    string queryUrl = Constants.StoryQueryUrl($"?query={sanitizedName}");
                    string res =
                        BotClient.GetStringAsync(queryUrl)
                            .Result;
                    Story.Rootobject searchResult = JsonConvert.DeserializeObject<Story.Rootobject>(res);
                    resultCollection.Add(searchResult);
                }
            }
            return resultCollection;
        }
    }
}
