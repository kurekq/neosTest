
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
  
  public partial class BR_KLIENCI_CENY<TModel> where TModel : MODEL.BR_KLIENCI_CENY, new()
  {

    /// <param name="ecomChannelRef"></param>
    /// <param name="calcMode"></param>
    /// <param name="inventoryList"></param>
    /// <param name="currencyList"></param>
    [UUID("3e261f89a5cf4c2ca0ad5d17affdfada")]
    virtual public string CalculateInventoryPricesForEcomChannel(int ecomChannelRef, CalculateMode calcMode, 
      List<Int64> inventoryList = null, List<string> currencyList = null)   
    {
      string resultMsg = "";
      try
      {   
        string sqlSelectInventoriesToExportString = "";
        var channelParams = LOGIC.ECOMSTDMETHODS.LoadEcomChannelParams(ecomChannelRef);  
        var ecomInventoryPriceLogic = new LOGIC.ECOMINVENTORYPRICES();
        var ecomChannel = new LOGIC.ECOMCHANNELS().Get(ecomChannelRef);
        int calculatedPricesCounter = 0; 
        
        var inventoryPricesParams = new BR_KLIENCI_CENYParameters();
        if(string.IsNullOrEmpty(channelParams["_clientprice"]))
        {
          throw new Exception($"Nie znaleziono klineta do naliczania cen dla kanału sprzedaży: {ecomChannel.NAME}");
        }
        inventoryPricesParams._klient = channelParams["_clientprice"].AsInteger;
    
    
        inventoryPricesParams._datacen = DateTime.Now;
        var inventoryPricesData = new LOGIC.BR_KLIENCI_CENY().WithParameters(inventoryPricesParams);       
    
        currencyList = currencyList == null ?  new List<string>() : currencyList; 
        //odpalamy procedurę nalicznia cen i zwracamy wszystkie dla danego klienta
        //zostawiamy tylko ceny w walutach, które przekazano, chyba, że lista pusta, to wtedy wszystkie waluty
        var inventoriesPricesList = 
          (from p in inventoryPricesData.Get().ToList()
          where p.JEDNGLOWNA == 1
            && (currencyList.Count == 0 || currencyList.Contains(p.WALUTA))  
          select p);  
    
        switch (calcMode)
        {
          case CalculateMode.Invalid:
            //naliczamy dla trybu invalid
            //w trybie aktualizacji naliczamy tylko dla cen oznaczonych jako nieaktualne dla danego klienta
            
            sqlSelectInventoriesToExportString =
              @"select p.ref invpriceref, i.wersjaref wersjaref, p.currency  
                from ecominventories i
                  join ecominventoryprices p on p.ecominventoryref = i.ref
                    and p.isvalid = 0 " +           
                $"where i.ACTIVE = 1 and i.ecomchannelref = 0{ecomChannelRef.ToString()}";           
            
            //wykorzystujemy QuerySQL, bo nie mamy obiektu z powyższy źródłem danych
            foreach(var dataRow in CORE.QuerySQL(sqlSelectInventoriesToExportString))
            {         
              var ecomInventoryPrice = ecomInventoryPriceLogic.Get(dataRow["INVPRICEREF"].AsInteger); 
              if(ecomInventoryPrice != null)
              {
                try
                {
                  //wyszukujemy cenę dla wybranego towaru w danej walucie
                  var newPrice = 
                    (from p in inventoriesPricesList 
                      where p.WERSJAREF == dataRow["WERSJAREF"].AsInteger 
                        && p.WALUTA == dataRow["CURRENCY"]  
                      select p).SingleOrDefault();
    
                  if(newPrice != null)
                  {       
                    ecomInventoryPrice.BASICPRICE = decimal.Round((newPrice.ORGCENANET ?? 0), 2);
                    ecomInventoryPrice.PROMOPRICE = decimal.Round((newPrice.CENANET ?? 0), 2);
                    ecomInventoryPrice.ISVALID = 1;
                    ecomInventoryPrice.SYNCSTATUS = (int)EcomSyncStatus.ExportPending;
                    ecomInventoryPrice.LASTCALCTMSTMP = DateTime.Now;
                    ecomInventoryPriceLogic.Update(ecomInventoryPrice);            
                  }
                }
                catch(Exception ex)
                {
                  throw new Exception($"Błąd naliczania ceny o ref: {dataRow["INVPRICEREF"]} dla towaru: {ecomInventoryPrice.KTM}");           
                }
                calculatedPricesCounter++;        
              }
              else
              {
                resultMsg += $"Nie znaleziono ceny o ref {dataRow["INVREF"]} dla towaru: {ecomInventoryPrice.KTM} w kanale sprzedaży";
                break;
              }         
            }
            break;
    
          case CalculateMode.All:    
            //w trybie naliczania wszystkiego przechodzimy po towarach w ecmoinventory 
            //i aktualizujemy lub dodajemy nowe jeżeli nie ma ceny z wybraną walutą w ecominventoryprices
    
            foreach(var ecomInventory in new LOGIC.ECOMINVENTORIES().Get().Where(i => i.ECOMCHANNELREF == ecomChannelRef && i.ACTIVE == 1))
            {
              //wyszukujemy wyliczone ceny dla wybranego towaru w danej walucie;
              var pricesListForInventory = 
                (from p in inventoriesPricesList 
                  where p.WERSJAREF == ecomInventory.WERSJAREF               
                  select p).ToList();
                  
              foreach(var ecomInventoryPrice in ecomInventoryPriceLogic.Get().Where(i => i.ECOMINVENTORYREF == ecomInventory.REF))
              {
                ecomInventoryPrice.SYNCSTATUS = (int)EcomSyncStatus.Unsynchronizable;
                ecomInventoryPriceLogic.Update(ecomInventoryPrice);               
              }
    
              //dla każdego wpisu w kanale sprzedaży dodajemy lub lub aktualizujemy wpis w ECOMINVENTORYPRICES
              foreach(var price in pricesListForInventory)
              {
                try
                {
                  var ecomInventoryPrice = ecomInventoryPriceLogic.Get().Where(i => i.ECOMINVENTORYREF == ecomInventory.REF && i.CURRENCY == price.WALUTA).SingleOrDefault();
    
                  if(ecomInventoryPrice == null) 
                  {
                    ecomInventoryPrice = new ECOM.MODEL.ECOMINVENTORYPRICES();
                    ecomInventoryPrice.ECOMINVENTORYREF = ecomInventory.REF;
                    ecomInventoryPrice.CURRENCY = price.WALUTA; 
                    ecomInventoryPrice.BASICPRICE = decimal.Round((price.ORGCENANET ?? 0), 2);
                    ecomInventoryPrice.PROMOPRICE = decimal.Round((price.CENANET ?? 0), 2);
                    ecomInventoryPrice.ISVALID = 1;
                    ecomInventoryPrice.SYNCSTATUS = (int)EcomSyncStatus.ExportPending;
                    ecomInventoryPrice.LASTCALCTMSTMP = DateTime.Now;
                    ecomInventoryPriceLogic.Create(ecomInventoryPrice);
                  }
                  else
                  {
                    ecomInventoryPrice.ECOMINVENTORYREF = ecomInventory.REF;
                    ecomInventoryPrice.CURRENCY = price.WALUTA; 
                    ecomInventoryPrice.BASICPRICE = decimal.Round((price.ORGCENANET ?? 0), 2);
                    ecomInventoryPrice.PROMOPRICE = decimal.Round((price.CENANET ?? 0), 2);
                    ecomInventoryPrice.ISVALID = 1;
                    ecomInventoryPrice.SYNCSTATUS = (int)EcomSyncStatus.ExportPending;
                    ecomInventoryPrice.LASTCALCTMSTMP = DateTime.Now;  
                    ecomInventoryPriceLogic.Update(ecomInventoryPrice);          
                  }
                  calculatedPricesCounter++;
                }          
                catch(Exception ex)
                {            
                  throw new Exception($"Błąd dodawania naliczania ceny dla wersji towaru: {price.WERSJAREF}: {ex.Message}");
                }     
              } 
            } 
            break;
    
          case CalculateMode.List:    
            //aktualizacja dla wybranych towarów wskazanych 
            if((inventoryList?.Count ?? 0) == 0)
            {
              throw new Exception($"Brak listy z towarami dla naliczenia cen wybranych towarów"); 
            }
    
            //w trybie naliczania dla wybranych towarów przechodzimy po towarach w ecmoinventory, wyliczamy dla nich ceny
            //i aktualizujemy lub dodajemy nowe do jeżeli nie ma ceny z wybraną walutą w ecominventoryprices
            //w trybie listy towarów dodatkowo filtrujemy po towarach
            foreach(var ecomInventory in new LOGIC.ECOMINVENTORIES().Get().Where(i => inventoryList.Contains(i.REF ?? 0)))
            {
              //wyszukujemy wyliczone ceny dla wybranego towaru;
              var pricesListForInventory = 
                (from p in inventoriesPricesList 
                  where p.WERSJAREF == ecomInventory.WERSJAREF               
                  select p).ToList();
    
              foreach(var ecomInventoryPrice in ecomInventoryPriceLogic.Get().Where(i => i.ECOMINVENTORYREF == ecomInventory.REF))
              {
                ecomInventoryPrice.SYNCSTATUS = (int)EcomSyncStatus.Unsynchronizable;
                ecomInventoryPriceLogic.Update(ecomInventoryPrice);               
              }        
    
              //dla każdego wpisu w kanale sprzedaży dodajemy wpis w ECOMINVENTORYPRICES
              foreach(var price in pricesListForInventory)
              {
                try
                {
                  var ecomInventoryPrice = ecomInventoryPriceLogic.Get().Where(i => i.ECOMINVENTORYREF == ecomInventory.REF && i.CURRENCY == price.WALUTA).SingleOrDefault();
    
                  if(ecomInventoryPrice == null) 
                  {
                    ecomInventoryPrice = new ECOM.MODEL.ECOMINVENTORYPRICES();
                    ecomInventoryPrice.ECOMINVENTORYREF = ecomInventory.REF;
                    ecomInventoryPrice.CURRENCY = price.WALUTA; 
                    ecomInventoryPrice.BASICPRICE = decimal.Round((price.ORGCENANET ?? 0), 2);
                    ecomInventoryPrice.PROMOPRICE = decimal.Round((price.CENANET ?? 0), 2);
                    ecomInventoryPrice.ISVALID = 1;
                    ecomInventoryPrice.SYNCSTATUS = (int)EcomSyncStatus.ExportPending;
                    ecomInventoryPrice.LASTCALCTMSTMP = DateTime.Now;
                    ecomInventoryPriceLogic.Create(ecomInventoryPrice);
                  }
                  else
                  {
                    ecomInventoryPrice.ECOMINVENTORYREF = ecomInventory.REF;
                    ecomInventoryPrice.CURRENCY = price.WALUTA; 
                    ecomInventoryPrice.BASICPRICE = decimal.Round((price.ORGCENANET ?? 0), 2);
                    ecomInventoryPrice.PROMOPRICE = decimal.Round((price.CENANET ?? 0), 2);
                    ecomInventoryPrice.ISVALID = 1;
                    ecomInventoryPrice.SYNCSTATUS = (int)EcomSyncStatus.ExportPending;
                    ecomInventoryPrice.LASTCALCTMSTMP = DateTime.Now;  
                    ecomInventoryPriceLogic.Update(ecomInventoryPrice);          
                  }
                  calculatedPricesCounter++; 
                }
                catch(Exception ex)
                {            
                  throw new Exception($"Błąd dodawania naliczania ceny dla wersji towaru: {price.WERSJAREF}: {ex.Message}");
                }             
              }
            }
            break;
          
          defalut:
            throw new Exception($"Nieobsłużony tryb naliczania cen towarów: {calcMode.ToString()}");                  
            break;      
        }
    
        if(string.IsNullOrEmpty(resultMsg))
        {
          resultMsg = $"Ilość cen naliczonych w kanale {ecomChannel.NAME}: {calculatedPricesCounter.ToString()}";    
        }
      }
      catch(Exception ex)
      {
        resultMsg += ex;
      }  
      return resultMsg;
    }
    
    


  }
}
