using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

public class PostHandler : IHttpHandler
{
    public void ProcessRequest(HttpContext context)
    {
        //Site.ValidateToken(context); Already logged in...

        if (!context.User.Identity.IsAuthenticated)
            throw new HttpException(403, "No access");

        string mode = context.Request.QueryString["mode"];
        string slug = context.Request.Form["id"];

        if (mode == "file")
        {
            if (context.Request.Files.Count > 0)
            {
                try
                {
                    var file = context.Request.Files[0];
                    var fileName = file.FileName;
                    var location = "~/files";
                    bool isMedia = !fileName.ToLower().EndsWith(".md");
                    if (isMedia)
                        location += "/media";
                    var path = context.Server.MapPath(location);
                    var fullPath = Path.Combine(path, fileName);
                    file.SaveAs(fullPath);
                    if (!isMedia)
                    {
                        Site.ClearStartPageCache();
                        HttpRuntime.Cache.Remove("items");
                    }
                }
                catch
                {
                    throw new HttpException(400, "File failed to save.");
                }
            }
            else
            {
                throw new HttpException(400, "No file to upload.");
            }
        }
        else if (mode == "media")
        {
            if (context.Request.Files.Count > 0)
            {
                try
                {
                    var file = context.Request.Files[0];
                    var fileName = file.FileName;
                    var path = context.Server.MapPath("~/files/media");
                    var fullPath = Path.Combine(path, fileName);
                    file.SaveAs(fullPath);
                    Site.ClearStartPageCache();
                    HttpRuntime.Cache.Remove("items");
                }
                catch
                {
                    throw new HttpException(400, "File failed to save.");
                }
            }
            else
            {
                throw new HttpException(400, "No file to upload.");
            }
        }
        else if (mode == "delete")
        {
            var fileName = context.Request.Form["fileName"];
            var path = context.Server.MapPath("~/files");
            var fullPath = Path.Combine(path, fileName);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                Site.ClearStartPageCache();
                HttpRuntime.Cache.Remove("items");
            }
            else
            {
                throw new HttpException(400, "Nothing to delete.");
            }

        }
        else if (mode == "save")
        {
            try
            {
                var fileName = context.Request.Form["fileName"];
                var data = context.Request.Form["lines"];
                var lines = data.Split('\n').ToList();
                PostManager.Save(fileName, lines);
                Site.ClearStartPageCache();
                HttpRuntime.Cache.Remove("items");
            }
            catch
            {
                throw new HttpException(400, "File failed to save.");
            }
        }
        else if (mode == "create")
        {
            var fileName = context.Request.Form["fileName"];
            if (!fileName.ToLower().EndsWith(".md"))
            {
                fileName += ".md";
            }
            var path = context.Server.MapPath("~/files");
            var fullPath = Path.Combine(path, fileName);

            if (File.Exists(fullPath))
            {
                throw new HttpException(409, "File already exists.");
            }
            else
            {
                var lines = new List<string>();
                lines.Add("Title:\t\t\tNew Title");
                lines.Add("Type:\t\t\tPost");
                lines.Add("Author:\t\tAuthor");
                lines.Add("PubDate:\t\t" + DateTime.Now.ToString("yyyy-MM-dd hh:mm tt"));
                lines.Add("Categories:\t\t");
                lines.Add("Tags:\t\t\t");
                lines.Add("Image:\t\t\t");
                lines.Add("Link:\t\t\tnone");
                lines.Add("Slug:\t\t\tnew-item-" + DateTime.Now.ToString("yyyyMMddhhmmtt").ToLower());
                lines.Add("Status:\t\t\tDraft");
                lines.Add("Excerpt:\t\t");
                lines.Add("");
                lines.Add("Contents of post goes here.");
                PostManager.Save(fileName, lines);
                Site.ClearStartPageCache();
                HttpRuntime.Cache.Remove("items");
            }
        }
    }


    private void EditPost(string Slug, string title, string excerpt, string content, DateTime pubDate, bool isPublished, string[] categories)
    {
        Post post;

        post = PostManager.GetAllPosts().FirstOrDefault(p => p.Slug == Slug);

        if (post != null)
        {
            post.Title = title;
            post.Description = excerpt;
            post.Content = content;
            post.PubDate = pubDate;
            post.Categories = categories.ToList();
            //post.IsPublished = isPublished;
        }
        else
        {
            post = new Post() { Title = title, Description = excerpt, Content = content, PubDate = pubDate, Slug = CreateSlug(title), Categories = categories.ToList() };
            HttpContext.Current.Response.Write(post.Url);
        }

        SaveFilesToDisk(post);

        //post.IsPublished = isPublished;
        //PostManager.Save(post);
    }

    private void SaveFilesToDisk(Post post)
    {
        //foreach (Match match in Regex.Matches(post.Content, "(src|href)=\"(data:([^\"]+))\""))
        //{
        //    string extension = Regex.Match(match.Value, "data:([^/]+)/([a-z]+);base64").Groups[2].Value;

        //    byte[] bytes = ConvertToBytes(match.Groups[2].Value);
        //    //string path = Site.SaveFileToDisk(bytes, extension);

        //    string value = string.Format("src=\"{0}\" alt=\"\" ", path);

        //    if (match.Groups[1].Value == "href")
        //        value = string.Format("href=\"{0}\"", path);

        //    post.Content = post.Content.Replace(match.Value, value);
        //}
    }

    private byte[] ConvertToBytes(string base64)
    {
        int index = base64.IndexOf("base64,", StringComparison.Ordinal) + 7;
        return Convert.FromBase64String(base64.Substring(index));
    }

    public static string CreateSlug(string title)
    {
        title = title.ToLowerInvariant().Replace(" ", "-").Replace("#", "");
        title = RemoveDiacritics(title);

        if (PostManager.GetAllPosts().Any(p => string.Equals(p.Slug, title, StringComparison.OrdinalIgnoreCase)))
            throw new HttpException(409, "Already in use");

        return title.ToLowerInvariant();
    }

    static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    public bool IsReusable
    {
        get { return false; }
    }
}