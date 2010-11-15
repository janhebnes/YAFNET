/* Yet Another Forum.net
 * Copyright (C) 2006-2010 Jaben Cargman
 * http://www.yetanotherforum.net/
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 */
namespace YAF.Classes
{
  #region Using

  using System;
  using System.Data;
  using System.Globalization;
  using System.Text;
  using System.Web;
  using System.Web.Caching;

  using YAF.Classes.Data;
  using YAF.Classes.Utils;

  #endregion

  /// <summary>
  /// The rewrite url builder.
  /// </summary>
  public class RewriteUrlBuilder : BaseUrlBuilder
  {
    #region Constants and Fields

    /// <summary>
    /// The cache size.
    /// </summary>
    private int _cacheSize = 500;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets CacheSize.
    /// </summary>
    protected int CacheSize
    {
      get
      {
        return this._cacheSize;
      }

      set
      {
        if (this._cacheSize > 0)
        {
          this._cacheSize = value;
        }
      }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// The build url.
    /// </summary>
    /// <param name="url">
    /// The url.
    /// </param>
    /// <returns>
    /// The build url.
    /// </returns>
    public override string BuildUrl(string url)
    {
      string newUrl = "{0}{1}?{2}".FormatWith(AppPath, Config.ForceScriptName ?? ScriptName, url);

      // create scriptName
      string scriptName = "{0}{1}".FormatWith(AppPath, Config.ForceScriptName ?? ScriptName);

      // get the base script file from the config -- defaults to, well, default.aspx :)
      string scriptFile = Config.BaseScriptFile;

      if (scriptName.EndsWith(scriptFile))
      {
        string before = scriptName.Remove(scriptName.LastIndexOf(scriptFile));

        var parser = new SimpleURLParameterParser(url);

        // create "rewritten" url...
        newUrl = before + Config.UrlRewritingPrefix;

        string useKey = string.Empty;
        string description = string.Empty;
        string pageName = parser["g"];
        const bool showKey = false;
        bool handlePage = false;

        switch (parser["g"])
        {
          case "topics":
            useKey = "f";
            description = this.GetForumName(Convert.ToInt32(parser[useKey]));
            handlePage = true;
            break;
          case "posts":
            if (parser["t"].IsSet())
            {
              useKey = "t";
              pageName += "t";
              description = this.GetTopicName(Convert.ToInt32(parser[useKey]));
            }
            else if (parser["m"].IsSet())
            {
              useKey = "m";
              pageName += "m";

              try
              {
                  description = this.GetTopicNameFromMessage(Convert.ToInt32(parser[useKey]));
              }
              catch (Exception)
              {
                  description = "posts";
              }
            
            }

            handlePage = true;
            break;
          case "profile":
            useKey = "u";

            // description = GetProfileName( Convert.ToInt32( parser [useKey] ) );
            break;
          case "forum":
            if (parser["c"].IsSet())
            {
              useKey = "c";
              description = this.GetCategoryName(Convert.ToInt32(parser[useKey]));
            }

            break;
        }

        newUrl += pageName;

        if (useKey.Length > 0)
        {
            newUrl += parser[useKey];
        }

        if (handlePage && parser["p"] != null)
        {
          int page = Convert.ToInt32(parser["p"]);
          if (page != 1)
          {
            newUrl += string.Format("p{0}", page);
          }

          parser.Parameters.Remove("p");
        }

        if (description.Length > 0)
        {
            if (description.EndsWith("-"))
            {
                description = description.Remove(description.Length - 1, 1);
            }

          newUrl += string.Format("_{0}", description);
        }

        newUrl += ".aspx";

        string restURL = parser.CreateQueryString(new[] { "g", useKey });

        // append to the url if there are additional (unsupported) parameters
        if (restURL.Length > 0)
        {
          newUrl += string.Format("?{0}", restURL);
        }

        // see if we can just use the default (/)
        if (newUrl.EndsWith("yaf_forum.aspx"))
        {
          // remove in favor of just slash...
          newUrl = newUrl.Remove(newUrl.LastIndexOf("yaf_forum.aspx"), "yaf_forum.aspx".Length);
        }

        // add anchor
        if (parser.HasAnchor)
        {
          newUrl += string.Format("#{0}", parser.Anchor);
        }
      }

      // just make sure & is &amp; ...
      newUrl = newUrl.Replace("&amp;", "&").Replace("&", "&amp;");

      return newUrl;
    }

    #endregion

    #region Methods

    /// <summary>
    /// The high range.
    /// </summary>
    /// <param name="id">
    /// The id.
    /// </param>
    /// <returns>
    /// The high range.
    /// </returns>
    protected int HighRange(int id)
    {
      return (int)(Math.Ceiling((double)(id / this._cacheSize)) * this._cacheSize);
    }

    /// <summary>
    /// The low range.
    /// </summary>
    /// <param name="id">
    /// The id.
    /// </param>
    /// <returns>
    /// The low range.
    /// </returns>
    protected int LowRange(int id)
    {
      return (int)(Math.Floor((double)(id / this._cacheSize)) * this._cacheSize);
    }

    /// <summary>
    /// The clean string for url.
    /// </summary>
    /// <param name="str">
    /// The str.
    /// </param>
    /// <returns>
    /// The clean string for url.
    /// </returns>
    protected static string CleanStringForURL(string str)
    {
      var sb = new StringBuilder();

      // trim...
     
      if (Config.UrlRewritingMode == "Unicode")
      {
          str = HttpUtility.UrlDecode(str.Trim());
      }
      else
      {
          str = HttpContext.Current.Server.HtmlDecode(str.Trim());
      }

        // fix ampersand...
      str = str.Replace("&", "and");

      // normalize the Unicode
      str = str.Normalize(NormalizationForm.FormD);
      if (Config.UrlRewritingMode == "Unicode")
      {
          foreach (char currentChar in str)
          {
              if (char.IsWhiteSpace(currentChar) || char.IsPunctuation(currentChar))
              {
                  sb.Append('-');
              }
              else if (char.GetUnicodeCategory(currentChar) != UnicodeCategory.NonSpacingMark &&
                             !char.IsSymbol(currentChar))
              {
                  sb.Append(currentChar);
              }
          }
          string strNew = sb.ToString();

          if (strNew.EndsWith("-"))
          {
              strNew = strNew.Remove(strNew.Length - 1, 1);
          }

          return HttpUtility.UrlEncode(strNew.ToLowerInvariant());
      }
      else 
      {
          if (Config.UrlRewritingMode == "Translit")
          {
              return str.Unidecode().Replace(" ","-");
          }
          else
          {
              foreach (char currentChar in str)
              {
                  if (char.IsWhiteSpace(currentChar) || currentChar == '.')
                  {
                      sb.Append('-');
                  }
                  else if (char.GetUnicodeCategory(currentChar) != UnicodeCategory.NonSpacingMark && !char.IsPunctuation(currentChar) &&
                           !char.IsSymbol(currentChar) && currentChar < 128)
                  {
                      sb.Append(currentChar);
                  }
              }
              return sb.ToString();
              
          }
      }

     
    }

    /// <summary>
    /// The get cache name.
    /// </summary>
    /// <param name="type">
    /// The type.
    /// </param>
    /// <param name="id">
    /// The id.
    /// </param>
    /// <returns>
    /// The get cache name.
    /// </returns>
    protected string GetCacheName(string type, int id)
    {
      return @"urlRewritingDT-{0}-Range-{1}-to-{2}".FormatWith(type, this.HighRange(id), this.LowRange(id));
    }

    /// <summary>
    /// The get category name.
    /// </summary>
    /// <param name="id">
    /// The id.
    /// </param>
    /// <returns>
    /// The get category name.
    /// </returns>
    protected string GetCategoryName(int id)
    {
      const string type = "Category";
      const string primaryKey = "CategoryID";
      const string nameField = "Name";

      DataRow row = this.GetDataRowFromCache(type, id);

      if (row == null)
      {
        // get the section desired...
        DataTable list = DB.category_simplelist(this.LowRange(id), this.CacheSize);

        // set it up in the cache
        row = this.SetupDataToCache(ref list, type, id, primaryKey);

        if (row == null)
        {
          return string.Empty;
        }
      }

      return CleanStringForURL(row[nameField].ToString());
    }

    /// <summary>
    /// The get data row from cache.
    /// </summary>
    /// <param name="type">
    /// The type.
    /// </param>
    /// <param name="id">
    /// The id.
    /// </param>
    /// <returns>
    /// </returns>
    protected DataRow GetDataRowFromCache(string type, int id)
    {
      // get the datatable and find the value
      var list = HttpContext.Current.Cache[this.GetCacheName(type, id)] as DataTable;

      if (list != null)
      {
        DataRow row = list.Rows.Find(id);

        // valid, return...
        if (row != null)
        {
          return row;
        }

          // invalidate this cache section
          HttpContext.Current.Cache.Remove(this.GetCacheName(type, id));
      }

      return null;
    }

    /// <summary>
    /// The get forum name.
    /// </summary>
    /// <param name="id">
    /// The id.
    /// </param>
    /// <returns>
    /// The get forum name.
    /// </returns>
    protected string GetForumName(int id)
    {
      const string type = "Forum";
      const string primaryKey = "ForumID";
      const string nameField = "Name";

      DataRow row = this.GetDataRowFromCache(type, id);

      if (row == null)
      {
        // get the section desired...
        DataTable list = DB.forum_simplelist(this.LowRange(id), this.CacheSize);

        // set it up in the cache
        row = this.SetupDataToCache(ref list, type, id, primaryKey);

        if (row == null)
        {
          return string.Empty;
        }
      }

      return CleanStringForURL(row[nameField].ToString());
    }

    /// <summary>
    /// The get profile name.
    /// </summary>
    /// <param name="id">
    /// The id.
    /// </param>
    /// <returns>
    /// The get profile name.
    /// </returns>
    protected string GetProfileName(int id)
    {
      const string type = "Profile";
      const string primaryKey = "UserID";
      const string nameField = "Name";

      DataRow row = this.GetDataRowFromCache(type, id);

      if (row == null)
      {
        // get the section desired...
        DataTable list = DB.user_simplelist(this.LowRange(id), this.CacheSize);

        // set it up in the cache
        row = this.SetupDataToCache(ref list, type, id, primaryKey);

        if (row == null)
        {
          return string.Empty;
        }
      }

      return CleanStringForURL(row[nameField].ToString());
    }

    /// <summary>
    /// The get topic name.
    /// </summary>
    /// <param name="id">
    /// The id.
    /// </param>
    /// <returns>
    /// The get topic name.
    /// </returns>
    protected string GetTopicName(int id)
    {
      const string type = "Topic";
      const string primaryKey = "TopicID";
      const string nameField = "Topic";

      DataRow row = this.GetDataRowFromCache(type, id);

      if (row == null)
      {
        // get the section desired...
        DataTable list = DB.topic_simplelist(this.LowRange(id), this.CacheSize);

        // set it up in the cache
        row = this.SetupDataToCache(ref list, type, id, primaryKey);

        if (row == null)
        {
          return string.Empty;
        }
      }

      return CleanStringForURL(row[nameField].ToString());
    }

    /// <summary>
    /// The get topic name from message.
    /// </summary>
    /// <param name="id">
    /// The id.
    /// </param>
    /// <returns>
    /// The get topic name from message.
    /// </returns>
    protected string GetTopicNameFromMessage(int id)
    {
      const string type = "Message";
      const string primaryKey = "MessageID";

      DataRow row = this.GetDataRowFromCache(type, id);

      if (row == null)
      {
        // get the section desired...
        DataTable list = DB.message_simplelist(this.LowRange(id), this.CacheSize);

        // set it up in the cache
        row = this.SetupDataToCache(ref list, type, id, primaryKey);

        if (row == null)
        {
          return string.Empty;
        }
      }

      return this.GetTopicName(Convert.ToInt32(row["TopicID"]));
    }

    /// <summary>
    /// The setup data to cache.
    /// </summary>
    /// <param name="list">
    /// The list.
    /// </param>
    /// <param name="type">
    /// The type.
    /// </param>
    /// <param name="id">
    /// The id.
    /// </param>
    /// <param name="primaryKey">
    /// The primary key.
    /// </param>
    /// <returns>
    /// </returns>
    protected DataRow SetupDataToCache(ref DataTable list, string type, int id, string primaryKey)
    {
      DataRow row = null;

      if (list != null)
      {
        list.Columns[primaryKey].Unique = true;
        list.PrimaryKey = new[] { list.Columns[primaryKey] };

        // store it for the future
        var randomValue = new Random();
        HttpContext.Current.Cache.Insert(
          this.GetCacheName(type, id), 
          list, 
          null, 
          DateTime.UtcNow.AddMinutes(randomValue.Next(5, 15)), 
          Cache.NoSlidingExpiration, 
          CacheItemPriority.Low, 
          null);

        // find and return profile..
        row = list.Rows.Find(id);

        if (row == null)
        {
          // invalidate this cache section
          HttpContext.Current.Cache.Remove(this.GetCacheName(type, id));
        }
      }

      return row;
    }

    #endregion
  }
}