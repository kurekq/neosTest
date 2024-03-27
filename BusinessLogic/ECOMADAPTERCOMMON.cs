
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
  
  public partial class ECOMADAPTERCOMMON<TModel> where TModel : MODEL.ECOMADAPTERCOMMON, new()
  {

    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("00511c7a061b4577991daceadf2541b1")]
    [PhysicalQueueName("ExportInventoryStockQueue")]
    virtual public HandlerResult ExportInventoryStocksCommandHandler(ConsumeContext<ECOM.ExportInventoryStocksCommand> context)
    {
      //przekazujemy cały kontekst, żeby nie musieć przekazywać osobno każdego parametru
      string errMsg = DoExportInventoryStock(context);
    
      if(!string.IsNullOrEmpty(errMsg))
      {
        context.Message.Message = errMsg;
        throw new Exception(errMsg);     
      } 
      
      return HandlerResult.Handled;
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("0d2f88323ee9454c81122da64d48dd67")]
    virtual public HandlerResult GetStocksListCommandHandler(ConsumeContext<ECOM.GetStocksListCommand> context)
    {
      var result = DoGetStocksList(context);
      context.SetResult("Result", result);
      return HandlerResult.Handled;
    }
    
    
    


    /// <summary>
    /// wywoluje metodę abstrakcyjna DoExportInventory, która konwertuje dane do wysłania ze struktu uniwersalnych do stryktur witryny kanału sprzedaży np. IAI, realizuję wymianę danych i aktualizuje styatus synchronizacji w kanale sprzedaży.
    /// </summary>
    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("1a5b30bd9e784c7a9a9848df5e161dc0")]
    [PhysicalQueueName("ExportInventoryQueue")]
    virtual public HandlerResult ExportInventoryCommandHandler(ConsumeContext<ECOM.ExportInventoryCommand> context)
    {
      //przekazujemy cały kontekst, żeby nie musieć przekazywać osobno każdego parametru
      string errMsg = DoExportInventory(context);
    
      if(!string.IsNullOrEmpty(errMsg))
      {
        context.Message.Message = errMsg;
        throw new Exception(errMsg);     
      }  
      
      return HandlerResult.Handled;
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("1e9f51019a114fc382e9bac8a4bd0537")]
    [PhysicalQueueName("ExportInventoryQueue")]
    virtual public HandlerResult ExportInventoryPricesCommandHandler(ConsumeContext<ECOM.ExportInventoryPricesCommand> context)
    {
      //przekazujemy cały kontekst, żeby nie musieć przekazywać osobno każdego parametru
      string errMsg = DoExportInventoryPrices(context);
    
      if(!string.IsNullOrEmpty(errMsg))
      {
        context.Message.Message = errMsg;
        throw new Exception(errMsg);     
      } 
      
      return HandlerResult.Handled;
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("2451e7afcb964a77a9dcddfd389b23ee")]
    virtual public HandlerResult GetOrdersListCommandHandler(ConsumeContext<ECOM.GetOrdersListCommand> context)
    {
      var result = DoGetOrdersList(context);
      context.SetResult("Result", result);
      return HandlerResult.Handled;
    }
    
    


    /// <param name="context"></param>
    [UUID("2e3bdebe568741d194d7321727d6939f")]
    virtual public List<EcomInventoryStockInfo> DoGetStocksList(ConsumeContext<ECOM.GetStocksListCommand> context)
    {
      throw new NotImplementedByDesignException("Metoda adaptera abstrakcyjnego, do przeciążenia");
      return new List<EcomInventoryStockInfo>();
    }
    
    
    


    /// <summary>
    /// Standardowy handler realizuujący import zamówienia, Uruchamia standardowe lub przeciążone metody
    /// InitializeEcomOrder i  ImportOrderProcess, realizują dodanie lub aktualizację zamówienia z danych pobranych z kanału sprzedaży.
    /// Hanlder akualizuje datę najnowszego złożonego zamówienia na witrynie kanału sprzedaży 
    /// </summary>
    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("2e6a5f14b77245c8a015711615229f7a")]
    virtual public HandlerResult ImportOrderCommandHandler(ConsumeContext<ECOM.ImportOrderCommand> context)
    {  
      try
      {
        Contexts ecomChannelParams = LOGIC.ECOMSTDMETHODS.LoadEcomChannelParams(context.Message.EcomChannel);
    
        var methodsObject = LOGIC.ECOMCHANNELS.FindMethodObject(context.Message.EcomChannel);
        //najpierw założenie rekordu w ECOMORDERS
          
        RunInAutonomousTransaction(
          ()=> 
          { 
            methodsObject.Logic.InitializeEcomOrder(context.Message.EcomChannel, context.Message.OrderInfo, 
              ecomChannelParams, context.Message.GetIdentifier()); 
          },
          true
        );
         //jeżeli przy inicjalizacji ecomorder nie pojawi się exception, to kontynuujemy import 
        RunInAutonomousTransaction(
          ()=> 
          { 
            methodsObject.Logic.ProcessImportOrder(context.Message.EcomChannel, context.Message.OrderInfo, 
              ecomChannelParams);
            
            LOGIC.ECOMCHANNELS.CalcLastOrderTimestap(context.Message.EcomChannel, context.Message.OrderInfo.OrderAddDate);
          },
          true
    
        );
        //odtworzenie daty ostatniego zamówienia
      }
      catch(Exception ex)
      {     
        //błąd zapisywany w autonomicznej transakcji, żeby zapisał się nawet w przypadku exceptiona bazodanowego 
        RunInAutonomousTransaction(
          ()=>
          {
            LOGIC.ECOMCHANNELS.CalcLastOrderTimestap(context.Message.EcomChannel, null);
            var ecomOrder = new ECOM.ECOMORDERS();
            ecomOrder.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{ecomOrder.ECOMCHANNELREF.Symbol} = {context.Message.EcomChannel} " +
             $"AND {nameof(ECOM.ECOMORDERS)}.{ecomOrder.ECOMORDERID.Symbol} = '{context.Message.OrderInfo.OrderId}'" +
             $"AND {nameof(ECOM.ECOMORDERS)}.{ecomOrder.ECOMORDERSYMBOL.Symbol} = '{context.Message.OrderInfo.OrderSymbol}'"); 
            if(ecomOrder.FirstRecord())
            {  
              ecomOrder.EditRecord();
              ecomOrder.LASTSYNCERROR = ex.Message;
              ecomOrder.SYNCSTATUS = (int)EcomSyncStatus.ImportError;
              if(!ecomOrder.PostRecord())
              {
                throw new Exception("Błąd zapisu danych zamówienia do bazy.");
              } 
            }
            else
            {
              throw new Exception($"Nie znaleziono zamówienia o ID: {context.Message.OrderInfo.OrderId}, symbol zamówienia: {context.Message.OrderInfo.OrderSymbol}");
            }
            ecomOrder.Close();
          },
          true
        );                
        context.Message.Message = "Błąd importu zamówienia: " + ex.Message;
        throw new Exception(ex.Message);
      }
      finally
      {
        //dla trybów pobrania wszystkiego lub pobrania od ostatniej aktualizacji
        //aktualizujemy datę ostatniej próby pobrania zamówień datą dodania najnowszego (według daty pobrania na stronie) 
        //zamówienia pobranego dla kanału
        /*
        [ML] czekamy na poprawkę buga TSU-1908 z blokowaniem rekordów
        RunInAutonomousTransaction(
          ()=> 
          {
            var ecomChannel = new ECOM.ECOMCHANNELS();
            if(ecomChannel.FindRecord("REF", context.Message.EcomChannel.ToString()))
            {          
              if(context.Message.OrderInfo.OrderAddDate != null
                && context.Message.OrderInfo.OrderAddDate > ecomChannel.LASTORDERTIMESTMP.AsDateTime
                && (context.Message.ImportMode == importMode.All 
                  || context.Message.ImportMode == importMode.SinceLastImport))              
              {
              ecomChannel.LASTORDERTIMESTMP = context.Message.OrderInfo.OrderAddDate;          
              
              ecomChannel.PostRecord();
              }
            }                       
          },
          true        
        );
        */           
      }
      return HandlerResult.Handled;        
    }
    
    


    /// <summary>
    /// Metoda do przeciążenia w adapterze kanału sprzedaży.
    /// Metoda weryfikuje, czy drajwer fizyczny obsługuje taką konfigurację parametrów, buduje i wywołuje fizczne zapytanie w drajwerze, np. w drajwerze IAI, otrzymuje odpowiedź, zwraca komunikato błędzie, jeśi jest poprawne, to strukturę Response pakuje do osobnej komendy implementowanej już na poziomie drajwera realnego (w obiekcie dzieczącym) np. GetOrderResponse 
    /// </summary>
    /// <param name="context"></param>
    [UUID("5e21560c9ae8407cbb3b316d39c6d9c6")]
    virtual public string DoImportOrdersReqest(ConsumeContext<ECOM.ImportOrdersRequestCommand> context)
    {
      throw new NotImplementedByDesignException("Metoda adaptera abstrakcyjnego, do przeciążenia");
      return "";
    }
    
    


    /// <param name="context"></param>
    [UUID("60f446bf9af74d5397a6c4d36d7dd07b")]
    virtual public List<EcomOrderInfo> DoGetOrdersList(ConsumeContext<ECOM.GetOrdersListCommand> context)
    {
      throw new NotImplementedByDesignException("Metoda adaptera abstrakcyjnego, do przeciążenia");
      return new List<EcomOrderInfo>();
    }
    
    
    


    /// <param name="context"></param>
    [UUID("6b882c3957e14988952696de7627b71c")]
    virtual public List<EcomInventoryPriceInfo> DoGetPricesList(ConsumeContext<ECOM.GetPricesListCommand> context)
    {
      throw new NotImplementedByDesignException("Metoda adaptera abstrakcyjnego, do przeciążenia");
      return new List<EcomInventoryPriceInfo>();
    }
    
    
    


    /// <summary>
    /// Abstrakcyjny handler komendy ImportOrdersRequestCommand będącej częścią mechanizmu importu zamówień.
    /// Handler realizuje zapytanie witryny sprzedaży o listę zamówień o podanych parametrach. W adapterze rzeyczywistym metoda przekształca paramtry uniwersalne zapytania i wysła komendę (sama do siebie) juz fizyczną realizującą zapytnaie o zamówienai z drajwera.
    /// </summary>
    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("6b88f5985551453aabda870d22fbaffc")]
    virtual public HandlerResult ImportOrdersRequestCommandHandler(ConsumeContext<ECOM.ImportOrdersRequestCommand> context)
    {
      //przekazujemy cały kontekst, żeby nie musieć przekazywać osobno każdego parametru
      string errMsg = DoImportOrdersReqest(context);
    
      if(!string.IsNullOrEmpty(errMsg))
      {
        context.Message.Message = errMsg;
        throw new Exception(errMsg);     
      }  
      
      return HandlerResult.Handled;
    }
    
    


    /// <summary>
    /// tymczasowe metoda aktualizująca parametr konektora. W wersji skończonej aktualizacja będzie odbywać się przez dodaną
    /// przez ZRT metodę z EDA, a metoda UpdateConnectorParams zostanie usunięta
    /// parametry:
    ///  - connectorRef ref konektora, dla którego aktualizujemy parametr
    ///  - orderImportInterval nowa wartość parametru orderImportInterval
    /// </summary>
    /// <param name="connectorRef"></param>
    /// <param name="orderImportInterval"></param>
    [UUID("780fd7c022dc4414b8caa6d8474e2d77")]
    public static void UpdateConnectorParams(int? connectorRef, string orderImportInterval)
    {
      //[ML] do refaktoryzacji po przetestowaniu porawek ZRT na 6.0.5
      // tymczasowe rozwiązanie czekamy na skończenie projektu eda przez zrt 
      CORE.RunSQL(
        $"update sys_edaconnectorparams " + 
          $"set NVALUE = dateadd(0{orderImportInterval} minute to current_timestamp) " +
          $"where EDACONNECTORREF = 0{connectorRef} and NKEY = 'autogetordernexttime' "); 
    }
    
    


    /// <summary>
    /// wywoluje metodę abstrakcyjna DoExportOrderStatus, która konwertuje dane do wysłania ze struktu uniwersalnych do struktur witryny kanału sprzedaży np. IAI, realizuję wymianę danych i aktualizuje styatus synchronizacji w kanale sprzedaży.
    /// </summary>
    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("857981a820bc443aa6a5ead947b14336")]
    virtual public HandlerResult ExportOrdersStatusCommandHandler(ConsumeContext<ECOM.ExportOrdersStatusCommand> context)
    {
      //przekazujemy cały kontekst, żeby nie musieć przekazywać osobno każdego parametru
      string errMsg = DoExportOrderStatus(context);
    
      if(!string.IsNullOrEmpty(errMsg))
      {
        context.Message.Message = errMsg;
        throw new Exception(errMsg);     
      }  
      
      return HandlerResult.Handled;
    }
    
    


    /// <param name="context"></param>
    [UUID("8c74b36fd2f64f388de3e0e85039239f")]
    virtual public string DoExportInventoryPrices(ConsumeContext<ECOM.ExportInventoryPricesCommand> context)
    {
      throw new NotImplementedByDesignException("Metoda adaptera abstrakcyjnego, do przeciążenia");
      return "";
    }
    
    
    


    /// <summary>
    /// Metoda abstarkcyjna do przeciązenienia w adapterze realnym. Realizauje konwersję z danych uniwersalnych o towarach zawartych w strukturze List&lt;EcomInventoryInfo&gt; do struktur danego drajwera i wysyła dane statusuów do kanału sprzedaży w osobnych  komendach na poziomie adaptera realnego np. PutOrderStatusRequestCommand. Po otrzymaniu odpowiedzi zaznacza, że udało się zaktualizować status, albo że wystąpił błąd
    /// Uruchamiana w handlerze ExportInventoryCommandHandler 
    /// </summary>
    /// <param name="context"></param>
    [UUID("92772b069e3c4a24986dc1ffcaca0820")]
    virtual public string DoExportInventory(ConsumeContext<ECOM.ExportInventoryCommand> context)
    {
     throw new NotImplementedByDesignException("Metoda adaptera abstrakcyjnego, do przeciążenia");
      return "";
    }
    
    
    


    /// <summary>
    /// Metoda abstarkcyjna do przeciązenienia w adapterze realnym. Realizauje konwersję z danych uniwersalnych o statusach zawartych w strukturze List&lt;EcomOrderStatusInfo&gt; do struktur danego drajwera i wysyła dane statusuów do kanału sprzedaży w osobnych  komendach na poziomie adaptera realnego np. PutProductRequestCommand. Po otrzymaniu odpowiedzi zaznacza, że udało się zaktualizować status, albo że wystąp;ił błąd
    /// Uruchamiana w handlerze ExportOrdersStatusCommandHandler 
    /// </summary>
    /// <param name="context"></param>
    [UUID("c0f215a67fa1445cb55ef777e1aee583")]
    virtual public string DoExportOrderStatus(ConsumeContext<ECOM.ExportOrdersStatusCommand> context)
    {
      throw new NotImplementedByDesignException("Metoda adaptera abstrakcyjnego, do przeciążenia");
      return "";
    }
    
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("c1432e654d5242c1876d733006ee8a1f")]
    virtual public HandlerResult ImportOrdersPaymentsRequestCommandHandler(ConsumeContext<ECOM.ImportOrdersPaymentsRequestCommand> context)
    {
      string errMsg = DoImportOrdersPayments(context);
    
      if(!string.IsNullOrEmpty(errMsg))
      {
        context.Message.Message = errMsg;
        throw new Exception(errMsg);     
      }  
      
      return HandlerResult.Handled;
    }
    
    


    /// <param name="context"></param>
    [UUID("c68da931d3784480ba09cd96406d8c0c")]
    virtual public string DoImportOrdersPayments(ConsumeContext<ECOM.ImportOrdersPaymentsRequestCommand> context)
    {
      throw new NotImplementedByDesignException("Metoda adaptera abstrakcyjnego, do przeciążenia");
      return "";
    }
    
    
    


    /// <summary>
    /// Metoda pobiera dane potrzebne do wygenerowania komend na podstawie przekazanego zapytania i generuje komendy eksportu towarów w kanałach sprzedaży w zadanym trybie (konektor, adapter, kanał sprzedaży).
    /// Generuje komendy eksportu towarów dla wszystkich kanałów, objętych podanym trybem i zwraca informację o błędach.
    /// Używana w metodzie ExportInventoryRequest do genrowania komend w schedulerze
    /// </summary>
    /// <param name="sqlString"></param>
    /// <param name="connectorRef"></param>
    /// <param name="expReqMode"></param>
    [UUID("d64b88a962ec438bb010d401560513c9")]
    public static string GenerateExportInventoryCommands(string sqlString, int? connectorRef, RequestMode? expReqMode)
    {
      string message = "";
        
      foreach(var dataRow in CORE.QuerySQL(sqlString))
      {    
        try 
        {
          if(DateTime.Parse(dataRow["SENDINVENTORYNEXTTIME"]) < DateTime.Now)            
          { 
            //uruchamiamy wspólną metodę generującą komendy eksportu dla wybranego kanału
            message += LOGIC.ECOMCHANNELS.SendInventory(dataRow["CHANNEL"].AsInteger, ExportMode.LastChange) + "\n";
           
            if(expReqMode == RequestMode.Connector)
            {
              // rozwiązanie tymczasowe - czekamy na dokończenie projektu EDA przez ZRT
              CORE.RunSQL(
                "update SYS_EDACONNECTORPARAMS " + 
                    $"set NVALUE = dateadd(0{dataRow["SENDINVENTORYINTERVAL"]} minute to current_timestamp) " +
                    $"where EDACONNECTORREF = 0{connectorRef} and NKEY = 'autosendinventorynexttime' ");
            }             
          }
          else
          {
            message += $"Nie wygenerowano komendy aktualizacji towarów dla kanału: {dataRow["CHANNEL"]}\n" ;
          }    
        } 
        catch(Exception ex)
        {        
          message += $"Błąd komendy aktualizacji towarów dla kanału: {dataRow["CHANNEL"]}: {ex.Message}\n";
          continue;    
        }        
      }     
      return message;
    }
    
    


    /// <param name="context"></param>
    [UUID("da2b428c09d8413daab623d8dbd93cbd")]
    virtual public string DoExportInventoryStock(ConsumeContext<ECOM.ExportInventoryStocksCommand> context)
    {
      throw new NotImplementedByDesignException("Metoda adaptera abstrakcyjnego, do przeciążenia");
      return "";
    }
    
    
    


    /// <summary>
    /// Metoda logiki pobierająca z bazy informacje na potrzeby mechanizmu eksportu zamówień z reguły schedulera (TODO).
    /// Użyta w metodzie interfejsowej UpdateOrdersRequest
    /// parametry:
    ///  - sqlString - select dla bazy danych, na podstawie którego pobierane są dane dla importu zamówień
    /// zwraca:
    ///  dane potrzebne do zainicjoawania eksportu zamówienia: symbol i grupę konektora, kiedy zamowienia były eksportowane i interwał eksportu
    /// </summary>
    /// <param name="sqlString"></param>
    [UUID("daf6fef1a61848089881eb9f4c024c3f")]
    public static List<ExportOrdersDataRow> GetExportOrdersData(string sqlString)
    {
      //[ML]Prawdopodobnie do refaktoryzacji po ostatnich poprawkach, ale nieskasowana, bo może
      //się przydać
      
      var result = new List<ExportOrdersDataRow>();
    
      foreach(var dataRow in CORE.QuerySQL(sqlString))
      {
        var resultRow = new ExportOrdersDataRow(); 
        resultRow.orderRef = dataRow["REF"].AsInteger;
        resultRow.connectorSymbol = dataRow["SYMBOL"].AsString;
        resultRow.connectorGroupName = dataRow["GROUPNAME"].AsString;
        resultRow.channelRef = dataRow["CHANNEL"].AsInteger;
        result.Add(resultRow);  
      }
      
      return result;
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("f33eac8640754985a57694a47fa03f40")]
    virtual public HandlerResult GetPricesListCommandHandler(ConsumeContext<ECOM.GetPricesListCommand> context)
    {
      var result = DoGetPricesList(context);
      context.SetResult("Result", result);
      return HandlerResult.Handled;
    }
    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("caf7bfeebbeb4eb7a859235634c64a59")]
    virtual public HandlerResult ImportOrderPackagesRequestCommandHandler(ConsumeContext<ECOM.ImportOrderPackagesRequestCommand> context)
    {
      string errMsg = DoImportOrderPackages(context);
    
      if(!string.IsNullOrEmpty(errMsg))
      {
        context.Message.Message = errMsg;
        throw new Exception(errMsg);     
      }  
      
      return HandlerResult.Handled;
    }
    /// <param name="context"></param>
    [UUID("439f67b1370e42b3875a04081b634fc3")]
    virtual public string DoImportOrderPackages(ConsumeContext<ECOM.ImportOrderPackagesRequestCommand> context)
    {
      throw new NotImplementedByDesignException("Metoda adaptera abstrakcyjnego, do przeciążenia");
      return "";
    }
    
    


  }
}
