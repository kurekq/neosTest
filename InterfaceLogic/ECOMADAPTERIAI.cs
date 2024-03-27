
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
//ERRSOURCE: ECOM.ECOMADAPTERIAI

  public partial class ECOMADAPTERIAI
  {
    /// <summary>
    /// metoda otwiera stronę dot. cen towarów w panelu administratora sklepu internetowego w przeglądarce internetowej
    /// </summary>
    /// <param name="connectorRef"></param>
    /// <param name="ecomPriceRef"></param>
    [UUID("046a02fa255a4334a8a322773f3c8dfb")]
    override public void DoOpenPriceInAdminPanel(int connectorRef, int ecomPriceRef)
    {
      //metoda otwiera stronę dot. cen towarów w panelu administratora sklepu internetowego w przeglądarce internetowej   
      //netoda przyjmuje ecomPriceRef, ponieważ ceny towarów nie mają swojego identyfikatora w kanale sprzedaży 
      var ecomPrice = new ECOM.ECOMINVENTORYPRICES();
      var connector = new SYSTEM.EDACONNECTORS();
      connector.FilterAndSort($"SYS_EDACONNECTORS.{connector.REF.Symbol} = 0{connectorRef}");
      if(!connector.FirstRecord())
      {
        throw new Exception($"Nie znaleziono konektora o REF: {connectorRef}");    
      }
    
      var connectorParams = EDA.CONNECTORS.Get($"{connector.GROUPNAME}.{connector.SYMBOL}").Params;  
      ecomPrice.FilterAndSort($"{nameof(ECOM.ECOMINVENTORYPRICES)}.{ecomPrice.REF.Symbol} = 0{ecomPriceRef}");
      if(ecomPrice.FirstRecord())
      {
        var ecomInventory = new ECOM.ECOMINVENTORIES();
        ecomInventory.FilterAndSort($"{nameof(ECOM.ECOMINVENTORIES)}.{ecomInventory.REF.Symbol} = 0{ecomPrice.ECOMINVENTORYREF}");
        if(ecomInventory.FirstRecord())
        { 
          string linkString = $"{connectorParams["basedomain"]}/panel/product.php?idt={ecomInventory.ECOMINVENTORYID}#prices";
          GUI.ShellExecute(linkString, null);
        }
        else
        {
          throw new Exception($"Nie znaleziono towaru o REF: {ecomPrice.ECOMINVENTORYREF}");
        }
      }
      else
      {
        throw new Exception($"Nie znaleziono wpisu ceny o REF: {ecomPriceRef}");
      }      
      connector.Close();      
    }
    
    

    /// <summary>
    /// metoda otwiera stronę dot. stanu magazynowego towaru w panelu administratora sklepu internetowego w domyślnej przeglądarce
    /// </summary>
    /// <param name="connectorRef"></param>
    /// <param name="ecomInventoryStockRef"></param>
    [UUID("49f517ea9ebb437fb36277ef3f262299")]
    override public void DoOpenInventoryStockInAdminPanel(int connectorRef, int ecomInventoryStockRef)
    {
      //metoda otwiera stronę dot. stanów towarów w panelu administratora sklepu internetowego w przeglądarce internetowej   
      //netoda przyjmuje ecomInventoryStockRef, ponieważ stany towarów nie mają swojego identyfikatora w IAI
      var ecomInventoryStock = new ECOM.ECOMINVENTORYSTOCKS();
      var connector = new SYSTEM.EDACONNECTORS();
      connector.FilterAndSort($"SYS_EDACONNECTORS.{connector.REF.Symbol} = 0{connectorRef}");
      if(!connector.FirstRecord())
      {
        throw new Exception($"Nie znaleziono konektora o REF: {connectorRef}");    
      }
      
      var connectorParams = EDA.CONNECTORS.Get($"{connector.GROUPNAME}.{connector.SYMBOL}").Params;
      ecomInventoryStock.FilterAndSort($"{nameof(ECOM.ECOMINVENTORYSTOCKS)}.{ecomInventoryStock.REF.Symbol} = 0{ecomInventoryStockRef}"); 
      if(ecomInventoryStock.FirstRecord()) 
      {    
        var ecomInventory = new ECOM.ECOMINVENTORIES();
        ecomInventory.FilterAndSort($"{nameof(ECOM.ECOMINVENTORIES)}.{ecomInventory.REF.Symbol} = 0{ecomInventoryStock.ECOMINVENTORYREF}"); 
        if(ecomInventory.FirstRecord()) 
        {
          string linkString = $"{connectorParams["basedomain"]}/panel/product.php?idt={ecomInventory.ECOMINVENTORYID}#stocks";    
          GUI.ShellExecute(linkString, null);
        }
        else
        {
          throw new Exception($"Nie znaleziono towaru o REF: {ecomInventoryStock.ECOMINVENTORYREF}");
        }
      }
      else
      {
        throw new Exception($"Nie znaleziono wpisu ceny o REF: {ecomInventoryStockRef}");
      }  
      connector.Close();        
    }
    
    

    /// <param name="connector"></param>
    /// <param name="inventoryId"></param>
    [UUID("7f4ce645223744acbdb5fb1c5ad94a97")]
    override public void DoOpenInventoryInAdminPanel(int connector, string inventoryId)
    {
      string domain = LOGIC.ECOMADAPTERIAI.OpenOrderInAdminPanelLogic(connector);  
      string linkString = $"{domain}/panel/product.php?idt={inventoryId.ToString()}";  
      GUI.ShellExecute(linkString, null);
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
    override public void DoOpenOrderInAdminPanel(int connector, string orderId)
    { 
      string domain = LOGIC.ECOMADAPTERIAI.OpenOrderInAdminPanelLogic(connector);  
      string linkString = $"{domain}/panel/orderd.php?idt={orderId.ToString()}";  
      GUI.ShellExecute(linkString, null); 
    }
  }
	//ERRSOURCE: structure PutOrderStatusResponseCommand { ... } in object ECOM.ECOMADAPTERIAI
  [CustomData("DataStructure=Y")]
  [UUID("0ba763ee7e3a4cae987def8e42f8f87c")]
  public class PutOrderStatusResponseCommand : NeosCommand
  {
      public string Message {get; set;}
      public ExportMode exportMode {get; set;} // na razie nieużywane
      public int EcomChannel {get; set;}    
      public EComIAIDriver.IAIUpdateOrders.responseType ApiResponse {get;set;}
  }
  
  
	//ERRSOURCE: structure PutProductsStocksRequestCommand { ... } in object ECOM.ECOMADAPTERIAI
  [CustomData("DataStructure=Y")]
  [UUID("18ff794ead92416c8633bf59c6cb5bdc")]
  public class PutProductsStocksRequestCommand : NeosCommand
  {
      public string Message {get;set;}
      public int EcomChannel {get; set;}   
      /// ref z tabeli ECOMINVENTORIES
      public int InventoryRef {get; set;} 
      public int ChannelStockRef {get; set;}     
      public EComIAIDriver.IAIUpdateProductStock.requestType ApiRequest {get; set;}  
  }
  
  
	//ERRSOURCE: structure PutOrderStatusRequestCommand { ... } in object ECOM.ECOMADAPTERIAI
  /// <summary>
  /// Komenda zawierająca strukturę danych ze statusami towarów, która zostanie wysłana do fizycznego sterownika IAI 
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("283bbbb619784d8b893e37835c193b80")]
  public class PutOrderStatusRequestCommand : NeosCommand
  {
      public string Message {get; set;}
      public ExportMode exportMode {get; set;} // na razie nieużywane
      public int EcomChannel {get; set;} 
      public string EcomOrderId {get; set;}   
      public EComIAIDriver.IAIUpdateOrders.requestType ApiRequest {get; set;}
      ///<summary>
      ///zapytanie do API do zakładania opakowań, które od strony Teneum
      ///wysyłamy jako status, ale w IAI to jest ozobna metoda API
      ///</summary>
      public EComIAIDriver.IAIAddPackages.addPackagesRequestType ApiPackagesRequest {get; set;}      
  }
  
  
	//ERRSOURCE: structure PutProductPriceRequestCommand { ... } in object ECOM.ECOMADAPTERIAI
  [CustomData("DataStructure=Y")]
  [UUID("33824c7d99324963b549936d4bc6f3f6")]
  public class PutProductPriceRequestCommand : NeosCommand
  {
      public string Message {get;set;}
      public int EcomChannel {get; set;}        
      public EComIAIDriver.IAIUpdateProducts.requestType ApiRequest {get; set;}    
  }
  
  
	//ERRSOURCE: structure PutAttachmentRequestCommand { ... } in object ECOM.ECOMADAPTERIAI
  [CustomData("DataStructure=Y")]
  [UUID("33bd87aee9c142b28976e4d25438d965")]
  public class PutAttachmentRequestCommand : NeosCommand
  {
      public string Message {get; set;}
      public int EcomChannel {get; set;}    
      public ExportMode exportMode {get; set;} // na razie nieużywane
      public EComIAIDriver.IAIInsertDocuments.insertDocumentsRequestType ApiInsertAttachmentRequest {get; set;}
  }
  
  
	//ERRSOURCE: structure PutProductsStocksResponseCommand { ... } in object ECOM.ECOMADAPTERIAI
  [CustomData("DataStructure=Y")]
  [UUID("3f621388e9cd433aa0a1099cc24f9d1a")]
  public class PutProductsStocksResponseCommand : NeosCommand
  {
      public string Message {get;set;} 
      public int EcomChannel {get; set;}   
      /// ref z tabeli ECOMINVENTORIES
      public int InventoryRef {get; set;} 
      public int ChannelStockRef {get; set;}
      public EComIAIDriver.IAIUpdateProductStock.responseType ApiResponse {get;set;}
  }
  
  
	//ERRSOURCE: structure PutOrderPacakgesRequestCommand { ... } in object ECOM.ECOMADAPTERIAI
  [CustomData("DataStructure=Y")]
  [UUID("4ab05325ac7147bfa223f8962496fc07")]
  public class PutOrderPacakgesRequestCommand : NeosCommand
  {
      public string Message {get; set;}
      public ExportMode exportMode {get; set;} // na razie nieużywane
      public int EcomChannel {get; set;} 
      ///<summary>
      ///zapytanie do API do zakładania opakowań, które od strony Teneum
      ///wysyłamy jako status, ale w IAI to jest ozobna metoda API
      ///</summary>
      public EComIAIDriver.IAIAddPackages.addPackagesRequestType ApiPackagesRequest {get; set;}      
  
  }
  
  
	//ERRSOURCE: structure GetOrdersResponseCommand { ... } in object ECOM.ECOMADAPTERIAI
  /// <summary>
  /// Komenda procesu pobrania zamówień w danym kanale sprzedaży zawierająca odpowiedź API z listą zamówień
  /// pola:
  /// - Message - pole na komunikaty łatwo widoczne w monitorze EDA np. treść błędów przy obsłudze komendy przez handler
  /// - EcomChannel kanal sprzedaży, dla którego importowane są zamówienia
  /// - ApiResponse - struktura zawierająca dane zamówień pobranych z API
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("708ff049e4c149768be88016bbab8fd8")]
  public class GetOrdersResponseCommand : NeosCommand
  {
      public string Message {get;set;}
      public importMode ImportMode {get; set;}
      public int EcomChannel {get; set;} 
      public EComIAIDriver.IAIOrders.responseType ApiResponse {get;set;}
  }
  
  
	//ERRSOURCE: structure PutProductRequestCommand { ... } in object ECOM.ECOMADAPTERIAI
  [CustomData("DataStructure=Y")]
  [UUID("857fdbbcb40c491eb3190f78756b1842")]
  public class PutProductRequestCommand : NeosCommand
  {
      public string Message {get;set;}
      public int EcomChannel {get; set;}
      ///<summary>ref wersji produktu</summary>
      public int EcomInventoryVersion {get; set;}  
      public EComIAIDriver.IAIUpdateProducts.requestType ApiRequest {get; set;}    
  }
  
  
	//ERRSOURCE: structure PutProductResponseCommand { ... } in object ECOM.ECOMADAPTERIAI
  [CustomData("DataStructure=Y")]
  [UUID("947d8e5e5a57457c890f0302310496fa")]
  public class PutProductResponseCommand : NeosCommand
  {
      public string Message {get;set;}
      ///<summary>ref kanału sprzedaży </summary>    
      public int EcomChannel {get; set;}
      ///<summary>ref wersji</summary>
      public int EcomInventoryVersion {get; set;}  
      public EComIAIDriver.IAIUpdateProducts.responseType ApiResponse {get;set;}
  }
  
  
	//ERRSOURCE: structure ExportIAIOrderRequestCommand { ... } in object ECOM.ECOMADAPTERIAI
  /// <summary>
  /// Komenda procesu aktualizacji zamówienia w danym kanale sprzedaży zawierająca dane niezbędne do wysyłki zamówienia do API
  /// W IAI używana do eksportu statusów zamówienia
  /// pola:
  /// - OrderRef - ref zamówienia z tabeli NAGZAM
  /// - Message - pole na komunikaty łatwo widoczne w monitorze EDA np. treść błędów przy obsłudze komendy przez handler
  /// - ApiRequest - struktura zawierająca dane zamówienia, które zostaną wysłane na witrynę 
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("a5c56323f82348fa91cc4605456730ce")]
  public class ExportIAIOrderRequestCommand : NeosCommand
  {
      public int OrderRef {get; set;}
      public string Message {get;set;}
      public EComIAIDriver.IAIUpdateOrders.requestType ApiRequest {get; set;}   
  }
  
  
	//ERRSOURCE: structure PutProductPriceResponseCommand { ... } in object ECOM.ECOMADAPTERIAI
  [CustomData("DataStructure=Y")]
  [UUID("b7780c3936eb4bed869966593a66f85a")]
  public class PutProductPriceResponseCommand : NeosCommand
  {
      public string Message {get;set;}
      ///<summary>ref kanału sprzedaży </summary>    
      public int EcomChannel {get; set;}    
      public EComIAIDriver.IAIUpdateProducts.responseType ApiResponse {get;set;}
  }
  
  
	//ERRSOURCE: structure ExportIAIOrderResponseCommand { ... } in object ECOM.ECOMADAPTERIAI
  /// <summary>
  /// Komenda procesu aktualizacji zamówienia w danym kanale sprzedaży zawierająca odpowiedź API na próbę wysyłki zamówienia
  /// W IAI używana do eksportu statusów zamówienia
  /// pola:
  /// - OrderRef - ref zamówienia z tabeli NAGZAM
  /// - Message - pole na komunikaty łatwo widoczne w monitorze EDA np. treść błędów przy obsłudze komendy przez handler
  /// - ApiResponse - struktura zawierająca nieprzetworzoną odpowiedź API do wysyłki/ aktualizowania towarów
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("c968491b8b844addbf16f97d53136acc")]
  public class ExportIAIOrderResponseCommand : NeosCommand
  {
      public int OrderRef {get; set;}
      public string Message {get;set;}
      public EComIAIDriver.IAIUpdateOrders.responseType ApiResponse {get;set;}
  }
  
  
	//ERRSOURCE: structure PutAttachmentResponseCommand { ... } in object ECOM.ECOMADAPTERIAI
  [CustomData("DataStructure=Y")]
  [UUID("d1bef6c1ee0941da8c538ed0a3545e94")]
  public class PutAttachmentResponseCommand : NeosCommand
  {
      public string Message {get; set;}
      public ExportMode exportMode {get; set;} // na razie nieużywane
      public int EcomChannel {get; set;}    
      public EComIAIDriver.IAIInsertDocuments.insertDocumentsResponseType ApiInsertAttachmentResponse {get;set;}
  }
  
  
	//ERRSOURCE: structure GetOrdersRequestCommand { ... } in object ECOM.ECOMADAPTERIAI
  /// <summary>
  /// Komenda procesu pobrania zamówień w danym kanale sprzedaży zawierająca dane niezbędne do pobrania zamówień z API
  /// pola:
  /// - Message - pole na komunikaty łatwo widoczne w monitorze EDA np. treść błędów przy obsłudze komendy przez handler
  /// - ImportMode - tryb pobrania zamówień określany przez typ wyliczeniowy importOrderMode
  /// - EcomChannel kanal sprzedaży, dla którego importowane są zamówienia
  /// - ApiRequest - struktura zawierająca dane na podstawie których będą pobrane zamówienia
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("f3750c7a618a4e45bca43bb8ffcd9ff8")]
  public class GetOrdersRequestCommand : NeosCommand
  { 
      public string Message {get; set;}
      public importMode ImportMode {get; set;}
      public int EcomChannel {get; set;}    
      public EComIAIDriver.IAIOrders.requestType ApiRequest {get; set;}        
  }
}
