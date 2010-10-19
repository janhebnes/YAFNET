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

namespace YAF.controls
{
  // YAF.Pages
  #region Using

  using System;
  using System.Data;
  using System.Linq;
  using System.Web;
  using System.Web.UI.HtmlControls;
  using System.Web.UI.WebControls;

  using YAF.Classes.Core;
  using YAF.Classes.Data;
  using YAF.Classes.Utils;
  using YAF.Controls;

  #endregion

  /// <summary>
  /// PollList Class
  /// </summary>
  public partial class PollChoiceList : BaseUserControl
  {

    #region Properties

    /// <summary>
    /// Gets or sets IsLocked
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Gets or sets IsLocked
    /// </summary>
    public bool IsClosed { get; set; }

    /// <summary>
    /// Gets or sets MaxImageAspect. Stores max aspect to get rows of equal height.
    /// </summary>
    public decimal MaxImageAspect { get; set; }

    /// <summary>
    /// Gets or sets Can Vote
    /// </summary>
    public bool CanVote { get; set; }

    /// <summary>
    ///   The HideResults.
    /// </summary>
    public bool HideResults { get; set; }

    /// <summary>
    ///   The DataSource.
    /// </summary>
    public DataTable DataSource { get; set; }

    /// <summary>
    ///   The PollId.
    /// </summary>
    public int PollId { get; set; }

    /// <summary>
    ///   The ChoiceId.
    /// </summary>
    public int ChoiceId { get; set; }

    /// <summary>
    ///   The DaysToRun.
    /// </summary>
    public int? DaysToRun { get; set; }

    /// <summary>
    ///   The Votes.
    /// </summary>
    public int Votes { get; set; }

    /// <summary>
    /// The event bubbles info to parent control to rebind repeater. 
    /// </summary>
    public event EventHandler ChoiceVoted;

    #endregion

    #region Protected Methods

    /// <summary>
    /// Get Theme Contents
    /// </summary>
    /// <param name="page">
    /// The Page
    /// </param>
    /// <param name="tag">
    /// Tag
    /// </param>
    /// <returns>
    /// Content
    /// </returns>
    protected string GetThemeContents(string page, string tag)
    {
      return this.PageContext.Theme.GetItem(page, tag);
    }

    #endregion

    #region Methods

    /// <summary>
    /// The get image height.
    /// </summary>
    /// <param name="mimeType">
    /// The mime type.
    /// </param>
    /// <returns>
    /// The get image height.
    /// </returns>
    protected int GetImageHeight(object mimeType)
    {
      string[] attrs = mimeType.ToString().Split('!')[1].Split(';');
      return Convert.ToInt32(attrs[1]);
    }

    /// <summary>
    /// The get poll question.
    /// </summary>
    /// <returns>
    /// The get poll question.
    /// </returns>
    protected string GetPollQuestion()
    {
        return this.DataSource.Rows[0]["Question"].ToString();
    }

    /// <summary>
    /// The get total.
    /// </summary>
    /// <param name="pollId">
    /// The poll Id.
    /// </param>
    /// <returns>
    /// The get total.
    /// </returns>
    protected string GetTotal(object pollId)
    {

        return this.DataSource.Rows[0]["Total"].ToString();

        return string.Empty;
    }

    /// <summary>
    /// Page_Load
    /// </summary>
    /// <param name="sender">
    /// </param>
    /// <param name="e">
    /// </param>
    protected void Page_Load(object sender, EventArgs e)
    {

        PageContext.LoadMessage.Clear();
        BindData();
       
    }

