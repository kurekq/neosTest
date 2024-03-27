
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
//ERRSOURCE: ECOM.ECOMCHECKSTOCKS

  public partial class ECOMCHECKSTOCKS
  {
    [UUID("1117e04b218e42febba719f179b9009c")]
    virtual public void OperatorSendReport()
    {
      var ecomChannel = new ECOM.ECOMCHANNELS();
      ecomChannel.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannel.REF.Symbol} = 0{_channelref}");
      if(!ecomChannel.FirstRecord())
      {
        throw new Exception($"Nie znaleziono kanału sprzedaży o REF : {_channelref.ToString()}");
      }
      string emailAddresses = ecomChannel.AUTOORDREPORTEMAILADDRESS;
      string message = LOGIC.ECOMCHECKSTOCKS.SendStockVerificationReport(_channelref.AsInteger, emailAddresses);
      GUI.ShowBalloonHint(message,"Wysłanie raportu", IconType.INFORMATION);
      ecomChannel.Close();
    }
    
    

    [UUID("35226390cd4a4816a02d8013c7586ab7")]
    virtual public void OperatorSetSelectedInventoryStocksAsInvalid()
    {
      
    }
    
    

    [UUID("b5adc30895b242df99da80a302595c5d")]
    virtual public string Validate_emailaddresses()
    {
      if(_emailaddresses.Empty)
      {
        return "Wprowadź adresy email";
      }
      return "";
    }
    
    

    [UUID("c711980cfdbc42d9a83122e4eb2d5613")]
    virtual public void OperatorEnterEmailAddresses()
    {
      var c = new Contexts();     
      c.Add("_channelref", _channelref);
      GUI.ShowForm("ECOM.ECOMCHECKSTOCKS", "ENTEREMAIL", c);
    }
    
    

    [UUID("e697e3a6bf37462885610eac13eef620")]
    virtual public bool IsOperatorConfirmAndSendReportEnabled()
    {
      if(_emailaddresses.Empty)
      {
        return false;
      }
      return true;
    }
    
    

    [UUID("eab6655ed90b49b8ba00fc38eea8e24b")]
    virtual public void OperatorConfirmAndSendReport()
    {
      var ecomChannel = new ECOM.ECOMCHANNELS(); 
      string emailAddresses = ecomChannel.AUTOORDREPORTEMAILADDRESS;
      string message = LOGIC.ECOMCHECKSTOCKS.SendStockVerificationReport(_channelref.AsInteger, _emailaddresses);
      GUI.ShowBalloonHint(message,"Wysłanie raportu", IconType.INFORMATION);
      CloseForm();//
    }
  }

}
