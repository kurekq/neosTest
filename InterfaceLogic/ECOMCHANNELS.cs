
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
//ERRSOURCE: ECOM.ECOMCHANNELS

  public partial class ECOMCHANNELS
  {
    [UUID("0724c6ab858543609adfd8b5cd38e42c")]
    public static string SchedulerSendOrdersVerificationReport()
    {
      return LOGIC.ECOMCHECKORDERS.AutoSendOrdersVerificationReport();
    }
    
    
    

    /// <param name="masterobject"></param>
    [UUID("0b4c2880b7cb4f039deb6695cc7e4e5f")]
    virtual public string FilterMETHODSOBJ(SYSTEM.OBJECT masterobject)
    {
      // UUID obiektu ECOMSTDMETHODS
      return "OBJECTUUID = '30e793334a984fe695f84c137f75987e' OR PARENTOBJECTUUID = '30e793334a984fe695f84c137f75987e'";
    }
    
    

    [UUID("11399fb323f64254b959cb9ee973f8e4")]
    virtual public void StocksVerification()
    {
      var c = new Contexts();
      c.Add("_channelref", this.REF);
      GUI.ShowForm("ECOM.ECOMCHECKSTOCKS", "BROWSE", c); 
    }
    
    

    [UUID("2192f0803adc413280c2725be6ce7e00")]
    public static string SchedulerExportStatus()
    {
      return LOGIC.ECOMCHANNELS.SendOrdersStatusForChannels(null, null, null); 
    } 
    
    
    

    [UUID("223900c6cad24d21b660d06eaf924a9d")]
    public static string SchedulerSendStockVerificationReport()
    {
      return LOGIC.ECOMCHECKSTOCKS.AutoSendStockVerificationReport();
    }
    
    
    

    /// <param name="detailobject"></param>
    [UUID("275a650ee46b4643acd0ed054e2802c6")]
    virtual public void ConnectWithECOMSTDMETHODS(ECOMSTDMETHODS detailobject)
    {
      var c = LOGIC.ECOMSTDMETHODS.LoadEcomChannelParams(this.REF.AsInteger);
      detailobject.SetContext(c); 
    }
    
    

    [UUID("2ae9a1dc13d84324b226ea832bdc5e85")]
    public static string SchedulerImportOrder()
    {
      return LOGIC.ECOMCHANNELS.GetOrdersForChannels(null, null, null, 
        importMode.SinceLastImport); 
    
    }
    
    
    

    /// <param name="detailobject"></param>
    [UUID("34b163613bce49aa954e55ad2121efab")]
    virtual public void ConnectWithECOMCHANNELCONVERT(ECOMCHANNELCONVERT detailobject)
    {
        detailobject.SetContext("ECOMCHANNELREF",this.REF);  
    }
    
    

    /// <summary>
    /// automatycznie wygenerowana metoda na inicjalizację pola REF
    /// </summary>
    [UUID("3e4ca2c1d88d4042937e26c21b21490c")]
    virtual public string InitializeREF()
    { 
      return GenRef();
    }
    
    

    [UUID("3fed0cfd7e1443aebf98698f298ae08c")]
    virtual public string SetDBFilter()
    {
      string sqlFilter = "COMPANY = " + SessionInfo.GlobalParam["CURRENTCOMPANY"].AsInteger;
      if(_schowonlyactive == 1){
        sqlFilter += " and ACTIVE = 1";
      }
      return sqlFilter;
    }
    
    

    /// <param name="detailobject"></param>
    [UUID("4a35abf540b34395b1632085d51f69c8")]
    virtual public void ConnectWithECOMCHANNELSTOCK(ECOMCHANNELSTOCK detailobject)
    {    
        var c = new Contexts();
        c.Add("_ecomchannelref", this.REF);
        c.Add("ECOMCHANNELREF", this.REF);
        detailobject.SetContext(c);
    }
    
    

    [UUID("58cf92f58c50468d81edc0b4f3a9e7eb")]
    public static string SchedulerExportInventory()
    {
      return LOGIC.ECOMCHANNELS.SendInventoryForChannels(null, null, null); 
    } 
    
    
    

    [UUID("5b7f25ea369c43ada791dc59fd007993")]
    virtual public string InitializeCOMPANY()
    {
      return SessionInfo.GlobalParam["CURRENTCOMPANY"];
    }
    
    

    /// <param name="detailobject"></param>
    [UUID("608bcbbc66b1413b9e9aa83316b1fa27")]
    virtual public void ConnectWithECOMINVENTORYPRICES(ECOMINVENTORYPRICES detailobject)
    {    
        var c = new Contexts();  
        c.Add("_ecomchannelref", this.REF.AsInteger);  
        detailobject.SetContext(c); 
    }
    
    

    /// <summary>
    /// metoda uruchamiana przez schedulera wysyłająca ceny zmienionych towarów we wszysykich aktywnych kanałach sprzedaży
    /// </summary>
    /// <param name="mode"></param>
    [UUID("664175bc04a24e709699bbdcbc7e9166")]
    public static string SchedulerSendInventoryPrices(CalculateMode mode)
    {
      //metoda uruchamiana przez schedulera naliczająca i wysyłająca ceny zmienionych towarów we wszysykich aktywnych kanałach sprzedaży
      
      string message = "";
      message += LOGIC.ECOMCHANNELS.CalculateInventroryPrices(mode);
      message += LOGIC.ECOMCHANNELS.SendInventoryPricesForChannels(null, null, null, ExportMode.LastChange);
      return message;
      
    }
    
    
    

    /// <summary>
    /// Metoda jest częścią mechanizmu dodawania towarów do kanałów sprzedaży z poziomu kartotek towarów.
    /// Uruchamiana jest akcją VCL w oknie Asortymentu i otwiera słownik kanałów sprzedaży, żeby wybrać kanał, do którego
    /// dodajemy towary
    /// inventoryList - string z listą ktmów towarów rozdzielonych średnikami
    /// </summary>
    /// <param name="inventoryList"></param>
    [UUID("69b2f2fdcaf64f6e9dba418d44fbc175")]
    public static void ShowEcomChannelDict(string inventoryList)
    {    
        Contexts c = new Contexts();
        c.Add("_inventorieslist", inventoryList);
        GUI.ShowForm("ECOM.ECOMCHANNELS", "DICT", c);    
    } 
    
    

    /// <param name="detailobject"></param>
    [UUID("6b6f4910f393409fa155dcfc3d117f65")]
    virtual public void ConnectWithECOMACCOUNTS(ECOMACCOUNTS detailobject)
    {
        detailobject.SetContext("ECOMCHANNELREF",this.REF);  
    }
    
    

    /// <param name="detailobject"></param>
    [UUID("74a0ebdec7b14416a226ea8421f98166")]
    virtual public void ConnectWithECOMORDERS(ECOMORDERS detailobject)
    {    
        var c = new Contexts();
        c.Add("_ecomchannelref", this.REF);
        c.Add("ECOMCHANNELREF", this.REF);
        detailobject.SetContext(c);
    }
    
    

    [UUID("78161d772197461b9e3e9b862d48632b")]
    public static string SchedulerSendPriceVerificationReport()
    {
      return LOGIC.ECOMCHECKPRICES.AutoSendPriceVerificationReport();
    }
    
    
    

    [UUID("80a0788142cb4b16a0066f4032f7ed72")]
    virtual public void DataVerification()
    {
      var c = new Contexts();
      c.Add("_channelref", this.REF);
      GUI.ShowForm("ECOM.ECOMCHECKORDERS", "INSERTPARAMS", c);
    }
    
    

    /// <summary>
    /// Metoda wykorzystywana w mechanizmie dodawania listy towarów do kanalów sprzedaży z poziom kartoteki towarów.
    /// Jest uruchamiana po wyborze kanału sprzedaży w słowniku kanałów sprzedazy
    /// Metoda korzysta z parametru _inventorieslist, który zawiera listę KTM towarów do eksportu wybranych w oknie Asortyment. 
    /// Metoda dzieli listę na pojedyncze wpisy KTM i wywołuje AddInventoryFromTDLogic.
    /// 
    /// </summary>
    [UUID("80c81d9d7c454b4ab3a4f25a3618d540")]
    virtual public void AddInventoryFromTD()
    {
      string[] splitList = _inventorieslist.ToString().TrimEnd(';').Split(';');  
      foreach (var ktm in splitList)
      {
        try
        {
          LOGIC.ECOMINVENTORIES.AddInventoryToEcomChannel(this.REF.AsInteger, ktm);      
        }
        catch(Exception ex)
        {
          throw new Exception($"Błąd dodawania towaru o ktm {ktm} do kanału sprzedaży: " + ex);
        }
      }  
      CloseForm();
    }
    
    

    /// <param name="mode"></param>
    [UUID("96ae696776114eedb38ecf023916f338")]
    public static string SchedulerExportInventoryStocks(CalculateMode mode)
    {
      string message = "";
      message += LOGIC.ECOMCHANNELS.CalculateInventroryStocks(mode);
      message += LOGIC.ECOMCHANNELS.SendInventoryStocksForChannels(null, null, null, ExportMode.LastChange);
      return message;
    }
    
    
    

    [UUID("9770f84794584c9ab148d582147568b2")]
    virtual public void ShowReg()
    {    
        ShowRecord("MASTEREDIT");
    }
    
    

    [UUID("98a6fe5de5c748fa9e96d0b5f18f3ec6")]
    virtual public void PricesVerification()
    {
      var c = new Contexts();
      c.Add("_channelref", this.REF);
      GUI.ShowForm("ECOM.ECOMCHECKPRICES", "BROWSE", c);
    }
    
    

    /// <summary>
    /// Metoda wywołana jest przez akcję neosową naliczania statystyk kanałów sprzedaży.
    /// Pobiera dane na temat statystyk z metodyCalculateChannelStatsLogic i aktualizuje kolumny na kanałach sprzedaży
    /// np. ilość zamówień pobanych dzisiaj, ilość zamówień pobranych w miesiącu itp.
    /// 
    /// </summary>
    /// <param name="showBalloonHint"></param>
    [UUID("9913012be5aa4c59abee75dc57356242")]
    virtual public string CalculateChannelsStats(bool showBalloonHint = true)
    { 
      string msg = ""; 
      DateTime temp;
      List<StatDataRow> channelsStatList = new List<StatDataRow>();
      try
      {
        channelsStatList = LOGIC.ECOMCHANNELS.GetChannelsStats();
        if(channelsStatList.Count > 0)
        {
      
          var channel = new ECOM.ECOMCHANNELS();
          foreach(var channelStats in channelsStatList) 
          {
            channel.FilterAndSort($"{nameof(ECOM.ECOMCHANNELS)}.{channel.REF.Symbol} = 0{channelStats.ChannelRef}");
            if(channel.FirstRecord())
            {
              channel.EditRecord();
              channel.STATDAILYORDERSCOUNT = channelStats.DailyStat; 
              channel.STATMONTHLYORDERSCOUNT = channelStats.MonthlyStat;
              channel.STATORDERSFORINTERVATION = channelStats.OrdersIntervation;
              channel.STATINVENTORYWITHERROR = channelStats.InventoryWithError;
              channel.STATPRICESWITHERROR = channelStats.PricesWithError;        
    
              if(DateTime.TryParse(channelStats.LastOrderSync, out temp))
              {
                channel.STATLASTORDERDSYNC = temp;
              }
              temp = default(DateTime);
              if(DateTime.TryParse(channelStats.LastOrderTimestmp, out temp))
              {
                channel.LASTORDERTIMESTMP = temp;
              }
              temp = default(DateTime);
              if(DateTime.TryParse(channelStats.LastOrderChangeTimestmp, out temp))
              {
                channel.LASTORDERCHANGETMSTMP = temp;
              }
              temp = default(DateTime);
              if(DateTime.TryParse(channelStats.LastPricesUpdate, out temp))
              {
                channel.STATLASTPRICESUPDATE = temp;
              }
              temp = default(DateTime);
              if(DateTime.TryParse(channelStats.LastStocksUpdate, out temp))
              {
                channel.STATLASTSTOCKSUPDATE = temp;
              }
    
              if(!channel.PostRecord())
              {
                throw new Exception("Błąd zapisu statystyk do bazy danych.");
              }
            }
            else
            {
              throw new Exception($"Nie znaleziono kanału sprzedaży o REF: {channelStats.ChannelRef}");
            } 
          } 
          channel.Close(); 
        }
        else
        {
          throw new Exception("Naliczenie statysyk zakończone niepowodzeniem.");
        } 
        
          
      }
      catch(Exception ex)
      {
        msg  = $"Błąd naliczania statystyk kanałów sprzedaży: {ex.Message}\n";
        if(showBalloonHint)
        {
          GUI.ShowBalloonHint(msg,"Naliczanie statystyk",IconType.STOP);
        }
        return msg;
      }
    
      if(showBalloonHint)
      {
        msg = "Naliczenie statysyk zakończone sukcesem.";
        GUI.ShowBalloonHint(msg, "Naliczanie statystyk", IconType.INFORMATION);
      }
      RefreshGrid();  
      return msg;
    
    } 
    
    

    [UUID("bf482d514858466aad201d3d14419bdc")]
    public static string SchedulerCalculateChannelsStats()
    {
      return new ECOM.ECOMCHANNELS().CalculateChannelsStats(false);
    }
    
    
    

    /// <summary>
    /// Metoda otwiera okno z podppowiedzią dla operatora zawierającą pełną listę atrybutów towarów używaną do konfiguracji
    /// listy atrybutów towaru, które powinny być synchronizowane w kanale sptzedaży. Podpowiedź uruchamiana jest z poziomu okna konfiguracji
    /// kanałów sprzedaży
    /// </summary>
    [UUID("c1f87d7504694ca8b3e08d557b1d8462")]
    virtual public void ShowAtributes()
    { 
        GUI.ShowForm("ECOM.ECOMCHANNELS", "ATTRIBUTES");
    }
    
    

    /// <summary>
    /// Metoda otwiera okno kanałów sprzedaży, uruchamiana ze wstążlki
    /// </summary>
    [CustomData("Usings=;Neos.BusinessPlatform.Devel;")]
    [UUID("d2add09513af4c2294e404958d183112")]
    public static void BrowseECOMCHANNELS()
    {   
        GUI.ShowForm("ECOM.ECOMCHANNELS", "BROWSE");
    }
    
    

    /// <param name="detailobject"></param>
    [UUID("e1bbe21cef0544c2ac47625171eae9e0")]
    virtual public void ConnectWithECOMINVENTORYSTOCKS(ECOMINVENTORYSTOCKS detailobject)
    {   
        var c = new Contexts();
        c.Add("_ecomchannelref", this.REF);
        detailobject.SetContext(c);    
    }
    
    

    /// <param name="masterobject"></param>
    [UUID("e25beaac65cc4275bfdbee0f743c9920")]
    virtual public string FilterCHANNELGROUP(ECOM.ECOMCHANNELSGROUPS masterobject)
    {
      return "COMPANY = " + this.COMPANY.AsInteger;
    }
    
    

    [UUID("e55df5fc9b864010a6d62362a485b6cc")]
    virtual public string Calculate_bindstring()
    {
      return "STATDAILYORDERSCOUNT=*=%TclRed%U*";
      /*Wynik tej metody zostanie przypisany do pola _bindstring.
      Jeżeli metoda ma nie zmieniać bieżącej wartości pola _bindstring, to musi zwracać null.
      Metoda ta nie może jawnie modyfikować wartości pola _bindstring, gdyż grozi to zapętleniem kodu!!!
      Metoda jest uruchamiana automatycznie, gdy dowolne z wykorzystanych pól w tej metodzie zmieni swoją wartość.*/
    ;
    }
    
    

    /// <param name="detailobject"></param>
    [UUID("e89a0d71099a49c4975e95f324d7a10a")]
    virtual public void ConnectWithECOMINVENTORIES(ECOMINVENTORIES detailobject)
    {
        var c = new Contexts();
        c.Add("ECOMCHANNELREF",this.REF);
        c.Add("_channelref", this.REF.AsInteger);
        detailobject.SetContext(c); 
    }
    
    

    /// <summary>
    /// Metoda uruchamia naliczanie statystyk kanału sprzedaży i odświerzanie okno przeglądania kanalów. Uruchamiana spod przycisku i cyklicznie na formie Browse.
    /// </summary>
    [UUID("ec37ad7a96324c39949e77d284768a0b")]
    virtual public void RefreshGrid()
    { 
        // JCZ - chyba to nadmiarowe - za każdym odświerzeniem naliczamy statystyki ? Od tego jest metoda. CalculateChannelStats() ;  
        RefreshData("ECOM.ECOMCHANNELS","C");      
    }
    
    

    [UUID("ecb63d8fe1dc482bba035b879470119f")]
    virtual public void ShowChannelConfig()
    {
      var c = LOGIC.ECOMSTDMETHODS.LoadEcomChannelParams(this.REF.AsInteger);
      c.Add("_ecomchannelname", NAME);
      //jezeli nazwa obiektu z metodami synchronizacji nie jest uzupełniona, to otwieramy okno podstawowe
      string neosObject = (string.IsNullOrEmpty(this.METHODSOBJ) ? "ECOM.ECOMSTDMETHODS" : this.METHODSOBJ.ToString()); 
      GUI.ShowForm(neosObject, "SETUP", c);  
    }
    
    

    /// <param name="detailobject"></param>
    [UUID("f045eacf10d24019bca4a89372d0545a")]
    virtual public void ConnectWithECOMCHANNELSTATES(ECOMCHANNELSTATES detailobject)
    {
        detailobject.SetContext("ECOMCHANNELREF",this.REF);
    }
    
    

    [UUID("f1d32c509cd649e7a360a53c9ee8a802")]
    virtual public string Initialize_bindstring()
    {
      return "STATDAILYORDERSCOUNT=*=%TclRed%U*";
    }
    
    

    [CustomData("Usings=;Neos.BusinessPlatform.Devel;")]
    [UUID("f8e60ed3660a47d39317af767cbc5192")]
    public static void ConfigECOMCHANNELS()
    {
        GUI.ShowForm("ECOM.ECOMCHANNELS", "BROWSECONFIG");
    }
    
    

    /// <summary>
    /// Metada zwracająca nazwę okna MASTEREDIT
    /// </summary>
    [UUID("fe0c3d8678584b218963da6876fc2cb6")]
    virtual public string SetMASTEREDITFormLabel()
    {
      return this.NAME;
    }
  }
	//ERRSOURCE: structure StatDataRow { ... } in object ECOM.ECOMCHANNELS
  /// <summary>
  /// Struktura wykorzystywana do zwracania danych z metody RefreshLogic używana przy pobieraniu statystyk
  ///  - ChannelRef - ref kanału sprzedaży dla którego pobrane zostały statystyki 
  /// -  DailyStat - dzienna ilość zamówień 
  ///  - MonthlyStat - miesięczna ilość zamówień
  ///  - OrderIntervention - ilość interwencji 
  ///  - LastOrderSync - data ostatniej synchronizacji towarów
  ///  - LastOrderCreate - data ostatniego zamówienia
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("b9783188790d48e98fe884d23faed722")]
  public class StatDataRow
  {
      public int ChannelRef {get; set;}
      public int DailyStat {get; set;}
      public int MonthlyStat {get; set;}
      public int OrdersIntervation {get; set;}
      public string LastOrderCreate {get; set;}
      public string LastOrderSync {get; set;}
      public string LastOrderTimestmp {get; set;}
      public string LastOrderChangeTimestmp {get; set;}
      public string LastPricesUpdate {get; set;}
      public string LastStocksUpdate {get; set;}
      public int InventoryWithError {get; set;} 
      public int PricesWithError {get; set;}    
  }
}
