
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
//ERRSOURCE: ECOM.ECOMORDERPAYMENTS

  public partial class ECOMORDERPAYMENTS
  {
    [UUID("8367dcb84f794a6a88d488541b18c0a9")]
    virtual public void OperatorGetOrdersPaymentHistory()
    {
      try
      {
        string getOrdersPaymentHistoryMsg = "";
        if(_ecomorderid.Empty)
        {
          throw new Exception("Brak ID zamówienia.");
        }
        List<int> orderList = new List<int>();
        orderList.Add(_ecomorderid.AsInteger);
        getOrdersPaymentHistoryMsg = LOGIC.ECOMCHANNELS.GetOrdersPayment(_ecomchannelref.AsInteger, orderList) 
          + "\n" + getOrdersPaymentHistoryMsg;
        if(!string.IsNullOrEmpty(getOrdersPaymentHistoryMsg))
        {
          GUI.ShowBalloonHint(getOrdersPaymentHistoryMsg, "Pobieranie historii płatności zamówień", IconType.INFORMATION);   
        }  
        RefreshData("ECOM.ECOMORDERPAYMENTS","C");  
      }
      catch(Exception ex)
      {
        throw new Exception(ex.Message);
        ShowBalloonHint($"Błąd generowania komendy pobierania historii płatności zamówień dla kanału sprzedaży {this.ECOMCHANNELREF}: {ex.Message}", "Pobieranie historii płatności zamówień", IconType.STOP); 
      }
      
    }
    
    

    [UUID("99c82a3e8ca74c14a496f30da9e83270")]
    virtual public string SetDBFilter()
    {
      return $"ECOMORDERREF = '{this._ecomorderref.AsInteger}' and ECOMCHANNELREF = {this._ecomchannelref.AsInteger}";
    }
    
    

    /// <summary>
    /// automatycznie wygenerowana metoda na inicjalizację pola REF
    /// </summary>
    [UUID("be48c310cefd4f5d926d0f16d9d1e606")]
    virtual public string InitializeREF()
    {
      return GenRef();
    }
  }

}
