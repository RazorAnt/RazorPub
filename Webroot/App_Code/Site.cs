using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Caching;
using System.Web.Helpers;
using System.Web.Hosting;

public static class Site
{
    public static string Title { get; private set; }
    public static string Description { get; private set; }
    public static string Theme { get; private set; }
    public static string Image { get; private set; }
    public static int PostsPerPage { get; private set; }
    public static int DaysToComment { get; private set; }
    public static bool ModerateComments { get; private set; }
    public static string BlogPath { get; private set; }

	static Site()
	{
        Theme = ConfigurationManager.AppSettings.Get("blog:theme");
        Title = ConfigurationManager.AppSettings.Get("blog:name");
        Description = ConfigurationManager.AppSettings.Get("blog:description");
        PostsPerPage = int.Parse(ConfigurationManager.AppSettings.Get("blog:postsPerPage"));
        DaysToComment = int.Parse(ConfigurationManager.AppSettings.Get("blog:daysToComment"));
        Image = ConfigurationManager.AppSettings.Get("blog:image");
        ModerateComments = bool.Parse(ConfigurationManager.AppSettings.Get("blog:moderateComments"));
        BlogPath = ConfigurationManager.AppSettings.Get("blog:path");
	}

    public static string Engine
    {
        get { return "RazorPub"; }
    }

    public static string HandlerPath
    {
        get
        {
            var value = string.Empty;
            if (HttpContext.Current.User.Identity.IsAuthenticated)
            {
                value = "/";
                if (!string.IsNullOrEmpty(BlogPath))
                {
                    value += BlogPath + "/";
                }
                value += "post.ashx";
            }
            return value;
        }
    }

    public static string CurrentSlug
    {
        get { return (HttpContext.Current.Request.QueryString["slug"] ?? string.Empty).Trim().ToLowerInvariant(); }
    }

    public static string CurrentCategory
    {
        get { return (HttpContext.Current.Request.QueryString["category"] ?? string.Empty).Trim().ToLowerInvariant(); }
    }

    public static string CurrentTag
    {
        get { return (HttpContext.Current.Request.QueryString["tag"] ?? string.Empty).Trim().ToLowerInvariant(); }
    }

    public static string CurrentAuthor
    {
        get { return (HttpContext.Current.Request.QueryString["author"] ?? string.Empty).Trim().ToLowerInvariant(); }
    }

    public static bool IsNewPost
    {
        get
        {
            return HttpContext.Current.Request.RawUrl.Trim('/') == (!string.IsNullOrWhiteSpace(BlogPath) ? BlogPath + "/" : "") + "edit/new";
        }
    }

    public static bool IsPost
    {
        get
        {
            return (HttpContext.Current.Request.RawUrl.ToLower().Contains("/post/"));
        }
    }

    public static bool IsEditing
    {
        get
        {
            return HttpContext.Current.Request.QueryString["mode"] == "edit";
        }
    }

    public static Post CurrentPost
    {
        get
        {
            if (HttpContext.Current.Items["currentpost"] == null && !string.IsNullOrEmpty(CurrentSlug))
            {
                var post = PostManager.GetAllPosts().FirstOrDefault(p => p.Slug == CurrentSlug);

                if (post != null && (post.Status == PostStatus.Published || HttpContext.Current.User.Identity.IsAuthenticated))
                    HttpContext.Current.Items["currentpost"] = PostManager.GetAllPosts().FirstOrDefault(p => p.Slug == CurrentSlug);
            }

            return HttpContext.Current.Items["currentpost"] as Post;
        }
    }

    public static string GetNextPage()
    {
        if (!string.IsNullOrEmpty(CurrentSlug))
        {
            var current = Site.GetPosts().ToList().IndexOf(CurrentPost);
            if (current > 0)
                return Site.GetPosts().ToList()[current - 1].Url.ToString();
        }
        else if (CurrentPage > 1)
        {
            return GetPagingUrl(-1);
        }

        return null;
    }

    public static string GetPrevPage()
    {
        if (!string.IsNullOrEmpty(CurrentSlug))
        {
            var current = Site.GetPosts().ToList().IndexOf(CurrentPost);
            if (current > -1 && Site.GetPosts().ToList().Count > current + 1)
                return Site.GetPosts().ToList()[current + 1].Url.ToString();
        }
        else
        {
            return GetPagingUrl(1);
        }

        return null;
    }

    public static int CurrentPage
    {
        get
        {
            int page = 0;
            if (int.TryParse(HttpContext.Current.Request.QueryString["page"], out page))
                return page;

            return 1;
        }
    }

    public static IEnumerable<Post> GetPosts(int postsPerPage = 0)
    {
        var posts = from p in PostManager.GetAllPosts()
                    where ((p.Status == PostStatus.Published && p.PubDate <= DateTime.Now) || HttpContext.Current.User.Identity.IsAuthenticated)
                        && (p.Type == PostType.Post)
                    select p;

        string category = HttpContext.Current.Request.QueryString["category"];
        if (!string.IsNullOrEmpty(category))
        {
            posts = posts.Where(p => p.Categories.Any(c => string.Equals(c, category, StringComparison.OrdinalIgnoreCase)));
        }

        string tag = HttpContext.Current.Request.QueryString["tag"];
        if (!string.IsNullOrEmpty(tag))
        {
            posts = posts.Where(p => p.Tags.Any(c => string.Equals(c, tag, StringComparison.OrdinalIgnoreCase)));
        }

        string author = HttpContext.Current.Request.QueryString["author"];
        if (!string.IsNullOrEmpty(author))
        {
            posts = posts.Where(p => p.Author.ToLower() == author);
        }

        if (postsPerPage > 0)
        {
            posts = posts.Skip(postsPerPage * (CurrentPage - 1)).Take(postsPerPage);
        }

        return posts;
    }

