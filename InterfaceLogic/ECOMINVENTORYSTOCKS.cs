
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
//ERRSOURCE: ECOM.ECOMINVENTORYSTOCKS

  public partial class ECOMINVENTORYSTOCKS
  {
    [UUID("29d0f41bc74c437399d95946910ba895")]
    virtual public string SetBROWSEFormDBFilter()
    {
      if(string.IsNullOrEmpty(_ecomchannelref))
      {
        return "";
      }
      else
      {
        return $"es.ECOMCHANNELREF = 0{_ecomchannelref.ToString()}" ;
      }
    }
    
    

    [UUID("3a3ff8dfd19b469fb3fd8e545f47e142")]
    virtual public void OperatorSetSelectedInventoryStocksAsInvalid()
    {
      string result = "";    
      var ecomInventoryStock = new  ECOM.ECOMINVENTORYSTOCKS();
      foreach (var stock in SelectedRowsOrCurrent)
      {
        try
        {   
          ecomInventoryStock.FilterAndSort($"{nameof(ECOM.ECOMINVENTORYSTOCKS)}.{ecomInventoryStock.REF.Symbol} = 0{stock["REF"]}");
          if(ecomInventoryStock.FirstRecord())         
          {
            ecomInventoryStock.EditRecord();
            ecomInventoryStock.ISVALID = 0;      
            if(!ecomInventoryStock.PostRecord())
            {
              throw new Exception($"Błąd ustawienia stanu mag. jako nieaktualney o REF: {stock["REF"]}. Błąd zapisu do bazy danych");
            }	         
          }
          else
          {
            result += $"Błąd oznaczania stanu mag. o REF: {stock["REF"]} do wysłania\n";
            continue;    
          }
        }
        catch(Exception ex)
        {
          result += $"Błąd oznaczania stanu mag. o REF: {stock["REF"]} do wysłania: {ex.Message}\n";
          continue;
        }       
      }
    
      if(string.IsNullOrEmpty(result))
      {
        GUI.ShowBalloonHint($"Oznaczono zaznaczone stany mag. jako nieaktualne","", IconType.INFORMATION);      
        this.RefreshData();
      }
      else
      {
        GUI.ShowBalloonHint($"Błąd oznaczania stanów mag.: {result}","", IconType.STOP);       
      }
      ecomInventoryStock.Close();
      return;  
    }
    
    

    [UUID("45a9fd1dfae9436fb0ce07941e509b80")]
    virtual public void SendSelectedInventoryStocks()
    {
      var inventoryStockList = new List<Int64>();  
      var ecomInventoryStock = new ECOM.ECOMINVENTORYSTOCKS(); 
      var ecominventory_logic = new ECOM.LOGIC.ECOMINVENTORIES();
     
      foreach (var item in SelectedRowsOrCurrent)
      {    
        ecomInventoryStock.FilterAndSort($"{nameof(ECOM.ECOMINVENTORYSTOCKS)}.{ecomInventoryStock.REF.Symbol} = 0{item["REF"]}");
        if(ecomInventoryStock.FirstRecord())  
        {
          if (ecominventory_logic.Get(ecomInventoryStock.ECOMINVENTORYREF.AsInteger)?.ACTIVE == 1)
          {
            ecomInventoryStock.EditRecord();
            ecomInventoryStock.SYNCSTATUS = (int)EcomSyncStatus.ExportPending;
            if(ecomInventoryStock.PostRecord())
            {
              inventoryStockList.Add(Int64.Parse(item["REF"])); 
            }
            else
            {
              throw new Exception($"Błąd aktualizacji statusu stanu mag. o REF: {item["REF"]}. Błąd zapisu do bazy danych.");
            }
          }
        }       
        else
        {
          throw new Exception("Nie znaleziono stanu magazynowego o ref: " + item["REF"]);
        }
      }
      
      if(inventoryStockList.Count > 0)
      {
        try
        {    
          var sendInventoryMsg = LOGIC.ECOMCHANNELS.SendInventoryStocks(_ecomchannelref.AsInteger, ExportMode.List, inventoryStockList);
          if(!string.IsNullOrEmpty(sendInventoryMsg))
          {
            GUI.ShowBalloonHint(sendInventoryMsg,"Wysyłka stanów magazynowych", IconType.INFORMATION);   
          }      
        }
        catch(Exception ex)
        {
          GUI.ShowBalloonHint($"Błąd generowania komend wysyłki stanów magazynowych dla kanału {_ecomchannelref}:\n{ex.Message}","Wysyłka stanów magazynowych", IconType.STOP);
          return;
        }    
      }
      else
      {
        GUI.ShowBalloonHint("Nie wybrano stanów magazynowych dla aktywnych towarów do wysłania.", "Wysyłka stanów magazynowych", IconType.STOP);
      }  
      ecomInventoryStock.Close();
      DeselectAllRecords();
    }
    
    

    [UUID("5065e90ecf114c9d854048271d5587c8")]
    virtual public void SendChangedInventoriesStocks()
    { 
      try
      {       
        var sendInventoryStocksMsg = LOGIC.ECOMCHANNELS.SendInventoryStocks(_ecomchannelref.AsInteger, 
          ExportMode.LastChange);
        if(!string.IsNullOrEmpty(sendInventoryStocksMsg))
        {
          GUI.ShowBalloonHint(sendInventoryStocksMsg,"", IconType.WARNING);       
        }
        else
        {
          GUI.ShowBalloonHint($"Wygenerowano komendę wysyłki stanów magazynowych w kanale sprzedaży {_ecomchannelref}","", IconType.INFORMATION); 
        }
      }
      catch(Exception ex)
      {
        GUI.ShowBalloonHint($"Błąd generowania komend wysyłki stanów magazynowych dla kanału {_ecomchannelref}:\n{ex.Message}","", 0);        
      } 
    }
    
    

    [UUID("532e05a5ef4440c6836001b54bea1833")]
    virtual public void OpenInventoryStockInAdminPanel()
    {
      try
      {    
        var ecomChannel = new ECOM.ECOMCHANNELS();
        ecomChannel.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannel.REF.Symbol} = 0{_ecomchannelref}");
        if(!ecomChannel.FirstRecord())
        {
          throw new Exception($"Nie znaleziono kanału sprzedaży o ref : {_ecomchannelref.ToString()}");
        }
        string adapterName = LOGIC.ECOMORDERS.GetAdapterForConnector(ecomChannel.CONNECTOR.AsInteger);
        var adapter = GUI.CreateObject(adapterName);
        (adapter as ECOMADAPTERCOMMON).DoOpenInventoryStockInAdminPanel(ecomChannel.CONNECTOR.AsInteger, this.REF.AsInteger);
        ecomChannel.Close();
      }
      catch(Exception ex)
      {
        GUI.ShowBalloonHint($"Błąd otwarcia panelu adminstratora witryny dla stanu magazynowego towaru o ref {this.REF}:{ex.Message}.","", IconType.STOP);      
      } 
    }
    
    

    [UUID("581a8f13706e4f4f974157af745425f8")]
    virtual public void OperatorSendSelectedInventoryStocks()
    {
      int questionCnt = 5;
      if(SelectedRowsOrCurrent.Count >= questionCnt)
      {
        ShowMessageBox($"Czy zaktualizować dane stanów magazynowych dla {SelectedRows.Count.ToString()} towarów?", 
          "Aktualizowanie wielu stanów magazynowych", IconType.QUESTION,
          Actions.SendSelectedInventoryStocks, Actions.CreateAction("Nie", "ICON_3"));
      }
      else if(SelectedRowsOrCurrent.Count > 0)
      {    
        SendSelectedInventoryStocks();
      }
      else
      {
        GUI.ShowBalloonHint("Nie wybrano stanów magazynowych do wysłania.", "Wysyłka stanów magazynowych", IconType.STOP);
      }  
    }
    
    

    [UUID("61611edbf31f43fc984d81791c89d89c")]
    virtual public void ShowEDASyncsForStocks()
    {   
      var ecomChannel = new ECOM.ECOMCHANNELS();
      ecomChannel.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannel.REF.Symbol} = 0{_ecomchannelref}");
      if(ecomChannel.FirstRecord())  
      {
        var c = new Contexts();     
        c.Add("_connector", ecomChannel.CONNECTOR);
        c.Add("_messagetype", "ECOM.ExportInventoryStocksCommand");  
        var monitorForm = GUI.ShowForm("SYSTEM.EDAMONITOR", "BROWSE", c);    
      }
      else
      {
        ShowBalloonHint($"Nie znaleziono kanału sprzedaży o REF: {_ecomchannelref}.", "", IconType.STOP);
      }    
      ecomChannel.Close();
    }
    
    

    [UUID("96fb11979bcc413088dacd8bbc0118b4")]
    virtual public void CalculateAllInventoryStocks()
    {
      try
      {      
        var calculateInventoryStocksMsg 
          = LOGIC.ECOMCHANNELS.CalculateInventroryStocks(CalculateMode.All, this._ecomchannelref.AsInteger);
    
        if(!string.IsNullOrEmpty(calculateInventoryStocksMsg))
        {
          GUI.ShowBalloonHint(calculateInventoryStocksMsg,"", IconType.WARNING);   
        }
        else
        {
          GUI.ShowBalloonHint("Naliczono stany magazynowe w kanale sprzedaży","", IconType.INFORMATION); 
        }         
      }
      catch(Exception ex)
      {
        GUI.ShowBalloonHint($"Błąd naliczania stanów magazynowych: {ex.Message}" ,"", IconType.STOP);   
      }
      this.RefreshData();
    }
    
    

    [UUID("a68217b4e96e416fb3028cd4650fe0c9")]
    virtual public void OperatorCalculateAllInventoryStocks()
    {
         ShowMessageBox(@"Czy naliczyć ponownie stany mag. dla wszystkich towarów w kanale sprzedaży? "
          + @"Naliczanie może potrwać kilka minut.", 
          "Naliczanie stanów magazynowych", IconType.QUESTION,
          Actions.CalculateAllInventoryStocks, Actions.CreateAction("Nie", "ICON_3"));  
    }
    
    

    [UUID("b9e4910f99964a72a6f3f66393c70981")]
    virtual public void OperatorCalculateInvalidInventoryStocks()
    {
      try
      {    
        var calculateInventoryStocksMsg 
          = LOGIC.ECOMCHANNELS.CalculateInventroryStocks(CalculateMode.Invalid, this._ecomchannelref.AsInteger);
    
        if(!string.IsNullOrEmpty(calculateInventoryStocksMsg))
        {
          GUI.ShowBalloonHint(calculateInventoryStocksMsg,"", IconType.WARNING);   
        }
        else
        {
          GUI.ShowBalloonHint("Naliczono nieaktualne stany magazynowe w kanale sprzedaży","", IconType.INFORMATION);   
        }         
      }
      catch(Exception ex)
      {
        GUI.ShowBalloonHint($"Błąd naliczania stanów magazynowych: {ex.Message}" ,"", IconType.STOP);   
      }
      this.RefreshData();
    }
    
    

    [UUID("bcbe91cbcfbc4162af33d46ffaba2d3f")]
    virtual public void OperatorSendChangedInventoryStocks()
    {
      ShowMessageBox($"Czy wysłać dane nieaktualnych stanów magazynowych?", 
      "Wysyłka nieaktualnych stanów magazynowych", IconType.QUESTION,
      Actions.SendChangedInventoriesStocks, Actions.CreateAction("Nie", "ICON_3")); 
    }
    
    

    /// <summary>
    /// automatycznie wygenerowana metoda na inicjalizację pola REF
    /// </summary>
    [UUID("cd476a28a2eb407fa15677e2a08c785a")]
    virtual public string InitializeREF()
    {
      return GenRef();
    }
  }
	//ERRSOURCE: structure CheckInventoryStockChangedCommand { ... } in object ECOM.ECOMINVENTORYSTOCKS
  [CustomData("DataStructure=Y")]
  [UUID("8a971664d3414f3aa3ab454fa744ba33")]
  public class CheckInventoryStockChangedCommand : NeosCommand
  {      
    public int WersjaRef {get; set;}
    public string DefMagaz {get; set;}
    public string Message {get; set;}
    ///<summary>pole rozszerzające dla przekazywania danych X'owych, w notacji JSON</summary>    
    public string ExtendData {get; set;}      
  }
}
