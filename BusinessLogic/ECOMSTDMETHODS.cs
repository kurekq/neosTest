
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
  
  public partial class ECOMSTDMETHODS<TModel> where TModel : MODEL.ECOMSTDMETHODS, new()
  {

    /// <param name="OrderRef"></param>
    [UUID("00b79248b8f3429c8688df9e1fa90df4")]
    virtual public NagzamStatusChanged CalcNagzamStatusChangedFromDB(int OrderRef)
    {
      
      string sqlString = @"select nzam.ref nagzamref, nzam.rejestr nagzamrej, nzam.oplacone nagzamopl, nzam.anulowano nagzamanul
                            from ecomorders eord
                              left join nagzam nzam on nzam.ref = eord.nagzamref " +
                          $"where eord.ref = {OrderRef}";  
      var result = new NagzamStatusChanged();
      var data = CORE.QuerySQL(sqlString).First();
      
      result.NagzamRef = data["NAGZAMREF"].AsInteger;
      result.NagzamRejestr = data["NAGZAMREJ"];
      result.NagzamOplacone = data["NAGZAMOPL"].AsInteger;
      result.NagzamAnulowano = data["NAGZAMANUL"].AsInteger;
      
       
      return result;
    }
    
    
    


    /// <param name="ecomChannelRef"></param>
    /// <param name="orderInfo"></param>
    [UUID("02c2d08d69554c9a8e413f96da313ca4")]
    virtual public void CheckOrderRegister(int ecomChannelRef, EcomOrderInfo orderInfo)
    {
      string orderRegistry = "";
      var ecomOrder = new ECOM.ECOMORDERS();  
      var channelParams = LOGIC.ECOMSTDMETHODS.LoadEcomChannelParams(ecomChannelRef);
        
      try
      {
        //sprawdzamy tylko ja mamy podane id zamówienia
        if(orderInfo.OrderId != null)
        {
          //szukamy, czy wczesniej pobraliśmy to zamówienie i jest w ECOMORDERS
          ecomOrder.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{ecomOrder.ECOMCHANNELREF.Symbol} = 0{ecomChannelRef}  " + 
            $"AND {nameof(ECOM.ECOMORDERS)}.{ecomOrder.ECOMORDERID.Symbol} = '{orderInfo.OrderId}' ");
          //jeśli znajdziemy zamówienie - sprawdzamy w jakim rejestrze się znajduje 
          //jeśli nie - wracamy do metody importu zamówienia  
          if(ecomOrder.FirstRecord())
          {
            if(!String.IsNullOrEmpty(ecomOrder.NAGZAMREF.ToString()))
            {
              var nagzam = new ECOM.NAGZAM();
              nagzam.FilterAndSort($"{nameof(ECOM.NAGZAM)}.{nagzam.REF.Symbol} = 0{ecomOrder.NAGZAMREF} ");
              if(nagzam.FirstRecord())
              {
                orderRegistry = nagzam.REJESTR.ToString();
              }
              if(!String.IsNullOrEmpty(orderRegistry) && orderRegistry != channelParams["_orderregistry"].AsString)
              {
                throw new Exception($"Nie można aktualizować zamówień znajdujących się w rejestrze {orderRegistry}\n");
              }
            }
          }
        }    
      }
      catch(Exception ex)
      {
        throw new Exception(ex.Message);
      } 
    
      ecomOrder.Close();
    }
    
    
    


    /// <summary>
    /// Metoda zwraca klasę EcomOrderStatusInfo zawierającą informacje o aktualnym statusie zamówienia wyliczone na podstawie przekazanych danych o zamówieniu lub
    /// na podstawie danych z bazy
    /// </summary>
    /// <param name="ecomOrderRef"></param>
    /// <param name="orderStatusData"></param>
    [UUID("0480076acf5d49b4942e3fd814a71040")]
    virtual public EcomOrderStatusInfo GetOrderStatusInfo(Int64 ecomOrderRef, NagzamStatusChanged orderStatusData)
    {
      //pobranie danych do struktury EcomOrderStatus info albo z danych NagzamStatusChanged, 
      //albo z danych w bazie danych (aktualny status w bazie danych)
      EcomOrderStatusInfo result = new EcomOrderStatusInfo();
      var ecomOrder = new ECOM.ECOMORDERS();
      ecomOrder.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{ecomOrder.REF.Symbol} = 0{ecomOrderRef}");
      if(!ecomOrder.FirstRecord()) 
      {
        throw new Exception($"Nieznalezione zamówienie w kanale sprzedaży dla REF={ecomOrderRef}");
      }
      result.EcomOrderRef = ecomOrder.REF.AsInt64;
      result.EcomOrderEDAID = ecomOrder.EDAID;
      result.OrderId = ecomOrder.ECOMORDERID.ToString();
      result.OrderSymbol = ecomOrder.ECOMORDERSYMBOL;
        
      //okreslenie statusu w kanale sprzedazy - albo na podstawie danych z zdarzenia, albo na podstawie już zapisanej informacji w ECOMORDER
      var channelState = new ECOM.ECOMCHANNELSTATES();
      var filepath = "";
      var hash = "";
      if(orderStatusData != null)
      {
        //okreslenie statusu zamowienia w kanale sprzedaży za pomocą struktury danych o statusie - wywolanie procedury standardowej okrelsajacej status na podstawie fitrów po statusie
        result.EcomOrderStatusRef =  LOGIC.ECOMCHANNELSTATES.CalcOrderStateInChannel(ecomOrder.ECOMCHANNELREF.AsInteger, orderStatusData);
        //tutaj dodanie ewentualnei innych danych bezpośrednio ze struktury NagzamStatusChanged 
        if(orderStatusData.ListwysdAkcept == 2)
        {
          result.OrderSpedInfo = GetSpedInfo(ecomOrderRef, orderStatusData);
        }  
      
         // Wysyłamy załącznik jeśli faktura jest zaakceptowana   
        if(orderStatusData.NagfakAkcept == 1)
        {
          filepath = System.IO.Path.Combine(orderStatusData.AttachmentPath, orderStatusData.NagfakRef + ".pdf");
          hash = GetFileCheckSum(filepath);
          // Jesli hash pliku różni się z hashem zapisanym w bazie lub jeśli w bazie nie ma hashu pliku to wygeneruj fakturę 
          if(ecomOrder.INVOICEPDFCHECKSUM != hash 
             || String.IsNullOrEmpty(ecomOrder.INVOICEPDFCHECKSUM.ToString()) 
             || (File.Exists(filepath) && (File.GetCreationTime(filepath) < orderStatusData.NagfakDataAkcept)))
          {
            var command = new ECOM.PrintOrderInvoiceCommand()    
            {	
              NagfakRef = orderStatusData.NagfakRef,
              InvoicePath = orderStatusData.AttachmentPath
            };
            EDA.ExecuteCommand("ECOM.ECOMORDERS.PrintOrderInvoiceCommandHandler", command);  
            
            // Generuję hash nowego pliku 
            hash = GetFileCheckSum(filepath);
            result.InvoiceInfo = GetInvoiceInfo(ecomOrderRef, orderStatusData);
            ecomOrder.EditRecord();
            ecomOrder.INVOICEPDFCHECKSUM = hash;
            if(!ecomOrder.PostRecord())
            {
              throw new Exception($"Błąd aktualizacji checksumy faktury zamówienia o REF: {ecomOrder.REF} ");
            }
          }
        }  
      }
      else
      {
        //i tutaj ewentualnie uzupełnienie innych danych     
    	//odczytuje status z ECOMORDERS
        result.EcomOrderStatusRef = ecomOrder.ECOMCHANNELSTATE.AsInteger;
        result.InvoiceInfo = GetInvoiceInfo(ecomOrderRef, orderStatusData);
        if(!String.IsNullOrEmpty(result.InvoiceInfo.NagfakRef))
        {
          var command = new ECOM.PrintOrderInvoiceCommand()    
          {	
            NagfakRef = result.InvoiceInfo.NagfakRef,
            InvoicePath = result.InvoiceInfo.AttachmentPath
          };
          EDA.ExecuteCommand("ECOM.ECOMORDERS.PrintOrderInvoiceCommandHandler", command);  
            
          // Generuję hash nowego pliku 
          filepath = System.IO.Path.Combine(result.InvoiceInfo.AttachmentPath, result.InvoiceInfo + ".pdf");
          hash = GetFileCheckSum(filepath);
          // Przypisuję nowy hash do ECOMORDERS
          ecomOrder.EditRecord();
          ecomOrder.INVOICEPDFCHECKSUM = hash;
          if(!ecomOrder.PostRecord())
          {
            throw new Exception($"Błąd aktualizacji checksumy faktury zamówienia o REF: {ecomOrder.REF} ");
          }  
        }
        
      }
      
      channelState.FilterAndSort($"{nameof(ECOM.ECOMCHANNELSTATES)}.{channelState.REF.Symbol} = 0{result.EcomOrderStatusRef}");
      if(channelState.FirstRecord()) 
      {
        result.OrderStatusId = channelState.SYMBOL;
      }
      else
      {
        result.OrderStatusId = "";
      }
      ecomOrder.Close();
      return result;
    }
    
    
    


    /// <summary>
    /// Główna metoda importu zamówień wykorzystywana w handlerze ImportOrderCommandHandler (ECOMADAPTERIAI). 
    /// Odpowiada za dodawanie i aktualizację zamówień do standardowych tabel Teneum.
    /// kolejno wywołuje metody InsertOrUpdate... zakładające konto internetowe w kanale sprzedaży (ECOMACCOUNTS) i klienta, odbiorcę,
    /// nagłówek i pozycje zamówienia oraz wpłaty do zamówienia, a następnie wywołuje metodę spradzającą, czy zamowienia pobrało sie prawodłowo  
    /// i przenosi je do rejestru prawidłowo pobranych zamówień skonfigurowanego w kanałach sprzedaży.
    /// parametry:
    ///  - ecomChannelRef - ref kanału sprzedaży;
    ///  - orderInfo - dane o zamówieniu do importu/ aktualizacji w formie klasy uniwersalnej EcomOrderInfo
    /// zwraca:
    ///  - komunikaty o błędach, które wystąpiły przt pobieraniu zamówienia
    ///  - ecomChannelRef - ref kanału sprzedaży, do którego dodane będzie zamówienie
    /// zwraca:
    ///  - komunikaty o błędach w pobraniu zamówień 
    /// 
    /// </summary>
    /// <param name="ecomChannelRef"></param>
    /// <param name="orderInfo"></param>
    /// <param name="ecomChannelParams"></param>
    [UUID("136e697680454fd2a8fdaaaf3a6f2d5b")]
    virtual public void ProcessImportOrder(int ecomChannelRef, EcomOrderInfo orderInfo, Contexts ecomChannelParams)
    {
      string errorMsg = "";
      int? clientRef = null;
      int? clientAccountRef = null;
      int? recipientRef = null;
      int? orderRef = null; 
    
      try
      {
        //najpierw sprawdzamy, czy możemy importować zamówienie. Nie możemy jeśli znajduje się w innym rejestrze niż "nowe" 
        CheckOrderRegister(ecomChannelRef, orderInfo);
      }
      catch(Exception ex)
      {
        errorMsg += ex.Message;
      }
      if(string.IsNullOrEmpty(errorMsg))
      {
        try
        {
          //importujemy konto klienta
          clientAccountRef = InsertOrUpdateClientAccount(ecomChannelRef, orderInfo.ClientAccount);
        }
        catch(Exception ex)
        {
          errorMsg += $"Błąd importu konta internetowego: {ex.Message}\n";        
        }
    
        try
        {
          //importujemy/ aktualizujemy klienta
          if(clientAccountRef != null)
          {
            clientRef = InsertOrUpdateClient(ecomChannelRef, orderInfo.BillingAddress, clientAccountRef, ecomChannelParams);
          }    
        }
        catch(Exception ex)
        {
          //errorMsg += $"Błąd importu danych klienta: {ex.Message}\n";
          errorMsg += $"Błąd importu danych klienta: {ex.Message}";     
        }
        
        try
        {
          //potem odbiorcę
          if(clientRef != null)
          {
            recipientRef = InsertOrUpdateRecipient(clientRef, orderInfo.DeliveryAddress);
          }    
        }
        catch(Exception ex)
        {
          errorMsg += $"Błąd importu odbiorcy: {ex.Message}\n";   
        }
        
        try
        {
          //dane nagłówka zamówienia
          orderRef = InsertOrUpdateOrder(ecomChannelRef, clientAccountRef, clientRef, recipientRef, orderInfo, ecomChannelParams);     
        }
        catch(Exception ex)
        {
          errorMsg += "Błąd danych zamówienia: " + ex.Message;    
        } 
    
        //insert pozycji
        try
        {
          if(orderRef != null)
          {
            InsertOrUpdateOrderLines(ecomChannelRef, orderRef, orderInfo, ecomChannelParams);
          }
        }
        catch(Exception ex)
        {
          errorMsg += "Błąd danych pozycji zamówienia: " + ex.Message;
          
        }
    
        //płatności do zamówienia
        try
        {
          if(orderRef != null)
          {
            InsertOrUpdatePrepaids(ecomChannelRef, orderRef, clientRef, orderInfo);
          }
        }
        catch(Exception ex)
        {
          errorMsg += "Błąd danych przedpłat dla zamówienia: " + ex.Message;    
        }
    
        //jeżeli udało się pobrać/ zaktuzlizować wszystkie dane, to robimy walidację 
        //na podstawie tego co jest już w bazie  
        try
        {
          if(clientRef != null && clientAccountRef != null && recipientRef != null && orderRef != null
            && string.IsNullOrEmpty(errorMsg))
          { 
            var orderData = new ECOM.NAGZAM();
            orderData.FilterAndSort($"{nameof(ECOM.NAGZAM)}.{orderData.REF.Symbol} = 0{orderRef.ToString()}");
            if(orderData.FirstRecord())
            {
              if(ValidateOrderData(ecomChannelRef, orderRef, orderInfo, ecomChannelParams))
              {        
                //jezeli walidacja jest ok i zamowienie jest w rejestrze początkowym, 
                //to przenosimy z rejestru nowych do pobranych poprawnie   
                if(string.IsNullOrEmpty(ecomChannelParams["_orderregistry"]))
                {
                  errorMsg += "Nie znaleziono rejestru docelowego.";
                }
                else if(orderData.REJESTR == ecomChannelParams["_orderregistrynew"].AsString)
                {                   
                  orderData.REJESTR = ecomChannelParams["_orderregistry"].AsString;
                  if(!orderData.PostRecord())
                  {
                    throw new Exception($"Błąd zapisu rejestru docelowego: {ecomChannelParams["_orderregistry"]} do bazy danych");
                  }	        
                }
                
                
                ECOM.ECOMORDERS ecomOrder = new ECOM.ECOMORDERS();
                ecomOrder.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{ecomOrder.ECOMCHANNELREF.Symbol} = 0{ecomChannelRef.ToString()} " + 
                  $"AND {ecomOrder.ECOMORDERID.Symbol} = '{orderInfo.OrderId}'");
                if(ecomOrder.FirstRecord())
                {
                  //walidacja udana więc oznaczamy flagę  
                  ecomOrder.EditRecord();     
                  ecomOrder.SYNCSTATUS = (int)EcomSyncStatus.Imported;
                  ecomOrder.LASTSYNCERROR = "";                                    
                  if(!ecomOrder.PostRecord())
                  {
                    throw new Exception($"Błąd zapisu aktualizacji statusu zamówienia o REF: {orderInfo.OrderId} do bazy danych");
                  }	
                }
                ecomOrder.Close();
              }
            }
            orderData.Close();   
          }    
        }
        catch(Exception ex)
        {       
          errorMsg += "Błąd walidacji danych zamówienia: " + ex.Message;
        }
      }
      
      if(!string.IsNullOrEmpty(errorMsg))
      {
        //na koniec w przypadku błędu rzucamy wyjątek, żeby cofnąć cały import zamówienia
        throw new Exception(errorMsg);
      }  
    }
    
    


    /// <summary>
    /// Metoda spradza, czy dane zamówienia zaimportowanego do bazy danych są kompletne i zgodne z danymi zamówienia, które przyszły z kanału sprzedaży.
    /// Na tej podstawie zamówienie np. zostaje oznaczone jako zaimportowane poprawnie i przeniesione do rejestru z poprawnie pobranymi zamówieniami.
    /// paremetry:
    ///  - ecomChannel - ref zamówienia w kanale sprzadaży (ECOMORDERS)
    ///  - nagzamRef - ref zamówienia  w tabeli NAGZAM
    ///  - orderInfo - dane zamówienia pobrane z witryny w postaci klasy uniwersalnej EcomOrderInfo
    ///  - ecomChannelParams - lista parametów kanału sprzedażty wykorzystwana przy synchronizacji danych w danym kanale
    /// zwraca:
    ///  - wartość bool oznaczającą, czy walidacja zakończyła się sukcesem 
    /// </summary>
    /// <param name="ecomChannel"></param>
    /// <param name="nagzamRef"></param>
    /// <param name="orderInfo"></param>
    /// <param name="ecomChannelParams"></param>
    [UUID("224a97e96d8848d8ba3c09d4ec012a93")]
    public static bool ValidateOrderData(int ecomChannel, int? nagzamRef, EcomOrderInfo orderInfo, Contexts ecomChannelParams)
    {
      bool isOrderDataValid = true;
      string errorMsg = "";
    
      //pobieramy dane związane z zamówieniem z bazy, żeby mieć pewność, że żadne triggery nic 
      //nie przeliczyły i dane się nie rozjechały
    
      bool deliveryCostSpecified = (orderInfo?.Dispatch?.DeliveryCost ?? 0m) > 0m;
      
      //sprawdzamy pozycje
      //sprawdzamy, czy zgadza się wartość, netto i brutto, towar 
      var orderLines = new List<ECOM.POZZAM>(); 
      string selectLinesForOrder = $"select ref from pozzam where zamowienie = 0{nagzamRef}"; 
      foreach(var orderLinedataRow in CORE.QuerySQL(selectLinesForOrder))
      {
        var orderLine = new ECOM.POZZAM();
        orderLine.FilterAndSort($"{nameof(ECOM.POZZAM)}.{orderLine.REF.Symbol} = 0{orderLinedataRow["REF"]}");
        orderLine.FirstRecord();
    
        int? ecomInventoryRef = new LOGIC.ECOMINVENTORIESGROUPS()
          .WithParameters( new ECOMINVENTORIESGROUPSParameters() { _ecomchannelref = ecomChannel })
          .GetInventoryRef(orderLine.WERSJAREF.AsInteger);
    
        if (ecomInventoryRef == null 
          && ecomChannelParams["_productunknown"].AsInteger != orderLine.WERSJAREF
          && ecomChannelParams["_deliverservice"].AsInteger != orderLine.WERSJAREF)
        {      
          errorMsg += $"Nie odnaleziono towaru: {orderLine.KTM} (id w kan. sprzedazy: {ecomChannel}) \n"; 
        }  
        else 
        {
          ECOM.ECOMINVENTORIES channelInv = new ECOM.ECOMINVENTORIES();
          if (ecomInventoryRef > 0)
          {
            channelInv.FilterAndSort($"{nameof(ECOM.ECOMINVENTORIES)}.{channelInv.REF.Symbol} = {ecomInventoryRef} ");   
            channelInv.FirstRecord(); 
          }
          //UWAGA!!! zaokrąglenie do dwóch miejsc, bo na razie trrigger przelicza ceny
          if(deliveryCostSpecified && orderLine.WERSJAREF == ecomChannelParams["_deliverservice"].AsInteger)
          { 
            //tu jest sprawdzenie, czy zgadza się koszt dostawy brutto
            if(Decimal.Round(orderLine.CENABRU.AsDecimal, 2) != orderInfo.Dispatch.DeliveryCost)
            {
              errorMsg += "Nieprawidłowa wartość pozycji kosztu dostawy\n";
            }      
          }
          else
          {
            if(ecomChannelParams["_productunknown"].AsInteger == channelInv.WERSJAREF)
            {
              errorMsg = "W zamówieniu występuje produkt nieznany.";
            }
            else
            {
            //jeżeli pozycja nie jest kosztem dostawy, to sprawdzamy, czy zgadza się towar, ilość, ceny
            var wyn = (from l in orderInfo.OrderLines 
              where l.ProductId == channelInv.ECOMINVENTORYID
                  && l.ProductQuantity == orderLine.ILOSC.AsDecimal
                  && l.ProductOrderPrice == Decimal.Round(orderLine.CENABRU.AsDecimal, 2)
                  && l.ProductOrderPriceNet == Decimal.Round(orderLine.CENANET.AsDecimal, 2)
                select l).FirstOrDefault();
    
              if(wyn == null)
              {
                errorMsg += $"Niezgodność pozycji po przetworzeniu. Brak pozycji z towarem: {orderLine.KTM} (id w kan. sprzedazy: {ecomChannel}) " +
                $"z ilością: {orderLine.ILOSC}, ceną brutto: {orderLine.CENABRU}, ceną netto: {orderLine.CENANET} w pobranych danych zamówienia\n";       
              }
            }        
          }
          orderLines.Add(orderLine);       
        }
        orderLine.Close();
      }  
    
      //sprawdzamy dane nagłówka
      if(orderLines.Count == 0)
      {
        errorMsg += "Zamówienie bez pozycji\n"; 
      }  
      else if((deliveryCostSpecified && orderLines.Count - 1 != orderInfo.OrderLines?.Count)
        || (!deliveryCostSpecified && orderLines.Count != orderInfo.OrderLines?.Count))
      {
        //jeżeli koszt dostawy > 0 to w TD jest dodatkowa pozycja z tym kosztem, jeżeli nie
        //to powinno być tyle samo pozycji
        errorMsg += 
          $"Niezgodna liczba pozycji zamówienia: w kanale sprzedaży: {orderLines.Count}, w Teneum: {orderInfo.OrderLines?.Count} \n";      
      } 
    
      var orderData = new ECOM.NAGZAM();
      orderData.FilterAndSort($"{nameof(ECOM.NAGZAM)}.{orderData.REF.Symbol} = 0{nagzamRef}"); 
      orderData.FirstRecord(); 
      if(string.IsNullOrEmpty(orderData.KLIENT))
      {
        errorMsg += "Brak klienta dla zamówienia \n";
      }
    
      if(string.IsNullOrEmpty(orderData.ODBIORCAID))
      {
        errorMsg += "Brak odbiorcy dla zamówienia \n";
      }
    
      if(string.IsNullOrEmpty(orderData.SPOSDOST)
        || orderData.SPOSDOST != LOGIC.ECOMCHANNELCONVERT.ConvertFromChannelValue(ecomChannel, "SPOSDOST", orderInfo?.Dispatch?.DispatchId))
      {
        errorMsg += "Brak sposobu dostawy dla zamówienia, lub sposób dostawy niezgodny w kanale sprzedaży i w Teneum \n";
      }  
    
      if(string.IsNullOrEmpty(orderData.SPOSZAP)
        || orderData.SPOSZAP != LOGIC.ECOMCHANNELCONVERT.ConvertFromChannelValue(ecomChannel, "PLATNOSCI", orderInfo?.Payment?.PaymentType))    
      {
        errorMsg += "Brak sposobu płatności dla zamówienia, lub sposób płatności niezgodny w kanale sprzedaży i w Teneum \n";
      }
      orderData.Close();
      
      if(!string.IsNullOrEmpty(errorMsg))
      {        
        throw new Exception(errorMsg);
      }
    
      return true;
    }
    
    


    /// <summary>
    /// Metoda pobiera z bazy dane, którymi są uzupełniane parametry na oknie atrybutów kanału spzredaży
    /// </summary>
    /// <param name="ecomChannelRef"></param>
    [UUID("2862db4d5feb48a5b695c3fb97e1b404")]
    public static Contexts LoadEcomChannelParams(int ecomChannelRef)
    {
      var c = new Contexts();
      var sqlString = 
        $"select ep.nkey, ep.nvalue from ecomstandardmethodparams ep where ep.ecomchannel = 0{ecomChannelRef.ToString()}";
      foreach(var dataRow in CORE.QuerySQL(sqlString))
      {
        c.Add(dataRow["NKEY"], dataRow["NVALUE"]);   
      }
      //dodajemy ecomchannel osobno, poniważ nie chcemy, żeby zapisywał się w bazie danych, wiec select nic nam nie zwróci
      c.Add("_ecomchannel", ecomChannelRef);  
      return c;
    }
    
    
    


    /// <param name="fullName"></param>
    [UUID("2fb3c21004de44a99e86ee9d32f461ff")]
    public static List<string> FullNameSeparation(string fullName)
    {
      var divitedName = new List<string>();
      var names = fullName.Split(' ');
      foreach(var item in names)
      {
        divitedName.Add(item);
      }
      return divitedName;
    }
    
    
    


    /// <summary>
    /// metoda przyjmuje obiekt EcomInventoryInfo, ktm i string attributesToSend zawierający
    ///  listę pól do wysyłki na witrynę (bo przy aktualizacji lista póla do wysłki może być inna niż przy dodawaniu)
    ///   i uzupełnia EcomInventoryInfo danymi o jednostakch pobranymi z TOWJEDN
    /// </summary>
    /// <param name="ecomChannelRef"></param>
    /// <param name="ktm"></param>
    /// <param name="inventoryInfo"></param>
    /// <param name="attributesToSend"></param>
    [UUID("325113c9131b4e60b2eafadd35255c99")]
    virtual public void GetUnitsForInventoryInfo(int ecomChannelRef, string ktm, EcomInventoryInfo inventoryInfo, string attributesToSend)
    {
      //metoda przyjmuje obiekt EcomInventoryInfo, ktm i string attributesToSend zawierający
      //listę pól do wysyłki na witrynę (bo przy aktualizacji lista póla do wysłki może być inna niż przy dodawaniu)
      //i uzupełnia EcomInventoryInfo danymi o jednostakch pobranymi z TOWJEDN
      try
      {
        if(!attributesToSend.Contains("[Units]"))
        {
          return;
        }
    
        if (!inventoryInfo.WersjaRef.HasValue)
        {
          throw new Exception($"Nie znaleziono wersji dla towaru o ktm: {ktm}");
        }
        
        var unit = new ECOM.TOWJEDN();
        unit.FilterAndSort($"{nameof(ECOM.TOWJEDN)}.{unit.KTM.Symbol} = '{ktm}'");
        if(unit.FirstRecord())
        {
          inventoryInfo.Units = new List<EcomInventoryUnitInfo>();
          do
          {
            string newEAN = null;
            if (attributesToSend.Contains("[Units].[EAN]"))
            {
              var towkodkresk = new ECOM.TOWKODKRESK();
              towkodkresk.FilterAndSort($@"WERSJAREF = {inventoryInfo.WersjaRef} and TOWJEDNREF = {unit.REF}", "GL desc");       
              if (towkodkresk.FirstRecord())
              {
                newEAN = towkodkresk.TOWJEDNREF.ToString();
              }
            }
    
            var inventoryUnit = new ECOM.EcomInventoryUnitInfo()
            {
              UnitId = !attributesToSend.Contains("[Units].[UnitId]") || string.IsNullOrEmpty(unit.JEDN) ?
                null : LOGIC.ECOMCHANNELCONVERT.ConvertToChannelValue(ecomChannelRef,
                "MIARA", unit.JEDN),                       
              Factor = !attributesToSend.Contains("[Units].[Factor]") || string.IsNullOrEmpty(unit.PRZELICZ) ?
                null as decimal? : unit.PRZELICZ?.AsDecimal, 
              Weight = !attributesToSend.Contains("[Units].[Weight]") || string.IsNullOrEmpty(unit.WAGA) ?  
                null as decimal? : unit.WAGA?.AsDecimal, 
              Height = !attributesToSend.Contains("[Units].[Height]") || string.IsNullOrEmpty(unit.WYS) ?
                null as decimal? : unit.WYS?.AsDecimal,  
              Width = !attributesToSend.Contains("[Units].[Width]") || string.IsNullOrEmpty(unit.DLUG) ?
                null as decimal? : unit.DLUG?.AsDecimal,
              Length = !attributesToSend.Contains("[Units].[Length]") || string.IsNullOrEmpty(unit.SZER) ?
                null as decimal? : unit.SZER?.AsDecimal,                          
              IsBaseUnit = !attributesToSend.Contains("[Units].[IsBaseUnit]") || string.IsNullOrEmpty(unit.GLOWNA) ?
                null as bool? : unit.GLOWNA == "1",
              EAN = newEAN                         
            };
              inventoryInfo.Units.Add(inventoryUnit);
    
          } while (unit.NextRecord());
          
        }
        else
        {
          throw new Exception($"Nie znaleziono jednostek dla towaru o ktm: {ktm}");
        }
        unit.Close();
      }
      catch(Exception ex)
      {
        throw new Exception($"Błąd dodawania jednostek dla towaru o ktm: {ktm} \n {ex.Message}");        
      }
     
    }
    
    
    


    /// <summary>
    ///  metoda przyjmuje obiekt EcomInventoryInfo, ktm i string attributesToSend zawierający
    ///   listę pól do wysyłki na witrynę (bo przy aktualizacji lista póla do wysłki może być inna niż przy dodawaniu)
    ///   i uzupełnia EcomInventoryInfo danymi o zdjęciach pobranymi z TOWPLIKI
    /// </summary>
    /// <param name="ecomChannelRef"></param>
    /// <param name="ktm"></param>
    /// <param name="inventoryInfo"></param>
    /// <param name="attributesToSend"></param>
    [UUID("420a8d2b1792436b8f0589bf2d078732")]
    virtual public void GetImagesForInventoryInfo(int ecomChannelRef, string ktm, EcomInventoryInfo inventoryInfo, string attributesToSend)
    {
      //metoda przyjmuje obiekt EcomInventoryInfo, ktm i string attributesToSend zawierający
      //listę pól do wysyłki na witrynę (bo przy aktualizacji lista póla do wysłki może być inna niż przy dodawaniu)
      //i uzupełnia EcomInventoryInfo danymi o zdjęciach pobranymi z TOWPLIKI
      try
      {
        //zdjęcia
        if(!attributesToSend.Contains("[Images]"))
        {
          return;
        }
    
        var repository = new ECOM.S_STORAGE();
        repository.FilterAndSort($"{nameof(ECOM.S_STORAGE)}.{repository.LOCATION.Symbol} = 0 " + 
          $"AND {nameof(ECOM.S_STORAGE)}.{repository.STORAGETYPE.Symbol} = 'TOWPLIKI'");
        if(!repository.FirstRecord())  
        {
          throw new Exception("Nie znaleziono repoytorium dla zdjęć");
        }  
    
        inventoryInfo.Images = new List<EcomInventoryImageInfo>();
    
        string selectSql = 
        $"select numer numer, sciezka sciezka from xk_towary_get_files('{ktm}', null, 0, 1)";   
    
        foreach(var image in CORE.QuerySQL(selectSql))
        {           
          string picturePath = null;
          if(attributesToSend.Contains("[Images].[PictureSource]"))
          {
            picturePath = System.IO.Path.Combine(repository.HOSTPATH.ToString().TrimEnd('\\'), image["SCIEZKA"].ToString().Trim('\\'));
          }
          
          var inventoryImage = new ECOM.EcomInventoryImageInfo()
          {
            InventoryId = attributesToSend.Contains("[Images].[InventoryId]") ? inventoryInfo.InventoryId : null, 
            WersjaRef = attributesToSend.Contains("[Images].[WersjaRef]") ? inventoryInfo.WersjaRef : null as int?, 
            PictureSource = picturePath, 
            PictureSourceType = attributesToSend.Contains("[Images].[PictureSourceType]") ? "base64" : null, 
            PictureNumber = attributesToSend.Contains("[Images].[PictureNumber]") ? image["NUMER"].AsInteger : null as int?         
          };
          inventoryInfo.Images.Add(inventoryImage); 
        }
        repository.Close();
      }
      catch(Exception ex)
      {
        throw new Exception($"Błąd dodawania zdjęć dla towaru: {ktm}\n {ex.Message}");        
      }
    }
    
    
    


    /// <summary>
    /// Metoda w zależności od konfiguracji kanału sprzedaży zakłada lub wyszukuje i akktualizuje dane klienta
    /// i w razie potrzeby spina go z kontem internetowym dla danego kanału sprzedaży. 
    /// 
    /// parametry:
    ///  - ecomChannel - ref kanału sprzedaży
    ///  - clientBillingAddress - dane do adresowe rozliczeń w formie klasy uniwersalnej
    ///  - clientAccountRef - ref konta interentowego, z którym klient będzie powiązany
    /// zwraca:
    ///  - ref założonego lub znalezionego klienta 
    /// 
    /// 
    /// 
    /// </summary>
    /// <param name="ecomChannel"></param>
    /// <param name="clientBillingAddress"></param>
    /// <param name="clientAccountRef"></param>
    /// <param name="ecomChannelParams"></param>
    [UUID("4fcf3c14d1dc4510a8345c6b21c39044")]
    public static int InsertOrUpdateClient(int ecomChannel, EcomAddressInfo clientBillingAddress, int? clientAccountRef,
      Contexts ecomChannelParams)
    {  
      int company = LOGIC.ECOMSTDMETHODS.GetCompanyFromEcomChannel(ecomChannel);
      bool isFirm = false;   
      //sparwdzamy czy konto ma dopiętego klienta
      ECOM.ECOMACCOUNTS clientAccount = new ECOM.ECOMACCOUNTS();
      clientAccount.FilterAndSort($"{nameof(ECOM.ECOMACCOUNTS)}.{clientAccount.REF.Symbol} = 0{clientAccountRef}");
      clientAccount.FirstRecord();  
    
      isFirm = !string.IsNullOrEmpty(clientBillingAddress?.FirmName);
    
      // sprawdzenie czy pole klient detaliczny w ecomchannels jest uzupelnione  
      COMMON.KLIENCI clientData = new COMMON.KLIENCI();  
      
      if(!isFirm)
      {
        if(!string.IsNullOrEmpty(clientBillingAddress?.FullName))
        {
          // jeśli dostajemy imie i nazwisko w jednej zmiennej należy je rozdzielić
          var divitedName = FullNameSeparation(clientBillingAddress.FullName);
          if(divitedName.Count() < 2)
          {
            throw new Exception("Brak nazwiska dla osoby prywatnej");
          }
          foreach(var item in divitedName)
          {
            if(divitedName.LastOrDefault() == item)
            {
              clientBillingAddress.LastName = item;
            }
            else
            {
              clientBillingAddress.FirstName += item + " ";
            }
          }
        }
        
        if(string.IsNullOrEmpty(clientBillingAddress?.FirstName) && string.IsNullOrEmpty(clientBillingAddress?.FullName))
        {
          throw new Exception("Brak imienia dla osoby prywatnej");
        }
        
        if(string.IsNullOrEmpty(clientBillingAddress?.LastName) && string.IsNullOrEmpty(clientBillingAddress?.FullName))
        {
          throw new Exception("Brak nazwiska dla osoby prywatnej");
        }
        
        if(string.IsNullOrEmpty(clientAccount?.EMAIL))
        {
          throw new Exception("Brak email dla osoby prywatnej");
        }
      }
      else
      {
        if(string.IsNullOrEmpty(clientBillingAddress?.Nip))
        {      
          throw new Exception("Brak nip dla firmy");
        }
      }  
    
      //sprawdzamy, czy do znalezionego konta klienta dopięty jest wpis w klienci
      //jak nie ma to szukamy klienta po nipie dla firm  
      //i po email, telefonie i nazwisku dla prywatnych
    
      clientData.FilterAndSort($"{nameof(COMMON.KLIENCI)}.{clientData.REF.Symbol} = 0{clientAccount.KLIENTREF}");
      clientData.FirstRecord();
      if(string.IsNullOrEmpty(clientAccount.KLIENTREF))
      {
        //klient nie jest połączony z danymi konta, więc szukamy pasującego klienta
        if(isFirm)
        {
          //po nipie dla firm
          clientData.FilterAndSort($"{nameof(COMMON.KLIENCI)}.{clientData.FIRMA.Symbol} = 1 AND {nameof(COMMON.KLIENCI)}.{clientData.PROSTYNIP.Symbol} = '{clientBillingAddress.Nip}'");
          clientData.FirstRecord();
          //clientData.FindRecord("FIRMA;PROSTYNIP", $"1;{clientBillingAddress.Nip}");  
        }
        else if(!string.IsNullOrEmpty(ecomChannelParams["_retailclient"]))
        {
          //jak mamy klienta detalicznego to go dodajemy     
          clientData.FilterAndSort($"{nameof(COMMON.KLIENCI)}.{clientData.REF.Symbol} = 0{ecomChannelParams["_retailclient"]}");
          clientData.FirstRecord();
          //clientData.FindRecord("REF", $"{ecomChannelParams["_retailclient"]}");  
        }
        else
        {
          //po email, telefonie i nazwisku dla prywatnych, sprawdznie czy są podane email i nazwisko jest wyżej
          clientData.FilterAndSort($"{nameof(COMMON.KLIENCI)}.{clientData.FIRMA.Symbol} = 0 " + 
            $"AND {nameof(COMMON.KLIENCI)}.{clientData.NAZWISKO.Symbol} = '{clientBillingAddress.LastName}' " +
            $"AND {nameof(COMMON.KLIENCI)}.{clientData.EMAIL.Symbol} = '{clientBillingAddress.Email}'");
          clientData.FirstRecord();
          //clientData.FindRecord("FIRMA;NAZWISKO;EMAIL", 
            //$"0;{clientBillingAddress.LastName};{clientAccount.EMAIL}");  
        }
      }
      else
      {
        //pobieramy dane powiązanego klienta
        clientData.FilterAndSort($"{nameof(COMMON.KLIENCI)}.{clientData.REF.Symbol} = 0{clientAccount.KLIENTREF}");
        clientData.FirstRecord();
        ///clientData.FindRecord("REF", clientAccount.KLIENTREF);
      }
      
      //nie aktualizujemy klienta detalicznego
      if((ecomChannelParams["_retailclient"].AsInteger != clientData.REF))
      {
        //jezeli nie znaleziono klienta, to trzeba go dodać
        if(string.IsNullOrEmpty(clientData.REF))
        {    
          clientData.NewRecord();
          clientData.FIRMA = isFirm ? 1 : 0;
          clientData.NAZWA = isFirm ? 
          clientBillingAddress.FirmName : clientBillingAddress.FirstName + " " + clientBillingAddress.LastName;
          clientData.FSKROT = clientData.NAZWA.Substring(0, Math.Min(clientData.NAZWA.Length, 40));
          clientData.IMIE = clientBillingAddress.FirstName;
          clientData.NAZWISKO = clientBillingAddress.LastName;
          clientData.NIP = clientBillingAddress.Nip;
          clientData.ULICA = clientBillingAddress.Street;
          clientData.KODP = clientBillingAddress.ZipCode;
          clientData.KRAJ = clientBillingAddress.CountryName;
          clientData.KRAJID = clientBillingAddress.CountryId;
          clientData.TELEFON = clientBillingAddress.Phone;
          clientData.EMAIL = clientAccount.EMAIL;
          clientData.ZAGRANICZNY = clientBillingAddress.CountryId == "PL" ? 0 : 1;
          clientData.ODDZIAL = SessionInfo.GlobalParam["AKTUODDZIAL"].AsString;
          clientData.COMPANY = company;
          clientData.MIASTO = clientBillingAddress.City;
          clientData.NRLOKALU = clientBillingAddress.AppartmentNo;
          clientData.NRDOMU = clientBillingAddress.BuildingNo;    
        }
        else
        {
          clientData.EditRecord();        
          clientData.FIRMA = isFirm ? 1 : 0;
          clientData.NAZWA = isFirm ? 
          clientBillingAddress.FirmName : clientBillingAddress.FirstName + " " + clientBillingAddress.LastName;
          clientData.FSKROT = clientData.NAZWA.Substring(0, Math.Min(clientData.NAZWA.Length, 40));
          clientData.IMIE = clientBillingAddress.FirstName;
          clientData.NAZWISKO = clientBillingAddress.LastName;
          clientData.NIP = clientBillingAddress.Nip;
          clientData.ULICA = clientBillingAddress.Street;
          clientData.KODP = clientBillingAddress.ZipCode;
          clientData.KRAJ = clientBillingAddress.CountryName;
          clientData.KRAJID = clientBillingAddress.CountryId;
          clientData.TELEFON = clientBillingAddress.Phone;
          clientData.EMAIL = clientAccount.EMAIL;
          clientData.ZAGRANICZNY = clientBillingAddress.CountryId == "PL" ? 0 : 1;
          clientData.MIASTO = clientBillingAddress.City;
          clientData.NRLOKALU = clientBillingAddress.AppartmentNo;
          clientData.NRDOMU = clientBillingAddress.BuildingNo;    
        }  
          
        if(!clientData.PostRecord())
        {
          throw new Exception($"Błąd zapisu klienta o REF {clientData.REF} do bazy danych");
        }
      }
      //jeżeli zmienił się klient przypisany do konta klienta
      if(clientAccount.KLIENTREF != clientData.REF)
      {
        clientAccount.EditRecord();
        clientAccount.KLIENTREF = clientData.REF;   
        if(!clientAccount.PostRecord())
        {
          throw new Exception($"Błąd zapisu konta klienta o REF {clientData.REF} do bazy danych");
        }
      }
      clientAccount.Close();
      return clientData.REF.AsInteger;
    }
    
    


    /// <summary>
    /// Metoda zakłada lub aktualizuje dane o wpłatach do zamówienia. Wykorzystywana jest w metodzie ImportOrderProceed.
    /// parametry:
    ///  - ecomChannelRef - ref kanału sprzedaży
    ///  - orderRef - ref nagłówka zamówienia 
    ///  - clientRef- ref klienta do zamówienia
    /// 
    /// 
    /// 
    /// </summary>
    /// <param name="ecomChannelRef"></param>
    /// <param name="orderRef"></param>
    /// <param name="clientRef"></param>
    /// <param name="orderInfo"></param>
    [UUID("53ddd037798647729b8c81f59d73687d")]
    public static void InsertOrUpdatePrepaids(int ecomChannelRef, int? orderRef, int? clientRef, 
      EcomOrderInfo orderInfo)
    {
      string errorMsg = "";
      if((orderInfo.Prepaids?.Count ?? 0) > 0)
      {  
        var prepaidData = new ECOM.ROZRACHROZ();
        foreach(var prepaid in orderInfo.Prepaids)
        {
          try 
          {
            string paymentDesc = string.IsNullOrEmpty(prepaid.PrepaidId) != true ? $"{prepaid.PrepaidId}" : $"{prepaid.PaymentDate}";       
            string bkAccount = CORE.GetField("KONTOFK", $"SLO_DANE(1,0{clientRef})");   
            prepaidData.FilterAndSort($"{nameof(ECOM.ROZRACHROZ)}.{prepaidData.DOKTYP.Symbol} = 'Z' " + 
              $"AND {nameof(ECOM.ROZRACHROZ)}.{prepaidData.DOKREF.Symbol} = 0{orderRef} " + 
              $"AND {nameof(ECOM.ROZRACHROZ)}.{prepaidData.SYMBFAK.Symbol} = '{paymentDesc}'"); 
     
            if(!prepaidData.FirstRecord()) 
            {
              prepaidData.NewRecord();
              prepaidData.DOKTYP = "Z"; 
              prepaidData.DOKREF = orderRef;
              prepaidData.SLODEF = 1;  //standardowy słownik klientów
              prepaidData.SLOPOZ = clientRef;
              prepaidData.WINIEN = prepaid.Value;
              prepaidData.STABLE = "NAGZAM";
              prepaidData.SREF = orderRef;
              prepaidData.KONTOFK = bkAccount;
              DataFieldValue walutaTemp = LOGIC.ECOMCHANNELCONVERT.ConvertFromChannelValue(ecomChannelRef, "WALUTY", prepaid.Currency);     
              prepaidData.WALUTA = String.IsNullOrEmpty(walutaTemp) ? null : walutaTemp;
              prepaidData.SYMBFAK = paymentDesc;   
              prepaidData.PAYDAY = prepaid.PaymentDate;                     
            } 
            else
            {
              prepaidData.EditRecord();
              prepaidData.DOKREF = orderRef;
              prepaidData.SLODEF = 1; //standardowy słownik klientów
              prepaidData.SLOPOZ = clientRef;
              prepaidData.WINIEN = prepaid.Value;
              prepaidData.STABLE = "NAGZAM";
              prepaidData.SREF = orderRef;
              DataFieldValue walutaTemp = LOGIC.ECOMCHANNELCONVERT.ConvertFromChannelValue(ecomChannelRef, "WALUTY", prepaid.Currency);     
              prepaidData.WALUTA = String.IsNullOrEmpty(walutaTemp) ? null : walutaTemp;    
              prepaidData.PAYDAY = prepaid.PaymentDate;             
            } 
            if(!prepaidData.PostRecord())
            {
              throw new Exception($"Błąd zapisu płatności do zamówienia o REF {orderRef} do bazy danych");
            }   
          }
          catch(Exception ex)
          {
            errorMsg += $"{ex.Message} \n";
            //przy błędzie pomijamy wplatę i robimy kolejną, zeby dostać od razu błedy dla wszystkich wpłat, a nie dla jednej
            continue;      
          }
        }
        prepaidData.Close();
      }
      
      if(!string.IsNullOrEmpty(errorMsg))
      {
        throw new Exception(errorMsg);
      }  
    }
    
    


    /// <param name="inventoryStocksList"></param>
    /// <param name="pinventoryStockInfo"></param>
    /// <param name="command"></param>
    [UUID("5d0c8244660b450bb1bec0278ab92a94")]
    virtual public bool PrepareSendInventoryStocks(List<Int64> inventoryStocksList, EcomInventoryStockInfo pinventoryStockInfo, ExportInventoryStocksCommand command)
    {
      var inventoryStocksInfoList = new List<EcomInventoryStockInfo>();
      //ceny towarów do wysyłki na podstawie listy 
      if(inventoryStocksList != null)
      {
        foreach(var ecomInventoryStockRef in inventoryStocksList)
        {
          var inventoryStockInfo = GetInventoryStockInfo(ecomInventoryStockRef);
          inventoryStocksInfoList.Add(inventoryStockInfo);        
        }
      }
    
      //dodanie przygotowanego wcześniej stanu mag towaru
      if(pinventoryStockInfo != null)
      {
        inventoryStocksInfoList.Add(pinventoryStockInfo);
      }
      command.InventoryStocksInfoList = inventoryStocksInfoList;  
      return true;   
    }
    
    


    /// <param name="inventoryList"></param>
    /// <param name="pinventoryInfo"></param>
    /// <param name="command"></param>
    [UUID("656b3e278ace489e8fac37bc755691a6")]
    virtual public bool PrepareSendInventory(List<Int64> inventoryList, EcomInventoryInfo pinventoryInfo, ExportInventoryCommand command)
    {
      var inventoryInfoList = new List<EcomInventoryInfo>();
      //towary do wysyłkik na podstawie listy 
      if(inventoryList != null)
      {
        foreach(var ecomInventoryREF in inventoryList)
        {
          var inventoryInfo = GetInventoryInfo(ecomInventoryREF);
          inventoryInfoList.Add(inventoryInfo);        
        }
      }
      //dodanie przygotowanego wcześniej towaru
      if(pinventoryInfo != null)
      {
        inventoryInfoList.Add(pinventoryInfo);
      }
      command.InventoryInfoList = inventoryInfoList;  
      return true; 
    }
    
    


    /// <param name="ecomOrderRef"></param>
    /// <param name="orderStatusData"></param>
    [UUID("6c6f7147106746fbadd299209ec7db4a")]
    virtual public EcomSpedInfo GetSpedInfo(Int64 ecomOrderRef, NagzamStatusChanged orderStatusData)
    {   
      //metoda wylicza dane o spedycji dla zamówienia albo z danych o statusie zamówienia (dane spedycyjne wysyłają się
      //jako zmiana statusu) ablo na podstawie danych z bazy 
      var ecomOrder = new ECOM.ECOMORDERS();
      ecomOrder.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{ecomOrder.REF.Symbol} = 0{ecomOrderRef}");
      if(!ecomOrder.FirstRecord()) 
      {
        throw new Exception($"Nieznalezione zamówienie w kanale sprzedaży dla REF={ecomOrderRef}");
      }
    
      var result = new EcomSpedInfo();
      if(orderStatusData != null)
      {
        result.ListwysdRef = orderStatusData.ListwysdRef;
        result.ShipperSymbol = LOGIC.ECOMCHANNELCONVERT.ConvertToChannelValue(ecomOrder.ECOMCHANNELREF.AsInteger,
          "SPOSDOST", orderStatusData.SposdostRef.ToString());
        result.ShippingSymbol = orderStatusData.ListwysdSymbolsped;
        result.CodValue = orderStatusData.ListwysdPobranie;
        result.CodCurrency = orderStatusData.ListwysdPobraniewal;
        result.PackageList = new List<EcomPackageInfo>();
        foreach(var package in orderStatusData.ListywysdrozOpk)
        {
          var packageInfo = new EcomPackageInfo()
          {
            ListywsydrozOpkRef = package.ListywysdrozOpkRef,
            ShipqingSymbol = package.ListywysdrozopkSymbolSped
          };
          result.PackageList.Add(packageInfo);
        }
      }
      ecomOrder.Close();
      return result;  
    }
    
    


    /// <param name="ecomChannelRef"></param>
    /// <param name="ecomInventory"></param>
    [UUID("7c780478bce846d2bb70ff9332244082")]
    virtual public string GetInventoryEDAID(int ecomChannelRef,  EcomInventoryInfo ecomInventory)
    {  
      //[ML] Na chwilę obecną id towaru składamy z ref kanału sprzedaży i refa wersji i id z kanału sprzedaży  
      //żeby identyfikator niósł sam w sobie jakieś informacje, a przy tym był unikalny bo zawsze powinniśmy mieć albo refa wersji (eksport])
      //albo id z kanału (import) 
        
      string result = "";
      var inventoryData = new ECOM.ECOMINVENTORIES();
      //najpierw szukam ECOMINVENTORY po EcomInventoryRef
      if(result == "" && ecomInventory.EcomInventoryRef > 0)
      {  
        inventoryData.FilterAndSort($"{nameof(ECOM.ECOMINVENTORIES)}.{inventoryData.REF.Symbol} = 0{ecomInventory.EcomInventoryRef}");
        if(inventoryData.FirstRecord())    
        {
          result = inventoryData.EDAID;
        }
      }
    
      //potem szukam ECOMINVENTORY po InventoryID
      if(result == "" && !string.IsNullOrEmpty(ecomInventory.InventoryId))
      {    
        inventoryData.FilterAndSort($"{nameof(ECOM.ECOMINVENTORIES)}.{inventoryData.ECOMCHANNELREF.Symbol} = 0{ecomChannelRef} AND {inventoryData.ECOMINVENTORYID.Symbol} = '{ecomInventory.InventoryId}'");
        if(inventoryData.FirstRecord())   
        {
          result = inventoryData.EDAID;
        }
      }
      //szukam po WERSJAREF
      if(result == "" && ecomInventory.WersjaRef > 0)
      {
        inventoryData.FilterAndSort($"{nameof(ECOM.ECOMINVENTORIES)}.{inventoryData.ECOMCHANNELREF.Symbol} = 0{ecomChannelRef} AND {inventoryData.WERSJAREF.Symbol} = 0{ecomInventory.WersjaRef}");
        if(inventoryData.FirstRecord())   
        {
          result = inventoryData.EDAID;
        }
      }
      
      //jak nie znaleziono ECOMINVENTORY, to nadaję symbol
      if(result == "")
      {
        result = $"INV{ecomChannelRef.ToString()}";
        if(!string.IsNullOrEmpty(ecomInventory.InventoryId))
        {
          result+=$"_{ecomInventory.InventoryId}";
        }
        else if(ecomInventory.WersjaRef > 0)  
        {
          result+=$"__{(ecomInventory.WersjaRef ?? 0).ToString()}";
        }
        else
        {
          throw new Exception ("Nie można określić identyfikatora EDA dla towaru. Brak informacji o InventoryId lub WersjaRef");  
        }
      }
      inventoryData.Close();
      return result;
    }
    
    


    /// <summary>
    /// Metoda zwraca listę pól danej klasy. Wykorzystywana np. do naliczana pełnej listy pól struktury EcomInventoryInfo. Lista jest używana do filtrowana danych towaru wysłanych na witrynę
    /// </summary>
    /// <param name="classType"></param>
    /// <param name="prefix"></param>
    /// <param name="recursionLevel"></param>
    [UUID("8f88ef1a4347440bb96b14204e2107aa")]
    public static string GetClassFieldList(Type classType, string prefix = "", int recursionLevel = 0)
    {
      if(recursionLevel >= 10)
      {
        //na wszelki wypadek blokada zbyt wielu rekurencji
        return "";
      }    
      string result = "";    
      var listOfFieldNames = classType.GetProperties();
       
      foreach (var field in listOfFieldNames)
      { 
        result += $"{prefix}[{field.Name}]\n";        
        if (field.GetCustomAttributes(typeof(SynchronizeContent), false).Length > 0)
        {
          //pole ma przypisany atrybut SynchronizeContent, więc odpalamy GetClassFieldList rekurencyjnie,
          //żeby dodać do listy pól do wysyłania pola z klasy zagnieżdżonej np. pola z klasy EcomInventoryImageInfo
          //która jest zagnieżdżona w klasie EcomInventoryInfo                
          var temp = Activator.CreateInstance(Type.GetType(field.PropertyType.FullName));            
          result += GetClassFieldList(
            temp is System.Collections.IEnumerable ? temp.GetType().GetGenericArguments()[0] : temp.GetType(),
            $"  [{field.Name}].",
            recursionLevel + 1);
        }
      }    
      return result;
    }
    
    


    /// <summary>
    /// Metoda zakłada lub aktualizuje zamówienie. Wykorzystywana jest w metodzie ImportOrderProceed.
    /// Metoda najpierw wyszukje zamówienie na liście zamowień dla kanału sprzedaży (ECOMORDERS) i jeżeli takie zamówienie
    /// istnieje, to je aktualizuje. W przeciwnym wypadku zakłada zamówienie w kanale sprzedaży.
    /// W następnym kroku dodawany lub aktualizowany jest nagłówerk zamówienia, które było połączne z zamówieniem w kanale (ECOMCHANNELS).
    /// Jeżeli zamówienia zostało właśnie dodane, to jest parowane w wpisem o zamówieniu w kanale sprzedaży (ECOMORDERS)
    /// parametry:
    /// ecomChannelRef- ref kanału sprzedaży
    /// clientAccountRef - ref konta internetowego klienta
    /// clientRef - ref klienta (z tabeli KLIENCI)
    /// recipientRef - ref odbiorcy zamówienia (z tableli ODBIORCY)
    /// orderInfo - uniwersalna struktura danych zawierająca informacje o zamówieniu
    /// ecomChannelParams - lista parametów kanału sprzedażty wykorzystwana przy synchronizacji danych w danym kanale
    /// zwraca:
    ///  - ref zamówienia  z tabeli NAGZAM
    /// 
    /// Jeśli tak to aktualizujemy dane zamówienia.
    /// Jeśli brak zamówienia w ECOMORDERS to dodajemy je również do NAGZAM
    /// 
    /// </summary>
    /// <param name="ecomChannelRef"></param>
    /// <param name="clientAccountRef"></param>
    /// <param name="clientRef"></param>
    /// <param name="recipientRef"></param>
    /// <param name="orderInfo"></param>
    /// <param name="ecomChannelParams"></param>
    [UUID("8ffb951d0c604dab914292073ad6d5ce")]
    public static int InsertOrUpdateOrder(int ecomChannelRef, int? clientAccountRef, int? clientRef, 
      int? recipientRef, EcomOrderInfo orderInfo, Contexts ecomChannelParams)
    {  
      string orderRegistry = "";
      var ecomOrder = new ECOM.ECOMORDERS();  
      var orderStatusData = new ECOM.ECOMCHANNELSTATES();
      var channelParams = LOGIC.ECOMSTDMETHODS.LoadEcomChannelParams(ecomChannelRef);
      int company = LOGIC.ECOMSTDMETHODS.GetCompanyFromEcomChannel(ecomChannelRef);
    
      try
      {
        //szukamy, czy wczesniej pobraliśmy to zamówienie i jest w ECOMORDERS
        ecomOrder.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{ecomOrder.ECOMCHANNELREF.Symbol} = 0{ecomChannelRef}  " + 
          $"AND {nameof(ECOM.ECOMORDERS)}.{ecomOrder.ECOMORDERID.Symbol} = '{orderInfo.OrderId}' ");
          
        if(ecomOrder.FirstRecord())
        {
          var nagzam = new ECOM.NAGZAM();
          nagzam.FilterAndSort($"{nameof(ECOM.NAGZAM)}.{nagzam.REF.Symbol} = 0{ecomOrder.NAGZAMREF} ");
          if(nagzam.FirstRecord())
          {
            orderRegistry = nagzam.REJESTR.ToString();
          }
          nagzam.Close();
        }
        if(orderRegistry == channelParams["_orderregistry"].AsString || String.IsNullOrEmpty(orderRegistry))
        {
          if(!ecomOrder.FirstRecord())
          {
            throw new Exception($"Błąd wewnętrzny: Nie znalezionio nagłówka ECOMORDERS dla zamówienia {orderInfo.OrderId} przy imporcie danych o zamówieniu");       
          }
          
          orderStatusData.FilterAndSort($"{nameof(ECOM.ECOMCHANNELSTATES)}.{orderStatusData.ECOMCHANNELREF.Symbol} = 0{ecomChannelRef} " + 
            $"AND {nameof(ECOM.ECOMCHANNELSTATES)}.{orderStatusData.SYMBOL.Symbol} = '{orderInfo?.OrderStatus?.OrderStatusId}' ");
          if(!orderStatusData.FirstRecord())
          {
            throw new Exception($"Brak konfiguracji statusu \"{orderInfo?.OrderStatus?.OrderStatusId}\"w kanale sprzedaży {ecomOrder.ECOMCHANNELREF_SYMBOL.ToSQLString()}");
          }
    
          ecomOrder.ECOMCHANNELREF = ecomChannelRef;
          ecomOrder.ECOMACCOUNTREF = clientAccountRef;
          //ecomOrderData.CHANELLSTATUS = orderInfo?.OrderStatus?.OrderStatusId;
          ecomOrder.LASTGETTMSTMP = DateTime.Now;
          ecomOrder.ECOMORDERID = orderInfo?.OrderId;
          ecomOrder.ECOMORDERSYMBOL = orderInfo?.OrderSymbol;  
          ecomOrder.ECOMORDERDATE = orderInfo?.OrderAddDate; 
          ecomOrder.SYNCSTATUS = (int)EcomSyncStatus.ImportProceeding;
          ecomOrder.SYNCSENDCHANELLSTATUS = (int)EcomSyncStatus.Imported; 
          ecomOrder.OREDERSTATUSCHECKSUM = "";//kasujemy, skoro ustawiamy jako zaimportowy - naliczy się przy pierwszej próbie wysłania
          if(!ecomOrder.PostRecord())
          {
            throw new Exception($"Błąd zapisu zamówienia o REF: {ecomOrder.REF} do bazy danych");
          }
        }
        else
        {
          throw new Exception($"Nie można aktualizować zamówień znajdujących się w rejestrze {orderRegistry}");
        }
      }
      catch(Exception ex)
      {
        throw new Exception("Błąd importu danych zamówienia dla kanału sprzedaży: " + ex.Message);
      }  
          
      var recipent = new ECOM.ODBIORCY();
      recipent.FilterAndSort($"{nameof(ECOM.ODBIORCY)}.{recipent.REF.Symbol} = 0{recipientRef}");
      recipent.FirstRecord();
      var orderData = new ECOM.NAGZAM();
      orderData.FilterAndSort($"{nameof(ECOM.NAGZAM)}.{orderData.REF.Symbol} = 0{ecomOrder.NAGZAMREF}");
      if(!orderData.FirstRecord())
      {    
        //na ecomorders nie ma refa zamówienia, więc zakładamy nowe zamówienie
        //nawet jak nie ma platnosci, klienta itp.    
        var ecomChannelStock = new ECOM.ECOMCHANNELSTOCK();
        orderData.NewRecord();
        //wyszukujemy magazyn, do którego insertujemy zamówienie na podstawie magazynu wirtualnego
        ecomChannelStock.FilterAndSort($"{nameof(ECOM.ECOMCHANNELSTOCK)}.{ecomChannelStock.ECOMCHANNELREF.Symbol} = 0{ecomChannelRef} " + 
          $"AND {nameof(ECOM.ECOMCHANNELSTOCK)}.{ecomChannelStock.ECOMSTOCKID.Symbol} = '{orderInfo.StockId}'");
        if(ecomChannelStock.FirstRecord())
        {
          orderData.MAGAZYN = ecomChannelStock.DEFMAGAZ;
        }
        else
        {
          throw new Exception($"Nie znaleziono magazynu o id: {orderInfo.StockId}");
        }     
    
        orderData.COMPANY = company;
        orderData.REJESTR = ecomChannelParams["_orderregistrynew"].AsString;    
        orderData.TYPZAM = ecomChannelParams["_ordertype"].AsString;      
        orderData.DATAWE = DateTime.Now;    
        orderData.KLIENT = clientRef;        
        orderData.ODBIORCAID = recipientRef;
        DataFieldValue sposdostTemp = LOGIC.ECOMCHANNELCONVERT.ConvertFromChannelValue(ecomChannelRef, "SPOSDOST", orderInfo?.Dispatch?.DispatchId);    
        orderData.SPOSDOST = String.IsNullOrEmpty(sposdostTemp) ? null : sposdostTemp;
        DataFieldValue sposzapTemp = LOGIC.ECOMCHANNELCONVERT.ConvertFromChannelValue(ecomChannelRef, "PLATNOSCI", orderInfo?.Payment?.PaymentType);     
        orderData.SPOSZAP = String.IsNullOrEmpty(sposzapTemp) ? null : sposzapTemp;
        orderData.UWAGI = orderInfo?.NoteToOrder; 
        orderData.UWAGISPED = orderInfo?.NoteToCourier;
        DataFieldValue walutaTemp = LOGIC.ECOMCHANNELCONVERT.ConvertFromChannelValue(ecomChannelRef, "WALUTY", orderInfo?.Payment?.Currency);     
        orderData.WALUTA = String.IsNullOrEmpty(walutaTemp) ? null : walutaTemp;
        orderData.WALUTOWE = orderData.WALUTA == "PLN" ? 0 : 1;
        orderData.KURS = orderInfo?.Payment?.Rate;
        DataFieldValue bnTemp = LOGIC.ECOMCHANNELCONVERT.ConvertFromChannelValue(ecomChannelRef, "BN", orderInfo?.Payment?.CalculateType);     
        orderData.BN = String.IsNullOrEmpty(bnTemp) ? null : bnTemp;  
        orderData.DMIASTO = recipent?.DMIASTO;
        orderData.DKRAJID = recipent?.DKRAJID;
        orderData.DNRDOMU = recipent?.DNRDOMU;
        orderData.DNRLOKALU = recipent?.DNRLOKALU;
        orderData.DULICA = recipent?.DULICA;
        orderData.DKODP = recipent?.DKODP;    
        // wpisanie punktu odbioru 
        orderData.PUNKTODBIORU = orderInfo?.PickupPointAddress?.PickupPointId;
        orderData.PUNKTODBIORUEMAIL = orderInfo?.PickupPointAddress?.Email;
        orderData.PUNKTODBIORUTEL = orderInfo?.PickupPointAddress?.Phone;
        orderData.ZRODLOZAM = "ECOM";
        
        ecomChannelStock.Close();
      }
      else   
      {
        orderData.EditRecord();
        orderData.COMPANY = company;
        orderData.KLIENT = clientRef;
        orderData.ODBIORCAID = recipientRef;
        DataFieldValue sposdostTemp = LOGIC.ECOMCHANNELCONVERT.ConvertFromChannelValue(ecomChannelRef, "SPOSDOST", orderInfo?.Dispatch?.DispatchId);    
        orderData.SPOSDOST = String.IsNullOrEmpty(sposdostTemp) ? null : sposdostTemp;
        DataFieldValue sposzapTemp = LOGIC.ECOMCHANNELCONVERT.ConvertFromChannelValue(ecomChannelRef, "PLATNOSCI", orderInfo?.Payment?.PaymentType);     
        orderData.SPOSZAP = String.IsNullOrEmpty(sposzapTemp) ? null : sposzapTemp;
        orderData.UWAGI = orderInfo.NoteToOrder; 
        orderData.UWAGISPED = orderInfo.NoteToCourier;
        orderData.KLIENT = clientRef;        
        orderData.ODBIORCAID = recipientRef;
        orderData.DMIASTO = recipent?.DMIASTO;
        orderData.DKRAJID = recipent?.DKRAJID;
        orderData.DNRDOMU = recipent?.DNRDOMU;
        orderData.DNRLOKALU = recipent?.DNRLOKALU;
        orderData.DULICA = recipent?.DULICA;
        orderData.DKODP = recipent?.DKODP;  
        // wpisanie punktu odbioru 
        orderData.PUNKTODBIORU = orderInfo?.PickupPointAddress?.PickupPointId;
        orderData.PUNKTODBIORUEMAIL = orderInfo?.PickupPointAddress?.Email;
        orderData.PUNKTODBIORUTEL = orderInfo?.PickupPointAddress?.Phone;    
        orderData.COMPANY = company;
      }
      /* tu Paweł ma wątpliwosci, do konsultacji
      else
      {
        throw new Exception("Nastąpiła próba edycji zamówienia, które jest już zablokowane");
      }
      */
    
      if((orderInfo?.Prepaids?.Count ?? 0) > 0)
      {
        decimal? value = 0;  
        value = orderInfo?.Prepaids.Sum(prepaid => prepaid.Value);
    
        orderData.OPLACONE = orderInfo.Payment.OrderValue > value ? 0 : 1;
      } 
    
      if(ecomOrder.ECOMCHANNELSTATE != orderStatusData?.REF)
      {
        //nastąpiła zmiana statusu zamówienia przez witrynę
    
        //ustawiamy status z witryny, który może zostać nadpisany naliczaniem
        //statusu zamówienia w Sente
        ecomOrder.EditRecord();
        ecomOrder.ECOMCHANNELSTATE = orderStatusData?.REF.AsInteger;
        if(!ecomOrder.PostRecord())
        {
          throw new Exception($"Błąd zapisu aktualizacji zamówienia o REF: {ecomOrder.REF} do bazy danych");
        }
      }
      
      if(!orderData.PostRecord())
      {
        throw new Exception($"Błąd zapisu zamówienia {orderData.REF} do bazy danych.");
      }
      if(ecomOrder.NAGZAMREF != orderData.REF)
      {
        ecomOrder.EditRecord();
        ecomOrder.NAGZAMREF = orderData.REF;
        if(!ecomOrder.PostRecord())
        {
          throw new Exception($"Błąd zapisu aktualizacji zamówienia o REF: {ecomOrder.REF} do bazy danych");
        }
      }  
      orderStatusData.Close();
      ecomOrder.Close();
      return orderData.REF.AsInteger;
    }
    
    


    /// <param name="inventoryPricesList"></param>
    /// <param name="pinventoryPriceInfo"></param>
    /// <param name="command"></param>
    [UUID("92550bad53ed4907b499b7b5267e3882")]
    virtual public bool PrepareSendInventoryPrices(List<Int64> inventoryPricesList, EcomInventoryPriceInfo pinventoryPriceInfo, ExportInventoryPricesCommand command)
    {
      var inventoryPricesInfoList = new List<EcomInventoryPriceInfo>();
      //ceny towarów do wysyłki na podstawie listy 
      if(inventoryPricesList != null)
      {
        foreach(var ecomInventoryPriceRef in inventoryPricesList)
        {
          var inventoryPricesInfo = GetInventoryPriceInfo(ecomInventoryPriceRef);
          inventoryPricesInfoList.Add(inventoryPricesInfo);        
        }
      }
    
      //dodanie przygotowanej wcześniej ceny towaru
      if(pinventoryPriceInfo != null)
      {
        inventoryPricesInfoList.Add(pinventoryPriceInfo);
      }
      command.InventoryPricesInfoList = inventoryPricesInfoList;  
      return true;   
    }
    
    


    /// <param name="ecomInventoryPriceRef"></param>
    [UUID("92fb713494334ddda9ca2fd946ec17ca")]
    virtual public EcomInventoryPriceInfo GetInventoryPriceInfo(Int64 ecomInventoryPriceRef)
    {
      //metoda zwraca wypełniony obiekt klasy EcomInventoryPricesInfo
      string errMsg = "";  
      EcomInventoryPriceInfo result = new EcomInventoryPriceInfo();  
          
      try
      {
        var version = new ECOM.WERSJE();
        var product = new ECOM.TOWARY();    
        var inventoryPrice = new ECOM.ECOMINVENTORYPRICES();
        var inventory = new ECOM.ECOMINVENTORIES();
        inventoryPrice.FilterAndSort($"{nameof(ECOM.ECOMINVENTORYPRICES)}.{inventoryPrice.REF.Symbol} = 0{ecomInventoryPriceRef}");
        if(inventoryPrice.FirstRecord())  
        {
          inventory.FilterAndSort($"{nameof(ECOM.ECOMINVENTORIES)}.{inventory.REF.Symbol} = 0{inventoryPrice.ECOMINVENTORYREF}");
          if(!inventory.FirstRecord())  
          {
            throw new Exception($"Nie znaleziono towaru o REF: {inventoryPrice.ECOMINVENTORYREF} w kanale sprzedaży");        
          }
    
          version.FilterAndSort($"{nameof(ECOM.WERSJE)}.{version.REF.Symbol} = 0{inventory.WERSJAREF}");
          if(!version.FirstRecord())  
          {
            throw new Exception($"Nie znaleziono towaru o wersji: {inventory.WERSJAREF} "); 
          }
    
          product.FilterAndSort($"{nameof(ECOM.TOWARY)}.{product.KTM.Symbol} = '{version.KTM}'");
          if(!product.FirstRecord())  
          {
            throw new Exception($"Nie znaleziono towaru o KTM: {version.KTM} "); 
          }
    
          var vatData = GetVatForVerscountry(
            new GetVatForVerscountryIn()
            {
              Country = "PL", 
              Fordate = DateTime.Now,
              Vers = version.REF.AsInteger
            });
    
          if(vatData?.Vatrate == null)
          {
            throw new Exception($"Nie znaleziono stawki VAT dla ktm: {version.KTM} w walucie polski złoty.");         
          }  
          
          result = new EcomInventoryPriceInfo()
          {
            EcomInventoryPriceRef = inventoryPrice.REF.AsInt64,
            InventoryId = inventory.ECOMINVENTORYID.ToString(),            
            RetailPriceNet = inventoryPrice.BASICPRICE.AsNumeric, 
            RetailPrice = inventoryPrice.BASICPRICE.AsNumeric + (inventoryPrice.BASICPRICE.AsNumeric * (vatData.Vatrate.Value / 100)),       
            DiscountPriceNet = inventoryPrice.PROMOPRICE.AsNumeric,
            Currency = LOGIC.ECOMCHANNELCONVERT.ConvertToChannelValue(inventory.ECOMCHANNELREF.AsInteger, "WALUTY", inventoryPrice.CURRENCY)                
          };
        }
        else
        {      
          throw new Exception($"Nie znaleziono wpisu o cenie dla towaru o REF: {ecomInventoryPriceRef}");
        }
        version.Close();
        product.Close();
        inventoryPrice.Close();
        inventory.Close();
      }
      catch(Exception ex)
      {
        errMsg += ex.Message;        
      }
    
      if(!string.IsNullOrEmpty(errMsg))
      {
        throw new Exception($"Błąd generowania danych do struktur przejściowych dla ceny towaru o REF:{ecomInventoryPriceRef.ToString()}");
      }  
      return result;
    }
    
    


    /// <summary>
    /// Metoda zakłada lub aktualizuje dane odbiorcy zamówienia. Wywołana jest w metodzie ImportOrderProceed.
    /// W pierwszym kroku następuje próba wyszukania odbiorcy dla klienta na podstawie imienia, nazwiska, danych adresowych itp.
    /// Jeżeli znaleziono odbiorcę, to jego dane są aktualizowane, a przeciwnym wypadku zakładany jest nowy odbiorca.
    /// parametry:
    ///  - clientRef - ref klienta dla zamówienia
    ///  - deliveryAddress - dane adresu odbioru pobrane z witryny
    /// zwraca:
    ///  - ref dodanego luv znalezionego odbiorcy
    /// 
    /// </summary>
    /// <param name="clientRef"></param>
    /// <param name="deliveryAddress"></param>
    [UUID("93a604c7b27e499aafc693dd0d4c7a42")]
    public static int InsertOrUpdateRecipient(int? clientRef, EcomAddressInfo deliveryAddress)
    {
      //szukamy odbiorcy dla danego klienta o zadanych danych adresowych
      var recipientData = new ECOM.ODBIORCY();
      //[ML ]do sparwdzenia, bo czasem nie podpowiada istniejącego odbiorcy, tylko zakłada nowego,
      //problem zgłoszony w TSU-1620, po poprawce powinno działać 
    
      recipientData.FilterAndSort($"{nameof(ECOM.ODBIORCY)}.{recipientData.KLIENT.Symbol} = 0{clientRef} " +
        $"AND {nameof(ECOM.ODBIORCY)}.{recipientData.NAZWAFIRMY.Symbol} = '{deliveryAddress.FirmName}'" +
        $"AND {nameof(ECOM.ODBIORCY)}.{recipientData.IMIE.Symbol} = '{deliveryAddress.FirstName}'" +
        $"AND {nameof(ECOM.ODBIORCY)}.{recipientData.NAZWISKO.Symbol} = '{deliveryAddress.LastName}'" +
        $"AND {nameof(ECOM.ODBIORCY)}.{recipientData.DKRAJID.Symbol} = '{deliveryAddress.CountryId}'" + 
        $"AND {nameof(ECOM.ODBIORCY)}.{recipientData.DTELEFON.Symbol} = '{deliveryAddress.Phone}'" + 
        $"AND {nameof(ECOM.ODBIORCY)}.{recipientData.DKODP.Symbol} = '{deliveryAddress.ZipCode}'" +
        $"AND {nameof(ECOM.ODBIORCY)}.{recipientData.DMIASTO.Symbol} = '{deliveryAddress.City}'" +
        $"AND {nameof(ECOM.ODBIORCY)}.{recipientData.DULICA.Symbol} = '{deliveryAddress.Street}'");
          
      if(!recipientData.FirstRecord())
      {    
        //nie znaleziono odbiorcy, to zakładamy nowego
        recipientData.NewRecord();
        recipientData.NAZWA = string.IsNullOrEmpty(deliveryAddress.FirmName) ? 
          deliveryAddress.FirstName + " " + deliveryAddress.LastName :  
          deliveryAddress.FirmName;
        recipientData.NAZWAFIRMY = deliveryAddress.FirmName;
        recipientData.DULICA = deliveryAddress.Street;
        recipientData.DNRLOKALU = deliveryAddress.AppartmentNo;
        recipientData.DNRDOMU = deliveryAddress.BuildingNo;  
        recipientData.DMIASTO = deliveryAddress.City;    
        recipientData.DKODP = deliveryAddress.ZipCode;
        recipientData.DTELEFON = deliveryAddress.Phone;
        recipientData.IMIE = deliveryAddress.FirstName;    
        recipientData.NAZWISKO = deliveryAddress.LastName;
        recipientData.DKRAJID = deliveryAddress.CountryId;
        recipientData.KLIENT = clientRef;
      }  
      else
      {    
        recipientData.EditRecord();
        recipientData.NAZWA = string.IsNullOrEmpty(deliveryAddress.FirmName) ? 
          deliveryAddress.FirstName + " " + deliveryAddress.LastName :  
          deliveryAddress.FirmName;
        recipientData.NAZWAFIRMY = deliveryAddress.FirmName;
        recipientData.DULICA = deliveryAddress.Street;
        recipientData.DNRLOKALU = deliveryAddress.AppartmentNo;
        recipientData.DNRDOMU = deliveryAddress.BuildingNo;    
        recipientData.DMIASTO = deliveryAddress.City;    
        recipientData.DKODP = deliveryAddress.ZipCode;
        recipientData.DTELEFON = deliveryAddress.Phone;
        recipientData.IMIE = deliveryAddress.FirstName;    
        recipientData.NAZWISKO = deliveryAddress.LastName;
        recipientData.DKRAJID = deliveryAddress.CountryId;    
      }
      
      if(!recipientData.PostRecord())
      {
        throw new Exception($"Błąd zapisu odbiorcy {deliveryAddress.FirmName} do bazy danych");
      }	
    
      if(string.IsNullOrEmpty(recipientData.REF))
      {
        throw new Exception("Nie znaleziono ani nie dodano odbiorcy");
      }
    
      return recipientData.REF.AsInteger;
    }
    
    


    /// <param name="ecomInventoryStockRef"></param>
    [UUID("9ff46f8e4fac445f972017ec7340cbe5")]
    virtual public EcomInventoryStockInfo GetInventoryStockInfo(Int64 ecomInventoryStockRef)
    {
      //metoda zwraca wypełniony obiekt klasy EcomInventoryStockInfo
      string errMsg = "";  
      var result = new EcomInventoryStockInfo();  
          
      try
      {    
        var inventoryStock = new ECOM.ECOMINVENTORYSTOCKS();
        var channelStock = new ECOM.ECOMCHANNELSTOCK();
        var inventory = new ECOM.ECOMINVENTORIES();
    
        inventoryStock.FilterAndSort($"{nameof(ECOM.ECOMINVENTORYSTOCKS)}.{inventoryStock.REF.Symbol} = 0{ecomInventoryStockRef}");
        if(inventoryStock.FirstRecord())  
        {
          inventory.FilterAndSort($"{nameof(ECOM.ECOMINVENTORIES)}.{inventory.REF.Symbol} = 0{inventoryStock.ECOMINVENTORYREF}");
          if(!inventory.FirstRecord())  
          {
            throw new Exception($"Nie znaleziono towaru o REF: {inventoryStock.ECOMINVENTORYREF} w kanale sprzedaży");        
          }
          channelStock.FilterAndSort($"{nameof(ECOM.ECOMCHANNELSTOCK)}.{channelStock.REF.Symbol} = 0{inventoryStock.ECOMCHANNELSTOCKREF}");
          if(channelStock.FirstRecord())  
          {
            result = new EcomInventoryStockInfo()
            {
              EcomInventoryStockRef = inventoryStock.REF.AsInt64,
              EcomChannelStockRef = inventoryStock.ECOMCHANNELSTOCKREF.AsInt64,
              ChannelStockId = channelStock.ECOMSTOCKID,
              InventoryId = inventory.ECOMINVENTORYID.ToString(),
              WersjaRef = inventory.WERSJAREF,         
              Quantity = inventoryStock.QUANTITY.AsDecimal             
            };
          }
          else
          {
            throw new Exception($"Nie znaleziono wpisu o stanie magazynowym o REF: {inventoryStock.ECOMCHANNELSTOCKREF}"); 
          }
        }
        else
        {      
          throw new Exception($"Nie znaleziono wpisu o stanie magazynowym dla towaru o REF: {ecomInventoryStockRef}");
        }
        inventoryStock.Close();
        inventory.Close();
        channelStock.Close();
      }
      catch(Exception ex)
      {
        errMsg += ex.Message;        
      }
    
      if(!string.IsNullOrEmpty(errMsg))
      {
        throw new Exception($"Błąd generowania danych do struktur przejściowych dla stanów magazynowych towaru o REF:{ecomInventoryStockRef.ToString()}");
      }  
      return result;
    }
    
    


    /// <param name="ecomChannel"></param>
    [UUID("a23ad4c7e33a409b991acf94748af294")]
    public static int GetCompanyFromEcomChannel(int ecomChannel)
    {
      var ecomchannelModel = new LOGIC.ECOMCHANNELS().Get(ecomChannel);
      if ((ecomchannelModel?.COMPANY ?? 0) == 0)
      {
        throw new Exception("Brak skonfigurowanej spółki do której przypisany jest kanał sprzedaży.");
      }
      return (int)ecomchannelModel.COMPANY;
    }
    
    
    


    /// <param name="filepath"></param>
    [UUID("a3f3ef1211664970a067cc0367c5bbd5")]
    virtual public string GetFileCheckSum(string filepath)
    {
      // Pętla sprawdza, czy plik nie jest używany przez inny proces
      // Jeśli nie jest to generuje hash pliku 
      // Pętla powstała, ponieważ zaraz po wygenerowaniu pliku dostawaliśmy błąd, że plik jest używany przez inny proces
      var hash = "";
      if(File.Exists(filepath))
          {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create()) 
            {
              int counter = 0;
              do
              {
                try
                {
                  counter++;  
                  var file = Convert.ToBase64String(File.ReadAllBytes(filepath));
                  hash = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(file))).Replace("-", String.Empty);             
                  break;
                }
                catch(Exception ex)
                {
                  System.Threading.Thread.Sleep(300);
                  continue; 
                }
                
              }while(counter < 10);
                 
            }
          }
      return hash;
    }
    
    
    


    /// <param name="ecomInventoryRef"></param>
    [UUID("a5b3fda6b8024f1eb6c14afbd271b505")]
    virtual public EcomInventoryInfo GetInventoryInfo(Int64 ecomInventoryRef)
    {  
      string errMsg = "";
      //pobranie danych do struktury EcomInv EcomInventoryInfo z danych w bazie
      EcomInventoryInfo result;  
    
      var ecomInventory = new ECOM.ECOMINVENTORIES();
      ecomInventory.FilterAndSort($"{nameof(ECOM.ECOMINVENTORIES)}.{ecomInventory.REF.Symbol} = 0{ecomInventoryRef}");
      if(!ecomInventory.FirstRecord())   
      {
        throw new Exception($"Nie znaleziono towaru w kanale sprzedaży dla REF={ecomInventoryRef}");
      }
    
      //info o wwrsji pobieramy w metodzie głównej, żeby potem nie musieć w każdej kolejnej robić selecta do wersji,
      //żeby dostać ktm itp.
      var versionData = new ECOM.WERSJE();
      versionData.FilterAndSort($"{nameof(ECOM.WERSJE)}.{versionData.REF.Symbol} = 0{ecomInventory.WERSJAREF}");
      if(!versionData.FirstRecord())   
      {    
        throw new Exception ($"Nie znaleziono wersji o ref: {ecomInventory.WERSJAREF}.");        
      }
    
      //w zaleznosci od tego czy wysyłamy towar pierwszy raz, czy go aktualizujemy pobieramy inny parametr,
      //na podstawie którego zachodzi filtrowanie, które dane towaru bedą wysłane
      Contexts ecomChannelParams = LOGIC.ECOMSTDMETHODS.LoadEcomChannelParams(ecomInventory.ECOMCHANNELREF.AsInteger);
      string atributesToSend;
      if(string.IsNullOrEmpty(ecomInventory.ECOMINVENTORYID) && 
        string.IsNullOrEmpty(ecomInventory.LASTSENDTMSTMP))
      {
        atributesToSend = ecomChannelParams["_inventoryattributestosend"];
      }
      else
      {
        atributesToSend = ecomChannelParams["_inventoryattributestoupdate"];
      } 
    
      var ecomChannel = new ECOM.ECOMCHANNELS();
      ecomChannel.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannel.REF.Symbol} = 0{ecomInventory.ECOMCHANNELREF}");
      if(!ecomChannel.FirstRecord())   
      {
        errMsg += $"Nie znaleziono kanału sprzedaży o REF: {ecomInventory.ECOMCHANNELREF}";       
      }   
    
      if(!string.IsNullOrEmpty(errMsg))
      {
        throw new Exception($"Błąd generowania danych do struktur przejściowych dla towaru: {ecomInventoryRef}");
      }
    
       
      //metoda wypełnia obiekt klasy przejściowej inventoryInfo danymi z tabeli ECOMINVENTORY i wywołuje kolejne metody, które
      //uzupełniają dane inventoryInfo na podstwie innych tabelk standardowych (TOWARY, TOWJEDN, WERSJE itp.)    
      try
      {
        if(string.IsNullOrEmpty(ecomInventory.ECOMINVENTORYID))
        {
          //jezeli towar nie ma nadanego symbolu pobranego z kanału sprzedaży to
          //sprawdzamy, czy konkretny towar nie ma nadanego symbolu w innym kanale sprzedaży dla tego samego konektora,
          //żeby uniknąc sytuacji, gdzie ten sam towar jest na witrynie pod dwoma symbolami
          var inventorySymbolsInChannel = LOGIC.ECOMINVENTORIES.GetEcomChannelsSymbolsForInventory(ecomInventory.WERSJAREF.AsInteger,ecomChannel.CONNECTOR.AsInteger);
          switch(inventorySymbolsInChannel.Count())
          {          
            case 0:
              //nie ma symbolu towaru w żadnym kanale powiązanym z konektorem
              break;
            case 1:
              //jest symbol, wiec go przepisujemy
              ecomInventory.ECOMINVENTORYID = inventorySymbolsInChannel.First();                       
              break;
            defalut:
              //jest wiecej niz jeden symbol dla konektora, tzn., że coś jest chyba jest nie tak nie tak
              throw new Exception("Towar ma różne symbole w kanałach sprzedaży dla tego samego konektora.");
              break;
          }
        }
    
        result = new EcomInventoryInfo()
        {
          //Dla parametrów wymaganych do działania integracji nie sprawdzamy, czy są w liście parametrów do wysyłki
          EcomInventoryRef = ecomInventory.REF.AsInt64,
          InventoryId = ecomInventory.ECOMINVENTORYID,
          IsVisible = !atributesToSend.Contains("[IsVisible]") || string.IsNullOrEmpty(ecomInventory.ACTIVE) ?
            null as int? : ecomInventory.ACTIVE?.AsInteger,
          WersjaRef = versionData.REF.AsInteger
        };
    
        //uzupełnianie danych na podstawie danych z TOWARY, WERSJE i VAT
        GetProductForInventoryInfo(ecomInventory.ECOMCHANNELREF.AsInteger, versionData.KTM, result, atributesToSend);
    
        //jednostki towaru
        GetUnitsForInventoryInfo(ecomInventory.ECOMCHANNELREF.AsInteger, versionData.KTM, result, atributesToSend);
    
        //zdjęcia dla towaru
        GetImagesForInventoryInfo(ecomInventory.ECOMCHANNELREF.AsInteger, versionData.KTM, result, atributesToSend);
      }
      catch(Exception ex)
      {
        throw new Exception($"Błąd dodawania danych dla towaru o wersji {versionData.REF}: {ex.Message}");        
      } 
      ecomInventory.Close();
      return result;
    }
    
    


    /// <param name="importMode"></param>
    [UUID("a8e7f108ba4e46919a334dd8c0a29cf2")]
    virtual public bool IsImportModeRequiresOrderList(importMode importMode)
    {
      switch(importMode)
    	{
    		case importMode.SinceLastImport:												
    		  return false;
    		case importMode.Selected:
    			return true;
    		case importMode.OrderId:								
    			return true;
    		case importMode.DateRange:
    			return false;
    		case importMode.All:
    			return false;
    		default:
    			throw new Exception("Nieobsłużony tryb pobierania zamówień");
    	}
    }
    
    


    /// <summary>
    ///  metoda przyjmuje obiekt EcomInventoryInfo ktm i string attributesToSend zawierający
    ///  listę pól do wysyłki na witrynę (bo przy aktualizacji lista póla do wysłki może być inna niż przy dodawaniu)
    ///   i uzupełnia EcomInventoryInfo danymi pobranymi z tabel Towary i VAT  
    /// </summary>
    /// <param name="ecomChannelRef"></param>
    /// <param name="ktm"></param>
    /// <param name="inventoryInfo"></param>
    /// <param name="attributesToSend"></param>
    [UUID("b062ad364ba8468bbebde4dde2dc3bad")]
    virtual public void GetProductForInventoryInfo(int ecomChannelRef, string ktm, EcomInventoryInfo inventoryInfo, string attributesToSend)
    {
      //metoda przyjmuje obiekt EcomInventoryInfo ktm i string attributesToSend zawierający
      //listę pól do wysyłki na witrynę (bo przy aktualizacji lista póla do wysłki może być inna niż przy dodawaniu)
      //i uzupełnia EcomInventoryInfo danymi pobranymi z tabel Towary i VAT  
      var inventoryData = new ECOM.TOWARY();
      var inventoryVersion = new ECOM.WERSJE();
      inventoryData.FilterAndSort($"{nameof(ECOM.TOWARY)}.{inventoryData.KTM.Symbol} = '{ktm}'");
      inventoryVersion.FilterAndSort($"{nameof(ECOM.WERSJE)}.{inventoryVersion.REF.Symbol} = 0{inventoryInfo.WersjaRef}");
      if(inventoryData.FirstRecord() && inventoryVersion.FirstRecord())  
      {
        inventoryInfo.CategoryId = !attributesToSend.Contains("[CategoryId]") ? LOGIC.ECOMCHANNELCONVERT.ConvertToChannelValue(ecomChannelRef, 
        "TOWARY", "GRUPA", inventoryData.GRUPA) : null;
        inventoryInfo.Type = attributesToSend.Contains("[Type]") ? LOGIC.ECOMCHANNELCONVERT.ConvertToChannelValue(ecomChannelRef,
        "TOWTYPES", inventoryData.USLUGA) : null;    
        string inventoryName = inventoryVersion?.NRWERSJI.ToInt() > 0 ? inventoryData?.NAZWA.ToString() + " - " + inventoryVersion?.NAZWA.ToString() : inventoryData?.NAZWA.ToString();
    
        inventoryInfo.InventoryNames = attributesToSend.Contains("[InventoryNames]") ? 
          new List<EcomInventoryTextInfo>() 
          {
            new EcomInventoryTextInfo()
            {
              Language = attributesToSend.Contains("[InventoryNames].[Language]") ? LOGIC.ECOMCHANNELCONVERT.ConvertToChannelValue(ecomChannelRef,
              "LANGUAGES", "0") : null,
              Text = attributesToSend.Contains("[InventoryNames].[Text]") ? inventoryName : null
            }
          } : null;
        inventoryInfo.InventoryDescriptions = attributesToSend.Contains("[InventoryDescriptions]") ? 
          new List<EcomInventoryTextInfo>()
          {
            new EcomInventoryTextInfo()
            {
              Language = attributesToSend.Contains("[InventoryDescriptions].[Language]") ? LOGIC.ECOMCHANNELCONVERT.ConvertToChannelValue(ecomChannelRef,
              "LANGUAGES", "0") : null,
              Text = attributesToSend.Contains("[InventoryDescriptions].[Text]") ? inventoryData.OPISROZ : null
            }
          } : null;
    
        //informacje o wacie w tej samej metodzie, bo grupa vat jest powiązana z towarem
        var vatData = GetVatForVerscountry(
          new GetVatForVerscountryIn()
          {
            Country = "PL", 
            Fordate = DateTime.Now,
            Ktm = ktm
          });
    
        if(vatData == null)
        {
          throw new Exception($"Nie znaleziono stawki VAT dla ktm: {ktm} w walucie polski złoty.");        
        }
      
        inventoryInfo.Vat = !attributesToSend.Contains("[Vat]") || string.IsNullOrEmpty(vatData.Vatid) ?
          null : LOGIC.ECOMCHANNELCONVERT.ConvertToChannelValue(ecomChannelRef,"VAT", vatData.Vatid);
    
        inventoryInfo.VatFree = !attributesToSend.Contains("[VatFree]") || string.IsNullOrEmpty(vatData.Vatid) ?
          null as bool? : vatData.Vatid == "ZW"; //[ML] ZW wyrzucić do parametrów kanału sprzedaży, 
           
      }
      else
      {
        throw new Exception ($"Nie znaleziono towaru o ktm: {ktm}.");
      }  
      inventoryData.Close();    
      inventoryVersion.Close();   
       
    }
    
    
    


    /// <param name="ecomOrderRef"></param>
    /// <param name="orderStatusData"></param>
    [UUID("bcb28e6ad0954387bec5e533c0516660")]
    virtual public EcomAttachmentInfo GetInvoiceInfo(Int64 ecomOrderRef, NagzamStatusChanged orderStatusData)
    {   
      //metoda do załączników uzupełniana na podstawie danych z bazy (załączniki wysyłają się
      //jako zmiana statusu) 
      var ecomOrder = new ECOM.ECOMORDERS();
      ecomOrder.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{ecomOrder.REF.Symbol} = 0{ecomOrderRef}");
      if(!ecomOrder.FirstRecord()) 
      {
        throw new Exception($"Nieznalezione zamówienie w kanale sprzedaży dla REF={ecomOrderRef}");
      }
      
      var result = new EcomAttachmentInfo();
      if(orderStatusData != null)
      {
        result.OrderId = ecomOrder.ECOMORDERID;
        result.FileName = orderStatusData.NagfakRef;
        result.AttachmentSymbol = orderStatusData.NagfakSymbol;
        result.AttachmentPath = orderStatusData.AttachmentPath;
        result.NagfakRef = orderStatusData.NagfakRef;
        result.Typfak = orderStatusData.TypfakSymbol;
      }
      else
      {
        var storage = new ECOM.S_STORAGE();
        var nagzam = new ECOM.NAGZAM();
        storage.FilterAndSort($"{nameof(ECOM.S_STORAGE)}.{storage.STORAGETYPE.Symbol} = 'CRMINVOICES'");
        if(!storage.FirstRecord()) 
        {
          throw new Exception($"Nie znaleziono ścieżki do faktur.");
        }
        nagzam.FilterAndSort($"{nameof(ECOM.NAGZAM)}.{nagzam.REF.Symbol} = 0{ecomOrder.NAGZAMREF}");
        
        if(nagzam.FirstRecord())   
        {
          var invoice = CORE.QuerySQL(
              @"select first 1 distinct nf.ref, nf.akceptacja, nf.symbol, nf.typ
                from nagzam nz
                join dokumnag dn on dn.zamowienie = nz.ref
                join nagfak nf on (nf.refdokm = dn.ref or dn.faktura = nf.ref or
                  nf.fromnagzam = nz.ref or nz.faktura = nf.ref) " +
                $"where (nz.ref = 0{nagzam.REF} or nz.org_ref = 0{nagzam.REF})").FirstOrDefault();
          if(invoice != null)
          {
            result.OrderId = ecomOrder.ECOMORDERID;
            result.AttachmentPath = storage.HOSTPATH;
            result.FileName = invoice["REF"].ToString();    
            result.AttachmentSymbol = invoice["SYMBOL"].ToString(); 
            result.NagfakRef = invoice["REF"].ToString();   
            result.Typfak = invoice["TYP"].ToString();
          }
        }
        storage.Close();
        nagzam.Close();
      }
      ecomOrder.Close();
      
    
      return result;  
    }
    
    


    /// <param name="ecomChannelRef"></param>
    /// <param name="ecomOrder"></param>
    [UUID("c46613c2144d49a880886d8b27c47fe2")]
    virtual public string GetOrderEDAID(int ecomChannelRef, EcomOrderInfo ecomOrder)
    {
      //[ML] Na chwilę obecną id zamówienia składamy z refa kanału sprzedaży i refa nagzama i id z kanału sprzedaży
      //żeby identyfikator niósł sam w sobie jakieś informacje, a przy tym był unikalny bo zawsze powinniśmy mieć albo refa nagmaza (eksport])
      //albo id z kanału (import)   
      string result = "";
      var orderData = new ECOM.ECOMORDERS();
      //najpierw szukam ECOMORDER po EcomOrderRef
      if(result=="" && ecomOrder.EcomOrderRef > 0)
      { 
        orderData.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{orderData.REF.Symbol} = 0{ecomOrder.EcomOrderRef}");
        if(orderData.FirstRecord())    
        {
          result = orderData.EDAID;
        }
      }
      //najpierw szukam ECOMORDER po OrderID
      if(result=="" && ecomOrder.OrderId != null)
      {    
        orderData.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{orderData.ECOMCHANNELREF.Symbol} = 0{ecomChannelRef} " + 
          $"AND {nameof(ECOM.ECOMORDERS)}.{orderData.ECOMORDERID.Symbol} = '{ecomOrder.OrderId}'");
        if(orderData.FirstRecord())  
        {
          result = orderData.EDAID;
        }
      }
      //szukam po NAGZAMREF
      if(result == "" && ecomOrder.NagzamRef > 0)
      {
        orderData.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{orderData.ECOMCHANNELREF.Symbol} = 0{ecomChannelRef} " +
          $"AND {nameof(ECOM.ECOMORDERS)}.{orderData.ECOMORDERID.Symbol} = '{ecomOrder.OrderId}'");
        if(orderData.FirstRecord())  
        {
          result = orderData.EDAID;
        }
      }
      
      //jak nie znaleziono EDAORDERS, to nadaję zam
      if(result == ""){
        result = $"ORD{ecomChannelRef.ToString()}";
        if(ecomOrder.OrderId != null)
          result+=$"_{ecomOrder.OrderId}";
        else if(ecomOrder.NagzamRef > 0)  
          result+=$"__{(ecomOrder.NagzamRef ?? 0).ToString()}";
        else
          throw new Exception ("Nie można określić identyfikatora EDA dla zamówienia. Brak informacji i OrderId lub NagzamRef");  
      }
      orderData.Close();
      return result;
    }
    
    


    /// <summary>
    /// Metoda dodaje pozycje do zamówienia. Wykorzystywana jest w metodzie ImportOrderProceed.
    /// Przy koliejnych importach tego samego zamówienia pozycje zamówień są aktualizowane.Towary do pozycji znajdowane są na podstwie danyc z tabeli  ECOMINVEMTORIES.
    /// W zależności od konfiguracji kanału sprzedaży jeżeli w słowniku towarów dla kanału ECOMINVEMTORIES nie znaleziono wpisu do towaru, 
    /// pozycja jest albo pomijana albo uzupełniana towarem domyślnym.
    /// Parametry wejściowe: 
    /// ecomChannelRef - ref kanału sprzedaży
    /// orderRef - ref nagłówka zamówienia 
    /// orderInfo - struktura danych zawierająca informacje o zamówieniu
    /// ecomChannelParams - lista parametów kanału sprzedażty wykorzystwana przy synchronizacji danych w danym kanale 
    /// 
    /// </summary>
    /// <param name="ecomChannelRef"></param>
    /// <param name="orderRef"></param>
    /// <param name="orderInfo"></param>
    /// <param name="ecomChannelParams"></param>
    [UUID("c800b6bc98054c83a4eddca72f0218b4")]
    public static void InsertOrUpdateOrderLines(int ecomChannelRef, int? orderRef, 
      EcomOrderInfo orderInfo, Contexts ecomChannelParams)
    {  
      string errorMsg = "";
      var orderLines = new LOGIC.POZZAM();
      //lista nowych pozycji pobrana z kanału sprzedaży
      var ecomOrderLines = orderInfo.OrderLines.ToList();
      var ecomInventories = new LOGIC.ECOMINVENTORIES();
      var ecomInventory = new ECOM.MODEL.ECOMINVENTORIES();
    
      foreach(var orderLine in orderLines.Get().Where(p => p.ZAMOWIENIE == orderRef))
      {
        //pobieramy dane towary z ECOMINVENTORIES    
        try
        {
          int? ecomInventoryRef = new LOGIC.ECOMINVENTORIESGROUPS()
            .WithParameters( new ECOMINVENTORIESGROUPSParameters() { _ecomchannelref = ecomChannelRef })
            .GetInventoryRef((orderLine.WERSJAREF ?? 0));
            
          if (ecomInventoryRef == null)
          {
            ecomInventoryRef = 0;
          }  
          ecomInventory = ecomInventories.Get().Where(i => i.REF == (int)ecomInventoryRef).Single();
        }
        catch(Exception ex)
        {
          //moze sie zdarzyc, że jak nie znajdziemy towaru, bo jest to usługa transportowa, albo produkt nieznany
          //w innych przypadkach excpetion
          if(orderLine.WERSJAREF != ecomChannelParams["_deliverservice"].AsInteger 
            && orderLine.WERSJAREF != ecomChannelParams["_productunknown"].AsInteger)
          {
            throw new Exception($"W kanale sprzedaży nieznaleziono towaru o wersji: {orderLine.WERSJAREF}");
          }    
        }
    
        //wyszukujemy we właśnie pobranych pozycjach pasującą do lini z POZZAM
        var matchingEcomOrderLine = 
          ecomOrderLines.
            Where(l => l.ProductId == ecomInventory?.ECOMINVENTORYID 
              && l.ProductQuantity == orderLine.ILOSC
              && l.ProductOrderPriceNet == orderLine.CENANET
              && l.ProductOrderPrice == orderLine.CENABRU)
            .FirstOrDefault();
        
        if(matchingEcomOrderLine != null)
        {
          //Kasujemy pozycję z listy tych, które właśnie przyszły i nie ruszamy pozzama
          ecomOrderLines.Remove(matchingEcomOrderLine);      
        }
        else if(orderLine.WERSJAREF != ecomChannelParams["_deliverservice"].AsInteger
          || orderLine.CENABRU != orderInfo.Dispatch.DeliveryCost
          || orderLine.ILOSC != 1)
        {
          //kasujemy pozzama, bo już nie pasuje do listy, która przyszła z witryny
          //i nie jest to pozycja z kosztem dostawy
          orderLines.Delete(orderLine.REF.Value);
        }   
      }
    
      //teraz dodajemy wszytkie te pozycje z listy własnie pobranych, które jeszcze zostały
       string bnTemp = LOGIC.ECOMCHANNELCONVERT.ConvertFromChannelValue(ecomChannelRef, "BN", orderInfo?.Payment?.CalculateType);           
      
      //dodajemy pozycje z witryny, których nie udało sie dopasować do tych pobranych wcześniej
      if(ecomOrderLines != null && (ecomOrderLines?.Count ?? 0) > 0)
      {    
        foreach(var ecomOrderline in ecomOrderLines)
        {  
          try
          { 
            //jaki to towar w sente
            var ecomInventoriesInChannels = ecomInventories.Get().Where(i => i.ECOMINVENTORYID == ecomOrderline.ProductId).ToList();
    
            ecomInventory = ecomInventoriesInChannels.Where(i => i.ECOMCHANNELREF == ecomChannelRef).FirstOrDefault();
            if (ecomInventory == null)
            {
              ecomInventory = ecomInventoriesInChannels.FirstOrDefault();
            }
    
            var newOrderLine = new ECOM.MODEL.POZZAM();
    
            if(ecomInventory == null)
            {  
              if(string.IsNullOrEmpty(ecomChannelParams["_productunknown"]))
              {
                //jak nie znaleziono dopasowania i produktu domyślnego, to pomijamy pozycję
                throw new Exception($"Towar nieznany nie jest ustawiony w konfiguracji kanału sprzedaży.");    
                continue;
              }
              else
              {
                // uzupelnienie nieznanego towaru w towar domyslny        
                newOrderLine.ZAMOWIENIE = orderRef;
                newOrderLine.WERSJAREF = ecomChannelParams["_productunknown"].AsInteger;
                newOrderLine.ILOSC = ecomOrderline.ProductQuantity;
              } 
            }
            else
            {          
              newOrderLine.ZAMOWIENIE = orderRef;
              newOrderLine.WERSJAREF = ecomInventory.WERSJAREF;
              newOrderLine.ILOSC = ecomOrderline.ProductQuantity;
            }
    
            switch (bnTemp)
            {
              case "B":          
                newOrderLine.CENACEN = ecomOrderline.ProductOrderPrice;        
                break;
              case "N":
                newOrderLine.CENACEN = ecomOrderline.ProductOrderPriceNet; 
                break;
              default:
                throw new Exception("Brak sposobu wyliczania ceny brutto/ netto"); 
                //jak nie da się przetłumaczyc, to nie dodajemy żadnej ceny
                break;
            }
    
            DataFieldValue grVat = LOGIC.ECOMCHANNELCONVERT.ConvertFromChannelValue(ecomChannelRef, "VAT", ecomOrderline?.ProductVat);           
            newOrderLine.GR_VAT = string.IsNullOrEmpty(grVat) ? null : grVat;
            newOrderLine.OUT = 1;        
            orderLines.Create(newOrderLine);
          }
          catch(Exception ex)
          {
            errorMsg += $"{ex.Message} \n";
            //przy błędzie na pozycji pomijamy pozycję i robimy kolejną
            continue;
          }
        }
      }
    
      //dodanie pozycji z kosztem dostawy
      try
      {
         var newDeliverServiceOrderLine = new ECOM.MODEL.POZZAM();
    
        //wyszukujemy we właśnie pobranych pozycjach pasującą do lini z POZZAM
        int deliveryCostVersion = ecomChannelParams["_deliverservice"].AsInteger;
        ECOM.MODEL.POZZAM deliverServiceOrderLine = 
          orderLines.Get().
            Where(l => l.ZAMOWIENIE == orderRef 
              && (l.WERSJAREF == deliveryCostVersion))           
            .FirstOrDefault();
       
        //jezeli nie ma pozycji z kosztzem dostawy, a koszt przyszedł z witryny, to dodajemy pozycję
        if(deliverServiceOrderLine == null
          && (orderInfo?.Dispatch?.DeliveryCost ?? 0m) > 0m)
        {
          if(!string.IsNullOrEmpty(ecomChannelParams["_deliverservice"]))
          {
            newDeliverServiceOrderLine.ZAMOWIENIE = orderRef;
            newDeliverServiceOrderLine.WERSJAREF = ecomChannelParams["_deliverservice"].AsInteger;
            newDeliverServiceOrderLine.ILOSC = 1;
    
            switch (bnTemp)
            {
              case "B":          
                newDeliverServiceOrderLine.CENACEN = orderInfo.Dispatch.DeliveryCost;        
                break;
              case "N":
                newDeliverServiceOrderLine.CENACEN = orderInfo.Dispatch.DeliveryCost;       
                break;
              default:
                throw new Exception("Brak sposobu wyliczania ceny: brutto/ netto"); 
                //jak nie da się przetłumaczyc, to nie dodajemy żadnej ceny
                break;
            }
            if(!string.IsNullOrEmpty(orderInfo.Dispatch.DeliveryVat))
            {
              DataFieldValue grVat = LOGIC.ECOMCHANNELCONVERT.ConvertFromChannelValue(ecomChannelRef, "VAT", orderInfo.Dispatch.DeliveryVat.ToString());           
              newDeliverServiceOrderLine.GR_VAT = string.IsNullOrEmpty(grVat) ? null : grVat;
              newDeliverServiceOrderLine.OUT = 1;        
              orderLines.Create(newDeliverServiceOrderLine);
            }
            else
            {
              // jeśli z bramki nie dostaliśmy vatu dostawy, to pobieramy go z usługi dostawy z naszej bazy.
              var delivery = new ECOM.WERSJE();
              delivery.FilterAndSort($"{nameof(ECOM.WERSJE)}.{delivery.REF.Symbol} = 0{ecomChannelParams["_deliverservice"]}");
              if(delivery.FirstRecord())
              {
                newDeliverServiceOrderLine.GR_VAT = string.IsNullOrEmpty(delivery.VAT.ToString()) ? null : delivery.VAT;
                newDeliverServiceOrderLine.OUT = 1;        
                orderLines.Create(newDeliverServiceOrderLine);
              }
              else
              {
                errorMsg += "Nie znaleziono usługi kosztu dostawy. \n";
              }
              delivery.Close();
            }
          }
          else
          {
            errorMsg += $"Brak usługi kosztu dostawy\n";              
          }
        }    
      }
      catch(Exception ex)
      {
        errorMsg += $"Błąd dodawania pozycji kosztu dostawy: {ex.Message} \n";
      }
      
      if(!string.IsNullOrEmpty(errorMsg))
      {
        throw new Exception(errorMsg);
      }
    }
    
    


    /// <summary>
    /// Metoda dodaje wpis w ECOMORDERS. Wykorzysytwana jest w ImportOrderCommandHandler na początku procesu
    /// importu danych zamówienia ze struktur przejściowych do tabel bazy danych
    /// </summary>
    /// <param name="ecomChannelRef"></param>
    /// <param name="orderInfo"></param>
    /// <param name="ecomChannelParams"></param>
    /// <param name="edaId"></param>
    [UUID("ca5a46fb200b4da38d6ab5903d29297e")]
    virtual public void InitializeEcomOrder(int ecomChannelRef, EcomOrderInfo orderInfo, Contexts ecomChannelParams, string edaId)
    {
      var ecomOrderData = new ECOM.ECOMORDERS();  
      try
      {
        //insert do ECOMORDERS powinien pójść nawet jeżeli nie uda sie insert zamówienia
        //szukamy, czy wczesniej pobraliśmy to zamówienie i jest w ECOMORDERS
        ecomOrderData.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{ecomOrderData.ECOMCHANNELREF.Symbol} = 0{ecomChannelRef} " +
          $"AND {nameof(ECOM.ECOMORDERS)}.{ecomOrderData.ECOMORDERID.Symbol} = '{orderInfo.OrderId}'" );
    
        if(!ecomOrderData.FirstRecord()) 
        {      
          //nie mamy takiego ECOMORDER - inijalizujemy
          ecomOrderData.NewRecord();
          ecomOrderData.ECOMCHANNELREF = ecomChannelRef;
          ecomOrderData.ECOMORDERID = orderInfo?.OrderId;
          ecomOrderData.ECOMORDERDATE = orderInfo?.OrderAddDate;
          //ecomOrderData.CHANELLSTATUS = orderInfo?.OrderStatus?.OrderStatus;
          ecomOrderData.SYNCSENDCHANELLSTATUS = (int)EcomSyncStatus.Imported;
        }
        ecomOrderData.SYNCSTATUS = (int)EcomSyncStatus.ImportProceeding;
        ecomOrderData.LASTGETTMSTMP = DateTime.Now;      
        if(orderInfo.OrderChangeDate != null)
          ecomOrderData.LASTCHANGETMSTMP  = orderInfo?.OrderChangeDate;
        else
          ecomOrderData.LASTCHANGETMSTMP = orderInfo?.OrderAddDate;
        ecomOrderData.ECOMORDERSYMBOL = orderInfo?.OrderSymbol;  
        if(string.IsNullOrEmpty(ecomOrderData.EDAID))
          ecomOrderData.EDAID = edaId;   
    
        if(!ecomOrderData.PostRecord())
        {
          throw new Exception($"Błąd zapisu zamówienia o REF: {ecomOrderData.REF} ");
        }
        
      }
      catch(Exception ex)
      {
        throw new Exception("Błąd zakładania nagłówka ECOMORDER: " + ex.Message);
      }
      ecomOrderData.Close();
    }
    
    


    /// <param name="ecomChannelRef"></param>
    /// <param name="ecomOrderStatus"></param>
    [UUID("d63218b1876a4faf92197a19b533affa")]
    virtual public  string GetOrderEDAID(int ecomChannelRef, EcomOrderStatusInfo ecomOrderStatus)
    {
      //korzystam z metody relizujacej identyfikację dla struktury ecomOrderInfo
      EcomOrderInfo orderInfo = new EcomOrderInfo();
      orderInfo.OrderId = ecomOrderStatus.OrderId;
      orderInfo.EcomOrderRef = ecomOrderStatus.EcomOrderRef;
      return GetOrderEDAID(ecomChannelRef, orderInfo); 
    }
    
    


    /// <summary>
    /// Metoda znajduje lub zakłada nowe konto internetowe dla zamówieniai i zwraca ref konta internetowego.
    /// parametry:
    ///  - ecomChannel - ref kanału sprzedaży
    ///  - clientAccountIfno - dane konta internetowgo pobrane z kanału sprzedaży
    /// - ecomChannelParams - lista parametów kanału sprzedażty wykorzystwana przy synchronizacji danych w danym kanale
    /// </summary>
    /// <param name="ecomChannel"></param>
    /// <param name="clientAccountInfo"></param>
    [UUID("e3105add26454627b96f5c6c4ad9da49")]
    public static int InsertOrUpdateClientAccount(int ecomChannel, EcomClientAccountInfo clientAccountInfo)
    {  
      //sprawdzamy czy konto istnieje w tabeli ECOMACCOUNTS, jak nie to zakładamy nowe konto  
      ECOM.ECOMACCOUNTS clientAccount = new ECOM.ECOMACCOUNTS();
      clientAccount.FilterAndSort($"{nameof(ECOM.ECOMACCOUNTS)}.{clientAccount.ECOMCHANNELREF.Symbol} = 0{ecomChannel} " +
        $"AND {nameof(ECOM.ECOMACCOUNTS)}.{clientAccount.ACCOUNTID.Symbol} = '{clientAccountInfo.Id}'");
    
      if(!clientAccount.FirstRecord())
      {    
        //nie ma to zakładamy nowe konto
        clientAccount.NewRecord();    
        clientAccount.ECOMCHANNELREF = ecomChannel;
        clientAccount.ACCOUNTID = clientAccountInfo.Id;
        clientAccount.LOGIN = clientAccountInfo.Login;
        clientAccount.EMAIL = clientAccountInfo.Email;    
      }
      else
      {
        //aktualizujemy     
        clientAccount.EditRecord();
        clientAccount.LOGIN = clientAccountInfo.Login;
        clientAccount.EMAIL = clientAccountInfo.Email;    
      }
      
      if(!clientAccount.PostRecord())
      {
        throw new Exception($"Błąd zapisu konta klienta o REF: {clientAccount.REF} do bazy danych");
      }
      
      return clientAccount.REF.AsInteger;  
    }
    
    


    /// <param name="filledParams"></param>
    /// <param name="paramsFromForm"></param>
    [UUID("f2af1ba56e7f47699a31e40e499e6006")]
    virtual public void SaveEcomChannelParams(Contexts filledParams, Contexts paramsFromForm)
    {  
      int ecomchannel = filledParams["_ecomchannel"].AsInteger;    
      paramsFromForm.Remove("_ecomchannel");
      
      foreach(System.Collections.DictionaryEntry p in paramsFromForm)
      {
        string val = (filledParams.Contains(p.Key) ? filledParams[p.Key].AsString : ""); 
        //[ML] na razie aktualizacja w najporstszy możliwy sposób
        var sqlString =      
          "update or insert into ECOMSTANDARDMETHODPARAMS(ecomchannel, nkey, nvalue)" +
            $" values(0{ecomchannel}, '{p.Key}', '{val}')" +
            " matching(ecomchannel, nkey)";    
        CORE.RunSQL(sqlString);   
      }  
    }
    
    
    
    


    /// <summary>
    /// przegląda tabelę ECOMORDERS z właściwym SYNCSTATUS
    /// dla każdego buduje dane o zamówieniu do struktury EcomOrderStatusInfo
    /// zwraca listę struktur EComOrderStatusInfo&quot;
    /// </summary>
    /// <param name="ecomOrderList"></param>
    /// <param name="porderStatusInfo"></param>
    /// <param name="command"></param>
    [UUID("fb322787b8394845905f366aef2b7088")]
    virtual public bool PrepareSendOrderStatus(List<Int64> ecomOrderList, EcomOrderStatusInfo porderStatusInfo, ExportOrdersStatusCommand command )
    {
      var orderStatusInfoList = new List<EcomOrderStatusInfo>();
      //budowa wstatusów na podstawie listy zamówień
      if(ecomOrderList != null)
      foreach(var ecomOrderREF in ecomOrderList)
      {
        var orderStatusInfo = GetOrderStatusInfo(ecomOrderREF, null);
        orderStatusInfoList.Add(orderStatusInfo);  
      }
      //dodanie przygotowane wcześniej statusu zamówienia
      if(porderStatusInfo != null)
        orderStatusInfoList.Add(porderStatusInfo);
      command.OrderStatusInfoList = orderStatusInfoList;
      return true;
    }
    
    
    


    /// <param name="input"></param>
    [CustomData("FromProcedureName=GET_VAT_FOR_VERSCOUNTRY")]
    [UUID("5e9f8db46a3c4c52bbf911186a08ee52")]
    virtual public GetVatForVerscountryOut GetVatForVerscountry(GetVatForVerscountryIn input)
    {
      /**
       * Metoda logiki biznesowej wygenerowana automatycznie na podstawie procedury: GET_VAT_FOR_VERSCOUNTRY
       **/
      return RunProcedure<GetVatForVerscountryIn, GetVatForVerscountryOut>("GET_VAT_FOR_VERSCOUNTRY", input);
    }
    /// <param name="ecomChannel"></param>
    /// <param name="orderPackageInfo"></param>
    [UUID("1c1f0dcb470244a29dbbec4405693421")]
    public static int InsertOrUpdateOrderPackage(int ecomChannel,EcomOrderPackageInfo orderPackageInfo)
    {  
      //sprawdzamy czy paczka istnieje w tabeli ECOMPARCELS, jak nie to zakładamy nową  
      ECOM.ECOMORDERS order = new ECOM.ECOMORDERS();
      order.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{order.ECOMCHANNELREF.Symbol} = 0{ecomChannel} " +
        $"AND {nameof(ECOM.ECOMORDERS)}.{order.ECOMORDERID.Symbol} = '{orderPackageInfo.OrderId}'");
    
      if(!order.FirstRecord())
      {
        throw new Exception($"Nie znaleziono zamówienia: {orderPackageInfo.OrderId} w bazie danych");
      }
      
      ECOM.ECOMPARCELS orderPackage = new ECOM.ECOMPARCELS();
      orderPackage.FilterAndSort($"{nameof(ECOM.ECOMPARCELS)}.{orderPackage.ECOMORDER.Symbol} = 0{order.REF} " +
        $"AND {nameof(ECOM.ECOMPARCELS)}.{orderPackage.ECOMPARCELID.Symbol} = '{orderPackageInfo.ExternalPackageId}'");
      
      if(!orderPackage.FirstRecord())
      {    
        //nie ma to zakładamy nową paczkę
        orderPackage.NewRecord();
        orderPackage.ECOMORDER = order.REF;
        orderPackage.ECOMPARCELID = orderPackageInfo.ExternalPackageId;
        orderPackage.SYNCSTATUS = (int)EcomSyncStatus.ImportProceeding;
        orderPackage.ECOMPARCELDATE = orderPackageInfo.PackageDate;
        orderPackage.PACKAGENUMBER = orderPackageInfo.PackageNumber;  
        orderPackage.COURIERCODE = orderPackageInfo.CourierCode; 
      }
      else
      {
        //aktualizujemy     
        orderPackage.EditRecord();
        orderPackage.ECOMPARCELDATE = orderPackageInfo.PackageDate;
        orderPackage.PACKAGENUMBER = orderPackageInfo.PackageNumber; 
        orderPackage.SYNCSTATUS = (int)EcomSyncStatus.ImportProceeding;
        orderPackage.COURIERCODE = orderPackageInfo.CourierCode; 
      }
      
      if(!orderPackage.PostRecord())
      {
        throw new Exception($"Błąd zapisu paczki: {orderPackageInfo.ExternalPackageId} do zamówienia: {orderPackageInfo.OrderId} do bazy danych");
      }
    
      order.Close();
    
      return orderPackage.REF.AsInteger;  
    }
    /// <param name="ecomChannel"></param>
    /// <param name="packageLabelInfo"></param>
    [UUID("b7c282cf409c4bf8834d1f41aa61fc4c")]
    virtual public int InsertOrUpdatePackageLabel(int ecomChannel,EcomPackageLabelInfo packageLabelInfo)
    {  
      //sprawdzamy czy paczka istnieje w tabeli ECOMPARCELS, jak nie to zakładamy nową  
      ECOM.ECOMORDERS order = new ECOM.ECOMORDERS();
      order.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{order.ECOMCHANNELREF.Symbol} = 0{ecomChannel} " +
        $"AND {nameof(ECOM.ECOMORDERS)}.{order.ECOMORDERID.Symbol} = '{packageLabelInfo.OrderId}'");
    
      if(!order.FirstRecord())
      {
        throw new Exception($"Nie znaleziono zamówienia do etykiety: {packageLabelInfo.OrderId} w bazie danych");
      }  
    
      ECOM.ECOMPARCELS package = new ECOM.ECOMPARCELS();
      package.FilterAndSort($"{nameof(ECOM.ECOMPARCELS)}.{package.ECOMORDER.Symbol} = 0{order.REF} " +
        $"AND {nameof(ECOM.ECOMPARCELS)}.{package.ECOMPARCELID.Symbol} = '{packageLabelInfo.PackageId}'");
    
      if(!package.FirstRecord())
      {
        throw new Exception($"Nie znaleziono paczki do pobrania etykiety: {packageLabelInfo.PackageId} w bazie danych");
      }
      else
      {
        //aktualizujemy     
        package.EditRecord();
        package.LABELPATH = packageLabelInfo.LabelPath;
        package.SYNCSTATUS = (int)EcomSyncStatus.Imported;
        package.LABELTYPE = packageLabelInfo.LabelType; 
      }
      
      
      if(!package.PostRecord())
      {
        throw new Exception($"Błąd zapisu etykiety do paczki: {packageLabelInfo.PackageId} do zamówienia: {packageLabelInfo.OrderId} do bazy danych");
      }
      order.Close();
      
      return package.REF.AsInteger;  
    }
    
    


  }
}
