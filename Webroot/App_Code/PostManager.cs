using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;

public static class PostManager
{
    private static string _folder = HostingEnvironment.MapPath("~/files/");
    private static string _mediafolder = HostingEnvironment.MapPath("~/files/media/");

    public static List<Post> GetAllPosts()
    {
        if (HttpRuntime.Cache["items"] == null)
            LoadPosts();

        if (HttpRuntime.Cache["items"] != null)
        {
            return (List<Post>)HttpRuntime.Cache["items"];
        }
        return new List<Post>();
    }

    public static List<string> GetMediaFiles()
    {
        var list = new List<string>();

        if (!Directory.Exists(_folder))
            Directory.CreateDirectory(_folder);
        if (!Directory.Exists(_mediafolder))
            Directory.CreateDirectory(_mediafolder);

        foreach (string file in Directory.EnumerateFiles(_mediafolder, "*.*", SearchOption.TopDirectoryOnly))
        {
            var shortName = file.Substring(_mediafolder.Length);
            list.Add(shortName);
        }

        return list;
    }

    public static List<string> GetRawText(string fileName)
    {
        var lines = new List<string>();

        // Lookup file
        var fullpath = fileName;
        if (fullpath.StartsWith(_folder))
            fileName = fileName.Replace(_folder, "");
        else
            fullpath = _folder + fileName;

        if (File.Exists(fullpath))
        {
            using (StreamReader sr = new StreamReader(fullpath))
            {
                string line;
                StringBuilder markdown = new StringBuilder();
                while ((line = sr.ReadLine()) != null)
                {
                    lines.Add(line + "\n");
                }
            }
        }
        return lines;
    }

    public static void Save(string fileName, List<string> lines)
    {
        // Lookup file
        var fullpath = fileName;
        if (fullpath.StartsWith(_folder))
            fileName = fileName.Replace(_folder, "");
        else
            fullpath = _folder + fileName;

        if (File.Exists(fullpath))
        {
            // Re-write file
            File.Delete(fullpath);
        }
        using (StreamWriter sw = new StreamWriter(fullpath))
        {
            foreach (var line in lines)
            {
                var clean = line.Replace("\r", "");
                sw.WriteLine(clean);
            }
        }
        
    }

    public static void Delete(Post post)
    {
        Site.ClearStartPageCache();
    }

    private static void LoadPosts()
    {
        if (!Directory.Exists(_folder))
            Directory.CreateDirectory(_folder);

        List<Post> items = new List<Post>();

        // Can this be done in parallel to speed it up?
        foreach (string file in Directory.EnumerateFiles(_folder, "*.md", SearchOption.TopDirectoryOnly))
        {
            var item = GetPost(file);
            if (item != null)
                items.Add(item);
        }

        if (items.Count > 0)
        {
            items.Sort((p1, p2) => p2.PubDate.CompareTo(p1.PubDate));
            HttpRuntime.Cache.Insert("items", items);
        }

    }

    private static Post GetPost(string filename)
    {
        Post post = null;

        // Lookup file
        var fullpath = filename;
        if (fullpath.StartsWith(_folder))
            filename = filename.Replace(_folder, "");
        else
            fullpath = _folder + filename + ".md";

        if (File.Exists(fullpath))
        {
            post = new Post();

            post.FileName = filename;
            post.LastModified = File.GetLastWriteTime(fullpath);

            using (StreamReader sr = new StreamReader(fullpath))
            {
                // Get title and Settings First
                bool gotHeader = false;
                string line;
                StringBuilder markdown = new StringBuilder();
                while ((line = sr.ReadLine()) != null)
                {
                    if (gotHeader)
                        markdown.AppendLine(line);
                    else
                    {
                        if (line.Trim() == string.Empty)
                            gotHeader = true;
                        else
                            GetMetaData(post, line);
                    }
                }

                post.Content = markdown.ToString();
            }

            // Validate
            if (string.IsNullOrEmpty(post.Title))
            {
                post = null;
            }
            else if (string.IsNullOrEmpty(post.Slug))
            {
                post.Slug = GenerateSlug(post.Title);
            }
            //TODO: Duplicate check??

        }
        return post;
    }

    private static void GetMetaData(Post post, string line)
    {
        // Attempt to get settings
        if (line.ToLower().StartsWith("title:"))
        {
            post.Title = ExtractMeta(line);
        }
        if (line.ToLower().StartsWith("type:"))
        {
            var type = ExtractMeta(line).ToLower();
            if (type == "page")
                post.Type = PostType.Page;
            else
                post.Type = PostType.Post;
        }
        if (line.ToLower().StartsWith("author:"))
        {
            post.Author = ExtractMeta(line);
        }
        if (line.ToLower().StartsWith("pubdate:"))
        {
            DateTime dateActual;
            var dateString = ExtractMeta(line);
            if (DateTime.TryParse(dateString, out dateActual))
                post.PubDate = dateActual;
        }
        if (line.ToLower().StartsWith("category:") || line.ToLower().StartsWith("categories:"))
        {
            var catLine = ExtractMeta(line);
            var catArray = catLine.Split(',');
            for (int i = 0; i < catArray.Length; i++)
            {
                post.Categories.Add(catArray[i].Trim());
            }
        }
        if (line.ToLower().StartsWith("tag:") || line.ToLower().StartsWith("tags:"))
        {
            var tagLine = ExtractMeta(line).ToLower();
            var tagArray = tagLine.Split(',');
            for (int i = 0; i < tagArray.Length; i++)
            {
                post.Tags.Add(tagArray[i].Trim());
            }
        }
        if (line.ToLower().StartsWith("image:"))
        {
            post.Image = ExtractMeta(line);
        }
        if (line.ToLower().StartsWith("link:"))
        {
            post.Link = ExtractMeta(line);
        }
        if (line.ToLower().StartsWith("slug:"))
        {
            post.Slug = ExtractMeta(line);
        }
        if (line.ToLower().StartsWith("excerpt:"))
        {
            post.Excerpt = ExtractMeta(line);
        }
        if (line.ToLower().StartsWith("description:"))
        {
            post.Description = ExtractMeta(line);
        }
        if (line.ToLower().StartsWith("status:"))
        {
            var status = ExtractMeta(line).ToLower();
            if (status == "published")
                post.Status = PostStatus.Published;
            else
                post.Status = PostStatus.Draft;
        }

    }

    private static string ExtractMeta(string line)
    {
        return line.Substring(line.IndexOf(':') + 1).Trim();
    }

    public static string GenerateSlug(string phrase)
    {
        string str = RemoveAccent(phrase).ToLower();
        // invalid chars           
        str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
        // convert multiple spaces into one space   
        str = Regex.Replace(str, @"\s+", " ").Trim();
        // cut and trim 
        str = str.Substring(0, str.Length <= 45 ? str.Length : 45).Trim();
        str = Regex.Replace(str, @"\s", "-"); // hyphens   
        return str;
    }

    private static string RemoveAccent(string txt)
    {
        byte[] bytes = System.Text.Encoding.GetEncoding("Cyrillic").GetBytes(txt);
        return System.Text.Encoding.ASCII.GetString(bytes);
    }

}