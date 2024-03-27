
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
  
  public partial class ECOMCHANNELS<TModel> where TModel : MODEL.ECOMCHANNELS, new()
  {

    /// <param name="adapter"></param>
    /// <param name="connectorRef"></param>
    /// <param name="ecomChannelRef"></param>
    /// <param name="exportMode"></param>
    [UUID("01df7e1b46fc406bb477509d95e4fb45")]
    public static string SendInventoryStocksForChannels(string adapter, int? connectorRef, int? ecomChannelRef, ExportMode exportMode)
    {
      RequestMode expReqMode;
      //składanie zapytania do bazy danych, które pobierze listę kanałów sprzedaży, w których trzeba wysłać towary  
      string message = "";
      string sqlString = 
        @"select cha.ref channel,
            iif(coalesce(cha.autosendstockslasttime, '') = '', '1900-01-01', cha.autosendstockslasttime) sendlasttime,
            coalesce(cha.autosendstocksinterval, 0) sendinterval
          from ecomchannels cha 
            join sys_edaconnectors con on con.ref = cha.connector 
          where cha.active = 1 ";        
      if(!string.IsNullOrEmpty(adapter)) 
      {
        expReqMode = RequestMode.Adapter;
        sqlString += $"and con.adapter = '{adapter}' "; 
      }
      else if(connectorRef != null)
      {
        expReqMode = RequestMode.Connector;
        sqlString += $"and con.ref = 0{connectorRef} ";
      }
      else if(ecomChannelRef != null)
      {
        expReqMode = RequestMode.Channel;
        sqlString += $"and echa.ref = 0{ecomChannelRef} ";
      }
    
      try 
      {
        foreach(var dataRow in CORE.QuerySQL(sqlString))
        {
          if(dataRow["SENDINTERVAL"].AsInteger > 0)
          {
            DateTime nextSendDate = dataRow["SENDLASTTIME"].AsDateTime;
            nextSendDate = nextSendDate.AddMinutes(dataRow["SENDINTERVAL"].AsInteger);
            if(nextSendDate < DateTime.Now)            
            { 
              //uruchamiamy standardową metodę generującą komendy eksportu dla wybranego kanału
              message += SendInventoryStocks(dataRow["CHANNEL"].AsInteger, exportMode) + "\n";      
            }
          }
          else
          {
            message += $"Nie wygenerowano komendy aktualizacji stanów magazynowych dla kanału: {dataRow["CHANNEL"]}\n Kanał nie ma ustawionego interwału aktualizacji stanów magazynowych.\n";
          }    
        }
      }
      catch(Exception ex)
      {        
        throw new Exception($"Błąd komendy aktualizacji stanów magazynowych : {ex.Message}");   
      }  
    
      if(String.IsNullOrEmpty(message))
      {
        message = "Nie znaleziono aktywnych kanałów sprzedaży.";
      }
      return message; 
    }
    
    
    


    /// <summary>
    /// Metoda jest częścią mechanizmu naliczania statystyk kanały sprzedaży. Uruchamiana przez metodę interfejsu CalculateChannelStats
    /// nalicza wymagane statystyki na podstawie danych z bazy
    /// 
    /// </summary>
    [UUID("03b38bb83f1049548bc2d9bb64e4d055")]
    public static List<StatDataRow> GetChannelsStats()
    {
        //select wylicza statystyki (ile nowych z dnia i z miesiąca, ile błędnych) 
        //dla każdego kanału sprzedaży
        string sqlString = 
            @"select CHANNELREF, LASTSYNCH, LASTORD, LASTORDCHANGE, MONTHLYORDCOUNT, DAILYORDCOUNT, ORDSERRORCOUNT, INVENTERRORCOUNT,
                LASTPRICESSYNC, PRICESERRORCOUNT, LASTSTOCKSYNCH
                from GET_ECOMCHANNELS_STATS";
      
        var result = new List<StatDataRow>();
        
        foreach(var dataRow in CORE.QuerySQL(sqlString))
        {  
           var resultRow = new StatDataRow(); 
           resultRow.ChannelRef = dataRow["CHANNELREF"].AsInteger;
           resultRow.DailyStat = dataRow["DAILYORDCOUNT"].AsInteger;
           resultRow.MonthlyStat = dataRow["MONTHLYORDCOUNT"].AsInteger;
           resultRow.OrdersIntervation = dataRow["ORDSERRORCOUNT"].AsInteger;
           resultRow.LastOrderTimestmp = dataRow["LASTORD"];
           resultRow.LastOrderSync = dataRow["LASTSYNCH"];
           resultRow.InventoryWithError = dataRow["INVENTERRORCOUNT"].AsInteger;
           resultRow.LastOrderChangeTimestmp = dataRow["LASTORDCHANGE"];
           resultRow.LastPricesUpdate = dataRow["LASTPRICESSYNC"];
           resultRow.PricesWithError = dataRow["PRICESERRORCOUNT"].AsInteger;
           resultRow.LastStocksUpdate = dataRow["LASTSTOCKSYNCH"];
    
           result.Add(resultRow); 
           
        }  
        return result;  
    }
    
    


    /// <param name="adapter"></param>
    /// <param name="connectorRef"></param>
    /// <param name="ecomChannelRef"></param>
    /// <param name="importMode"></param>
    /// <param name="orderList"></param>
    /// <param name="dateBegin"></param>
    /// <param name="dateEnd"></param>
    [UUID("325c13f2f91243c6b7b84ee868415e71")]
    public static string GetOrdersForChannels(string adapter, int? connectorRef, int? ecomChannelRef, 
      importMode importMode, List<int> orderList = null, DateTime? dateBegin = null, DateTime? dateEnd = null)
    {
      string message = "";
      DateTime nextGetOrders;
      RequestMode impReqMode;
    
      string sqlString =
          @"select distinct ch.ref channel, con.ref connector, iif(coalesce(ch.autogetordlasttime,'') = '', '1900-01-01', ch.autogetordlasttime) autogetordlasttime,
              iif(coalesce(ch.lastorderchangetmstmp,'') = '', '1900-01-01', ch.lastorderchangetmstmp) lastorderchangetmstmp,
              coalesce(ch.importorderblock, 1) importblock, coalesce(ch.autogetordinterval, 0) getordinterval
            from ecomchannels ch
              join sys_edaconnectors con on con.ref = ch.connector 
              where ch.active = 1  ";
      
      if(!string.IsNullOrEmpty(adapter)) 
      {
        impReqMode = RequestMode.Adapter;
        //pobieramy listę kanałów sprzedaży powiązanych z danym adapterem na zasadzie adapter -> konektor -> kanał
        // oraz listę potrzebnych do importu zamówień parametrów (grupę i symbol konektora, interwał, czy konektor nie ma blokady pobiernia)
        
        sqlString = sqlString + $"and con.adapter = '{adapter}'";    
      }
      else if(connectorRef != null)
      {
        impReqMode = RequestMode.Connector;
        //pobieramy listę kanałów sprzedaży powiązanych z danym konektorem
        // oraz listę potrzebnych do importu zamówień parametrów (grupę i symbol konektora, interwał, czy konektor nie ma blokady pobiernia)    
        sqlString = sqlString + $"and con.ref = 0{connectorRef}";
      }
      else if(ecomChannelRef != null)
      {
        impReqMode = RequestMode.Channel;
        //pobieramy listę potrzebnych do importu zamówień parametrów (grupę i symbol konektora, interwał, czy konektor nie ma blokady pobiernia)
        //dla dengo kanału sprzedaży    
        sqlString = sqlString + $"and ch.ref = 0{ecomChannelRef}";
      }
      
      var ecomChannel = new ECOM.ECOMCHANNELS();    
      //generujemy komendy dla kazdego kanału sprzedaży
      foreach(var dataRow in CORE.QuerySQL(sqlString))
      { 
        if (dataRow["GETORDINTERVAL"].AsInteger == 0)
        {
          message += $"Nie pobrano zamówień z kanału sprzedaży {dataRow["CHANNEL"]}.\nKanał nie ma ustawionego interwału pobierania zamówień.\n";
        }
        if(dataRow["IMPORTBLOCK"].AsInteger == 1)
        {
          message += $"Pobieranie zamówień dla kanału sprzedaży {dataRow["CHANNEL"]} jest zablokowane.\n";
        }
        if(dataRow["IMPORTBLOCK"].AsInteger == 0 && dataRow["GETORDINTERVAL"].AsInteger > 0)
        {
          nextGetOrders = dataRow["AUTOGETORDLASTTIME"].AsDateTime;
          nextGetOrders = nextGetOrders.AddMinutes(dataRow["GETORDINTERVAL"].AsInteger);
         
          if(nextGetOrders < DateTime.Now)
          {
            try
            {
              message += LOGIC.ECOMCHANNELS.GetOrders(dataRow["CHANNEL"].AsInteger, importMode,
                (importMode == importMode.SinceLastImport ? Convert.ToDateTime(dataRow["LASTORDERCHANGETMSTMP"]) : dateBegin),
                dateEnd, orderList) + "\n";
            }
            catch(Exception ex)
            {       
              message += $"Błąd genorowania komendy pobierania zamówień dla kanału: {dataRow["CHANNEL"]}: {ex.Message}\n";   
            }          
          }
          else 
          {
            message += $"Nie pobrano zamówień z kanału sprzedaży {dataRow["CHANNEL"]}.\nData kolejnego pobrania zamówień ustawiona jest na: {nextGetOrders} \n";
          } 
        }         
      }  
    
      if(String.IsNullOrEmpty(message))
      {
        message = "Nie znaleziono aktywnych kanałów sprzedaży.";
      }
      
      return message; 
    }
    
    
    


    /// <param name="ecomChannelRef"></param>
    [UUID("37eef05d2c75407aa37b3b6dbe92eb8f")]
    public static ECOM.BR_KLIENCI_CENY FindPriceMethodObject(int ecomChannelRef)
    {
      //Metoda zwracająca dla danego kanału sprzedaży odpowiednią klasę metod obsługi exportu/importu dla danego kanału sprzedaży.
      var channelParams = LOGIC.ECOMSTDMETHODS.LoadEcomChannelParams(ecomChannelRef);
      var ecomChannel = new ECOM.ECOMCHANNELS();
      ecomChannel.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannel.REF.Symbol} = 0{ecomChannelRef}");
      if(ecomChannel.FirstRecord())
      {
        if(string.IsNullOrEmpty(channelParams["_calcpricemethodsobj"].ToString()))
          return new ECOM.BR_KLIENCI_CENY();
        else
        {
          try
          {
            var orderSyncObject = CreateObject(channelParams["_calcpricemethodsobj"]);
            return (orderSyncObject as ECOM.BR_KLIENCI_CENY);
          }
          catch(Exception e)
          {
            throw new Exception($"Nie udało się powołać obiektu metod obliczania cen towarów {channelParams["_calcpricemethodsobj"]} dla kanału sprzedaży  {ecomChannelRef}");
          }
        }
      }
      else
      {
        throw new Exception($"Nie znaleziono kanału sprzedaży o REF: {ecomChannelRef}");
      }
      ecomChannel.Close();     
      return null;
    }
    
    


    /// <param name="ecomChannelRef"></param>
    /// <param name="inventoryList"></param>
    [UUID("3a9859a6838046098d89f184c0e1a592")]
    public static List<EcomInventoryStockInfo> GetStocksList(int ecomChannelRef, List<String> inventoryList)
    {
      string resultMsg = "";  
      var stockInfoList = new List<EcomInventoryStockInfo>();
      var ecomChannelData = new ECOM.ECOMCHANNELS();
      var connectorData = new SYSTEM.EDACONNECTORS();
      ecomChannelData.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannelData.REF.Symbol} = 0{ecomChannelRef}");
      if(!ecomChannelData.FirstRecord())
      {
        throw new Exception($"Nie znaleziono kanału sprzedaży o REF: {ecomChannelRef}");    
      }
      connectorData.FilterAndSort($"SYS_EDACONNECTORS.{connectorData.REF.Symbol} = 0{ecomChannelData.CONNECTOR}");
      if(!connectorData.FirstRecord())
      {
        throw new Exception($"Nie znaleziono konektora o REF: {ecomChannelData.CONNECTOR}");    
      }
      try
      {
        var command = new ECOM.GetStocksListCommand()     
        {      
          EcomChannel = ecomChannelData.REF.AsInteger,
          InventoryIdList = inventoryList 
        };    
    
        if(string.IsNullOrEmpty(connectorData.GROUPNAME)
          || string.IsNullOrEmpty(connectorData.SYMBOL))
        {
          throw new Exception("Brak pełniej nazwy konektora\n"); 
        }
        else
        {     
          var result = EDA.ExecuteCommand(connectorData.GROUPNAME + "." + connectorData.SYMBOL, command);      
          var handlerResult = result.HandlerResult;  
          stockInfoList = (List<EcomInventoryStockInfo>)result["Result"];       
        }
      }  
      catch(Exception ex)
      { 
        throw new Exception("Błąd komunikacji z bramką. " + ex.Message);      
      }  
      ecomChannelData.Close();
      connectorData.Close();
    
    
      return stockInfoList; 
    }
    
    


    /// <param name="channelRef"></param>
    /// <param name="OrderChangeDate"></param>
    [UUID("3b9741a137224a56a90f3b224022daf7")]
    public static void CalcLastOrderTimestap(int channelRef, DateTime? OrderChangeDate)
    {
      string errMsg = "";
      var channel = new ECOM.ECOMCHANNELS();
      
      try
      {
        channel.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{channel.REF.Symbol} = 0{channelRef} ");
        if(channel.FirstRecord())
        {
          channel.EditRecord();
          if(OrderChangeDate == null)
          {   
            var lastOrdChangeTime =  GetLastOrderChangeTimestamp(channelRef); 
            channel.LASTORDERCHANGETMSTMP  = lastOrdChangeTime;
          }else if(OrderChangeDate > channel.LASTORDERCHANGETMSTMP.AsDateTime)
          {
            channel.LASTORDERCHANGETMSTMP = OrderChangeDate ;   
          }
          channel.AUTOORDREPORTLASTTIME = null;
          if(!channel.PostRecord())
          {
            throw new Exception($"Błąd aktualizacji daty ostatniego pobrania zamówienia w kanale sprzedaży o REF: {channelRef}");
          }	 
        }
        else
        {
          throw new Exception($"Nie znaleziono kanału sprzedaży o REF: {channelRef}");
        }
        
      }
      catch(Exception ex)
      {
        throw new Exception($"Błąd aktualizacji daty ostatniego pobrania zmówienia: {channelRef.ToString()}: {ex.Message}\n");
      }  
      channel.Close();
    }
    
    
    
    


    /// <param name="ecomChannelRef"></param>
    /// <param name="orderList"></param>
    [UUID("3dc1834de737484887fdcf74ee20ae96")]
    public static string GetOrdersPayment(int ecomChannelRef, List<int> orderList)
    {
      string resultMsg = "";  
      var ecomChannelData = new ECOM.ECOMCHANNELS();
      var connectorData = new SYSTEM.EDACONNECTORS();
      ecomChannelData.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannelData.REF.Symbol} = 0{ecomChannelRef}");
      if(!ecomChannelData.FirstRecord())
      {
        throw new Exception($"Nie znaleziono kanału sprzedaży o REF: {ecomChannelRef}");    
      }
      connectorData.FilterAndSort($"SYS_EDACONNECTORS.{connectorData.REF.Symbol} = 0{ecomChannelData.CONNECTOR}");
      if(!connectorData.FirstRecord())
      {
        throw new Exception($"Nie znaleziono konektora o REF: {ecomChannelData.CONNECTOR}");    
      }
    
      try
      { 
        var command = new ECOM.ImportOrdersPaymentsRequestCommand()     
        {      
          EcomChannel = ecomChannelData.REF.AsInteger,      
          OrderList = orderList      
        };
           
    
        if(string.IsNullOrEmpty(connectorData.GROUPNAME)
          || string.IsNullOrEmpty(connectorData.SYMBOL))
        {
          resultMsg += "Brak pełniej nazwy konektora\n";       
        }
        else
        {      
          EDA.SendCommand(connectorData.GROUPNAME + "." + connectorData.SYMBOL, command);
          resultMsg = $"Wygenerowano komendę pobrania historii płatności zamówień w kanale {ecomChannelData.NAME}\n";                     
        }
      }  
      catch(Exception ex)
      {
        resultMsg += $"Nie udało sie wysłać komendy pobrania historii zamowień. Błąd: {ex.Message}\n";         
      } 
    
    
      return resultMsg;
    }
    
    
    


    /// <param name="ecomChannelRef"></param>
    /// <param name="exportMode"></param>
    /// <param name="inventoryStocksList"></param>
    [UUID("3e33624778dd4b0281e9c16782c351a5")]
    public static string SendInventoryStocks(int ecomChannelRef, ExportMode exportMode, 
      List<Int64> inventoryStocksList = null)
    {
      //metoda dla zadanej listy lub trybu wysyłki stanów magazynowych generuje komendę aktualizacji stanów w kanale sprzedaży
      //dodatkowo sprawdza, czy towar był wysłany na witrynę i jeżeli nie to uruchamia najpierw wysyłkę towaru
      string resultMsg = "";  
      string sqlSelectInventoriesToExportString = "";
      var ecomChannelData = new ECOM.ECOMCHANNELS();
      var connectorData = new SYSTEM.EDACONNECTORS();
      ecomChannelData.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannelData.REF.Symbol} = 0{ecomChannelRef}");
      if(!ecomChannelData.FirstRecord())
      {
        throw new Exception($"Nie znaleziono kanału sprzedaży o ref : {ecomChannelRef.ToString()}");
      }
    
      connectorData.FilterAndSort($"SYS_EDACONNECTORS.{connectorData.REF.Symbol} = 0{ecomChannelData.CONNECTOR}");
      if(!connectorData.FirstRecord())
      {
        throw new Exception($"Nie znaleziono konektora : {ecomChannelData.CONNECTOR.ToString()}");
      }
    
      var inventoryToExportList = new List<Int64>();
      if(inventoryStocksList == null)
      {
        inventoryStocksList = new List<Int64>();
      }
      //listę stanów magazynowych do wysłania wypełniamy w zależności od wybranego trybu,  
      switch (exportMode)
      {      
        case ExportMode.LastChange:
          //zmienione         
          sqlSelectInventoriesToExportString =
          @"select s.ref, iif(ei.ecominventoryid is null, ei.ref, null) invref
            from ecominventories ei
              join ecominventorystocks s on s.ecominventoryref = ei.ref " +
                $"and s.syncstatus = {(int)EcomSyncStatus.ExportPending} " +
            $"where ei.ACTIVE = 1 and ei.ecomchannelref = {ecomChannelRef}";
    
          //pobieramy liste stanów mag z wyliczonego wyżej selecta
          foreach(var inv in CORE.QuerySQL(sqlSelectInventoriesToExportString))
          {
            inventoryStocksList.Add(inv["REF"].AsInteger64);
            if(!string.IsNullOrEmpty(inv["INVREF"]))
            {
              inventoryToExportList.Add(inv["INVREF"].AsInteger64);
            }
          }             
              
          break;
        case ExportMode.List:
          //wszystkie stany mag dla przekazanej listy towarów
          if((inventoryStocksList?.Count ?? 0) == 0)
          {
            resultMsg = $"Brak listy stanów magazynowych dla trybu wysyłki listy stanów magazynowych";                    
          }     
          else
          {        
            sqlSelectInventoriesToExportString =
             @"select iif(ei.ecominventoryid is null, ei.ref, null) invref 
              from ecominventories ei
                join ecominventorystocks s on s.ecominventoryref = ei.ref " +
              $"where ei.ACTIVE = 1 and ei.ecomchannelref = {ecomChannelRef} ";
    
            //listę towarów doklejamy do selecta
            sqlSelectInventoriesToExportString += 
              "and s.ref in (" + string.Join(",", inventoryStocksList.Select(n => n.ToString()).ToArray()) + ")";              
    
            foreach(var inv in CORE.QuerySQL(sqlSelectInventoriesToExportString))
            {          
              if(!string.IsNullOrEmpty(inv["INVREF"]))
              {
                inventoryToExportList.Add(inv["INVREF"].AsInteger64);
              }
            }   
          }                        
          break;
        case ExportMode.All:
          //wszystkie stany mag w kanale sprzedaży     
          sqlSelectInventoriesToExportString =
             @"select s.ref, iif(ei.ecominventoryid is null, ei.ref, null) invref
              from ecominventories ei
                join ecominventorystocks s on s.ecominventoryref = ei.ref " +
              $"where ei.ACTIVE = 1 and ei.ecomchannelref = {ecomChannelRef}";
    
          //pobieramy liste stanów mag z wyliczonego wyżej selecta
          foreach(var inv in CORE.QuerySQL(sqlSelectInventoriesToExportString))
          {
            inventoryStocksList.Add(inv["REF"].AsInteger64);
            if(!string.IsNullOrEmpty(inv["INVREF"]))
            {
              inventoryToExportList.Add(inv["INVREF"].AsInteger64);
            }
          } 
          break;
        defalut:
          throw new Exception($"Nieobsłużony tryb wysyłania danych stanów magazynowch: {exportMode.ToString()}");                
          break; 
      }  
    
      //TODO jedna kolejka obejmuje wysyłkę wielu stanów mag, więc chyba symbol musi być stały, 
      //ale generowany w ramach pewnie powinna go zwracać jakaś metoda
      string logicalQueue = "ExportInventoryStockQueueLogical";
    
      if(inventoryToExportList.Count() > 0)
      {
        //jezeli są towary, które ani razu dobrze się nie wysłały, to przed wysyłką stanów mag puszczamy
        //wysyłkę towarów    
        SendInventory(ecomChannelRef, ExportMode.List, inventoryToExportList.Distinct().ToList(), logicalQueue);
      } 
      
      try
      {
        var command = new ECOM.ExportInventoryStocksCommand()
        {
          EcomChannel = ecomChannelData.REF.AsInteger,
          EcomChannelSymbol = ecomChannelData.SYMBOL,      
        };  
    
        ECOM.ECOMSTDMETHODS methods = LOGIC.ECOMCHANNELS.FindMethodObject(ecomChannelRef);
        if(methods != null)
        {
          var result = methods.Logic.PrepareSendInventoryStocks(inventoryStocksList, null, command);
        }
    
        if(string.IsNullOrEmpty(connectorData.GROUPNAME)
          || string.IsNullOrEmpty(connectorData.SYMBOL))
        {
          throw new Exception($"Brak pełniej nazwy konektora dla kanału sprzedaży {ecomChannelData.SYMBOL}\n");        
        }
        else
        {
          if((command.InventoryStocksInfoList?.Count ?? 0) > 0)
          {        
            //wysyłamy komendę, jezeli są towary do aktualizacji
            command.SetLogicalQueueIdentifier(logicalQueue);
            //odblokowujemy kolejkę, bo nawet jeżeli wyślemy towary i wtedy dostaniemy błąd, to i tak
            //chcemy spróbować wysłać listę stanów mag, żeby nie było tak, że błąd wysyłki jednego towaru zablokuje wysyłke
            //wszytkich stanów
            command.SetResumeProcessingQueue();                           
            //wysyłka na kolejkę konektora
            EDA.SendCommand($"{connectorData.GROUPNAME + "." + connectorData.SYMBOL}:ExportInventoryStockQueue", command); 
            resultMsg = $"Wygenerowano komendę wysyłki {command.InventoryStocksInfoList?.Count.ToString()} towarów w kanale {ecomChannelRef.ToString()}\n";
          }
          else
          {
            //nie ma nic do wysłania
            resultMsg = $"Brak towarów do wysłania w kanale {ecomChannelRef.ToString()}";
          }
        }
      }  
      catch(Exception ex)
      {    
        throw new Exception($"Błąd generowania komend wysyłki stanów magazynowych w kanale sprzedaży {ecomChannelData.NAME}: {ex.Message}");        
      }  
      ecomChannelData.Close();
      connectorData.Close();
      return resultMsg;
    }
    
    
    


    /// <param name="ecomChannelRef"></param>
    /// <param name="exportMode"></param>
    /// <param name="inventoryPricesList"></param>
    /// <param name="currencyList"></param>
    [UUID("49647e6df1e2475b86814ddaf2b37650")]
    public static string SendInventoryPrices(int ecomChannelRef, ExportMode exportMode, 
      List<Int64> inventoryPricesList = null, List<Int64> currencyList = null)
    {
      //metoda dla zadanej listy lub trybu wysyłki cen generuje komendę aktualizacji cen w kanale sprzedaży
      //dodatkowo sprawdza, czy towar był wysłany na witrynę i jeżeli nie to uruchamia najpierw wysyłkę towaru
      string resultMsg = "";  
      string sqlSelectInventoriesToExportString = "";
      var ecomChannelData = new ECOM.ECOMCHANNELS();
      var connectorData = new SYSTEM.EDACONNECTORS();
      ecomChannelData.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannelData.REF.Symbol} = 0{ecomChannelRef}");
      if(!ecomChannelData.FirstRecord())
      {
        throw new Exception($"Nie znaleziono kanału sprzedaży o ref : {ecomChannelRef.ToString()}");
      }
    
      connectorData.FilterAndSort($"SYS_EDACONNECTORS.{connectorData.REF.Symbol} = 0{ecomChannelData.CONNECTOR}");
      if(!connectorData.FirstRecord())
      {
        throw new Exception($"Nie znaleziono konektora : {ecomChannelData.CONNECTOR.ToString()}");
      }
    
      var inventoryToExportList = new List<Int64>();
      if(inventoryPricesList == null)
      {
        inventoryPricesList = new List<Int64>();
      }
      //listę cen towarów do wysłania wypełniamy w zależności od wybranego trybu,  
      switch (exportMode)
      {      
        case ExportMode.LastChange:
          //zmienione         
          sqlSelectInventoriesToExportString =
          @"select p.ref, iif(ei.ecominventoryid is null, ei.ref, null) invref
            from ecominventories ei
              join ecominventoryprices p on p.ecominventoryref = ei.ref " +
                $"and p.syncstatus = {(int)EcomSyncStatus.ExportPending} " +
            $"where ei.ACTIVE = 1 and ei.ecomchannelref = {ecomChannelRef}";
    
          //pobieramy liste cen z wyliczonego wyżej selecta
          foreach(var inv in CORE.QuerySQL(sqlSelectInventoriesToExportString))
          {
            inventoryPricesList.Add(inv["REF"].AsInteger64);
            if(!string.IsNullOrEmpty(inv["INVREF"]))
            {
              inventoryToExportList.Add(inv["INVREF"].AsInteger64);
            }
          }             
              
          break;
        case ExportMode.List:
          //wszystkie ceny dla przekazanej listy towarów
          if((inventoryPricesList?.Count ?? 0) == 0)
          {
            resultMsg = $"Brak listy cen towarów dla trybu wysyłki listy cen";                    
          }     
          else
          {        
            sqlSelectInventoriesToExportString =
             @"select iif(ei.ecominventoryid is null, ei.ref, null) invref 
              from ecominventories ei
                join ecominventoryprices p on p.ecominventoryref = ei.ref " +
              $"where ei.ACTIVE = 1 and ei.ecomchannelref = {ecomChannelRef} ";
    
            //listę towarów doklejamy do selecta
            sqlSelectInventoriesToExportString += 
              "and p.ref in (" + string.Join(",", inventoryPricesList.Select(n => n.ToString()).ToArray()) + ")";              
    
            foreach(var inv in CORE.QuerySQL(sqlSelectInventoriesToExportString))
            {          
              if(!string.IsNullOrEmpty(inv["INVREF"]))
              {
                inventoryToExportList.Add(inv["INVREF"].AsInteger64);
              }
            }   
          }
                            
          break;
        case ExportMode.All:
          //wszystkie naliczone ceny w kanale sprzedaży     
          sqlSelectInventoriesToExportString =
             @"select p.ref, iif(ei.ecominventoryid is null, ei.ref, null) invref
              from ecominventories ei
                join ecominventoryprices p on p.ecominventoryref = ei.ref " +
              $"where ei.ACTIVE = 1 and ei.ecomchannelref = {ecomChannelRef}";
    
          //pobieramy liste cen z wyliczonego wyżej selecta
          foreach(var inv in CORE.QuerySQL(sqlSelectInventoriesToExportString))
          {
            inventoryPricesList.Add(inv["REF"].AsInteger64);
            if(!string.IsNullOrEmpty(inv["INVREF"]))
            {
              inventoryToExportList.Add(inv["INVREF"].AsInteger64);
            }
          } 
          break;
        defalut:
          throw new Exception($"Nieobsłużony tryb wysyłania danych cen towaru: {exportMode.ToString()}");                
          break; 
      }  
    
      //[ML] TODO jedna kolejka obejmuje wiele cen towarów i wiele towarów, więc chyba symbol musi być stały, 
      //ale generowany w ramach pewnie powinna go zwracać jakaś metoda
      string logicalQueue = "ExportInventoryQueueLogical";
    
      if(inventoryToExportList.Count() > 0)
      {
        //jezeli są towary, które ani razu dobrze się nie wysłały, to przed wysyłką cen puszczamy
        //wysyłkę towarów    
        SendInventory(ecomChannelRef, ExportMode.List, inventoryToExportList.Distinct().ToList(), logicalQueue);
      } 
      
      try
      {
        var command = new ECOM.ExportInventoryPricesCommand()
        {
          EcomChannel = ecomChannelData.REF.AsInteger,
          EcomChannelSymbol = ecomChannelData.SYMBOL,      
        };  
    
        ECOM.ECOMSTDMETHODS methods = LOGIC.ECOMCHANNELS.FindMethodObject(ecomChannelRef);
        if(methods != null)
        {
          var result = methods.Logic.PrepareSendInventoryPrices(inventoryPricesList, null, command);
        }
    
        if(string.IsNullOrEmpty(connectorData.GROUPNAME)
          || string.IsNullOrEmpty(connectorData.SYMBOL))
        {
          throw new Exception($"Brak pełniej nazwy konektora dla kanału sprzedaży {ecomChannelData.SYMBOL}\n");        
        }
        else
        {
          if((command.InventoryPricesInfoList?.Count ?? 0) > 0)
          {        
            //wysyłamy komendę, jezeli są towary do aktualizacji
            command.SetLogicalQueueIdentifier(logicalQueue);
            //odblokowujemy kolejkę, bo nawet jeżeli wyślemy towary i wtedy dostaniemy błąd, to i tak
            //chcemy spróbować wysłać listę cen, żeby nie było tak, że błąd wysyłki jednego towaru zablokuje wysyłke
            //wszytkich cen
            command.SetResumeProcessingQueue();                           
            //wysyłka na kolejkę konektora
            EDA.SendCommand($"{connectorData.GROUPNAME + "." + connectorData.SYMBOL}:ExportInventoryQueue", command); 
            resultMsg = $"Wygenerowano komendę wysyłki {command.InventoryPricesInfoList?.Count.ToString()} towarów w kanale {ecomChannelRef.ToString()}\n";
          }
          else
          {
            //nie ma nic do wysłania
            resultMsg = $"Brak towarów do wysłania w kanale {ecomChannelRef.ToString()}";
          }
        }
      }  
      catch(Exception ex)
      {    
        throw new Exception($"Błąd generowania komend wysyłki cen towarów w kanale sprzedaży {ecomChannelData.NAME}: {ex.Message}");        
      }  
      ecomChannelData.Close();
      connectorData.Close();
      return resultMsg;
    }
    
    
    


    /// <param name="ecomChannelRef"></param>
    [UUID("51e56f0b9ef146fa8403cbb141007772")]
    public static ECOM.ECOMSTDMETHODS FindMethodObject(int ecomChannelRef)
    {
      //Metoda zwracająca dla danego kanału sprzedaży odpowiednią klasę metod obsługi exportu/importu dla danego kanału sprzedaży.
        var ecomChannel = new ECOM.ECOMCHANNELS();
        ecomChannel.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannel.REF.Symbol} = 0{ecomChannelRef}");
        if(ecomChannel.FirstRecord())
        {
          if(string.IsNullOrEmpty(ecomChannel.METHODSOBJ))
            return new ECOM.ECOMSTDMETHODS();
          else
          {
            try
            {
              var orderSyncObject = CreateObject(ecomChannel.METHODSOBJ);
              return (orderSyncObject as ECOM.ECOMSTDMETHODS);
            }
            catch(Exception e)
            {
              throw new Exception($"Nie udało się powołać obiektu metod importu/eksportu {ecomChannel.METHODSOBJ} dla kanału sprzedaży  {ecomChannelRef}");
            }
          }
        }
        else
        {
          throw new Exception($"Nie znaleziono kanału sprzedaży o REF: {ecomChannelRef}");
        }
        ecomChannel.Close();
            
      return null;
    }
    
    
    


    /// <param name="ecomChannelRef"></param>
    /// <param name="inventoryList"></param>
    [UUID("71017548435542298b044df60fe54455")]
    public static List<EcomInventoryPriceInfo> GetPricesList(int ecomChannelRef, List<String> inventoryList)
    {
      string resultMsg = "";  
      var priceInfoList = new List<EcomInventoryPriceInfo>();
      var ecomChannelData = new ECOM.ECOMCHANNELS();
      var connectorData = new SYSTEM.EDACONNECTORS();
      ecomChannelData.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannelData.REF.Symbol} = 0{ecomChannelRef}");
      if(!ecomChannelData.FirstRecord())
      {
        throw new Exception($"Nie znaleziono kanału sprzedaży o REF: {ecomChannelRef}");    
      }
      connectorData.FilterAndSort($"SYS_EDACONNECTORS.{connectorData.REF.Symbol} = 0{ecomChannelData.CONNECTOR}");
      if(!connectorData.FirstRecord())
      {
        throw new Exception($"Nie znaleziono konektora o REF: {ecomChannelData.CONNECTOR}");    
      }
    
      try
      {
        var command = new ECOM.GetPricesListCommand()     
        {      
          EcomChannel = ecomChannelData.REF.AsInteger,
          InventoryIdList = inventoryList 
        };    
    
        if(string.IsNullOrEmpty(connectorData.GROUPNAME)
          || string.IsNullOrEmpty(connectorData.SYMBOL))
        {
          throw new Exception("Brak pełniej nazwy konektora\n");       
        }
        else
        {     
          var result = EDA.ExecuteCommand(connectorData.GROUPNAME + "." + connectorData.SYMBOL, command);      
          var handlerResult = result.HandlerResult;  
          priceInfoList = (List<EcomInventoryPriceInfo>)result["Result"];       
        }
      }  
      catch(Exception ex)
      { 
        throw new Exception("Błąd komunikacji z bramką. " + ex.Message);      
      }  
      ecomChannelData.Close();
      connectorData.Close();
    
      return priceInfoList; 
    }
    
    
    
    


    /// <summary>
    /// metoda do użycia przez operatorów z UI i z schedulera
    /// ;  
    /// metoda uruchamia naliczanie stanów magazynów wirtualnych dla danego kanału sprzedaży, lub wszystkich kanałów, jeżeli żadnego nie podano; 
    ///   
    /// wywołuje metodę CalculateInventoryStocksForEcomChannel z obiektu standardowego albo ze wskazanego na kanale; 
    /// </summary>
    /// <param name="calcMode"></param>
    /// <param name="ecomChannelRef"></param>
    /// <param name="ecomChannelStocksList"></param>
    /// <param name="inventoryStocksList"></param>
    [UUID("73271f4c26374c65a478109b7e367cd4")]
    public static string CalculateInventroryStocks(CalculateMode calcMode, int? ecomChannelRef = null, 
      List<Int64> ecomChannelStocksList = null, List<Int64> inventoryStocksList = null)
    {
      //metoda do użycia przez operatorów z UI i z schedulera
      //metoda uruchamia naliczanie stanów magazynów wirtualnych dla danego kanału sprzedaży, lub wszystkich kanałów, jeżeli żadnego nie podano  
      //wywołuje metodę CalculateInventoryStocksForEcomChannel z obiektu standardowego albo ze wskazanego na kanale 
      
      string resultMsg = "";
     
      try
      {    
        //jeżeli nie podano kanału sprzedaży, to chcemy naliczać we wszystkich   
        var ecomChannelList = new List<int>();
        if(ecomChannelRef != null)
        {
          ecomChannelList.Add(ecomChannelRef ?? 0);
        }
        else
        {     
          //ecomChannelList = new LOGIC.ECOMCHANNELS().Get().ToList().Select(ch => (ch.REF ?? 0)).ToList();    
          ecomChannelList = new LOGIC.ECOMCHANNELS().Get().ToList().Where(ch => ch.ACTIVE == 1).Select(ch => (ch.REF ?? 0)).ToList();               
        }  
    
        foreach(var ecomChannel in ecomChannelList)
        {
          //wyliczamy per kanał, bo możliwe, że będą różne metody dla każdego kanału  
          ECOM.BR_TOWARY_STANY methods = LOGIC.ECOMCHANNELS.FindStockMethodObject(ecomChannel);
          if(methods != null)
          {
            resultMsg += methods.Logic.CalculateInventoryStocksForEcomChannel(ecomChannel, calcMode, inventoryStocksList, ecomChannelStocksList) + "\n";
          }
        }
      }
      catch(Exception ex)
      {
        throw new Exception("Nie udało sie naliczyć stanów magazynowych towarów. Błąd: " + ex.Message);
      } 
      
      return resultMsg;
    }
    
    
    


    /// <param name="channelref"></param>
    [UUID("8301cbe9acdf4369aca2768be27b7f01")]
    public static bool IsBLChannel(int channelref)
    {
      var channel = new ECOM.LOGIC.ECOMCHANNELS().Get(channelref);
      if(channel != null)
      {
        var connector = new SYSTEM.LOGIC.EDACONNECTORS().Get((int) channel.CONNECTOR);
        return connector.SYMBOL == "BLCON";
      }
    
      return false;
    }
    
    
    


    /// <param name="ecomChannelRef"></param>
    [UUID("9797c561290a42d4a49810f3a49648e6")]
    public static ECOM.BR_TOWARY_STANY FindStockMethodObject(int ecomChannelRef)
    {
      //Metoda zwracająca dla danego kanału sprzedaży odpowiednią klasę metod obsługi exportu/importu dla danego kanału sprzedaży.
      var channelParams = LOGIC.ECOMSTDMETHODS.LoadEcomChannelParams(ecomChannelRef);
      var ecomChannel = new ECOM.ECOMCHANNELS();
      ecomChannel.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannel.REF.Symbol} = 0{ecomChannelRef}");
      if(ecomChannel.FirstRecord())
      {
        if(string.IsNullOrEmpty(channelParams["_calcstockmethodsobj"].ToString()))
          return new ECOM.BR_TOWARY_STANY();
        else
        {
          try
          {
            var orderSyncObject = CreateObject(channelParams["_calcstockmethodsobj"]);
            return (orderSyncObject as ECOM.BR_TOWARY_STANY);
          }
          catch(Exception e)
          {
            throw new Exception($"Nie udało się powołać obiektu metod obliczania stanów magazynowych {channelParams["_calcstockmethodsobj"]} dla kanału sprzedaży  {ecomChannelRef}");
          }
        }
      }
      else
      {
        throw new Exception($"Nie znaleziono kanału sprzedaży o REF: {ecomChannelRef}");
      }
      ecomChannel.Close();     
      return null;
    }
    
    
    


    /// <param name="channelRef"></param>
    [UUID("9e10c69c625e45579227dfbcdb0fa020")]
    public static DateTime GetLastOrderChangeTimestamp(int channelRef)
    {
      var ecomOrder = new ECOM.ECOMORDERS();
      string filterStr = $"{nameof(ECOMORDERS)}.{ecomOrder.ECOMCHANNELREF.Symbol} = {channelRef}"; 
      string sortStr = $"{nameof(ECOMORDERS)}.{ecomOrder.LASTCHANGETMSTMP.Symbol} desc";
    
      ecomOrder.FilterAndSort(filterStr, sortStr);
      if(ecomOrder.FirstRecord())
      {
        return DateTime.Parse(ecomOrder.LASTCHANGETMSTMP);
      }
      else
      {
        throw new Exception($"Nie znaleziono kanału sprzedaży o REF: {channelRef}");
      }  
      ecomOrder.Close();
    }
    
    
    


    /// <param name="ecomChannelRef"></param>
    /// <param name="mode"></param>
    /// <param name="dateBegin"></param>
    /// <param name="dateEnd"></param>
    /// <param name="orderList"></param>
    [UUID("a3f5fd7d63f54f649c9166dbe6a9ddfd")]
    public static string GetOrders(int ecomChannelRef, importMode mode = importMode.SinceLastImport, 
      DateTime? dateBegin = null, DateTime? dateEnd = null, List<int> orderList = null)
    {
      string resultMsg = "";  
      var ecomChannelData = new ECOM.ECOMCHANNELS();
      var connectorData = new SYSTEM.EDACONNECTORS();
      ecomChannelData.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannelData.REF.Symbol} = 0{ecomChannelRef}");
      if(!ecomChannelData.FirstRecord())
      {
        throw new Exception($"Nie znaleziono kanału sprzedaży o REF: {ecomChannelRef}");    
      }
      connectorData.FilterAndSort($"SYS_EDACONNECTORS.{connectorData.REF.Symbol} = 0{ecomChannelData.CONNECTOR}");
      if(!connectorData.FirstRecord())
      {
        throw new Exception($"Nie znaleziono konektora o REF: {ecomChannelData.CONNECTOR}");    
      }
    
      try
      {
        var command = new ECOM.ImportOrdersRequestCommand()     
        {      
          ImportMode = mode,
          EcomChannel = ecomChannelData.REF.AsInteger,      
          OrderDateFrom = dateBegin,
          OrderDateTo = dateEnd,
          OrderList = orderList?.Select(o => o.ToString()).ToList() 
        };    
    
        if(string.IsNullOrEmpty(connectorData.GROUPNAME)
          || string.IsNullOrEmpty(connectorData.SYMBOL))
        {
          resultMsg += "Brak pełniej nazwy konektora\n";       
        }
        else
        {      
          EDA.SendCommand(connectorData.GROUPNAME + "." + connectorData.SYMBOL, command);
          resultMsg = $"Wygenerowano komendę pobrania zamówień w trybie: {mode.ToString()} w kanale {ecomChannelData.NAME}\n";
    
          //aktualizujemy czas kolejnego pobrania zamówień w danym kanale sprzedaży
          //dla trybów pobrania wsztkich zamówień i pobrania od ostatniej aktualizacji      
          if(mode == importMode.SinceLastImport || mode == importMode.All)
          {
            ecomChannelData.EditRecord();
            ecomChannelData.AUTOGETORDLASTTIME = DateTime.Now;
            if(!ecomChannelData.PostRecord())
            {
              throw new Exception($"Błąd aktualizacji czas kolejnego pobrania zamówień w kanale sprzedaży o REF: {ecomChannelData.REF}");
            }	 
          }                        
        }
      }  
      catch(Exception ex)
      {
        resultMsg += $"Nie udało sie wysłać komendy pobrania zamowień. Błąd: {ex.Message}\n";         
      }
      ecomChannelData.Close();
      connectorData.Close();
    
      return resultMsg;
    }
    
    


    /// <summary>
    /// Dla zadanej listy zamówień, lub danych o statusie zamówienia zawartych w klasie EcomOrderStatusInfo metoda tworzy nową komendę wysylki statusu na witrynę kanału sprzedaży wypełnia ją danymi i wysyła na szynę EDA
    /// </summary>
    /// <param name="ecomChannelRef"></param>
    /// <param name="exportMode"></param>
    /// <param name="orderList"></param>
    /// <param name="orderStatusInfo"></param>
    /// <param name="logicalQueue"></param>
    [UUID("a5bc6c60b40d4aff948c882b4e0e74eb")]
    public static string SendOrdersStatus(int ecomChannelRef, ExportMode exportMode, List<Int64> orderList = null, EcomOrderStatusInfo orderStatusInfo = null,
      string logicalQueue = null)
    {
      //metoda dla zadanej listy zamówień (ECOMORDERS.REF) generuje komendę wysłąnai statusu.
      string resultMsg = "";  
      var ecomChannelData = new ECOM.ECOMCHANNELS();
      var connectorData = new SYSTEM.EDACONNECTORS();
      ecomChannelData.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannelData.REF.Symbol} = 0{ecomChannelRef}");
      if(!ecomChannelData.FirstRecord())
      {
        throw new Exception($"Nie znaleziono kanału sprzedaży o ref : {ecomChannelRef.ToString()}");
      }
    
      connectorData.FilterAndSort($"SYS_EDACONNECTORS.{connectorData.REF.Symbol} = 0{ecomChannelData.CONNECTOR}");
      if(!connectorData.FirstRecord())
      {
        throw new Exception($"Nie znaleziono konektora : {ecomChannelRef.ToString()}");
      }
      try
      {    
        var command = new ECOM.ExportOrdersStatusCommand()
        {
          EcomChannel = ecomChannelData.REF.AsInteger
        };
    
        ECOM.ECOMSTDMETHODS methods = LOGIC.ECOMCHANNELS.FindMethodObject(ecomChannelRef);
        if(methods != null)
        {
          var result =  methods.Logic.PrepareSendOrderStatus(orderList, orderStatusInfo, command); 
        };
       
        
        if(string.IsNullOrEmpty(connectorData.GROUPNAME)
          || string.IsNullOrEmpty(connectorData.SYMBOL))
        {
          throw new Exception($"Brak pełniej nazwy konektora dla kanału sprzedaży {ecomChannelData.SYMBOL}\n");      
        }
        else
        {      
          if(command.OrderStatusInfoList.Count == 1)
          {
            GetEDAIdentifierData getid = LOGIC.ECOMORDERS.GetEDAID(ecomChannelRef, command.OrderStatusInfoList.First());
            if(getid.Result)
            {
             command.SetIdentifier(getid.EDAIdentifier);	
            }
          }
          command.SetLogicalQueueIdentifier(logicalQueue);
          EDA.SendCommand(connectorData.GROUPNAME + "." + connectorData.SYMBOL, command);         
          resultMsg = $"Wygenerowano komendę wysyłki {command.OrderStatusInfoList?.Count.ToString()} statusów zamówień w kanale {ecomChannelRef}\n";       
          if(exportMode == ExportMode.LastChange || exportMode == ExportMode.All)
            {
              ecomChannelData.EditRecord();
              ecomChannelData.AUTOSENDORDERSTATUSLASTTIME = DateTime.Now;
              if(!ecomChannelData.PostRecord())
              {
                throw new Exception($"Błąd aktualizacji czasu ostatniego wysłania statusów zamówień w kanale sprzedaży o REF: {ecomChannelData.REF}");
              }	 
            }                               
        }
      }  
      catch(Exception ex)
      {
        throw new Exception($"Błąd generowania komend wysyłki statusów zamówień w kanale sprzedaży {ecomChannelData.NAME}: {ex.Message}");   
      } 
      ecomChannelData.Close();
      connectorData.Close();
      return resultMsg;
    }
    
    


    /// <param name="adapter"></param>
    /// <param name="connectorRef"></param>
    /// <param name="ecomChannelRef"></param>
    [UUID("b9959c3a60294d6e949fec170b4df88e")]
    public static string SendInventoryForChannels(string adapter, int? connectorRef, int? ecomChannelRef)
    {
      RequestMode expReqMode;
      //składanie zapytania do bazy danych, które pobierze listę kanałów sprzedaży, w których trzeba wysłać towary  
      string message = "";
      var sqlString = @"select cha.ref channel,
                               iif(coalesce(cha.autosendinventorylasttime, '') = '', '1900-01-01', cha.autosendinventorylasttime) sendlasttime,
                               coalesce(cha.autosendinventoryinterval, 0) sendinterval
                             from ecomchannels cha 
                              join sys_edaconnectors con on con.ref = cha.connector 
                              where cha.active = 1 ";        
      if(!string.IsNullOrEmpty(adapter)) 
      {
        expReqMode = RequestMode.Adapter;
        sqlString += $"and con.adapter = '{adapter}' "; 
      }
      else if(connectorRef != null)
      {
        expReqMode = RequestMode.Connector;
        sqlString += $"and con.ref = 0{connectorRef} ";
      }
      else if(ecomChannelRef != null)
      {
        expReqMode = RequestMode.Channel;
        sqlString += $"and echa.ref = 0{ecomChannelRef} ";
      }
    
    
      foreach(var dataRow in CORE.QuerySQL(sqlString))
      {    
        try 
        {
          if(dataRow["SENDINTERVAL"].AsInteger > 0)
          {
            DateTime nextSendInventory = dataRow["SENDLASTTIME"].AsDateTime;
            nextSendInventory = nextSendInventory.AddMinutes(dataRow["SENDINTERVAL"].AsInteger);
            if(nextSendInventory < DateTime.Now)            
            { 
              //uruchamiamy wspólną metodę generującą komendy eksportu dla wybranego kanału
              message += SendInventory(dataRow["CHANNEL"].AsInteger, ExportMode.LastChange) + "\n";      
            }
          }
          else
          {
            message += $"Nie wygenerowano komendy aktualizacji towarów dla kanału: {dataRow["CHANNEL"]}\n Kanał nie ma ustawionego interwału aktualizacji towarów.\n";
          }    
        } 
        catch(Exception ex)
        {        
          message += $"Błąd komendy aktualizacji towarów dla kanału: {dataRow["CHANNEL"]}: {ex.Message}\n";
          continue;    
        }        
      }  
     
      if(String.IsNullOrEmpty(message))
      {
        message = "Nie znaleziono aktywnych kanałów sprzedaży.";
      }
    
      return message; 
    }
    
    
    


    /// <summary>
    /// Metoda GetOrdersList wykorzystywana jest przy wersyfikacji spójności danych. Wywoływana jest przez metodę Fill w obiekcie ECOMCHECKORDERS
    /// 
    /// Przyjmuje parametry: 
    /// ecomChannelRef - ref kanału sprzedaży,
    /// importMode - opjca importu (tutaj opcją importu będzie pobranie zamówień z określonego przedziału czasowego), 
    /// dateBegin, dateEnd - zakres dat od kiedy sprawdzane bedą zamówienia. 
    /// 
    /// Zwraca:
    /// orderInfoList - listę pobranych zamówień z kanału sprzedaży
    /// 
    /// Metoda wywołuje komendę &quot;GetOrdersListCommand&quot;, która pobiera i zwraca zamówienia z kanału sprzedaży
    /// 
    /// </summary>
    /// <param name="ecomChannelRef"></param>
    /// <param name="mode"></param>
    /// <param name="dateBegin"></param>
    /// <param name="dateEnd"></param>
    /// <param name="orderList"></param>
    [UUID("e3aadaba82dd4d7ab6832590ee7db042")]
    public static List<EcomOrderInfo> GetOrdersList(int ecomChannelRef, importMode mode = importMode.SinceLastImport, 
      DateTime? dateBegin = null, DateTime? dateEnd = null, List<string> orderList = null)
    { 
      var orderInfoList = new List<EcomOrderInfo>();
      var ecomChannelData = new ECOM.ECOMCHANNELS();
      var connectorData = new SYSTEM.EDACONNECTORS();
    
      ecomChannelData.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannelData.REF.Symbol} =0{ecomChannelRef}");
      if(!ecomChannelData.FirstRecord())
      {
        throw new Exception($"Nie znaleziono kanału sprzedaży o REF: {ecomChannelRef}");    
      }
      connectorData.FilterAndSort($"SYS_EDACONNECTORS.{connectorData.REF.Symbol} = 0{ecomChannelData.CONNECTOR}");
      if(!connectorData.FirstRecord())
      {
        throw new Exception($"Nie znaleziono konektora o REF: {ecomChannelData.CONNECTOR}");    
      }
      
      try
      {
        var command = new ECOM.GetOrdersListCommand()     
        {      
          ImportMode = mode,
          EcomChannel = ecomChannelData.REF.AsInteger,      
          OrderDateFrom = dateBegin,
          OrderDateTo = dateEnd    
        };    
    
        if(string.IsNullOrEmpty(connectorData.GROUPNAME)
          || string.IsNullOrEmpty(connectorData.SYMBOL))
        {
          throw new Exception("Brak pełniej nazwy konektora\n");       
        }
        else
        {     
          var result = EDA.ExecuteCommand(connectorData.GROUPNAME + "." + connectorData.SYMBOL, command);
          var handlerResult = result.HandlerResult;  
          orderInfoList = (List<EcomOrderInfo>)result["Result"];       
        }
      }  
      catch(Exception ex)
      {
        throw new Exception($"Nie udało sie wysłać komendy pobrania zamowień. Błąd: {ex.Message}\n");         
      }  
      ecomChannelData.Close();
      connectorData.Close();
    
      return orderInfoList; 
    }
    
    
    
    


    /// <summary>
    /// metoda do użycia przez opratorów z UI i schedulera
    /// metoda nalicza ceny towarów dla danego kanału sprzedaży, lub wszystkich kanałów, jeżeli nie podano
    /// jezeli ceny dla danego towaru w danej walucie nie ma w kanale, to dodaje nowy wpis,
    /// jeżeli jest to aktualizuje wpis, ustawia isValid = true i oznacza do eksportu
    /// </summary>
    /// <param name="calcMode"></param>
    /// <param name="ecomChannelRef"></param>
    /// <param name="currencyList"></param>
    /// <param name="inventoryList"></param>
    [UUID("e42ec45dface4b8ea2b68f1c17207444")]
    public static string CalculateInventroryPrices(CalculateMode calcMode, int? ecomChannelRef = null, 
      List<string> currencyList = null, List<Int64> inventoryList = null)
    {
      //metoda do użycia przez opratorów z UI i schedulera
      //metoda nalicza ceny towarów dla danego kanału sprzedaży, lub wszystkich kanałów, jeżeli nie podano
      //jezeli ceny dla danego towaru w danej walucie nie ma w kanale, to dodaje nowy wpis,
      //jeżeli jest to aktualizuje wpis, ustawia isValid = true i oznacza do eksportu
      
      string resultMsg = "";
     
      try
      {    
        //jeżeli nie podano kanału sprzedaży, to chcemy naliczać we wszystkich   
        var ecomChannelList = new List<int>();
        if(ecomChannelRef != null)
        {
          ecomChannelList.Add(ecomChannelRef ?? 0);
        }
        else
        {     
          //ecomChannelList = new LOGIC.ECOMCHANNELS().Get().ToList().Select(ch => (ch.REF ?? 0)).Where(ch => ch.==1).ToList();       
          ecomChannelList = new LOGIC.ECOMCHANNELS().Get().ToList().Where(ch => ch.ACTIVE == 1).Select(ch => (ch.REF ?? 0)).ToList();          
        }  
    
        foreach(var ecomChannel in ecomChannelList)
        {
          //ceny wyliczamy per kanał, bo możliwe, że będą inne, bo np. liczone dla innego klienta    
          ECOM.BR_KLIENCI_CENY methods = LOGIC.ECOMCHANNELS.FindPriceMethodObject(ecomChannel);
          if(methods != null)
          {
            resultMsg += methods.Logic.CalculateInventoryPricesForEcomChannel(ecomChannel, calcMode, inventoryList, currencyList) + "\n";
          }
        }
      }
      catch(Exception ex)
      {
        throw new Exception("Nie udało sie naliczyć cen towarów. Błąd: " + ex.Message);
      } 
      
      return resultMsg;
    }
    
    
    


    /// <param name="exportMode"></param>
    [UUID("e5f160598cc345b999afd1eacd7b6559")]
    public static string AutoSendInventoryPrices(ExportMode exportMode)
    {
      //metoda logiki dla wysyłki cen towarów przez schedulera wysyła ceny do wszystkich aktywnych kanałów
      //sprzedaży w trybie "niekatualne" albo "wszystkie"    
      string message = "";
      var ecomChannel = new ECOM.ECOMCHANNELS(); 
      try
      {
        if(exportMode != ExportMode.LastChange && exportMode != ExportMode.All)
        {
          return $"Nieobsługiwany tryb wysyłania cen: {exportMode.ToString()}";      
        }  
    
        ecomChannel.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannel.ACTIVE.Symbol} = 1");
        if(ecomChannel.FirstRecord())
        {
          do
          {
            try
            {          
              int intervalSend = ecomChannel.AUTOSENDPRICESINTERVAL.AsInteger;
              DateTime nextSendDate = ecomChannel.AUTOSENDPRICESLASTTIME.AsDateTime.AddMinutes(intervalSend);        
              if(intervalSend > 0)
              {
                if(DateTime.Now > nextSendDate)
                {            
                  ecomChannel.EditRecord();  
                  ecomChannel.AUTOSENDPRICESLASTTIME = DateTime.Now;
                  if(!ecomChannel.PostRecord())
                  {
                    throw new Exception($"Błąd aktualizacji daty kolejnego wysłania raportu cen towarów w kanale sprzedaży o REF: {ecomChannel.REF}");
                  }	 
                  message += LOGIC.ECOMCHANNELS.SendInventoryPrices(ecomChannel.REF.AsInteger, exportMode);
                }
                else
                {
                  message += $"Kolejna próba wysłania cen dla kanału {ecomChannel.NAME} obędzie się: {nextSendDate}. \n";
                }
              }
              else if(intervalSend <= 0) 
              {
                message += $"Kanał {ecomChannel.NAME} nie ma ustawionego interwału wysyłania cen produktów. \n";
              }
            }
            catch(Exception)
            {
              //robimy catcha, żeby błąd w jednym kanale nie blokował innych
              message += $"Błąd wysyłania cen towarów dla kanału {ecomChannel.NAME}";
            }
          } while(ecomChannel.NextRecord());
          
        }
        
      }
      catch(Exception ex)
      {
        message = $"Błąd wysłania cen towarów {ex.Message}.\n" + message;
      }
      ecomChannel.Close();
      return message;  
    }
    
    
    


    /// <summary>
    /// metoda dla zadanej listy lub trybu wysyłki towarów generuje i wysyła na szynę EDA komendę aktualizacji w kanale sprzedaży
    /// </summary>
    /// <param name="ecomChannelRef"></param>
    /// <param name="exportMode"></param>
    /// <param name="inventoryList"></param>
    /// <param name="logicalQueue"></param>
    [UUID("ea034afe2ce0478fbcf75d93f9637de0")]
    public static string SendInventory(int ecomChannelRef, ExportMode exportMode, List<Int64> inventoryList = null,
      string logicalQueue = null)
    {
      //metoda dla zadanej listy lub trybu wysyłki towarów generuje komendę aktualizacji w kanale sprzedaży
      string resultMsg = "";  
      string sqlSelectInventoriesToExportString = "";
      var ecomChannelData = new ECOM.ECOMCHANNELS();
      var connectorData = new SYSTEM.EDACONNECTORS();
      ecomChannelData.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannelData.REF.Symbol} = 0{ecomChannelRef}");
      if(!ecomChannelData.FirstRecord())
      {
        throw new Exception($"Nie znaleziono kanału sprzedaży o ref : {ecomChannelRef.ToString()}");
      }
    
      connectorData.FilterAndSort($"SYS_EDACONNECTORS.{connectorData.REF.Symbol} = 0{ecomChannelData.CONNECTOR}");
      if(!connectorData.FirstRecord())
      {
        throw new Exception($"Nie znaleziono konektora : {ecomChannelData.CONNECTOR.ToString()}");
      }
    
      try
      {
        //listę towarów do wysłania wypełniamy w zależności od wybranego trybu,
        //chyba, że wybrano tryb wysyłki listy, to wtedy wysyłamy przekazaną listę towarów.
        switch (exportMode)
        {      
          case ExportMode.LastChange:
            //zmienione    
            inventoryList = new List<Int64>();
            sqlSelectInventoriesToExportString =
              "select inv.ref " +
                "from ecominventories inv " +
                $"where inv.ACTIVE = 1 and inv.ecomchannelref = {ecomChannelRef} " +
                  $"and inv.syncstatus = {(int)EcomSyncStatus.ExportPending}";
            
            foreach(var inv in CORE.QuerySQL(sqlSelectInventoriesToExportString))
            {
              inventoryList.Add(inv["REF"].AsInteger64);
            }         
            break;
          case ExportMode.List:
            //przekazana lista
            if((inventoryList?.Count ?? 0) == 0)
            {
              resultMsg = $"Brak listy z towarami dla trybu wysyłki listy zamówień";                    
            }                    
            break;
          case ExportMode.All:
            //wszystkie towary z kanału sprzedaży
            inventoryList = new List<Int64>();
            sqlSelectInventoriesToExportString =
              "select inv.ref " +
                "from ecominventories inv " +
                $"where inv.ACTIVE = 1 and inv.ecomchannelref = ({ecomChannelRef})";
    
            foreach(var inv in CORE.QuerySQL(sqlSelectInventoriesToExportString))
            {
              inventoryList.Add(inv["REF"].AsInteger64);
            }                 
            break;
          defalut:
            throw new Exception($"Nieobsłużony tryb wysyłania danych towaru: {exportMode.ToString()}");                
            break; 
        }   
    
        var command = new ECOM.ExportInventoryCommand()
        {
          EcomChannel = ecomChannelData.REF.AsInteger,
          EcomChannelSymbol = ecomChannelData.SYMBOL,      
        };  
    
        ECOM.ECOMSTDMETHODS methods = LOGIC.ECOMCHANNELS.FindMethodObject(ecomChannelRef);
        if(methods != null)
        {
          //naliczamy dane towarów i umieszczamy w komendzie metodą per kanał sprzedaży
          var result = methods.Logic.PrepareSendInventory(inventoryList, null, command); 
        }
    
        if(string.IsNullOrEmpty(connectorData.GROUPNAME)
          || string.IsNullOrEmpty(connectorData.SYMBOL))
        {
          throw new Exception($"Brak pełniej nazwy konektora dla kanału sprzedaży {ecomChannelData.SYMBOL}\n");        
        }
        else
        {
          //wysyłamy komendę, jezeli są towary do aktualizacji
          if((command.InventoryInfoList?.Count ?? 0) > 0)
          {
            if(command.InventoryInfoList.Count==1)
            {
              GetEDAIdentifierData getid = LOGIC.ECOMINVENTORIES.GetEDAID(ecomChannelRef, command.InventoryInfoList[0]);
              if(getid.Result)
              {
                command.SetIdentifier(getid.EDAIdentifier);
              }
            }
            
            //jeżeli przekazano kolejkę logiczną w parametrze wejściowym (użyte np. żeby wysłać towaru, których nie ma na witrynie 
            //przed wysyłką cen) to wysyłamy na kolejkę logiczną
            if(!string.IsNullOrEmpty(logicalQueue))
            {
              command.SetLogicalQueueIdentifier(logicalQueue);
              //dodajemy zwolnienie kolejki, bo nie chcemy, żeby towar, ktróry był wysyłany wcześniej i spowodował błąd blokował
              //wysłanie kolejnego
              command.SetResumeProcessingQueue();
            }
            EDA.SendCommand($"{connectorData.GROUPNAME + "." + connectorData.SYMBOL}:ExportInventoryQueue", command);
            resultMsg = $"Wygenerowano komendę wysyłki {command.InventoryInfoList?.Count.ToString()} towarów w kanale {ecomChannelRef.ToString()}\n";
            //aktualizujemy czas kolejnego wysłania towarów w danym kanale sprzedaży
            //dla trybów wysłania wszystkich towarów i od ostatniej aktualizacji 
            if(exportMode == ExportMode.LastChange || exportMode == ExportMode.All)
            {
              ecomChannelData.EditRecord();
              ecomChannelData.AUTOSENDINVENTORYLASTTIME = DateTime.Now;
              if(!ecomChannelData.PostRecord())
              {
                throw new Exception($"Błąd aktualizacji czas kolejnego pobrania zamówień w kanale sprzedaży o REF: {ecomChannelData.REF}");
              }	 
            }    
          }
          else
          {
            //nie ma nic do wysłania
            resultMsg = $"Brak towarów do wysłania w kanale {ecomChannelRef.ToString()}";
          }
        }
      }  
      catch(Exception ex)
      {
        throw new Exception($"Błąd generowania komend wysyłki towarów w kanale sprzedaży {ecomChannelData.NAME}: {ex.Message}");        
      }  
      ecomChannelData.Close();
      connectorData.Close();
      return resultMsg;
    }
    
    


    /// <param name="adapter"></param>
    /// <param name="connectorRef"></param>
    /// <param name="ecomChannelRef"></param>
    [UUID("ee89a9e8102446af8d4b5e504b0bd5b9")]
    public static string SendOrdersStatusForChannels(string adapter, int? connectorRef, int? ecomChannelRef)
    {
      RequestMode expReqMode;
      var ecomord = new LOGIC.ECOMORDERS();
      //składanie zapytania do bazy danych, które pobierze listę kanałów sprzedaży, w których trzeba wysłać towary  
      string message = "";
      string sqlString = @"select cha.ref channel,
                               iif(coalesce(cha.autosendorderstatuslasttime, '') = '', '1900-01-01', cha.autosendorderstatuslasttime) sendlasttime,
                               coalesce(cha.autosendorderstatusinterval, 0) sendinterval
                             from ecomchannels cha 
                              join sys_edaconnectors con on con.ref = cha.connector 
                             where cha.active = 1 ";        
      if(!string.IsNullOrEmpty(adapter)) 
      {
        expReqMode = RequestMode.Adapter;
        sqlString += $"and con.adapter = '{adapter}' "; 
      }
      else if(connectorRef != null)
      {
        expReqMode = RequestMode.Connector;
        sqlString += $"and con.ref = 0{connectorRef} ";
      }
      else if(ecomChannelRef != null)
      {
        expReqMode = RequestMode.Channel;
        sqlString += $"and echa.ref = 0{ecomChannelRef} ";
      }
    
      foreach(var dataRow in CORE.QuerySQL(sqlString))
      {    
        try 
        {
          var ordStatToExportList = new List<Int64>();
          ordStatToExportList = ecomord.GetOrdStatusToExport(dataRow["CHANNEL"].AsInteger);
          if(dataRow["SENDINTERVAL"].AsInteger > 0)
          {
            DateTime nextSendInventory = dataRow["SENDLASTTIME"].AsDateTime;
            nextSendInventory = nextSendInventory.AddMinutes(dataRow["SENDINTERVAL"].AsInteger);
            if(nextSendInventory < DateTime.Now)            
            { 
              //uruchamiamy wspólną metodę generującą komendy eksportu dla wybranego kanału
              message += SendOrdersStatus(dataRow["CHANNEL"].AsInteger, ExportMode.All, ordStatToExportList) + "\n";      
            }
          }
          else
          {
            message += $"Nie wygenerowano komendy wysyłki statusów dla kanału: {dataRow["CHANNEL"]}\n Kanał nie ma ustawionego interwału wysyłki statusów.\n";
          }    
        } 
        catch(Exception ex)
        {        
          message += $"Błąd komendy aktualizacji statusów dla kanału: {dataRow["CHANNEL"]}: {ex.Message}\n";
          continue;    
        }        
      }  
    
      if(String.IsNullOrEmpty(message))
      {
        message = "Nie znaleziono aktywnych kanałów sprzedaży.";
      }
    
      return message; 
    }
    
    
    


    /// <param name="adapter"></param>
    /// <param name="connectorRef"></param>
    /// <param name="ecomChannelRef"></param>
    /// <param name="exportMode"></param>
    [UUID("f5a61751038546808b1ae543894d1f18")]
    public static string SendInventoryPricesForChannels(string adapter, int? connectorRef, int? ecomChannelRef, ExportMode exportMode)
    {
      RequestMode expReqMode;
      //składanie zapytania do bazy danych, które pobierze listę kanałów sprzedaży, w których trzeba wysłać towary  
      string message = "";
      string sqlString = 
        @"select cha.ref channel,
            iif(coalesce(cha.autosendpriceslasttime, '') = '', '1900-01-01', cha.autosendpriceslasttime) sendlasttime,
            coalesce(cha.autosendpricesinterval, 0) sendinterval
          from ecomchannels cha 
            join sys_edaconnectors con on con.ref = cha.connector 
          where cha.active = 1 ";        
      if(!string.IsNullOrEmpty(adapter)) 
      {
        expReqMode = RequestMode.Adapter;
        sqlString += $"and con.adapter = '{adapter}' "; 
      }
      else if(connectorRef != null)
      {
        expReqMode = RequestMode.Connector;
        sqlString += $"and con.ref = 0{connectorRef} ";
      }
      else if(ecomChannelRef != null)
      {
        expReqMode = RequestMode.Channel;
        sqlString += $"and echa.ref = 0{ecomChannelRef} ";
      }
    
      try 
      {
        foreach(var dataRow in CORE.QuerySQL(sqlString))
        {
          if(dataRow["SENDINTERVAL"].AsInteger > 0)
          {
            DateTime nextSendDate = dataRow["SENDLASTTIME"].AsDateTime;
            nextSendDate = nextSendDate.AddMinutes(dataRow["SENDINTERVAL"].AsInteger);
            if(nextSendDate < DateTime.Now)            
            { 
              //uruchamiamy standardową metodę generującą komendy eksportu dla wybranego kanału
              message += SendInventoryPrices(dataRow["CHANNEL"].AsInteger, exportMode) + "\n";      
            }
          }
          else
          {
            message += $"Nie wygenerowano komendy aktualizacji cen towarów dla kanału: {dataRow["CHANNEL"]}\n Kanał nie ma ustawionego interwału aktualizacji towarów.\n";
          }    
        }
      }
      catch(Exception ex)
      {        
        throw new Exception($"Błąd komendy aktualizacji cen towarów : {ex.Message}");   
      } 
    
      if(String.IsNullOrEmpty(message))
      {
        message = "Nie znaleziono aktywnych kanałów sprzedaży.";
      }
       
      return message; 
    }
    /// <param name="ecomChannelRef"></param>
    /// <param name="OrderId"></param>
    [UUID("d473b293393846c4a5b4a0369bbd575f")]
    public static string GetOrderPackages(int ecomChannelRef, int OrderId)
    {
      string resultMsg = "";
      var ecomChannelData = new ECOM.ECOMCHANNELS();
      var connectorData = new SYSTEM.EDACONNECTORS();
    
      ecomChannelData.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannelData.REF.Symbol} = 0{ecomChannelRef}");
      if (!ecomChannelData.FirstRecord())
      {
        throw new Exception($"Nie znaleziono kanału sprzedaży o REF: {ecomChannelRef}");
      }
    
      connectorData.FilterAndSort($"SYS_EDACONNECTORS.{connectorData.REF.Symbol} = 0{ecomChannelData.CONNECTOR}");
      if (!connectorData.FirstRecord())
      {
        throw new Exception($"Nie znaleziono konektora o REF: {ecomChannelData.CONNECTOR}");
      }
      try
      {
        var command = new ECOM.ImportOrderPackagesRequestCommand()
        {
          OrderId = OrderId,
          EcomChannel = ecomChannelData.REF.AsInteger,
    
        };
    
        if (string.IsNullOrEmpty(connectorData.GROUPNAME)
           || string.IsNullOrEmpty(connectorData.SYMBOL))
        {
          resultMsg += "Brak pełnej nazwy konektora\n";
        }
        else
        {
          EDA.SendCommand(connectorData.GROUPNAME + "." + connectorData.SYMBOL, command);
          resultMsg = $"Wygenerowano komendę pobrania paczek do zamówienia {OrderId} w kanale {ecomChannelData.NAME}\n";
        }
      }
      catch (Exception ex)
      {
        resultMsg += $"Nie udało sie wysłać komendy pobrania paczek. Błąd: {ex.Message}\n";
      }
      
      return resultMsg;
    }
    
    
    


  }
}
