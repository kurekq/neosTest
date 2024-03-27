
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
  
  public partial class ECOMADAPTERIAI<TModel> where TModel : MODEL.ECOMADAPTERIAI, new()
  {

    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("0fde3afef01b4a9497a3c3eaa0aa2071")]
    [PhysicalQueueName("ExportInventoryStockQueue")]
    virtual public HandlerResult PutProductsStocksResponseCommandHandler(ConsumeContext<ECOM.PutProductsStocksResponseCommand> context)
    {
      string errMsg = "";
      {
        foreach(var response in context.Message.ApiResponse.results)
        {
          //aktualizacje każdego w osobnej transakcji, bo jak dostaniemy exceptiona, to i tak chcemy oznaczyć te stany
          //które udało się wysłać
          RunInAutonomousTransaction(
            ()=>
            {
              var inventoryStocks = new ECOM.ECOMINVENTORYSTOCKS();
              try
              {
                inventoryStocks.FilterAndSort($"es.{inventoryStocks.ECOMSTOCKID.Symbol} = '{response.stockId}' " +
                  $"AND {inventoryStocks.WERSJAREF.Symbol} = {response.productSizeCodeExternal} " +
                  $"AND es.{inventoryStocks.ECOMCHANNELREF.Symbol} = 0{context.Message.EcomChannel}");
                if(inventoryStocks.FirstRecord())
                {            
                  if(response.faultCode != 0)
                  {
                    throw new Exception("Błąd API: " + response.faultString);      
                  }
                  else
                  {   
                    inventoryStocks.EditRecord();
                    inventoryStocks.SYNCSTATUS = (int)EcomSyncStatus.Exported;
                    inventoryStocks.LASTSYNCERROR = "";
                    inventoryStocks.LASTSENDTMSTMP = DateTime.Now;
                    if(!inventoryStocks.PostRecord())
                    {
                      throw new Exception($"Błąd zapisu zmiany statusu wersji towaru: {response.productSizeCodeExternal}");
                    }	  
                  } 
                }
                else
                {
                  //jak nie znaleziono rekordu, to nie aktualizujemy żadnego ECOMSTOCKS
                  //tylko zapisujemy błąd i idziemy dalej
                  errMsg += $"Nie znaleziono stanu magazynowego dla wersji towaru: {response.productSizeCodeExternal}\n";
                }
              }
              catch(Exception ex)
              {
                inventoryStocks.EditRecord();
                inventoryStocks.SYNCSTATUS = (int)EcomSyncStatus.ExportError;
                inventoryStocks.LASTSYNCERROR = ex.Message;							
                if(!inventoryStocks.PostRecord())
                {
                  throw new Exception($"Błąd zapisu zmiany statusu wersji towaru: {response.productSizeCodeExternal}");
                }	  
                errMsg += $"{ex.Message}\n";           
              }
              inventoryStocks.Close();
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
    
    


    [UUID("20b43084229d420e9ce857617c8d5c88")]
    virtual public object PutOrderPackages()
    {
      var driver = new EComIAIDriver.IAIDriver();
    		
      ClientData clientData = new ClientData()
      {
        Login = Parameters["apilogin"].AsString,
        Password = Parameters["apipassword"].AsString,
        Address = Parameters["updateordersapiaddress"].AsString,
        MaxReceivedMessageSize = Parameters["maxreceivedmessagesize"].AsInteger
      };
        
      return "";
    }
    
    
    


    /// <summary>
    /// Metoda uruchamiana przez metodę interfejsu OpenOrderInAdminPanel pobieracjąca parametr konektora zawierający
    /// adres domeny, w której dostępny jest panel administratora
    /// parametry
    /// connector - ref konektora
    /// zwraca:
    ///  - domenę w której znajduje się panel administratora IAI. Jest ona parametrem konektora. 
    /// </summary>
    /// <param name="connector"></param>
    [UUID("44d6cf844d0b4b928937ec41616d8956")]
    public static string OpenOrderInAdminPanelLogic(int connector)
    {
      return CORE.GetField("NVALUE", $"SYS_EDACONNECTORPARAMS where EDACONNECTORREF = 0{connector.ToString()} and NKEY = 'basedomain'");    
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("47b3cab3a4fb44dfba517859fdf83fa0")]
    virtual public HandlerResult PutOrderPacakgesRequestCommandHandler(ConsumeContext<ECOM.PutOrderPacakgesRequestCommand> context)
    {
      try
      {
        var driver = new EComIAIDriver.IAIDriver();
    			
    		ClientData clientData = new ClientData()
    		{
    			Login = Parameters["apilogin"].AsString,
    			Password = Parameters["apipassword"].AsString,
    			Address = Parameters["addpackagesapiaddress"].AsString,
    			MaxReceivedMessageSize = Parameters["maxreceivedmessagesize"].AsInteger
    		};
    
    		EComIAIDriver.IAIAddPackages.addPackagesResponseType response = null;	
    		string errorMessage;
    		response = driver.AddPackages(clientData, context.Message.ApiPackagesRequest, out errorMessage);
    
    		if(!string.IsNullOrEmpty(errorMessage))
    		{
    			throw new Exception($"Błąd wysłania danych paczkek: {errorMessage}");
    		}
    		else if(response == null)
    		{			
    			throw new Exception("Brak danych w odpowiedzi bramki");			
    		}
    		else if((response.errors?.faultCode ?? 0) != 0)
    		{	
    			throw new Exception($"Błąd wysłania danych paczkek. Kod błędu: {response.errors.faultCode}, Opis: {response.errors.faultString}");
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
    [UUID("510408cb0e3c4967bee3c98e516178ee")]
    [PhysicalQueueName("ExportInventoryQueue")]
    virtual public HandlerResult PutProductRequestCommandHandler(ConsumeContext<ECOM.PutProductRequestCommand> context)
    {
      if(context.Message.ApiRequest.@params.products.Count() > 1)
      {
        throw new Exception("Aktualizacja i wysłanie wielu towarów nieobsługiwane.");
      }
      EComIAIDriver.IAIUpdateProducts.responseType response = null;
    
      try
      {     
        var driver = new EComIAIDriver.IAIDriver();   
        ClientData clientData = new ClientData()
        {
          Login = Parameters["apilogin"].AsString,
          Password = Parameters["apipassword"].AsString,
          Address = Parameters["updateproductsapiaddress"].AsString,
          MaxReceivedMessageSize = Parameters["maxreceivedmessagesize"].AsInteger
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
        else if((response.errors?.faultCode ?? 0) != 0)
        { 
          throw new Exception($"Błąd wysłania produktu. Kod błędu: {response.errors.faultCode}, Opis: {response.errors.faultString}");
        }
        
        var command = new ECOM.PutProductResponseCommand() 
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
        EDA.ExecuteCommand($"{context.Headers.Get<string>("NeosConnector")}:ExportInventoryQueue", command);        
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
            var tempProductId = response.results?.productsResults?.FirstOrDefault()?.productId;
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
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("7703e3c9fcd74d5881b9ae509aee6b06")]
    virtual public HandlerResult PutOrderStatusResponseCommandHandler(ConsumeContext<ECOM.PutOrderStatusResponseCommand> context)
    {
      string errorMsg = ""; 
      foreach(var orderResult in context.Message.ApiResponse.results.ordersResults)
      {
        try
        {  
          if(orderResult.faultCode != 0)
          {      
            throw new Exception("Błąd API: " + orderResult.faultString); 
          }
          else
          {        
            var ecomOrderData = new ECOM.ECOMORDERS(); 
            ecomOrderData.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{ecomOrderData.ECOMCHANNELREF.Symbol} = 0{context.Message.EcomChannel} " +
              $"AND {nameof(ECOM.ECOMORDERS)}.{ecomOrderData.ECOMORDERSYMBOL.Symbol} = '{orderResult.orderId}'");
            if(ecomOrderData.FirstRecord())
            {   
              ecomOrderData.EditRecord();    
              ecomOrderData.SYNCSENDCHANELLSTATUS = (int)EcomSyncStatus.Exported;
              ecomOrderData.LASTSENDTMSTMP = DateTime.Now; 
              ecomOrderData.LASTSTATUSSYNCERROR = "";           
              if(!ecomOrderData.PostRecord())
              {
                throw new Exception($"Błąd zapisu zmiany statusu zamówienia o ID: {orderResult.orderId}");
              }
            }
            else
            {
              throw new Exception($"Nie znaleziono zamówienia o symbolu: {orderResult.orderId} w kanale: {context.Message.EcomChannel.ToString()}");                     
            }
            ecomOrderData.Close();
          }
        }
        catch(Exception ex)
        {
          RunInAutonomousTransaction(()=>{
            var ecomOrderData = new ECOM.ECOMORDERS();
            ecomOrderData.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{ecomOrderData.ECOMCHANNELREF.Symbol} = 0{context.Message.EcomChannel} " +
              $"AND {nameof(ECOM.ECOMORDERS)}.{ecomOrderData.ECOMORDERSYMBOL.Symbol} = '{orderResult.orderId}'");
            if(ecomOrderData.FirstRecord()) 
            {     
              ecomOrderData.EditRecord();  
              ecomOrderData.SYNCSENDCHANELLSTATUS = (int)EcomSyncStatus.ExportError;            
              if(!ecomOrderData.PostRecord())
              {
                throw new Exception($"Błąd zapisu zmiany statusu zamówienia o ID: {orderResult.orderId}");
              }     
            }
            else
            {
              throw new Exception($"Nie znaleziono zamówienia o symbolu: {orderResult.orderId} w kanale: {context.Message.EcomChannel.ToString()}");                     
            }  
             ecomOrderData.Close();     
          });
    
          errorMsg += 
            $"Błąd aktualizacji statusu zamówienia o symbolu: {orderResult.orderId} " +
              $" w kanale sprzedaży: {context.Message.EcomChannel.ToString()}: {ex.Message}\n";                  
          
          continue;
        }
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
    [UUID("858cfdc5e4fd4c66bfce09f00fb0ccb5")]
    virtual public HandlerResult PutAttachmentResponseCommandHandler(ConsumeContext<ECOM.PutAttachmentResponseCommand> context)
    {
      try
      {
        var insertAttachmentResult = context.Message.ApiInsertAttachmentResponse.documentsResults.First();              
        if(insertAttachmentResult.errors.faultCode != 0)
        {
          throw new Exception("Błąd API: " + insertAttachmentResult.errors.faultString);      
        }
      }
    	catch(Exception ex)
    	{    
        context.Message.Message += ex.Message;
        throw new Exception(ex.Message); 
    	} 
      return HandlerResult.Handled;
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("87c8c9326a9b4363b72f3721e1ca504a")]
    virtual public HandlerResult PutAttachmentRequestCommandHandler(ConsumeContext<ECOM.PutAttachmentRequestCommand> context)
    {
    	try
    	{
        	var driver = new EComIAIDriver.IAIDriver();			
    		ClientData clientData = new ClientData()
    		{
    			Login = Parameters["apilogin"].AsString,
    			Password = Parameters["apipassword"].AsString,
    			Address = Parameters["insertdocumentsapiaddress"].AsString,
    			MaxReceivedMessageSize = Parameters["maxreceivedmessagesize"].AsInteger
    		};
    
    		EComIAIDriver.IAIInsertDocuments.insertDocumentsResponseType response = null;
    		string errorMessage;
    		response = driver.InsertDocuments(clientData, context.Message.ApiInsertAttachmentRequest, out errorMessage);
    
    		if(!string.IsNullOrEmpty(errorMessage))
    		{
    			throw new Exception($"Błąd wysłania załącznika: {errorMessage}");
    		}
    		else if(response == null)
    		{			
    			throw new Exception("Brak danych w odpowiedzi bramki");			
    		}
    		else if((response.errors?.faultCode ?? 0) != 0)
    		{	
    			throw new Exception($"Błąd wysłania załącznika. Kod błędu: {response.errors.faultCode}, Opis: {response.errors.faultString}");
    		}
    		
    		var command = new ECOM.PutAttachmentResponseCommand()    
    		{
    			//EcomChannel = context.Message.EcomChannel,
    			ApiInsertAttachmentResponse = response			
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
    
    


    /// <summary>
    /// Hanlder komendy PutProductResponseCommand, która zawiera odpowiedź witryne na wysłanie do niej nowego towaru, lub aktualizację jjuz istniejącego.
    /// Handler aktualizuje symbol towaru ECOMINVENTORYID i aktualizuje status synchronizacji towaru. W przypadku błędu rzuca wyjątek
    /// </summary>
    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("a417211b901b4b048e67adcf159a3ff1")]
    [PhysicalQueueName("ExportInventoryQueue")]
    virtual public HandlerResult PutProductResponseCommandHandler(ConsumeContext<ECOM.PutProductResponseCommand> context)
    {  
      try
      {
        var productResult = context.Message.ApiResponse.results.productsResults.First();              
        if(productResult.faultCode != 0)
        {
          throw new Exception("Błąd API: " + productResult.faultString);      
        }
        else
        {          
          ECOM.ECOMINVENTORIES inventory = new ECOM.ECOMINVENTORIES();
          inventory.FilterAndSort($"{nameof(ECOM.ECOMINVENTORIES)}.{inventory.ECOMCHANNELREF.Symbol} = 0{context.Message.EcomChannel} " +
            $"AND {nameof(ECOM.ECOMINVENTORIES)}.{inventory.WERSJAREF.Symbol} = 0{context.Message.EcomInventoryVersion}");
          if(inventory.FirstRecord())
          {		 
            inventory.EditRecord();
            inventory.ECOMINVENTORYID = productResult.productId;
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
            throw new Exception($"Nie znaleziono wersji towaru o REF: {context.Message.EcomInventoryVersion} w kanale: {context.Message.EcomChannel.ToString()}");
          }
          inventory.Close();
        }
      }
    	catch(Exception ex)
    	{    
        context.Message.Message += ex.Message;
        throw new Exception("Błąd aktualizacji danych towaru w kanale sprzedaży " + ex.Message);
    	} 
      return HandlerResult.Handled;
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("b5dd1b37ee9c4d9dbb0d6abe6b1a1231")]
    [PhysicalQueueName("ExportInventoryQueue")]
    virtual public HandlerResult PutProductPriceRequestCommandHandler(ConsumeContext<ECOM.PutProductPriceRequestCommand> context)
    {
    	EComIAIDriver.IAIUpdateProducts.responseType response = null;
      try
      {			
        var driver = new EComIAIDriver.IAIDriver();
    		ClientData clientData = new ClientData()
    		{
    			Login = Parameters["apilogin"].AsString,
    			Password = Parameters["apipassword"].AsString,
    			Address = Parameters["updateproductsapiaddress"].AsString,
    			MaxReceivedMessageSize = Parameters["maxreceivedmessagesize"].AsInteger
    		};			
    			
    		string errorMessage;
    		//to nie jest pomyłka, aktualizacja ceny towaru i samego towaru to jest ta sama metoda w API IAI
    		response = driver.UpdateProduct(clientData, context.Message.ApiRequest, out errorMessage);
    		if(!string.IsNullOrEmpty(errorMessage))
    		{
    			throw new Exception($"Błąd wysłania ceny produktu: {errorMessage}");
    		}
    		else if(response == null)
    		{			
    			throw new Exception("Brak danych w odpowiedzi bramki");			
    		}
    		else if((response.errors?.faultCode ?? 0) != 0)
    		{	
    			throw new Exception($"Błąd wysłania ceny produktu. Kod błędu: {response.errors.faultCode}, Opis: {response.errors.faultString}");
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
    			foreach(var ecomInventoryId in context.Message.ApiRequest.@params.products.Select(p => p.productId))
    			{	
    				ecomInventory.FilterAndSort($"{nameof(ECOM.ECOMINVENTORIES)}.{ecomInventory.ECOMINVENTORYID.Symbol} = '{ecomInventoryId}' " +
    					$"AND {nameof(ECOM.ECOMINVENTORIES)}.{ecomInventory.ECOMCHANNELREF.Symbol} = 0{context.Message.EcomChannel}");
    				if(ecomInventory.FirstRecord())				
    				{
    					//cena per produkt w IAI jest tylko jedna więc bez forselecta
    					invPrice.FilterAndSort($"{nameof(ECOM.ECOMINVENTORYPRICES)}.{invPrice.ECOMINVENTORYREF.Symbol} = 0{ecomInventory.REF}");
    					if(invPrice.FirstRecord())
    					{		
    						invPrice.EditRecord();				
    						invPrice.SYNCSTATUS = (int)EcomSyncStatus.ExportError;							
    						if(!invPrice.PostRecord())
    						{
    							throw new Exception($"Błąd zapisu zmiany statusu ceny dla zamówienia o ID: {ecomInventoryId}");
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
    			}
    			invPrice.Close();
    			ecomInventory.Close();
    		},
    		true
    	);
    	context.Message.Message = ex.Message;
    	throw new Exception(ex.Message);
      }
    
    	try
    	{
    		var command = new ECOM.PutProductPriceResponseCommand() 
    		{
    			ApiResponse = response,			
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
    
    


    /// <param name="productSource"></param>
    [UUID("c0febf213678445d92030ad9bf65cc8f")]
    virtual public EcomInventoryPriceInfo IAIProductInfoToEComInventoryInfo(EComIAIDriver.IAIGetProducts.resultType productSource)
    {
      var priceInfo = new EcomInventoryPriceInfo();
      try
      {  
        if(productSource?.productVat == 0)
        {
          priceInfo.RetailPrice = (decimal)productSource?.productRetailPrice;
          priceInfo.RetailPriceNet = (decimal)productSource?.productRetailPrice;
        }
        else
        {
          decimal vat = (decimal)productSource?.productVat / 100 + 1;
          decimal priceNet = (decimal)productSource?.productRetailPrice / vat;
          priceInfo.RetailPriceNet = Decimal.Round(priceNet, 2);
        }
        priceInfo.InventoryId = productSource?.productId.ToString();
       
        priceInfo.Currency = productSource?.currencyId;     
      } 
      catch(Exception ex)
      {
        throw new Exception("Błąd dodawnia danych konta do klas uniwersalnych: " + ex.Message);
      }  
     
      return priceInfo;
       
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("c1002797523241f5a2e63f622be01e58")]
    [PhysicalQueueName("ExportInventoryStockQueue")]
    virtual public HandlerResult PutProductsStocksRequestCommandHandler(ConsumeContext<ECOM.PutProductsStocksRequestCommand> context)
    {
    	EComIAIDriver.IAIUpdateProductStock.responseType response = null; 
    	try
      	{			
        	var driver = new EComIAIDriver.IAIDriver();
    		ClientData clientData = new ClientData()
    		{
    			Login = Parameters["apilogin"].AsString,
    			Password = Parameters["apipassword"].AsString,
    			Address = Parameters["udpateproductsstocksapiaddress"].AsString,
    			MaxReceivedMessageSize = Parameters["maxreceivedmessagesize"].AsInteger
    		};			
    			
    		string errorMessage;
    		
    		response = driver.UpdateProductStock(clientData, context.Message.ApiRequest, out errorMessage);
    		if(!string.IsNullOrEmpty(errorMessage))
    		{
    			throw new Exception($"Błąd wysłania stanów magazynowych: {errorMessage}");
    		}
    		else if(response == null)
    		{			
    			throw new Exception("Brak danych w odpowiedzi bramki");			
    		}
    		else if((response.errors?.faultCode ?? 0) != 0)
    		{	
    			throw new Exception($"Błąd wysłania stanów magazynowych. Kod błędu: {response.errors.faultCode}, Opis: {response.errors.faultString}");
    		}
      	}
      	catch(Exception ex)
      	{
    		//jeżeli dostaliśmy błąd API np. mamy zły adres API, to znaczy, że nie wysłano żadnych stanów mag i podbijamy
    		//błąd eksportu				   
    		
    		RunInAutonomousTransaction(
    			()=>			
    			{		
    				var invStock = new ECOM.ECOMINVENTORYSTOCKS();
    				
    				foreach(var ecomStock in context.Message.ApiRequest.@params.products)
    				{
    					invStock.FilterAndSort($"{nameof(ECOM.ECOMINVENTORYSTOCKS)}.{invStock.ECOMSTOCKID.Symbol} = '{ecomStock.stockId}' " +
    						$"AND {nameof(ECOM.ECOMINVENTORYSTOCKS)}.{invStock.WERSJAREF.Symbol} = {ecomStock.productSizeCodeExternal} " +
    						$"AND {nameof(ECOM.ECOMINVENTORYSTOCKS)}.{invStock.ECOMCHANNELREF.Symbol} = 0{context.Message.EcomChannel}");
            			if(invStock.FirstRecord())		
    					{
    						invStock.EditRecord();
    						invStock.SYNCSTATUS = (int)EcomSyncStatus.ExportError;		
    						invStock.LASTSYNCERROR = ex.Message;					
    						if(!invStock.PostRecord())
    						{
    							throw new Exception($"Błąd zapisu zmiany statusu stanu mag. towaru o wersji: {ecomStock.productSizeCodeExternal}");
    						}	  
    					}
    					else
    					{
    						throw new Exception($"Nie znaleziono stanu mag. towaru o wersji: {ecomStock.productSizeCodeExternal} w kanale: {context.Message.EcomChannel.ToString()}");
    					}		    
    				}
    				invStock.Close(); 
    						  			
    			},
    			true
    		);
    		
    		context.Message.Message = ex.Message;
    	  	throw new Exception(ex.Message);
      	}
    
    	try
    	{
    		var command = new ECOM.PutProductsStocksResponseCommand() 
    		{
    			ApiResponse = response,			
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
    /// Handler wykorzystywany w procesie pobierania zamówień z IAI. Uruchamiany w przeciążonej metodize DoImportOrdersReqest. Przyjmuje komendę GetIAIOrdersRequestCommand zawierającą dane do odfiltrowania listy zamówień w formacie gotowym do wysyłki na witrynę,
    /// a następnie uruchamia metodę sterownika IAI, który wysyła zapytanie o zamówienia do API. Jeżeli proces przebiegł bez błędów, to wysyła komendę EDA
    /// GetIAIOrdersReponseCommand.
    /// </summary>
    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("d128c5d367fb475ab9d5a9aeeae0f709")]
    virtual public HandlerResult GetOrdersRequestCommandHandler(ConsumeContext<ECOM.GetOrdersRequestCommand> context)
    {	
      	try
    	{			
    		var driver = new EComIAIDriver.IAIDriver();
    		//dane do logowania do API IAI
    		ClientData clientData = new ClientData()
    		{
    			Login = Parameters["apilogin"].AsString,
    			Password = Parameters["apipassword"].AsString,
    			Address = Parameters["apiaddress"].AsString,
    			MaxReceivedMessageSize = Parameters["maxreceivedmessagesize"].AsInteger
    		};			
    
    		EComIAIDriver.IAIOrders.responseType response = null;	
    		
    		string errorMessage;
    		//pobranie synchroniczne zamówień z IAI
    		response = driver.GetOrders(clientData, context.Message.ApiRequest, out errorMessage);		
    
    		if(!string.IsNullOrEmpty(errorMessage))
    		{
    			throw new Exception(errorMessage);
    		}
    		else if(response == null)
    		{
    			context.Message.Message = "Brak danych w odpowiedzi bramki";
    			throw new Exception(context.Message.Message);			
    		}
    		else if((response.errors?.faultCode ?? 0) != 0)
    		{
    			context.Message.Message = 
    				$"Błąd pobierania zamówień z kanału sprzedaży. Kod błędu: {response.errors.faultCode}, Opis: {response.errors.faultString}";
    			throw new Exception(context.Message.Message);
    		}
    		
    		var command = new ECOM.GetOrdersResponseCommand()    
    		{
    			ImportMode = context.Message.ImportMode,
    			EcomChannel = context.Message.EcomChannel,
    			ApiResponse = response
    		};		
    		EDA.SendCommand(context.Headers.Get<string>("NeosConnector"), command); 
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
    [UUID("e8c9321e6df24cb39ed6477153ae5241")]
    virtual public HandlerResult PutOrderStatusRequestCommandHandler(ConsumeContext<ECOM.PutOrderStatusRequestCommand> context)
    {	
      try
    	{			
    		var driver = new EComIAIDriver.IAIDriver();
    		
    		ClientData clientData = new ClientData()
    		{
    			Login = Parameters["apilogin"].AsString,
    			Password = Parameters["apipassword"].AsString,
    			Address = Parameters["updateordersapiaddress"].AsString,
    			MaxReceivedMessageSize = Parameters["maxreceivedmessagesize"].AsInteger  
    		};
    
    		EComIAIDriver.IAIUpdateOrders.responseType response = null;	
    		
    		string errorMessage;
    		response = driver.UpdateOrders(clientData, context.Message.ApiRequest, out errorMessage);
    
    		if(!string.IsNullOrEmpty(errorMessage))
    		{
    			throw new Exception($"Błąd aktualizacji zamówienia: {errorMessage}");
    		}
    		else if(response == null)
    		{			
    			throw new Exception("Brak danych w odpowiedzi bramki");			
    		}
    		else if((response.errors?.faultCode ?? 0) != 0)
    		{
    			context.Message.Message = 
    				$"Błąd aktualizacji zamówienia. Kod błędu: {response.errors.faultCode}, Opis: {response.errors.faultString}";
    			throw new Exception(context.Message.Message);
    		}
        
    		var command = new ECOM.PutOrderStatusResponseCommand()    
    		{
    			EcomChannel = context.Message.EcomChannel,
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
    [UUID("eb551636bc0e4f71ac85f526ff7aace7")]
    [PhysicalQueueName("ExportInventoryQueue")]
    virtual public HandlerResult PutProductPriceResponseCommandHandler(ConsumeContext<ECOM.PutProductPriceResponseCommand> context)
    {
      string errMsg = ""; 
      
      foreach(var productPriceResult in context.Message.ApiResponse.results.productsResults)
      {
        //aktualizacje każdego w osobnej transakcji, bo jak dostaniemy exceptiona, to i tak chcemy oznaczyć te ceny
        //które udało się wysłać 
        RunInAutonomousTransaction(
          ()=>
          {
            var invPrice = new ECOM.ECOMINVENTORYPRICES();
            try
            {
              var invPriceData = CORE.QuerySQL(
                @"select p.ref
                  from ecominventories ei
                    join ecominventoryprices p on p.ecominventoryref = ei.ref " +
                  $"where ei.ACTIVE = 1 and ei.ecomchannelref = 0{context.Message.EcomChannel.ToString()} " + 
                    $"and ei.ecominventoryid = '{productPriceResult.productId}'").FirstOrDefault();
    
              if(invPriceData == null)
              {
                throw new Exception($"Nie znaleziono ceny dla towaru o id: {productPriceResult.productId}");
              }
              invPrice.FilterAndSort($"{nameof(ECOM.ECOMINVENTORYPRICES)}.{invPrice.REF.Symbol} = 0{invPriceData["REF"]}");
              if(invPrice.FirstRecord())	
              {            
                if(productPriceResult.faultCode != 0)
                {              
                  throw new Exception("Błąd API: " + productPriceResult.faultString);   
                                
                }
                else
                {
                  invPrice.EditRecord();
                  invPrice.SYNCSTATUS = (int)EcomSyncStatus.Exported;        
                  invPrice.LASTSENDTMSTMP = DateTime.Now;
                  if(!invPrice.PostRecord())
                  {
                    throw new Exception($"Błąd zapisu zmiany statusu ceny towaru o REF: {invPriceData["REF"]}");
                  }	
                } 
              }
              else
              {
                //jak nie znaleziono rekordu, to nie aktualizujemy żadnego ECOMPRICE, no bo nie mamy której,
                //tylko zapisujemy błąd i idziemy dalej
                errMsg += $"Nie znaleziono ceny towaru o ref: {invPriceData["REF"]}\n";
              }
            }
            catch(Exception ex)
            {
              invPrice.EditRecord();
              invPrice.SYNCSTATUS = (int)EcomSyncStatus.ExportError;							
              if(!invPrice.PostRecord())
              {
                throw new Exception($"Błąd zapisu zmiany statusu ceny towaru o REF: {invPrice.ECOMINVENTORYREF}");
              }	 
              errMsg += $"{ex.Message}\n";           
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
    
    


    /// <param name="productSource"></param>
    /// <param name="productId"></param>
    [UUID("ee18f14b02e84a27a0069f97508d7014")]
    virtual public EcomInventoryStockInfo IAIProductInfoToEcomStockInfo(EComIAIDriver.IAIGetProducts.productStockQuantitiesType productSource, string productId)
    {
      var stockInfo = new EcomInventoryStockInfo();
      try
      {  
        stockInfo.InventoryId = productId;
        stockInfo.Quantity = Convert.ToDecimal(productSource?.productSizesData?.First().productSizeQuantity);  
        stockInfo.ChannelStockId = productSource?.stockId.ToString();      
      } 
      catch(Exception ex)
      {
        throw new Exception("Błąd dodawnia danych stanów magazynowych do klas uniwersalnych: " + ex.Message);
      }  
     
      return stockInfo;
       
    }
    
    


    /// <summary>
    /// Handler wykorzystywany w procesie pobierania zamówień z IAI. Przyjmuje komendę GetIAIOrdersReponseCommand zawierającą nieprzetworzoną listę zamówień pobranych ze sklepu, Dzieli ją na osobne zamówienia, każde z nich wpisuje do uniwersalnej klasy EcomOrderInfo,
    /// a następnie dla każdego zamówienia wysyła komendę EDA ImportOrderCommand, wykona import pojedynczego zamówienia
    /// </summary>
    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("ee590855d1f94466bc4ef155e3d3d63c")]
    virtual public HandlerResult GetOrdersResponseCommandHandler(ConsumeContext<ECOM.GetOrdersResponseCommand> context)
    {
    	//skoro udało się pobrać listę zamówień, to rozbijamy na osobne komendy per zamowienie
    	string errorMsg = "";	
    	foreach(var orderResult in context.Message.ApiResponse.Results)
    	{
    		EcomOrderInfo orderInfo = new EcomOrderInfo();							
    		try
    		{
    			orderInfo = IAIOrderToEcomOrderInfo(orderResult);
    			var command = new ECOM.ImportOrderCommand()    
    			{
    				Message = "Zamówienie " + orderResult.orderId + " " + errorMsg,
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
    			EDA.SendCommand(context.Headers.Get<string>("NeosConnector"), command); 				
    		}
    		catch(Exception ex)
    		{
    			errorMsg += $"Błąd wysyłki komendy przetworzenia zamówienia {orderResult.orderId}: {ex.Message}. " + "\n";
    		}									
    	}
    
    	if(!string.IsNullOrEmpty(errorMsg))
    	{
    		//rzucamy wyjątek, bo chcemy, żeby handler został oznaczony jako błędny
    		throw new Exception(errorMsg);		
    	}			
    
      	return HandlerResult.PipeSuccess;
    }
    
    


    /// <summary>
    /// Metoda przenosi dane zamówienia ze struktur IAI do uniwersalnych struktur Teneum.
    /// Wykonywane są też niezbędne konwersje na podstawie ECOMCHANNELCONVERT
    /// parametry
    /// ecomChannel - ref kanału sprzedaży
    /// orderSource - dane zamówienia w formacie IAI
    /// 
    /// orderInfo - dane zamówienia
    /// </summary>
    /// <param name="orderSource"></param>
    [UUID("f4a2bcd9c3ba43158a7a9411544f316b")]
    virtual public EcomOrderInfo IAIOrderToEcomOrderInfo(EComIAIDriver.IAIOrders.ResultType orderSource)
    {  
      var orderInfo = new EcomOrderInfo();
      DateTime tempDate = new DateTime();  
    
      try
      {    
        //dane konta klienta
        orderInfo.ClientAccount = new EcomClientAccountInfo()
        {
          Id = orderSource?.clientResult?.clientAccount?.clientId.ToString(),
          Login = orderSource?.clientResult?.clientAccount?.clientLogin,
          Phone = orderSource?.clientResult?.clientAccount?.clientPhone1, 
          ExternalCode = orderSource?.clientResult?.clientAccount?.clientCodeExternal,
          Email = orderSource?.clientResult?.clientAccount?.clientEmail
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
          Street = orderSource?.clientResult?.clientBillingAddress?.clientStreet, 
          ZipCode = orderSource?.clientResult?.clientBillingAddress?.clientZipCode,
          City = orderSource?.clientResult?.clientBillingAddress?.clientCity, 
          Province = orderSource?.clientResult?.clientBillingAddress?.clientProvince,
          CountryId = orderSource?.clientResult?.clientBillingAddress?.clientCountryId, 
          CountryName = orderSource?.clientResult?.clientBillingAddress?.clientCountryName, 
          FirmName = orderSource?.clientResult?.clientBillingAddress?.clientFirm,
          FirstName = orderSource?.clientResult?.clientBillingAddress?.clientFirstName, 
          LastName = orderSource?.clientResult?.clientBillingAddress?.clientLastName, 
          Phone = orderSource?.clientResult?.clientBillingAddress?.clientPhone1,
          Nip = orderSource?.clientResult?.clientBillingAddress?.clientNip  
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
          AddressId = orderSource?.clientResult?.clientDeliveryAddress?.clientDeliveryAddressId,      
          Street = orderSource?.clientResult?.clientDeliveryAddress?.clientDeliveryAddressStreet, 
          ZipCode = orderSource?.clientResult?.clientDeliveryAddress?.clientDeliveryAddressZipCode,
          City = orderSource?.clientResult?.clientDeliveryAddress?.clientDeliveryAddressCity, 
          Province = orderSource?.clientResult?.clientDeliveryAddress?.clientDeliveryAddressProvince, 
          CountryId = orderSource?.clientResult?.clientDeliveryAddress?.clientDeliveryAddressCountryId, 
          CountryName = orderSource?.clientResult?.clientDeliveryAddress?.clientDeliveryAddressCountry,
          FirmName = orderSource?.clientResult?.clientDeliveryAddress?.clientDeliveryAddressFirm, 
          FirstName = orderSource?.clientResult?.clientDeliveryAddress?.clientDeliveryAddressFirstName, 
          LastName = orderSource?.clientResult?.clientDeliveryAddress?.clientDeliveryAddressLastName,
          Phone = orderSource?.clientResult?.clientDeliveryAddress?.clientDeliveryAddressPhone1,
          PickupPointId = orderSource?.clientResult?.clientDeliveryAddress?.clientDeliveryAddressPickupPointInternalId.ToString()
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
          OrderStatusId = orderSource.orderDetails.orderStatus          
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
          DispatchId = orderSource.orderDetails.dispatch.courierId.ToString(),   //id sposobu dostawy z kanału sprzedaży
          DeliveryDate = 
            (DateTime.TryParse(orderSource?.orderDetails?.dispatch?.deliveryDate, out tempDate) ? 
              tempDate : null as DateTime?),
          EstimatedDeliveryDate = 
            (DateTime.TryParse(orderSource?.orderDetails?.dispatch?.estimatedDeliveryDate, out tempDate) ? 
              tempDate : null as DateTime?),
          DeliveryWeight = orderSource?.orderDetails?.dispatch?.deliveryWeight,
          DeliveryCost = Convert.ToDecimal(orderSource?.orderDetails?.payments?.orderBaseCurrency?.orderDeliveryCost),
          DeliveryVat = orderSource?.orderDetails?.payments?.orderBaseCurrency?.orderDeliveryVat.ToString()
        };
      }  
      catch(Exception ex)
      {
        throw new Exception("Błąd dodawnia sposobu odbioru do klas uniwarsalnych: " + ex.Message);
      }  
     
      try
      {
        //płatność
        orderInfo.Payment = new EcomPaymentInfo()
        {
          PaymentType = orderSource?.orderDetails?.payments?.orderPaymentType,      
          Currency = orderSource?.orderDetails?.payments.orderCurrency.currencyId, 
          OrderValue = (decimal)orderSource?.orderDetails?.payments.orderCurrency.orderProductsCost,      
          CalculateType = orderSource?.orderDetails?.payments?.orderWorthCalculateType.ToString(), 
          RebatePercent = Convert.ToDecimal(orderSource?.orderDetails?.payments?.orderRebatePercent),     
          PaymentDays = orderSource?.orderDetails?.payments?.orderPaymentDays,
          Rate = Convert.ToDecimal(orderSource?.orderDetails?.payments?.orderCurrency?.orderCurrencyValue)
        };
      } 
      catch(Exception ex)
      {
        throw new Exception("Błąd dodawnia płatności do klas uniwarsalnych: " + ex.Message);
      } 
       
      try
      {
        //dane nagłówka zamówienia
        orderInfo.OrderId = orderSource?.orderSerialNumber.ToString();      
        orderInfo.OrderSymbol = orderSource?.orderId; 
        orderInfo.OrderBridgeNote = orderSource?.orderBridgeNote; 
        orderInfo.OrderConfirmation = orderSource?.orderDetails?.orderConfirmation;           
        orderInfo.OrderAddDate = 
          (DateTime.TryParse(orderSource?.orderDetails?.orderAddDate, out tempDate) ? tempDate : null as DateTime?);
        orderInfo.OrderDispatchDate = 
          (DateTime.TryParse(orderSource?.orderDetails?.orderDispatchDate, out tempDate) ? tempDate : null as DateTime?);
        orderInfo.NoteToCourier = orderSource?.orderDetails?.clientNoteToCourier; 
        orderInfo.NoteToOrder = orderSource?.orderDetails?.clientNoteToOrder;
        orderInfo.IsOrderCancelled = orderSource?.orderDetails?.orderStatus == "canceled";
        orderInfo.StockId = orderSource?.orderDetails?.stockId.ToString();
      }  
      catch(Exception ex)
      {
        throw new Exception("Błąd dodawania nagłówka zamówienia do klas uniwarsalnych: " + ex.Message);
      }  
    
      try
      {
         //pozycje zamówienia
        if((orderSource?.orderDetails?.productsResults?.Length ?? 0) > 0)
        {
          orderInfo.OrderLines = new List<EcomOrderLineInfo>();  
          foreach(var line in orderSource?.orderDetails?.productsResults)
          {
            
            EcomOrderLineInfo orderLine = new EcomOrderLineInfo()
            {
              ProductId = line?.productId.ToString(),
              ProductName =line?.productName,
              ProductCode = line?.productCode,
              VersionName = line?.versionName,        
              StockId = line?.stockId.ToString(),
              ProductSerialNumber = line?.productSerialNumber,
              ProductQuantity = Convert.ToDecimal(line?.productQuantity),
              ProductWeight =  Convert.ToDecimal(line?.productWeight),
              ProductVat = line?.productVat.ToString(),
              ProductVatFree = line?.productVatFree,        
              ProductOrderPrice = Convert.ToDecimal(line?.productOrderPrice), 
              ProductOrderPriceNet = Convert.ToDecimal(line?.productOrderPriceNet), 
              RemarksToProduct = line?.remarksToProduct,        
              Position = line?.basketPosition
            };
            
            
            orderInfo.OrderLines.Add(orderLine);
          }    
        }
      }  
      catch(Exception ex)
      {
        throw new Exception("Błąd dodawania pozycji zamówienia do klas uniwarsalnych: " + ex.Message);
      }
    
      try
      {
        //przedplaty dla zamówienia
        if((orderSource?.orderDetails?.prepaids?.Length ?? 0) > 0)
        {
          orderInfo.Prepaids = new List<EcomPrepaidInfo>();  
          foreach(var p in orderSource?.orderDetails?.prepaids)
          {        
            EcomPrepaidInfo prepaid = new EcomPrepaidInfo()
            {
              PrepaidId = p.prepaidId.ToString(),
              Currency = p.currencyId,
              Value = Convert.ToDecimal(p.paymentValue),
              PaymentDate = 
                (DateTime.TryParse(p.paymentAddDate, out tempDate) ? tempDate : null as DateTime?),         
              PaymentType = p.payformId.ToString()     
            };        
            
            orderInfo.Prepaids.Add(prepaid);
          }    
        }
      }  
      catch(Exception ex)
      {
        throw new Exception("Błąd dodawania danych o przedpłatach do klas uniwarsalnych: " + ex.Message);
      }  
    
      //punkt odbioru    
      try
      {
        if((orderSource?.clientResult?.clientPickupPointAddress?.pickupPointId?.Length ?? 0) > 0)
        {
          orderInfo.PickupPointAddress = new EcomAddressInfo()
          {
            PickupPointId = orderSource?.clientResult?.clientPickupPointAddress?.pickupPointId,
            City = orderSource?.clientResult?.clientPickupPointAddress?.city,
            Street = orderSource?.clientResult?.clientPickupPointAddress?.street,
            ZipCode = orderSource?.clientResult?.clientPickupPointAddress?.zipCode,
            Phone = orderSource?.clientResult?.clientAccount?.clientPhone1,
            Email = orderSource?.clientResult?.clientAccount?.clientEmail
          }; 
        }  
    
      } 
      catch(Exception ex)
      {
        throw new Exception("Błąd dodawania danych o punkcie odbioru do klas uniwarsalnych: " + ex.Message);
      } 
    
      return orderInfo;
    }
    
    


    /// <summary>
    /// Metoda do przeciążenia w adapterze kanału sprzedaży.
    /// Metoda weryfikuje, czy drajwer fizyczny obsługuje taką konfigurację parametrów, buduje i wywołuje fizczne zapytanie w drajwerze, np. w drajwerze IAI, otrzymuje odpowiedź, zwraca komunikat o błędzie, jeśi jest poprawne, to strukturę Response pakuje do osobnej komendy implementowanej już na poziomie drajwera realnego (w obiekcie dzieczącym) np. GetOrderResponse 
    /// </summary>
    /// <param name="context"></param>
    [UUID("5e21560c9ae8407cbb3b316d39c6d9c6")]
    override public string DoImportOrdersReqest(ConsumeContext<ECOM.ImportOrdersRequestCommand> context)
    {
      	DateTime? dateFrom = null;
    	DateTime? dateTo = null;
    	List<int> orderList = null;
      	string errMsg = ""; 		
    	
    	try
    	{		
    		importMode importMode = context.Message.ImportMode;
    
    		//ustawiamy daty przekazywane do requesta w zależności od trybu
    		switch(importMode)
    		{
    			//od ostatniego importu, czyli ustawiamy tylko datę poczatkową
    			case importMode.SinceLastImport:												
    				dateFrom = context.Message.OrderDateFrom; 
    				dateTo = null;
    				break;
    			//zakres dat czyli ustawiamy początek i koniec
    			case importMode.DateRange:
    				dateFrom = context.Message.OrderDateFrom;
    				dateTo = context.Message.OrderDateTo;
    				break;
    			//lista id zamówień lub pojedyncze id podane przez operatora  
    			case importMode.Selected:
    			case importMode.OrderId:								
    				orderList = context.Message.OrderList.Select(int.Parse).ToList();
    				break;
    			//pobieranie wszystkich zamówień - ustawiamy datę na rok 1900, ponieważ IAI wymaga użycia chociaż jednego filtra 
    			case importMode.All:
    				dateFrom = new DateTime(1900, 01, 01, 00, 00, 00);  
    				dateTo = null;
    				break;
    			default:
    				throw new Exception("Nieobsłużony tryb pobierania zamówień");
    				break;
    		}
    		
    		var request = new EComIAIDriver.IAIOrders.requestType();
    		request.@params = new EComIAIDriver.IAIOrders.paramsType();
    		
    		//tu jest cały kod obsługujący fitry zamówień do pobrania w requescie do API
    		switch(importMode)
    		{   //ustalenie zakresu dat
    			case importMode.DateRange:
    			case importMode.SinceLastImport:
    			case importMode.All:
    				#region zakres dat
    				request.@params.ordersRange = new EComIAIDriver.IAIOrders.ordersRangeType();		
    				request.@params.ordersRange.ordersDateRange = new EComIAIDriver.IAIOrders.ordersDateRangeType();
    				request.@params.ordersRange.ordersDateRange.ordersDatesTypes = 
    					new EComIAIDriver.IAIOrders.ordersDatesTypeType[] 
    					{ 
    						EComIAIDriver.IAIOrders.ordersDatesTypeType.add, 
    						EComIAIDriver.IAIOrders.ordersDatesTypeType.modified 
    					};
    				request.@params.ordersRange.ordersDateRange.ordersDateTypeSpecified = true;
    				if(dateFrom != null)
    				{			
    					request.@params.ordersRange.ordersDateRange.ordersDateBegin = (dateFrom ?? DateTime.Now).ToString("yyyy-MM-dd HH:mm:ss");
    				}
    				if(dateTo != null)
    				{
    					request.@params.ordersRange.ordersDateRange.ordersDateEnd = (dateTo ?? DateTime.Now).ToString("yyyy-MM-dd HH:mm:ss"); 
    				}
    				#endregion zakres dat
    				break;
    			case importMode.Selected:
    			case importMode.OrderId:	
    				request.@params.ordersSerialNumbers = orderList.ToArray();
    				break;
    			default:
    				throw new Exception("Nieobsłużony tryb pobierania zamówień");
    				break;		
    		}
    		//ustawienie kanału sprzedaży w requescie - zbieramy zamówienia tylko ze wskazanego kanału sprzedazy - nazwa kanału sprzedaży
    		var ecomCahnnelData =  new ECOM.ECOMCHANNELS();
    		ecomCahnnelData.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomCahnnelData.REF.Symbol} = 0{context.Message.EcomChannel}");
            if(ecomCahnnelData.FirstRecord())
    		{
    			if(string.IsNullOrEmpty(ecomCahnnelData.SYMBOL))
    			{
    				throw new Exception("Nie znaleziono symbolu zewnętrznego dla kanału sprzedaży");
    			}
    			else
    			{
    				request.@params.orderSource = new EComIAIDriver.IAIOrders.orderSourceType();
    				request.@params.orderSource.shopsIds = new int[]{ecomCahnnelData.SYMBOL.AsInteger};
    			}		
    			
    			var command = new ECOM.GetOrdersRequestCommand()    
    			{ 
    				EcomChannel = context.Message.EcomChannel,
    				ApiRequest = request
    			};
    			EDA.SendCommand(context.Headers.Get<string>("NeosConnector"), command, context); 
    		}
    		else
    		{
    			throw new Exception("Nie znaleziono kanału sprzedaży o REF: " + ecomCahnnelData.REF);
    		}
    		ecomCahnnelData.Close();
    		
    	}
    	catch(Exception ex)
    	{
    		errMsg = ex.Message;
    	}		
        return errMsg;
    
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("00511c7a061b4577991daceadf2541b1")]
    [PhysicalQueueName("ExportInventoryStockQueue")]
    override public HandlerResult ExportInventoryStocksCommandHandler(ConsumeContext<ECOM.ExportInventoryStocksCommand> context)
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
    
    


    /// <summary>
    /// wywoluje metodę abstrakcyjna DoExportInventory, która konwertuje dane do wysłania ze struktu uniwersalnych do stryktur witryny kanału sprzedaży np. IAI, realizuję wymianę danych i aktualizuje styatus synchronizacji w kanale sprzedaży.
    /// </summary>
    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("1a5b30bd9e784c7a9a9848df5e161dc0")]
    [PhysicalQueueName("ExportInventoryQueue")]
    override public HandlerResult ExportInventoryCommandHandler(ConsumeContext<ECOM.ExportInventoryCommand> context)
    {
      return base.ExportInventoryCommandHandler(context);
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("1e9f51019a114fc382e9bac8a4bd0537")]
    [PhysicalQueueName("ExportInventoryQueue")]
    override public HandlerResult ExportInventoryPricesCommandHandler(ConsumeContext<ECOM.ExportInventoryPricesCommand> context)
    {
      return base.ExportInventoryPricesCommandHandler(context);
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("2451e7afcb964a77a9dcddfd389b23ee")]
    override public HandlerResult GetOrdersListCommandHandler(ConsumeContext<ECOM.GetOrdersListCommand> context)
    {
      var result = DoGetOrdersList(context);
      context.SetResult("Result", result);
      return HandlerResult.Handled;
    }   
    
    


    /// <param name="context"></param>
    [UUID("2e3bdebe568741d194d7321727d6939f")]
    override public List<EcomInventoryStockInfo> DoGetStocksList(ConsumeContext<ECOM.GetStocksListCommand> context)
    {
    	string errMsg = "";
    	 
    	EComIAIDriver.IAIGetProducts.responseType response = null;
    	var stockInfoList = new List<EcomInventoryStockInfo>();			
    	
    	try
    	{	
    		var productsToGetList = new List<EComIAIDriver.IAIGetProducts.productIndexItemType>();	
    		var request = new EComIAIDriver.IAIGetProducts.requestType();
    		request.@params = new EComIAIDriver.IAIGetProducts.paramsType();
    
    		// dodanie do listy wszystkich id produktów z danego kanału 
    		foreach(var item in context.Message.InventoryIdList)
    		{
    			var product = new EComIAIDriver.IAIGetProducts.productIndexItemType();
    			product.productIndex = item;
    			productsToGetList.Add(product);	
    		}
    		//ustawienie kanału sprzedaży w requescie - zbieramy produkty tylko ze wskazanego kanału sprzedazy - nazwa kanału sprzedaży
    		var ecomCahnnelData =  new ECOM.ECOMCHANNELS();
    		ecomCahnnelData.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomCahnnelData.REF.Symbol} = 0{context.Message.EcomChannel}");
            if(ecomCahnnelData.FirstRecord())
    		{
    
    			if(string.IsNullOrEmpty(ecomCahnnelData.SYMBOL))
    			{
    				throw new Exception("Nie znaleziono symbolu zewnętrznego dla kanału sprzedaży");
    			}
    			else
    			{
    			request.@params.productIndexes = productsToGetList.ToArray(); 
    			}		
    		
    				var driver = new EComIAIDriver.IAIDriver();	
    				//dane do logowania do API IAI
    				ClientData clientData = new ClientData()
    				{
    					Login = Parameters["apilogin"].AsString,
    					Password = Parameters["apipassword"].AsString,
    					Address = Parameters["getproductsapiaddress"].AsString,
    					MaxReceivedMessageSize = Parameters["maxreceivedmessagesize"].AsInteger   
    				};			
    				
    				string errorMessage;							
    				response = driver.GetProducts(clientData, request, out errorMessage);		
    
    				if(!string.IsNullOrEmpty(errorMessage))
    				{
    					throw new Exception(errorMessage);
    				}
    				else if(response == null)
    				{
    					throw new Exception("Brak danych w odpowiedzi bramki");			
    				}
    				else if((response.errors?.faultCode ?? 0) != 0)
    				{
    					errMsg = $"Błąd pobierania produktów z kanału sprzedaży. Kod błędu: {response.errors.faultCode}, Opis: {response.errors.faultString}";
    					throw new Exception(errMsg);
    				}	
    				
    			//skoro udało się pobrać listę produktów, to rozbijamy na osobne komendy per produkt
    				
    			
    			foreach(var productResult in response.results)
    			{
    				EcomInventoryStockInfo stockInfo = new EcomInventoryStockInfo();
    				try
    				{
    					if((productResult?.productStocksData?.productStocksQuantities?.Count() ?? 0) > 0)
    					{
    						foreach(var item in productResult?.productStocksData?.productStocksQuantities)
    						{
    							stockInfo = IAIProductInfoToEcomStockInfo(item, productResult.productId.ToString());
    							stockInfoList.Add(stockInfo);	
    						}
    					}			
    				}
    				catch(Exception ex)
    				{
    					errMsg = $"Błąd wysyłki komendy pobrania towaru {productResult.productId}: {ex.Message}.\n";
    					throw new Exception(errMsg);
    				}						
    			}				
    		}
    		else
    		{
    			throw new Exception("Nie znaleziono kanału sprzedaży o REF: " + ecomCahnnelData.REF);
    		}
    		ecomCahnnelData.Close();
    	}
    	catch(Exception ex)
    	{
    		throw new Exception(ex.Message);
    	}
      	return stockInfoList;
    }
    
    
    


    /// <param name="context"></param>
    [UUID("60f446bf9af74d5397a6c4d36d7dd07b")]
    override public List<EcomOrderInfo> DoGetOrdersList(ConsumeContext<ECOM.GetOrdersListCommand> context)
    {
        DateTime? dateFrom = null;
    	DateTime? dateTo = null;
    	List<int> orderList = null;
      	string errMsg = ""; 
    	EComIAIDriver.IAIOrders.responseType response = null;
    	var orderInfoList = new List<EcomOrderInfo>();			
    	
    	try
    	{		
    		// tu wystarczy importmode.datarange
    		importMode importMode = context.Message.ImportMode;
    		switch(importMode)
    		{
    			//zakres dat czyli ustawiamy początek i koniec
    			case importMode.DateRange:
    				dateFrom = context.Message.OrderDateFrom;
    				dateTo = context.Message.OrderDateTo;
    				break;
    			default:
    				throw new Exception("Nieobsłużony tryb pobierania zamówień");
    				break;
    		}
    		
    		var request = new EComIAIDriver.IAIOrders.requestType();
    		request.@params = new EComIAIDriver.IAIOrders.paramsType();
    		
    		//tu jest cały kod obsługujący fitry zamówień do pobrania w requescie do API
    		switch(importMode)
    		{   //ustalenie zakresu dat
    			case importMode.DateRange:
    				#region zakres dat
    				request.@params.ordersRange = new EComIAIDriver.IAIOrders.ordersRangeType();		
    				request.@params.ordersRange.ordersDateRange = new EComIAIDriver.IAIOrders.ordersDateRangeType();
    				request.@params.ordersRange.ordersDateRange.ordersDatesTypes = 
    					new EComIAIDriver.IAIOrders.ordersDatesTypeType[] 
    					{ 
    						EComIAIDriver.IAIOrders.ordersDatesTypeType.add,
    						EComIAIDriver.IAIOrders.ordersDatesTypeType.modified 
    					};
    				request.@params.ordersRange.ordersDateRange.ordersDateTypeSpecified = true;
    				if(dateFrom != null)
    				{			
    					request.@params.ordersRange.ordersDateRange.ordersDateBegin = (dateFrom ?? DateTime.Now).ToString("yyyy-MM-dd HH:mm:ss");
    				}
    				if(dateTo != null)
    				{
    					request.@params.ordersRange.ordersDateRange.ordersDateEnd = (dateTo ?? DateTime.Now).ToString("yyyy-MM-dd HH:mm:ss"); 
    				}
    				#endregion zakres dat
    				break;
    			default:
    				throw new Exception("Nieobsłużony tryb pobierania zamówień");
    				break;		
    		}
    		//ustawienie kanału sprzedaży w requescie - zbieramy zamówienia tylko ze wskazanego kanału sprzedazy - nazwa kanału sprzedaży
    		
    		var ecomCahnnelData =  new ECOM.ECOMCHANNELS();
    		ecomCahnnelData.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomCahnnelData.REF.Symbol} = 0{context.Message.EcomChannel}");
            if(ecomCahnnelData.FirstRecord())
    		{
    			if(string.IsNullOrEmpty(ecomCahnnelData.SYMBOL))
    			{
    				throw new Exception("Nie znaleziono symbolu zewnętrznego dla kanału sprzedaży");
    			}
    			else
    			{
    				request.@params.orderSource = new EComIAIDriver.IAIOrders.orderSourceType();
    				request.@params.orderSource.shopsIds = new int[]{ecomCahnnelData.SYMBOL.AsInteger};
    			}		
    						
    			var driver = new EComIAIDriver.IAIDriver();
    			//dane do logowania do API IAI
    			ClientData clientData = new ClientData()
    			{
    				Login = Parameters["apilogin"].AsString,
    				Password = Parameters["apipassword"].AsString,
    				Address = Parameters["apiaddress"].AsString,
    				MaxReceivedMessageSize = Parameters["maxreceivedmessagesize"].AsInteger
    			};			
    			
    			string errorMessage;
    			//pobranie synchroniczne zamówień z IAI								
    			response = driver.GetOrders(clientData, request, out errorMessage);		
    
    			if(!string.IsNullOrEmpty(errorMessage))
    			{
    				throw new Exception(errorMessage);
    			}
    			else if(response == null)
    			{
    				throw new Exception("Brak danych w odpowiedzi bramki");			
    			}
    			else if((response.errors?.faultCode ?? 0) != 0)
    			{
    				errMsg = $"Błąd pobierania zamówień z kanału sprzedaży. Kod błędu: {response.errors.faultCode}, Opis: {response.errors.faultString}";
    				throw new Exception(errMsg);
    			}
    						
    			//skoro udało się pobrać listę zamówień, to rozbijamy na osobne komendy per zamowienie
    										
    			foreach(var orderResult in response.Results)
    			{
    				EcomOrderInfo orderInfo = new EcomOrderInfo();					
    				try
    				{
    					orderInfo = IAIOrderToEcomOrderInfo(orderResult);
    					//Suma zamówienia produkty + koszt dostawy
    					orderInfo.Payment.OrderValue = orderInfo.Payment.OrderValue + orderInfo.Dispatch.DeliveryCost;
    					orderInfoList.Add(orderInfo);				
    				}
    				catch(Exception ex)
    				{
    					errMsg = $"Błąd wysyłki komendy przetworzenia zamówienia {orderResult.orderId}: {ex.Message}. " + "\n";
    					throw new Exception(errMsg);
    				}						
    			}				
    		}
    		else
    		{
    			throw new Exception($"Nie znaleziono kanału sprzedaży o REF: {ecomCahnnelData.REF}.");
    		}
    		ecomCahnnelData.Close();		
    	}
    	catch(Exception ex)
    	{
    		throw new Exception(ex.Message);
    	}
      	return orderInfoList;
    }
    
    


    /// <param name="context"></param>
    [UUID("6b882c3957e14988952696de7627b71c")]
    override public List<EcomInventoryPriceInfo> DoGetPricesList(ConsumeContext<ECOM.GetPricesListCommand> context)
    {
    	string errMsg = "";
    	 
    	EComIAIDriver.IAIGetProducts.responseType response = null;
    	var priceInfoList = new List<EcomInventoryPriceInfo>();			
    	
    	try
    	{	
    		var productsToGetList = new List<EComIAIDriver.IAIGetProducts.productIndexItemType>();	
    		var request = new EComIAIDriver.IAIGetProducts.requestType();
    		request.@params = new EComIAIDriver.IAIGetProducts.paramsType();
    
    		// dodanie do listy wszystkich id produktów z danego kanału 
    		foreach(var item in context.Message.InventoryIdList)
    		{
    			var product = new EComIAIDriver.IAIGetProducts.productIndexItemType();
    			product.productIndex = item;
    			productsToGetList.Add(product);	
    		}		
    		//ustawienie kanału sprzedaży w requescie - zbieramy produkty tylko ze wskazanego kanału sprzedazy - nazwa kanału sprzedaży
    		var ecomCahnnelData =  new ECOM.ECOMCHANNELS();
    		ecomCahnnelData.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomCahnnelData.REF.Symbol} = 0{context.Message.EcomChannel}");
            if(ecomCahnnelData.FirstRecord())
    		{
    			
    			if(string.IsNullOrEmpty(ecomCahnnelData.SYMBOL))
    			{
    				throw new Exception("Nie znaleziono symbolu zewnętrznego dla kanału sprzedaży");
    			}
    			else
    			{
    			request.@params.productIndexes = productsToGetList.ToArray(); 
    			}		
    						
    			var driver = new EComIAIDriver.IAIDriver();
    			//dane do logowania do API IAI
    			ClientData clientData = new ClientData()
    			{
    				Login = Parameters["apilogin"].AsString,
    				Password = Parameters["apipassword"].AsString,
    				Address = Parameters["getproductsapiaddress"].AsString,
    				MaxReceivedMessageSize = Parameters["maxreceivedmessagesize"].AsInteger   
    			};			
    				
    			string errorMessage;							
    			response = driver.GetProducts(clientData, request, out errorMessage);		
    
    			if(!string.IsNullOrEmpty(errorMessage))
    			{
    				throw new Exception(errorMessage);
    			}
    			else if(response == null)
    			{
    				throw new Exception("Brak danych w odpowiedzi bramki");			
    			}
    			else if((response.errors?.faultCode ?? 0) != 0)
    			{
    				errMsg = $"Błąd pobierania produktów z kanału sprzedaży. Kod błędu: {response.errors.faultCode}, Opis: {response.errors.faultString}";
    				throw new Exception(errMsg);
    			}
    			
    					
    			//skoro udało się pobrać listę produktów, to rozbijamy na osobne komendy per produkt
    										
    			foreach(var productResult in response.results)
    			{
    				EcomInventoryPriceInfo priceInfo = new EcomInventoryPriceInfo();
    				try
    				{
    					priceInfo = IAIProductInfoToEComInventoryInfo(productResult);
    					priceInfoList.Add(priceInfo);				
    				}
    				catch(Exception ex)
    				{
    					errMsg = $"Błąd wysyłki komendy pobrania towaru {productResult.productId}: {ex.Message}.\n";
    					throw new Exception(errMsg);
    				}						
    			}		
    			
    				
    		}
    		else
    		{
    			throw new Exception("Nie znaleziono kanału sprzedaży o REF: " + ecomCahnnelData.REF);
    		}
    		ecomCahnnelData.Close();	
    	}
    	catch(Exception ex)
    	{
    		throw new Exception(ex.Message);
    	}	
    	
      	return priceInfoList;
    }
    
    


    /// <summary>
    /// Abstrakcyjny handler komendy ImportOrdersRequestCommand będącej częścią mechanizmu importu zamówień.
    /// Handler realizuje zapytanie witryny sprzedaży o listę zamówień o podanych parametrach. W adapterze rzeyczywistym metoda przekształca paramtry uniwersalne zapytania i wysła komendę (sama do siebie) juz fizyczną realizującą zapytnaie o zamówienai z drajwera.
    /// </summary>
    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("6b88f5985551453aabda870d22fbaffc")]
    override public HandlerResult ImportOrdersRequestCommandHandler(ConsumeContext<ECOM.ImportOrdersRequestCommand> context)
    {
      return base.ImportOrdersRequestCommandHandler(context);
    }
    
    


    /// <summary>
    /// wywoluje metodę abstrakcyjna DoExportOrderStatus, która konwertuje dane do wysłania ze struktu uniwersalnych do struktur witryny kanału sprzedaży np. IAI, realizuję wymianę danych i aktualizuje styatus synchronizacji w kanale sprzedaży.
    /// </summary>
    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("857981a820bc443aa6a5ead947b14336")]
    override public HandlerResult ExportOrdersStatusCommandHandler(ConsumeContext<ECOM.ExportOrdersStatusCommand> context)
    {
      return base.ExportOrdersStatusCommandHandler(context);
    }
    
    


    /// <param name="context"></param>
    [UUID("8c74b36fd2f64f388de3e0e85039239f")]
    override public string DoExportInventoryPrices(ConsumeContext<ECOM.ExportInventoryPricesCommand> context)
    {  
      string errMsg = "";    
     
      //wysyłamy ceny w jednym zapytaniu do API 
      var invPrice = new ECOM.ECOMINVENTORYPRICES();
    
      //w IAI można wysyłać cenę w tylko jednej walucie na raz więc sprawdzamy, 
      //czy w danych do wysłania nie ma kilku wpisów dla tego samego towaru w różnych walutach
      //grupujemy ceny po towarach i sprawdzamy, czy którys nie ma podanych cen w kilku walutach
      var pricesGrouppedByInventory = 
        from priceInfo in context.Message.InventoryPricesInfoList
        group priceInfo by priceInfo.InventoryId into pricesGroup
        select pricesGroup;
    
      foreach(var pricesGroup in pricesGrouppedByInventory)
      {
        if(pricesGroup.Count() > 1)
        {
          errMsg += $"Nastąpiła próba wysyłki cen w wielu walutach dla towaru o id: {pricesGroup.Key}\n"; 
        }   
      }     
    
      //jeżeli mamy błędy już na tym etapie np. wystąpił wpis o wielu cenach dla jednego towaru 
      //to przerywamy eksport
      if(!string.IsNullOrEmpty(errMsg))
      {
        return errMsg;    
      }
    
      var request = new EComIAIDriver.IAIUpdateProducts.requestType();
      request.@params = new EComIAIDriver.IAIUpdateProducts.paramsType();
    
      var productsToSendList = new List<EComIAIDriver.IAIUpdateProducts.productType>();
     
      foreach(var inventoryPriceInfo in context.Message.InventoryPricesInfoList)
      {        
        try
        {     
          invPrice.FilterAndSort($"{nameof(ECOM.ECOMINVENTORYPRICES)}.{invPrice.REF.Symbol} = 0{inventoryPriceInfo.EcomInventoryPriceRef}");
          if(!invPrice.FirstRecord())
          {
            errMsg += $"Nie znaleziono ceny o ref: {inventoryPriceInfo.EcomInventoryPriceRef.ToString()} w kanale: {context.Message.EcomChannel.ToString()}\n";            
            continue;
          }
               
          var product = new EComIAIDriver.IAIUpdateProducts.productType();
          if(!string.IsNullOrEmpty(inventoryPriceInfo.InventoryId))
          {        
            product.productId = Convert.ToInt32(inventoryPriceInfo.InventoryId);
            product.productIdSpecified = true;
          }
          else
          {
            //nie dodajemy do listy aktualizacji towarów, których jeszcze nie ma na witrynie, żeby nie doprowadzić do sytuacji
            //kiedy w IAI doda się towar, który ma tylko cenę (choć pewnie i tak IAI to zablokuje)         
            continue;
          }     
          
          product.productRetailPriceNet = Decimal.ToSingle(inventoryPriceInfo?.RetailPriceNet ?? 0m);
          product.productRetailPriceNetSpecified = product?.productRetailPriceNet > 0;
          product.productRetailPrice = Decimal.ToSingle(inventoryPriceInfo?.RetailPrice ?? 0m);
          product.productRetailPriceSpecified = product?.productRetailPrice > 0;
          product.currencyId = inventoryPriceInfo.Currency;
    
          productsToSendList.Add(product);
          RunInAutonomousTransaction(
            ()=>
            {
              invPrice.EditRecord();            
              invPrice.SYNCSTATUS = (int)EcomSyncStatus.ExportProceeding;         
              if(!invPrice.PostRecord())
              {
                throw new Exception($"Błąd zapisu zmiany statusu eksportu ceny o REF: {inventoryPriceInfo.EcomInventoryPriceRef}");
              }           
            },
            true
          );                   
        }
        catch(Exception ex)
        {
          errMsg += $"Błąd przy generowaniu danych do wysłania cen na witrynę dla towaru o id: {inventoryPriceInfo.InventoryId} " +
            $"w kanale sprzedaży {context.Message.EcomChannel}: {ex.Message}\n";       
          
          RunInAutonomousTransaction(
            ()=>
            {  
              invPrice.EditRecord();       
              invPrice.SYNCSTATUS = (int)EcomSyncStatus.ExportError;          
              if(!invPrice.PostRecord())
              {
                throw new Exception($"Błąd zapisu zmiany statusu eksportu ceny o REF: {inventoryPriceInfo.EcomInventoryPriceRef}");
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
        request.@params.products = productsToSendList.ToArray();
    
        var command = new ECOM.PutProductPriceRequestCommand()    
        {	
          EcomChannel = context.Message.EcomChannel,
          ApiRequest = request    
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
      foreach(var inventoryInfo in context.Message.InventoryInfoList)
      {
        try
        {
          inv.FilterAndSort($"{nameof(ECOM.ECOMINVENTORIES)}.{inv.REF.Symbol} = 0{inventoryInfo.EcomInventoryRef}");
          if(!inv.FirstRecord())
          {   
            throw new Exception($"Nie znaleziono towaru o wersji: {inventoryInfo.WersjaRef} w kanale: {context.Message.EcomChannel}\n"); 
          }
     
          var request = new EComIAIDriver.IAIUpdateProducts.requestType();
          request.@params = new EComIAIDriver.IAIUpdateProducts.paramsType();
          request.@params.settings = new EComIAIDriver.IAIUpdateProducts.settingsType();
          request.@params.settings.settingAddingSizeAllowed = EComIAIDriver.IAIUpdateProducts.settingAddingSizeAllowedType.y;
          request.@params.settings.settingAddingSizeAllowedSpecified = true;
          request.@params.picturesSettings = new EComIAIDriver.IAIUpdateProducts.picturesSettingsType();
          request.@params.picturesSettings.picturesSettingDeleteProductIcons 
            = EComIAIDriver.IAIUpdateProducts.picturesSettingDeleteProductIconsType.y;
          request.@params.picturesSettings.picturesSettingDeleteProductIconsSpecified = true;
          request.@params.picturesSettings.picturesSettingDeleteProductPictures = EComIAIDriver.IAIUpdateProducts.picturesSettingDeleteProductPicturesType.y;
          request.@params.picturesSettings.picturesSettingDeleteProductPicturesSpecified = true;
          request.@params.products = new EComIAIDriver.IAIUpdateProducts.productType[1];    
          request.@params.products[0] = new EComIAIDriver.IAIUpdateProducts.productType();
          if(!string.IsNullOrEmpty(inventoryInfo.InventoryId))
          {
            request.@params.products[0].productId = Convert.ToInt32(inventoryInfo.InventoryId);
          }
          request.@params.products[0].productIdSpecified = 
            !string.IsNullOrEmpty(inventoryInfo.InventoryId);       
        
          request.@params.products[0].productSizeCodeExternal = inventoryInfo.WersjaRef.ToString();
          request.@params.products[0].shopsMaskSpecified = true;
          request.@params.products[0].shopsMask = (int)System.Math.Pow(2, Int32.Parse(context.Message.EcomChannelSymbol) - 1) ;
    
          // ustawienie rozmiaru produktu - wymagane do późniejszej wysyłki stanów
          // wysyłamy stan magazynowy dla danego rozmiaru produktu 
          request.@params.products[0].productSizes = new EComIAIDriver.IAIUpdateProducts.productSizeType[1];
          request.@params.products[0].productSizes[0] = new EComIAIDriver.IAIUpdateProducts.productSizeType();
          request.@params.products[0].productSizes[0].sizePanelName = "uniw";
          request.@params.products[0].productSizes[0].productSizeCodeExternal = inventoryInfo.WersjaRef.ToString();
    
    
    
    
          // ustawienie widoczności towaru na witrynie:
          if(inventoryInfo?.IsVisible != null)
          {
            request.@params.products[0].productInVisible = inventoryInfo.IsVisible == 0 ? "n" : "y";
            request.@params.products[0].productShopsAttributes = new EComIAIDriver.IAIUpdateProducts.productShopsAttributeType[1];
            request.@params.products[0].productShopsAttributes[0] = new EComIAIDriver.IAIUpdateProducts.productShopsAttributeType();
            request.@params.products[0].productShopsAttributes[0].shopId = Int32.Parse(context.Message.EcomChannelSymbol);
            request.@params.products[0].productShopsAttributes[0].shopIdSpecified = true;
            request.@params.products[0].productShopsAttributes[0].shopInVisible = 
              inventoryInfo.IsVisible == 1 ? 
                EComIAIDriver.IAIUpdateProducts.shopInVisibleType.y : 
                EComIAIDriver.IAIUpdateProducts.shopInVisibleType.n;
            request.@params.products[0].productShopsAttributes[0].shopInVisibleSpecified = true;
          }
    
          request.@params.products[0].productRetailPriceNet = Decimal.ToSingle(inventoryInfo?.RetailPriceNet ?? 0m);
          request.@params.products[0].productRetailPriceNetSpecified = request.@params.products[0]?.productRetailPriceNet > 0;
          request.@params.products[0].productRetailPrice = Decimal.ToSingle(inventoryInfo?.RetailPrice ?? 0m);
          request.@params.products[0].productRetailPriceSpecified = request.@params.products[0]?.productRetailPrice > 0;
    
          // żeby ustawić produkt zwolniony z podatku należy:
          // parametr productVat ustawić na zero 
          // parametr productVatFree ustawić na "y"
          if(!string.IsNullOrEmpty(inventoryInfo?.Vat))
          {
            request.@params.products[0].productVat = float.Parse(inventoryInfo?.Vat);
            request.@params.products[0].productVatSpecified = request.@params.products[0]?.productVat >= 0;
            request.@params.products[0].productVatFree = (inventoryInfo?.VatFree ?? false) ? "y" : "n";
          }
          var baseUnit = inventoryInfo?.Units?.Where(u => u.IsBaseUnit == true).FirstOrDefault();
          
          if(baseUnit != null)
          {
            request.@params.products[0].unitId = Convert.ToInt32(baseUnit?.UnitId);
            request.@params.products[0].unitIdSpecified = true;  
          }      
    
          if((inventoryInfo?.InventoryNames?.Count ?? 0) > 0)
          {
            request.@params.products[0].productNames = new EComIAIDriver.IAIUpdateProducts.productNamesType();
            request.@params.products[0].productNames.productNamesLangData = 
            new EComIAIDriver.IAIUpdateProducts.productNameLangDataType[inventoryInfo.InventoryNames.Count];
            int i = 0;
            //nazw może być kilka np. w różnych językach 
            foreach(var name in inventoryInfo.InventoryNames)
            {
              request.@params.products[0].productNames.productNamesLangData[i] 
                = new EComIAIDriver.IAIUpdateProducts.productNameLangDataType()
              {
                langId = name.Language,
                productName = name.Text
              };          
              i++;
            }     
          }
    
          //opisy
          if((inventoryInfo?.InventoryDescriptions?.Count ?? 0) > 0)
          {
            request.@params.products[0].productLongDescriptions = new EComIAIDriver.IAIUpdateProducts.productLongDescriptionsType();
            request.@params.products[0].productLongDescriptions.productLongDescriptionsLangData = 
            new EComIAIDriver.IAIUpdateProducts.productLongDescriptionLangDataType[inventoryInfo.InventoryDescriptions.Count];
            int i = 0;
            //nazw może być kilka np. w różnych językach 
            foreach(var desc in inventoryInfo.InventoryDescriptions)
            {
              request.@params.products[0].productLongDescriptions.productLongDescriptionsLangData[i] 
                = new EComIAIDriver.IAIUpdateProducts.productLongDescriptionLangDataType()
              {
                langId = desc.Language,
                productLongDescription = desc.Text
              };          
              i++;
            }     
          }   	     
    
          //dodawnie zdjęć
          //var imageRequest = new EComIAIDriver.IAISetProductPicture.requestType();
          if((inventoryInfo?.Images?.Count ?? 0 ) > 0)
          {     
            request.@params.picturesSettings = new EComIAIDriver.IAIUpdateProducts.picturesSettingsType()
            {
                picturesSettingInputType = EComIAIDriver.IAIUpdateProducts.picturesSettingInputTypeType.base64,
                picturesSettingInputTypeSpecified = true,
                picturesSettingOverwrite = EComIAIDriver.IAIUpdateProducts.picturesSettingOverwriteType.y,
                picturesSettingOverwriteSpecified = true,
                picturesSettingDeleteOriginalPictures = 
                  EComIAIDriver.IAIUpdateProducts.picturesSettingDeleteOriginalPicturesType.y,
                picturesSettingDeleteOriginalPicturesSpecified = true,
                picturesSettingDeleteProductPictures = 
                  EComIAIDriver.IAIUpdateProducts.picturesSettingDeleteProductPicturesType.y,
                picturesSettingDeleteProductPicturesSpecified = true,
                picturesSettingDeleteProductIcons = 
                  EComIAIDriver.IAIUpdateProducts.picturesSettingDeleteProductIconsType.y,
                picturesSettingDeleteProductIconsSpecified = true,             
            };
    
            request.@params.products[0].productPicturesReplace = 
              new EComIAIDriver.IAIUpdateProducts.productPictureReplaceType[inventoryInfo.Images.Count];
    
            int i = 0;
            foreach(var image in inventoryInfo.Images)
            {
              request.@params.products[0].productPicturesReplace[i] = 
                new EComIAIDriver.IAIUpdateProducts.productPictureReplaceType();
              request.@params.products[0].productPicturesReplace[i].productPictureNumber = image.PictureNumber ?? 0;
              request.@params.products[0].productPicturesReplace[i].productPictureNumberSpecified = true;
              request.@params.products[0].productPicturesReplace[i].productPictureSource = image.PictureSource;      
              i++;
            }
          }    
    
          var command = new ECOM.PutProductRequestCommand()    
          {			
            ApiRequest = request,    
            EcomChannel = context.Message.EcomChannel,
            //zapisujemy wersję osobno, bo w obsłudze respona z api nie będziemy mieli po czym wyszukać, który to towary wysłaliśmy          
            EcomInventoryVersion = (inventoryInfo.WersjaRef ?? 0) 
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
                throw new Exception($"Błąd zapisu zmiany statusu dla zamówienia o REF: {inventoryInfo.EcomInventoryRef}");
              }  
            },
            true
          );
                 
          EDA.SendCommand($"{context.Headers.Get<string>("NeosConnector")}:ExportInventoryQueue", command);      
        }
        catch(Exception ex)
        {
          errMsg += $"Błąd przy generowaniu danych do wysłania na witrynę dla wersji {inventoryInfo.WersjaRef} " +
            $"w kanale sprzedaży {context.Message.EcomChannel}: {ex.Message}\n"; 
            
          RunInAutonomousTransaction(
            ()=>
            {
              inv.EditRecord();
              inv.SYNCSTATUS = (int)EcomSyncStatus.ExportError;
              inv.LASTSYNCERROR = ex.Message;      
              
              if(!inv.PostRecord())
              {
                throw new Exception($"Błąd zapisu zmiany statusu dla zamówienia o REF: {inventoryInfo.EcomInventoryRef}");
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
      string ecomOrderID = "";
      if((context.Message.OrderStatusInfoList?.Count ?? 0) > 0)
      {  
        var ecomOrder = new ECOM.ECOMORDERS();
        foreach(var orderStatus in context.Message.OrderStatusInfoList)
        {
          try
          {
            
            ecomOrderID = orderStatus.OrderId; 
            var request = new EComIAIDriver.IAIUpdateOrders.requestType();
            request.@params = new EComIAIDriver.IAIUpdateOrders.paramsType();
            //wymianę danych z fizycznym driverem robimy dla każdego statusu osobno,
            //żeby móc łatwiej przesledzić historię eksportu statusu dla zamówienia
            request.@params.orders = new EComIAIDriver.IAIUpdateOrders.orderType[1];           
            
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
            
            request.@params.orders[0] =
            new EComIAIDriver.IAIUpdateOrders.orderType()
            {
              orderId = orderStatus.OrderSymbol,          
              orderSerialNumber = Int32.Parse(orderStatus.OrderId),
              orderStatus = orderStatus.OrderStatusId 
            };
    
            //generujemy dane requesta do API dla paczek zamówienia
            EComIAIDriver.IAIAddPackages.addPackagesRequestType packagesRequest = null;
            if((orderStatus.OrderSpedInfo?.PackageList?.Count ?? 0) > 0)
            {
              packagesRequest = new EComIAIDriver.IAIAddPackages.addPackagesRequestType();
              packagesRequest.@params = new EComIAIDriver.IAIAddPackages.paramsType();
              packagesRequest.@params.orderPackages = new EComIAIDriver.IAIAddPackages.addPackagesOrderPackagesRequestType[1];
              packagesRequest.@params.orderPackages[0] = new EComIAIDriver.IAIAddPackages.addPackagesOrderPackagesRequestType();                
              //to nie błąd: dlas stuatuss zamówienia orderId jest zapisane SerialNumber, a tutaj w orderId
              packagesRequest.@params.orderPackages[0].orderId = orderStatus.OrderId; 
              
              var packageList = new List<EComIAIDriver.IAIAddPackages.addPackagesPackagesRequestType>();          
              foreach(var package in orderStatus.OrderSpedInfo.PackageList)
              {
                var packageToExport = new EComIAIDriver.IAIAddPackages.addPackagesPackagesRequestType();
                packageToExport.delivery = Int32.Parse(orderStatus.OrderSpedInfo.ShipperSymbol);
                packageToExport.packageNumber = package.ShipqingSymbol; //numer śledzenia paczki u spedytora
                packageList.Add(packageToExport);
              }
              //skałdamy i wysyłam synchronicznie komendę dodawania paczek, bo musi iść najpierw
              //dodanie paczek
              packagesRequest.@params.orderPackages[0].packages = packageList.ToArray();
              var packagesCommand = new ECOM.PutOrderPacakgesRequestCommand()
              {
                EcomChannel = context.Message.EcomChannel,          
                ApiPackagesRequest = packagesRequest
              };        
              EDA.ExecuteCommand(context.Headers.Get<string>("NeosConnector"), packagesCommand);
                    
            }
    
            EComIAIDriver.IAIInsertDocuments.insertDocumentsRequestType insertAttachmentRequest = null;
            if(!String.IsNullOrEmpty(orderStatus?.InvoiceInfo?.NagfakRef))
            {
              insertAttachmentRequest = new EComIAIDriver.IAIInsertDocuments.insertDocumentsRequestType();
              insertAttachmentRequest.@params = new EComIAIDriver.IAIInsertDocuments.paramsType();
              insertAttachmentRequest.@params.documents = new EComIAIDriver.IAIInsertDocuments.documentType[1];
              insertAttachmentRequest.@params.documents[0] = new EComIAIDriver.IAIInsertDocuments.documentType();
              insertAttachmentRequest.@params.documents[0].orderSerialNumber = Int32.Parse(orderStatus.InvoiceInfo.OrderId);
              insertAttachmentRequest.@params.documents[0].name = orderStatus.InvoiceInfo.FileName;
              // w parametr pdfBase64 wpisuje sciezke do pliku pdf
              // sterownik konwertuje na base64
              insertAttachmentRequest.@params.documents[0].pdfBase64 = orderStatus.InvoiceInfo.AttachmentPath;
              insertAttachmentRequest.@params.documents[0].additionalData = new EComIAIDriver.IAIInsertDocuments.additionalDataType();
              insertAttachmentRequest.@params.documents[0].additionalData.documentId = orderStatus.InvoiceInfo.AttachmentSymbol;
              
              var insertAttachmentCommand = new ECOM.PutAttachmentRequestCommand()
              {
                EcomChannel = context.Message.EcomChannel,
                ApiInsertAttachmentRequest = insertAttachmentRequest
              };  
              EDA.ExecuteCommand(context.Headers.Get<string>("NeosConnector"), insertAttachmentCommand);
            }
    
            var edaIdentifierData = ecomOrder.EDAID;        
            var command = new ECOM.PutOrderStatusRequestCommand()    
            {
              EcomChannel = context.Message.EcomChannel,
              EcomOrderId = orderStatus.OrderId,
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
                ecomOrder.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{ecomOrder.ECOMCHANNELREF.Symbol} = 0{context.Message.EcomChannel}" +
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
      var invStock = new ECOM.ECOMINVENTORYSTOCKS();  
    
      var request = new EComIAIDriver.IAIUpdateProductStock.requestType();
      request.@params = new EComIAIDriver.IAIUpdateProductStock.paramsType();
    
      var stocksToSendList = new List<EComIAIDriver.IAIUpdateProductStock.productType>();
    
      foreach(var inventoryStockInfo in context.Message.InventoryStocksInfoList)
      {        
        try
        {
          invStock.FilterAndSort($"{nameof(ECOM.ECOMINVENTORYSTOCKS)}.{invStock.REF.Symbol} = 0{inventoryStockInfo.EcomInventoryStockRef}");
          if(!invStock.FirstRecord())
          {   
            errMsg += $"Nie znaleziono stanu magazynowego o ref: {inventoryStockInfo.EcomInventoryStockRef} w kanale: {context.Message.EcomChannel}\n";            
            continue; 
          }     
    
          var productStock = new EComIAIDriver.IAIUpdateProductStock.productType();
          
          productStock.productSizeCodeExternal = inventoryStockInfo.WersjaRef;
          productStock.stockId = Convert.ToInt32(inventoryStockInfo.ChannelStockId);
          productStock.stockIdSpecified = true;
          productStock.productSizeQuantity = (float)inventoryStockInfo.Quantity;
          productStock.productSizeQuantitySpecified = true;
    
          stocksToSendList.Add(productStock);
    
          RunInAutonomousTransaction(
            ()=>
            {      
              invStock.EditRecord();      
              invStock.SYNCSTATUS = (int)EcomSyncStatus.ExportProceeding;           
              if(!invStock.PostRecord())
              {
                throw new Exception($"Błąd zapisu zmiany statusu dla zamówienia o REF: {inventoryStockInfo.EcomInventoryStockRef}");
              }         
            },
            true
          );                   
        }
        catch(Exception ex)
        {
          errMsg += $"Błąd przy generowaniu danych do wysłania stanu magazynowego na witrynę dla towaru o id: {inventoryStockInfo.EcomInventoryStockRef} " +
            $"w kanale sprzedaży {context.Message.EcomChannel}: {ex.Message}\n";       
          
          RunInAutonomousTransaction(
            ()=>
            {
              invStock.FilterAndSort($"{nameof(ECOM.ECOMINVENTORYSTOCKS)}.{invStock.REF.Symbol} = 0{inventoryStockInfo.EcomInventoryStockRef}");
              if(invStock.FirstRecord())
              {    
                invStock.EditRecord();      
                invStock.SYNCSTATUS = (int)EcomSyncStatus.ExportError;   
                invStock.LASTSYNCERROR = ex.Message;       
                if(!invStock.PostRecord())
                {
                  throw new Exception($"Błąd zapisu zmiany statusu dla zamówienia o REF: {inventoryStockInfo.EcomInventoryStockRef}");
                } 
              
              }
              else
              {
                throw new Exception($"Nie znaleziono stanu mag. dla towaru o id: {inventoryStockInfo.EcomInventoryStockRef}.");
              }
            },
            true
          );
          continue;
        }      
      }
    
      if(stocksToSendList.Count > 0)
      {
        request.@params.products = stocksToSendList.ToArray();
    
        var putProductsStocksCommand = new ECOM.PutProductsStocksRequestCommand()  
        {	
          EcomChannel = context.Message.EcomChannel,
          ApiRequest = request 
        };  
    
        if(!string.IsNullOrEmpty(context.Message.GetLogicalQueueIdentifier()))
        {
          putProductsStocksCommand.SetLogicalQueueIdentifier(context.Message.GetLogicalQueueIdentifier());
        }		    
          
        EDA.SendCommand(context.Headers.Get<string>("NeosConnector"), putProductsStocksCommand);
      }         
      invStock.Close();        
      return errMsg;
    }
    
    
    


  }
}
