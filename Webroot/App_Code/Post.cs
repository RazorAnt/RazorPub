using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using CommonMark;

public class Post
{
    public Post()
    {
        Type = PostType.Post;
        Status = PostStatus.Draft;
        Tags = new List<string>();
        Categories = new List<string>();
    }

    public string Title { get; set; }
    public PostType Type { get; set; }
    public string Author { get; set; }
    public DateTime PubDate { get; set; }
    public List<string> Categories { get; set; }
    public List<string> Tags { get; set; }
    public string Image { get; set; }
    public string Link { get; set; }
    public string Slug { get; set; }
    public string Excerpt { get; set; }
    public string Description { get; set; }
    public PostStatus Status { get; set; }
    public string Content { get; set; } // In Markdown
    
    public string FileName { get; set; } // Retrieved from File
    public DateTime LastModified { get; set; } // Retrieved from File

    public Uri AbsoluteUrl
    {
        get
        {
            Uri requestUrl = HttpContext.Current.Request.Url;
            return new Uri(requestUrl.Scheme + "://" + requestUrl.Authority + Url, UriKind.Absolute);
        }
    }

    public Uri Url
    {
        get
        {
            var pathType = "~/post/";
            return new Uri(VirtualPathUtility.ToAbsolute(pathType + Slug), UriKind.Relative);
        }
    }

    public string AutoExcerpt()
    {
        if (!string.IsNullOrWhiteSpace(Excerpt))
            return Excerpt;
        if (string.IsNullOrWhiteSpace(Content))
            return string.Empty;

        var RegexStripHtml = new Regex("<[^>]*>", RegexOptions.Compiled);
        var clean = RegexStripHtml.Replace(GetHtmlContent(), string.Empty).Trim();
        clean = clean.Replace("&#160;", "");
        clean = clean.Replace("&nbsp;", "");
        clean = clean.Replace("&rsquo;", "'");

        if (clean.Length > 150)
            clean = clean.Substring(0, 150) + "...";

        return clean;
    }

    public string GetHtmlContent()
    {
        var postContent = Content;

        // Vimeo helper [viemo:123456789]
        var vimeo = "<div class=\"video\"><iframe src=\"https://player.vimeo.com/video/{0}\" frameborder=\"0\" webkitallowfullscreen allowfullscreen></iframe></div>";
        postContent = Regex.Replace(postContent, @"\[vimeo:(.*?)\]", (Match m) => string.Format(vimeo, m.Groups[1].Value));

        // YouTube helper [youtube:xyzAbc123]
        var youtube = "<div class=\"video\"><iframe src=\"//www.youtube.com/embed/{0}?modestbranding=1&amp;theme=light\" allowfullscreen></iframe></div>";
        postContent = Regex.Replace(postContent, @"\[youtube:(.*?)\]", (Match m) => string.Format(youtube, m.Groups[1].Value));

        // Lightbox helper [lightbox src=""]
        var lightbox = "<a href=\"{0}\" class=\"lightbox\" rel=\"lightbox\"><img src=\"{0}\" alt=\"\" /></a>";
        postContent = Regex.Replace(postContent, @"\[lightbox src=""(.*?)\""]", (Match m) => string.Format(lightbox, m.Groups[1].Value));

        // LightboxLeft helper [lightboxleft src=""]
        var lightboxleft = "<a href=\"{0}\" class=\"lightbox\" style=\"float: left;\" rel=\"lightbox\"><img src=\"{0}\" alt=\"\" /></a>";
        postContent = Regex.Replace(postContent, @"\[lightboxleft src=""(.*?)\""]", (Match m) => string.Format(lightboxleft, m.Groups[1].Value));

        // LightboxRightt helper [lightboxleft src=""]
        var lightboxright = "<a href=\"{0}\" class=\"lightbox\" style=\"float: right;\" rel=\"lightbox\"><img src=\"{0}\" alt=\"\" /></a>";
        postContent = Regex.Replace(postContent, @"\[lightboxright src=""(.*?)\""]", (Match m) => string.Format(lightboxright, m.Groups[1].Value));

        // LeftRightClear helper [leftrightclear]
        var lrclear = "<div style=\"clear: both;\"></div>";
        postContent = Regex.Replace(postContent, @"\[leftrightclear]", (Match m) => string.Format(lrclear, m.Groups[1].Value));

        // LightboxMax helper [lightboxmax src=""]
        var lightboxmax = "<a href=\"{0}\" class=\"lightbox\" rel=\"lightbox\"><img class=\"fullimage\" src=\"{0}\" alt=\"\" /></a>";
        postContent = Regex.Replace(postContent, @"\[lightboxmax src=""(.*?)\""]", (Match m) => string.Format(lightboxmax, m.Groups[1].Value));

        // Lightbox Col1 (Gallery) helper [lightbox1 src=""]
        var lightbox1 = "<a href=\"{0}\" rel=\"lightbox\" data-fluidbox class=\"col-1\"><img src=\"{0}\" alt=\"\" /></a>";
        postContent = Regex.Replace(postContent, @"\[lightbox1 src=""(.*?)\""]", (Match m) => string.Format(lightbox1, m.Groups[1].Value));
        // Lightbox Col2 (Gallery) helper [lightbox2 src=""]
        var lightbox2 = "<a href=\"{0}\" rel=\"lightbox\" data-fluidbox class=\"col-2\"><img src=\"{0}\" alt=\"\" /></a>";
        postContent = Regex.Replace(postContent, @"\[lightbox2 src=""(.*?)\""]", (Match m) => string.Format(lightbox2, m.Groups[1].Value));
        // Lightbox Col3 (Gallery) helper [lightbox3 src=""]
        var lightbox3 = "<a href=\"{0}\" rel=\"lightbox\" data-fluidbox class=\"col-3\"><img src=\"{0}\" alt=\"\" /></a>";
        postContent = Regex.Replace(postContent, @"\[lightbox3 src=""(.*?)\""]", (Match m) => string.Format(lightbox3, m.Groups[1].Value));

        // Gallery helper [gallery] and [/gallery]
        var gallerystart = "<div class=\"gallery\">";
        var galleryend = "</div>";
        postContent = Regex.Replace(postContent, @"\[gallery]", (Match m) => string.Format(gallerystart, m.Groups[1].Value));
        postContent = Regex.Replace(postContent, @"\[/gallery]", (Match m) => string.Format(galleryend, m.Groups[1].Value));

        // Add in PostImage is it exists
        if (!string.IsNullOrEmpty(Image))
        {
            postContent = "<a href=\"" + Image + "\" class=\"lightbox\" rel=\"lightbox\"><img class=\"fullimage\" src=\"" + Image + "\" alt=\"\"></a>\n\n" + postContent;
        }

        string result = CommonMarkConverter.Convert(postContent);

        // Images replaced by CDN paths if they are located in the /posts/ folder
        //var cdn = ConfigurationManager.AppSettings.Get("blog:cdnUrl");
        //var root = ConfigurationManager.AppSettings.Get("blog:path") + "/posts/";

        //if (!root.StartsWith("/"))
        //    root = "/" + root;

        //result = Regex.Replace(result, "<img.*?src=\"([^\"]+)\"", (Match m) =>
        //{
        //    string src = m.Groups[1].Value;
        //    int index = src.IndexOf(root);

        //    if (index > -1)
        //    {
        //        string clean = src.Substring(index);
        //        return m.Value.Replace(src, cdn + clean);
        //    }

        //    return m.Value;
        //});

        return result;
    }



}