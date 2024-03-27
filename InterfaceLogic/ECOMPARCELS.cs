
#region usings
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Data;
using System.Globalization;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Concurrency;
using System.Drawing;
using Neos.BusinessPlatform.Custom;
using Neos.BusinessPlatform.Dev.Legacy;
using Neos.Core;
using Neos.Core.Misc;
using Neos.Core.Database;
using Neos.Core.Communication.FileTransfer;
using Neos.BusinessPlatform.Serializer;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Net.Http;
using Neos.Core.Communication.Webservice;
using Neos.Core.ImapSynchro;
using Neos.Core.Api.Voip;
using Neos.Core.Interop.Misc;
using Neos.Core.Interop.DataTypes;
using Neos.Core.Interop.Interfaces;
using Neos.Core.Interop.Enums;
using Neos.Core.Interop.Exceptions.Scripting;
using Neos.Core.Interop.Database.Metadata.Attributes;
using Neos.Core.Interop.Services.Webservice.Models;
using Neos.Core.Interop.Telemetry.Models;
using Neos.Core.Interop.Messaging.Interfaces;
using Neos.Core.Messaging.Interfaces;
using Neos.BusinessPlatform.Custom.APIExtensions;
using Limilabs.Mail;
using Limilabs.Mail.MIME;
using Limilabs.Mail.Headers;
using RestSharp;
using Newtonsoft.Json;
using Neos.Common;
using Autofac;
using Neos.BusinessPlatform.Dev.NGui;
using Neos.BusinessPlatform.Dev.NTelemetry;
using Neos.BusinessPlatform.Dev.NSystem;
using Neos.BusinessPlatform.Dev.NEDA;
using MassTransit;
using GreenPipes;
using System.Threading.Tasks;
using Neos.BusinessPlatform.Messaging.Attributes;
using Neos.BusinessPlatform.Messaging.Connectors;
using Neos.BusinessPlatform.Messaging;
using Neos.Core.Interop.DAL.Interfaces;
using Neos.Core.Messaging.Extensions;
using Shipping.CzechLogistic;
using Shipping.DPD;
using Shipping.Fedex;
using Shipping.GLS;
using GUSPlugin;
using Shipping.InPost;
using KSeFPlugin;
using Shipping.NoLimit;
using Shipping.PWR;
using Shipping.Raben;
using Shipping.Schenker;
using Shipping.UPS;
using ViesPlugin;
using NeosPrinterHelper;
using EComIAIDriver;
using System.Linq.Dynamic.Core;
using EComBLDriver;
using Neos.SServer;
using COMMON;
using SYSTEM;
using TD;
#endregion

namespace ECOM
{
//ERRSOURCE: ECOM.ECOMPARCELS

  public partial class ECOMPARCELS
  {
    /// <summary>
    /// automatycznie wygenerowana metoda na inicjalizację pola REF
    /// </summary>
    [UUID("010b0a2f51f64d95bf664528523e7bd3")]
    virtual public string InitializeREF()
    {
      return GenRef();
    }
    
    
    

    [UUID("10b9afa8a99d45ac9bf08a570f04e420")]
    virtual public string SetBROWSELABELSFormDBFilter()
    {
      return " ECOMORDER = " + _ecomorder.AsInteger;
    }
    
    

    [UUID("2c2fb57251e046a4bbdb329cdee2eeb5")]
    virtual public void ShowAttachments()
    {
        string filename = this.ECOMORDER_ECOMORDERID + @"\" + Path.GetFileName(this.LABELPATH);
        var ti = GUI.CreateFileTransfer("ECOMLABELS", filename);
        ti.Direction = TransferDirection.DownloadFromServer;
        ti.OnError = (tinfo) => { 
          GUI.ShowBalloonHint(tinfo.Exception.Message, "Błąd", IconType.STOP);
        };
        ti.OnSuccess = (tinfo) => { 
          GUI.ShellExecute(ti.LocalFile,"");
          GUI.ShowBalloonHint("Plik znajdziesz w "+ti.LocalFile,"OK", IconType.INFORMATION);
        };
        ti.StartAsync(); //uruchamiamy transfer asynchronicznie
    }
    
    

    [UUID("b9d709f2dc4741e6892c75a9181123e6")]
    virtual public string SetLabelUrl()
    {
      string filename =  this.ECOMORDER_ECOMORDERID + @"\" + Path.GetFileName(this.LABELPATH);
      string url = filename;
      if(!string.IsNullOrEmpty(filename))
      {    
        url = GUI.GetRepositoryFileUrl("ECOMLABELS",filename);
        if(GUI.IsChromiumPreviewFormat(filename))
          {
            return url; 
          }
        else
          {
          url = GUI.GetWWWClientUrl("static/nopreview.html", "");
          return url;
          }
      }
      else
      {
        url = GUI.GetWWWClientUrl("static/noattachments.html", "");
      }  
      return url; 
    }
    
    

    [UUID("e57628803b4442e3a9334054425b9250")]
    virtual public bool IsShowAttachmentsEnabled()
    {
      if(!String.IsNullOrEmpty(this.LABELPATH.ToString()))
      {
        return true;
      }
      return false;
    }
  }

}
