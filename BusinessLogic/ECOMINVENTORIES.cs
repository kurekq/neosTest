
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
  
  public partial class ECOMINVENTORIES<TModel> where TModel : MODEL.ECOMINVENTORIES, new()
  {

    /// <summary>
    /// Metoda dodająca wszystkie wersje towaru do kanału sprzedaży (tabela ECOMINVENTORIES) .
    /// Jeżeli wersja towaru jest już dodana do kanału sprzedaży metoda ją aktualizuje, lub kończy działania, w zależności od parametrów wejściowych
    /// Używana w obsłudze zdarzenia na zmianę towaru i przy dodawaniu wybranych towarów z poziomu kartoteki towarów
    /// parametry:
    ///  - ecomChannelRef - ref kanału sprzedaży, do ktorego będą dodawane wpisy
    ///  - ktm towaru, którego wersje będą dodawane do kanalu sprzedaży
    ///  - allowUpdate - flaga oznaczająca, czy można aktualizować wcześnej dodany wpis
    /// 
    /// </summary>
    /// <param name="ecomChannelRef"></param>
    /// <param name="ktm"></param>
    /// <param name="allowUpdate"></param>
    [UUID("0bb823f74a5e4c03ab42a838e9607544")]
    public static void AddInventoryToEcomChannel(int ecomChannelRef, string ktm, bool allowUpdate = false)
    {
      string sqlString = 
        $"select wer.ref, wer.ktm, coalesce(wer.witryna, 0) witryna from wersje wer where wer.ktm = '{ktm}'";
      var ecomInventory = new ECOM.ECOMINVENTORIES();
      foreach(var dataRow in CORE.QuerySQL(sqlString))
      {   
        ecomInventory.FilterAndSort($"{nameof(ECOM.ECOMINVENTORIES)}.{ecomInventory.ECOMCHANNELREF.Symbol} = {ecomChannelRef} " +
          $"AND {nameof(ECOM.ECOMINVENTORIES)}.{ecomInventory.WERSJAREF.Symbol} = {dataRow["REF"]}");
        if(ecomInventory.FirstRecord())
        { 
          //Contexts ecomChannelParams = ECOM.ECOMSTANDARDPROC. ECOM.ECOMSTANDARDPROC.LoadEcomChannelParams(ecomChannelRef);
          var ecomInventories = new ECOM.ECOMINVENTORIES(); 
          if(dataRow["WITRYNA"].AsInteger == 1 || dataRow["WITRYNA"].AsInteger == 2)
          { 
            //nie ma, a chcemy dodać w ECOMORDERS
            ecomInventories.NewRecord(); 
            ecomInventories.ECOMCHANNELREF = ecomChannelRef;
            ecomInventories.WERSJAREF = dataRow["REF"].AsInteger;                    
            ecomInventories.SYNCSTATUS = (int)EcomSyncStatus.Unsynchronizable; 
              /*(ecomChannelParams["_sendinventoryautomatically"].AsInteger == 1 ? 
                (int)EcomSyncStatus.ExportPending : (int)EcomSyncStatus.Unsynchronizable);*/
            ecomInventories.ACTIVE = dataRow["WITRYNA"].AsInteger == 1 ? 1 : 0;    
    
            if(!ecomInventories.PostRecord())
            {
              throw new Exception("Błąd dodania towaru do bazy danych");
            }
          }           
        } 
        else
        {
          throw new Exception($"Nie znaleziono wersji towaru o REF: {dataRow["REF"]}");
        }    
      }
      ecomInventory.Close();
    }
    
    


    /// <summary>
    /// Handler zdarzenia bazodanowego zmiany na tabeli TOWPLIKI, 
    /// wywołuje metodę UpdateInventoryRequest sprawdzającą status towaru w kanalach sprzedaży,
    /// ponieważ na chwilę obecną zdjęcia traktowane są jako część towaru 
    /// 
    /// </summary>
    /// <param name="context"></param>
    [CustomData("MessageHandlerType=E")]
    [UUID("13a2625eb89543a4acc65c6000eaf5d1")]
    virtual public HandlerResult FileChangedHandler(ConsumeContext<ECOM.FileChanged> context)
    {
      if (XkIsFileImage(context.Message))
      {
        try
        {    
          var command = new ECOM.CheckInventoryChangedCommand()     
          {      
            VersionRef = context.Message.WersjaRef,
            Ktm = context.Message.Ktm
          };
          command.SetDeduplicationIdentifier("INVENTORY_" + context.Message.WersjaRef.ToString());
          command.SetIdentifier("INVENTORY_"+ context.Message.WersjaRef.ToString());    
          EDA.SendCommand("ECOM.ECOMINVENTORIES.CheckInventoryChangedCommandHandler",command);
        }
        catch(Exception ex)
        {
          context.Message.Message = ex.Message;
          throw new Exception(ex.Message);
        }
      }
    
      return HandlerResult.Handled;  
    }
    
    


    /// <summary>
    /// Handler zdarzenia bazodanowego zmiany na tabeli ATRYBUTY, 
    /// wywołuje metodę UpdateInventoryRequest sprawdzającą status towaru w kanałach sprzedaży. 
    /// </summary>
    /// <param name="context"></param>
    [CustomData("MessageHandlerType=E")]
    [UUID("2243823eb2e24f3abf180d8ca3838bb9")]
    virtual public HandlerResult AttributeChangedHandler(ConsumeContext<ECOM.AttributeChanged> context)
    {
      try
      {
        var command = new ECOM.CheckInventoryChangedCommand()     
        {      
          VersionRef = context.Message.WersjaRef,
          Ktm = context.Message.Ktm
        };
        command.SetDeduplicationIdentifier("INENTORY_" + context.Message.WersjaRef.ToString());    
        command.SetIdentifier("INENTORY_" + context.Message.WersjaRef.ToString());    
        EDA.SendCommand("ECOM.ECOMINVENTORIES.CheckInventoryChangedCommandHandler",command);
      }
      catch(Exception ex)
      {
        context.Message.Message = ex.Message;
        throw new Exception(ex.Message);
      }
      return HandlerResult.Handled;
    }
    
    


    /// <param name="ecomChannelRef"></param>
    /// <param name="ecomInventory"></param>
    [UUID("358d21517287492ea68e3795843e6123")]
    public static GetEDAIdentifierData GetEDAID(int ecomChannelRef, EcomInventoryInfo ecomInventory)
    {
      string EDAInventoryId = "";
      string errorMsg = "";  
      try
      {    
        ECOM.ECOMSTDMETHODS methods = LOGIC.ECOMCHANNELS.FindMethodObject(ecomChannelRef);
        if(methods != null)
        {
          EDAInventoryId =  methods.Logic.GetInventoryEDAID(ecomChannelRef, ecomInventory);
        }
      }
      catch(Exception ex)
      {
        if(!string.IsNullOrEmpty(ecomInventory.InventoryId))
        {
          errorMsg = ($"Błąd generowania identyfikatora EDA dla towaru o id {ecomInventory.InventoryId}, w kanale sprzedaży {ecomChannelRef}: {ex.Message}");
        }
        else if(ecomInventory.WersjaRef > 0)  
        {
          errorMsg = ($"Błąd generowania identyfikatora EDA dla towaru o WersjaRef {ecomInventory.WersjaRef}, w kanale sprzedaży {ecomChannelRef}: {ex.Message}");
        }
        else
        {
          errorMsg = ($"Błąd generowania identyfikatora EDA dla nieokreślonego towaru, w kanale sprzedaży {ecomChannelRef}: {ex.Message}");
        }
    
        return new GetEDAIdentifierData(){Result = false, ErrorMsg = errorMsg, EDAIdentifier = null};
      }    
      return new GetEDAIdentifierData(){Result = true, ErrorMsg = "", EDAIdentifier = EDAInventoryId};
    }
    
    


    /// <summary>
    /// Handler zdarzenia bazodanowego zmiany na tabeli TOWARY, 
    /// wywołuje metodę UpdateInventoryRequest sprawdzającą status towaru w kanalach sprzedaży. 
    /// </summary>
    /// <param name="context"></param>
    [CustomData("MessageHandlerType=E")]
    [UUID("45b557ab42e743549fc4ac51e92d9679")]
    virtual public HandlerResult InventoryChangedHandler(ConsumeContext<ECOM.InventoryChanged> context)
    {
      try
      {    
        ECOM.WERSJE version = new ECOM.WERSJE();
        version.FilterAndSort($"{nameof(ECOM.WERSJE)}.{version.KTM.Symbol} = '{context.Message.Ktm}'");
        if(!version.FirstRecord())
        {
          throw new Exception($"Nie znaleziono wersji towari o ktm : {context.Message.Ktm}");
        }
    
        //UpdateInventoryRequest(context.Message.Ktm, wersja.REF.AsInteger);
        var command = new CheckInventoryChangedCommand()     
        {      
          VersionRef = version.REF.AsInteger,
          Ktm = context.Message.Ktm
        };
        command.SetDeduplicationIdentifier("INVENTORY_" + version.REF);
        command.SetIdentifier("INVENTORY_"+version.REF);
        EDA.SendCommand("ECOM.ECOMINVENTORIES.CheckInventoryChangedCommandHandler",command);
        version.Close();
      }
      catch(Exception ex)
      {
        context.Message.Message = ex.Message;
        throw new Exception(ex.Message);
      }
      return HandlerResult.Handled;  
    }
    
    


    /// <summary>
    /// Metoda sprawdza czy dane towaru są aktualne uruchamiając metodę wyliczania sumy kontrolnej dla towaru.
    /// Jeśli jest nieaktualny to zaznacza, że towar w kanale sprzedaży jest nieaktualny i zwraca true
    /// 
    /// parametry: 
    /// orderRef - ref zamówienia z tabeli ECOMORDERS
    /// connectorGroupName - nazwa grupy konektroa z tabeli SYS_EDACONNECTORS
    /// connectorSymbol - symbol konektroa z tabeli SYS_EDACONNECTORS
    /// 
    /// zwracane:
    /// wartość bool mówiąca, czy status wymaga aktualizacji w kanale sprzedaży (true = wymaga)
    /// </summary>
    /// <param name="inventoryRef"></param>
    /// <param name="inventoryData"></param>
    [UUID("66e8ccd569fe4f5397ff0eecc272e885")]
    public static bool CheckInventoryUpdate(int inventoryRef, EcomInventoryInfo inventoryData)
    {
      var ecomInventories = new ECOM.ECOMINVENTORIES(); 
      ecomInventories.FilterAndSort($"{nameof(ECOM.ECOMINVENTORIES)}.{ecomInventories.REF.Symbol} = {inventoryRef}");
      if(!ecomInventories.FirstRecord())
      {
        throw new Exception($"Nie znaleziono wpisu w towarach kanałów sprzedaży o ref: {inventoryRef.ToString()}");
      }
      //jest więc aktualizujemy jeżeli suma kontrolna jest inna niż wyliczona     
      //wyliczenie sumy kontrolnej towaru
      string newInventoryCheckSum = GetInventoryCheckSum(inventoryData);
    
      if(ecomInventories.INVENTORYCHECKSUM != newInventoryCheckSum)
      { 
        ecomInventories.EditRecord();   
        ecomInventories.INVENTORYCHECKSUM = newInventoryCheckSum;           
        ecomInventories.SYNCSTATUS = (int)EcomSyncStatus.ExportPending; 
        if(!ecomInventories.PostRecord())
        {
          throw new Exception($"Błąd aktualizacji statusu towaru o REF: {inventoryRef}");
        }	 
        ecomInventories.Close();
        return true;    
      }
      else
      {
        ecomInventories.Close();
        return false;
      }
    }
    
    


    /// <summary>
    /// Metoda wylicza sumę kontrolną z klasy EcomInventoryInfo z wykorzystaniem szyfrowania MD5.
    /// </summary>
    /// <param name="inventoryData"></param>
    [UUID("6e527b3937f746618ec6fd31b8f1faad")]
    public static string GetInventoryCheckSum(EcomInventoryInfo inventoryData)
    {  
      //Serializujemy klasę do JSONa
      string stringToHash = JsonConvert.SerializeObject(inventoryData);
      string hash;
      //Z uzyskanego stringa Wyliczamy hash MD5 i  bierzemy 20 ostatnich znaków
      using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create()) 
      {
        hash = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(stringToHash))).Replace("-", String.Empty);
      }  
      return hash.Remove(0, 12);
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("7772741238e54f35a6173dcfd8185201")]
    [Wait(5000)]
    virtual public HandlerResult CheckInventoryChangedCommandHandler(ConsumeContext<ECOM.CheckInventoryChangedCommand> context)
    {
      if (context.ShouldIgnore())
      {
        return HandlerResult.Ignored;  
      }
      else
      {
        LOGIC.ECOMINVENTORIES.CheckInventoryChanged(context.Message.Ktm, context.Message.VersionRef);  
        return HandlerResult.Handled;
      }
    }
    
    


    /// <summary>
    /// Metoda uruchamiana w handlerze komendy CheckInventoryChangedCommand, która wykonywana jest w reakcji na eventy zmian na towarze
    /// Sprawdza czy towar jest w kanałach sprzedaży, a następnie:
    /// - jezeli jest w kanale i TOWARY.WITRYNA = 1, to ustawia do aktualizacji
    /// - jeżeli nie ma  i TOWARY.WITRYNA = 1 to dodaje w kanale i ustawia do aktualizacji
    /// - jeżeli jest i witryna = 0 to ukrywa produkt w kanałach (nie kasuje)
    /// - jeżeli nie ma i witryna= 0, to nic nie robi
    /// </summary>
    /// <param name="ktm"></param>
    /// <param name="wersjaRef"></param>
    /// <param name="channelRef"></param>
    [UUID("7b10e56dfa6841009db1a90df401276f")]
    public static void CheckInventoryChanged(string ktm, int wersjaRef, int? channelRef = null)
    {  
      //szukamy towaru we wszystkich kanałach sprzedaży, żeby przeliczyć sumę kontrolną  
      string sqlString = 
        @"select ein.ecominventoryid, ein.ref ecominvref , scon.groupname, scon.symbol, scon.ref connectorref, 
            echa.ref channel, ein.syncstatus
          from ecomchannels echa " + 
            "join ecominventories ein on ein.ecomchannelref = echa.ref " +
              $"and ein.wersjaref = 0{wersjaRef} " +          
            "left join sys_edaconnectors scon on scon.ref = echa.connector " +
          (channelRef == null ? "" :  $"where echa.ref = {channelRef}");
    
           
      var inventory = new ECOM.TOWARY();
      inventory.FilterAndSort($"{nameof(ECOM.TOWARY)}.{inventory.KTM.Symbol} = '{ktm}'");
      if(!inventory.FirstRecord())
      {
        throw new Exception($"Nie znaleziono towaru o ktm: {ktm}");
      }
    
      //sprawdzamy każdy kanal sprzedaży
      foreach(var dataRow in CORE.QuerySQL(sqlString))
      {            
        if(string.IsNullOrEmpty(dataRow["ECOMINVREF"]))
        {
          throw new Exception("Weszło w dodawanie");      
        }
        else if(dataRow["SYNCSTATUS"].AsInteger != (int)EcomSyncStatus.ExportPending 
          && dataRow["SYNCSTATUS"].AsInteger != (int)EcomSyncStatus.Unsynchronizable)
        {    
          //towar jest w kanale sprzedazy i nie ma ustawionego statusu mówiacego, żeby nie synchronizować, 
          //ani że już czeka w kolejce 
          
          //wyliczamy dane EcomInventoryInfo, z którego potem liczymy checksume
          var methodsObject = LOGIC.ECOMCHANNELS.FindMethodObject(dataRow["CHANNEL"].AsInteger);
          var inventoryInfo = methodsObject.Logic.GetInventoryInfo(dataRow["ECOMINVREF"].AsInteger64);
    
          //metoda liczy checksume i w razie potrzeby ustawia status towaru w kanale spzredazy na "Do eksportu"
          //w odróżnieniu od wysyłki statusuów, kóte muszą iść, po kolei 
          //resztę wysyłki zrealizuje metoda schedulera ew. kliknięcie przez operatora przycisku Wysyłki nieaktulnych towarów 
          LOGIC.ECOMINVENTORIES.CheckInventoryUpdate(dataRow["ECOMINVREF"].AsInteger, inventoryInfo); 
        }    
      } 
      inventory.Close();
    }
    
    


    /// <summary>
    /// Handler zdarzenia bazodanowego zmiany na tabeli TOWKODKRESK, 
    /// wywołuje metodę UpdateInventoryRequest sprawdzającą status towaru w kanalach sprzedaży. 
    /// </summary>
    /// <param name="context"></param>
    [CustomData("MessageHandlerType=E")]
    [UUID("7ebdfef0c44d4844a24ac81c5f9022e7")]
    virtual public HandlerResult BarcodeChangedHandler(ConsumeContext<ECOM.BarcodeChanged> context)
    {
      try
      {    
        var command = new ECOM.CheckInventoryChangedCommand()     
        {      
          VersionRef = context.Message.WersjaRef,
          Ktm = context.Message.Ktm
        };
        command.SetDeduplicationIdentifier("INVENTORY_" + context.Message.WersjaRef.ToString());
        command.SetIdentifier("INVENTORY_" + context.Message.WersjaRef.ToString());
        EDA.SendCommand("ECOM.ECOMINVENTORIES.CheckInventoryChangedCommandHandler",command);
      }
      catch(Exception ex)
      {
        context.Message.Message = ex.Message;
        throw new Exception(ex.Message);
      }
      return HandlerResult.Handled; 
    }
    
    


    /// <summary>
    /// Handler zdarzenia bazodanowego zmiany na tabeli WERSJE, 
    /// wywołuje metodę UpdateInventoryRequest sprawdzającą status towaru w kanalach sprzedaży. 
    /// </summary>
    /// <param name="context"></param>
    [CustomData("MessageHandlerType=E")]
    [UUID("82b0e62414a2482ea88db757d30b4389")]
    virtual public HandlerResult VersionChangedHandler(ConsumeContext<ECOM.VersionChanged> context)
    {
      try
      {    
        var command = new ECOM.CheckInventoryChangedCommand()     
        {      
          VersionRef = context.Message.WersjaRef,
          Ktm = context.Message.Ktm
        };
        command.SetDeduplicationIdentifier("INVENTORY_" + context.Message.WersjaRef.ToString());
        command.SetIdentifier("INVENTORY_" + context.Message.WersjaRef.ToString());
        EDA.SendCommand("ECOM.ECOMINVENTORIES.CheckInventoryChangedCommandHandler",command);
      }
      catch(Exception ex)
      {
        context.Message.Message = ex.Message;
        throw new Exception(ex.Message);
      }
      return HandlerResult.Handled;
    }
    
    


    /// <summary>
    /// Metoda zwraca listę symboli dla wybranego towaru we wszystkich kanałach sprzedaży z przypisanym danycm konektorem.
    /// Używana przy generowaniu danych do eksportu towaru do kanału sprzedaży
    /// </summary>
    /// <param name="inventoryVersion"></param>
    /// <param name="connector"></param>
    [UUID("9089f12665d14b34a05f24c1172c1a85")]
    public static List<string> GetEcomChannelsSymbolsForInventory(int inventoryVersion, int connector)
    {
      string getSymbolSqlString =
        @"select distinct ei.ecominventoryid
            from ecominventories ei
            join ecomchannels ec on ec.ref = ei.ecomchannelref " +
                $"and ec.connector = 0{connector} " +
            $"where ei.wersjaref = 0{inventoryVersion} " +        
            @"and coalesce(ei.ecominventoryid, '') != ''";
      var symbols = new List<string>();  
      foreach(var dataRow in CORE.QuerySQL(getSymbolSqlString))
      {    
        symbols.Add(dataRow["ECOMINVENTORYID"]);       
      }  
      return symbols;
    }
    
    


    /// <summary>
    /// Handler zdarzenia bazodanowego zmiany na tabeli TOWJEDN 
    /// </summary>
    /// <param name="context"></param>
    [CustomData("MessageHandlerType=E")]
    [UUID("9cedacc136204c3aabd0a83b7de27a27")]
    virtual public HandlerResult UnitChangedHandler(ConsumeContext<ECOM.UnitChanged> context)
    {
      try
      {    
        ECOM.WERSJE version = new ECOM.WERSJE();
        version.FilterAndSort($"{nameof(ECOM.WERSJE)}.{version.KTM.Symbol} = '{context.Message.Ktm}'");
        if(!version.FirstRecord())
        {
          throw new Exception("Nie znaleziono towaru o ktm: " + context.Message.Ktm);
        }
        //UpdateInventoryRequest(context.Message.Ktm, wersja.REF.AsInteger);
        var command = new CheckInventoryChangedCommand()     
        {      
          VersionRef = version.REF.AsInteger,
          Ktm = context.Message.Ktm
        };
        command.SetDeduplicationIdentifier("INVENTORY_" + version.REF);
        command.SetIdentifier("INVENTORY_" + version.REF);
        EDA.SendCommand("ECOM.ECOMINVENTORIES.CheckInventoryChangedCommandHandler",command);
        version.Close();
      }
      catch(Exception ex)
      {
        context.Message.Message = ex.Message;
        throw new Exception(ex.Message);
      }
      return HandlerResult.Handled;
    }
    
    


    /// <param name="f"></param>
    [UUID("d00747e7bea540e0ae3b61590ad26f2f")]
    virtual public bool XkIsFileImage(FileChanged f)
    {
      //Metoda do nadpisania przez wdrożeniowca, ma rozstrzygać czy przekazany plik jest zdjęciem.
      return true;
    }
    
    
    


  }
}
