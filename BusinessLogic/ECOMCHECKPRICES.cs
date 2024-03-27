
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

namespace ECOM.UNDERLYINGLOGIC
{
  #region usings
  using System.Collections.Generic;
  using ECOM;
  using Neos.BusinessPlatform.Dev.NCore;
  using Neos.BusinessPlatform.Dev.NEDA;
  using Neos.BusinessPlatform.Dev.NTelemetry;
  using Neos.Core.Interop.Messaging.Enums;
  using Neos.Core.Interop.Messaging.Models;
  using static Neos.BusinessPlatform.Dev.NCore.CORE;
  #endregion
  
  public partial class ECOMCHECKPRICES<TModel> where TModel : MODEL.ECOMCHECKPRICES, new()
  {

    /// <param name="context"></param>
    [UUID("Fill")]
    override protected IEnumerable<TModel> Fill(Contexts context)
    {
      var inventoryList = new List<String>(); 
      string sqlString = @"select inv.ref, inv.ecominventoryid inventoryid, w.nazwat, invp.currency, invp.basicprice, invp.isvalid
                              from ECOMINVENTORYPRICES invp
                                join ECOMINVENTORIES inv on inv.ref = invp.ecominventoryref
                                join WERSJE w on w.ref = inv.wersjaref " +   
                            $"where inv.ECOMCHANNELREF = {context["_channelref"]} "; 
    
      foreach(var item in CORE.QuerySQL(sqlString))
      {
        inventoryList.Add(item["INVENTORYID"].AsString);
      }
      
      var pricesFromWebList = LOGIC.ECOMCHANNELS.GetPricesList(context["_channelref"].AsInteger, inventoryList);
      
      var pricesFromDBList = CORE.QuerySQL(ECOM.ProjectInfo.DefaultDatabaseAlias, sqlString).Select(s => new { 
        ECOMINVENTORYREF = s["REF"], 
        ECOMINVENTORYID = s["INVENTORYID"].AsString,
        BASICPRICE = s["BASICPRICE"],
        CURRENCY = s["CURRENCY"],
        NAME = s["NAZWAT"],
        ISVALID = s["ISVALID"]
      }).ToList();  
        
      var leftOuterJoin =
          from priceFromWeb in pricesFromWebList
          join priceFromDb in pricesFromDBList on priceFromWeb.InventoryId.ToString() equals priceFromDb.ECOMINVENTORYID into temp
          from priceFromDb in temp.DefaultIfEmpty()
          select new
          {
            InventoryId = priceFromWeb.InventoryId,
            BasicPriceWeb = priceFromWeb.RetailPriceNet,
            BasicPriceDb = priceFromDb?.BASICPRICE, 
            CurrencyWeb = priceFromWeb.Currency,
            CurrenyDb = priceFromDb?.CURRENCY,  
            InventoryNameDb = priceFromDb?.NAME,
            InventoryRef =  priceFromDb?.ECOMINVENTORYREF,
            PriceIsValid = priceFromDb?.ISVALID
          };
    
      var rightOuterJoin =
          from priceFromDb in pricesFromDBList
          join priceFromWeb in pricesFromWebList on priceFromDb.ECOMINVENTORYID equals priceFromWeb.InventoryId.ToString() into temp
          from priceFromWeb in temp.DefaultIfEmpty()
          select new
          {
            InventoryId = priceFromDb.ECOMINVENTORYID,
            BasicPriceWeb = priceFromWeb?.RetailPriceNet,
            BasicPriceDb = priceFromDb.BASICPRICE,
            CurrencyWeb = priceFromWeb?.Currency,
            CurrenyDb = priceFromDb.CURRENCY,  
            InventoryNameDb = priceFromDb.NAME,
            InventoryRef =  priceFromDb.ECOMINVENTORYREF,   
            PriceIsValid = priceFromDb.ISVALID 
          };
      
      var fullOuterJoin = leftOuterJoin.Union(rightOuterJoin);
    
      foreach(var f in fullOuterJoin)
      {
        var x = new TModel();
        x.ECOMINVENTORYID = f?.InventoryId.ToString();
        x.BASICPRICEDB = (f?.BasicPriceDb == null ? "Brak danych": f.BasicPriceDb.AsString);
        x.BASICPRICEWEB = (f?.BasicPriceWeb == null ? "Brak danych": f.BasicPriceWeb.ToString());
        x.CURRENCYDB = (f?.CurrenyDb == null ? "Brak danych": f.CurrenyDb.ToString());
        x.CURRENCYWEB = (f?.CurrencyWeb == null ? "Brak danych": f.CurrencyWeb.ToString()); 
        x.NAME = (f?.InventoryNameDb == null ? "Brak danych": f.InventoryNameDb.AsString);
        x.ECOMINVENTORYREF = (f?.InventoryRef == null ? "Brak danych": f.InventoryRef.AsString);
        x.PRICEISVALID = (f.PriceIsValid == null ? "Brak danych": f.PriceIsValid.AsString);
        x.ERRORMESSAGE = ValidateData(x);
        yield return x;     
      }
    
    }
    
    


