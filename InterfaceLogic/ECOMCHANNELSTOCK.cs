
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
//ERRSOURCE: ECOM.ECOMCHANNELSTOCK

  public partial class ECOMCHANNELSTOCK
  {
    /// <summary>
    /// Uruchamia naliczanie wszystkich stanów magazynowych w wybranych magazynach
    /// </summary>
    [UUID("83cd17587c7a41f7a1321786f9c95543")]
    virtual public void OperatorCalculateSelectedChannelStocks()
    {
      try
      {
        var ecomCahnnelStockList = new List<Int64>();
        foreach(var item in SelectedRowsOrCurrent)
        {
          ecomCahnnelStockList.Add(Int64.Parse(item["REF"]));       
        }
    
        var calculateChannelStocksMsg 
          = LOGIC.ECOMCHANNELS.CalculateInventroryStocks(CalculateMode.All, this._ecomchannelref.AsInteger, ecomCahnnelStockList);
    
        if(!string.IsNullOrEmpty(calculateChannelStocksMsg))
        {
          GUI.ShowBalloonHint(calculateChannelStocksMsg,"", IconType.WARNING);   
        }
        else
        {
          GUI.ShowBalloonHint("Naliczono nieaktualne stany magazynowe w kanale sprzedaży dla wybranych magazynów","", IconType.INFORMATION);   
        }         
      }
      catch(Exception ex)
      {
        GUI.ShowBalloonHint($"Błąd naliczania stanów magazynowych: {ex.Message}" ,"", IconType.STOP);   
      }
      this.RefreshData();  
    }
    
    

    [UUID("917b5543b6b5421b91150be847043245")]
    virtual public string SetBROWSEFormDBFilter()
    {
      if(string.IsNullOrEmpty(_ecomchannelref))
      {
        return "";
      }
      else
      {
        return $"ECOMCHANNELREF = 0{_ecomchannelref.ToString()}" ;
      }
    }
    
    

    [UUID("a9fdd0675adf49bdb8eb3a97c4bddaf2")]
    virtual public string InitializeECOMCHANNELREF()
    {
      return this._ecomchannelref;
    }
    
    

    /// <summary>
    /// automatycznie wygenerowana metoda na inicjalizację pola REF
    /// </summary>
    [UUID("af1c2ecde47443fca7e0c01fff53e1ab")]
    virtual public string InitializeREF()
    {
      return GenRef();
    }
  }

}
