
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
  
  public partial class ECOMADAPTERBL<TModel> where TModel : MODEL.ECOMADAPTERBL, new()
  {

    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("105f75cfa60a4c64b175224cbc8a154a")]
    virtual public HandlerResult BLPutProductsStocksResponseCommandHandler(ConsumeContext<ECOM.BLPutProductsStocksResponseCommand> context)
    {
      string errMsg = "";
      if(context.Message.ApiResponse.Status != "SUCCESS")
      {
        errMsg += $"Błąd API: {context.Message.ApiResponse.Status}, kod błędu: {context.Message.ApiResponse.ErrorCode}, treść błędu:  {context.Message.ApiResponse.ErrorMessage}";      
      }
      foreach(var product in context.Message.ApiRequest.Products)
      {
        foreach(var stock in product.Value.Stock.Keys)
        {
        //aktualizacje każdego w osobnej transakcji, bo jak dostaniemy exceptiona, to i tak chcemy oznaczyć te stany
        //które udało się wysłać
          RunInAutonomousTransaction(
          ()=>
          {
            var invStock = new ECOM.ECOMINVENTORYSTOCKS();
            try
            {
                var ecomInv = new ECOM.LOGIC.ECOMINVENTORIES().Get().Where(i => i.ECOMINVENTORYID == product.Key && i.ECOMCHANNELREF == context.Message.EcomChannel).FirstOrDefault();
    
                invStock.FilterAndSort($"es.{invStock.ECOMSTOCKID.Symbol} = '{stock}' " +
                  $"AND ei.{invStock.ECOMINVENTORYID.Symbol} = '{product.Key}' " +
                  $"AND es.{invStock.ECOMCHANNELREF.Symbol} = 0{context.Message.EcomChannel}");
    
                if(invStock.FirstRecord() && ecomInv?.ACTIVE == 1)
                {    
                    invStock.EditRecord();
                    invStock.SYNCSTATUS = (int)EcomSyncStatus.Exported;
                    invStock.LASTSYNCERROR = "";
                    invStock.LASTSENDTMSTMP = DateTime.Now;
                    if(!invStock.PostRecord())
                    {
                      errMsg += $"Błąd zapisu zmiany statusu towaru o ID: {product.Key} ";
                    }	   
                }   
                else
                {
                  invStock.EditRecord();
                  invStock.SYNCSTATUS = (int)EcomSyncStatus.ExportError;
                  invStock.LASTSYNCERROR = $"Próba eksportu stanu magazynowego dla nieaktywnego towaru: {product.Key}";							
                  if(!invStock.PostRecord())
                  {
                    errMsg += $"Błąd zapisu zmiany statusu towaru o ID: {product.Value} ";
                  }
                  errMsg += $"Próba eksportu stanu magazynowego dla nieaktywnego towaru: {product.Key}\n";
                }
                
            }    
            catch(Exception ex)
            {
                invStock.EditRecord();
                invStock.SYNCSTATUS = (int)EcomSyncStatus.ExportError;
                invStock.LASTSYNCERROR = ex.Message;							
                if(!invStock.PostRecord())
                {
                  errMsg += $"Błąd zapisu zmiany statusu towaru o ID: {product.Value} ";
                }	  
                errMsg += $"{ex.Message}\n";           
            }
            invStock.Close();
          },
          false
        );     
      }
      }
      if(!string.IsNullOrEmpty(errMsg))
      {
        throw new Exception(errMsg);
        context.Message.Message = errMsg;
      }  
      
    
      return HandlerResult.Handled;
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("388075baf9944935a793b96222f0f240")]
    [PhysicalQueueName("ExportInventoryQueue")]
    virtual public HandlerResult BLPutProductPriceResponseCommandHandler(ConsumeContext<ECOM.BLPutProductPriceResponseCommand> context)
    {
      string errMsg = ""; 
      if(context.Message.ApiResponse.Status != "SUCCESS")
      {
        errMsg += $"Błąd API: {context.Message.ApiResponse.Status}, kod błędu: {context.Message.ApiResponse.ErrorCode}, treść błędu:  {context.Message.ApiResponse.ErrorMessage}";      
      }
      foreach(var productPriceResult in context.Message.ProductsToSendList)
      {
        //aktualizacje każdego w osobnej transakcji, bo jak dostaniemy exceptiona, to i tak chcemy oznaczyć te ceny
        //które udało się wysłać 
        RunInAutonomousTransaction(
          ()=>
          {
            bool isError = false;
            var invPrice = new ECOM.ECOMINVENTORYPRICES();
            try
            {
              var invPriceData = CORE.QuerySQL(
                @"select p.ref, ei.ACTIVE
                  from ecominventories ei
                    join ecominventoryprices p on p.ecominventoryref = ei.ref " +
                  $"where ei.ecomchannelref = 0{context.Message.EcomChannel.ToString()} " + 
                    $"and ei.ecominventoryid = '{productPriceResult}'").FirstOrDefault();
    
              if(invPriceData == null)
              {
                isError = true;
                errMsg += $"Nie znaleziono ceny dla towaru o ID: {productPriceResult}";
              }
              invPrice.FilterAndSort($"{nameof(ECOM.ECOMINVENTORYPRICES)}.{invPrice.REF.Symbol} = 0{invPriceData["REF"]} ");
              if(invPrice.FirstRecord() && invPriceData["ACTIVE"].AsString == "1")	
              {            
                if(context.Message.ApiResponse.Warnings.ToString() != "{}")
                {        
                  isError = true;      
                  errMsg += "Błąd API: " + context.Message.ApiResponse.Warnings.ToString();                               
                }
                else
                {            
                  invPrice.EditRecord();
                  invPrice.SYNCSTATUS = (int)EcomSyncStatus.Exported;        
                  invPrice.LASTSENDTMSTMP = DateTime.Now;
                  if(!invPrice.PostRecord())
                  {
                    isError = true;
                    errMsg += $"Błąd zapisu zmiany statusu ceny towaru o REF: {invPriceData["REF"]} ";
                  }	
                } 
              }
              else
              {
                isError = true;
                errMsg += $"Nie znaleziono ceny towaru o ref: {invPriceData["REF"]}\n";
              }      
            }
            catch(Exception ex)
            {
              isError = true;	 
              errMsg += $"{ex.Message}\n";           
            }
            if (isError && invPrice != null)
            {
              invPrice.EditRecord();
              invPrice.SYNCSTATUS = (int)EcomSyncStatus.ExportError;     
              invPrice.LASTSENDTMSTMP = DateTime.Now;
              if(!invPrice.PostRecord())
              {
                errMsg += $"Błąd zapisu zmiany statusu ceny towaru o REF: {invPrice.ECOMINVENTORYREF} ";
              }	 
            }
            invPrice.Close();
          },
          false
        );     
      }
      
      if(!string.IsNullOrEmpty(errMsg))
      {
        throw new Exception(errMsg);
        context.Message.Message = errMsg;
      }    
      
      return HandlerResult.Handled;
    }
    
    
    


    /// <param name="orderSource"></param>
    [UUID("3b58a8638dbb413cbd57e3640d66813d")]
    virtual public EcomOrderInfo BLOrderToEcomOrderInfo(EComBLDriver.Models.BLGetOrdersResponse.Order orderSource)
    {  
      var orderInfo = new EcomOrderInfo();
      DateTime tempDate = new DateTime(); 
      decimal? totalWeight = 0.0m; 
    
    
      try
      {    
        //dane konta klienta
        orderInfo.ClientAccount = new EcomClientAccountInfo()
        {
          Id = orderSource?.UserLogin,
          Login = orderSource?.UserLogin,
          Phone = orderSource?.Phone, 
          Email = orderSource?.Email
        };
      } 
      catch(Exception ex)
      {
        throw new Exception("Błąd dodawnia danych konta do klas uniwersalnych: " + ex.Message);
      }  
    
      try
      {
        //adres rozliczeniowy
        orderInfo.BillingAddress = new EcomAddressInfo()
        {           
          Street = orderSource?.InvoiceAddress, 
          ZipCode = orderSource?.InvoicePostcode,
          City = orderSource?.InvoiceCity, 
          CountryId = orderSource?.InvoiceCountryCode, 
          CountryName = orderSource?.InvoiceCountry, 
          FirmName = orderSource?.InvoiceCompany,
          // baselinker zwraca imie i nazwisko w jednej zmienej 
          FullName = !String.IsNullOrEmpty(orderSource?.InvoiceFullname) ? orderSource?.InvoiceFullname : orderSource?.DeliveryFullname, 
          Phone = orderSource?.Phone,
          Nip = orderSource?.InvoiceNip,
          Email = orderSource?.Email
        };
      }  
      catch(Exception ex)
      {
        throw new Exception("Błąd dodawnia danych rozliczeniowych klienta do klas uniwarsalnych: " + ex.Message);
      }  
    
      try
      {
        //adres odbiorcy
        orderInfo.DeliveryAddress = new EcomAddressInfo()
        {     
          Street = orderSource?.DeliveryAddress, 
          ZipCode = orderSource?.DeliveryPostcode,
          City = orderSource?.DeliveryCity,  
          CountryId = orderSource?.deliverycountryCode, 
          CountryName = orderSource?.deliverycountry,
          FirmName = orderSource?.DeliveryCompany, 
          FirstName = orderSource?.DeliveryFullname, 
          //LastName = orderSource?.DeliveryFullname,
          FullName = orderSource?.DeliveryFullname,
          Phone = orderSource?.Phone,
          PickupPointId = orderSource?.DeliveryPointId.ToString()
        };
      }  
      catch(Exception ex)
      {
        throw new Exception("Błąd dodawnia adresu odbioru do klas uniwarsalnych: " + ex.Message);
      }  
    
      try
      {
        //status zamówienia
        orderInfo.OrderStatus = new EcomOrderStatusInfo()
        {
          OrderStatusId = orderSource.OrderStatusId.ToString()          
        };
      }  
      catch(Exception ex)
      {
        throw new Exception("Błąd dodawnia statusu do klas uniwarsalnych: " + ex.Message);
      }
      
      try
      {
        //sposób odbioru
        orderInfo.Dispatch = new EcomDispatchInfo()
        {
          DispatchId = orderSource.DeliveryMethod.ToString(),   //id sposobu dostawy z kanału sprzedaży   
          DeliveryCost = Convert.ToDecimal(orderSource?.DeliveryPrice)
        };
      }  
      catch(Exception ex)
      {
        throw new Exception("Błąd dodawnia sposobu odbioru do klas uniwarsalnych: " + ex.Message);
      }  
     
      try
      {
        // obliczanie wartości zamówinia 
        float orderValue = orderSource.DeliveryPrice; 
        foreach(var p in orderSource.Products)
        {
          orderValue += p.PriceBrutto * p.Quantity; 
        }
        //płatność
        orderInfo.Payment = new EcomPaymentInfo()
        {
          PaymentType = orderSource?.PaymentMethod,      
          Currency = orderSource?.Currency,
          OrderValue = Convert.ToDecimal(orderValue),
          CalculateType = "B"
        };
      } 
      catch(Exception ex)
      {
        throw new Exception("Błąd dodawnia płatności do klas uniwarsalnych: " + ex.Message);
      } 
       
      try
      {
        //dane nagłówka zamówienia
        orderInfo.OrderId = orderSource?.OrderId.ToString();      
        orderInfo.OrderSymbol = orderSource?.OrderId.ToString(); 
        orderInfo.OrderConfirmation = orderSource?.Confirmed.ToString();           
        orderInfo.OrderAddDate = 
          ((DateTime.TryParse((DateTimeOffset.FromUnixTimeSeconds((long)orderSource?.DateAdd)).ToString(), out tempDate)) ? tempDate : null as DateTime?);
        orderInfo.NoteToOrder = orderSource?.UserComments;
      }  
      catch(Exception ex)
      {
        throw new Exception("Błąd dodawania nagłówka zamówienia do klas uniwarsalnych: " + ex.Message);
      }  
    
      try
      {
         //pozycje zamówienia
        if((orderSource?.Products?.Count ?? 0) > 0)
        {
          orderInfo.OrderLines = new List<EcomOrderLineInfo>();  
          foreach(var line in orderSource?.Products)
          {
            
            EcomOrderLineInfo orderLine = new EcomOrderLineInfo()
            {
              ProductId = line?.ProductId?.ToString(),
              ProductName =line?.Name,      
              ProductQuantity = Convert.ToDecimal(line?.Quantity),
              ProductWeight =  Convert.ToDecimal(line?.Weight),
              ProductVat = line?.TaxRate.ToString(),      
              ProductOrderPrice = Convert.ToDecimal(line?.PriceBrutto), 
              ProductOrderPriceNet = Decimal.Round(Convert.ToDecimal(line?.PriceBrutto) / (1 + (Convert.ToDecimal(line?.TaxRate) / 100.0m)) , 2)
            };
            totalWeight += orderLine.ProductWeight;
            
            
            orderInfo.OrderLines.Add(orderLine);
            orderInfo.StockId = "bl_" + line?.WarehouseId.ToString();
          }    
        }
        orderInfo.Dispatch.DeliveryWeight = totalWeight;
      }  
      catch(Exception ex)
      {
        throw new Exception("Błąd dodawania pozycji zamówienia do klas uniwarsalnych: " + ex.Message);
      }
    
      try
      {
        //przedpłaty dla zamówienia
        if((orderSource?.PaymentDone ?? 0) > 0 && orderSource?.Confirmed == true)
        {
          orderInfo.Prepaids = new List<EcomPrepaidInfo>();   
          EcomPrepaidInfo prepaid = new EcomPrepaidInfo()
          {
            PrepaidId = orderSource.OrderId.ToString(),
            Currency = orderSource.Currency, 
            Value = Convert.ToDecimal(orderSource?.PaymentDone),      
            PaymentType = orderSource.PaymentMethodCod.ToString()
          };   
    
          orderInfo.Prepaids.Add(prepaid);        
        }
      }  
      catch(Exception ex)
      {
        throw new Exception("Błąd dodawania danych o przedpłatach do klas uniwarsalnych: " + ex.Message);
      }  
    
      //punkt odbioru    
      try
      {
        if((orderSource?.DeliveryPointName?.Length ?? 0) > 0)
        {
          orderInfo.PickupPointAddress = new EcomAddressInfo()
          {
            PickupPointId = orderSource?.DeliveryPointId,
            City = orderSource?.DeliveryPointCity,
            Street = orderSource?.DeliveryPointAddress,
            ZipCode = orderSource?.DeliveryPointPostcode,
            Phone = orderSource?.Phone,
            Email = orderSource?.Email
          }; 
        }  
    
      } 
      catch(Exception ex)
      {
        throw new Exception("Błąd dodawania danych o punkcie odbioru do klas uniwarsalnych: " + ex.Message);
      } 
      
      return orderInfo;
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("3de5a9598bb844dbb04fc48ce1878570")]
    [PhysicalQueueName("ExportInventoryQueue")]
    virtual public HandlerResult BLPutProductRequestCommandHandler(ConsumeContext<ECOM.BLPutProductRequestCommand> context)
    {
      EComBLDriver.Models.BLUpdateProductResponse response = null;
      try
      {     
        var driver = new EComBLDriver.BLDriver();   
        EComBLDriver.BLDriver.ClientData clientData = new EComBLDriver.BLDriver.ClientData
        {
          TokenAPI = Parameters["apitoken"].AsString,	
          Method = Parameters["addinventoryproduct"].AsString,	
          ApiDomain = Parameters["apidomain"].AsString 
        };
        
        string errorMessage;
        response = driver.UpdateProduct(clientData, context.Message.ApiRequest, out errorMessage);
    
        if(!string.IsNullOrEmpty(errorMessage))
        {
          throw new Exception($"Błąd wysłania produktu: {errorMessage}");
        }
        else if(response == null)
        {     
          throw new Exception("Brak danych w odpowiedzi bramki");     
        }
        else if(response?.Warnings?.ToString() != "{}" && !String.IsNullOrEmpty(response?.Warnings?.ToString()) || response.Status != "SUCCESS")
        { 
          throw new Exception($"Błąd wysłania produktu. Status: {response.Status}. Opis błędu: {response.Warnings} {response.ErrorMessage}");
        }
        
        var command = new ECOM.BLPutProductResponseCommand() 
        {
          ApiResponse = response,     
          EcomChannel = context.Message.EcomChannel,
          //zapisujemy wersję osobno, bo w obsłudze respona z api nie będziemy mieli po czym wyszukać, który towary wysłaliśmy          
          EcomInventoryVersion = context.Message.EcomInventoryVersion
        };
        
        if(!string.IsNullOrEmpty(context.Message.GetLogicalQueueIdentifier()))
        {
          command.SetLogicalQueueIdentifier(context.Message.GetLogicalQueueIdentifier());
        }
        command.SetIdentifier(context.Message.GetIdentifier());
        //EDA.ExecuteCommand($"{context.Headers.Get<string>("NeosConnector")}:ExportInventoryQueue", command);   
        EDA.ExecuteCommand(context.Headers.Get<string>("NeosConnector"), command);	       
      }
      catch(Exception ex)
      {
        context.Message.Message = ex.Message;   
        RunInAutonomousTransaction(
          ()=>
        {
          var inv = new ECOM.ECOMINVENTORIES();
          inv.FilterAndSort($"{nameof(ECOM.ECOMINVENTORIES)}.{inv.ECOMCHANNELREF.Symbol} = 0{context.Message.EcomChannel} " +
            $"AND {nameof(ECOM.ECOMINVENTORIES)}.{inv.WERSJAREF.Symbol} = 0{context.Message.EcomInventoryVersion}");
          if(inv.FirstRecord())		
          {
            //id produktu chcemy ustawiać/ zmieniać tylko jeżeli została zwrócona, żeby przypakiem jej nie nullować
            var tempProductId = response.ProductId;
            inv.EditRecord();
            if(tempProductId != null)
            {
              inv.ECOMINVENTORYID = tempProductId;
            }
            inv.SYNCSTATUS = (int)EcomSyncStatus.ExportError;
            inv.LASTSYNCERROR = context.Message.Message;
            inv.EDAID = context.Message.GetIdentifier();        
            if(!inv.PostRecord())
            {
              throw new Exception($"Błąd zapisu zmiany statusu towaru o wersji: {context.Message.EcomInventoryVersion}");
            }	          
          }
          else
          {
            throw new Exception($"Nie znaleziono wersji towaru: {context.Message.EcomInventoryVersion} w kanale: {context.Message.EcomChannel.ToString()}");
          }
          inv.Close();
        },
        true
        );
        
        throw new Exception(ex.Message);  
      }  
      return HandlerResult.Handled; 
    }  
    
    


    /// <param name="connector"></param>
    [UUID("3e2646b0594342948df02ce0b705bce0")]
    public static string OpenOrderInAdminPanelLogic(int connector)
    {
      return CORE.GetField("NVALUE", $"SYS_EDACONNECTORPARAMS where EDACONNECTORREF = 0{connector.ToString()} and NKEY = 'basedomain'");    
    }
    
    
    


    /// <param name="productSource"></param>
    /// <param name="priceGroup"></param>
    /// <param name="channelRef"></param>
    [UUID("401e65555ec0494c9e937ce98e7b1fc2")]
    virtual public EcomInventoryPriceInfo BLProductInfoToEComInventoryInfo(EComBLDriver.Models.BLGetInventoryListResponse.ProductModel productSource, KeyValuePair<string, float> priceGroup, int channelRef)
    {
      var priceInfo = new EcomInventoryPriceInfo();
      var product = new ECOM.TOWARY();
      var version = new ECOM.WERSJE();
      var inventory = new ECOM.ECOMINVENTORIES();
      decimal retailPrice;
      try
      {  
        if(productSource.Id != null)
        {
          retailPrice =  (decimal) priceGroup.Value;
          inventory.FilterAndSort($"{nameof(ECOM.ECOMINVENTORIES)}.{inventory.ECOMINVENTORYID.Symbol} = '{productSource.Id}'");
          if(inventory.FirstRecord())
          {
            version.FilterAndSort($"{nameof(ECOM.WERSJE)}.{inventory.REF.Symbol} = 0{inventory.WERSJAREF}");
            if(version.FirstRecord())
            {
              product.FilterAndSort($"{nameof(ECOM.TOWARY)}.{product.KTM.Symbol} = '{version.KTM}'");
              if(product.FirstRecord())
              {
                var vatData = new ECOMSTDMETHODS().Logic.GetVatForVerscountry(
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
    
                priceInfo.RetailPriceNet = (retailPrice / (1 + vatData.Vatrate.Value/100));
              }
            }
          }
          priceInfo.RetailPrice = retailPrice;   
          priceInfo.InventoryId = productSource.Id.ToString();
          priceInfo.Currency = LOGIC.ECOMCHANNELCONVERT.ConvertFromChannelValue(channelRef, "WALUTY",  priceGroup.Key);
        }
      } 
      catch(Exception ex)
      {
        throw new Exception("Błąd dodawnia danych cen towaru do klas uniwersalnych: " + ex.Message);
      }  
     
      inventory.Close();  
      version.Close();
      product.Close();
      return priceInfo;
       
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("5cd11d5c3b6e46068246ca1e22765bfb")]
    virtual public HandlerResult BLPutInvoiceCarrierResponseCommandHandler(ConsumeContext<ECOM.BLPutInvoiceCarrierResponseCommand> context)
    {
      try
      {         
        if(context.Message.ApiResponse.Status != "SUCCESS")
        {
          throw new Exception($"Błąd API: {context.Message.ApiResponse.Status}, kod błędu: {context.Message.ApiResponse.ErrorCode}, treść błędu:  {context.Message.ApiResponse.ErrorMessage}");      
        }
        else
        { 
          // po udanej próbie założenia nośnika faktury zamówenia można przystąpić do 
          // wysłania pliku faktury na witrynę baselinkera  
          var insertAttachmentRequest = new EComBLDriver.Models.BLAddOrderInvoiceRequest();
          insertAttachmentRequest.InvoiceId = context.Message.ApiResponse.InvoiceId;
          insertAttachmentRequest.File = context.Message.InvoiceFilePath; 
          insertAttachmentRequest.ExternalName =  context.Message.AttachmentSymbol; 
                      
          var insertAttachmentCommand = new ECOM.BLPutAttachmentRequestCommand()
          {
            EcomChannel = context.Message.EcomChannel,
            EcomOrderId = context.Message.EcomOrderId,
            ApiInsertAttachmentRequest = insertAttachmentRequest
          };  
          EDA.ExecuteCommand(context.Headers.Get<string>("NeosConnector"), insertAttachmentCommand);
          
        }
      }
    	catch(Exception ex)
    	{    
        context.Message.Message += ex.Message;
        throw new Exception($"Błąd wysłania faktury dla zamówienia o ID {context.Message.EcomOrderId} " + ex.Message);
    	} 
      return HandlerResult.Handled;
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("5f793f6f9b9446a7895efe86e3329fb2")]
    virtual public HandlerResult BLPutOrderStatusResponseCommandHandler(ConsumeContext<ECOM.BLPutOrderStatusResponseCommand> context)
    {
      string errorMsg = ""; 
      try
      {  
        if(context.Message.ApiResponse.Status != "SUCCESS")
        {      
          throw new Exception($"Błąd API: {context.Message.ApiResponse.Status}, kod błędu: {context.Message.ApiResponse.ErrorCode}, treść błędu:  {context.Message.ApiResponse.ErrorMessage}"); 
        }
        else
        {        
          var ecomOrderData = new ECOM.ECOMORDERS(); 
          ecomOrderData.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{ecomOrderData.ECOMCHANNELREF.Symbol} = 0{context.Message.EcomChannel} " +
            $"AND {nameof(ECOM.ECOMORDERS)}.{ecomOrderData.ECOMORDERID.Symbol} = '{context.Message.EcomOrderId}'");
          if(ecomOrderData.FirstRecord())
          {   
            ecomOrderData.EditRecord();    
            ecomOrderData.SYNCSENDCHANELLSTATUS = (int)EcomSyncStatus.Exported;
            ecomOrderData.LASTSENDTMSTMP = DateTime.Now; 
            ecomOrderData.LASTSTATUSSYNCERROR = "";           
            if(!ecomOrderData.PostRecord())
            {
              throw new Exception($"Błąd zapisu zmiany statusu zamówienia o ID: {context.Message.EcomOrderId}");
            }
          }
          else
          {
            throw new Exception($"Nie znaleziono zamówienia o ID: {context.Message.EcomOrderId} w kanale: {context.Message.EcomChannel.ToString()}");                     
          }
          ecomOrderData.Close();
        }
      }
      catch(Exception ex)
      {
        RunInAutonomousTransaction(()=>{
          var ecomOrderData = new ECOM.ECOMORDERS();
          ecomOrderData.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{ecomOrderData.ECOMCHANNELREF.Symbol} = 0{context.Message.EcomChannel} " +
            $"AND {nameof(ECOM.ECOMORDERS)}.{ecomOrderData.ECOMORDERSYMBOL.Symbol} = '{context.Message.EcomOrderId}'");
          if(ecomOrderData.FirstRecord()) 
          {     
            ecomOrderData.EditRecord();  
            ecomOrderData.SYNCSENDCHANELLSTATUS = (int)EcomSyncStatus.ExportError;            
            if(!ecomOrderData.PostRecord())
            {
              throw new Exception($"Błąd zapisu zmiany statusu zamówienia o ID: {context.Message.EcomOrderId}");
            }     
          }
          else
          {
            throw new Exception($"Nie znaleziono zamówienia o ID: {context.Message.EcomOrderId} w kanale: {context.Message.EcomChannel.ToString()}");                     
          } 
          ecomOrderData.Close();     
        });
    
        errorMsg += 
          $"Błąd aktualizacji statusu zamówienia o ID: {context.Message.EcomOrderId} " +
            $" w kanale sprzedaży: {context.Message.EcomChannel.ToString()}: {ex.Message}\n";                  
        
        
      }
    
      if(!string.IsNullOrEmpty(errorMsg))
      {
        context.Message.Message = errorMsg;
        throw new Exception(errorMsg);
      } 
      
      return  HandlerResult.Handled;
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("68acf655e303486a9fe798ff7ee20840")]
    virtual public HandlerResult BLGetOrdersRequestCommandHandler(ConsumeContext<ECOM.BLGetOrdersRequestCommand> context)
    {
    	string errMsg = "";	
      	try
    	{			
    		var driver = new EComBLDriver.BLDriver();
    		
    		BLDriver.ClientData clientData = new BLDriver.ClientData()
    		{				
    			TokenAPI = Parameters["apitoken"].AsString,		
    			Method = Parameters["getorders"].AsString,	
    			ApiDomain = Parameters["apidomain"].AsString
    		};			
    				
    		string errorMessage;
    		//pobranie synchroniczne zamówień z BL								
    		var response = driver.GetOrders(clientData, context.Message.BLApiRequest, out errorMessage);		
    
    		if(!string.IsNullOrEmpty(errorMessage))
    		{
    			throw new Exception(errorMessage);
    		}
    		else if(response == null)
    		{
    			throw new Exception("Brak danych w odpowiedzi bramki");			
    		}
    		else if(response.Status != "SUCCESS")
    		{
    			errMsg = $"Błąd pobierania zamówień z kanału sprzedaży. Kod błędu: {response.ErrorCode}, Opis: {response.ErrorMessage}";
    			throw new Exception(errMsg);
    		}
    		else
    		{		
    			var command = new ECOM.BLGetOrdersResponseCommand()
    			{
    				BLApiResponse = response,
    				EcomChannel = context.Message.EcomChannel,
    				ImportMode = context.Message.ImportMode
    			};
    
    			command.SetIdentifier(context.Message.GetIdentifier());
    					
    			EDA.SendCommand(context.Headers.Get<string>("NeosConnector"), command);  
    		
    			/*
    			 jeśli w kanale występuje więcej niż 100 zamówień 
    			 odpytujemy o kolejne zamówienia bramkę, ponieważ zwraca maksymalnie 
    			 100 zamówień w jednej odpowiedzi
    			*/
    			if (response.Orders.Count == 100)
    			{								
    				var getOrdersCommand = new ECOM.BLGetOrdersRequestCommand()
    				{
    					BLApiRequest = context.Message.BLApiRequest,
    					EcomChannel = context.Message.EcomChannel,
    					Message = context.Message.Message
    				};
    				// pobieramy kolejne zamówienia od największej daty zamówienia zwróconej w poprzedniej odpowiedzi
    				getOrdersCommand.BLApiRequest.DateConfirmedFrom = response.Orders.Max(o => o.DateConfirmed);
    				
    				EDA.SendCommand("ECOM.ECOMADAPTERBL.BLGetOrdersRequestCommandHandler", getOrdersCommand);			
    			}	
    		}
    	}
    	catch(Exception ex)
    	{
    		context.Message.Message = ex.Message;
    		throw new Exception(ex.Message);
    	}
      	return HandlerResult.PipeSuccess;
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("7d6d3870528146158687290ec87e4e48")]
    virtual public HandlerResult BLPutAttachmentRequestCommandHandler(ConsumeContext<ECOM.BLPutAttachmentRequestCommand> context)
    {
    	try
    	{
    		var driver = new EComBLDriver.BLDriver();   
    		EComBLDriver.BLDriver.ClientData clientData = new EComBLDriver.BLDriver.ClientData
    		{
    			TokenAPI = Parameters["apitoken"].AsString,	
    			Method = Parameters["addorderinvoicefile"].AsString,	
    			ApiDomain = Parameters["apidomain"].AsString 
    		};	
    	
    		EComBLDriver.Models.BLAddOrderInvoiceResponse response = null;
    
    		string errorMessage;
    		response = driver.InsertOrderInvoice(clientData, context.Message.ApiInsertAttachmentRequest, out errorMessage);
    
    		if(!string.IsNullOrEmpty(errorMessage))
    		{
    			throw new Exception($"Błąd wysłania załącznika: {errorMessage}");
    		}
    		else if(response == null)
    		{			
    			throw new Exception("Brak danych w odpowiedzi bramki");			
    		}
    		else if(response.Status != "SUCCESS")
    		{
    		throw new Exception($"Błąd API: {response.Status}, kod błędu: {response.ErrorCode}, treść błędu:  {response.ErrorMessage}");      
    		}
    		
      	}
      	catch(Exception ex)
    	{		
    		context.Message.Message = ex.Message;
    		throw new Exception(ex.Message);	
    	}		
      return HandlerResult.Handled; 
    }
    
    


    /// <param name="productSource"></param>
    /// <param name="stock"></param>
    /// <param name="channelRef"></param>
    [UUID("7e3e37fa25d24a8ea32115f9ba763676")]
    virtual public EcomInventoryStockInfo BLProductInfoToEcomStockInfo(EComBLDriver.Models.BLGetInventoryListResponse.ProductModel productSource, KeyValuePair<string, float> stock, int channelRef)
    {
      var stockInfo = new EcomInventoryStockInfo();
      try
      {  
        stockInfo.InventoryId = productSource.Id.ToString();
        stockInfo.Quantity = (decimal) stock.Value;
        stockInfo.ChannelStockId = stock.Key;      
      } 
      catch(Exception ex)
      {
        throw new Exception("Błąd dodawnia danych stanów magazynowych do klas uniwersalnych: " + ex.Message);
      }  
     
      return stockInfo;
       
    }
    
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("80e41d8f36b04456ace4ef01b27ab6e3")]
    virtual public HandlerResult BLGetOrdersResponseCommandHandler(ConsumeContext<ECOM.BLGetOrdersResponseCommand> context)
    {	
    	string errMsg = "";
    	
        foreach(var orderResult in context.Message.BLApiResponse.Orders)
    	{
    		EcomOrderInfo orderInfo = new EcomOrderInfo();					
    		try
    		{
    			orderInfo = BLOrderToEcomOrderInfo(orderResult);
    			// Pobieranie historii płatności, jeśli jest włączona w konfiguracji konektora
    			var request = new EComBLDriver.Models.BLGetOrderPaymentsHistoryRequest();
    			request.ShowFullHistory = true;				
    			request.OrderId = orderResult.OrderId;
    
    		  var getPaymentCommand = new ECOM.BLImportOrdersPaymentsRequestCommand()    
    			{ 
    				EcomChannel = context.Message.EcomChannel,
    				ApiRequest = request
    			};
    			var result = EDA.ExecuteCommand(context.Headers.Get<string>("NeosConnector"), getPaymentCommand, context); 				
    			var handlerResult = result.HandlerResult;
    			// lista historii płatności zwrócona z BLImportOrdersPaymentsRequestCommandHandler
    			orderInfo.Prepaids = (List<EcomPrepaidInfo>)result["Result"];  	 
    								 
    			var command = new ECOM.ImportOrderCommand()    
    			{
    				Message = "Zamówienie " + orderResult.OrderId,
    				EcomChannel = context.Message.EcomChannel,				
    				OrderInfo = orderInfo,
    				ImportMode = context.Message.ImportMode
    			};
    			var edaIdentifierData = LOGIC.ECOMORDERS.GetEDAID(context.Message.EcomChannel, command.OrderInfo);
    			
    			if(!edaIdentifierData.Result)
    			{
    				throw new Exception(edaIdentifierData.ErrorMsg);					
    			}
    			command.SetIdentifier(edaIdentifierData.EDAIdentifier);		
    			// datę zamówień dostajemy w formacie unix time stamp
          DateTime lastOrderDate = DateTimeOffset.FromUnixTimeSeconds(orderResult.DateAdd).ToLocalTime().DateTime;
    			command.OrderInfo.OrderAddDate = lastOrderDate;		
    
    			EDA.SendCommand(context.Headers.Get<string>("NeosConnector"), command); 			
    		}
    		catch(Exception ex)
    		{
    			errMsg = $"Błąd wysyłki komendy przetworzenia zamówienia {orderResult.OrderId}: {ex.Message}. " + "\n";
    			throw new Exception(errMsg);
    		}								
    	}
    
        return HandlerResult.PipeSuccess;
    }
    
    


    /// <summary>
    /// Metoda służy do dodania nośnika faktur. 
    /// Każde zamówienie musi posiadać swój nośnik, aby można było dodać fakturę. 
    /// Nowy nośnik zakładamy podając ID zamówienia oraz ID rodzaju faktury (ID rodzaju faktury można znaleźć w panelu administracyjnym baselinkera lub w słowniku konwersji w konfiguracji kanału sprzedaży).
    /// </summary>
    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("869ddde5182946589f1292113a26cab8")]
    virtual public HandlerResult BLPutInvoiceCarrierRequestCommandHandler(ConsumeContext<ECOM.BLPutInvoiceCarrierRequestCommand> context)
    {	
      try
    	{			
    		var driver = new EComBLDriver.BLDriver();   
        EComBLDriver.BLDriver.ClientData clientData = new EComBLDriver.BLDriver.ClientData
        {
          TokenAPI = Parameters["apitoken"].AsString,	
          Method = Parameters["addinvoice"].AsString,	
          ApiDomain = Parameters["apidomain"].AsString 
        };	
    	
    		EComBLDriver.Models.BLAddOrderInvoiceCarrierResponse response = null;
    		
    		string errorMessage;
    		response = driver.InsertDocumentsCarrier(clientData, context.Message.ApiInvoiceCarrierRequest, out errorMessage);
    
    		if(!string.IsNullOrEmpty(errorMessage))
    		{
    			throw new Exception($"Błąd dodania nośnika faktur zamówienia: {errorMessage}");
    		}
    		else if(response == null)
    		{			
    			throw new Exception("Brak danych w odpowiedzi bramki");			
    		}
    
        
    		var command = new ECOM.BLPutInvoiceCarrierResponseCommand()    
    		{
    			EcomChannel = context.Message.EcomChannel,
    			EcomOrderId = context.Message.EcomOrderId.ToString(),
    			InvoiceFilePath = context.Message.InvoiceFilePath,
    			AttachmentSymbol = context.Message.AttachmentSymbol,
    			ApiResponse = response			
    		};
    		command.SetIdentifier(context.Message.GetIdentifier());
    		var result = EDA.ExecuteCommand(context.Headers.Get<string>("NeosConnector"), command); 
        
    	}
    	catch(Exception ex)
    	{		
    		context.Message.Message = ex.Message;
    		throw new Exception(ex.Message);	
    	}	
    
      return HandlerResult.Handled;
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("8b18a22b92a049f79bc910ac0f611061")]
    virtual public HandlerResult BLImportOrdersPaymentsRequestCommandHandler(ConsumeContext<ECOM.BLImportOrdersPaymentsRequestCommand> context)
    {
      try
    	{
    		string errorMessage;
    		var driver = new EComBLDriver.BLDriver(); 
    		var paymentList = new List<EcomPrepaidInfo>();
    		EComBLDriver.BLDriver.ClientData clientData = new EComBLDriver.BLDriver.ClientData
    		{
    			TokenAPI = Parameters["apitoken"].AsString,	
    			Method = Parameters["getorderpaymentshistory"].AsString,	
    			ApiDomain = Parameters["apidomain"].AsString 
    		};	
    	
    		EComBLDriver.Models.BLGetOrderPaymentsHistoryResponse response = null;
    		
    		response = driver.GetOrderPaymentsHistory(clientData, context.Message.ApiRequest, out errorMessage);
    
    		if(!string.IsNullOrEmpty(errorMessage))
    		{
    			throw new Exception($"Błąd pobrania historii płatności zamówienia o id {context.Message.ApiRequest.OrderId} treść błędu: {errorMessage}");
    		}
    		else if(response == null)
    		{			
    			throw new Exception("Brak danych w odpowiedzi bramki");			
    		}
    		if(response.Status != "SUCCESS")
        {
          throw new Exception($"Błąd API: {response.Status}, kod błędu: {response.ErrorCode}, treść błędu: {response.ErrorMessage}");      
        }
    		
    		foreach(var payment in response.Payments)
    		{
    			var item = new EcomPrepaidInfo();
    			var paymentDate = DateTimeOffset.FromUnixTimeSeconds(payment.Date).ToLocalTime().DateTime;
    			item.Value = (decimal) payment.PaidAfter - (decimal) payment.PaidBefore;
    			item.Currency = payment.Currency;
    			item.PrepaidId = payment.ExternalPaymentId;
    			item.PaymentDate = paymentDate;
    			
    			paymentList.Add(item);
    		}
    		// zwracamy w context historię płatności 
    		context.SetResult("Result", paymentList);
    	}
    	catch(Exception ex)
    	{
    		context.Message.Message += ex.Message;
    		RunInAutonomousTransaction(
      	()=>
    		{
    		  var ecomOrder = new ECOM.ECOMORDERS();
    			ecomOrder.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{ecomOrder.ECOMCHANNELREF.Symbol} = 0{context.Message.EcomChannel} " +
    				$"AND {nameof(ECOM.ECOMORDERS)}.{ecomOrder.ECOMORDERID.Symbol} = '{context.Message.ApiRequest.OrderId}'");
          if(ecomOrder.FirstRecord())
    			{
    				ecomOrder.EditRecord();
    				ecomOrder.SYNCSTATUS = (int)EcomSyncStatus.ImportError;
            ecomOrder.LASTSYNCERROR = context.Message.Message;
    				
    				if(!ecomOrder.PostRecord())
    				{
    					throw new Exception($"Nieudana próba zapisu błędu pobrania historii płatności zamówienia o ID: {context.Message.ApiRequest.OrderId}");
    				}           
    		  }
    			else
    			{
    				throw new Exception($"Nie znaleziono zamówienia o ID: {context.Message.ApiRequest.OrderId} w kanale: {context.Message.EcomChannel.ToString()}");
    			}
    			ecomOrder.Close();
    		},
    		true
    	  );
    		
        throw new Exception(ex.Message); 
    	}	
    		
      return HandlerResult.Handled; 
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("8f641c1453374d70ab47dacd99a457c8")]
    [PhysicalQueueName("ExportInventoryQueue")]
    virtual public HandlerResult BLPutProductResponseCommandHandler(ConsumeContext<ECOM.BLPutProductResponseCommand> context)
    {
     try
      {
        if(context.Message.ApiResponse.Status != "SUCCESS")
        {
          throw new Exception($"Błąd API: {context.Message.ApiResponse.Status}, kod błędu: {context.Message.ApiResponse.ErrorCode}, treść błędu:  {context.Message.ApiResponse.ErrorMessage}");      
        }
        else
        {      
          ECOM.ECOMINVENTORIES inventory = new ECOM.ECOMINVENTORIES();
          inventory.FilterAndSort($"{nameof(ECOM.ECOMINVENTORIES)}.{inventory.ECOMCHANNELREF.Symbol} = 0{context.Message.EcomChannel} " +
            $"AND {nameof(ECOM.ECOMINVENTORIES)}.{inventory.WERSJAREF.Symbol} = 0{context.Message.EcomInventoryVersion}");
          if(inventory.FirstRecord())
          {		 
            inventory.EditRecord();
            inventory.ECOMINVENTORYID = context.Message.ApiResponse.ProductId;
            inventory.SYNCSTATUS = (int)EcomSyncStatus.Exported;
            inventory.LASTSYNCERROR = "";
            inventory.LASTSENDTMSTMP = DateTime.Now;
            inventory.EDAID = context.Message.GetIdentifier();
            if(!inventory.PostRecord())
            {
              throw new Exception($"Błąd zapisu zmiany statusu towaru o wersji: {context.Message.EcomInventoryVersion}");
            }	
          }
          else
          {
            throw new Exception($"Nie znaleziono wersji towaru o REF: {context.Message.EcomInventoryVersion} w kanale: {context.Message.EcomChannel}");
          }
          inventory.Close();    
        }
      }
    	catch(Exception ex)
    	{    
        context.Message.Message += ex.Message;
        throw new Exception($"Błąd aktualizacji danych towaru w kanale sprzedaży {context.Message.EcomChannel}. {ex.Message}");
    	} 
      return HandlerResult.Handled;
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("a278d20ba104476ebd0f015953bb67f0")]
    virtual public HandlerResult BLImportOrderLogsRequestCommandHandler(ConsumeContext<ECOM.BLImportOrderLogsRequestCommand> context)
    {
      try
    	{
    		var driver = new EComBLDriver.BLDriver();   
    		EComBLDriver.BLDriver.ClientData clientData = new EComBLDriver.BLDriver.ClientData
    		{
    			TokenAPI = Parameters["apitoken"].AsString,	
    			Method = Parameters["getjournallist"].AsString,	
    			ApiDomain = Parameters["apidomain"].AsString 
    		};	
    	
    		EComBLDriver.Models.BLImportOrderLogsResponse response = null;
    
    		string errorMessage;
    		response = driver.ImportOrderLogs(clientData, context.Message.ApiRequest, out errorMessage);
    
    		if(!string.IsNullOrEmpty(errorMessage))
    		{
    			throw new Exception($"Błąd pobrania logów zamówienia o id {context.Message.EcomOrderId} treść błędu: {errorMessage}");
    		}
    		else if(response == null)
    		{			
    			throw new Exception("Brak danych w odpowiedzi bramki");			
    		}
    
    		var command = new ECOM.BLImportOrderLogsResponseCommand()    
    		{
    			EcomChannel = context.Message.EcomChannel,
    			EcomOrderId = context.Message.EcomOrderId,
    			ApiResponse = response			
    		};
    		command.SetIdentifier(context.Message.GetIdentifier());
    		var result = EDA.ExecuteCommand(context.Headers.Get<string>("NeosConnector"), command);
        
      }
    	catch(Exception ex)
    	{
    		context.Message.Message = ex.Message;
    		RunInAutonomousTransaction(
      	()=>
    		{
    		  var ecomOrder = new ECOM.ECOMORDERS();
    			ecomOrder.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{ecomOrder.ECOMCHANNELREF.Symbol} = 0{context.Message.EcomChannel} " +
    				$"AND {nameof(ECOM.ECOMORDERS)}.{ecomOrder.ECOMORDERID.Symbol} = '{context.Message.EcomOrderId}'");
          if(ecomOrder.FirstRecord())
    			{
    				ecomOrder.EditRecord();
    				ecomOrder.LASTSYNCERROR = ex.Message;
    				
    				if(!ecomOrder.PostRecord())
    				{
    					throw new Exception($"Nieudana próba zapisu błędu pobrania logów zamówienia o ID: {context.Message.EcomOrderId}");
    				}           
    		  }
    			else
    			{
    				throw new Exception($"Nie znaleziono zamówienia o ID: {context.Message.EcomOrderId} w kanale: {context.Message.EcomChannel.ToString()}");
    			}
    			ecomOrder.Close();
    		},
    		true
    	  );
    		throw new Exception(ex.Message);
    	}		
      return HandlerResult.Handled; 
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("b158d42081594bf28c58ac109d9e704a")]
    [PhysicalQueueName("ExportInventoryQueue")]
    virtual public HandlerResult BLPutProductPriceRequestCommandHandler(ConsumeContext<ECOM.BLPutProductPriceRequestCommand> context)
    {
    	EComBLDriver.Models.BLUpdateProductPriceResponse response = null;
      try
      {			
    		var driver = new EComBLDriver.BLDriver();   
        EComBLDriver.BLDriver.ClientData clientData = new EComBLDriver.BLDriver.ClientData
        {
          TokenAPI = Parameters["apitoken"].AsString,	
          Method = Parameters["updateinventoryproductsprices"].AsString,	
          ApiDomain = Parameters["apidomain"].AsString 
        };			
    			
    		string errorMessage;
    
    		response = driver.UpdateProductPrice(clientData, context.Message.ApiRequest, out errorMessage);
    		if(!string.IsNullOrEmpty(errorMessage))
    		{
    			throw new Exception($"Błąd wysłania ceny produktu: {errorMessage}");
    		}
    		else if(response == null)
    		{			
    			throw new Exception("Brak danych w odpowiedzi bramki");			
    		}
    		else if(response?.Warnings?.ToString() != "{}" && !String.IsNullOrEmpty(response?.Warnings?.ToString()) || response.Status != "SUCCESS")
        { 
          throw new Exception($"Błąd wysłania ceny produktu. Błąd API: {response.Status}, kod błędu: {response.ErrorCode}, treść błędu:  {response.ErrorMessage} {response.Warnings}");
        }
      }
      catch(Exception ex)
      {
    	//jeżeli dostaliśmy błąd API np. mamy zły adres API, to znaczy, że nie wysłano żadnej ceny i na każdej podbijamy
    	//błąd eksportu
    		 
    	RunInAutonomousTransaction(
    		()=>			
    		{
    			var ecomInventory = new ECOM.ECOMINVENTORIES();
    			var invPrice = new ECOM.ECOMINVENTORYPRICES();
    			foreach(var ecomInventoryId in context.Message.ProductsToSendList)		
    			{	
    				ecomInventory.FilterAndSort($"{nameof(ECOM.ECOMINVENTORIES)}.{ecomInventory.ECOMINVENTORYID.Symbol} = '{ecomInventoryId}' " +
    					$"AND {nameof(ECOM.ECOMINVENTORIES)}.{ecomInventory.ECOMCHANNELREF.Symbol} = 0{context.Message.EcomChannel}");
    				if(ecomInventory.FirstRecord())				
    				{
    					invPrice.FilterAndSort($"{nameof(ECOM.ECOMINVENTORYPRICES)}.{invPrice.ECOMINVENTORYREF.Symbol} = 0{ecomInventory.REF}");
    					if(invPrice.FirstRecord())
    					{		
    						invPrice.EditRecord();				
    						invPrice.SYNCSTATUS = (int)EcomSyncStatus.ExportError;							
    						if(!invPrice.PostRecord())
    						{
    							throw new Exception($"Błąd zapisu zmiany statusu ceny dla towaru o ID: {ecomInventoryId}");
    						}						
    					} 
    					else
    					{
    						throw new Exception($"Nie znaleziono ceny towaru o ID: {ecomInventoryId} w kanale: {context.Message.EcomChannel.ToString()}");
    					}    						
    				}
    				else
    				{
    					throw new Exception($"Nie znaleziono towaru o ID: {ecomInventoryId} w kanale: {context.Message.EcomChannel.ToString()}");
    				}
    				ecomInventory.Close();
    				invPrice.Close();
    			}
    		},
    		true
    	);
    	context.Message.Message = ex.Message;
    	throw new Exception(ex.Message);
      }
    
    	try
    	{
    		var command = new ECOM.BLPutProductPriceResponseCommand() 
    		{
    			ApiResponse = response,			
    			EcomChannel = context.Message.EcomChannel,
    			ProductsToSendList = context.Message.ProductsToSendList				
    		};
    		command.SetIdentifier(context.Message.GetIdentifier());
    		if(!string.IsNullOrEmpty(context.Message.GetLogicalQueueIdentifier()))
    		{
    			command.SetLogicalQueueIdentifier(context.Message.GetLogicalQueueIdentifier());
    		}
    		EDA.ExecuteCommand(context.Headers.Get<string>("NeosConnector"), command);	           
    	}
    	catch(Exception ex)
    	{
    		context.Message.Message = ex.Message;
    	  throw new Exception(ex.Message);	
    	}
      return HandlerResult.Handled; 
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("b21ff18f7a3f445a8870b0b95c2cd8ea")]
    virtual public HandlerResult BLPutOrderPacakgeRequestCommandHandler(ConsumeContext<ECOM.BLPutOrderPacakgeRequestCommand> context)
    {
    	try
    	{
    		var driver = new EComBLDriver.BLDriver();   
    		EComBLDriver.BLDriver.ClientData clientData = new EComBLDriver.BLDriver.ClientData
    		{
    			TokenAPI = Parameters["apitoken"].AsString,	
    			Method = Parameters["createpackage"].AsString,	
    			ApiDomain = Parameters["apidomain"].AsString 
    		};	
    	
    		EComBLDriver.Models.BLAddPackagesResponse response = null;
    		
    		string errorMessage;
    		response = driver.AddOrderPackages(clientData, context.Message.ApiPackageRequest, out errorMessage);
    
    		if(!string.IsNullOrEmpty(errorMessage))
    		{
    			throw new Exception($"Błąd dodania paczek do zamówienia o ID {context.Message.ApiPackageRequest.OrderId}. Treść błędu: {errorMessage}");
    		}
    		else if(response == null)
    		{			
    			throw new Exception("Brak danych w odpowiedzi bramki");			
    		}
    		else if(response.Status != "SUCCESS")
    		{
    			throw new Exception($"Błąd API: {response.Status}, kod błędu: {response.ErrorCode}, treść błędu:  {response.ErrorMessage}");      
    		}
    	}
      	catch(Exception ex)
      	{		
    		context.Message.Message = ex.Message;
    		throw new Exception(ex.Message);	
      	}		
      return HandlerResult.Handled; 
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("bdaee8ac30704456ba8b2fbf1078c170")]
    virtual public HandlerResult BLPutOrderStatusRequestCommandHandler(ConsumeContext<ECOM.BLPutOrderStatusRequestCommand> context)
    {	
      try
    	{			
    		var driver = new EComBLDriver.BLDriver();   
        EComBLDriver.BLDriver.ClientData clientData = new EComBLDriver.BLDriver.ClientData
        {
          TokenAPI = Parameters["apitoken"].AsString,	
          Method = Parameters["setorderstatus"].AsString,	
          ApiDomain = Parameters["apidomain"].AsString 
        };	
    	
    		EComBLDriver.Models.BLExportOrderStatusResponse response = null;
    		
    		string errorMessage;
    		response = driver.ExportOrderStatus(clientData, context.Message.ApiRequest, out errorMessage);
    
    		if(!string.IsNullOrEmpty(errorMessage))
    		{
    			throw new Exception($"Błąd aktualizacji zamówienia: {errorMessage}");
    		}
    		else if(response == null)
    		{			
    			throw new Exception("Brak danych w odpowiedzi bramki");			
    		}
    
        
    		var command = new ECOM.BLPutOrderStatusResponseCommand()    
    		{
    			EcomChannel = context.Message.EcomChannel,
    			EcomOrderId = context.Message.EcomOrderId,
    			ApiResponse = response			
    		};
    		command.SetIdentifier(context.Message.GetIdentifier());
    		var result = EDA.ExecuteCommand(context.Headers.Get<string>("NeosConnector"), command); 
        
    	}
      catch(Exception ex)
    	{
    		context.Message.Message = ex.Message;
    		RunInAutonomousTransaction(
      	()=>
    		{
    		  var ecomOrder = new ECOM.ECOMORDERS();
    			ecomOrder.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{ecomOrder.ECOMCHANNELREF.Symbol} = 0{context.Message.EcomChannel} " +
    				$"AND {nameof(ECOM.ECOMORDERS)}.{ecomOrder.ECOMORDERID.Symbol} = '{context.Message.EcomOrderId}'");
          if(ecomOrder.FirstRecord())
    			{
    				ecomOrder.EditRecord();
    				ecomOrder.LASTSTATUSSYNCERROR = ex.Message;
    				ecomOrder.SYNCSTATUS = (int)EcomSyncStatus.ExportError;
    				if(!ecomOrder.PostRecord())
    				{
    					throw new Exception($"Błąd zapisu zmiany statusu zamówienia o ID: {context.Message.EcomOrderId}");
    				}           
    		  }
    			else
    			{
    				throw new Exception($"Nie znaleziono zamówienia o ID: {context.Message.EcomOrderId} w kanale: {context.Message.EcomChannel.ToString()}");
    			}
    			ecomOrder.Close();
    		},
    		true
    	  );
    		throw new Exception(ex.Message);
    	}
    
      return HandlerResult.Handled;
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("e3ecacf76c7e401da0ca18c9a00368a9")]
    virtual public HandlerResult BLImportOrderLogsResponseCommandHandler(ConsumeContext<ECOM.BLImportOrderLogsResponseCommand> context)
    {
      var orderList = new HashSet<int>();
      string errorMsg = "";            
      if(context.Message.ApiResponse.Status != "SUCCESS")
      {
        throw new Exception($"Błąd API: {context.Message.ApiResponse.Status}, kod błędu: {context.Message.ApiResponse.ErrorCode}, treść błędu:  {context.Message.ApiResponse.ErrorMessage}");      
      }
      else
      {          
        foreach (var item in context.Message.ApiResponse.Logs)
        {
          try
          { 
            var connector = context.GetConnector();
            EDA.CONNECTORS.SetState(connector, "_lastorderlogid", item.LogId.ToString());
            var orderLogList = new HashSet<int>();
            // metoda do obsługi logów
            orderLogList = ProcessOrderLogByType(item.OrderId.ToString(), item.LogType.ToString(), item.ObjectId.ToString(), context.Message.EcomChannel);
            // jeśli jest kilka logów do jednego zamówienia to wyłapujemy to i pobieramy 
            // tylko raz zamówienie, które uległo zmianie
            orderList.UnionWith(orderLogList);
    
          }
          catch(Exception ex)
          {
            RunInAutonomousTransaction(()=>{
              var ecomOrderData = new ECOM.ECOMORDERS();
              ecomOrderData.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{ecomOrderData.ECOMCHANNELREF.Symbol} = 0{context.Message.EcomChannel} " +
                $"AND {nameof(ECOM.ECOMORDERS)}.{ecomOrderData.ECOMORDERSYMBOL.Symbol} = '{item.OrderId}'");
              if(ecomOrderData.FirstRecord()) 
              {     
                ecomOrderData.EditRecord();  
                ecomOrderData.SYNCSTATUS = (int)EcomSyncStatus.ExportError;            
                if(!ecomOrderData.PostRecord())
                {
                  throw new Exception($"Błąd zapisu zmiany statusu zamówienia o ID: {item.OrderId}");
                }     
              }
              else
              {
                throw new Exception($"Nie znaleziono zamówienia o symbolu: {item.OrderId} w kanale: {context.Message.EcomChannel.ToString()}");                     
              }   
              ecomOrderData.Close();    
            });
    
            errorMsg += 
              $"Błąd aktualizacji statusu zamówienia o symbolu: {item.OrderId} " +
                $" w kanale sprzedaży: {context.Message.EcomChannel.ToString()}: {ex.Message}\n";                  
            
            continue;
          }
        }
        // pobieramy zamówienia, które uległy zmianie
        LOGIC.ECOMCHANNELS.GetOrders(context.Message.EcomChannel, importMode.Selected, null, null, orderList.ToList());     
      }
    
      if(!string.IsNullOrEmpty(errorMsg))
      {
        context.Message.Message = errorMsg;
        throw new Exception(errorMsg);
      } 
      
      
      return HandlerResult.Handled;
    }
    
    


    /// <param name="orderId"></param>
    /// <param name="logType"></param>
    /// <param name="objectId"></param>
    /// <param name="channelRef"></param>
    [UUID("e88650fccb1b4feb94a22699bf9ff821")]
    virtual public HashSet<int> BLLogInfo(int orderId, int logType, int objectId, int channelRef)
    {
        var orderList = new HashSet<int>();
        try
        {    
            orderList.Add(orderId);
            // zamówienie zostało podzielone 
            if(logType == 6)
            {
                // pobieramy oryginalne zamówienie oraz nowe, które zostało utowrzone w procesie dzielenia zamówienia
                orderList.Add(objectId);
            }
            // zamówienie zostało połączone
            if(logType == 5)
            {
                var ecomOrder = new ECOM.ECOMORDERS();
                var ecomState = new ECOM.ECOMCHANNELSTATES();
                ecomOrder.FilterAndSort($"{nameof(ECOMORDERS)}.{ecomOrder.ECOMCHANNELREF.Symbol} = 0{channelRef} " +
                    $"and {nameof(ECOMORDERS)}.{ecomOrder.ECOMORDERID.Symbol} = '{objectId}'"); 
                ecomState.FilterAndSort($"{nameof(ECOMCHANNELSTATES)}.{ecomState.ECOMCHANNELREF.Symbol} = 0{channelRef} " +
                    $"and {nameof(ECOMCHANNELSTATES)}.{ecomState.SYMBOL.Symbol} = '0' ");
                if(ecomOrder.FirstRecord())
                {
                    // zmiana statusu zamówienia, które zostało dołączone do innego
                   if(ecomState.FirstRecord())
                   {
                       ecomOrder.EditRecord();
                       ecomOrder.ECOMCHANNELSTATE = ecomState.REF;
                       if(!ecomOrder.PostRecord())
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
            }
        }
        catch(Exception e)
        {
            throw new Exception("Błąd obsługi logów: " + e.Message);
        }
        return orderList;
    }
    
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("f79f8724af714d7a9810e25f9db81c34")]
    [PhysicalQueueName("ExportInventoryStockQueue")]
    virtual public HandlerResult BLPutProductsStocksRequestCommandHandler(ConsumeContext<ECOM.BLPutProductsStocksRequestCommand> context)
    {
    	EComBLDriver.Models.BLUpdateProductStockResponse response = null;
    	try
      	{			
        	var driver = new EComBLDriver.BLDriver();   
    		var clientData = new EComBLDriver.BLDriver.ClientData
    		{
    			TokenAPI = Parameters["apitoken"].AsString,	
    			Method = Parameters["updateinventoryproductsstock"].AsString,	
    			ApiDomain = Parameters["apidomain"].AsString 
    		};			
    			
    		string errorMessage;
    		response = driver.UpdateProductsStock(clientData, context.Message.ApiRequest, out errorMessage);
    
    		if(!string.IsNullOrEmpty(errorMessage))
    		{
    			throw new Exception($"Błąd wysłania stanów magazywnowych produktu: {errorMessage}");
    		}
    		else if(response == null)
    		{     
    			throw new Exception("Brak danych w odpowiedzi bramki");     
    		}
    		else if(response?.Warnings?.ToString() != "{}" && !String.IsNullOrEmpty(response?.Warnings?.ToString()) || response.Status != "SUCCESS")
    		{ 
    			throw new Exception($"Błąd wysłania stanów magazywnowych produktu. Opis błędu: {response.ErrorMessage} {response?.Warnings?.ToString()}");
    		}
      	}
      	catch(Exception ex)
      	{
    		//jeżeli dostaliśmy błąd API np. mamy zły adres API, to znaczy, że nie wysłano żadnych stanów mag i podbijamy
    		//błąd eksportu				   
    				
    				var channelStock = new ECOM.ECOMCHANNELSTOCK();
    				
    				var inv = new ECOM.ECOMINVENTORIES();
    				
    				foreach(var product in context.Message.ApiRequest.Products)
    				{
    					foreach(var stock in product.Value.Stock.Keys)
    					{
               
                RunInAutonomousTransaction(()=>			
    			      {
                  var invStock = new ECOM.ECOMINVENTORYSTOCKS();
    					  	invStock.FilterAndSort($"es.{invStock.ECOMSTOCKID.Symbol} = '{stock}' " +
    					  		$"AND ei.{invStock.ECOMINVENTORYID.Symbol} = '{product.Key}' " +
    					  		$"AND es.{invStock.ECOMCHANNELREF.Symbol} = 0{context.Message.EcomChannel}");
    
    					  	if(invStock.FirstRecord())		
    					  	{
    						  	invStock.EditRecord();
    						  	invStock.SYNCSTATUS = (int)EcomSyncStatus.ExportError;		
    						  	invStock.LASTSYNCERROR = ex.Message;					
    						  	if(!invStock.PostRecord())
    						  	{
    						  		throw new Exception($"Błąd zapisu zmiany statusu stanu mag. towaru o ID: {product.Key}");
    						  	}	  
    					  	}
    					  	else
    					  	{
    					  		throw new Exception($"Nie znaleziono stanu mag. towaru o ID: {product.Key} w kanale: {context.Message.EcomChannel.ToString()}");
    					  	}
    					  	invStock.Close();
    					  }, true);		    
    				  } 	
            }
    		context.Message.Message = ex.Message;
    	  	throw new Exception(ex.Message);
      	}
    
    	try
    	{
    		var command = new ECOM.BLPutProductsStocksResponseCommand() 
    		{
    			ApiResponse = response,	
    			ApiRequest = context.Message.ApiRequest,		
    			EcomChannel = context.Message.EcomChannel					
    		};
    		command.SetIdentifier(context.Message.GetIdentifier());
    		if(!string.IsNullOrEmpty(context.Message.GetLogicalQueueIdentifier()))
    		{
    			command.SetLogicalQueueIdentifier(context.Message.GetLogicalQueueIdentifier());
    		}
    		EDA.ExecuteCommand(context.Headers.Get<string>("NeosConnector"), command);	           
    	}
    	catch(Exception ex)
    	{
    		context.Message.Message = ex.Message;
    	  	throw new Exception(ex.Message);	
    	}
    
      return HandlerResult.Handled; 
    }
    
    


    /// <summary>
    /// Metoda do pobierania stanów magazynowych towarów. Wykorzystywana do tworzenia weryfikacji stanów magazynowych znajdujących się w Teneum oraz na witrynie.
    /// </summary>
    /// <param name="context"></param>
    [UUID("2e3bdebe568741d194d7321727d6939f")]
    override public List<EcomInventoryStockInfo> DoGetStocksList(ConsumeContext<ECOM.GetStocksListCommand> context)
    {
    	string errMsg = "";
    	var stockInfoList = new List<EcomInventoryStockInfo>();	
    	EComBLDriver.Models.BLGetInventoryListResponse response = null;
    	try
    	{	
    		Contexts ecomChannelParams = LOGIC.ECOMSTDMETHODS.LoadEcomChannelParams(context.Message.EcomChannel);
    		var request = new EComBLDriver.Models.BLGetInventoryListRequest();
    		request.InventoryId = ecomChannelParams["_inventorycatalogid"];
    							
    		var driver = new EComBLDriver.BLDriver();		
    		BLDriver.ClientData clientData = new BLDriver.ClientData()
    		{	
    			
    			TokenAPI = Parameters["apitoken"].AsString,		
    			Method = Parameters["getinventoryproductslist"].AsString,	
    			ApiDomain = Parameters["apidomain"].AsString
    		};					
    			
    			string errorMessage;							
    			response = driver.GetInventoryList(clientData, request, out errorMessage);		
    
    			if(!string.IsNullOrEmpty(errorMessage))
    			{
    				throw new Exception($"Błąd pobrania stanów magazynowych produktu: {errorMessage}");
    			}
    			else if(response == null)
    			{     
    				throw new Exception("Brak danych w odpowiedzi bramki");     
    			}
    			else if(response.Status != "SUCCESS" || response?.Warnings?.ToString() != "{}" && !String.IsNullOrEmpty(response?.Warnings?.ToString()))
    			{ 
    				throw new Exception($"Błąd pobrania stanów magazynowych produktu. Status: {response.Status}. Opis błędu: {response.ErrorMessage} {response.Warnings}");
    			}
    		
    			
    		//skoro udało się pobrać listę produktów, to rozbijamy na osobne komendy per produkt
    								
    		foreach(var productResult in response.Products.Values)
    		{
    			if(productResult?.Stocks?.Count > 0)
    			{
    				foreach (var stock in productResult.Stocks)
    				{
    					var stockInfo = new EcomInventoryStockInfo();
    					try
    					{
    						stockInfo = BLProductInfoToEcomStockInfo(productResult, stock, context.Message.EcomChannel);
    						stockInfoList.Add(stockInfo);				
    					}
    					catch(Exception ex)
    					{
    						errMsg = $"Błąd pobrania stanów magazynowych towaru {productResult.Id}: {ex.Message}.\n";
    						throw new Exception(errMsg);
    					}			
    				}
    			}			
    		}									
    	}
    	catch(Exception ex)
    	{
    		throw new Exception(ex.Message);
    	}
    	
      	return stockInfoList;
    }
    
    


    /// <summary>
    /// Metoda do pobierania cen towarów. Wykorzystywana do tworzenia weryfikacji cen znajdujących się w Teneum oraz na witrynie.
    /// </summary>
    /// <param name="context"></param>
    [UUID("6b882c3957e14988952696de7627b71c")]
    override public List<EcomInventoryPriceInfo> DoGetPricesList(ConsumeContext<ECOM.GetPricesListCommand> context)
    {
    	string errMsg = "";
    	var priceInfoList = new List<EcomInventoryPriceInfo>();	
    	EComBLDriver.Models.BLGetInventoryListResponse response = null;
    	try
    	{	
    		Contexts ecomChannelParams = LOGIC.ECOMSTDMETHODS.LoadEcomChannelParams(context.Message.EcomChannel);
    		var request = new EComBLDriver.Models.BLGetInventoryListRequest();
    		request.InventoryId = ecomChannelParams["_inventorycatalogid"];				
    			
    		var driver = new EComBLDriver.BLDriver();		
    		BLDriver.ClientData clientData = new BLDriver.ClientData()
    		{	
    			
    			TokenAPI = Parameters["apitoken"].AsString,		
    			Method = Parameters["getinventoryproductslist"].AsString,	
    			ApiDomain = Parameters["apidomain"].AsString
    		};					
    			
    		string errorMessage;							
    		response = driver.GetInventoryList(clientData, request, out errorMessage);		
    
    		if(!string.IsNullOrEmpty(errorMessage))
    		{
    			throw new Exception($"Błąd pobrania cen produktu: {errorMessage}");
    		}
    		else if(response == null)
    		{     
    			throw new Exception("Brak danych w odpowiedzi bramki");     
    		}
    		else if(response.Status != "SUCCESS")
    		{ 
    			throw new Exception($"Błąd pobrania cen produktu. Status: {response.Status}. Opis błędu: {response.ErrorMessage}");
    		}
    		
    	
    
    	//skoro udało się pobrać listę produktów, to rozbijamy na osobne komendy per produkt
    								
    		foreach(var productResult in response.Products.Values)
    		{
    			if(productResult?.Prices?.Count > 0)
    			{
    				foreach (var priceGr in productResult.Prices)
    				{
    					EcomInventoryPriceInfo priceInfo = new EcomInventoryPriceInfo();
    					try
    					{
    						priceInfo = BLProductInfoToEComInventoryInfo(productResult, priceGr, context.Message.EcomChannel);
    						priceInfoList.Add(priceInfo);				
    					}
    					catch(Exception ex)
    					{
    						errMsg = $"Błąd pobrania cen towaru {productResult.Id}: {ex.Message}.\n";
    						throw new Exception(errMsg);
    					}			
    				}
    			}			
    		}						
    	}
    	catch(Exception ex)
    	{
    		throw new Exception(ex.Message);
    	}
    	
      	return priceInfoList;
    }
    
    


    /// <summary>
    /// Metoda do pobierania historii płatności zamówienia
    /// </summary>
    /// <param name="context"></param>
    [UUID("c68da931d3784480ba09cd96406d8c0c")]
    override public string DoImportOrdersPayments(ConsumeContext<ECOM.ImportOrdersPaymentsRequestCommand> context)
    {
      	string errMsg = ""; 		
    	
    	try
    	{
    		var request = new EComBLDriver.Models.BLGetOrderPaymentsHistoryRequest();
    		request.ShowFullHistory = true;
    		foreach(var order in context.Message.OrderList)
    		{
    			request.OrderId = order;
    			var command = new ECOM.BLImportOrdersPaymentsRequestCommand()    
    			{ 
    				EcomChannel = context.Message.EcomChannel,
    				ApiRequest = request
    			};
    			EDA.SendCommand(context.Headers.Get<string>("NeosConnector"), command, context); 			
    		}
    	}
    	catch(Exception ex)
    	{
    		errMsg = ex.Message;
    	}
    		
        return errMsg;
    	
    
    }
    
    


    /// <summary>
    /// Metoda do przeciążenia w adapterze kanału sprzedaży.
    /// Metoda weryfikuje, czy drajwer fizyczny obsługuje taką konfigurację parametrów, buduje i wywołuje fizczne zapytanie w drajwerze, np. w drajwerze IAI, otrzymuje odpowiedź, zwraca komunikato błędzie, jeśi jest poprawne, to strukturę Response pakuje do osobnej komendy implementowanej już na poziomie drajwera realnego (w obiekcie dzieczącym) np. GetOrderResponse 
    /// </summary>
    /// <param name="context"></param>
    [UUID("5e21560c9ae8407cbb3b316d39c6d9c6")]
    override public string DoImportOrdersReqest(
        ConsumeContext<ECOM.ImportOrdersRequestCommand> context) {
      string errMsg = "";
    
      try {
        
        var methodObject = LOGIC.ECOMCHANNELS.FindMethodObject(context.Message.EcomChannel);
        var request = new EComBLDriver.Models.BLGetOrdersRequest();
        var importMode = context.Message.ImportMode;
        var orderList = new List<int>();
    
        // pobieramy wszystkie zamówienia (potwierdzone i niepotwierdzone w panelu
        // admina BL)
        request.GetUnconfirmedOrders = false;
        switch (importMode) {
          // od ostatniego importu dla potwierdzonych zamówień, czyli ustawiamy
          // tylko datę poczatkową sinceLastImport równiez aktualizuje zamówienia,
          // które zostały już pobrane, ale zostały zmienione w panleu sprawdzamy,
          // które zamówienie uległo zmianie poprzez pobranie logów zamówień z
          // baselinkera
          case importMode.SinceLastImport:
            request.DateConfirmedFrom =
                (int)((DateTimeOffset)context.Message.OrderDateFrom)
                    .ToUnixTimeSeconds() +
                1;
            // request.GetUnconfirmedOrders = false;
            break;
          // lista id zamówień lub pojedyncze id podane przez operatora
          case importMode.Selected:
            orderList = context.Message.OrderList.Select(Int32.Parse).ToList();
            request.GetUnconfirmedOrders = true;
            break;
          case importMode.OrderId:
            orderList = context.Message.OrderList.Select(Int32.Parse).ToList();
            request.GetUnconfirmedOrders = true;
            break;
          case importMode.DateRange:
            request.DateFrom = (int)((DateTimeOffset)context.Message.OrderDateFrom)
                                   .ToUnixTimeSeconds();
            break;
          // pobieranie wszystkich zamówień - ustawiamy datę na rok 1970 - początek
          // czasu wg UNIX
          case importMode.All:
            request.DateConfirmedFrom =
                (int)((DateTimeOffset) new DateTime(1970, 1, 1, 1, 0, 0))
                    .ToUnixTimeSeconds();
            break;
          default:
            throw new Exception("Nieobsłużony tryb pobierania zamówień");
            break;
        }
    
        if (methodObject.Logic.IsImportModeRequiresOrderList(importMode) &&
            (orderList?.Count ?? 0) == 0) {
          throw new Exception(
              $"Dla wybranego sposobu importu ({importMode.ToString()}) wymagana jest lista zamówień.");
        }
    
        var command = new ECOM.BLGetOrdersRequestCommand() {
          BLApiRequest = request, EcomChannel = context.Message.EcomChannel,
          Message = context.Message.Message, ImportMode = context.Message.ImportMode
        };
        if (orderList.Count > 0) {
          foreach (var order in orderList) {
            command.BLApiRequest.OrderId = order;
            EDA.SendCommand(context.Headers.Get<string>("NeosConnector"), command,
                            context);
          }
        } else {
          EDA.SendCommand(context.Headers.Get<string>("NeosConnector"), command,
                          context);
        }
    
        // jeśli pobieramy zamówienia w opcji od ostatniej aktualizacji ( opcja ta
        // pobiera nowo dodane zamówienia oraz te które uległy zmianie)
        if (context.Message.ImportMode == importMode.SinceLastImport) {
          var getLogRequest = new EComBLDriver.Models.BLImportOrderLogsRequest();
          /*
          wybieramy jakie typy logów nas interesuja
          3 - Opłaty za zamówienie
          5 - Łączenie zamówień
          6 - Dzielenie zamówienia
          11 - Editing delivery data
          12 - Adding a product to an order
          13 - Editing the product in the order
          14 - Removing the product from the orde
          16 - Editing order data
          18 - Order status change
          */
          Contexts ecomChannelParams = LOGIC.ECOMSTDMETHODS.LoadEcomChannelParams(context.Message.EcomChannel);
    			if (ecomChannelParams["_getlabelsautomatically"] == "1")
    			{
    			  getLogRequest.LogsTypes = new int[] {3, 5, 6, 9, 11, 12, 13, 14, 16, 18};
    			}
    			else
    			{
    			  getLogRequest.LogsTypes = new int[] {3, 5, 6, 11, 12, 13, 14, 16, 18};
    			}
    
          // szukamy jakie id miał ostatni pobrany log, aby wiedzieć od którego
          // zacząć pobieranie
          var connector = context.GetConnector();
          // pobieramy ze stanu konektora jaki ostatni log był pobrany
          // jeśli nie ma wpisu na temat ostatniego loga to pobieramy wszystkie
          // zaczynając od id = 1
          string item = EDA.CONNECTORS.GetState(connector, "_lastorderlogid");
          getLogRequest.LastLogId = String.IsNullOrEmpty(item) ? 0 : Int32.Parse(item);
          getLogRequest.LastLogId = getLogRequest.LastLogId + 1;
    
          var logCommand = new ECOM.BLImportOrderLogsRequestCommand() {
            EcomChannel = context.Message.EcomChannel, ApiRequest = getLogRequest
          };
          EDA.SendCommand(context.Headers.Get<string>("NeosConnector"), logCommand,
                          context);
        }
      } catch (Exception e) {
        throw new Exception("Błąd pobierania zamówień: " + e.Message);
      }
    
      return errMsg;
    }
    
    


    /// <param name="context"></param>
    [UUID("60f446bf9af74d5397a6c4d36d7dd07b")]
    override public List<EcomOrderInfo> DoGetOrdersList(ConsumeContext<ECOM.GetOrdersListCommand> context)
    {		
    	var orderInfoList = new List<EcomOrderInfo>();
    	try
    	{
    		string errMsg = "";	
    		var request = new EComBLDriver.Models.BLGetOrdersRequest();
    		EComBLDriver.Models.BLGetOrdersResponse response = null;					
    		request.DateFrom = (int)((DateTimeOffset)context.Message.OrderDateFrom).ToUnixTimeSeconds();	
    				
    		var driver = new EComBLDriver.BLDriver();		
    		BLDriver.ClientData clientData = new BLDriver.ClientData()
    		{				
    			TokenAPI = Parameters["apitoken"].AsString,		
    			Method = Parameters["getorders"].AsString,	
    			ApiDomain = Parameters["apidomain"].AsString
    		};			
    					
    		string errorMessage;
    		//pobranie synchroniczne zamówień z BL								
    		response = driver.GetOrders(clientData, request, out errorMessage);		
    
    		if(!string.IsNullOrEmpty(errorMessage))
    		{
    			throw new Exception(errorMessage);
    		}
    		else if(response == null)
    		{
    			throw new Exception("Brak danych w odpowiedzi bramki");			
    		}
    		else if(response.Status != "SUCCESS")
    		{
    			errMsg = $"Błąd pobierania zamówień z kanału sprzedaży. Kod błędu: {response.ErrorCode}, Opis: {response.ErrorMessage}";
    			throw new Exception(errMsg);
    		}
    			
    		//skoro udało się pobrać listę zamówień, to rozbijamy na osobne komendy per zamowienie										
    		foreach(var orderResult in response.Orders)
    		{
    			EcomOrderInfo orderInfo = new EcomOrderInfo();					
    			try
    			{
    				orderInfo = BLOrderToEcomOrderInfo(orderResult);
    				orderInfoList.Add(orderInfo);				
    			}
    			catch(Exception ex)
    			{
    				errMsg = $"Błąd wysyłki komendy przetworzenia zamówienia {orderResult.OrderId}: {ex.Message}. " + "\n";
    				throw new Exception(errMsg);
    			}						
    		}		
    		
    	}
    	catch(Exception ex)
    	{
    		throw new Exception(ex.Message);
    	}	
    	return orderInfoList;
    }
    
    


    /// <param name="context"></param>
    [UUID("8c74b36fd2f64f388de3e0e85039239f")]
    override public string DoExportInventoryPrices(ConsumeContext<ECOM.ExportInventoryPricesCommand> context)
    {
      string errMsg = "";    
      //wysyłamy ceny w jednym zapytaniu do API 
      var invPrice = new ECOM.ECOMINVENTORYPRICES();
      var request = new EComBLDriver.Models.BLUpdateProductPriceRequest();
      Contexts ecomChannelParams = LOGIC.ECOMSTDMETHODS.LoadEcomChannelParams(context.Message.EcomChannel);
      var productsToSendList = new List<string>();
      request.Products = new Dictionary<string, EComBLDriver.Models.BLUpdateProductPriceRequest.ProductModel>();
      request.InventoryId = ecomChannelParams["_inventorycatalogid"];
      
      foreach(var inventoryPriceInfo in context.Message.InventoryPricesInfoList)
      {        
        try
        {     
          var productModel = new EComBLDriver.Models.BLUpdateProductPriceRequest.ProductModel();
          productModel.PriceGr = new Dictionary<string, Newtonsoft.Json.Linq.JToken>();
          invPrice.FilterAndSort($"{nameof(ECOM.ECOMINVENTORYPRICES)}.{invPrice.REF.Symbol} = 0{inventoryPriceInfo.EcomInventoryPriceRef}");
          if(!invPrice.FirstRecord())
          {
            errMsg += $"Nie znaleziono ceny o ref: {inventoryPriceInfo.EcomInventoryPriceRef.ToString()} w kanale: {context.Message.EcomChannel.ToString()}\n";            
            continue;
          }
    
          productModel.PriceGr.Add(inventoryPriceInfo.Currency, Decimal.ToSingle(inventoryPriceInfo?.RetailPrice ?? 0m));   
          request.Products.Add(inventoryPriceInfo.InventoryId, productModel);
          productsToSendList.Add(inventoryPriceInfo.InventoryId);
          RunInAutonomousTransaction(
            ()=>
            {
              var autonomousInvPrice = new ECOM.ECOMINVENTORYPRICES();
              autonomousInvPrice.FilterAndSort($"{nameof(ECOM.ECOMINVENTORYPRICES)}.{autonomousInvPrice.REF.Symbol} = 0{inventoryPriceInfo.EcomInventoryPriceRef}");
              if(autonomousInvPrice.FirstRecord())
              {
                autonomousInvPrice.EditRecord();            
                autonomousInvPrice.SYNCSTATUS = (int)EcomSyncStatus.ExportProceeding;         
                if(!autonomousInvPrice.PostRecord())
                {
                  throw new Exception($"Błąd zapisu zmiany statusu eksportu ceny o REF: {inventoryPriceInfo.EcomInventoryPriceRef}");
                }  
              }                
            },
            true
          );                   
        }
        catch(Exception ex)
        {
          errMsg += $"Błąd przy generowaniu danych do wysłania cen na witrynę dla towaru o ID: {inventoryPriceInfo.InventoryId} " +
            $"w kanale sprzedaży {context.Message.EcomChannel}: {ex.Message}\n";       
          
          RunInAutonomousTransaction(
            ()=>
            {  
              var autonomousInvPrice = new ECOM.ECOMINVENTORYPRICES();
              autonomousInvPrice.FilterAndSort($"{nameof(ECOM.ECOMINVENTORYPRICES)}.{autonomousInvPrice.REF.Symbol} = 0{inventoryPriceInfo.EcomInventoryPriceRef}");
              if(autonomousInvPrice.FirstRecord())
              {
                autonomousInvPrice.EditRecord();       
                autonomousInvPrice.SYNCSTATUS = (int)EcomSyncStatus.ExportError;          
                if(!autonomousInvPrice.PostRecord())
                {
                  throw new Exception($"Błąd zapisu zmiany statusu eksportu ceny o REF: {inventoryPriceInfo.EcomInventoryPriceRef}");
                }   
              }
            },
            true
          );
          continue;
        }      
      }
      
      //jeżeli przynajmniej jedną cene udało się dodać, to wysyłamy eksport, bo nie chcemy, żeby błąd na jednej cenie blokował
      //eksport wszystkich
      if(productsToSendList.Count > 0)
      {
        var command = new ECOM.BLPutProductPriceRequestCommand()    
        {	
          EcomChannel = context.Message.EcomChannel,
          ApiRequest = request,
          ProductsToSendList = productsToSendList    
        };
    
        if(!string.IsNullOrEmpty(context.Message.GetLogicalQueueIdentifier()))
        {
          command.SetLogicalQueueIdentifier(context.Message.GetLogicalQueueIdentifier());
        }		    
          
        //pobieramy gdzie została wysłana poprzednia komenda z dokładnością do kolejki i wysyłamy na tą samą
        //var hanlder = context.Headers.FirstOrDefault(x => x.Key == "HandlerSymbol").Value.ToString();
        EDA.SendCommand(context.Headers.Get<string>("NeosConnector"), command);
      }         
      invPrice.Close();
      return errMsg;
    }
    
    


    /// <summary>
    /// Metoda abstarkcyjna do przeciązenienia w adapterze realnym. Realizauje konwersję z danych uniwersalnych o towarach zawartych w strukturze List&lt;EcomInventoryInfo&gt; do struktur danego drajwera i wysyła dane statusuów do kanału sprzedaży w osobnych  komendach na poziomie adaptera realnego np. PutOrderStatusRequestCommand. Po otrzymaniu odpowiedzi zaznacza, że udało się zaktualizować status, albo że wystąpił błąd
    /// Uruchamiana w handlerze ExportInventoryCommandHandler 
    /// </summary>
    /// <param name="context"></param>
    [UUID("92772b069e3c4a24986dc1ffcaca0820")]
    override public string DoExportInventory(ConsumeContext<ECOM.ExportInventoryCommand> context)
    {
      string errMsg = "";  
      //rozbijamy i wysyłamy każdy towar w innym zapytaniu do API, żeby można było łatwiej śledzić historię eksportu w monitorze 
      var inv = new ECOM.ECOMINVENTORIES();
      Contexts ecomChannelParams = LOGIC.ECOMSTDMETHODS.LoadEcomChannelParams(context.Message.EcomChannel);
      foreach(var inventoryInfo in context.Message.InventoryInfoList)
      {
        try
        {
          inv.FilterAndSort($"{nameof(ECOM.ECOMINVENTORIES)}.{inv.REF.Symbol} = 0{inventoryInfo?.EcomInventoryRef}");
          if(!inv.FirstRecord())
          {   
            throw new Exception($"Nie znaleziono towaru o wersji: {inventoryInfo?.WersjaRef} w kanale: {context.Message.EcomChannel}\n"); 
          }
          
          var request = new EComBLDriver.Models.BLUpdateProductRequest(); 
          request.Sku = inventoryInfo.KTM;
          request.InventoryId = ecomChannelParams["_inventorycatalogid"];
          request.ProductId = inventoryInfo?.InventoryId ?? "";
          if(!string.IsNullOrEmpty(inventoryInfo?.Vat))
          {
            request.TaxRate = int.Parse(inventoryInfo?.Vat);
          }
          request.TextFields = new Dictionary<string, string>();
          foreach(var name in inventoryInfo?.InventoryNames)
          {
            request.TextFields.Add("name", name.Text);
          }
          foreach(var desc in inventoryInfo?.InventoryDescriptions)
          {
            request.TextFields.Add("description", desc.Text);
          }
          int img = 0;
          if((inventoryInfo?.Images?.Count ?? 0 ) > 0)
          {
            request.Images = new List<string>();
            foreach(var image in inventoryInfo.Images)
            {
              request.Images.Add(image.PictureSource);
              img++;
            }
          }
    
          var baseUnit = inventoryInfo?.Units?.Where(u => u.IsBaseUnit == true).FirstOrDefault();
          if(baseUnit != null)
          {
            request.Ean = baseUnit?.EAN;
            request.Weight = (float)(string.IsNullOrEmpty(baseUnit?.Weight.ToString()) ? 0 : baseUnit?.Weight); 
            request.Length = (float)(string.IsNullOrEmpty(baseUnit?.Length.ToString()) ? 0 : baseUnit?.Length); 
            request.Width = (float)(string.IsNullOrEmpty(baseUnit?.Width.ToString()) ? 0 : baseUnit?.Width);   
          }  
          if (string.IsNullOrEmpty(request.Ean))
          {
            request.Ean = inventoryInfo?.EAN;
          }   
        
          var command = new ECOM.BLPutProductRequestCommand()    
          {			
            ApiRequest = request,    
            EcomChannel = context.Message.EcomChannel,
            //zapisujemy wersję osobno, bo w obsłudze respona z api nie będziemy mieli po czym wyszukać, który to towary wysłaliśmy          
            EcomInventoryVersion = (inventoryInfo?.WersjaRef ?? 0) 
          };
    
          if(!string.IsNullOrEmpty(context.Message.GetLogicalQueueIdentifier()))
          {
            command.SetLogicalQueueIdentifier(context.Message.GetLogicalQueueIdentifier());
          }		           
    
          var edaIdentifierData = LOGIC.ECOMINVENTORIES.GetEDAID(context.Message.EcomChannel, inventoryInfo);
          if(!edaIdentifierData.Result)
          {
            throw new Exception(edaIdentifierData.ErrorMsg);					
          }
          else
          {
            command.SetIdentifier(edaIdentifierData.EDAIdentifier);
          }
    
          RunInAutonomousTransaction(
            ()=>
            {
              inv.EditRecord();
              inv.SYNCSTATUS = (int)EcomSyncStatus.ExportProceeding;
              inv.LASTSYNCERROR = "";
              
              if(!inv.PostRecord())
              {
                throw new Exception($"Błąd zapisu zmiany statusu dla zamówienia o REF: {inventoryInfo?.EcomInventoryRef}");
              }  
            },
            true
          );
                 
          EDA.SendCommand($"{context.Headers.Get<string>("NeosConnector")}:ExportInventoryQueue", command);      
        }
        catch(Exception ex)
        {
          errMsg += $"Błąd przy generowaniu danych do wysłania na witrynę dla wersji {inventoryInfo?.WersjaRef} " +
            $"w kanale sprzedaży {context.Message.EcomChannel}: {ex.Message}\n"; 
            
          RunInAutonomousTransaction(
            ()=>
            {
              inv.EditRecord();
              inv.SYNCSTATUS = (int)EcomSyncStatus.ExportError;
              inv.LASTSYNCERROR = ex.Message;      
              
              if(!inv.PostRecord())
              {
                throw new Exception($"Błąd zapisu zmiany statusu dla zamówienia o REF: {inventoryInfo?.EcomInventoryRef}");
              }           
            },
            true
          );
          continue;
        }
      }
    
      if(!string.IsNullOrEmpty(errMsg))
      {
        throw new Exception(errMsg);
      }
      inv.Close();
      return errMsg;
    }
    
    


    /// <summary>
    /// Metoda abstarkcyjna do przeciązenienia w adapterze realnym. Realizauje konwersję z danych uniwersalnych o statusach zawartych w strukturze List&lt;EcomOrderStatusInfo&gt; do struktur danego drajwera i wysyła dane statusuów do kanału sprzedaży w osobnych  komendach na poziomie adaptera realnego np. PutProductRequestCommand. Po otrzymaniu odpowiedzi zaznacza, że udało się zaktualizować status, albo że wystąp;ił błąd
    /// Uruchamiana w handlerze ExportOrdersStatusCommandHandler 
    /// </summary>
    /// <param name="context"></param>
    [UUID("c0f215a67fa1445cb55ef777e1aee583")]
    override public string DoExportOrderStatus(ConsumeContext<ECOM.ExportOrdersStatusCommand> context)
    {
      string errMsg = "";  
      int? ecomOrderID = null;
      if((context.Message.OrderStatusInfoList?.Count ?? 0) > 0)
      {  
        var ecomOrder = new ECOM.ECOMORDERS();
        foreach(var orderStatus in context.Message.OrderStatusInfoList)
        {
          try
          {
            ecomOrderID = Int32.Parse(orderStatus.OrderId);
            var request = new EComBLDriver.Models.BLExportOrderStatusRequest();
            request.OrderId = Int32.Parse(orderStatus.OrderId);
            request.Status = orderStatus.OrderStatusId; 
           
            ecomOrder.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{ecomOrder.ECOMCHANNELREF.Symbol} = 0{context.Message.EcomChannel} " +
              $"AND {nameof(ECOM.ECOMORDERS)}.{ecomOrder.ECOMORDERID.Symbol} = '{orderStatus.OrderId}'");
            if(!ecomOrder.FirstRecord())
            {
              errMsg += $"Nie znaleziono zamówienia o symbolu: {orderStatus.OrderSymbol} w kanale: {context.Message.EcomChannel.ToString()}\n";            
              continue;        
            }
    
            if(string.IsNullOrEmpty(ecomOrder.EDAID))
            {
              //zamówienie w momencie próby eksportu statusu musi mieć przypisany identyfikator EDA
              errMsg += $"Zamówienia o symbolu: {orderStatus.OrderSymbol} w kanale: {context.Message.EcomChannel.ToString()} nie ma przypisanego identyfikatora EDA\n";            
              continue;
            }
            
            // generowanie danych requesta do API dla paczek zamówienia 
            var packageRequest = new EComBLDriver.Models.BLAddPackagesRequest();
            if((orderStatus.OrderSpedInfo?.PackageList?.Count ?? 0) > 0)
            {
              packageRequest.OrderId = Int32.Parse(orderStatus.OrderId);
              packageRequest.CourierCode = orderStatus.OrderSpedInfo.ShipperSymbol;
              // W BL paczki dodajemy pojedynczo
              foreach(var package in orderStatus.OrderSpedInfo.PackageList)
              {
                packageRequest.PackageNumber = package.ShipqingSymbol;
                var packagesCommand = new ECOM.BLPutOrderPacakgeRequestCommand()
                {
                  EcomChannel = context.Message.EcomChannel,          
                  ApiPackageRequest = packageRequest
                };        
                EDA.ExecuteCommand(context.Headers.Get<string>("NeosConnector"), packagesCommand);
              }
            }
    
            // wysyłka faktur
            if(!String.IsNullOrEmpty(orderStatus?.InvoiceInfo?.NagfakRef))
            {
              string typfak = LOGIC.ECOMCHANNELCONVERT.ConvertToChannelValue(context.Message.EcomChannel, "TYPFAK", orderStatus.InvoiceInfo.Typfak);
    
                // dodanie nośnika do faktur - jeśli nośnik już wcześniej został założony zostanie on pobrany z bramki
                // po otrzymaniu id nośnika jest wywołana komenda do wysłania faktury
                var insertCarrierRequest = new EComBLDriver.Models.BLAddOrderInvoiceCarrierRequest();
                insertCarrierRequest.OrderId = Int32.Parse(orderStatus.OrderId);
                insertCarrierRequest.SeriesId = Int32.Parse(typfak);
    
                var insertCarrierCommand = new ECOM.BLPutInvoiceCarrierRequestCommand()
                {
                  EcomChannel = context.Message.EcomChannel,
                  EcomOrderId = Int32.Parse(orderStatus.OrderId),
                  InvoiceFilePath = System.IO.Path.Combine(orderStatus.InvoiceInfo.AttachmentPath, orderStatus.InvoiceInfo.FileName + ".pdf"),
                  AttachmentSymbol = orderStatus.InvoiceInfo.AttachmentSymbol,
                  ApiInvoiceCarrierRequest = insertCarrierRequest
                };  
                EDA.ExecuteCommand(context.Headers.Get<string>("NeosConnector"), insertCarrierCommand);
            }
           
            var edaIdentifierData = ecomOrder.EDAID;        
            var command = new ECOM.BLPutOrderStatusRequestCommand()    
            {
              EcomChannel = context.Message.EcomChannel,
              EcomOrderId = Int32.Parse(orderStatus.OrderId),
              ApiRequest = request         
            };        
            command.SetIdentifier(edaIdentifierData);
            command.SetLogicalQueueIdentifier(context.Message.GetLogicalQueueIdentifier());
            
            RunInAutonomousTransaction(
              ()=>
              {     
                ecomOrder.EditRecord();   
                ecomOrder.SYNCSENDCHANELLSTATUS = (int)EcomSyncStatus.ExportProceeding;
                if(!ecomOrder.PostRecord())
                {
                  throw new Exception($"Błąd zapisu zmiany statusu zamówienia o ID: {ecomOrderID}");
                }
                EDA.SendCommand(context.Headers.Get<string>("NeosConnector"), command);         
              },
              true
            );              
          }
          catch(Exception e)
          {
            errMsg += e.Message;
            //błąd zapisywany w autonomicznej transakcji, żeby zapisał się nawet w przypadku exceptiona bazodanowego 
            RunInAutonomousTransaction(
              ()=>
              {
                ecomOrder.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{ecomOrder.ECOMCHANNELREF.Symbol} = {context.Message.EcomChannel}" +
                  $"AND {nameof(ECOM.ECOMORDERS)}.{ecomOrder.ECOMORDERID.Symbol} = '{ecomOrderID}'");
                if(ecomOrder.FirstRecord())
                {
                  ecomOrder.EditRecord();
                  ecomOrder.LASTSTATUSSYNCERROR = e.Message;
                  ecomOrder.SYNCSTATUS = (int)EcomSyncStatus.ExportError;
                  if(!ecomOrder.PostRecord())
                  {
                    throw new Exception($"Błąd zapisu zmiany statusu zamówienia o ID: {ecomOrderID}");
                  }
                }
                else
                {
                  throw new Exception($"Nie znaleziono zamówienia o ID: {ecomOrderID} w kanale: {context.Message.EcomChannel}");
                }  
              },
              true
            );                
            context.Message.Message = "Błąd eksportu statusu zamówienia: " + e.Message;
            continue;
          }  
        }    
        ecomOrder.Close();   
      }
      else
      {
        errMsg = "Brak statusów zamówienia do wysłania na witrynę.";
      }
      
      // zwracamy treść błędu, a wyjątek zostanie rzucony w handlerze, który odpala tą metodę 
      return errMsg;  
    }
    
    


    /// <param name="context"></param>
    [UUID("da2b428c09d8413daab623d8dbd93cbd")]
    override public string DoExportInventoryStock(ConsumeContext<ECOM.ExportInventoryStocksCommand> context)
    {
      string errMsg = "";      
      var request = new EComBLDriver.Models.BLUpdateProductStockRequest();
      Contexts ecomChannelParams = LOGIC.ECOMSTDMETHODS.LoadEcomChannelParams(context.Message.EcomChannel);
      request.InventoryId = ecomChannelParams["_inventorycatalogid"];
      request.Products = new Dictionary<string, EComBLDriver.Models.BLUpdateProductStockRequest.ProductModel>();
      
      foreach(var inventoryStockInfoGroup in context.Message.InventoryStocksInfoList.GroupBy(i => i.InventoryId))
      {        
        var productModel = new EComBLDriver.Models.BLUpdateProductStockRequest.ProductModel();
        productModel.Stock = new Dictionary<string, Newtonsoft.Json.Linq.JToken>();
        request.Products.Add(inventoryStockInfoGroup.Key, productModel);
        foreach (var inventoryStockInfo in inventoryStockInfoGroup)
        {
          try
          {
            productModel.Stock.Add(inventoryStockInfo.ChannelStockId, inventoryStockInfo.Quantity);
    
            RunInAutonomousTransaction(
              ()=>
              {     
                var invStock = new ECOM.ECOMINVENTORYSTOCKS();
                invStock.FilterAndSort($"{nameof(ECOM.ECOMINVENTORYSTOCKS)}.{invStock.REF.Symbol} = 0{inventoryStockInfo.EcomInventoryStockRef}");
                if(!invStock.FirstRecord())
                {   
                  errMsg += $"Nie znaleziono stanu magazynowego o ref: {inventoryStockInfo.EcomInventoryStockRef} w kanale: {context.Message.EcomChannel}\n";            
                }
                else 
                {
                  invStock.EditRecord();      
                  invStock.SYNCSTATUS = (int)EcomSyncStatus.ExportProceeding;           
                  if(!invStock.PostRecord())
                  {
                    throw new Exception($"Błąd zapisu zmiany statusu dla stanu mag. o REF: {inventoryStockInfo.EcomInventoryStockRef}");
                  } 
                }
                invStock.Close();      
              },
              true
            );
          }     
          catch(Exception ex)
          { 
            errMsg += $"Błąd przy generowaniu danych do wysłania stanu magazynowego na witrynę dla towaru o ID: {inventoryStockInfoGroup.Key} " +
              $"w kanale sprzedaży {context.Message.EcomChannel}: {ex.Message}\n";       
          
            RunInAutonomousTransaction(
              ()=>
              {
                var invStock = new ECOM.ECOMINVENTORYSTOCKS();
                invStock.FilterAndSort($"{nameof(ECOM.ECOMINVENTORYSTOCKS)}.{invStock.REF.Symbol} = 0{inventoryStockInfo.EcomInventoryStockRef}");
                if(invStock.FirstRecord())
                {    
                  invStock.EditRecord();      
                  invStock.SYNCSTATUS = (int)EcomSyncStatus.ExportError;   
                  invStock.LASTSYNCERROR = ex.Message;       
                  if(!invStock.PostRecord())
                  {
                    throw new Exception($"Błąd zapisu zmiany statusu dla stanu mag. o REF: {inventoryStockInfo.EcomInventoryStockRef}");
                  }          
                }
                else
                {
                  throw new Exception($"Nie znaleziono stanu mag. dla towaru o ID: {inventoryStockInfo.EcomInventoryStockRef}.");
                }
                invStock.Close(); 
              },
              true
            );
            continue;
          }                             
        }     
      }
      
      var command = new ECOM.BLPutProductsStocksRequestCommand()  
      {	
        EcomChannel = context.Message.EcomChannel,
        ApiRequest = request 
      };  
    
      if(!string.IsNullOrEmpty(context.Message.GetLogicalQueueIdentifier()))
      {
        command.SetLogicalQueueIdentifier(context.Message.GetLogicalQueueIdentifier());
      }		    
        
      EDA.SendCommand(context.Headers.Get<string>("NeosConnector"), command);   
      return errMsg;
    }
    /// <param name="context"></param>
    [UUID("439f67b1370e42b3875a04081b634fc3")]
    override public string DoImportOrderPackages(ConsumeContext<ECOM.ImportOrderPackagesRequestCommand> context)
    {
      string errMsg = ""; 		
    
    	try
    	{
        	var command = new ECOM.BLGetOrderPackagesRequestCommand()    
    			{ 
    				EcomOrderId = context.Message.OrderId,
    				EcomChannel = context.Message.EcomChannel
    			};
    		EDA.SendCommand(context.Headers.Get<string>("NeosConnector"), command, context);
    	}
    	catch(Exception ex)
    	{
    		errMsg = ex.Message;
    	}
        return errMsg;
    }
    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("d58b1aa90c9e4eef915f3673bbc0ce37")]
    virtual public HandlerResult BLGetOrderPackagesRequestCommandHandler(ConsumeContext<ECOM.BLGetOrderPackagesRequestCommand> context)
    {
    string errMsg = "";	
      	try
    	{			
    		var driver = new EComBLDriver.BLDriver();
    		
    		BLDriver.ClientData clientData = new BLDriver.ClientData()
    		{				
    			TokenAPI = Parameters["apitoken"].AsString,		
    			Method = Parameters["getorderpackages"].AsString,	
    			ApiDomain = Parameters["apidomain"].AsString
    		};			
    				
    		string errorMessage;
    
    		EComBLDriver.Models.BLGetOrderPackagesRequest BLApiRequest = new EComBLDriver.Models.BLGetOrderPackagesRequest()
    		{
    			OrderId = context.Message.EcomOrderId
    		};
    		
    		//pobranie synchroniczne paczek z BL								
    		var response = driver.BLGetOrderPackages(clientData, BLApiRequest, out errorMessage);		
    
    		if(!string.IsNullOrEmpty(errorMessage))
    		{
    			throw new Exception(errorMessage + " JsonConvert.SerializeObject(clientData)" + JsonConvert.SerializeObject(clientData));
    		}
    		if(response == null)
    		{
    			throw new Exception("Brak danych w odpowiedzi bramki");			
    		}
    		if(response.Status != "SUCCESS")
    		{
    			errMsg = $"Błąd pobierania paczek do zamówienia: " + context.Message.EcomOrderId + ". Kod błędu: {response.ErrorCode}, Opis: {response.ErrorMessage}";
    			throw new Exception(errMsg);
    		}
    	
    		if (response.Packages != null && response.Packages.Count() == 0)
    		{
    		throw new Exception("Brak paczek do zamówienia");
    		}		
    		var command = new ECOM.BLGetOrderPackagesResponseCommand()
    		{
    			OrderId = context.Message.EcomOrderId,
    			BLApiResponse = response,
    			EcomChannel = context.Message.EcomChannel
    		};
    
    		command.SetIdentifier(context.Message.GetIdentifier());
    				
    		EDA.SendCommand(context.Headers.Get<string>("NeosConnector"), command);  
    		
    	}
    	catch(Exception ex)
    	{
    		context.Message.Message = ex.Message;
    		throw new Exception(ex.Message);
    	}
      	return HandlerResult.PipeSuccess;
    }
    /// <param name="ecomOrder"></param>
    /// <param name="packageSource"></param>
    [UUID("f9f4fc64b5654bee83b3177ca8e316f5")]
    virtual public EcomOrderPackageInfo BLOrderPackageToEcomOrderPackageInfo(int ecomOrder,EComBLDriver.Models.BLGetOrderPackagesResponse.Package packageSource)
    {  
      var orderPackageInfo = new EcomOrderPackageInfo();
      
      try
      {    
        //danepaczki
        orderPackageInfo.OrderId = ecomOrder;
        orderPackageInfo.ExternalPackageId = packageSource.PackageId;
        orderPackageInfo.PackageNumber = packageSource.CourierPackageNr;
        orderPackageInfo.PackageDate = DateTime.Now;
        orderPackageInfo.CourierCode = packageSource.CourierCode;
      } 
      catch(Exception ex)
      {
        throw new Exception("Błąd dodawnia danych paczki do klas uniwersalnych: " + ex.Message);
      }  
    
      return orderPackageInfo;
    }
    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("c11f1b7a06e44dab8106a9bfa24613b6")]
    virtual public HandlerResult BLGetOrderPackagesResponseCommandHandler(ConsumeContext<ECOM.BLGetOrderPackagesResponseCommand> context)
    {
      
      string errMsg = "";
    
      foreach (var packageResult in context.Message.BLApiResponse.Packages)
      {
        EcomOrderPackageInfo orderPackageInfo = new EcomOrderPackageInfo();
        try 
        {
          orderPackageInfo = BLOrderPackageToEcomOrderPackageInfo(context.Message.OrderId, packageResult);
          LOGIC.ECOMSTDMETHODS.InsertOrUpdateOrderPackage(context.Message.EcomChannel ,orderPackageInfo); 
          var command = new ECOM.BLGetLabelRequestCommand()
    			{
    				EcomOrderId = orderPackageInfo.OrderId,
    				EcomChannel = context.Message.EcomChannel,
    				EcomPackageId = orderPackageInfo.ExternalPackageId,
    				EcomPackageNumber = orderPackageInfo.PackageNumber,
            EcomCourierCode = orderPackageInfo.CourierCode
    			};
    			command.SetIdentifier(context.Message.GetIdentifier());		
    			EDA.SendCommand(context.Headers.Get<string>("NeosConnector"), command);   
        }
        catch(Exception ex)
        {
          errMsg = $"Błąd zapisu paczki do zamówienia {orderPackageInfo.OrderId}: {ex.Message}. " + "\n";
          throw new Exception(errMsg);
        }
      }
    
      return HandlerResult.PipeSuccess;  
    }
    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("2ed4393759aa49fd948f18b02ba887b0")]
    virtual public HandlerResult BLGetLabelResponseCommandHandler(ConsumeContext<ECOM.BLGetLabelResponseCommand> context)
    {
      string errMsg = "";
    
      EcomPackageLabelInfo packageInfo = new EcomPackageLabelInfo();
      var methodsObject = LOGIC.ECOMCHANNELS.FindMethodObject(context.Message.EcomChannel);
    
      try
      {
        packageInfo = BLLabelInfoToEcomPackageLabelInfo(context.Message.EcomOrderId, context.Message.EcomPackageId, context.Message.BLApiResponse);
        int id = methodsObject.Logic.InsertOrUpdatePackageLabel(context.Message.EcomChannel, packageInfo);
      }
      catch (Exception ex)
      {
        errMsg = $"Błąd zapisu etykiety do paczki {context.Message.EcomPackageId}: {ex.Message}. " + "\n";
        throw new Exception(errMsg);
      }
    
      return HandlerResult.PipeSuccess;
    }
    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("09de5a3ea749423ab1bfab8840949888")]
    virtual public HandlerResult BLGetLabelRequestCommandHandler(ConsumeContext<ECOM.BLGetLabelRequestCommand> context)
    {
    string errMsg = "";	
      	try
    	{			
    		var driver = new EComBLDriver.BLDriver();
    		
    		BLDriver.ClientData clientData = new BLDriver.ClientData()
    		{				
    			TokenAPI = Parameters["apitoken"].AsString,		
    			Method = Parameters["getlabel"].AsString,	
    			ApiDomain = Parameters["apidomain"].AsString
    		};			
    				
    		string errorMessage;
    
    		EComBLDriver.Models.BLGetLabelRequest BLApiRequest = new EComBLDriver.Models.BLGetLabelRequest()
    		{
    			OrderId = context.Message.EcomOrderId,
    			FileSaveDirectory = Parameters["labelspath"].AsString,
    			PackageNumber = context.Message.EcomPackageNumber,
    			PackageId = context.Message.EcomPackageId,
    			CourierCode = context.Message.EcomCourierCode
    			
    		};
    		
    		//pobranie synchroniczne paczek z BL								
    		var response = driver.BLGetLabel(clientData, BLApiRequest, out errorMessage);		
    
    		if(!string.IsNullOrEmpty(errorMessage))
    		{
    			throw new Exception(errorMessage + " JsonConvert.SerializeObject(clientData)" + JsonConvert.SerializeObject(clientData));
    		}
    		if(response == null)
    		{
    			throw new Exception("Brak danych w odpowiedzi bramki");			
    		}
    		if(response.Status != "SUCCESS")
    		{
    			errMsg = $"Błąd pobierania etykiet do paczki: " + context.Message.EcomPackageId + ". Kod błędu: {response.ErrorCode}, Opis: {response.ErrorMessage}";
    			throw new Exception(errMsg);
    		}
    		if (String.IsNullOrEmpty(response.FilePath))
    		{
    			throw new Exception("Brak ścieżki do pliku etykiety");
    		}	
      	
    		var command = new ECOM.BLGetLabelResponseCommand()
    		{
    			EcomOrderId = context.Message.EcomOrderId,
    			EcomChannel = context.Message.EcomChannel,
    			EcomPackageId = context.Message.EcomPackageId,
    			EcomPackageNumber = context.Message.EcomPackageNumber,
    			BLApiResponse = response
    		};
    
    		command.SetIdentifier(context.Message.GetIdentifier());
    				
    		EDA.SendCommand(context.Headers.Get<string>("NeosConnector"), command);  
    
    	}
    	catch(Exception ex)
    	{
    		context.Message.Message = ex.Message;
    		throw new Exception(ex.Message);
    	}
      	return HandlerResult.PipeSuccess;
    }
    /// <param name="ecomOrder"></param>
    /// <param name="packageId"></param>
    /// <param name="labelSource"></param>
    [UUID("27282804d077427490f5f607fdd7642f")]
    virtual public EcomPackageLabelInfo BLLabelInfoToEcomPackageLabelInfo(int ecomOrder, int packageId,EComBLDriver.Models.BLGetLabelInfo labelSource)
    {  
      var labelInfo = new EcomPackageLabelInfo();
      
      try
      {    
        //danepaczki
        labelInfo.OrderId = ecomOrder;
        labelInfo.PackageId = packageId;
        labelInfo.LabelPath = labelSource.FilePath;
        var labelType = new ECOM.TYPYETYK();
        labelType.FilterAndSort($"{nameof(ECOM.TYPYETYK)}.{labelType.SYMBOL.Symbol} = '{labelSource.FileExtension.ToUpper()}' ");
        if(labelType.FirstRecord())
        {
          labelInfo.LabelType = labelType.REF.AsInteger;
        }
        labelType.Close();
      } 
      catch(Exception ex)
      {
        throw new Exception("Błąd dodawnia danych etykiet do klas uniwersalnych: " + ex.Message);
      }  
    
      return labelInfo;
    }
    /// <param name="orderId"></param>
    /// <param name="logType"></param>
    /// <param name="objectId"></param>
    /// <param name="channelRef"></param>
    [UUID("d31b37013e5e4d2fab500cd2ed28b427")]
    virtual public HashSet<int> ProcessOrderLogByType(string orderId, string logType, string objectId, int channelRef)
    {
      var orderList = new HashSet<int>();
      ILogStrategy strategy;
    
      try
      {
        orderList.Add(int.Parse(orderId));
        
        Dictionary<string, ILogStrategy> logStrategies = new Dictionary<string, ILogStrategy>
        {
              { "5", new MergeOrderStrategy() },
              { "6", new SplitOrderStrategy() },
              { "9", new PackageAddedStrategy() }
        };
    
        if (!logStrategies.TryGetValue(logType, out strategy))
        {
          strategy = new NoLogStrategy();
        }
        orderList.UnionWith(strategy.Execute(orderId, objectId, channelRef));
      }
    
      catch (Exception e)
    
      {
    
        throw new Exception("Błąd obsługi logów: " + e.Message);
    
      }
    
      return orderList;
    }
    /// <param name="MergeOrderStrategy("></param>


  }
}
