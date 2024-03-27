
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
  
  public partial class ECOMSTDMETHODSBL<TModel> where TModel : MODEL.ECOMSTDMETHODSBL, new()
  {

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
    override public void GetUnitsForInventoryInfo(int ecomChannelRef, string ktm, EcomInventoryInfo inventoryInfo, string attributesToSend)
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
                newEAN = towkodkresk.KODKRESK.ToString();
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
    
              //tutaj
    
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
    
    
    


    /// <param name="ecomInventoryRef"></param>
    [UUID("a5b3fda6b8024f1eb6c14afbd271b505")]
    override public EcomInventoryInfo GetInventoryInfo(Int64 ecomInventoryRef)
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
      string atributesToSend = ecomChannelParams["_inventoryattributestosend"];
    
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
    override public void GetProductForInventoryInfo(int ecomChannelRef, string ktm, EcomInventoryInfo inventoryInfo, string attributesToSend)
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
        inventoryInfo.KTM = attributesToSend.Contains("[SKU]") ? inventoryData.KTM : null;
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
    
        inventoryInfo.EAN = !string.IsNullOrEmpty(inventoryVersion.KODKRESK) ? inventoryVersion.KODKRESK : inventoryData.KODKRESK;   
      }
      else
      {
        throw new Exception ($"Nie znaleziono towaru o ktm: {ktm}.");
      }  
      inventoryData.Close();    
      inventoryVersion.Close();         
    }
    
    
    


  }
}