    /// <summary>
    /// The poll_ item command.
    /// </summary>
    /// <param name="source">
    /// The source.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void Poll_ItemCommand(object source, RepeaterCommandEventArgs e)
    {
      if (e.CommandName == "vote")
      {
       
          if (!this.CanVote)
        {
          this.PageContext.AddLoadMessage(this.PageContext.Localization.GetText("WARN_ALREADY_VOTED"));
          return;
        }

        if (this.IsLocked)
        {
          this.PageContext.AddLoadMessage(this.PageContext.Localization.GetText("WARN_TOPIC_LOCKED"));
          return;
        }

        if (IsClosed)
        {
            this.PageContext.AddLoadMessage(this.PageContext.Localization.GetText("WARN_POLL_CLOSED"));
            return;
        }

        object userID = null;
        object remoteIP = null;

        if (this.PageContext.BoardSettings.PollVoteTiedToIP)
        {
          remoteIP = IPHelper.IPStrToLong(this.Request.ServerVariables["REMOTE_ADDR"]).ToString();
        }

        if (!this.PageContext.IsGuest)
        {
          userID = this.PageContext.PageUserID;
        }

        DB.choice_vote(e.CommandArgument, userID, remoteIP);
       
       // this.ChoiceId = Convert.ToInt32(e.CommandArgument.ToString());
       // this.CanVote = false;

        // save the voting cookie...
        var c = new HttpCookie(this.VotingCookieName(PollId), e.CommandArgument.ToString())
          {
             Expires = DateTime.UtcNow.AddYears(1) 
          };
        this.Response.Cookies.Add(c);
        
        // show an info that the user is voted 
        string msg = this.PageContext.Localization.GetText("INFO_VOTED");
      
        this.BindData();

        if (ChoiceVoted != null)
            ChoiceVoted(source, e);

        // show the notification  window to user
        this.PageContext.AddLoadMessage(msg);
       
      }

    }

    /// <summary>
    /// The poll_ on item data bound.
    /// </summary>
    /// <param name="source">
    /// The source.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void Poll_OnItemDataBound(object source, RepeaterItemEventArgs e)
    {
      RepeaterItem item = e.Item;
      var drowv = (DataRowView)e.Item.DataItem;
      var trow = item.FindControlRecursiveAs<HtmlTableRow>("VoteTr");

      if (item.ItemType == ListItemType.Item || item.ItemType == ListItemType.AlternatingItem)
      {

        // Voting link 
        var myLinkButton = item.FindControlRecursiveAs<MyLinkButton>("MyLinkButton1");
        string pollId = drowv.Row["PollID"].ToString();

        myLinkButton.Enabled = this.CanVote;
        myLinkButton.ToolTip = this.PageContext.Localization.GetText("POLLEDIT", "POLL_PLEASEVOTE");
        myLinkButton.Visible = true;
        item.FindControlRecursiveAs<HtmlImage>("YourChoice").Visible = (int) drowv.Row["ChoiceID"] == this.ChoiceId;
        

        // Poll Choice image
        var choiceImage = item.FindControlRecursiveAs<HtmlImage>("ChoiceImage");
        var choiceAnchor = item.FindControlRecursiveAs<HtmlAnchor>("ChoiceAnchor");

        // Don't render if it's a standard image
        if (!drowv.Row["ObjectPath"].IsNullOrEmptyDBField())
        {
          choiceAnchor.Attributes["rel"] = "lightbox-group" + Guid.NewGuid().ToString().Substring(0, 5);
          choiceAnchor.HRef = drowv.Row["ObjectPath"].IsNullOrEmptyDBField()
                                ? this.GetThemeContents("VOTE", "POLL_CHOICE")
                                : this.HtmlEncode(drowv.Row["ObjectPath"].ToString());
          choiceAnchor.Title = drowv.Row["ObjectPath"].ToString();

          choiceImage.Src = choiceImage.Alt = this.HtmlEncode(drowv.Row["ObjectPath"].ToString());
         

          if (!drowv.Row["MimeType"].IsNullOrEmptyDBField())
          {
            decimal aspect = GetImageAspect(drowv.Row["MimeType"]);

            // hardcoded - bad
            const int imageWidth = 80;
            choiceImage.Attributes["style"] = "width:{0}px; height:{1}px;".FormatWith(
              imageWidth, choiceImage.Width / aspect);

            // reserved to get equal row heights
            string height = (this.MaxImageAspect * choiceImage.Width).ToString();
            trow.Attributes["style"] = "height:{0}px;".FormatWith(height);
          }
        }
        else
        {
          choiceImage.Alt = this.PageContext.Localization.GetText("POLLEDIT", "POLL_PLEASEVOTE");
          choiceImage.Src = this.GetThemeContents("VOTE", "POLL_CHOICE");
          choiceAnchor.HRef = string.Empty;
        }
         
        item.FindControlRecursiveAs<Panel>("MaskSpan").Visible = this.HideResults;
        item.FindControlRecursiveAs<Panel>("resultsSpan").Visible = !this.HideResults;
        item.FindControlRecursiveAs<Panel>("VoteSpan").Visible = !this.HideResults;
      }


    }


