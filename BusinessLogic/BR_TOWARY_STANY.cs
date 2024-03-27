
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
  
  public partial class BR_TOWARY_STANY<TModel> where TModel : MODEL.BR_TOWARY_STANY, new()
  {

    /// <param name="ecomChannelRef"></param>
    /// <param name="calcMode"></param>
    /// <param name="inventoryList"></param>
    /// <param name="ecomChannelStockList"></param>
    [UUID("807caaa7537046b5aac2ddedb1a43fd7")]
    virtual public string CalculateInventoryStocksForEcomChannel(int ecomChannelRef, CalculateMode calcMode, 
      List<Int64> inventoryList = null, List<Int64> ecomChannelStockList = null)      
    {  
      string resultMsg = "";  
      var ecomInventoryStocksLogic = new LOGIC.ECOMINVENTORYSTOCKS();
      var ecomChannelStockLogic = new LOGIC.ECOMCHANNELSTOCK();    
      var ecomChannel = new LOGIC.ECOMCHANNELS().Get(ecomChannelRef);
      int calculatedStockCounter = 0;
      var inventoryStocksParams = new BR_TOWARY_STANYParameters();
    
      
      //jeżeli przekazano listę magazynów wirtualnych, to wyciągamy z niej listę (distinct) magazynów rzeczywistych
      //i odpalamy standardową procedurę tylko dla tych magazynów
      if (ecomChannelStockList == null)
      {
        ecomChannelStockList = new List<Int64>();
      }
    
      var warehousesOnChannelStocks = new List<string>();
      if(ecomChannelStockList.Count > 0)
      {
        var warehousesOnChannelStocksTemp  =         
          ecomChannelStockLogic.Get().ToList()
            .Where(s => ecomChannelStockList.Contains(s.REF ?? 0))
            .Select(s => s.DEFMAGAZLIST).ToList();
    
        foreach(var whList in warehousesOnChannelStocksTemp)
        {
          warehousesOnChannelStocks.AddRange(whList.Trim().Split(new char[]{';'}, StringSplitOptions.RemoveEmptyEntries));      
        }
        //usuwamy duble
        warehousesOnChannelStocks = warehousesOnChannelStocks.Distinct().ToList();
      }  
    
      //odpalamy procedurę naliczania stanów i zwracamy wszystkie dla danego klienta
      //zostawiamy tylko ceny w walutach, które przekazano, chyba, że lista pusta, to wtedy wszystkie waluty
    
      switch (calcMode)
      {    
        case CalculateMode.Invalid:
          //naliczamy dla trybu invalid
          //w trybie aktualizacji naliczamy tylko dla stany oznaczone jako nieaktualne     
          
          string sqlSelectInventoriesToExportString =
            @"select distinct s.ref invstockref, i.wersjaref wersjaref 
              from ecominventories i
                join ecominventorystocks s on s.ecominventoryref = i.ref
                  and s.isvalid = 0 " +           
              $"where i.ACTIVE = 1 and i.ecomchannelref = 0{ecomChannelRef}";           
          
          //wykorzystujemy QuerySQL, bo nie mamy obiektu z powyższym źródłem danych
          foreach(var dataRow in CORE.QuerySQL(sqlSelectInventoriesToExportString))
          {                 
            var ecomInventoryStock = ecomInventoryStocksLogic.Get(dataRow["INVSTOCKREF"].AsInteger);         
            if(ecomInventoryStock != null)
            {
              try
              {
                var ecomChannelStock = ecomChannelStockLogic.Get(ecomInventoryStock.ECOMCHANNELSTOCKREF.Value);
                var warehousesForInventoryStock = 
                  ecomChannelStock.DEFMAGAZLIST.Split(new char[]{';'}, StringSplitOptions.RemoveEmptyEntries)
                  .ToList();
    
                //wyszukujemy stan magazynowy dla danego towaru dla wszystkich magazynów teneum zawartych w magazynie logicznym
                //dla wybranego stanu magazynowego
                inventoryStocksParams._wersjaref = dataRow["WERSJAREF"].AsInteger;
                var inventoryStockData = new LOGIC.BR_TOWARY_STANY().WithParameters(inventoryStocksParams);
                var newInventoryStock = inventoryStockData.Get().ToList()
                  .Where(s => (warehousesOnChannelStocks.Count == 0 
                    || warehousesOnChannelStocks.Contains(s.RMAGAZYN))
                    && warehousesForInventoryStock.Contains(s.RMAGAZYN));
    
                if(newInventoryStock != null)
                {              
                  //wrzucamy zsumowaną ilość za wszystkich magazynów przypisanych do magazynu logicznego
                  var newQuaintity = newInventoryStock.Sum(s => s.ILOSC);
                  ecomInventoryStock.QUANTITY = (newQuaintity >= ecomChannelStock.MINQUANTITY ? newQuaintity : 0m);            
                }
                else
                {
                  //jeżeli już nie mamy takiego stanu mag. to wysyłamy stan 0
                  ecomInventoryStock.QUANTITY = 0;   
                }
                ecomInventoryStock.ISVALID = 1;
                ecomInventoryStock.SYNCSTATUS = (int)EcomSyncStatus.ExportPending;
                ecomInventoryStocksLogic.Update(ecomInventoryStock);        
              }
              catch(Exception ex)
              {
                throw new Exception($"Błąd naliczania stanu mag. o ref: {dataRow["INVSTOCKREF"].AsInteger} dla towaru: {ecomInventoryStock.ECOMINVENTORYREF}");           
              }
              calculatedStockCounter++;        
            }
            else
            {
              resultMsg += $"Nie znaleziono stanu mag. o ref {dataRow["INVSTOCKREF"].AsInteger} w kanale sprzedaży";
              break;
            }         
          }
          break;   
       
        case CalculateMode.All:    
          //w trybie naliczania wszystkiego przechodzimy po towarach w ecominventory 
          //i aktualizujemy lub dodajemy nowe stany jeżeli nie ma stanu z wybraną walutą w ecominventorystocks
          foreach(var ecomInventory in new LOGIC.ECOMINVENTORIES().Get().Where(i => i.ECOMCHANNELREF == ecomChannelRef && i.ACTIVE == 1))
          {
            //dla kanału sprzedaży przechodzimy po wszystkich magazynach wirtualnych lub po liście przekazanej do metody 
            //i na podstawie danych zwróconych z procedury standardowej wyliczamy stan dla danego mag. wirtualnego
            try
            {
              foreach(var ecomChannelStock in 
                ecomChannelStockLogic.Get()
                  .Where(s => s.ECOMCHANNELREF == ecomChannelRef).ToList()
                  .Where(s => (ecomChannelStockList.Count == 0 ) || ecomChannelStockList.Contains(s.REF ?? 0)))
              {
                var warehousesOnChannelStock = 
                  ecomChannelStock.DEFMAGAZLIST.Split(new char[]{';'}, StringSplitOptions.RemoveEmptyEntries)
                  .ToList();
    
                //w zwróconych z procedury standardowej wyszukujemy stany towaru dla magazynów z listy mag wirtualnego
                inventoryStocksParams._wersjaref = ecomInventory.WERSJAREF;
                var inventoryStockData = new LOGIC.BR_TOWARY_STANY().WithParameters(inventoryStocksParams);
                var newStockListForInventory = inventoryStockData.Get().ToList()
                  .Where(s => (warehousesOnChannelStocks.Count == 0 
                    || warehousesOnChannelStocks.Contains(s.RMAGAZYN))
                    && warehousesOnChannelStock.Contains(s.RMAGAZYN));
    
                if (newStockListForInventory == null)
                {
                  throw new Exception($"Nie znaleziono stanów mag. dla wersji towaru {ecomInventory.WERSJAREF}");
                }
                var newQuaintity = newStockListForInventory.Sum(s => s.ILOSC); 
    
                var ecomInventoryStock = 
                  ecomInventoryStocksLogic.Get()
                  .Where(s => s.ECOMINVENTORYREF == ecomInventory.REF && s.ECOMCHANNELSTOCKREF == ecomChannelStock.REF)
                  .SingleOrDefault();          
    
                if(ecomInventoryStock == null)
                {
                  //jak nie ma to zakładamy nowy wpis
                  ecomInventoryStock = new ECOM.MODEL.ECOMINVENTORYSTOCKS();
                  ecomInventoryStock.QUANTITY = (newQuaintity >= ecomChannelStock.MINQUANTITY ? newQuaintity : 0m);
                  ecomInventoryStock.ISVALID = 1;
                  ecomInventoryStock.SYNCSTATUS = (int)EcomSyncStatus.ExportPending;
                  ecomInventoryStock.ECOMCHANNELSTOCKREF = ecomChannelStock.REF;
                  ecomInventoryStock.ECOMINVENTORYREF = ecomInventory.REF; 
                  ecomInventoryStocksLogic.Create(ecomInventoryStock);
                }
                else
                {           
                  //jak jest stan dla towaru w magazynie wirtualmym to go aktualizujemy            
                  ecomInventoryStock.QUANTITY = (newQuaintity >= ecomChannelStock.MINQUANTITY ? newQuaintity : 0m);
                  ecomInventoryStock.ISVALID = 1;
                  ecomInventoryStock.SYNCSTATUS = (int)EcomSyncStatus.ExportPending;
                  ecomInventoryStocksLogic.Update(ecomInventoryStock);
                }
                calculatedStockCounter++;
              }
            }
            catch(Exception ex)
            {            
              throw new Exception($"Błąd dodawania naliczania stanu mag. dla wersji towaru: {ecomInventory.WERSJAREF}: {ex.Message}");
            }    
          }
          break;
    
        case CalculateMode.List:    
          //aktualizacja dla wybranych towarów wskazanych 
          if((inventoryList?.Count ?? 0) == 0)
          {
            throw new Exception($"Brak listy z towarami dla naliczenia stanów wybranych towarów"); 
          }
    
          foreach(var ecomInventory in new LOGIC.ECOMINVENTORIES().Get()
            .Where(i => inventoryList.Contains(i.REF ?? 0)))
          {
            //dla każdego towaru na liście przechodzimy po wszystkich magazynach wirtualnych kanału sprzedaży lub po przekazanej liście, 
            //bierzemy z każdego listę magazynów teneum,
            //i na podstawie danych zwróconych z procedury standardowej wyliczamy stan dla danego mag. wirtualnego
            try
            {
              foreach(var ecomChannelStock in 
                ecomChannelStockLogic.Get()
                  .Where(s => s.ECOMCHANNELREF == ecomChannelRef).ToList()
                  .Where(s => (ecomChannelStockList.Count == 0 ) || ecomChannelStockList.Contains(s.REF ?? 0))) 
              {
                var warehousesOnChannelStock = 
                  ecomChannelStock.DEFMAGAZLIST.Split(new char[]{';'}, StringSplitOptions.RemoveEmptyEntries)
                  .ToList();
    
                //wyszukujemy stany towaru dla magazynów z listy mag wirtualnego
                inventoryStocksParams._wersjaref = ecomInventory.WERSJAREF;
                var inventoryStockData = new LOGIC.BR_TOWARY_STANY().WithParameters(inventoryStocksParams);
                var newStockListForInventory = inventoryStockData.Get().ToList()
                  .Where(s => (warehousesOnChannelStocks.Count == 0 
                    || warehousesOnChannelStocks.Contains(s.RMAGAZYN))
                    && warehousesOnChannelStock.Contains(s.RMAGAZYN));
                    
                if (newStockListForInventory == null)
                {
                  throw new Exception($"Nie znaleziono stanów mag. dla wersji towaru {ecomInventory.WERSJAREF}");
                }
                else 
                {
                  var newQuaintity = newStockListForInventory.Sum(s => s.ILOSC); 
    
                  var ecomInventoryStock = 
                    ecomInventoryStocksLogic.Get()
                    .Where(s => s.ECOMINVENTORYREF == ecomInventory.REF && s.ECOMCHANNELSTOCKREF == ecomChannelStock.REF)
                    .SingleOrDefault();
    
                  if(ecomInventoryStock == null)
                  {
                    //jak nie ma to zakładamy nowy wpis
                    ecomInventoryStock = new ECOM.MODEL.ECOMINVENTORYSTOCKS();
                    ecomInventoryStock.QUANTITY = (newQuaintity >= ecomChannelStock.MINQUANTITY ? newQuaintity : 0m);
                    ecomInventoryStock.ISVALID = 1;
                    ecomInventoryStock.SYNCSTATUS = (int)EcomSyncStatus.ExportPending;
                    ecomInventoryStock.ECOMCHANNELSTOCKREF = ecomChannelStock.REF;
                    ecomInventoryStock.ECOMINVENTORYREF = ecomInventory.REF; 
                    ecomInventoryStocksLogic.Create(ecomInventoryStock);
                  }
                  else
                  {           
                    //jak jest stan dla towaru w magazynie wirtualnym to go aktualizujemy            
                    ecomInventoryStock.QUANTITY = (newQuaintity >= ecomChannelStock.MINQUANTITY ? newQuaintity : 0m);
                    ecomInventoryStock.ISVALID = 1;
                    ecomInventoryStock.SYNCSTATUS = (int)EcomSyncStatus.ExportPending;
                    ecomInventoryStocksLogic.Update(ecomInventoryStock);
                  }
                  calculatedStockCounter++;
                }
              }
            }
            catch(Exception ex)
            {            
              throw new Exception($"Błąd dodawania naliczania stanu mag. dla wersji towaru: {ecomInventory.WERSJAREF}: {ex.Message}");
            }    
          }
          break;      
    
        defalut:
          throw new Exception($"Nieobsłużony tryb naliczania stanów mag. towarów: {calcMode.ToString()}");                  
      }
      
      if(string.IsNullOrEmpty(resultMsg))
      {
        resultMsg = $"Naliczono stanów: {calculatedStockCounter.ToString()} w kanale {ecomChannel.NAME}";    
      } 
     
      return resultMsg;   
    }
    
    
    
    


  }
}