    public static bool IsTopPost(Post post)
    {
        var posts = Site.GetPosts(Site.PostsPerPage);

        if (post.Url == posts.First().Url)
            return true;

        return false;
    }

    public static void ValidateToken(HttpContext context)
    {
        AntiForgery.Validate();
    }

    public static string SaveFileToDisk(byte[] bytes, string filename, string extension)
    {
        string relative = "~/files/";
        if (!extension.Contains("md"))
            relative += "media/";

        if (string.IsNullOrWhiteSpace(extension))
            extension = ".bin";
        else
            extension = "." + extension.Trim('.');

        relative += filename + extension;

        string file = HostingEnvironment.MapPath(relative);

        File.WriteAllBytes(file, bytes);

        if (extension == ".png" || extension == ".jpg")
        {
            var cruncher = new ImageCruncher.Cruncher();
            cruncher.CrunchImages(file);
        }

        return VirtualPathUtility.ToAbsolute(relative);
    }

    public static string GetPagingUrl(int move)
    {
        string url = "/page/{0}/";

        string category = HttpContext.Current.Request.QueryString["category"];
        if (!string.IsNullOrEmpty(category))
        {
            url = "/category/" + HttpUtility.UrlEncode(category.ToLowerInvariant()) + "/" + url;
        }

        string tag = HttpContext.Current.Request.QueryString["tag"];
        if (!string.IsNullOrEmpty(tag))
        {
            url = "/tag/" + HttpUtility.UrlEncode(tag.ToLowerInvariant()) + "/" + url;
        }

        string author = HttpContext.Current.Request.QueryString["author"];
        if (!string.IsNullOrEmpty(author))
        {
            url = "/author/" + HttpUtility.UrlEncode(author.ToLowerInvariant()) + "/" + url;
        }

        string relative = string.Format("~" + url, Site.CurrentPage + move);
        return VirtualPathUtility.ToAbsolute(relative);
    }

    public static string FingerPrint(string rootRelativePath, string cdnPath = "")
    {
        if (HttpContext.Current.Request.IsLocal)
            return rootRelativePath;

        if (!string.IsNullOrEmpty(cdnPath) && !HttpContext.Current.IsDebuggingEnabled)
            return cdnPath;

        if (HttpRuntime.Cache[rootRelativePath] == null)
        {
            string relative = VirtualPathUtility.ToAbsolute("~" + rootRelativePath);
            string absolute = HostingEnvironment.MapPath(relative);

            if (!File.Exists(absolute))
                throw new FileNotFoundException("File not found: " + absolute, absolute);

            DateTime date = File.GetLastWriteTime(absolute);
            int index = relative.LastIndexOf('.');

            string result = ConfigurationManager.AppSettings.Get("blog:cdnUrl") + relative.Insert(index, "_" + date.Ticks);

            HttpRuntime.Cache.Insert(rootRelativePath, result, new CacheDependency(absolute));
        }

        return HttpRuntime.Cache[rootRelativePath] as string;
    }

    public static void SetConditionalGetHeaders(DateTime lastModified, HttpContextBase context)
    {
        HttpResponseBase response = context.Response;
        HttpRequestBase request = context.Request;
        lastModified = new DateTime(lastModified.Year, lastModified.Month, lastModified.Day, lastModified.Hour, lastModified.Minute, lastModified.Second);

        string incomingDate = request.Headers["If-Modified-Since"];

        response.Cache.SetLastModified(lastModified);

        DateTime testDate = DateTime.MinValue;

        if (DateTime.TryParse(incomingDate, out testDate) && testDate == lastModified)
        {
            response.ClearContent();
            response.StatusCode = (int)System.Net.HttpStatusCode.NotModified;
            response.SuppressContent = true;
        }
    }

    public static Dictionary<string, int> GetCategories()
    {
        var categoryStrings = PostManager.GetAllPosts()
            .Where(p => ((p.Status == PostStatus.Published && p.PubDate <= DateTime.UtcNow) || HttpContext.Current.User.Identity.IsAuthenticated))
            .SelectMany(x => x.Categories).ToList().Distinct();
        var result = new Dictionary<string, int>();
        foreach (var cat in categoryStrings)
        {
            result.Add(cat,
                PostManager.GetAllPosts()
                .Where(p => ((p.Status == PostStatus.Published && p.PubDate <= DateTime.UtcNow) || HttpContext.Current.User.Identity.IsAuthenticated))
                .Count(p => p.Categories.Any(c => string.Equals(c, cat, StringComparison.OrdinalIgnoreCase)))
            );
        }

        return result;
    }

    public static List<string> GetTags()
    {
        var tags = PostManager.GetAllPosts()
            .Where(p => ((p.Status == PostStatus.Published && p.PubDate <= DateTime.UtcNow) || HttpContext.Current.User.Identity.IsAuthenticated))
            .SelectMany(x => x.Tags).ToList().Distinct();
        return tags.ToList();
    }

    public static void ClearStartPageCache()
    {
        HttpResponse.RemoveOutputCacheItem(string.Format("/{0}", BlogPath));
    }

}