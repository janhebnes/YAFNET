﻿/* Yet Another Forum.NET
 * Copyright (C) 2006-2009 Jaben Cargman
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
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Web.Compilation;

namespace YAF.Modules
{
  /// <summary>
  /// The yaf module.
  /// </summary>
  [AttributeUsage(AttributeTargets.Class)]
  public class YafModule : Attribute
  {
    /// <summary>
    /// The _module author.
    /// </summary>
    private string _moduleAuthor;

    /// <summary>
    /// The _module name.
    /// </summary>
    private string _moduleName;

    /// <summary>
    /// The _module version.
    /// </summary>
    private int _moduleVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="YafModule"/> class.
    /// </summary>
    /// <param name="moduleName">
    /// The module name.
    /// </param>
    /// <param name="moduleAuthor">
    /// The module author.
    /// </param>
    /// <param name="moduleVersion">
    /// The module version.
    /// </param>
    public YafModule(string moduleName, string moduleAuthor, int moduleVersion)
    {
      this._moduleName = moduleName;
      this._moduleAuthor = moduleAuthor;
      this._moduleVersion = moduleVersion;
    }

    /// <summary>
    /// Gets or sets ModuleName.
    /// </summary>
    public string ModuleName
    {
      get
      {
        return this._moduleName;
      }

      set
      {
        this._moduleName = value;
      }
    }

    /// <summary>
    /// Gets or sets ModuleAuthor.
    /// </summary>
    public string ModuleAuthor
    {
      get
      {
        return this._moduleAuthor;
      }

      set
      {
        this._moduleAuthor = value;
      }
    }

    /// <summary>
    /// Gets or sets ModuleVersion.
    /// </summary>
    public int ModuleVersion
    {
      get
      {
        return this._moduleVersion;
      }

      set
      {
        this._moduleVersion = value;
      }
    }
  }

  /// <summary>
  /// The i base module.
  /// </summary>
  public interface IBaseModule : IDisposable
  {
    /// <summary>
    /// Gets or sets ForumControlObj.
    /// </summary>
    object ForumControlObj
    {
      get;
      set;
    }

    /// <summary>
    /// The init.
    /// </summary>
    void Init();
  }

  /// <summary>
  /// Handles IBaseModule types.
  /// </summary>
  public class YafBaseModuleManager : YafModuleManager<IBaseModule>
  {
    /// <summary>
    /// The _init called.
    /// </summary>
    protected bool _initCalled = false;

    /// <summary>
    /// Prevents a default instance of the <see cref="YafBaseModuleManager"/> class from being created.
    /// </summary>
    private YafBaseModuleManager()
      : base("YAF.Modules", "YAF.Modules.IBaseModule")
    {
      if (ModuleClassTypes == null)
      {
        // get the .Core module to add...
        AddModules(
          new List<Assembly>()
            {
              Assembly.GetExecutingAssembly()
            });

        // re-add these modules...
        AddModules(BuildManager.CodeAssemblies);
      }
    }

    /// <summary>
    /// The call init modules.
    /// </summary>
    /// <param name="forumControl">
    /// The forum control.
    /// </param>
    public void CallInitModules(object forumControl)
    {
      if (!this._initCalled)
      {
        foreach (IBaseModule currentModule in Modules)
        {
          currentModule.ForumControlObj = forumControl;
          currentModule.Init();
        }

        this._initCalled = true;
      }
    }
  }
}