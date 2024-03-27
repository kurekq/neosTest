
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
  
  public partial class ECOMCHECKSTOCKS<TModel> where TModel : MODEL.ECOMCHECKSTOCKS, new()
  {

    /// <param name="context"></param>
    [UUID("Fill")]
    override protected IEnumerable<TModel> Fill(Contexts context)
    {
      var inventoryList = new List<String>(); 
      string sqlString = @"select invs.ref, invs.ecomchannelstockref, invs.quantity, invs.isvalid, inv.ecominventoryid inventoryid, w.nazwat, chs.ecomstockid
                              from ECOMINVENTORIES inv
                                left join ECOMINVENTORYSTOCKS invs on inv.ref = invs.ecominventoryref
                                left join ECOMCHANNELSTOCK chs on invs.ecomchannelstockref = chs.ref
                                left join WERSJE w on w.ref = inv.wersjaref " +   
                            $"where inv.ECOMCHANNELREF = {context["_channelref"]} "; 
    
      foreach(var item in CORE.QuerySQL(sqlString))
      {
        inventoryList.Add(item["INVENTORYID"].AsString);
      }
      
      var stocksFromWebList = LOGIC.ECOMCHANNELS.GetStocksList(context["_channelref"].AsInteger, inventoryList);
    
      var stocksFromDBList = CORE.QuerySQL(ECOM.ProjectInfo.DefaultDatabaseAlias, sqlString).Select(s => new {  
        ECOMINVENTORYID = s["INVENTORYID"].AsString,
        QUANTITY = s["QUANTITY"],
        NAME = s["NAZWAT"],
        ISVALID = s["ISVALID"],
        ECOMSTOCKID = s["ECOMSTOCKID"]
      }).ToList();  
        
      var leftOuterJoin =
          from stockFromWeb in stocksFromWebList
          join stockFromDb in stocksFromDBList on new {x1 = stockFromWeb.InventoryId.ToString(), x2 = stockFromWeb.ChannelStockId.ToString()} equals new {x1 = stockFromDb.ECOMINVENTORYID.ToString(), x2 = stockFromDb.ECOMSTOCKID.ToString()}  into temp
          from stockFromDb in temp.DefaultIfEmpty()
          select new
          {
            InventoryId = stockFromWeb.InventoryId,
            InventoryNameDb = stockFromDb?.NAME,
            QuantityWeb = stockFromWeb.Quantity,
            QuantityDb = stockFromDb?.QUANTITY,
            EcomStockIdWeb = stockFromWeb.ChannelStockId,
            EcomStockIdDb = stockFromDb?.ECOMSTOCKID,
            StockIsValid = stockFromDb?.ISVALID
            
          };
    
      var rightOuterJoin =
          from stockFromDb in stocksFromDBList
          join stockFromWeb in stocksFromWebList on new {x1 = stockFromDb.ECOMINVENTORYID.ToString(), x2 = stockFromDb.ECOMSTOCKID.ToString()} equals new {x1 = stockFromWeb.InventoryId.ToString(), x2 = stockFromWeb.ChannelStockId.ToString()} into temp
          from stockFromWeb in temp.DefaultIfEmpty()
          select new
          {
            InventoryId = stockFromDb.ECOMINVENTORYID.ToString(),
            InventoryNameDb = stockFromDb.NAME,
            QuantityWeb = stockFromWeb?.Quantity,
            QuantityDb = stockFromDb.QUANTITY, 
            EcomStockIdWeb = stockFromWeb?.ChannelStockId,
            EcomStockIdDb = stockFromDb.ECOMSTOCKID,          
            StockIsValid = stockFromDb.ISVALID
          };
      
    
      var fullOuterJoin = leftOuterJoin.Union(rightOuterJoin);
      int refKey = 0;
      foreach(var f in fullOuterJoin)
      {
        if (f.EcomStockIdDb != null)
        {
          refKey += 1;
          var x = new TModel();
          x.REF = refKey.ToString();
          x.ECOMINVENTORYID = f?.InventoryId.ToString();
          x.QUANTITYDB = (f?.QuantityDb == null ? "Brak danych": f.QuantityDb.AsString);
          x.QUANTITYWEB = (f?.QuantityWeb == null ? "Brak danych": f.QuantityWeb.ToString()); 
          x.NAME = (f?.InventoryNameDb == null ? "Brak danych": f.InventoryNameDb.AsString);
          x.STOCKISVALID = (f?.StockIsValid == null ? "Brak danych": f.StockIsValid.AsString);
          x.ECOMSTOCKIDWEB = (f?.EcomStockIdWeb == null ? "Brak danych": f.EcomStockIdWeb.ToString());
          x.ERRORMESSAGE = ValidateData(x);
          yield return x;  
        }
           
      }
    }
    
    


    [UUID("112bdfbcd96e4eada5ff3862202bb03e")]
    public static string AutoSendStockVerificationReport()
    {
      string sqlString = @"select ref, iif(coalesce(autostocksreportlasttime,'') = '', '1900-01-01', autostocksreportlasttime) reportlasttime,
                              coalesce(autostocksreportinterval, 0) reportinterval
                            from ecomchannels
                            where active = 1 ";
      string message = "";
      string emailAddresses = ""; 
      var ecomChannel = new ECOM.ECOMCHANNELS(); 
      try
      {   
        foreach(var channel in CORE.QuerySQL(sqlString))
        {
          ecomChannel.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannel.REF.Symbol} = 0{channel["REF"].AsString}");
    
          if(!ecomChannel.FirstRecord())
          {
            message += $"Nie znaleziono kanału o podanym REF: {channel["REF"].AsString} \n";
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
                ecomChannel.AUTOSTOCKSREPORTLASTTIME = DateTime.Now;
                if(!ecomChannel.PostRecord())
                {
                  throw new Exception($"Błąd aktualizacji czasu ostatniego wysłania raportu stanów mag. w kanale sprzedaży o REF: {ecomChannel.REF}");
                }	 
                message += SendStockVerificationReport(channel["REF"].AsInteger, emailAddresses);
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
    
    
    


    /// <param name="x"></param>
    [UUID("621dc1e5982d40be9291f7b87f260a5d")]
    virtual public string ValidateData(TModel x)
    {
      string message = "";
      decimal quantityDb;
      decimal quantityeWeb;
      if(x.QUANTITYWEB == "Brak danych" && x.QUANTITYDB == "0")
      {
        return "";
      }
      if(Decimal.TryParse(x.QUANTITYDB, out quantityDb) && Decimal.TryParse(x.QUANTITYWEB, out quantityeWeb))
      {
        if(quantityDb != quantityeWeb)
        {
          message = "Stan magazynowy wymaga synchronizacji.\n";
        }
      }
      else
      {
        message = "Brak danych o stanie magazynowym.\n";
      }
    
      return message;  
    }
    
    
    


    /// <param name="ecomChannelRef"></param>
    /// <param name="emailAddresses"></param>
    [UUID("f6d954bbe86b46b1ba7a8fd9d630dbed")]
    public static string SendStockVerificationReport(int ecomChannelRef, string emailAddresses)
    {
      string emailTitle = "Teneum/EKS - zgodność stanów magazynowych - "; 
      var p = new ECOMCHECKSTOCKSParameters();
      p._channelref = ecomChannelRef.ToString();
      var checkstocks = new LOGIC.ECOMCHECKSTOCKS().WithParameters(p);
      var ecomChannel = new ECOM.ECOMCHANNELS();
      ecomChannel.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannel.REF.Symbol} = 0{ecomChannelRef}");
      if(!ecomChannel.FirstRecord())
      {
        return $"Nie znaleziono kanłu sprzedaży o podanym ref: {ecomChannelRef.ToString()}.";
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
        sb.Append(string.Format(@"<h2>Stany magazynowe w {0} </h2>", channelName));
        sb.Append(string.Format(@"<h4 style='color:#444'>(<u style='color:#ff5c49'>{0}</u>)</h4>",DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
        sb.Append(@"<h4 style='color:#444'>Poniżej znajduje się lista stanów magazynowych towarów oraz komunikaty błędów:</h4>");
        sb.Append(@"<table style='width:100%;border: 1px dashed #aaa;'>");
        sb.Append(@"<tr>");
        sb.Append(@"<th style='width:auto;padding:4px;background-color:#ff5c49'><strong>ID towaru</strong></th>");
        sb.Append(@"<th style='width:auto;padding:4px;background-color:#ff5c49'><strong>Nazwa towaru</strong></th>");
        sb.Append(@"<th style='width:auto;padding:4px;background-color:#ff5c49'><strong>Stan magazynowy w Teneum</strong></th>");
        sb.Append(@"<th style='width:auto;padding:4px;background-color:#ff5c49'><strong>Stan magazynowy w kanale sprzedaży</strong></th>");
        sb.Append(@"<th style='width:auto;padding:4px;background-color:#ff5c49'><strong>Komunikaty błędów</strong></th>");
        sb.Append(@"</tr>");
        
        int cnt = 0;
        bool isError = false;
        
      foreach(var item in checkstocks.Get().OrderByDescending(x => x.ERRORMESSAGE))
      {  
        cnt++;
        
    		sb.Append(String.Format(@"<tr style='{0}'>", cnt % 2 == 0 ? "" : "background-color:#ddd;"));
        sb.Append(String.Format(@"<td style='width:auto;padding:4px;'><center>{0}</center></td>", item.ECOMINVENTORYID));
    		sb.Append(String.Format(@"<td style='width:auto;padding:4px;'><center>{0}</center></td>", item.NAME));
    		sb.Append(String.Format(@"<td style='width:auto;padding:4px;'><center>{0}</center></td>", item.QUANTITYDB));
    		sb.Append(String.Format(@"<td style='width:auto;padding:4px;'><center>{0}</center></td>", item.QUANTITYWEB)); 
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
    
    


  }
}