    /// <param name="ecomChannelRef"></param>
    /// <param name="emailAddresses"></param>
    [UUID("1c78e5e8936d4ea6a3744e710f01e5c2")]
    public static string SendPriceVerificationReport(int ecomChannelRef, string emailAddresses)
    {
      string emailTitle = "Teneum/EKS - zgodność cen - "; 
      var p = new ECOMCHECKPRICESParameters();
      p._channelref = ecomChannelRef;
      var checkprices = new LOGIC.ECOMCHECKPRICES().WithParameters(p);
      var ecomChannel = new ECOM.ECOMCHANNELS();
      ecomChannel.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannel.REF.Symbol} = 0{ecomChannelRef}");
      if(!ecomChannel.FirstRecord())
      {
        return $"Nie znaleziono kanłu sprzedaży o podanym REF: {ecomChannelRef}.";
      }
      string channelName =  ecomChannel.NAME;
      ecomChannel.Close();
    /*
      DateTime teraz = DateTime.Now;
      if ((teraz.DayOfWeek == DayOfWeek.Saturday || teraz.DayOfWeek == DayOfWeek.Sunday)  //w weekend
        || (teraz.TimeOfDay < new TimeSpan(7, 0, 0) || teraz.TimeOfDay > new TimeSpan(18, 0, 0))) //od 8 do 18
        {
    	  	return "Wysyłanie nieaktywne poza godzinami pracy.";
        }
    */  
        StringBuilder sb = new StringBuilder();
        sb.Append(@"<html>");
        sb.Append(@"<body style='font-family:sans-serif;'>");
        sb.Append(string.Format(@"<h2>Ceny towarów z {0} </h2>", channelName));
        sb.Append(string.Format(@"<h4 style='color:#444'>(<u style='color:#ff5c49'>{0}</u>)</h4>",DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
        sb.Append(@"<h4 style='color:#444'>Poniżej znajduje się lista towarów oraz komunikaty błędów:</h4>");
        sb.Append(@"<table style='width:100%;border: 1px dashed #aaa;'>");
        sb.Append(@"<tr>");
        sb.Append(@"<th style='width:auto;padding:4px;background-color:#ff5c49'><strong>ID towaru</strong></th>");
        sb.Append(@"<th style='width:auto;padding:4px;background-color:#ff5c49'><strong>Nazwa towaru</strong></th>");
        sb.Append(@"<th style='width:auto;padding:4px;background-color:#ff5c49'><strong>Wartość brutto w Teneum</strong></th>");
        sb.Append(@"<th style='width:auto;padding:4px;background-color:#ff5c49'><strong>Wartość brutto w kanale sprzedaży</strong></th>");
        sb.Append(@"<th style='width:auto;padding:4px;background-color:#ff5c49'><strong>Waluta w Teneum</strong></th>");
        sb.Append(@"<th style='width:auto;padding:4px;background-color:#ff5c49'><strong>Waluta kanale sprzedaży</strong></th>");
        sb.Append(@"<th style='width:auto;padding:4px;background-color:#ff5c49'><strong>Komunikaty błędów</strong></th>");
        sb.Append(@"</tr>");
        
        int cnt = 0;
        bool isError = false;
        
      foreach(var item in checkprices.Get().OrderByDescending(x => x.ERRORMESSAGE))
      {  
        cnt++;
        
    		sb.Append(String.Format(@"<tr style='{0}'>", cnt % 2 == 0 ? "" : "background-color:#ddd;"));
        sb.Append(String.Format(@"<td style='width:auto;padding:4px;'><center>{0}</center></td>", item.ECOMINVENTORYID));
    		sb.Append(String.Format(@"<td style='width:auto;padding:4px;'><center>{0}</center></td>", item.NAME));
    		sb.Append(String.Format(@"<td style='width:auto;padding:4px;'><center>{0}</center></td>", item.BASICPRICEDB));
    		sb.Append(String.Format(@"<td style='width:auto;padding:4px;'><center>{0}</center></td>", item.BASICPRICEWEB)); 
    		sb.Append(String.Format(@"<td style='width:auto;padding:4px;'><center>{0}</center></td>", item.CURRENCYDB)); 
    		sb.Append(String.Format(@"<td style='width:auto;padding:4px;'><center>{0}</center></td>", item.CURRENCYWEB)); 
        sb.Append(String.Format(@"<td style='width:auto;padding:4px;'><center>{0}</center></td>", item.ERRORMESSAGE));
    		sb.Append(@"</tr>"); 
        
        if(!isError && !String.IsNullOrEmpty(item.ERRORMESSAGE))
        {
          isError = true;
        }
      }
    
    
      sb.Append(@"</table>");
      sb.Append(@"</body>");
      sb.Append(@"</html>");
      
      if(isError)
      {
        emailTitle += "ERROR"; 
      }
      else
      {
        emailTitle += "OK";
      }
       
      if(!String.IsNullOrEmpty(emailAddresses))
      {
        CORE.SendEmail(emailAddresses.Replace(';', ','), emailTitle, sb.ToString());
      }
      else
      {
        return $"Próba wysłania raportu dla kanału {channelName} zakończona niepowodzeniem.\n" + 
        "Nie wprowadzono adresów e-mail odbiorców raportu w konfiguracji kanału sprzedaży.\n";
      }
      return $"Próba wysłania raportu dla kanału {channelName} zakończona sukcesem.\n";
    }
    
    


    /// <param name="x"></param>
    [UUID("2c6488f0946441c0aa0f769229f0ef7b")]
    virtual public string ValidateData(TModel x)
    {
      string message = "";
      decimal grossValueDb;
      decimal grossValueWeb;
      if(Decimal.TryParse(x.BASICPRICEDB, out grossValueDb) && Decimal.TryParse(x.BASICPRICEWEB, out grossValueWeb))
      {
        if(grossValueDb != grossValueWeb)
        {
          message = "Towar wymaga synchronizacji.\n";
        }
      }
      else
      {
        message = "Brak danych o cenie towaru.\n";
      }
    
      return message;  
    }
    
    


    [UUID("9490a8331a9146c0ae0e608fe8c6cdbb")]
    public static string AutoSendPriceVerificationReport()
    {
      string sqlString = @"select ref, iif(coalesce(autopricereportlasttime,'') = '', '1900-01-01', autopricereportlasttime) reportlasttime,
                              coalesce(autopricereportinterval, 0) reportinterval
                            from ecomchannels
                            where active = 1 ";
      string message = "";
      string emailAddresses = ""; 
      var ecomChannel = new ECOM.ECOMCHANNELS(); 
      try
      {   
        foreach(var channel in CORE.QuerySQL(sqlString))
        {
          ecomChannel.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannel.REF.Symbol} = 0{channel["REF"].AsInteger}");
    
          if(!ecomChannel.FirstRecord())
          {
            message += $"Nie znaleziono kanału o podanym ref: {channel["REF"].AsString} \n";
            continue;
          }
    
          else
          {  
            int intervalSend = channel["REPORTINTERVAL"].AsInteger;
            DateTime nextSendDate = channel["REPORTLASTTIME"].AsDateTime;
            emailAddresses = ecomChannel.AUTOORDREPORTEMAILADDRESS;
            if(intervalSend > 0 && !String.IsNullOrEmpty(emailAddresses))
            { 
              if( DateTime.Now > nextSendDate.AddMinutes(intervalSend)) 
              {
                ecomChannel.EditRecord();
                ecomChannel.AUTOPRICEREPORTLASTTIME = DateTime.Now;
                if(!ecomChannel.PostRecord())
                {
                  throw new Exception($"Błąd aktualizacji czasu ostatniego wysłania raportu zamówień w kanale sprzedaży o REF: {ecomChannel.REF}");
                }	 
                message += SendPriceVerificationReport(channel["REF"].AsInteger, emailAddresses);
              }
              else
              {
                message += $"Kolejna próba wysłania raportu dla kanału {ecomChannel.NAME} obędzie się: {nextSendDate.AddMinutes(intervalSend)}. \n";
              }
            }
            if(intervalSend <= 0) 
            {
              message += $"Kanał {ecomChannel.NAME} nie ma ustawionego interwau wysyłania raportu. \n";
            }
            if(String.IsNullOrEmpty(emailAddresses))
            {
              message += $"Kanał {ecomChannel.NAME} nie ma uzupełnionych adresów e-mail odbiorców raportu.\n";
            }
          }
        }
      }
      catch(Exception ex)
      {
        message = $"Błąd wysłania raportu {ex} dla kanału {ecomChannel.NAME}. " + message;
      }
      ecomChannel.Close();
      return message;
    }
    
    
    


  }
}
