
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
//ERRSOURCE: ECOM.ECOMCHECKPRICES

  public partial class ECOMCHECKPRICES
  {
    [UUID("06d185d98ade42e3a30e73ec4c1ddeb8")]
    virtual public void OperatorEnterEmailAddresses()
    {
      var c = new Contexts();     
      c.Add("_channelref", _channelref);
      GUI.ShowForm("ECOM.ECOMCHECKPRICES", "ENTEREMAIL", c);
    }
    
    

    [UUID("677efeffb7a143109307b544d2ba7490")]
    virtual public bool IsOperatorConfirmAndSendReportEnabled()
    {
      if(_emailaddresses.Empty)
      {
        return false;
      }
      return true;
    }
    
    

    [UUID("af437590665a4e45a0a260b71f03b7fe")]
    virtual public void OperatorConfirmAndSendReport()
    {
      var ecomChannel = new ECOM.ECOMCHANNELS(); 
      string emailAddresses = ecomChannel.AUTOORDREPORTEMAILADDRESS;
      string message = LOGIC.ECOMCHECKPRICES.SendPriceVerificationReport(_channelref.AsInteger, _emailaddresses);
      GUI.ShowBalloonHint(message,"Wysłanie raportu", IconType.INFORMATION);
      CloseForm();
    }
    
    

    [UUID("b462bc94c9d14dccb4865bb72d75b345")]
    virtual public bool IsOperatorSetSelectedInventoryPricesAsInvalidEnabled()
    {
      if(PRICEISVALID != "1")
      {
        return false;   
      }
      return true;
    }
    
    

    [UUID("c00a6d695f324a919b1d5c25936e2490")]
    virtual public void OperatorSetSelectedInventoryPricesAsInvalid()
    {
      string result = "";    
      var ecomInventoryPrice = new  ECOM.ECOMINVENTORYPRICES();
      foreach (var price in SelectedRowsOrCurrent)
      {
        try
        {   
          if(!String.IsNullOrEmpty(price["ECOMINVENTORYREF"].ToString()))
          {
            ecomInventoryPrice.FilterAndSort($"{nameof(ECOM.ECOMINVENTORYPRICES)}.{ecomInventoryPrice.ECOMINVENTORYREF.Symbol} = 0{price["ECOMINVENTORYREF"]}");
            if(ecomInventoryPrice.FirstRecord())
            {
              ecomInventoryPrice.EditRecord();
              ecomInventoryPrice.ISVALID = 0;      
              if(!ecomInventoryPrice.PostRecord())
              {
                throw new Exception($"Błąd zapisu do bazy danych oznaczenia ceny o REF: {price["REF"]} do wysłania");
              }	   
                    
            }
            else
            {
              result += $"Błąd oznaczania ceny o REF: {price["REF"]} do wysłania\n";
              continue;    
            }
          }
          else
          {
            result = "Nie wybrano cen do oznaczenia.";
          }
          
        }
        catch(Exception ex)
        {
          result += $"Błąd oznaczania ceny o REF: {price["REF"]} do wysłania: {ex.Message}\n";
          continue;
        }       
      }
    
      if(string.IsNullOrEmpty(result))
      {
        GUI.ShowBalloonHint($"Oznaczono zaznaczone ceny jako nieaktualne","", IconType.INFORMATION);      
        this.RefreshData();
      }
      else
      {
        GUI.ShowBalloonHint($"Błąd oznaczania cen: {result}","", IconType.STOP);       
      }
      ecomInventoryPrice.Close();
    
    }
    
    

    [UUID("c33135594a9943b6a28732a95a899a92")]
    virtual public string Validate_emailaddresses()
    {
      if(_emailaddresses.Empty)
      {
        return "Wprowadź adresy email";
      }
      return "";
    }
    
    

    [UUID("cfcfc374ee8446b58d6ab512c8d32e05")]
    virtual public void OperatorSendReport()
    {
      var ecomChannel = new ECOM.ECOMCHANNELS();
      ecomChannel.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannel.REF.Symbol} = 0{_channelref}");
      if(!ecomChannel.FirstRecord())
      {
        throw new Exception($"Nie znaleziono kanału sprzedaży o ref : {_channelref.ToString()}");
      }
      string emailAddresses = ecomChannel.AUTOORDREPORTEMAILADDRESS;
      string message = LOGIC.ECOMCHECKPRICES.SendPriceVerificationReport(_channelref.AsInteger, emailAddresses);
      GUI.ShowBalloonHint(message,"Wysłanie raportu", IconType.INFORMATION);
      ecomChannel.Close();
    }
  }

}