    /// <summary>
    /// The remove poll_ completely load.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void RemovePollCompletely_Load(object sender, EventArgs e)
    {
      ((ThemeButton)sender).Attributes["onclick"] =
        "return confirm('{0}');".FormatWith(this.PageContext.Localization.GetText("POLLEDIT", "ASK_POLL_DELETE_ALL"));
    }

    /// <summary>
    /// The remove poll_ load.
    /// </summary>
    /// <param name="sender">
    /// The sender.
    /// </param>
    /// <param name="e">
    /// The e.
    /// </param>
    protected void RemovePoll_Load(object sender, EventArgs e)
    {
      ((ThemeButton)sender).Attributes["onclick"] =
        "return confirm('{0}');".FormatWith(this.PageContext.Localization.GetText("POLLEDIT", "ASK_POLL_DELETE"));
    }

    /// <summary>
    /// The vote width.
    /// </summary>
    /// <param name="o">
    /// The o.
    /// </param>
    /// <returns>
    /// The vote width.
    /// </returns>
    protected int VoteWidth(object o)
    {
      var row = (DataRowView)o;
      return (int)row.Row["Stats"] * 80 / 100;
    }


    /// <summary>
    /// Returns an image width|height ratio.
    /// </summary>
    /// <param name="mimeType">
    /// </param>
    /// <returns>
    /// The get image aspect.
    /// </returns>
    private static decimal GetImageAspect(object mimeType)
    {
      if (!mimeType.IsNullOrEmptyDBField())
      {
        string[] attrs = mimeType.ToString().Split('!')[1].Split(';');
        decimal width = Convert.ToDecimal(attrs[0]);
        return width / Convert.ToDecimal(attrs[1]);
      }

      return 1;
    }

    /// <summary>
    /// The get poll is closed.
    /// </summary>
    /// <returns>
    /// The get poll is closed.
    /// </returns>
    protected string GetPollIsClosed()
    {
        string strPollClosed = string.Empty;
        if (this.IsClosed)
        {
            strPollClosed = this.PageContext.Localization.GetText("POLL_CLOSED");
        }

        return strPollClosed;
    }

    /// <summary>
    /// The bind data.
    /// </summary>
    private void BindData()
    {
      this.DataBind();
    }

    /// <summary>
    /// Checks if a poll has no votes.
    /// </summary>
    /// <param name="pollId">
    /// </param>
    /// <returns>
    /// The poll has no votes.
    /// </returns>
    private bool PollHasNoVotes(object pollId)
    {
      return
        this.DataSource.Rows.Cast<DataRow>().Where(dr => dr["PollID"].ToType<int>() == pollId.ToType<int>()).All(
          dr => dr["Votes"].ToType<int>() <= 0);
    }
    

    /// <summary>
    /// Gets VotingCookieName.
    /// </summary>
    /// <param name="pollId">
    /// The poll Id.
    /// </param>
    /// <returns>
    /// The voting cookie name.
    /// </returns>
    protected string VotingCookieName(int pollId)
    {
        return "poll#{0}".FormatWith(pollId);
    }

    #endregion
  }
}