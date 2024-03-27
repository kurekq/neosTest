
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
//ERRSOURCE: ECOM.ECOMINVENTORIES

  public partial class ECOMINVENTORIES
  {
    /// <summary>
    /// Metoda uruchamiana spod przycisku, otwierająca przefiltrowane okno monitora EDA pokazujące historię synchronizacji danych zamówienia ze sklepem internetowym.
    /// </summary>
    [UUID("129a98d5af9e4415937f221f0597622f")]
    virtual public void ShowEDASyncsForInventory()
    {
      if(string.IsNullOrEmpty(this.EDAID))
      {
        GUI.ShowBalloonHint("Towar nie był jeszcze synchronizowany (Brak identyfikatora EDA).","", IconType.INFORMATION);          
      }
      else
      {
        var c = new Contexts();       
        c.Add("_identifier", this.EDAID);     
        GUI.ShowForm("SYSTEM.EDAMONITOR", "BROWSE", c);
      }    
    }
    
    

    [UUID("16c4263764284d8c9c7321932e5bedd7")]
    virtual public object OpenInventoryInAdminPanel()
    {
      try
      {    
        var ecomChannel = new ECOM.ECOMCHANNELS();
        ecomChannel.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannel.REF.Symbol} = 0{this.ECOMCHANNELREF}");
        if(!ecomChannel.FirstRecord())
        {
          throw new Exception($"Nie znaleziono kanału sprzedaży o ref : {this.ECOMCHANNELREF}");
        }
        string adapterName = LOGIC.ECOMORDERS.GetAdapterForConnector(ecomChannel.CONNECTOR.AsInteger);
        var adapter = GUI.CreateObject(adapterName);
        (adapter as ECOMADAPTERCOMMON).DoOpenInventoryInAdminPanel(ecomChannel.CONNECTOR.AsInteger, this.ECOMINVENTORYID);
        ecomChannel.Close();
      }
      catch(Exception ex)
      {
        GUI.ShowBalloonHint($"Błąd otwarcia panelu adminstratora witryny dla towaru {this.WERSJAREF_NAZWA}:{ex.Message}.","", IconType.STOP);  
        return "";
      } 
      return "";  
    }
    
    

    [UUID("16f3bf0e0bd64b15a363334061dd15d1")]
    virtual public void OperatorAddInventories()
    { 
      var c = new Contexts(); 
      c.Add("_channelref", _channelref);
      GUI.ShowForm("ECOM.ECOMADDINVENTORIES", "DICT", c);
    }
    
    

    [UUID("2026f8353d11466c82eeb9fb2ff274fa")]
    virtual public void CustomActions()
    {  
      
    }
    
    

    [UUID("5e466d1d62734952ab58a7ec2efb0a71")]
    virtual public void OperatorSendChangedInventory()
    { 
      ShowMessageBox($"Czy zaktualizować dane nieaktualnych towarów?", 
        "Aktualizowanie wielu towarów", IconType.QUESTION,
        Actions.SendChangedInventory, Actions.CreateAction("Nie", "ICON_3")); 
    }
    
    

    [UUID("61b3513636954b84b1bb43b160f4eb62")]
    virtual public void OperatorSendSelectedInventory()
    {
      int questionCnt = 5;
      if(SelectedRowsOrCurrent.Count >= questionCnt)
      {
        ShowMessageBox($"Czy zaktualizować dane {SelectedRows.Count.ToString()} towarów?", 
          "Aktualizowanie wielu towarów", IconType.QUESTION,
          Actions.SendSelectedInventory, Actions.CreateAction("Nie", "ICON_3"));
      }
      else if(SelectedRowsOrCurrent.Count > 0)
      {    
        SendSelectedInventory();
      }
      else
      {
        GUI.ShowBalloonHint("Nie wybrano towarów do wysłania.", "Wysyłka towarów", IconType.STOP);
      }  
    }
    
    

    /// <summary>
    /// automatycznie wygenerowana metoda na inicjalizację pola REF
    /// </summary>
    [UUID("646de37c47744652bcdb2092ee4bd7d3")]
    virtual public string InitializeREF()
    {
      return GenRef();
    }
    
    
    

    [UUID("768944cccdfb4d7c97bc78603244b9af")]
    virtual public void SendAllInventory()
    {
      var ecomInventory = new ECOM.ECOMINVENTORIES();
      var sendInventoryMsg = "";
     
      ecomInventory.FilterAndSort($"{nameof(ECOM.ECOMINVENTORIES)}.{ecomInventory.ECOMCHANNELREF.Symbol} = 0{_channelref}");
      if(ecomInventory.FirstRecord())
      {
        do
        {
          try
          {
            ecomInventory.EditRecord();
            ecomInventory.SYNCSTATUS = (int)EcomSyncStatus.ExportPending;
            if(!ecomInventory.PostRecord())
            {
              throw new Exception($"Błąd aktualizacji statusu towaru o REF: {ecomInventory.WERSJAREF}");
            }	
          }
          catch(Exception ex)
          {
            sendInventoryMsg += $"Błąd wysyłki towaru: {ecomInventory.WERSJAREF} w kanale {_channelref}:\n{ex.Message}";
          }
        } while (ecomInventory.NextRecord());
    
        try
        {
          sendInventoryMsg = LOGIC.ECOMCHANNELS.SendInventory(_channelref.AsInteger, ExportMode.All) + "\n" + sendInventoryMsg; 
        
        }
        catch(Exception ex)
        {
          sendInventoryMsg = $"Błąd wysyłania komend wysyłki towaru: {ecomInventory.WERSJAREF} w kanale {_channelref}:\n"
            + ex.Message + "\n" + sendInventoryMsg;  
        }
      }
      else
      {
        sendInventoryMsg = $"NIe znaleziono towarów do wysyłki w kanale {_channelref}.";     
      }
    
      if(!string.IsNullOrEmpty(sendInventoryMsg))
      {
        GUI.ShowBalloonHint(sendInventoryMsg,"Wysyłka towarów", IconType.WARNING);   
      }
      ecomInventory.Close();
    }
    
    

    [UUID("8c395272fd894d02af8005dddba27d67")]
    virtual public void ShowEDAInvalidSyncsForInventories()
    {  
      var c = new Contexts();     
      c.Add("_connector", this.ECOMCHANNELREF_CONNECTOR.ToString());      
      var monitorForm = GUI.ShowForm("SYSTEM.EDAMONITOR", "BROWSE", c);
      //[ML] odpalenie okna nieprzetworzonych komunikatów z monitora przez wskazanie id widoku
      //dopoki ZRT nie zrealizuje zlecenia otwierania po nazwie widoku
      monitorForm.LoadViewSettings("a2d7b50abe594089983ca7b845679ede");  
    }
    
    

    [UUID("9cb08e90f98d410d97ebc9797b6ba439")]
    virtual public void SendSelectedInventory()
    {
      var inventoryList = new List<Int64>();  
      var ecomInventory = new ECOM.ECOMINVENTORIES();
      var ecominventory_logic = new ECOM.LOGIC.ECOMINVENTORIES(); 
     
      foreach (var item in SelectedRowsOrCurrent)
      {    
        ecomInventory.FilterAndSort($"{nameof(ECOM.ECOMINVENTORIES)}.{ecomInventory.REF.Symbol} = 0{item["REF"]}");
        if(ecomInventory.FirstRecord())
        {
          ecomInventory.EditRecord();
          ecomInventory.SYNCSTATUS = (int)EcomSyncStatus.ExportPending;
          if(ecomInventory.PostRecord())
          {
            inventoryList.Add(Int64.Parse(item["REF"])); 
          }
          else
          {
            GUI.ShowBalloonHint($"Błąd generowania komendy wysyłki towaru o ref {item["REF"]}.", "Wysyłka towarów", IconType.STOP);
          }
        } 
        else
        {
          GUI.ShowBalloonHint($"Nie znaleziono towaru o REF: {item["REF"]}", "Wysyłka towarów", IconType.STOP);
        }      
      }
    
      if(inventoryList.Count > 0)
      {
        try
        {    
          var sendInventoryMsg = LOGIC.ECOMCHANNELS.SendInventory(_channelref.AsInteger, ExportMode.List, inventoryList);
          if(!string.IsNullOrEmpty(sendInventoryMsg))
          {
            GUI.ShowBalloonHint(sendInventoryMsg,"Wysyłka towarów", IconType.INFORMATION);   
          }      
        }
        catch(Exception ex)
        {
          GUI.ShowBalloonHint($"Błąd generowania komend wysyłki towarów dla kanału {_channelref}:\n{ex.Message}","Wysyłka towarów", IconType.STOP);
          return;
        }    
      }
      else
      {
        GUI.ShowBalloonHint("Nie wybrano aktywnych towarów do wysłania.", "Wysyłka towarów", IconType.STOP);
      }  
      DeselectAllRecords(); 
      ecomInventory.Close();
    }
    
    

    [UUID("e16f3bf6131c4aedb22ff85380f1b35e")]
    virtual public void OperatorSendAllInventory()
    {
      ShowMessageBox($"Czy zaktualizować dane wszystkich towarów w kanale sprzedaży?", 
        "Aktualizowanie wszystkich towarów", IconType.QUESTION,
        Actions.SendAllInventory, Actions.CreateAction("Nie", "ICON_3"));  
    }
    
    

    [UUID("eb93363082744e509765ff712770f402")]
    virtual public string SetSyncInventoryLabel()
    {
      return this.ACTIVE == 1 ? "Deaktywuj" : "Aktywuj";
    }
    
    

    [UUID("ed5d373e55ab42c5ae807c919de1039e")]
    virtual public void SendChangedInventory()
    {  
      try
      {       
        var sendInventoryMsg = LOGIC.ECOMCHANNELS.SendInventory(_channelref.AsInteger, ExportMode.LastChange);
        if(!string.IsNullOrEmpty(sendInventoryMsg))
        {
          GUI.ShowBalloonHint(sendInventoryMsg,"Wysyłka towarów", IconType.INFORMATION);   
        }
      }
      catch(Exception ex)
      {
        GUI.ShowBalloonHint($"Błąd generowania komend wysyłki towarów dla kanału {_channelref}:\n{ex.Message}","Wysyłka towarów", IconType.STOP);        
      }
      DeselectAllRecords();        
    }
    
    

    [UUID("f94be3f151a94bd4802e370753bdc341")]
    virtual public void ToggleActiveInventory()
    {
      var inventory = new ECOMINVENTORIES();
      foreach (var item in SelectedRowsOrCurrent)
      {    
        try
        { 
          inventory.FilterAndSort($"{nameof(ECOM.ECOMINVENTORIES)}.{inventory.REF.Symbol} = 0{item["REF"]}");
          if(inventory.FirstRecord())     
          { 
            inventory.EditRecord();
            inventory.ACTIVE = inventory.ACTIVE == 1 ? 0 : 1;
            inventory.SYNCSTATUS = (int)EcomSyncStatus.ExportPending;                 
            if(!inventory.PostRecord())
            {
              throw new Exception($"Błąd aktualizacji statusu towaru o REF: {item["REF"]}");
            }	
          }
          else
          {
            throw new Exception($"Nie znaleziono towaru o REF: {item["REF"]}");
          }
        }
        catch(Exception ex) 
        {
          throw new Exception($"Błąd przy zmianie widoczności wersji towaru {inventory.WERSJAREF}: " + ex.Message);
        }    
      }
      inventory.Close();
      this.RefreshData();
    }
  }
	//ERRSOURCE: structure CheckInventoryChangedCommand { ... } in object ECOM.ECOMINVENTORIES
  [CustomData("DataStructure=Y")]
  [UUID("7490bb16ea744192bdf33e13318b7386")]
  public class CheckInventoryChangedCommand : NeosCommand
  {
      public int VersionRef {get; set;}
      public string Ktm {get; set;}
      public string Message {get; set;}
  }
  
  
	//ERRSOURCE: structure ImagesForInventoryDataRow { ... } in object ECOM.ECOMINVENTORIES
  /// <summary>
  /// Struktura danych wykorzystywana do zwracania danych z metody ImagesForInventoryLogic wykorzystywanej przy wysysłaniu zdjęć do kanału sprzedaży
  /// - ImageNumber - kolejny numer zdjęcia w Teneum
  /// - FilePath - ścieżka do pliku zdjęcia
  /// 
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("d3c0c26995814a96879dd12433507236")]
  public class ImagesForInventoryDataRow 
  {
      public int ImageNumber {get; set;}
      public string FilePath {get; set;}
  }
}
