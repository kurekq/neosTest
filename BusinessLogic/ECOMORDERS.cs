
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
  
  public partial class ECOMORDERS<TModel> where TModel : MODEL.ECOMORDERS, new()
  {

    /// <param name="nagzamRef"></param>
    [UUID("0337ee8003a94eda98be228b11f0b4a8")]
    virtual public bool IsOrderInvoicesExists(int nagzamRef)
    {
      var invoice = CORE.QuerySQL(
              @"select first 1 distinct nf.ref, nf.akceptacja
                from nagzam nz
                join dokumnag dn on dn.zamowienie = nz.ref
                join nagfak nf on (nf.refdokm = dn.ref or dn.faktura = nf.ref or
                  nf.fromnagzam = nz.ref or nz.faktura = nf.ref) " +
                $"where (nz.ref = {nagzamRef} or nz.org_ref = {nagzamRef})").FirstOrDefault();
    
      if(invoice != null)
      {          
        if(invoice["AKCEPTACJA"].AsInteger == 1)
        {
          return true;
        }
      }
      
      return false;
    }
    
    
    


    /// <param name="connector"></param>
    [UUID("16ca8433aff148d0afe8461e9e423430")]
    public static string GetAdapterForConnector(int connector)
    {
      return CORE.GetField("ADAPTER",$"SYS_EDACONNECTORS where ref = 0{connector}");
    }
    
    


    /// <param name="channelRef"></param>
    [UUID("25f9c77e87a64861bce81e40c817980b")]
    virtual public List<Int64> GetOrdStatusToExport(int channelRef)
    { 
      string sqlString = @"select ref
                            from ecomorders " + 
                          $"where syncsendchanellstatus = {(int)EcomSyncStatus.ExportPending} and ecomchannelref = {channelRef}";
    
      var result = new List<Int64>();
      foreach(var dataRow in CORE.QuerySQL(sqlString))
      {
        var ordRef = dataRow["REF"].AsInteger;
        result.Add(ordRef);
      } 
      return result;
    }
    
    
    


    /// <param name="ecomChannelRef"></param>
    /// <param name="ecomOrderStatus"></param>
    [UUID("296947e42d38425cb30cfe0ec57ab391")]
    public static GetEDAIdentifierData GetEDAID(int ecomChannelRef, EcomOrderStatusInfo ecomOrderStatus)
    {
      string EDAOrderId = "";
      string errorMsg = "";  
      try
      {    
        ECOM.ECOMSTDMETHODS methods = LOGIC.ECOMCHANNELS.FindMethodObject(ecomChannelRef);
        if(methods != null)
          EDAOrderId =  methods.Logic.GetOrderEDAID(ecomChannelRef,ecomOrderStatus);
      }
      catch(Exception ex)
      {
        if(ecomOrderStatus.OrderId != null)
          errorMsg = ($"Błąd generowania identyfikatora EDA dla statusu zamówienia o id {ecomOrderStatus.OrderId}, w kanale sprzedaży {ecomChannelRef}: {ex.Message}");
        else
          errorMsg = ($"Błąd generowania identyfikatora EDA dla statusu nieokreślonego zamówienia, w kanale sprzedaży {ecomChannelRef}: {ex.Message}");
          
        return new GetEDAIdentifierData(){Result = false, ErrorMsg = errorMsg, EDAIdentifier = null};
      }    
      return new GetEDAIdentifierData(){Result = true, ErrorMsg = "", EDAIdentifier = EDAOrderId};
    
    }
    
    


    /// <summary>
    /// Metoda zwraca ostatnio wybrane daty podczas pobierania zamówień w trybie z zakresem dat. 
    /// </summary>
    /// <param name="mode"></param>
    [UUID("43d777486c30465a9a0b9e899040780d")]
    virtual public string GetDateRange(int mode)
    {
        //metoda tymczasowa, do refaktoryzacji po zakończeniu projektu "dokończenia EDA" przez ZRT
        string dat = "";
        dat = mode == 1 ?  CORE.GetField("nvalue","sys_edaconnectorstates where nkey = 'daterangebegin'") : CORE.GetField("nvalue","sys_edaconnectorstates where nkey = 'daterangeend'"); 
        return dat;
    }
    
    


    /// <summary>
    /// Metoda uaktualnia ostatnio wybrane daty podczas pobierania zamówień z opcją zakresu dat.
    /// Wywoływana przez metodę GetOrdersDateRange.
    /// </summary>
    /// <param name="orderDateBeginRangeUpdate"></param>
    /// <param name="orderDateEndRangeUpdate"></param>
    [UUID("43e9c54d681b4b9197122c514ac9e161")]
    virtual public void UpdateDateRange(string orderDateBeginRangeUpdate, string orderDateEndRangeUpdate)
    {
    	try
    	{
    		CORE.RunSQL(orderDateBeginRangeUpdate);
    		CORE.RunSQL(orderDateEndRangeUpdate);
    	}   
    	catch(Exception ex)
    	{
    		throw new Exception(ex.Message);
    	} 
    }
    
    


    /// <summary>
    /// Handler wykonywany jest po wprowadzeniu zmiany na tabeli NAGZAM (obsługa zdarzenia z bazy danych).
    /// Uruchamia komendę CheckOrderChangedCommand z obługą deduplikacji;
    /// 
    /// </summary>
    /// <param name="context"></param>
    [CustomData("MessageHandlerType=E")]
    [UUID("6baf6fbb63e6482d9b639fe1c283e035")]
    virtual public HandlerResult NagzamHeadChangedHandler(ConsumeContext<ECOM.NagzamHeadChanged> context)
    {
      var command = new CheckOrderChangedCommand()     
      {      
        OrderRef = context.Message.NagzamRef
      };  
      command.SetDeduplicationIdentifier("NAGZAM" + context.Message.NagzamRef.ToString());
      EDA.SendCommand("ECOM.ECOMORDERS.CheckOrderChangedCommandHandler",command);  
      return HandlerResult.Handled;
    }
    
    


    /// <param name="ecomChannelRef"></param>
    /// <param name="ecomOrder"></param>
    [UUID("80c6462feee94866b2148ed4e783979d")]
    public static GetEDAIdentifierData GetEDAID(int ecomChannelRef, EcomOrderInfo ecomOrder)
    {
    
      string EDAOrderId = "";
      string errorMsg = "";  
      try
      {    
        ECOM.ECOMSTDMETHODS methods = LOGIC.ECOMCHANNELS.FindMethodObject(ecomChannelRef);
        if(methods != null)
          EDAOrderId =  methods.Logic.GetOrderEDAID(ecomChannelRef,ecomOrder);
      }
      catch(Exception ex)
      {
        if(ecomOrder.OrderId != null)
          errorMsg = ($"Błąd generowania identyfikatora EDA dla zamówienia o id {ecomOrder.OrderId}, w kanale sprzedaży {ecomChannelRef}: {ex.Message}");
        else if(ecomOrder.NagzamRef > 0)  
          errorMsg = ($"Błąd generowania identyfikatora EDA dla zamówienia o NagzamREf {ecomOrder.NagzamRef}, w kanale sprzedaży {ecomChannelRef}: {ex.Message}");
        else
          errorMsg = ($"Błąd generowania identyfikatora EDA dla nieokreślonego zamówienia, w kanale sprzedaży {ecomChannelRef}: {ex.Message}");
          
        return new GetEDAIdentifierData(){Result = false, ErrorMsg = errorMsg, EDAIdentifier = null};
      }    
      return new GetEDAIdentifierData(){Result = true, ErrorMsg = "", EDAIdentifier = EDAOrderId};
    
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("bae031507f60438d975669d630d41a58")]
    [Wait(5000)]
    virtual public HandlerResult CheckOrderChangedCommandHandler(ConsumeContext<ECOM.CheckOrderChangedCommand> context)
    {
        if (context.ShouldIgnore()) //Inaczej: consumeContext.SentTime < consumeContext.GetLastExecutionStartTime();
      {
        return HandlerResult.Ignored; // Wychodzimy z wartością Ignored jeśli komunikat jest nadmiarowy.
      }
      else
      {
        LOGIC.ECOMORDERS.CheckOrderChanged(context.Message.OrderRef);
        context.Message.Message = "Uruchomiono handler CheckOrderChangedCommandHandler. Aktualizacja zamówień w kanale sprzedaży niezaimplementowana"; 
        return HandlerResult.Handled;
      }
    }
    
    


    /// <param name="OrderRef"></param>
    /// <param name="channelRef"></param>
    [UUID("c09ac829b8d64fa8b45eece94f11fc7c")]
    public static void CheckOrderChanged(int OrderRef, int? channelRef = null)
    {
        //[ML] TODO: metoda ma wyliczać sumę kontrolną zamówienia w każdym kanale sprzedaży i jeżeli
        //sie zmieniła, to oznaczać je do eksportu w danym kanale, analogicznie do eksportu towa
        //rów  
    }
    
    


    /// <summary>
    /// Hanlder zdarzenia uruchamianego spod bazy danych na zmianę statusu zamówienia. Wysyła komendę CheckOrderStatusChangedCommand na kolejkę logiczną
    /// </summary>
    /// <param name="context"></param>
    [CustomData("MessageHandlerType=E")]
    [UUID("cdab6a645ca1497a9c9d7071e41af088")]
    virtual public HandlerResult NagzamStatusChangedHandler(ConsumeContext<ECOM.NagzamStatusChanged> context)
    {  
      var command = new CheckOrderStatusChangedCommand();  
      command.OrderParams = context.Message;
      //wysyłamy zmianę statusu zamówienia na kolejkę logiczną per zamówienie, zeby wymusić wysyłkę po kolei
    
      command.SetLogicalQueueIdentifier($"ORDER_{context.Message.NagzamRef}"); 
      EDA.SendCommand("ECOM.ECOMORDERS.CheckOrderStatusChangedCommandHandler",command);
      return HandlerResult.Handled; 
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("d2c35f852b934cf587a7515802c5e5a7")]
    virtual public HandlerResult PrintOrderInvoiceCommandHandler(ConsumeContext<ECOM.PrintOrderInvoiceCommand> context)
    {
      string msg = ""; 
      var nagfak = new ECOM.NAGFAK();
      nagfak.FilterAndSort($"{nameof(ECOM.NAGFAK)}.{nagfak.REF.Symbol} = 0{context.Message.NagfakRef}");
      if(nagfak.FirstRecord())  
      {
        try
        {
          if(nagfak.AKCEPTACJA != 1)
          {
            throw new Exception("Nie można wygenerować niezaakceptowanej faktury.");
          }
          var proReport = new Neos.ProReport.PDFPrinter.ProReport();         
          string refFak = nagfak.REF.ToString();
          string refKlient = nagfak.KLIENT.ToString();
          string pdf = "1";
          string ilkop = "0"; 
          string duplikat = "";
          string duplikatdat = "";            
          string korekta = nagfak.KOREKTA.ToString();
          string invoicePath = context.Message.InvoicePath;
          string drukNazwa = "fakturanew";            
          if(refFak != "0")          
          {
            var printAction = new Neos.ProReport.PDFPrinter.PrintAction(drukNazwa)
            .AddParameter("RPRT_NAGFAK", refFak)
            .AddParameter("UZYKLI_KLIENT_ID", refKlient)
            .AddParameter("ILKOP", ilkop)
            .AddParameter("DUPLIKAT", duplikat)
            .AddParameter("DUPLIKATDAT", duplikatdat)
            .AddParameter("PDF", pdf);
            printAction.OutputPath = invoicePath;                              
            printAction.OutputFile = nagfak.REF.ToString();                 
           
       
            printAction.OnError += (sender, args) => 
            {   
              msg = "Wystąpił błąd podczas generowania faktury. ";
              throw new Exception(msg);
            };
          
            proReport.PrintPDF(printAction, false);   
          }
        }   
        catch(Exception ex)
        {
          throw new Exception(msg + " " + ex);
        }
        
      }
      else
      {
        msg = "Wystąpił błąd podczas generowania faktury. Nie znaleziono faktury o ref: " + context.Message.NagfakRef;
        throw new Exception(msg);
      }
      nagfak.Close();
      return HandlerResult.Handled;
    }
    
    


    /// <summary>
    /// Metoda uruchamiana w handlerze komendy CheckOrderChangedCommand, która wykonywana jest w reakcji na eventy zmian na zamówieniu
    /// 
    /// </summary>
    /// <param name="nagzamRef"></param>
    /// <param name="orderRef"></param>
    [UUID("d6052b04baf24ce4886f048922b5c090")]
    virtual public string SendInvoice(int nagzamRef, int orderRef)  
    {
      string msg = "";
      var nagzam = new ECOM.NAGZAM();
      var nagfak = new ECOM.NAGFAK();
      var storage = new ECOM.S_STORAGE();
      nagzam.FilterAndSort($"{nameof(ECOM.NAGZAM)}.{nagzam.REF.Symbol} = 0{nagzamRef}");
      if(!nagzam.FirstRecord())
      {
        throw new Exception($"Nie znaleziono zamówienia o REF: {nagzamRef}.");
      } 
      storage.FilterAndSort($"{nameof(ECOM.S_STORAGE)}.{storage.STORAGETYPE.Symbol} = 'CRMINVOICES'");
      if(!storage.FirstRecord()) 
      {
        throw new Exception($"Nie znaleziono ścieżki do faktur.");
      }
      nagfak.FilterAndSort($"{nameof(ECOM.NAGFAK)}.{nagfak.REF.Symbol} = 0{nagzam.FAKTURA}");
      if(nagfak.FirstRecord())  
      {
        var receivedCommand = new NagzamStatusChanged()
        {
          NagzamRef = nagzamRef,
          NagzamRejestr = nagzam.REJESTR,
          NagzamOplacone = nagzam.OPLACONE.AsInteger,
          NagzamAnulowano = nagzam.ANULOWANO.AsInteger,
          NagfakRef = nagfak.REF,
          NagfakAkcept = nagfak.AKCEPTACJA.AsInteger,
          NagfakDataAkcept = nagfak.DATAAKC.AsDateTime,
          NagfakWydrukowano = nagfak.WYDRUKOWANO.AsInteger,
          NagfakSymbol = nagfak.SYMBOL,
          AttachmentPath = storage.HOSTPATH
        };
        EDA.RaiseEvent<NagzamStatusChanged>(receivedCommand);
      }
      else
      {
        msg = "Nie znaleziono faktury dla zamówienia o ref: " + orderRef;
      }
      nagzam.Close();
      nagfak.Close();
      storage.Close();
      return msg;
    
    }
    
    
    


    /// <summary>
    /// Handler wykonywany jest po wprowadzeniu zmiany na tabeli POZZAM(obsługa zdarzenia z bazy danych).
    /// Uruchamia komendę CheckOrderChangedCommand z obługą deduplikacji;
    /// </summary>
    /// <param name="context"></param>
    [CustomData("MessageHandlerType=E")]
    [UUID("df4559c7e9ce4aaf8465ade2e9c95706")]
    virtual public HandlerResult NagzamPosChangedHandler(ConsumeContext<ECOM.NagzamPosChanged> context)
    {
      var command = new CheckOrderChangedCommand()     
      {      
        OrderRef = context.Message.NagzamRef
      };  
      command.SetDeduplicationIdentifier("NAGZAM" + context.Message.NagzamRef.ToString());
      EDA.SendCommand("ECOM.ECOMORDERS.CheckOrderChangedCommandHandler",command);  
      return HandlerResult.Handled;
    }
    
    


    /// <param name="refKey"></param>
    [UUID("f37fecc8228e460981d1b239acf549d7")]
    virtual public string GetOrderId(int refKey)
    {
        String symbol = CORE.GetField("ECOMORDERID","ECOMORDERS where REF = '" + refKey + "'");
        return symbol;
    }
    
    


    /// <summary>
    /// Hanlder komendy wysyłanej w reakcji na Event zmiany statusu zamówienia. Wyszukuje zamówienie w kanałach sprzedaży, wylicza sumę kontrolną na podstawie danych w komendzie,
    /// porównuje ją z dotychczasową sumą kontrolną. Jeżeli sumy się różnią to aktualizuje sumę zamówienia w ecomorders, wylicza nowy status i uruchamia eksport statusu
    /// </summary>
    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("f65b8dad4bc6441d8b73ffd234185bde")]
    [PhysicalQueueName("ExportOrderStatusQueue")]
    virtual public HandlerResult CheckOrderStatusChangedCommandHandler(ConsumeContext<ECOM.CheckOrderStatusChangedCommand> context)
    {
      //wyszukujemy zamówienia o podanym refie w kanalach sprzedazy dla wybranych nagzamów   
      string sqlString = 
        @"select eord.ref, scon.groupname, scon.symbol, echa.ref channel 
          from ecomorders eord 
            join ecomchannels echa on eord.ecomchannelref = echa.ref
            join sys_edaconnectors scon on scon.ref = echa.connector " +
          $"where eord.nagzamref = 0{context.Message.OrderParams.NagzamRef}";
    
      var results = new List<ExportOrdersDataRow>();
    
      foreach(var dataRow in CORE.QuerySQL(sqlString))
      {
        var resultRow = new ExportOrdersDataRow(); 
        resultRow.orderRef = dataRow["REF"].AsInteger;
        resultRow.connectorSymbol = dataRow["SYMBOL"].AsString;
        resultRow.connectorGroupName = dataRow["GROUPNAME"].AsString;
        resultRow.channelRef = dataRow["CHANNEL"].AsInteger;
        results.Add(resultRow);  
      } 
      if((results?.Count ?? 0) == 0)
      {
        context.Message.Message = "Nie znaleziono zamówienia w kanałach sprzedaży";
      }
      else
      {
        foreach(var result in results)
        {
          try
          {
            //sprawdzam dla danego zamówienia w kanale sprzedaży, czy jego status sie zmienił względem danych w komunikacie
            //jeżeli się zmienił to naliczam OrderStatusInfo do dalszej wysyłki
            CheckOrderStateUpdateResult orderStatusCheck = LOGIC.ECOMCHANNELSTATES.CheckOrderStateUpdate(result.orderRef, context.Message.OrderParams); 
            if(orderStatusCheck.StateHasChanged)
            {
              //wysylam dane na podstawie struktury, a nie zapisu do bazy danych, aby nie pominać żadnego updateu statusu
              LOGIC.ECOMCHANNELS.SendOrdersStatus(result.channelRef, ExportMode.List, null, orderStatusCheck.OrderStatusInfo, 
                context.Message.GetLogicalQueueIdentifier());              
            }
          }
          catch(Exception ex)
          {
            context.Message.Message = ex.Message;
            throw new Exception(context.Message.Message);          
          } 
        }          
      }   
      return HandlerResult.Handled;
    }
    
    


  }
}
