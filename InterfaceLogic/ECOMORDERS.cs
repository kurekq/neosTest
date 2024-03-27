
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
//ERRSOURCE: ECOM.ECOMORDERS

  public partial class ECOMORDERS
  {
    [UUID("0ef97ee6b34f44199575a9528650ec54")]
    virtual public bool IsOperatorShowGetOrdersConfirmationEnabled()
    {
      if(this.formmode.AsInteger == (int)importMode.DateRange)
      {
        if (this.datebegin.AsDateTime == default || (IsDateToVisible() && this.dateend.AsDateTime == default))
        {
          return false;
        }   
      }
      if(this.formmode.AsInteger == (int)importMode.OrderId && this.orderid.Empty)
      {
        return false;
      }
      return true;
    }
    
    

    /// <summary>
    /// Metoda uruchamiania z menu interfejsu inicjującal proces pobrania wszystkich zamówień z kanału sprzedaży
    /// </summary>
    [UUID("0fd94fe4756c47d1bbe1a47981d4c4c6")]
    virtual public void GetAllOrders()
    { 
      try
      {
        var getOrdersMsg = LOGIC.ECOMCHANNELS.GetOrders(_ecomchannelref.AsInteger, importMode.All);
        if(!string.IsNullOrEmpty(getOrdersMsg))
        {
          GUI.ShowBalloonHint(getOrdersMsg, "Pobieranie zamówień", IconType.INFORMATION);   
        }     
      }
      catch(Exception ex)
      {
        ShowBalloonHint($"Błąd generowania komendy pobierania zamówień dla kanału sprzedaży {_ecomchannelref}: {ex.Message}", "Pobieranie zamówień", IconType.STOP); 
      }
    }
    
    

    [UUID("12f8c3045f894b7685958119f1494d23")]
    virtual public void OperatorSendInvoice()
    { 
      int questionCnt = 1;
      if(SelectedRowsOrCurrent.Count > questionCnt)
      {
        ShowMessageBox($"Czy wysłać faktury dla {SelectedRows.Count.ToString()} zamówień?", 
          "Wysyłka wielu faktur", IconType.QUESTION,
          Actions.SendSelectedOrderInvoices, Actions.CreateAction("Nie", "ICON_3"));
      }
      else if(SelectedRowsOrCurrent.Count > 0)
      {    
        SendSelectedOrderInvoices();
      }
      else
      {
        GUI.ShowBalloonHint("Nie wybrano zamówień do wysłania faktur.", "Wysyłka faktur", IconType.STOP);
      }
    }  
    
    

    /// <summary>
    /// Metoda interfejsowa otwierająca przefiltrowane okno monitora EDA wyświetlające błędne komunikaty związane z synchroniozacją zamówienia i jego statusu
    /// </summary>
    [UUID("13cb9b41677148b88a488c37c7e0f6eb")]
    virtual public void ShowEDAInvalidSyncsForOrders()
    {
    
          var c = new Contexts();
          //c.Add("_identifier", this.EDAID);     
          //c.Add("_messagetype", "ECOM.ImportOrderCommand");
          c.Add("_connector", this.ECOMCHANNELREF_CONNECTOR.ToString());
          //SYSTEM.EDAMONITOR s  = new SYSTEM.EDAMONITOR;
          //s._connector = this.ECOMCHANNELREF_CONNECTOR
          var monitorForm = GUI.ShowForm("SYSTEM.EDAMONITOR", "BROWSE", c);
          //[ML] odpalenie okna nieprzetworzonych komunikatów z monitora przez wskazanie id widoku
          //dopoki ZRT nie zrealizuje zlecenia otwierania po nazwie widoku
          monitorForm.LoadViewSettings("a2d7b50abe594089983ca7b845679ede");
      
    }
    
    

    /// <summary>
    /// Metoda wykonacza akcji
    /// </summary>
    [UUID("15ab72d3cab74c9aa0897a3118c47e2b")]
    virtual public void OperatorGetOrderByOrderId()
    {  
      var channel = new ECOM.ECOMCHANNELS();
      channel.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{channel.REF.Symbol} = 0{_ecomchannelref}");
      if(channel.FirstRecord())  
      {
        if(channel.IMPORTORDERBLOCK == "0" || String.IsNullOrEmpty(channel.IMPORTORDERBLOCK))
        {
          var c = new Contexts();
          c.Add("formmode", (int)importMode.OrderId); 
          c.Add("_ecomchannelref", _ecomchannelref); 
          ShowForm("ECOM.ECOMORDERS", "ORDEROPTIONS", c); 
        }
      
        else
        {
          ShowBalloonHint("Pobieranie zamówień jest zablokowane na kanale " + channel["NAME"], "Pobieranie zamówień", IconType.STOP);
        }
      }
      else
      {
        ShowBalloonHint("Nie znaleziono kanału o ref: " + this.ECOMCHANNELREF, "Pobieranie zamówień", IconType.STOP);
      } 
      channel.Close();
    }
    
    

    /// <summary>
    /// Metoda uruchamiana z interfejsu pobiera wszystkie zamówienia nowe i zmodyfikowane w podanym zakresie dat z kanału
    /// </summary>
    [UUID("1c616a1441394b8295febe9191f3a934")]
    virtual public void GetOrdersByDateRange()
    {
      //TODO przenieść zapytania SQL do logiki	
      string orderDateBeginRangeUpdate = 
      	@"update sys_edaconnectorstates s " +											
    	  "set s.nvalue = '"  + datebegin.ToString() + "' " +
    	  @"where s.nkey = 'daterangebegin'";	
      string orderDateEndRangeUpdate = 
        @"update sys_edaconnectorstates s " +
    	  "set s.nvalue = '"  + dateend.ToString() + "' " +
    	  @"where s.nkey = 'daterangeend'";		
    
    	Logic.UpdateDateRange(orderDateBeginRangeUpdate, orderDateEndRangeUpdate);	
      
    	try
    	{
    		var getOrdersMsg = LOGIC.ECOMCHANNELS.GetOrders(_ecomchannelref.AsInteger, importMode.DateRange, datebegin.AsDateTime,  dateend.AsDateTime);   
    	  if(!string.IsNullOrEmpty(getOrdersMsg))
    	  {
    		  ShowBalloonHint(getOrdersMsg, "Pobieranie zamówień", IconType.INFORMATION);
    	  }
    	}
    	catch(Exception ex)
    	{
    		ShowBalloonHint($"Błąd generowania komendy pobierania zamówień dla kanału sprzedaży: " + _ecomchannelref, "Pobieranie zamówień", IconType.STOP);
    	}
    	this.CloseForm();
    }
    
    

    /// <summary>
    /// Meoda do pobierania zamówień od ostatniej aktualizacji (tryb domyślny). Uruchamiana spod interfejsu użytkownika
    /// </summary>
    [UUID("1d7560cb6b884df795eefe0c46ae9d8d")]
    virtual public void OperatorGetNewOrders()
    {
      var channel = new ECOM.ECOMCHANNELS();
      channel.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{channel.REF.Symbol} = 0{_ecomchannelref}");
      if(channel.FirstRecord())  
      {
        if(channel.IMPORTORDERBLOCK == "0" || String.IsNullOrEmpty(channel.IMPORTORDERBLOCK))
        {
          try
          {        
            var getOrdersMsg = LOGIC.ECOMCHANNELS.GetOrders(_ecomchannelref.AsInteger, importMode.SinceLastImport, channel["LASTORDERCHANGETMSTMP"].AsDateTime);
            if(!string.IsNullOrEmpty(getOrdersMsg))
            {
              ShowBalloonHint(getOrdersMsg, "Pobieranie zamówień", IconType.INFORMATION);
            }            
          }
          catch(Exception ex)
          {
            ShowBalloonHint($"Błąd generowania komendy pobierania zamówień dla kanału sprzedaży: " + channel["NAME"], "Pobieranie zamówień", IconType.STOP);           
          }
        }
        else
        {
          ShowBalloonHint("Pobieranie zamówień jest zablokowane na kanale " + channel["NAME"], "Pobieranie zamówień", IconType.STOP);
        }
      }
      else
      {
        ShowBalloonHint("Nie znaleziono kanału o ref: " + _ecomchannelref, "Pobieranie zamówień", IconType.STOP);
      } 
      channel.Close();
    }
    
    

    /// <summary>
    /// Metoda wywołana jest przez przycisk &quot;Z zakresu dat&quot; z formy browse ECOMORDERS
    /// Metoda sprawdza czy pobieranie zamówień nie jest zablokowane. 
    /// Jeśli nie to wyświetla okno do wprowadzenia zakresu dat
    /// Jeśli tak to wyświetla stosowną informację.
    /// </summary>
    [UUID("3567c25c0a264a2bb2ffb60a7542f9de")]
    virtual public void OperatorGetOrdersByDateRange()
    {  
      var channel = new ECOM.ECOMCHANNELS();
      channel.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{channel.REF.Symbol} = 0{_ecomchannelref}");
      if(channel.FirstRecord())  
      {
        if(channel.IMPORTORDERBLOCK == "0" || String.IsNullOrEmpty(channel.IMPORTORDERBLOCK))
        {
          var c = new Contexts();
          c.Add("formmode", (int)importMode.DateRange);
          c.Add("_ecomchannelref", _ecomchannelref);   
          ShowForm("ECOM.ECOMORDERS", "ORDEROPTIONS", c); 
        }
        else
        {
          ShowBalloonHint("Pobieranie zamówień jest zablokowane na kanale " + channel["NAME"], "Pobieranie zamówień", IconType.STOP);
        }
      }
      else
      {
        ShowBalloonHint("Nie znaleziono kanału o ref: " + _ecomchannelref, "Pobieranie zamówień", IconType.STOP);
      } 
      channel.Close();
    }
    
    

    [UUID("534e4aa27a814276a81e6b0a42e6eefb")]
    virtual public bool IsDateToVisible()
    {
      if(this.IsBLChannel())
       return false;
      
      return true;
    }
    
    [UUID("5a4aac975dd7434bb9c7486553ec4bcf")]
    virtual public void ImportPackages()
    {
      LOGIC.ECOMCHANNELS.GetOrderPackages(this.ECOMCHANNELREF.AsInteger, this.ECOMORDERID.AsInteger);
      GUI.ShowBalloonHint("Komenda do pobrania paczek zamówienia została wygenerowana.", "Import paczek", IconType.INFORMATION);
    }
    
    

    [UUID("60b7dce5c7204502b859193576854e21")]
    virtual public void GetOrderByOrderId()
    {  
      var getordersMsg = "";  
      List<int> orderList = new List<int>();
      orderList.Add(this.orderid.AsInteger);
    
      try
      {
        getordersMsg = LOGIC.ECOMCHANNELS.GetOrders(_ecomchannelref.AsInteger, importMode.OrderId, null, null, orderList) 
          + "\n" + getordersMsg;
        if(!string.IsNullOrEmpty(getordersMsg))
        {
          GUI.ShowBalloonHint(getordersMsg,"Pobieranie zamówień", IconType.INFORMATION);   
        }     
      }
      catch(Exception ex)
      {
        ShowBalloonHint($"Błąd generowania komendy pobierania zamówienia dla kanału sprzedaży {_ecomchannelref}: {ex.Message}", "Pobieranie zamówień", IconType.STOP); 
      }
    
      CloseForm();
    }
    
    
    

    [UUID("632f123729b14d1c8750f269690eab8b")]
    virtual public void OperatorShowGetOrdersConfirmation()
    {    
      switch (this.formmode.AsInteger)
      {
          case (int)importMode.DateRange:
            OperatorShowGetOrdersByDateRangeConfirmation();  
            break;
          case (int)importMode.OrderId:
            GetOrderByOrderId();        
            break;
          default:
            ShowBalloonHint("Wybrano nieobsługiwany tryb pobierania zamówień", "Pobieranie zamówień", IconType.STOP);
            break;
      }
    
    }
    
    

    /// <summary>
    /// Metoda wywoływana jest z menu Synchronizacja -&gt; Import zamówień przez przycisk &quot;Wszystkie&quot;.
    /// Wyświetla czy na pewno chcemy pobrać pełną listę zamówień i w przypadku potwierdzenia wyświetla kolejną prośbę o potwierdzenie
    /// </summary>
    [UUID("6436995e09a54b958056b353f7119996")]
    virtual public void OperatorGetAllOrders()
    {   
      var channel = new ECOM.ECOMCHANNELS();
      channel.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{channel.REF.Symbol} = 0{_ecomchannelref}");
      if(channel.FirstRecord())  
      {
        if(channel.IMPORTORDERBLOCK == "0" || String.IsNullOrEmpty(channel.IMPORTORDERBLOCK))
        {
          string question = null; 
          question = "Czy na pewno chcesz pobrać\npełną listę zamówień?\n";
                    
          ShowMessageBox(question, "Aktualizacja wielu zamówień", IconType.QUESTION,
            Actions.OperatorShowGetAllOrdersConfirmation, Actions.CreateAction("Nie", "ICON_3")); 
        } 
        else
        {
          ShowBalloonHint("Pobieranie zamówień jest zablokowane na kanale " + channel["NAME"], "Pobieranie zamówień", IconType.STOP);
        }
      }
      else
      {
        ShowBalloonHint("Nie znaleziono kanału o ref: " + _ecomchannelref, "Pobieranie zamówień", IconType.STOP);
      }   
      channel.Close();
    }
    
    

    /// <summary>
    /// Metoda wyświetlająca pytanie o pobranie zamówień jeśi liczba wybranych zamówień jest większa niż 4.
    /// Metoda wywołana jest przez przycisk &quot;Wybrane&quot; z interfejsu
    /// </summary>
    [UUID("6531e4bac6be4231b67aa3e8f9003e29")]
    virtual public void OperatorGetSelectedOrders()
    {
      var channel = new ECOM.ECOMCHANNELS();
      channel.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{channel.REF.Symbol} = 0{_ecomchannelref}");
      if(channel.FirstRecord())  
      {
        if(channel.IMPORTORDERBLOCK == "0" || String.IsNullOrEmpty(channel.IMPORTORDERBLOCK))
        {
          if(SelectedRowsOrCurrent.Count > 4)
          {
            string question = "Czy na pewno chcesz zaktualizować " + SelectedRowsOrCurrent.Count + " zamówień?\n";          
            ShowMessageBox(question, "Aktualizowanie wielu zamówień", IconType.QUESTION,
              Actions.GetSelectedOrders, Actions.CreateAction("Nie", "ICON_3")); 
          }
          else
          {
            GetSelectedOrders();
          }
        }
        else
        {
          ShowBalloonHint("Pobieranie zamówień jest zablokowane na kanale " + channel["NAME"], "Pobieranie zamówień", IconType.STOP);
        } 
      }
      else
      {
        ShowBalloonHint("Nie znaleziono kanału o ref: " + _ecomchannelref, "Pobieranie zamówień", IconType.STOP);
      } 
      channel.Close();
    }
    
    

    [UUID("6543a58e641c43ac950fb89f5b4fb7de")]
    virtual public void CalcChosenStats()
    {
        if(SelectedRowsOrCurrent.Count > 0)
        { 
            var stdMethods = new ECOM.ECOMSTDMETHODS();
            foreach (var item in SelectedRowsOrCurrent)
            {
                var orderStatusData = stdMethods.Logic.CalcNagzamStatusChangedFromDB(Int32.Parse(item["REF"])); 
                var orderStatusCheck = LOGIC.ECOMCHANNELSTATES.CheckOrderStateUpdate(Int32.Parse(item["REF"]), orderStatusData);
            }    
            GUI.ShowBalloonHint("Uruchomiono metodę do przeliczenia statusów.", "Przeliczenie statusuów", IconType.INFORMATION);         
        } 
        else
        {
            GUI.ShowBalloonHint("Nie wybrano statusów do przeliczenia.", "Przeliczenie statusuów", IconType.STOP);
        }
      
    }
    
    

    [UUID("659f414a93714fc4a64718478ab6a802")]
    virtual public bool IsDisplayNagzamEnabled()
    {
      return !string.IsNullOrEmpty(this.NAGZAMREF.ToString());
    }
    
    

    [UUID("6ecd6fc5912f4008addb0518a15dbe0b")]
    virtual public void OperatorShowPaymentHistory()
    {
      Contexts c = new Contexts();
      c.Add("_dokref", this.NAGZAMREF);
      ShowForm("ECOM.ROZRACHROZ", "DICT", c); 
    }
    
    

    /// <summary>
    /// automatycznie wygenerowana metoda na inicjalizację pola REF
    /// </summary>
    [UUID("701c4e1c1a4749d08bd030e2e6881c49")]
    virtual public string InitializeREF()
    {
      return GenRef();
    }
    
    
    

    [UUID("741c21941a8d40c39b1f8215aeecd0c2")]
    virtual public string Validateorderid()
    {
      if(this.formmode.AsInteger == (int)importMode.OrderId)
      {
        //sparwdzamy, czy pole jest uzupełnione tylko w przypadku pobierania zamówienia po wybranym id
        return string.IsNullOrEmpty(this.orderid) ? "Pole nie może byc puste." : "";
      }
      else
      {
        return "";
      }
    }
    
    

    [UUID("812c03e4ee5b4b789e9c1efb151e8771")]
    virtual public string Initializedateend()
    {
      if(this.IsBLChannel())
        return DateTime.Now.ToString();
      
      return "";
    }
    
    

    [UUID("824abeb68c7641e7acc50573a4d4b4a6")]
    virtual public string SetOperatorGetOrdersByDateRangeLabel()
    {
      if(this.IsBLChannel())
        return "Z okresu od daty";
      
      return "Z zakresu dat";
    }
    
    

    [UUID("837d619c9803413ba5e3ad0148be7b84")]
    virtual public void RefreshBrowseGrid()
    {    
        this.RefreshData();   
    }
    
    

    [UUID("844e3127b07b49e48b1d9af460b8966c")]
    virtual public void OperatorSendOrdersStatus()
    {  
      int questionCnt = 1;
      if(SelectedRowsOrCurrent.Count > questionCnt)
      {
        ShowMessageBox($"Czy zaktualizować statusy dla {SelectedRows.Count.ToString()} zamówień?", 
          "Aktualizowanie wielu statusów", IconType.QUESTION,
          Actions.SendSelectedOrderStatuses, Actions.CreateAction("Nie", "ICON_3"));
      }
      else if(SelectedRowsOrCurrent.Count > 0)
      {    
        SendSelectedOrderStatuses();
      }
      else
      {
        GUI.ShowBalloonHint("Nie wybrano statusów zamówień do wysłania.", "Eksport statusów", IconType.STOP);
      }
    }
    
    

    /// <summary>
    /// Metoda zwraca ostatnio wybrane daty podczas pobierania zamówienia z zakresem dat
    /// </summary>
    [UUID("85a44ffbb0dd4fe990fa12654c865a15")]
    virtual public bool IsGetOrdersDateRangeVisible()
    {
        if(this.formmode.AsInteger == (int)importMode.DateRange)
        {    
            var con = EDA.CONNECTORS.Get("ECOM.ECOM");
            string date = Logic.GetDateRange(1);
            if(date != "")
            {
                datebegin = DateTime.Parse(date);
            }
            date = Logic.GetDateRange(2);
            if(date != "")
            {
                dateend = DateTime.Parse(date);   
            }
            return true;
        }
        else
        {
            return false;
        }   
    }
    
    

    [UUID("867feb58d1094420bdd09a2c6bd82953")]
    virtual public void OperatorAddShippingDocument()
    {  
      var c = new Contexts();
      c.Add("AKCEPT", "2");     
      //GUI.ShowForm(typeof(ECOM.LISTYWYSD).ToString(), "DICT", c);  
    
      var xxx = ShowDict("LISTWYSDREF","","ECOM.LISTYWYSD","REF","DICT", "AKCEPT = 2");
    }
    
    

    [UUID("a582f8628b064bf5b1e2fd3effbc13ef")]
    virtual public string Validatedateend()
    {  
      if(this.formmode.AsInteger == (int)importMode.DateRange)
      {
        if(this.dateend.Empty)
        {
          return "Wprowadź datę.";
        }
        if(this.dateend.AsDateTime < this.datebegin.AsDateTime)
        {
          return "Wprowadzono nieprawidłowy zakres dat.";  
        }
        return "";
      }
      else
      {
        return "";
      }    
    }
    
    

    [UUID("a951899d7bf1404799071bf2b6f9ff9b")]
    virtual public void ShowLabels()
    {
      Contexts c = new Contexts();
      c.Add("_ecomorder", this.REF);
      GUI.ShowForm("ECOMPARCELS", "BROWSELABELS", c);
    }
    
    

    /// <summary>
    /// Metoda wyświetlająca prosbę o potwierdzenie pobrania zamówień jeśi okres pobierania zamówień wynosi 30 dni.
    /// 
    /// </summary>
    [UUID("ae33d1ef266b4b3b828a3a201a03cc41")]
    virtual public void OperatorShowGetOrdersByDateRangeConfirmation()
    {
      var compareValue = dateend.AsDateTime - datebegin.AsDateTime;
      if (compareValue.Days > 30)
      {
        string question = "Czy na pewno chcesz zaktualizować \nlistę zamówień z " + compareValue.Days + " dni?";
                  
        ShowMessageBox(question, "Aktualizowanie wielu zamówień", IconType.QUESTION,
          Actions.GetOrdersByDateRange, Actions.CreateAction("Nie", "ICON_3")); 
      }
      else
      {
        GetOrdersByDateRange();
      }
    }
    
    

    /// <summary>
    /// Metoda uruchamiana z interfejsu uruchamia proces pobrania z witryny wybranych zamówień.
    /// </summary>
    [UUID("b073fc2cd48547188dcadd7a727dc5a7")]
    virtual public void GetSelectedOrders()
    {
      var getordersMsg = "";
      int orderId;
      List<int> orderList = new List<int>();
      foreach (var item in SelectedRowsOrCurrent)
      {
        try
        {
          orderId = Int32.Parse(Logic.GetOrderId(Int32.Parse(item["REF"])));
          orderList.Add(orderId);
        }
        catch(Exception ex)
        {
          getordersMsg += $"Błąd pobierania id dla zamówienia o ref: {item["REF"]}\n";
          continue;            
        }    
      }
    
      if(orderList.Count > 0)
      {
        try
        {
          getordersMsg = LOGIC.ECOMCHANNELS.GetOrders(_ecomchannelref.AsInteger, importMode.Selected, null, null, orderList) 
            + "\n" + getordersMsg;
          if(!string.IsNullOrEmpty(getordersMsg))
          {
            GUI.ShowBalloonHint(getordersMsg, "Pobieranie zamówień", IconType.INFORMATION);   
          }     
        }
        catch(Exception ex)
        {
          ShowBalloonHint($"Błąd generowania komendy pobierania zamówień dla kanału sprzedaży {_ecomchannelref}: {ex.Message}", "Pobieranie zamówień", IconType.STOP); 
        }
      }
      else
      {
        GUI.ShowBalloonHint("Nie wybrano zamówień do zaimportowania.", "Pobieranie zamówień", IconType.STOP);
      }  
    }
    
    

    /// <param name="masterobject"></param>
    [UUID("b205b7af8e674c2cb5fb87e9dff810da")]
    virtual public string FilterECOMCHANNELSTATE(ECOM.ECOMCHANNELSTATES masterobject)
    {
      string filter = $"ECOMCHANNELREF = {ECOMCHANNELREF}";
      return filter;
    }
    
    

    [UUID("b9c287c024f04a43a56b3a8f2dacab05")]
    virtual public bool IsBLChannel()
    {
      return ECOM.LOGIC.ECOMCHANNELS.IsBLChannel(this._ecomchannelref.AsInteger);
    }
    
    
    

    /// <summary>
    /// Metoda prosząca o potwierdzenie chęci pobrania wszystkich zamówień. Wywoływana za pomocą przycisku &quot;Tak&quot; po pierwszym zapytaniu wywołanym w OperatorGetAllOrders
    /// </summary>
    [UUID("bb769a0024a749d480e4d7f7cb099cb1")]
    virtual public void OperatorShowGetAllOrdersConfirmation()
    {
      string question = null; 
      question = "Pobranie wszystkich zamówień może potrwać klika minut.\n" +
                 "Czy kontynuować?";
    
      ShowMessageBox(question, "Aktualizowanie wielu zamówień",IconType.QUESTION,
        Actions.GetAllOrders, Actions.CreateAction("Nie", "ICON_3"));
    }
    
    

    /// <summary>
    /// Metoda interfejsowa do otwierania wybranego zamówienia w panelu Administratora sklepu połączonego z kanałem sprzedaży,
    /// o ile metoda jest przeciążona w adapterze przypisanym do konektora połączonego z kanałem sprzedaży
    /// </summary>
    [UUID("bf0df87f2b8d4d8997c9a4ea4a83c5f1")]
    virtual public object OpenOrderInAdminPanel()
    {  
      try
      {    
        var ecomChannel = new ECOM.ECOMCHANNELS();
        ecomChannel.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{ecomChannel.REF.Symbol} = 0{this.ECOMCHANNELREF}");
        if(!ecomChannel.FirstRecord())
        {
          throw new Exception("Nie znaleziono kanału sprzedaży o ref: " + this.ECOMCHANNELREF);
        }  
        string adapterName = LOGIC.ECOMORDERS.GetAdapterForConnector(ecomChannel.CONNECTOR.AsInteger);
        var adapter = GUI.CreateObject(adapterName);
        (adapter as ECOMADAPTERCOMMON).DoOpenOrderInAdminPanel(ecomChannel.CONNECTOR.AsInteger, this.ECOMORDERID);
        ecomChannel.Close();
      }
      catch(Exception ex)
      {
        GUI.ShowBalloonHint($"Błąd otwarcia panelu adminstratora witryny dla zamówienia {this.ECOMORDERSYMBOL}:{ex.Message}.","", 0);  
        return "";
      } 
      return "";  
    }
      
    
    

    /// <summary>
    /// Metoda interfejsowa do wymuszenia aktualizacji statusu zamówienia na witrynie
    /// </summary>
    [UUID("bf89168a49d9495592a35ca52f1069c6")]
    virtual public void SendSelectedOrderStatuses()
    {
      var orderStatusList = new List<Int64>();  
      var ecomOrder = new ECOM.ECOMORDERS();
      string errMsg = "";
      
      foreach (var item in SelectedRowsOrCurrent)
      { 
        ecomOrder.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{ecomOrder.REF.Symbol} = 0{item["REF"]}");
        if(ecomOrder.FirstRecord())     
        {
          ecomOrder.EditRecord();
          ecomOrder.SYNCSENDCHANELLSTATUS = (int)EcomSyncStatus.ExportPending;
          if(ecomOrder.PostRecord())
          {
            orderStatusList.Add(Int32.Parse(item["REF"]));
          }
          else
          {    
            throw new Exception($"Błąd aktualizacji statusu synchronizacji zamówienia o REF: {ecomOrder.REF} "); 
          }
        }    
      }
      
      if(orderStatusList.Count > 0)
      {
        try
        {
          var sendOrderStatusesMsg = LOGIC.ECOMCHANNELS.SendOrdersStatus(_ecomchannelref.AsInteger, ExportMode.List, orderStatusList);    
          if(!string.IsNullOrEmpty(sendOrderStatusesMsg))
          {
            GUI.ShowBalloonHint(sendOrderStatusesMsg, "Eksport statusów", IconType.INFORMATION);   
          }      
        }
        catch(Exception ex)
        {
          throw new Exception(ex.Message);
          GUI.ShowBalloonHint($"Błąd generowania komend wysyłki statusów zamówień dla kanału {_ecomchannelref}:\n{ex.Message}", "Eksport statusów", IconType.STOP);
          return;
        }
      }
      else
      {
        GUI.ShowBalloonHint("Nie wybrano statusów zamówień do wysłania.", "Eksport statusów", IconType.STOP);
      }  
      ecomOrder.Close();
    }
    
    

    [UUID("c296e63da5e045f1b9f23e3d48737cb6")]
    virtual public void SendSelectedOrderInvoices()
    {
      var orderList = new List<Int64>();  
      var ecomOrder = new ECOM.ECOMORDERS();
      var nagzam = new ECOM.NAGZAM();
      string errMsg = "";
      foreach (var item in SelectedRowsOrCurrent)
      { 
        ecomOrder.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{ecomOrder.REF.Symbol} = 0{item["REF"]}");
        if(!ecomOrder.FirstRecord())
        {
          throw new Exception($"Nie znaleziono zamówienia o REF: {item["REF"]}.");
        }
        else
        {
          nagzam.FilterAndSort($"{nameof(ECOM.NAGZAM)}.{nagzam.REF.Symbol} = 0{ecomOrder.NAGZAMREF}");
          if(nagzam.FirstRecord())
          {
            if(Logic.IsOrderInvoicesExists(nagzam.REF.AsInteger))
            {
              ecomOrder.EditRecord();
              ecomOrder.SYNCSENDCHANELLSTATUS = (int)EcomSyncStatus.ExportPending;
              ecomOrder.INVOICEPDFCHECKSUM = "";
              if(ecomOrder.PostRecord())
              {
                orderList.Add(Int32.Parse(item["REF"]));
              }
              else
              {
                throw new Exception($"Błąd aktualizacji statusu synchronizacji zamówienia o REF: {item["REF"]} ");
              }
            }
          }
          else
          {
            ecomOrder.EditRecord();
            ecomOrder.SYNCSENDCHANELLSTATUS = (int)EcomSyncStatus.ExportError;
            ecomOrder.LASTSTATUSSYNCERROR = $"Nie znaleziono zaakceptowanej faktury dla zamówienia o ref {ecomOrder.REF}.";
            ecomOrder.LASTSENDTMSTMP = DateTime.Now;
            if(!ecomOrder.PostRecord())
            {
              throw new Exception($"Błąd aktualizacji statusu synchronizacji zamówienia o REF: {ecomOrder.REF} ");
            }
          }       
        } 
      }
      
      if(orderList.Count > 0)
      {
        try
        {
          var sendOrderInvoicesMsg = LOGIC.ECOMCHANNELS.SendOrdersStatus(_ecomchannelref.AsInteger, ExportMode.List, orderList);    
          if(!string.IsNullOrEmpty(sendOrderInvoicesMsg))
          {
            GUI.ShowBalloonHint(sendOrderInvoicesMsg, "Wysyłka faktur", IconType.INFORMATION);   
          }      
        }
        catch(Exception ex)
        {
          foreach (var item in orderList)
          {
            ecomOrder.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{ecomOrder.REF.Symbol} = {item}");
            if(ecomOrder.FirstRecord())  
            {
              ecomOrder.SYNCSENDCHANELLSTATUS = (int)EcomSyncStatus.ExportError;
              ecomOrder.LASTSTATUSSYNCERROR = ex.Message;
              ecomOrder.LASTSENDTMSTMP = DateTime.Now;
              if(!ecomOrder.PostRecord())
              {
                throw new Exception($"Błąd aktualizacji statusu synchronizacji zamówienia o REF: {ecomOrder.REF} ");
              }
            }
            else
            {
              GUI.ShowBalloonHint($"Błąd generowania komendy wysyłki faktury zamówień dla kanału {_ecomchannelref} dla zamówienia {item}. \n{ex.Message}", "Wysyłka faktur", IconType.STOP);
            }
          }
          GUI.ShowBalloonHint($"Błąd generowania komend wysyłki faktur zamówień dla kanału {_ecomchannelref}.\n{ex.Message}", "Wysyłka faktur", IconType.STOP);
          return;
        }
      }
      else
      {
        GUI.ShowBalloonHint("Nie znaleziono zaakceptowanych faktur dla wybranych zamówień.", "Wysyłka faktur", IconType.STOP);
      }  
      ecomOrder.Close();
      nagzam.Close();
    }
    
    

    /// <summary>
    /// Metoda interfejsowa otwierająca przefiltrowane okno monitora EDA wyświetlające historię synchronizacji statusów wybranego zamówienia.
    /// </summary>
    [UUID("c4899066fb4144afb050d0f1c0a2caec")]
    virtual public void ShowEDASyncsForOrderStatuses()
    {
      if(string.IsNullOrEmpty(this.EDAID))
      {
          GUI.ShowBalloonHint("Zamówienie nie było jeszcze synchronizowane (Brak identyfikatora EDA).","", 0);          
      }
      else
      {
        var c = new Contexts(); 
        //[ML] tu jest problem bo chyba nie można ustawić fitrowania po wielu typach  
        //TODO[JCZ] - do przerobienia jak już będzie EDAID na ECOMORDER    
        c.Add("_messagetype", "ECOM.PutOrderStatusResponseCommand"); 
        c.Add("_identifier", this.EDAID);     
        GUI.ShowForm("SYSTEM.EDAMONITOR", "BROWSE", c);      
      }
    }
    
    

    [UUID("d147b2b9845548b1bc07b82290bfad48")]
    virtual public void GetSelectedOrdersPaymentHistory()
    {
      var getOrdersPaymentHistoryMsg = "";
      int orderId;
      List<int> orderList = new List<int>();
      foreach (var item in SelectedRowsOrCurrent)
      {
        try
        {
          orderId = Int32.Parse(Logic.GetOrderId(Int32.Parse(item["REF"])));
          orderList.Add(orderId);
        }
        catch(Exception ex)
        {
          getOrdersPaymentHistoryMsg += $"Błąd pobierania id dla zamówienia o ref: {item["REF"]}\n";           
        }    
      }
    
      if(orderList.Count > 0)
      {
        try
        {
          getOrdersPaymentHistoryMsg = LOGIC.ECOMCHANNELS.GetOrdersPayment(_ecomchannelref.AsInteger, orderList) 
            + "\n" + getOrdersPaymentHistoryMsg;
    
          if(!string.IsNullOrEmpty(getOrdersPaymentHistoryMsg))
          {
            GUI.ShowBalloonHint(getOrdersPaymentHistoryMsg, "Pobieranie historii płatności zamówień", IconType.INFORMATION);   
          }     
        }
        catch(Exception ex)
        {
          ShowBalloonHint($"Błąd generowania komendy pobierania historii płatności zamówień dla kanału sprzedaży {_ecomchannelref}: {ex.Message}", "Pobieranie historii płatności zamówień", IconType.STOP); 
        }
      }
      else
      {
        GUI.ShowBalloonHint("Nie wybrano zamówień do pobrania historii płatności.", "Pobieranie historii płatności zamówień", IconType.STOP);
      } 
    }
    
    

    [UUID("d333b531a3594ddbb376d0ba2d0b03d9")]
    virtual public string CalculateSYNCSENDCHANELLSTATUS()
    {
      if(!String.IsNullOrEmpty(ECOMCHANNELSTATE))
      {
        return ((int)EcomSyncStatus.ExportPending).ToString();
      }
      return null;
      /*Wynik tej metody zostanie przypisany do pola SYNCSENDCHANELLSTATUS.
      Jeżeli metoda ma nie zmieniać bieżącej wartości pola SYNCSENDCHANELLSTATUS, to musi zwracać null.
      Metoda ta nie może jawnie modyfikować wartości pola SYNCSENDCHANELLSTATUS, gdyż grozi to zapętleniem kodu!!!
      Metoda jest uruchamiana automatycznie, gdy dowolne z wykorzystanych pól w tej metodzie zmieni swoją wartość.*/
    ;
    }
    
    

    [UUID("d6907b958f984630a5483beebef143ed")]
    virtual public bool IsGetOrderByOrderIdVisible()
    {
      return this.formmode.AsInteger == (int)importMode.OrderId;  
    }
    
    

    /// <summary>
    /// Metoda interfejsowa otwierająca wybrane zamówienie w VCLowym oknie edycji zamówienia
    /// </summary>
    [UUID("d9ad5c0ca61e4970a8c0c32eae7d62fd")]
    virtual public void DisplayNagzam()
    {
        if(!string.IsNullOrEmpty(this.NAGZAMREF.ToString()))
        {    
            CallAction("DisplayNagzamFromNeos","NAGZAMREF=0"+this.NAGZAMREF);
        }
        else
        {
            GUI.ShowBalloonHint("Nie znaleziono zamówienia w rejestrach.", "", 0);        
        }
    }
    
    

    /// <summary>
    /// Metoda interfejsowa otwierająca przefiltrowane okno monitora EDA wyświetlające historię synchronizacji wybranego zamówienia.
    /// </summary>
    [UUID("decc80ba17fe4d6689f204600421c1ca")]
    virtual public void ShowEDASyncsForOrder()
    {
      if(string.IsNullOrEmpty(this.EDAID))
      {
          GUI.ShowBalloonHint("Zamówienie nie było jeszcze synchronizowane (Brak identyfikatora EDA).","", 0);          
      }
      else
      {
          var c = new Contexts();   
          //c.Add("_messagetype", "ECOM.ImportOrderCommand");
          c.Add("_identifier", this.EDAID);     
          GUI.ShowForm("SYSTEM.EDAMONITOR", "BROWSE", c);
      }    
    }
    
    

    [UUID("ed6122a0f2c84d4592e8bc5b89948be3")]
    virtual public string Validatedatebegin()
    {  
      if(this.formmode.AsInteger == (int)importMode.DateRange
        && this.datebegin.Empty)
      {   
        return "Wprowadź datę.";
      }
      else
      {
        return "";
      } 
    }
    
    

    [UUID("ee8d4caa94f34bbe9655902b262828af")]
    virtual public bool IsDisplayNagzamVisible()
    {
      //[PW] Metoda do usunięcia 
      //return !string.IsNullOrEmpty(this.NAGZAMREF.ToString());
      return true;
    }
    
    

    [UUID("f2b8fcbc821d40f4ac90b82e81dc038d")]
    virtual public void OperatorSendOrdStatusToExport()
    {
      var ordStatToExportList = new List<Int64>();  
      ordStatToExportList = Logic.GetOrdStatusToExport(_ecomchannelref.AsInteger);
    
      if(ordStatToExportList.Count > 0)
      {
        try
        {
          var sendOrderStatusesMsg = LOGIC.ECOMCHANNELS.SendOrdersStatus(_ecomchannelref.AsInteger, ExportMode.List, ordStatToExportList);    
          if(!string.IsNullOrEmpty(sendOrderStatusesMsg))
          {
            GUI.ShowBalloonHint(sendOrderStatusesMsg, "Eksport statusów", IconType.INFORMATION);   
          }      
        }
        catch(Exception ex)
        {
          GUI.ShowBalloonHint($"Błąd generowania komend wysyłki statusów zamówień dla kanału {_ecomchannelref}:\n{ex.Message}", "Eksport statusów", IconType.STOP);
          return;
        }
      }
      else
      {
        GUI.ShowBalloonHint("Nie znaleziono statusów zamówień do wysłania.", "Eksport statusów", IconType.STOP);
      }  
    }
  }
	//ERRSOURCE: structure PrintOrderInvoiceCommand { ... } in object ECOM.ECOMORDERS
  [CustomData("DataStructure=Y")]
  [UUID("0cb45f6ccfb243c8aa0974e3f6e67dec")]
  public class PrintOrderInvoiceCommand : NeosCommand
  {
      public int EcomChannel {get; set;}
      public string NagfakRef {get; set;}
      public string InvoicePath {get; set;}
  }
  
  
	//ERRSOURCE: structure CheckOrderStatusChangedCommand { ... } in object ECOM.ECOMORDERS
  [CustomData("DataStructure=Y")]
  [UUID("56978d0ccc8843acaeabdfbc5dd3f927")]
  public class CheckOrderStatusChangedCommand : NeosCommand
  {
      public NagzamStatusChanged OrderParams {get; set;}
      public string Message {get; set;}    
  }
  
  
	//ERRSOURCE: structure ConnectorDataRow { ... } in object ECOM.ECOMORDERS
  /// <summary>
  /// Struktura danych, któa przechowuje dane, czy w danym konektorze jest ustawiona blokada pobierania zamówien.
  /// Struktura używana jest w IsImportOrderActive
  /// active - blokada pobierania 
  /// connectorName - nazwa konektora
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("d0f45f51cb634b018709dee64d89b647")]
  public class ConnectorDataRow 
  {
      public int blocked {get; set;}
      public string connectorName {get; set;}
  }
  
  
	//ERRSOURCE: structure CheckOrderChangedCommand { ... } in object ECOM.ECOMORDERS
  [CustomData("DataStructure=Y")]
  [UUID("fd7d9492252441f48c18a6113b4a7df4")]
  public class CheckOrderChangedCommand : NeosCommand
  {
      public int OrderRef {get;set;}
      public string Message {get; set;}
  }
}
