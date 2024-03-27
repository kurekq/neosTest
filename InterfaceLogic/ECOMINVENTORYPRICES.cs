
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
//ERRSOURCE: ECOM.ECOMINVENTORYPRICES

  public partial class ECOMINVENTORYPRICES
  {
    [UUID("00dd20f875954f70af0b6fac0368e03e")]
    virtual public void CalculateAllInventoryPrices()
    {
      try
      {    
        var calculateInventoryPricesMsg = LOGIC.ECOMCHANNELS.CalculateInventroryPrices(CalculateMode.All, _ecomchannelref.AsInteger);
    
        if(!string.IsNullOrEmpty(calculateInventoryPricesMsg))
        {
          GUI.ShowBalloonHint(calculateInventoryPricesMsg,"", IconType.WARNING);   
        }
        else
        {
          GUI.ShowBalloonHint("Naliczono ceny w kanale sprzedaży","", IconType.INFORMATION);   
        }         
      }
      catch(Exception ex)
      {
        GUI.ShowBalloonHint($"Błąd naliczania cen towarów: {ex.Message}" ,"", IconType.STOP);    
      }
      this.RefreshData();
    }
    
    

    [UUID("246660ed8f8b4948a016b5065119894a")]
    virtual public object OpenPriceInAdminPanel()
    {  
      try
      {    
        var ecomChannel = new ECOM.ECOMCHANNELS();
        ecomChannel.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannel.REF.Symbol} = 0{_ecomchannelref}");
        if(!ecomChannel.FirstRecord())
        {
          throw new Exception("Nie znaleziono kanału sprzedaży o ref: " + _ecomchannelref);
        }
        string adapterName = LOGIC.ECOMORDERS.GetAdapterForConnector(ecomChannel.CONNECTOR.AsInteger);
        var adapter = GUI.CreateObject(adapterName);
        (adapter as ECOMADAPTERCOMMON).DoOpenPriceInAdminPanel(ecomChannel.CONNECTOR.AsInteger, this.REF.AsInteger);
        ecomChannel.Close();
      }
      catch(Exception ex)
      {
        GUI.ShowBalloonHint($"Błąd otwarcia panelu adminstratora witryny dla ceny towaru o ref {this.REF}:{ex.Message}.","", IconType.STOP);      
      }
      return "";
    }
    
    

    [UUID("250b4c70db564a2baa87652b0c50637f")]
    virtual public void CalculateAllInventoryPricesCurrenciesDict()
    {  
      var c = new Contexts();
      c.Add("_ecomchannel", _ecomchannelref);
      GUI.ShowForm(typeof(ECOM.WALUTY).ToString(), "DICT", c);  
    }
    
    

    [UUID("264127c1f2b14613bf6f6766f73b8eb4")]
    virtual public string SetBROWSEFormDBFilter()
    {
      if(string.IsNullOrEmpty(_ecomchannelref))
      {
        return "";
      }
      else
      {
        return $"ei.ECOMCHANNELREF = 0{_ecomchannelref.ToString()}" ;
      }
    }
    
    

    [UUID("3c32e9adfe7b4498b0e32d396f9bbe9d")]
    virtual public void ShowEDASyncsForPrices()
    {  
      var ecomChannel = new ECOM.ECOMCHANNELS();
      ecomChannel.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannel.REF.Symbol} = 0{_ecomchannelref}");
      if(ecomChannel.FirstRecord())
      {
        var c = new Contexts();     
        c.Add("_connector", ecomChannel.CONNECTOR);
        c.Add("_messagetype", "ECOM.ExportInventoryPricesCommand");  
        var monitorForm = GUI.ShowForm("SYSTEM.EDAMONITOR", "BROWSE", c);    
      }
      else
      {
        ShowBalloonHint($"Nie znaleziono kanału sprzedaży o REF: {_ecomchannelref}.", "", IconType.STOP);
      }  
      ecomChannel.Close();
    }
    
    

    [UUID("49a79ab3ca3f40db806c23d023407488")]
    virtual public void OperatorSetSelectedInventoryPricesAsInvalid()
    {
      string result = "";    
      var ecomInventoryPrice = new  ECOM.ECOMINVENTORYPRICES();
      foreach (var price in SelectedRowsOrCurrent)
      {
        try
        {  
          ecomInventoryPrice.FilterAndSort($"{nameof(ECOM.ECOMINVENTORYPRICES)}.{ecomInventoryPrice.REF.Symbol} = 0{price["REF"]}");
          if(ecomInventoryPrice.FirstRecord())          
          {
            ecomInventoryPrice.EditRecord();
            ecomInventoryPrice.ISVALID = 0;      
            if(!ecomInventoryPrice.PostRecord())
            {
              throw new Exception($"Błąd ustawienia ceny jako nieaktualnej o REF: {price["REF"]}. Błąd zapisu do bazy danych");
            }	       
          }
          else
          {
            result += $"Błąd oznaczania ceny o REF: {price["REF"]} do wysłania\n";
            continue;    
          }
        }
        catch(Exception ex)
        {
          result += $"Błąd oznaczania ceny o REF: {price["REF"]} do wysłania: {ex.Message}\n";
          continue;
        }       
      }
    
      if(string.IsNullOrEmpty(result))
      {
        GUI.ShowBalloonHint($"Oznaczono zaznaczone ceny jako nieaktualne","", IconType.INFORMATION);      
        this.RefreshData();
      }
      else
      {
        GUI.ShowBalloonHint($"Błąd oznaczania cen: {result}","", IconType.STOP);       
      }
      ecomInventoryPrice.Close();
      DeselectAllRecords(); 
      return;
    }
    
    

    [UUID("95543a8135154f298c0f010898e8228d")]
    virtual public void SendChangedInventoriesPrices()
    { 
      try
      {       
        var sendInventoryPicesMsg = LOGIC.ECOMCHANNELS.SendInventoryPrices(_ecomchannelref.AsInteger, 
          ExportMode.LastChange);
        if(!string.IsNullOrEmpty(sendInventoryPicesMsg))
        {
          GUI.ShowBalloonHint(sendInventoryPicesMsg,"", IconType.WARNING);       
        }
        else
        {
          GUI.ShowBalloonHint($"Wygenerowano komendę wysyłki cen towarów w kanale sprzedaży {_ecomchannelref}","", IconType.INFORMATION); 
        }
      }
      catch(Exception ex)
      {
        GUI.ShowBalloonHint($"Błąd generowania komend wysyłki towarów dla kanału {_ecomchannelref}:\n{ex.Message}","", 0);        
      } 
    }
    
    

    [UUID("9a260e136d6a48e1a7cd36384ccb8cd2")]
    virtual public void OperatorCalculateInvalidInventoryPrices()
    {  
      try
      {    
        var calculateInventoryPricesMsg = LOGIC.ECOMCHANNELS.CalculateInventroryPrices(CalculateMode.Invalid, _ecomchannelref.AsInteger);
    
        if(!string.IsNullOrEmpty(calculateInventoryPricesMsg))
        {
          GUI.ShowBalloonHint(calculateInventoryPricesMsg,"", IconType.WARNING);   
        }
        else
        {
          GUI.ShowBalloonHint("Naliczono nieaktualne ceny w kanale sprzedaży","", IconType.INFORMATION);   
        }         
      }
      catch(Exception ex)
      {
        GUI.ShowBalloonHint($"Błąd naliczania cen towarów: {ex.Message}" ,"", IconType.STOP);   
      }
      DeselectAllRecords(); 
      this.RefreshData();      
    }
    
    

    [UUID("ac8efc90213649d58a8960bbf0717f39")]
    virtual public void OperatorSendChangedInventoryPrices()
    {
      ShowMessageBox($"Czy wysłać dane nieaktualnych cen towarów?", 
      "Wysyłka nieaktualnych cen towarów", IconType.QUESTION,
      Actions.SendChangedInventoriesPrices, Actions.CreateAction("Nie", "ICON_3"));   
    } 
    
    

    /// <summary>
    /// automatycznie wygenerowana metoda na inicjalizację pola REF
    /// </summary>
    [UUID("b037c809da374f169062c91b30b9cfe8")]
    virtual public string InitializeREF()
    {
      return GenRef();
    }
    
    
    

    [UUID("b6bf6a72fe764e1bbd16f935b4322a56")]
    virtual public void OperatorSendSelectedInventoryPrices()
    {
      int questionCnt = 5;
      if(SelectedRowsOrCurrent.Count >= questionCnt)
      {
        ShowMessageBox($"Czy wysłać dane dla {SelectedRows.Count.ToString()} cen towarów?", 
          "Aktualizowanie cen towarów", IconType.QUESTION,
          Actions.SendSelectedInventoryPrices, Actions.CreateAction("Nie", "ICON_3"));
      }
      else if(SelectedRowsOrCurrent.Count > 0)
      {    
        SendSelectedInventoryPrices();
        DeselectAllRecords(); 
      }
      else
      {
        GUI.ShowBalloonHint("Nie wybrano cen towarów do wysłania.", "", IconType.WARNING);
      }  
    }
    
    

    [UUID("be1dff304f2746d3940707c6a32aeb01")]
    virtual public void OperatorCalculateAllInventoryPrices()
    {
         ShowMessageBox(@"Czy naliczyć ponownie ceny dla wszystkich towarów w kanale sprzedaży? "
          + @"Naliczanie może potrwać kilka minut.", 
          "Naliczanie cen towarów", IconType.QUESTION,
          Actions.CalculateAllInventoryPrices, Actions.CalculateAllInventoryPricesCurrenciesDict, Actions.CreateAction("Nie", "ICON_3"));  
    }
    
    

    [UUID("dffe2a4372eb4d72a8d84d92519f8b5b")]
    virtual public void SendSelectedInventoryPrices()
    {
      var inventoryPricesList = new List<Int64>(); 
      var ecomInventoryPrices = new ECOM.ECOMINVENTORYPRICES(); 
      var ecominventory_logic = new ECOM.LOGIC.ECOMINVENTORIES();
      
      foreach (var item in SelectedRowsOrCurrent)
      {
        ecomInventoryPrices.FilterAndSort($"{nameof(ECOM.ECOMINVENTORYPRICES)}.{ecomInventoryPrices.REF.Symbol} = 0{item["REF"]}");
        if(ecomInventoryPrices.FirstRecord())
        {
          if (ecominventory_logic.Get(ecomInventoryPrices.ECOMINVENTORYREF.AsInteger)?.ACTIVE == 1)
          {
            ecomInventoryPrices.EditRecord();
            ecomInventoryPrices.SYNCSTATUS = (int)EcomSyncStatus.ExportPending;
            if(ecomInventoryPrices.PostRecord())
            {
              inventoryPricesList.Add(Int64.Parse(item["REF"])); 
            }
            else
            {
              throw new Exception($"Błąd aktualizacji statusu ceny towaru o REF: {item["REF"]}");
            }
          } 
          else 
          {
            GUI.ShowBalloonHint($"Towar o REF: {item["REF"]} jest nieaktywny, aktualizacja niemożliwa", "", IconType.WARNING);
          }    
        }
        else
        {
          throw new Exception($"Nie znaleziono ceny towaru o REF: {item["REF"]}");
        }       
      }
      
      if(inventoryPricesList.Count > 0)
      {
        try
        {    
          var sendInventoryPricesMsg = LOGIC.ECOMCHANNELS.SendInventoryPrices(_ecomchannelref.AsInteger, ExportMode.List, inventoryPricesList);
          if(!string.IsNullOrEmpty(sendInventoryPricesMsg))
          {
            GUI.ShowBalloonHint(sendInventoryPricesMsg,"", IconType.INFORMATION);   
          }
          else
          {
            GUI.ShowBalloonHint($"Wygenerowano komendę wysyłki cen towarów w kanale sprzedaży {_ecomchannelref}","", IconType.INFORMATION); 
          }      
        }
        catch(Exception ex)
        {
          GUI.ShowBalloonHint($"Błąd generowania komend wysyłki towarów dla kanału {_ecomchannelref}:\n{ex.Message}","", IconType.STOP);
          return;
        }    
      }
      else
      {
        GUI.ShowBalloonHint("Nie wybrano aktywnych towarów do wysłania.", "", IconType.WARNING);
      } 
      ecomInventoryPrices.Close();
    }
    
    

    [UUID("f9e20e1c904741f490bc8471e36d5344")]
    virtual public void CustomActions()
    {
      
    }
  }
	//ERRSOURCE: structure CheckInventoryPriceChangedCommand { ... } in object ECOM.ECOMINVENTORYPRICES
  [CustomData("DataStructure=Y")]
  [UUID("f568cb3820fd43a4bba98a320dea72a7")]
  public class CheckInventoryPriceChangedCommand : NeosCommand
  {
    public int DefcennikRef {get;set;}
    public int WersjaRef {get;set;}
    public int TowjednRef {get;set;}
    public string Message {get; set;}    
  }
}
