
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
  
  public partial class ECOMCHECKORDERS<TModel> where TModel : MODEL.ECOMCHECKORDERS, new()
  {

    /// <summary>
    /// Metoda wykorzystywana jest przy wersyfikacji spójności danych. 
    /// Pobiera dane z bazy oraz wywołuje metodę GetOrdersList, która zwraca listę zamówień z kanału sprzedaży.
    /// Metoda robi  leftOuterJoin (dane otrzymane z kanału są łączone z danymi z bazy) następnie rightOuterJoin (dane z bazy są łączone z danymi z kanału sprzedaży), a na końcu fullOuterJoin (łączenie otrzymanych danych) i uzupełnienie pól modelu danych o otrzymane dane.
    /// 
    /// Operator może porównać dane z kanału sprzedaży z danymi z bazy czy się zgadzają.  
    /// 
    /// </summary>
    /// <param name="context"></param>
    [UUID("Fill")]
    override protected IEnumerable<TModel> Fill(Contexts context)
    {
      if (!string.IsNullOrEmpty(context["_fromdate"].AsString) && context["_fromdate"].AsDateTime != default)
      {  
        string sqlString = @"select ord.ECOMCHANNELREF, ord.ECOMORDERID, ord.ECOMCHANNELSTATE, ord.LASTCHANGETMSTMP, 
                                ord.ECOMORDERSYMBOL, kl.NAZWA, nag.SUMWARTNET, nag.SUMWARTBRU, nag.WALUTA
                              from ECOMORDERS ord 
                              join nagzam nag on nag.ref = ord.nagzamref
                              join klienci kl on kl.ref = nag.klient " +   
                              $"where ord.ECOMCHANNELREF = {context["_channelref"]} and ord.ECOMORDERDATE between '{context["_fromdate"].AsDateTime}' and '{context["_todate"].AsDateTime}'";  
        
        var ordersFromWebList = LOGIC.ECOMCHANNELS.GetOrdersList(context["_channelref"].AsInteger, 
          importMode.DateRange, context["_fromdate"].AsDateTime, context["_todate"].AsDateTime);  
      
        var ordersFromDBList = CORE.QuerySQL(ECOM.ProjectInfo.DefaultDatabaseAlias, sqlString).Select(s => new {  
          ECOMCHANNELREF = s["ECOMCHANNELREF"].AsInteger,
          ECOMORDERID = s["ECOMORDERID"].AsString,
          ECOMCHANNELSTATE = s["ECOMCHANNELSTATE"],     
          ECOMORDERSYMBOL = s["ECOMORDERSYMBOL"],
          CLIENTNAME = s["NAZWA"],
          ORDERNETPRICE = s["SUMWARTNET"],
          ORDERGROSSPRICE = s["SUMWARTBRU"],
          CURRENCY = s["WALUTA"],
          LASTCHANGE = s["LASTCHANGETMSTMP"]
    
        }).ToList();
        
        var leftOuterJoin =
            from orderFromWeb in ordersFromWebList
            join orderFromDb in ordersFromDBList on orderFromWeb.OrderId.ToString() equals orderFromDb.ECOMORDERID into temp
            from orderFromDb in temp.DefaultIfEmpty()
            select new
            {
              OrderID = orderFromWeb.OrderId,
              OrderStatusWeb = orderFromWeb.OrderStatus.OrderStatusId,
              OrderStatusDb = orderFromDb?.ECOMCHANNELSTATE,
              OrderSymbolWeb = orderFromWeb.OrderSymbol,
              OrderSymbolDb = orderFromDb?.ECOMORDERSYMBOL,  
              ClientNameDb = orderFromDb?.CLIENTNAME,    
              OrderNetPriceDb = orderFromDb?.ORDERNETPRICE,
              OrderGrossPriceWeb = orderFromWeb.Payment.OrderValue, 
              OrderGrossPriceDb = orderFromDb?.ORDERGROSSPRICE,
              CurrencyWeb = orderFromWeb.Payment.Currency, 
              CurrencyDb = orderFromDb?.CURRENCY,
              LastChangeDb = orderFromDb?.LASTCHANGE
            };
    
        var rightOuterJoin =
            from orderFromDb in ordersFromDBList
            join orderFromWeb in ordersFromWebList on orderFromDb.ECOMORDERID equals orderFromWeb.OrderId.ToString() into temp
            from orderFromWeb in temp.DefaultIfEmpty()
            select new
            {
              OrderID = orderFromDb.ECOMORDERID.ToString(),
              OrderStatusWeb = orderFromWeb?.OrderStatus.OrderStatusId,
              OrderStatusDb = orderFromDb.ECOMCHANNELSTATE,
              OrderSymbolWeb = orderFromWeb?.OrderSymbol,
              OrderSymbolDb = orderFromDb.ECOMORDERSYMBOL,
              ClientNameDb = orderFromDb.CLIENTNAME,
              OrderNetPriceDb = orderFromDb.ORDERNETPRICE,
              OrderGrossPriceWeb = orderFromWeb?.Payment?.OrderValue,
              OrderGrossPriceDb = orderFromDb.ORDERGROSSPRICE,
              CurrencyWeb = orderFromWeb?.Payment.Currency, 
              CurrencyDb = orderFromDb.CURRENCY,
              LastChangeDb = orderFromDb.LASTCHANGE
              
            };
    
      
    
        var fullOuterJoin = leftOuterJoin.Union(rightOuterJoin);
        var orderStatus = new ECOM.ECOMCHANNELSTATES(); 
        foreach(var f in fullOuterJoin)
        {
          var x = new TModel();
          x.ECOMORDERID = f?.OrderID.ToString();
          if(f?.OrderStatusWeb == null)
          {
            x.ORDERSTATUSWEB = "Brak danych";
          }
          else
          {
            orderStatus.FilterAndSort($"{nameof(ECOM.ECOMCHANNELSTATES)}.{orderStatus.SYMBOL.Symbol} = '{f.OrderStatusWeb}'");
            if(orderStatus.FirstRecord())
            { 
              x.ORDERSTATUSWEB = (orderStatus.DESC.ToString() == "" ? "Brak danych": orderStatus.DESC.ToString()); 
            }
            else
            {
              x.ORDERSTATUSWEB = $"Nie odnaleziono statusu o symbolu: {f?.OrderStatusWeb} w bazie danych.";
            }
          }
          
          if(f?.OrderStatusDb == null)
          {
            x.ORDERSTATUSDB = "Brak danych";
          }
          else
          {
            // uzupełnienie statusu o opis, a nie o ref
            orderStatus.FilterAndSort($"{nameof(ECOM.ECOMCHANNELSTATES)}.{orderStatus.REF.Symbol} = 0{f?.OrderStatusDb}");
            if(orderStatus.FirstRecord())
            { 
              x.ORDERSTATUSDB = (orderStatus.DESC.ToString() == "" ? "Brak danych": orderStatus.DESC.ToString()); 
            }
            else
            {
              x.ORDERSTATUSDB = $"Nie odnaleziono statusu o REF: {f?.OrderStatusDb} w bazie danych.";
            }
          }
          
          x.ORDERSYMBOLWEB = (f?.OrderSymbolWeb == null ? "Brak danych": f.OrderSymbolWeb); 
          x.ORDERSYMBOLDB = (f?.OrderSymbolDb == null ? "Brak danych": f.OrderSymbolDb.AsString);  
          x.CLIENTNAMEDB = (f?.ClientNameDb == null ? "Brak danych": f.ClientNameDb.AsString);  
          x.ORDERGROSSPRICEWEB = (f?.OrderGrossPriceWeb == null ? "Brak danych": f.OrderGrossPriceWeb.ToString()); 
          x.ORDERNETPRICEDB = (f?.OrderNetPriceDb == null ? "Brak danych": f.OrderNetPriceDb.AsString); 
          x.ORDERGROSSPRICEDB = (f?.OrderGrossPriceDb == null ? "Brak danych": f.OrderGrossPriceDb.AsString); 
          x.CURRENCYWEB = (f?.CurrencyWeb == null ? "Brak danych": f.CurrencyWeb); 
          x.CURRENCYDB = (f?.CurrencyDb == null ? "Brak danych": f.CurrencyDb.AsString); 
          x.LASTCHANGETMSTMPDB = (f?.LastChangeDb == null ? "Brak danych": f.LastChangeDb.AsString); 
          x.ERRORMESSAGES = ValidateData(x);  
          yield return x;   
        } 
        orderStatus.Close();
      } 
    }
    
    


    /// <param name="ecomChannelRef"></param>
    /// <param name="fromDate"></param>
    /// <param name="toDate"></param>
    /// <param name="emailAddresses"></param>
    [UUID("0c8da4fa43c34d188ed4b62bbc47545c")]
    public static string SendOrdersVerificationReport(int ecomChannelRef, DateTime fromDate, DateTime toDate, string emailAddresses)
    {
      string emailTitle = "Teneum/EKS - zgodność zamówień - "; 
      var p = new ECOMCHECKORDERSParameters();
      p._channelref = ecomChannelRef;
      p._fromdate = fromDate;
      p._todate = toDate;
      var checkorders = new LOGIC.ECOMCHECKORDERS().WithParameters(p);
      
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
        sb.Append(string.Format(@"<h2>Zamówienia z {0} </h2>", channelName));
        sb.Append(string.Format(@"<h4 style='color:#444'>(<u style='color:#ff5c49'>{0}</u>)</h4>",DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
        sb.Append(@"<h4 style='color:#444'>Poniżej znajduje się lista zamówień oraz komunikaty błędów:</h4>");
        sb.Append(@"<table style='width:100%;border: 1px dashed #aaa;'>");
        sb.Append(@"<tr>");
        sb.Append(@"<th style='width:auto;padding:4px;background-color:#ff5c49'><strong>ID zamówienia</strong></th>");
        sb.Append(@"<th style='width:auto;padding:4px;background-color:#ff5c49'><strong>Symbol zamówienia</strong></th>");
        sb.Append(@"<th style='width:auto;padding:4px;background-color:#ff5c49'><strong>Data ostatniej zmiany</strong></th>");
        sb.Append(@"<th style='width:auto;padding:4px;background-color:#ff5c49'><strong>Wartość brutto w Teneum</strong></th>");
        sb.Append(@"<th style='width:auto;padding:4px;background-color:#ff5c49'><strong>Wartość brutto w kanale sprzedaży</strong></th>");
        sb.Append(@"<th style='width:auto;padding:4px;background-color:#ff5c49'><strong>Waluta w Teneum</strong></th>");
        sb.Append(@"<th style='width:auto;padding:4px;background-color:#ff5c49'><strong>Waluta kanale sprzedaży</strong></th>");
        sb.Append(@"<th style='width:auto;padding:4px;background-color:#ff5c49'><strong>Status w Teneum</strong></th>");
        sb.Append(@"<th style='width:auto;padding:4px;background-color:#ff5c49'><strong>Status kanale sprzedaży</strong></th>");
        sb.Append(@"<th style='width:auto;padding:4px;background-color:#ff5c49'><strong>Komunikaty błędów</strong></th>");
        sb.Append(@"</tr>");
        
        int cnt = 0;
        bool isError = false;
        
      foreach(var item in checkorders.Get().OrderByDescending(x => x.ERRORMESSAGES))
      {
        
        cnt++;
    		sb.Append(String.Format(@"<tr style='{0}'>", cnt % 2 == 0 ? "" : "background-color:#ddd;"));
    		sb.Append(String.Format(@"<td style='width:auto;padding:4px;'><center>{0}</center></td>", item.ECOMORDERID));
    		sb.Append(String.Format(@"<td style='width:auto;padding:4px;'><center>{0}</center></td>", item.ORDERSYMBOLWEB));
    		sb.Append(String.Format(@"<td style='width:auto;padding:4px;'><center>{0}</center></td>", item.LASTCHANGETMSTMPDB)); 
    		sb.Append(String.Format(@"<td style='width:auto;padding:4px;'><center>{0}</center></td>", item.ORDERGROSSPRICEDB)); 
    		sb.Append(String.Format(@"<td style='width:auto;padding:4px;'><center>{0}</center></td>", item.ORDERGROSSPRICEWEB));
        sb.Append(String.Format(@"<td style='width:auto;padding:4px;'><center>{0}</center></td>", item.CURRENCYDB)); 
        sb.Append(String.Format(@"<td style='width:auto;padding:4px;'><center>{0}</center></td>", item.CURRENCYWEB)); 
        sb.Append(String.Format(@"<td style='width:auto;padding:4px;'><center>{0}</center></td>", item.ORDERSTATUSDB)); 
    		sb.Append(String.Format(@"<td style='width:auto;padding:4px;'><center>{0}</center></td>", item.ORDERSTATUSWEB));  
    		sb.Append(String.Format(@"<td style='width:auto;padding:4px;'><center>{0}</center></td>", item.ERRORMESSAGES));
    		sb.Append(@"</tr>"); 
        if(!isError && !String.IsNullOrEmpty(item.ERRORMESSAGES))
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
    [UUID("24b44e2cb1054fb888eceab314990783")]
    virtual public string ValidateData(TModel x)
    {
      string message = "";
      decimal grossValueDb = 0;
      decimal grossValueWeb = 0;
      if(Decimal.TryParse(x.ORDERGROSSPRICEDB, out grossValueDb) && Decimal.TryParse(x.ORDERGROSSPRICEWEB, out grossValueWeb))
      {
        if(grossValueDb != grossValueWeb)
        {
          message = "Wartość zamówienia różni się w bazie i na stronie.\n";
        }
      }
      else
      {
        message = "Brak danych o wartości zamówienia.\n";
      }
    
      if(x.ORDERSYMBOLDB != x.ORDERSYMBOLWEB)
      {
        message = message + "Symbol zamówienia różni się w bazie i na stronie.\n";
      }
    
      if(x.CURRENCYDB != x.CURRENCYWEB)
      {
        message += "Waluta zamówienia różni się w bazie i na stronie.\n";
      }
    
      if(x.ORDERSTATUSDB != x.ORDERSTATUSWEB)
      {
        message += "Status zamówienia różni się w bazie i na stronie.\n";
      }
      
      return message;  
      
    }
    
    
    


    [UUID("9f48be74023c42609564e24667f1a3c9")]
    public static string AutoSendOrdersVerificationReport()
    {
      string sqlString = @"select ref, iif(coalesce(autoordreportlasttime,'') = '', '1900-01-01', autoordreportlasttime) reportlasttime,
                              coalesce(autoordreportinterval, 0) reportinterval, coalesce(autoordreportnoofdays, 0) reportnoofdays 
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
            message += $"Nie znaleziono kanału o podanym ref: {channel["REF"].AsString} \n";
            continue;
          }
          else
          {  
            int intervalSend = channel["REPORTINTERVAL"].AsInteger;
            DateTime nextSendDate = channel["REPORTLASTTIME"].AsDateTime;
            int numberOfDays = channel["REPORTNOOFDAYS"].AsInteger;
            emailAddresses = ecomChannel.AUTOORDREPORTEMAILADDRESS;
            if(intervalSend > 0 && numberOfDays > 0 && !String.IsNullOrEmpty(emailAddresses))
            { 
              if( DateTime.Now > nextSendDate.AddMinutes(intervalSend)) 
              {
                DateTime fromDate = DateTime.Now.AddDays(-numberOfDays); // Data dzisiejsza minus ilość dni, które definiujemy w ECOMCHANNELS
                ecomChannel.EditRecord();
                ecomChannel.AUTOORDREPORTLASTTIME = DateTime.Now;
                if(!ecomChannel.PostRecord())
                {
                  throw new Exception($"Błąd aktualizacji czasu ostatniego wysłania raportu zamówień w kanale sprzedaży o REF: {ecomChannel.REF}");
                }	 
                message += SendOrdersVerificationReport(channel["REF"].AsInteger, fromDate, DateTime.Now, emailAddresses);
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
            if(numberOfDays <= 0) 
            {
              message += $"Kanał {ecomChannel.NAME} nie ma ustawionej ilości dni wstecz do sprawdzenia spójności zamówień. \n";
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
