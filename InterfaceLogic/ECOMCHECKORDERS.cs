
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
//ERRSOURCE: ECOM.ECOMCHECKORDERS

  public partial class ECOMCHECKORDERS
  {
    [UUID("3f6be14eda1b47fda5b9c7ab9ff6bb2b")]
    virtual public void OperatorSendReport()
    {
      var ecomChannel = new ECOM.ECOMCHANNELS();
      ecomChannel.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannel.REF.Symbol} = 0{_channelref}");
      if(!ecomChannel.FirstRecord())
      {
        throw new Exception($"Nie znaleziono kanału sprzedaży o ref : {_channelref.ToString()}");
      }
      
      string emailAddresses = ecomChannel.AUTOORDREPORTEMAILADDRESS;
      string message = LOGIC.ECOMCHECKORDERS.SendOrdersVerificationReport(_channelref.AsInteger, _fromdate.AsDateTime, _todate.AsDateTime, emailAddresses);
      GUI.ShowBalloonHint(message,"Wysłanie raportu", IconType.INFORMATION);
      ecomChannel.Close();
    }
    
    

    [UUID("776cd0cd1d74434bb96a0ccdd4dd1ea5")]
    virtual public void ShowCheckOrdersBrowse()
    {
      CloseForm();
      Contexts c = new Contexts();
      c.Add("_todate", _todate);
      c.Add("_fromdate", _fromdate); 
      c.Add("_channelref", _channelref);  
      GUI.ShowForm("ECOM.ECOMCHECKORDERS", "BROWSE", c);  
      
    }
    
    

    [UUID("7b8645136d0444389ab5d99a0270bf80")]
    virtual public string Initialize_todate()
    {
      return DateTime.Now.ToString();
    }
    
    

    [UUID("86ca21180a234fd0a5d65bc54ac35ae6")]
    virtual public string Validate_emailaddresses()
    {
      if(_emailaddresses.Empty)
      {
        return "Wprowadź adresy email";
      }
      return "";
    }
    
    

    [UUID("88110d40e4c84830b8e5579bb4c78fd5")]
    virtual public bool IsOperatorConfirmAndSendReportEnabled()
    {
      if(_emailaddresses.Empty)
      {
        return false;
      }
      return true;
    }
    
    

    [UUID("a09e6eeb64574564ba83d4ac81102a92")]
    virtual public string Validate_fromdate()
    { 
      if (_fromdate.Empty)
      {
       return "Wprowadź datę";
      }
      return "";
    }
    
    

    [UUID("a5ae7c52869440cc956a9fac742b0b23")]
    virtual public bool IsShowCheckOrdersBrowseEnabled()
    {
      if (_fromdate.Empty || _todate.Empty || _todate.AsDateTime < _fromdate.AsDateTime)
      {
        return false;  
      }
      return true;
    }
    
    

    [UUID("a5c1caa50585494c95e5268b46d5b589")]
    virtual public void OperatorConfirmAndSendReport()
    {
      var ecomChannel = new ECOM.ECOMCHANNELS(); 
      string emailAddresses = ecomChannel.AUTOORDREPORTEMAILADDRESS;
      string message = LOGIC.ECOMCHECKORDERS.SendOrdersVerificationReport(_channelref.AsInteger, _fromdate.AsDateTime, _todate.AsDateTime, _emailaddresses);
      GUI.ShowBalloonHint(message,"Wysłanie raportu", IconType.INFORMATION);
      CloseForm();
    }
    
    

    [UUID("c581592366b74097a2bad7e33661c0d9")]
    virtual public void OperatorEnterEmailAddresses()
    {
      var c = new Contexts();     
      c.Add("_channelref", _channelref);
      c.Add("_todate", _todate);
      c.Add("_fromdate", _fromdate);
      GUI.ShowForm("ECOM.ECOMCHECKORDERS", "ENTEREMAIL", c);
    }
    
    

    [UUID("ce727e1fee13438bb938005e7e49fab0")]
    virtual public string Validate_todate()
    {
      if (_todate.Empty)
      {
       return "Wprowadź datę";
      }
      if(_todate.AsDateTime < _fromdate.AsDateTime)
      {
        return "Wprowadzono nieprawidłowy zakres dat.";  
      }
      return "";
    }
  }

}
