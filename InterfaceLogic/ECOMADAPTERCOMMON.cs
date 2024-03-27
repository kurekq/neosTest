
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
//ERRSOURCE: ECOM.ECOMADAPTERCOMMON

  public partial class ECOMADAPTERCOMMON
  {
    /// <summary>
    /// metoda otwiera stronę dot. cen towarów w panelu administratora sklepu internetowego w przeglądarce internetowej
    ///   
    /// </summary>
    /// <param name="connector"></param>
    /// <param name="ecomPriceRef"></param>
    [UUID("046a02fa255a4334a8a322773f3c8dfb")]
    virtual public void DoOpenPriceInAdminPanel(int connector, int ecomPriceRef)
    {
      //metoda otwiera stronę dot. cen towarów w panelu administratora sklepu internetowego w przeglądarce internetowej
      throw new NotImplementedByDesignException("Metoda nieobsługiwana dla wybranego konektora");  
    }
    
    

    /// <summary>
    /// metoda otwiera stronę dot. stanu magazynowego towaru w panelu administratora sklepu internetowego w domyślnej przeglądarce
    /// </summary>
    /// <param name="connectorRef"></param>
    /// <param name="ecomInventoryStockRef"></param>
    [UUID("49f517ea9ebb437fb36277ef3f262299")]
    virtual public void DoOpenInventoryStockInAdminPanel(int connectorRef, int ecomInventoryStockRef)
    {
      //metoda otwiera stronę dot. stanu magazyniwego towaru w panelu administratora sklepu internetowego w przeglądarce internetowej
      throw new NotImplementedByDesignException("Metoda nieobsługiwana dla wybranego konektora");  
    }
    
    

    /// <param name="connector"></param>
    /// <param name="inventoryId"></param>
    [UUID("7f4ce645223744acbdb5fb1c5ad94a97")]
    virtual public void DoOpenInventoryInAdminPanel(int connector, string inventoryId)
    {
      throw new NotImplementedByDesignException("Metoda nieobsługiwana dla wybranego konektora");  
    }
    
    
    

    /// <summary>
    /// Metoda wirtualna służąca do otwierania wskazanego zamówienia w panelu administracyjnym w domyślnej przeglądarce windowsa.
    /// Przeciążana w konektorach dziedziczasych po ECOMORDERADAPTERCOMMON.
    /// Uruchamiana spod przycisku z panelu zamówień
    /// parametry:
    ///  - connector - ref konektora
    ///  - orderId - ref zamówienia w kanale sprzedaży (tabela ECOMORDERS)
    /// </summary>
    /// <param name="connector"></param>
    /// <param name="orderId"></param>
    [UUID("bbe6b331c0544ff896dde3760ad07244")]
    virtual public void DoOpenOrderInAdminPanel(int connector, string orderId)
    {
      throw new NotImplementedByDesignException("Metoda nieobsługiwana dla wybranego konektora");  
    }
    
    

    /// <summary>
    /// Metoda wykorzysywana do automatycznego wysyłania/ aktualizowania zamowień na witrynie przez schedulera.
    /// Aktualnie wykorzysywana do wysyłania zmian statusów zamówienia na wtrynę.
    /// Może działać w jednym z trzech trybów:
    /// - jeżeli podano adapter, to wyołuje komendę eskportu statusu dla każdego kanału sprz. dla konektora powiązanego z podanym adapterem
    /// - jeżeli podano connectorRef, to wyołuje komendę eksportu towarów dla każdego kanału sprz. powiązanego z danym konektorem
    /// - jeżeli podano ecomChannelRef, to wyołuje komendę eksportu towarów tylko dla danego kanału sprzedaży
    /// Metoda zwraca liczbę wysłanych komend eksportu towarów (osobne komendy dla każdego towaru w każdym kanale sprzedaży, 
    /// w którym wymagana jest aktualizacja) , lub komunikaty o błędach
    /// Parametry wejściowe:
    ///  - adapter - ref adaptera
    ///  - connectorRef - ref konektora
    ///  - ecomChannelRef - ref kanału sprzedaży
    /// Zwraca:
    ///  - liczbę wysłanych komend i komunikatu o błędach
    /// </summary>
    /// <param name="adapter"></param>
    /// <param name="connectorRef"></param>
    /// <param name="ecomChannelRef"></param>
    [UUID("debd0920c44849c482080383152b2fa9")]
    public static string ExportOrdersRequest(string adapter, int? connectorRef, int? ecomChannelRef)
    {
      //[ML]Prawdopodobnie do refaktoryzacji po ostatnich poprawkach, ale nie skasowana, bo może
      //się przydać
      string message = "";
      string sqlString = ""; 
      int successCnt = 0;
      int failCnt = 0;  
    
      if(!string.IsNullOrEmpty(adapter)) 
      {
        //zapytanie pobiera zamówienia do eksportu ze wszystkich kanałów sprzedaży powiązanych konektorami połączonymi z wybranym adapterem
        sqlString = 
          "select distinct eor.ref, con.groupname, con.symbol, ch.ref channel " +
            "from ecomchannels ch " +
              "join sys_edaconnectors con on con.ref = ch.connector " +
              "join ecomorders eor on eor.ecomchannelref = ch.ref " +
            $"where con.adapter = '{adapter}' and eor.syncsendchanellstatus = 0{(int)EcomSyncStatus.ExportPending}"; 
      }
      else if(connectorRef != null)
      {
        //zapytanie pobiera zamówienia do eksportu ze wszystkich kanałów sprzedaży powiązanych z wybranym konektorem
        sqlString = 
          "select distinct eor.ref, con.groupname, con.symbol, ch.ref channel " +
            "from sys_edaconnectors con " +
              "join ecomchannels ch on ch.connector = con.ref " +
              "join ecomorders eor on eor.ecomchannelref = ch.ref " +
            $"where con.ref = 0{connectorRef} and eor.syncsendchanellstatus = 0{(int)EcomSyncStatus.ExportPending}";
      }
      else if(ecomChannelRef != null)
      {
        //zapytanie pobiera zamówienia do eksportu z podanego kanału sprzedaży
        sqlString =
          "select distinct eor.ref, con.groupname, con.symbol, ch.ref channel " +
            "from ecomchannels ch " +
              "join sys_edaconnectors con on con.ref = ch.ref " +
              "join ecomorders eor on eor.ecomchannelref = ch.ref " +
            $"where ch.ref = 0{ecomChannelRef} and eor.syncsendchanellstatus = 0{(int)EcomSyncStatus.ExportPending}";
      }
      else
      {
        return "Nie podano adaptera, konektora ani kanału sprzedaży";
      }
    
      var results = LOGIC.ECOMADAPTERCOMMON.GetExportOrdersData(sqlString);
      if((results?.Count ?? 0) == 0)
      {
        return "Nie znaleziono zamówienia w kanałach sprzedaży";
      }
      else
      { 
        try 
        {
          foreach(var result in results)
          {
            //[ML] wysyłka komendy aktualizacji zamówienia na stronie powiązanej z kanałem sprzedaży do dorobienia
            throw new NotImplementedException("Aktualizacja zamówień na stronie nieobsługiwana (do dopisania)");
            successCnt++;        
          }
        }
        catch(Exception ex)
        { 
          failCnt++;
          message += ex.Message + "\n";    
        } 
      } 
      message = $"Wysłano {successCnt.ToString()} komend. Błąd dla {failCnt.ToString()} komend.\n" + message;
      return message;
    }
  }
	//ERRSOURCE: structure GetOrdersListCommand { ... } in object ECOM.ECOMADAPTERCOMMON
  [CustomData("DataStructure=Y")]
  [UUID("0dd63d1a5ed84b2ab97767f1b53ab003")]
  public class GetOrdersListCommand : NeosCommand
  {
      public string Message{get; set;}
      public int EcomChannel {get; set;}   
      public importMode ImportMode{get; set;} //określja tryb dla jakiego mają byc pobierane zamówienia;    
      public DateTime? OrderDateFrom {get; set;}
      public DateTime? OrderDateTo {get; set;}     
  }
  
  
	//ERRSOURCE: structure ExportOrdersDataRow { ... } in object ECOM.ECOMADAPTERCOMMON
  /// <summary>
  /// [ML] do refaktoryzacji w momencie realizacji eksportu zamówień
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("10e9d6a3ee2e47c1a52f20fd31a87404")]
  public class ExportOrdersDataRow 
  {
      public int channelRef;
      public int orderRef;
      public string connectorGroupName;
      public string connectorSymbol;
  }
  
  
  
	//ERRSOURCE: structure ExportOrdersStatusCommand { ... } in object ECOM.ECOMADAPTERCOMMON
  /// <summary>
  /// Komenda eksportu statusuów zamóeń. Zawiera listę struktur uniwresalnych EcomOrderStatusInfo(lub rozszeroznych uniwersalnych) które będą wysyłane do kanału sprzedaży
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("1d8d86e1fb0148558e9d6e3d600e72f3")]
  public class ExportOrdersStatusCommand : NeosCommand
  {
      public int EcomChannel {get; set;}
      public List<EcomOrderStatusInfo> OrderStatusInfoList {get; set;}     
      public string Message {get; set;}
  }
  
  
	//ERRSOURCE: structure GetEDAIdentifierData { ... } in object ECOM.ECOMADAPTERCOMMON
  /// <summary>
  /// Klasa zawierająca dane zwracane z metod GetEDAIdentifier Zawiera identyfikator EDA dla obiektu dla którego została wywolana metoda np. zamówienia, towaru itp.
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("23b46eba9a834c2c985a3e7de6e36847")]
  public class GetEDAIdentifierData 
  {
      public bool Result {get; set;}
      public string ErrorMsg {get; set;}
      public string EDAIdentifier {get; set;}
  }
  
  
  
	//ERRSOURCE: structure ExportInventoryPricesCommand { ... } in object ECOM.ECOMADAPTERCOMMON
  [CustomData("DataStructure=Y")]
  [UUID("368edfd04c394330b20f6de2b9596aa8")]
  public class ExportInventoryPricesCommand : NeosCommand
  {
      ///<summary>Ref kanału sprzadaży</summary>
      public int EcomChannel {get; set;}
      ///<summary>Symbol kanału sprzedaży na witrynie</summary>
      public string EcomChannelSymbol {get; set;}
      public string Message {get; set;}
      public List<EcomInventoryPriceInfo> InventoryPricesInfoList {get; set;}
  }
  
  
	//ERRSOURCE: structure ExportInventoryStocksCommand { ... } in object ECOM.ECOMADAPTERCOMMON
  [CustomData("DataStructure=Y")]
  [UUID("3c21f152b09c4461a4c297fa35f66494")]
  public class ExportInventoryStocksCommand : NeosCommand
  {
      ///<summary>Ref kanału sprzadaży</summary>
      public int EcomChannel {get; set;}
      ///<summary>Symbol kanału sprzedaży na witrynie</summary>
      public string EcomChannelSymbol {get; set;}
      public string Message {get; set;}
      public List<EcomInventoryStockInfo> InventoryStocksInfoList {get; set;}
  }
  
  
	//ERRSOURCE: structure importMode { ... } in object ECOM.ECOMADAPTERCOMMON
  /// <summary>
  /// Typ wyliczeniowy zawierający możliwe tryby pobierania zamówień 
  /// 
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("4880d35eb3e546b38ff59c55ab5e770a")]
  public enum importMode 
  {
      ///<summary>od daty ostatniego importu</summary>
      SinceLastImport = 0,
      ///<summary>w wybranym przedziale czasowym</summary>
      DateRange,
      ///<summary>lisat wybranych</summary>
      Selected,
      ///<summary>wszystkie</summary>
      All,
      ///<summary>Konkretne zamówienie o podanym id z witryny kanału</summary>
      OrderId
  }
  
  
	//ERRSOURCE: structure ImportOrderCommand { ... } in object ECOM.ECOMADAPTERCOMMON
  /// <summary>
  /// Komenda wykorzystywana przy imporcie zamówienia. Zawiera dane zamówienia pobrane w witryny
  /// w formacie uniwersalnej klasy do importu zamówień
  /// - Message - pole na komunikaty łatwo widoczne w monitorze EDA np. treść błędów przy obsłudze komendy przez handler
  /// - EcomChannel - ref kanału sprzedaży, w którym odbywa się synchronizacja zamówienia
  /// - OrderInfo - dane zamówienia pobrane z witryny
  /// 
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("5e1698b2f45f4e8a8803c199d9207a17")]
  public class ImportOrderCommand : NeosCommand
  {
      public string Message {get; set;}
      public int EcomChannel {get; set;} 
      public EcomOrderInfo OrderInfo {get; set;}
      public importMode ImportMode{get; set;}    
  }
  
  
	//ERRSOURCE: structure ExportMode { ... } in object ECOM.ECOMADAPTERCOMMON
  /// <summary>
  /// Typ wyliczeinowy z listą trybów w jakich może przebiegać eksport danych do kanału sprzedaży
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("8345e749088248c093e7fec113ccd7ce")]
  public enum ExportMode 
  {
      ///<summary>Eksport zmienionych</summary>
      LastChange = 0,
      ///<summary>Eksport listy zamówień, towarów itp.</summary>
      List,
      ///<summary>Eksport wszystkich zamówień, towarów itp.</summary>
      All    
  }
  
  
  
	//ERRSOURCE: structure GetPricesListCommand { ... } in object ECOM.ECOMADAPTERCOMMON
  [CustomData("DataStructure=Y")]
  [UUID("85065b2af9fe4500b72e72f188ec4aea")]
  public class GetPricesListCommand : NeosCommand
  {
      public string Message{get; set;}
      public int EcomChannel {get; set;} 
      public List<String> InventoryIdList {get; set;}
  }
  
  
	//ERRSOURCE: structure RequestMode { ... } in object ECOM.ECOMADAPTERCOMMON
  /// <summary>
  /// Typ wyliczeniowy oznaczający w jakim trybie działają metody do synchronizacji uruchamiane przez schedulera. 
  /// Obecnie wykorzystywane przy eksporcie towarów i imporcie zamówień w metodach uruchamianych z schedulera
  /// 
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("8c51c3c07a0c4d3882107b7fb7ae750c")]
  public enum RequestMode
  {
      ///<summary>dla kanałów sprzedaży powiązanych z adapterem</summary>
      Adapter = 0,
      ///<summary>dla kanałów sprzedaży powiązanych z konektorem</summary>
      Connector,
      ///<summary>dla wybranego kanału sprzedaży</summary>
      Channel
  }
  
  
	//ERRSOURCE: structure ExportInventoryCommand { ... } in object ECOM.ECOMADAPTERCOMMON
  /// <summary>
  /// Komenda eksportu towarów. Zawiera listę struktur uniwresalnych EcomInventoryInfo (lub rozszeroznych uniwersalnych) o towarach, które będą wysyłane do kanału sprzedaży
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("a860d984bab3449291aa8c77e91159a8")]
  public class ExportInventoryCommand : NeosCommand
  {
      ///<summary>Ref kanału sprzadaży</summary>
      public int EcomChannel {get; set;}
      ///<summary>Symbol kanału sprzedaży na witrynie</summary>
      public string EcomChannelSymbol {get; set;}
      public string Message {get; set;}
      public List<EcomInventoryInfo> InventoryInfoList {get; set;}
  }
  
  
	//ERRSOURCE: structure EcomSyncStatus { ... } in object ECOM.ECOMADAPTERCOMMON
  /// <summary>
  /// Typ wyliczeinowy z listą statusów jakie mogą przyjmować obiekty synchronizowane w kanale sprzedaży (towary, zamówienia, statusy zamówień itp.)
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("ac4818ce1f7343fab7d769224e1fe7af")]
  public enum EcomSyncStatus
  {
      ///<summary>Niesynchronizowane</summary>
      Unsynchronizable = 0,
      ///<summary>Wygenerowano już komendy importowe, ale nie są jeszcze przetworzone</summary>
      ImportProceeding = 12,
      ///<summary>Poprawnie przetworzono komendy importowe</summary>
      Imported = 13,
      ///<summary>Błąd importu</summary>
      ImportError = 15,
      ///<summary>Wiemy, że potrzebny jest eksport, ale komenda jeszcze nie wygenerowana</summary>
      ExportPending = 21,
      ///<summary>Komenda wysyłająca dane juz wygnerowana, ale jeszcze nie zakończona</summary>
      ExportProceeding = 22,
      ///<summary>Komenda wysyłajaca dane się poprawnie przetworzona.</summary>
      Exported = 23,
      ///<summary>Błąd exportu</summary>
      ExportError = 25
  }
  
  
	//ERRSOURCE: structure ImportOrdersPaymentsRequestCommand { ... } in object ECOM.ECOMADAPTERCOMMON
  [CustomData("DataStructure=Y")]
  [UUID("e6e3af4cdb7c44be8d9d2c7096614e07")]
  public class ImportOrdersPaymentsRequestCommand : NeosCommand
  {
      public string Message{get; set;}
      public int EcomChannel {get; set;}   
      public List<int> OrderList {get; set;}   
  }
  
  
	//ERRSOURCE: structure ImportOrdersRequestCommand { ... } in object ECOM.ECOMADAPTERCOMMON
  /// <summary>
  /// Komenda zawiera parametry potrzebne do pobrania zamówień w wybranym trybie (wybrane zamówienia, zakres dat, od ostatniego importu) 
  /// Pola:
  /// - Message - pole na komunikaty łatwo widoczne w monitorze EDA np. treść błędów przy obsłudze komendy przez handler
  /// - EcomChannel - ref kanału sprzedaży, w którym odbywa się eksport towaru
  /// - ImportMode - tryb pobierania zamówień określany przez typ wyliczeniowy importOrderMode 
  /// - LastOrderImportDate - data ostaniego pobierania zamówień, używana w trybie pobierania od ostatniej daty
  /// - OrderDateFrom - początek okresu z którego pobierane są zamowienia wykorzystywany przy pobieraniu zamowień z wybranego okresu 
  /// - OrderDateTo - koniec okresu z którego pobierane są zamowienia wykorzystywany przy pobieraniu zamowień z wybranego okresu
  /// - OrderList - lista zamówień, które mają być pobrane, wykorzystywana przy pobieraniu wybranych zamówień
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("e8caf7bf0d7545a8a842d2d7f88a06c6")]
  public class ImportOrdersRequestCommand : NeosCommand
  { 
      public string Message{get; set;}
      public int EcomChannel {get; set;}   
      public importMode ImportMode{get; set;} //określja tryb dla jakiego mają byc pobierane zamówienia;    
      public DateTime? OrderDateFrom {get; set;}
      public DateTime? OrderDateTo {get; set;}  
      public List<string> OrderList {get; set;}   
  }
  
  
	//ERRSOURCE: structure GetStocksListCommand { ... } in object ECOM.ECOMADAPTERCOMMON
  [CustomData("DataStructure=Y")]
  [UUID("fbce8b4ea12748ef86b63359223431cb")]
  public class GetStocksListCommand : NeosCommand
  {
      public string Message{get; set;}
      public int EcomChannel {get; set;} 
      public List<String> InventoryIdList {get; set;}
  }
  [CustomData("DataStructure=Y")]
  [UUID("1cf5e61b3ce84065b37685855be25f25")]
  public class ImportOrderPackagesRequestCommand : NeosCommand
  { 
      public string Message{get; set;}
      public int EcomChannel {get; set;}   
      public int OrderId {get; set;}   
  }
}
