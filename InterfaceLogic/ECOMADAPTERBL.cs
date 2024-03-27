
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
//ERRSOURCE: ECOM.ECOMADAPTERBL

  public partial class ECOMADAPTERBL
  {
    /// <summary>
    /// metoda otwiera stronę dot. cen towarów w panelu administratora sklepu internetowego w przeglądarce internetowej
    ///   
    /// </summary>
    /// <param name="connector"></param>
    /// <param name="ecomPriceRef"></param>
    [UUID("046a02fa255a4334a8a322773f3c8dfb")]
    override public void DoOpenPriceInAdminPanel(int connector, int ecomPriceRef)
    {
      string domain = LOGIC.ECOMADAPTERBL.OpenOrderInAdminPanelLogic(connector);  
      string linkString = $"{domain}/inventory_products.php#edit-general:{ecomPriceRef}";  
      GUI.ShellExecute(linkString, null); 
    }
    
    

    /// <summary>
    /// metoda otwiera stronę dot. stanu magazynowego towaru w panelu administratora sklepu internetowego w domyślnej przeglądarce
    /// </summary>
    /// <param name="connectorRef"></param>
    /// <param name="ecomInventoryStockRef"></param>
    [UUID("49f517ea9ebb437fb36277ef3f262299")]
    override public void DoOpenInventoryStockInAdminPanel(int connectorRef, int ecomInventoryStockRef)
    {
      string domain = LOGIC.ECOMADAPTERBL.OpenOrderInAdminPanelLogic(connectorRef);  
      string linkString = $"{domain}/inventory_products.php#edit-general:{ecomInventoryStockRef}";  
      GUI.ShellExecute(linkString, null); 
    }
    
    

    /// <param name="connector"></param>
    /// <param name="inventoryId"></param>
    [UUID("7f4ce645223744acbdb5fb1c5ad94a97")]
    override public void DoOpenInventoryInAdminPanel(int connector, string inventoryId)
    {
      string domain = LOGIC.ECOMADAPTERBL.OpenOrderInAdminPanelLogic(connector);  
      string linkString = $"{domain}/inventory_products.php#edit-general:{inventoryId}";  
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
      string domain = LOGIC.ECOMADAPTERBL.OpenOrderInAdminPanelLogic(connector);  
      string linkString = $"{domain}/orders.php#order:{orderId}";  
      GUI.ShellExecute(linkString, null); 
    }
  }
	//ERRSOURCE: structure BLPutOrderStatusResponseCommand { ... } in object ECOM.ECOMADAPTERBL
  [CustomData("DataStructure=Y")]
  [UUID("1e70d951a0ec4bc2953720a982754564")]
  public class BLPutOrderStatusResponseCommand : NeosCommand
  {
      public string Message {get; set;}
      public int EcomOrderId {get; set;}
      public int EcomChannel {get; set;}  
      public EComBLDriver.Models.BLExportOrderStatusResponse ApiResponse {get;set;}
  }
  
  
	//ERRSOURCE: structure BLPutProductResponseCommand { ... } in object ECOM.ECOMADAPTERBL
  [CustomData("DataStructure=Y")]
  [UUID("31579db5da7e4ac0a9ff656cc845aea5")]
  public class BLPutProductResponseCommand : NeosCommand
  {
      public string Message {get;set;}
      ///<summary>ref kanału sprzedaży </summary>    
      public int EcomChannel {get; set;}
      ///<summary>ref wersji</summary>
      public int EcomInventoryVersion {get; set;}  
      public EComBLDriver.Models.BLUpdateProductResponse ApiResponse {get; set;} 
  }
  
  
	//ERRSOURCE: structure BLPutProductPriceRequestCommand { ... } in object ECOM.ECOMADAPTERBL
  [CustomData("DataStructure=Y")]
  [UUID("3865c67bdc554027a3e2de9facfc3af6")]
  public class BLPutProductPriceRequestCommand : NeosCommand
  {
      public string Message {get;set;}
      public int EcomChannel {get; set;}        
      public EComBLDriver.Models.BLUpdateProductPriceRequest ApiRequest {get; set;}    
      public List<string> ProductsToSendList {get; set;}
  }
  
  
	//ERRSOURCE: structure BLPutOrderPacakgeRequestCommand { ... } in object ECOM.ECOMADAPTERBL
  [CustomData("DataStructure=Y")]
  [UUID("39ca4b332c16457a8f35d73778b1e799")]
  public class BLPutOrderPacakgeRequestCommand : NeosCommand
  {
      public string Message {get; set;}
      public int EcomChannel {get; set;} 
      public EComBLDriver.Models.BLAddPackagesRequest ApiPackageRequest {get; set;}
  }
  
  
	//ERRSOURCE: structure BLPutAttachmentRequestCommand { ... } in object ECOM.ECOMADAPTERBL
  [CustomData("DataStructure=Y")]
  [UUID("3b30778df9544c1db805298f5b6f33b9")]
  public class BLPutAttachmentRequestCommand : NeosCommand
  {
      public string Message {get; set;}
      public int EcomChannel {get; set;}    
      public string EcomOrderId {get; set;}   
      public EComBLDriver.Models.BLAddOrderInvoiceRequest ApiInsertAttachmentRequest {get; set;}
  }
  
  
	//ERRSOURCE: structure BLPutProductPriceResponseCommand { ... } in object ECOM.ECOMADAPTERBL
  [CustomData("DataStructure=Y")]
  [UUID("3bab95586ecb4c09b1d639a4fa571015")]
  public class BLPutProductPriceResponseCommand : NeosCommand
  {
      public string Message {get;set;}
      ///<summary>ref kanału sprzedaży </summary>    
      public int EcomChannel {get; set;}    
      public EComBLDriver.Models.BLUpdateProductPriceResponse ApiResponse {get;set;}
      public List<string> ProductsToSendList {get; set;}
  }
  
  
	//ERRSOURCE: structure BLGetOrdersResponseCommand { ... } in object ECOM.ECOMADAPTERBL
  [CustomData("DataStructure=Y")]
  [UUID("51eb192872e444f28a7f1e1a1ab1db80")]
  public class BLGetOrdersResponseCommand : NeosCommand
  {
      public string Message {get;set;}
      public importMode ImportMode {get; set;}
      public int EcomChannel {get; set;} 
      public EComBLDriver.Models.BLGetOrdersResponse BLApiResponse {get;set;}
  }
  
  
	//ERRSOURCE: structure BLPutProductRequestCommand { ... } in object ECOM.ECOMADAPTERBL
  [CustomData("DataStructure=Y")]
  [UUID("5e58c36146a3434097929c0e05489d82")]
  public class BLPutProductRequestCommand : NeosCommand
  {
      public string Message {get;set;}
      public int EcomChannel {get; set;}
      ///<summary>ref wersji produktu</summary>
      public int EcomInventoryVersion {get; set;}  
      public EComBLDriver.Models.BLUpdateProductRequest ApiRequest {get; set;} 
  }
  
  
	//ERRSOURCE: structure BLPutOrderStatusRequestCommand { ... } in object ECOM.ECOMADAPTERBL
  [CustomData("DataStructure=Y")]
  [UUID("65b79bcef2554ea0b0cd06af73a4fb24")]
  public class BLPutOrderStatusRequestCommand : NeosCommand
  {
      public string Message {get; set;}
      public int EcomChannel {get; set;} 
      public int EcomOrderId {get; set;}   
      public EComBLDriver.Models.BLExportOrderStatusRequest ApiRequest {get; set;}
  }
  
  
	//ERRSOURCE: structure BLGetOrdersRequestCommand { ... } in object ECOM.ECOMADAPTERBL
  [CustomData("DataStructure=Y")]
  [UUID("86d1f7b9dfd8426ea423a18a5f8626b6")]
  public class BLGetOrdersRequestCommand : NeosCommand
  {    
      public string Message {get; set;}
      public importMode ImportMode {get; set;}
      public int EcomChannel {get; set;}    
      public EComBLDriver.Models.BLGetOrdersRequest BLApiRequest {get; set;}      
  }
  
  
	//ERRSOURCE: structure BLImportOrderLogsResponseCommand { ... } in object ECOM.ECOMADAPTERBL
  [CustomData("DataStructure=Y")]
  [UUID("8731af25db4c4629803230258a9e67f5")]
  public class BLImportOrderLogsResponseCommand : NeosCommand
  {
      public string Message {get; set;}
      public int EcomChannel {get; set;}    
      public string EcomOrderId {get; set;}   
      public EComBLDriver.Models.BLImportOrderLogsResponse ApiResponse {get; set;}
  }
  
  
	//ERRSOURCE: structure BLPutInvoiceCarrierRequestCommand { ... } in object ECOM.ECOMADAPTERBL
  [CustomData("DataStructure=Y")]
  [UUID("a7e84bc0edea4bffa4f3ec29126c2a99")]
  public class BLPutInvoiceCarrierRequestCommand : NeosCommand
  {
      public string Message {get; set;}
      public int EcomChannel {get; set;}     
      public int EcomOrderId {get; set;}  
      public string InvoiceFilePath {get; set;} 
      public string AttachmentSymbol {get; set;} 
      public EComBLDriver.Models.BLAddOrderInvoiceCarrierRequest ApiInvoiceCarrierRequest {get; set;}
  }
  
  
	//ERRSOURCE: structure BLPutProductsStocksRequestCommand { ... } in object ECOM.ECOMADAPTERBL
  [CustomData("DataStructure=Y")]
  [UUID("b609a83089554766b898143051e41737")]
  public class BLPutProductsStocksRequestCommand : NeosCommand
  {
      public string Message {get;set;}
      public int EcomChannel {get; set;}   
      public EComBLDriver.Models.BLUpdateProductStockRequest ApiRequest {get; set;}  
  }
  
  
	//ERRSOURCE: structure BLImportOrderLogsRequestCommand { ... } in object ECOM.ECOMADAPTERBL
  [CustomData("DataStructure=Y")]
  [UUID("b73c585edbb54f439f61f18067b64cb4")]
  public class BLImportOrderLogsRequestCommand : NeosCommand
  {
      public string Message {get; set;}
      public int EcomChannel {get; set;}    
      public string EcomOrderId {get; set;}   
      public EComBLDriver.Models.BLImportOrderLogsRequest ApiRequest {get; set;}
  }
  
  
	//ERRSOURCE: structure BLImportOrdersPaymentsRequestCommand { ... } in object ECOM.ECOMADAPTERBL
  [CustomData("DataStructure=Y")]
  [UUID("c2f87976141b4b7ea32490288af2af97")]
  public class BLImportOrdersPaymentsRequestCommand : NeosCommand
  {
      public string Message {get; set;}
      public int EcomChannel {get; set;}   
      public EComBLDriver.Models.BLGetOrderPaymentsHistoryRequest ApiRequest {get; set;}
  }
  
  
  
	//ERRSOURCE: structure BLPutInvoiceCarrierResponseCommand { ... } in object ECOM.ECOMADAPTERBL
  [CustomData("DataStructure=Y")]
  [UUID("c47a01b0c7e7468e88b98417080786e5")]
  public class BLPutInvoiceCarrierResponseCommand : NeosCommand
  {
      public string Message {get; set;}
      public int EcomChannel {get; set;}    
      public string EcomOrderId {get; set;}  
      public string InvoiceFilePath {get; set;} 
      public string AttachmentSymbol {get; set;} 
      public EComBLDriver.Models.BLAddOrderInvoiceCarrierResponse ApiResponse {get; set;}
  }
  
  
	//ERRSOURCE: structure BLPutProductsStocksResponseCommand { ... } in object ECOM.ECOMADAPTERBL
  [CustomData("DataStructure=Y")]
  [UUID("cf5b9024e66644fcbc4b66da61aba7d1")]
  public class BLPutProductsStocksResponseCommand : NeosCommand
  {
      public string Message {get;set;}
      public int EcomChannel {get; set;}    
      public EComBLDriver.Models.BLUpdateProductStockRequest ApiRequest {get; set;} 
      public EComBLDriver.Models.BLUpdateProductStockResponse ApiResponse {get; set;}  
  }
  [CustomData("DataStructure=Y")]
  [UUID("c338f6f3f2d34a449236b9754b59200f")]
  public class BLGetOrderPackagesRequestCommand : NeosCommand
  {    
      public string Message {get; set;}
      public int EcomOrderId {get; set;}  
      public int EcomChannel {get; set;}      
  }
  [CustomData("DataStructure=Y")]
  [UUID("bb8b47f9b6254ce88b3afd661f0ff22e")]
  public class BLGetOrderPackagesResponseCommand : NeosCommand
  {
      public string Message {get;set;}
      public int OrderId {get;set;}
      public int EcomChannel {get; set;} 
      public EComBLDriver.Models.BLGetOrderPackagesResponse BLApiResponse {get;set;}
  }
  [CustomData("DataStructure=Y")]
  [UUID("9342a5417bba4019b77f7316f62cd293")]
  public class BLGetLabelResponseCommand : NeosCommand
  {
      public string Message {get; set;}
      public int EcomOrderId {get; set;}  
      public int EcomChannel {get; set;}   
      public int EcomPackageId {get; set;}  
      public string EcomPackageNumber {get; set;} 
      public EComBLDriver.Models.BLGetLabelInfo BLApiResponse {get;set;}
  }
  [CustomData("DataStructure=Y")]
  [UUID("51e44af060904e50be496d2e7557e0bf")]
  public class BLGetLabelRequestCommand : NeosCommand
  {    
      public string Message {get; set;}
      public int EcomOrderId {get; set;}  
      public int EcomChannel {get; set;}   
      public int EcomPackageId {get; set;}  
      public string EcomCourierCode {get; set;} 
      public string EcomPackageNumber {get; set;}   
  }
  [CustomData("DataStructure=Y")]
  [UUID("f275906bf57a47e5844eb95de92ebb8b")]
  public class MergeOrderStrategy : ILogStrategy
  {
    public HashSet<int> Execute(string orderId, string objectId, int channelRef)
  
    {
      var orderList = new HashSet<int>();
      var ecomOrder = new ECOM.ECOMORDERS();
      var ecomState = new ECOM.ECOMCHANNELSTATES();
      ecomOrder.FilterAndSort($"{nameof(ECOMORDERS)}.{ecomOrder.ECOMCHANNELREF.Symbol} = 0{channelRef} " +
          $"and {nameof(ECOMORDERS)}.{ecomOrder.ECOMORDERID.Symbol} = '{objectId}'");
      ecomState.FilterAndSort($"{nameof(ECOMCHANNELSTATES)}.{ecomState.ECOMCHANNELREF.Symbol} = 0{channelRef} " +
          $"and {nameof(ECOMCHANNELSTATES)}.{ecomState.SYMBOL.Symbol} = '0' ");
      if (ecomOrder.FirstRecord())
      {
        // zmiana statusu zamówienia, które zostało dołączone do innego
        if (ecomState.FirstRecord())
        {
          ecomOrder.EditRecord();
          ecomOrder.ECOMCHANNELSTATE = ecomState.REF;
          if (!ecomOrder.PostRecord())
          {
            throw new Exception($"Błąd zmiany statusu zamówienia o REF {ecomOrder.REF}");
          }
        }
        else
        {
          throw new Exception($"Nie znaleziono statusu 'Połączone'");
        }
      }
      else
      {
        throw new Exception($"Nie znaleziono zamówienia o ID {objectId}");
      }
      ecomOrder.Close();
      ecomState.Close();
      return orderList;
    }
  }
  [CustomData("DataStructure=Y")]
  [UUID("c84d11a1efa54e21b607bbad448e599f")]
  public class NoLogStrategy : ILogStrategy
  {
      public HashSet<int> Execute(string orderId, string objectId, int channelRef)
      {
          // domyślne zachowanie, gdy nie ma strategii dla danego typu loga
          var orderList = new HashSet<int>();
          return orderList;
      }
  }
  [CustomData("DataStructure=Y")]
  [UUID("e50622a7afa7471da9a770e3fff34b18")]
  public class PackageAddedStrategy : ILogStrategy
  {
    public HashSet<int> Execute(string orderId, string objectId, int channelRef)
    {
      var orderList = new HashSet<int>();
  
      var ecomOrder = new ECOM.ECOMORDERS();
  
      ecomOrder.FilterAndSort($"{nameof(ECOMORDERS)}.{ecomOrder.ECOMCHANNELREF.Symbol} = 0{channelRef} " +
      $"and {nameof(ECOMORDERS)}.{ecomOrder.ECOMORDERID.Symbol} = '{orderId}'");
      if (ecomOrder.FirstRecord())
      {
        LOGIC.ECOMCHANNELS.GetOrderPackages(channelRef, int.Parse("0" + orderId));
      }
      ecomOrder.Close();
      return orderList;
    }
  }
  [CustomData("DataStructure=Y")]
  [UUID("92c34534ad78498db97ad861ade419ec")]
  public class SplitOrderStrategy : ILogStrategy
  {
      public HashSet<int> Execute(string orderId, string objectId, int channelRef)
      {
          var orderList = new HashSet<int>();
          orderList.Add(int.Parse(objectId));
          return orderList;
      }
  
  }
  [CustomData("DataStructure=Y")]
  [UUID("bfa0115accc54b888b8708bcfdd18cdb")]
  public interface ILogStrategy
  {
      HashSet<int> Execute(string orderId, string objectId, int channelRef);
  }
}
