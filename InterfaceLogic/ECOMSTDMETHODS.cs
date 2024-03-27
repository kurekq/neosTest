
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
//ERRSOURCE: ECOM.ECOMSTDMETHODS

  public partial class ECOMSTDMETHODS
  {
    [UUID("PostRecord")]
    override public bool PostRecord()
    {
      var filledParams = this.GetAllParameters();
      var paramsFromForm = this.BuildContextFromParameters();
      this.Logic.SaveEcomChannelParams(filledParams, paramsFromForm);
      this.CloseForm();
      return true;  
    }
    
    

    [UUID("CancelRecord")]
    override public void CancelRecord()
    {
      /*
      Contexts paramsFromDatabase = LOGIC.ECOMSTDMETHODS.LoadEcomChannelParams(this._ecomchannel.AsInteger);
      var paramsFromForm = this.BuildContextFromParameters();
      Contexts paramsContext = new Contexts();
      foreach(System.Collections.DictionaryEntry el in paramsFromForm)
      {    
        if(paramsFromDatabase.Contains(el.Key))
        {
          paramsContext.Add(el.Key, paramsFromDatabase[el.Key]);
        }
        else
        {
          paramsContext.Add(el.Key, "");      
        }    
      }    
      */
      CloseForm();
    }
    
    

    /// <summary>
    /// [ML] do refaktoryzacji, usunięcia
    /// </summary>
    [UUID("13f4185147b544858d1a4cfdd4d5b8b8")]
    virtual public void SaveEcomChannelParamsOld()
    {/*
      var filledParams = this.GetAllParameters();
      var paramsFromForm = this.BuildContextFromParameters();
      Contexts paramsContext = new Contexts();
    
      foreach(System.Collections.DictionaryEntry el in paramsFromForm)
      {
        paramsContext.Add(el.Key, (filledParams.Contains(el.Key) ? filledParams[el.Key] : null));
      }  
           
      this.Logic.InsertOrUpdateEcomChannelParams(paramsContext);
      CloseForm();  
      */
    }
    
    

    [UUID("1674c0355d5f4d649adb3aa9320358dd")]
    virtual public string SetSETUPFormLabel()
    {
      return "Konfiguracja kanału " + this._ecomchannelname;
    }
    
    

    /// <summary>
    /// Wyświetla okno atrybutów parametryzujących metody synchronizacji danych w kanale sprzedaży
    /// </summary>
    [UUID("34f734dab9114b1780a45644609b50ec")]
    public static void ShowInventoryAttributesList()
    {  
      GUI.ShowForm(typeof(ECOM.ECOMSTDMETHODS).ToString(), "INVENTORYATTRIBUTES");
    }
    
    

    [UUID("73dcf1f1358c46d6a9ceb16db1049b39")]
    virtual public string Initialize_inventoryattributeslist()
    {   
       return LOGIC.ECOMSTDMETHODS.GetClassFieldList(typeof(ECOM.EcomInventoryInfo));
    }
    
    

    /// <param name="masterobject"></param>
    [UUID("a65bf0f3f2484ae281b282e9c231d73b")]
    virtual public string Filter_calcpricemethodsobj(SYSTEM.OBJECT masterobject)
    {
      return "OBJECTUUID = '70573b65cbe546728d77328bcdaa8db3' OR PARENTOBJECTUUID = '70573b65cbe546728d77328bcdaa8db3'";
    }
    
    

    /// <param name="masterobject"></param>
    [UUID("fa672c4b3efa46388cba1315ee144897")]
    virtual public string Filtermethodsobj(SYSTEM.OBJECT masterobject)
    {
      return "OBJECTUUID = 'adbb163781c34c69b8becd0bfc34a4c5' OR PARENTOBJECTUUID = 'adbb163781c34c69b8becd0bfc34a4c5'";
    }
  }
	//ERRSOURCE: structure EcomPaymentInfo { ... } in object ECOM.ECOMSTDMETHODS
  /// <summary>
  /// Unwersalna struktura przechowująca dane o sposobie platności zamówienia
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("099deb5434954414b1ab07a83c68746c")]
  public class EcomPaymentInfo
  {
      public string PaymentType {get; set;}
      public string Currency {get;set;}
      public decimal? OrderValue {get;set;}
      public decimal? Rate {get;set;}
      public string CalculateType {get;set;}
      public decimal? RebatePercent {get;set;}    
      public int? PaymentDays {get;set;}    
  }
  
  
	//ERRSOURCE: structure EcomInventoryTextInfo { ... } in object ECOM.ECOMSTDMETHODS
  /// <summary>
  /// struktura do przechowywania nazw, opisów razem z dodatkowymi parametrami np. symbolem języka
  /// Używana np. do przechowywania nazw towarów
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("0da1826ce6874a5789cf2bd8f4797c1b")]
  public class EcomInventoryTextInfo 
  {
      ///<summary>Treść opisu towaru wysyłana do kanału sprzedaży</summary>
      public string Text {get; set;}
      ///<summary>Jezyk, opisu (2 lub 3literowy symbol ISO) </summary>
      public string Language {get; set;}
  }
  
  
	//ERRSOURCE: structure EcomClientAccountInfo { ... } in object ECOM.ECOMSTDMETHODS
  /// <summary>
  /// Uniwersalna struktura wykorzystywana w metodzie IAIOrderToEcomOrderInfo w ECOMADAPTERIAI
  /// Przechowuje dane o koncie klienta 
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("141ef48ceeb046d78f724761c568cbd3")]
  public class EcomClientAccountInfo
  {
      public string Id {get; set;}
      public string Login {get; set;} 
      public string Phone {get; set;} 
      public string ExternalCode {get; set;}
      private string email;
  
      public string Email 
      {
          get
          {
              return email;
          } 
          set
          {
              email = string.IsNullOrEmpty(value) ? null : value;            
          }
      }    
  }
  
  
	//ERRSOURCE: structure EcomAttachmentInfo { ... } in object ECOM.ECOMSTDMETHODS
  [CustomData("DataStructure=Y")]
  [UUID("1bb3190e82b140d989e5c90cddc5ede3")]
  public class EcomAttachmentInfo 
  {
      // struktura danych do wysyłki załączników (faktur) do zamówienia
      public string OrderId {get;set;} 
      public string AttachmentSymbol {get; set;}
      public string FileName {get; set;}
      public string AttachmentPath {get; set;}
      public string NagfakRef {get; set;}
      public string Typfak {get; set;}
  }
  
  
  
	//ERRSOURCE: structure EcomDispatchInfo { ... } in object ECOM.ECOMSTDMETHODS
  /// <summary>
  /// Uniwersalna struktura wykorzystywana w metodzie IAIOrderToEcomOrderInfo w ECOMADAPTERIAI
  /// Przechowuje dane o dostawie to klienta jak termin, koszt dostawy itp.
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("1d0e6950134a4a1abf56162d2874d43d")]
  public class EcomDispatchInfo
  {    
      public string DispatchId {get; set;}   //id sposobu dostawy z kanału sprzedaży  
      public string DispatchName {get; set;}
      public DateTime? DeliveryDate {get; set;}
      public DateTime? EstimatedDeliveryDate {get; set;}
      public decimal? DeliveryWeight {get; set;}
      public decimal? DeliveryCost {get; set;}
      public string DeliveryVat {get; set;}
  }
  
  
	//ERRSOURCE: structure EcomOrderLineInfo { ... } in object ECOM.ECOMSTDMETHODS
  /// <summary>
  /// Struktura wykorzystywana w metodzie IAIOrderToEcomOrderInfo w ECOMADAPTERIAI
  /// Przechowuje dane na temat pozycji zamówienia
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("3a10f96dfa4e445e82766812101f1b51")]
  public class EcomOrderLineInfo
  {
      public string ProductId {get;set;}
      public string ProductName {get;set;}
      public string ProductCode {get;set;}
      public string VersionName {get;set;}    
      public string StockId {get;set;}
      public string ProductSerialNumber {get;set;}
      public decimal? ProductQuantity {get;set;}
      public decimal? ProductWeight {get;set;}
      public string ProductVat {get;set;}
      public string ProductVatFree {get;set;}    
      public decimal? ProductOrderPrice {get;set;}
      public decimal? ProductOrderPriceNet {get;set;}
      public string RemarksToProduct {get;set;}    
      public string ProductSerialNumbers {get;set;}    
      public int? Position {get;set;}
  }
  
  
	//ERRSOURCE: structure EcomSpedInfo { ... } in object ECOM.ECOMSTDMETHODS
  /// <summary>
  /// Klasa zawiera dane spedycyjne wykorzystywane przy wysyłaniu komunikatów o zmiane statusu zamówienia 
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("43ff6377edea4a08b93730862e748156")]
  public class EcomSpedInfo 
  {
    ///<summary>Lista paczek przesyłki z tabeli LISTYWYSDROZ_OPK</summary>
    public int? ListwysdRef {get;set;}
    ///<summary>symbol spedytora z tabeli SPEDYTORZY</summary>
    public string ShipperSymbol {get;set;}
    ///<summary>symbol przysyłki na podstawie wpisu w tabeli LISTYWYSD</summary>
    public string ShippingSymbol {get;set;}
    ///<summary>status spedycji na podstawie wpisu w tabeli LISTYWYSD</summary>
    public int? ListywysdStatusSped {get;set;}
    ///<summary>Kwota pobrania z tabeli LISTYWYSD</summary>    
    public decimal CodValue {get;set;}
    ///<summary>Waluta pobrania z tabeli LISTYWYSD</summary>
    public string CodCurrency {get;set;}
    ///<summary>Lista paczek przesyłki z tabeli LISTYWYSDROZ_OPK</summary>
    public List<EcomPackageInfo> PackageList {get;set;}
  }
  
  
  
	//ERRSOURCE: structure SynchronizeContent { ... } in object ECOM.ECOMSTDMETHODS
  /// <summary>
  /// Atrybut służący do oznaczania, zbiorów obiektów (list i tablic), których zawartość ma być brana pod uwagę przy wysyłce danych do kanałów sprzedaży.
  /// Wykorzystywany przy eksporcie towarów np. przy liście zdjęć dla towaru
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("4ef2f27045a84d5c8da24bd99bc234c9")]
  [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
  public class SynchronizeContent : Attribute
  {}
  
  
	//ERRSOURCE: structure GetVatForVerscountryOut { ... } in object ECOM.ECOMSTDMETHODS
  [CustomData("DataStructure=Y", "DataStructureType=OUT", "FromProcedureName=GET_VAT_FOR_VERSCOUNTRY", "LogicMethodUUID=5e9f8db46a3c4c52bbf911186a08ee52")]
  [UUID("5256ab9d5f5e484fb5a2ff7f38885923")]
  public class GetVatForVerscountryOut 
  {
    /** 
     *  Klasa automatycznie wygenerowana na podstawie procedury: GET_VAT_FOR_VERSCOUNTRY
     **/
  
    // DOMAIN: VAT_ID
    [Order(0)]
    public String Vatid {get; set;}
    // DOMAIN: CENY
    [Order(1)]
    public Decimal? Vatrate {get; set;}
    // DOMAIN: STRING20
    [Order(2)]
    public String Vatname {get; set;}
  
  }
  
  
	//ERRSOURCE: structure EcomInventoryStockInfo { ... } in object ECOM.ECOMSTDMETHODS
  /// <summary>
  /// Uniwersalna struktura zawierające dane o stanie magazynowym towaru, do zmapowania na struktury specyficzne dla kanału sprzedaży (dla eskportu),
  /// lub które zostaną zainsertowane do bazy danych (dla importu)
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("5b10596dfa0546a398966d049ace0584")]
  public class EcomInventoryStockInfo 
  {
    ///<summary>REF z tabeli ECOMINVENTORYSTOCKS</summary> 
    public Int64? EcomInventoryStockRef {get; set;}
    ///<summary>REF z tabeli ECOMCHANNELSTOCK
    public Int64? EcomChannelStockRef {get; set;}
    ///<summary>unikalne id magazynu w kanale sprzedaży, brane z tabeli ECOMCHANNELSTOCK</summary>
    public string ChannelStockId {get;set;}
    ///<summary>do wysyłki stanów magazynowych używamy ref wersji towaru, brane z tabeli ECOMINVENTORIES
    public string WersjaRef {get;set;}
    ///<summary>unikalne id towaru w kanale sprzedaży, brane z tabeli ECOMINVENTORIES</summary>
    public string InventoryId {get;set;}
    ///<summary>Ilość na danym magazynie wirtualnym</summary>  
    public decimal? Quantity {get;set;} 
    ///<summary>//dane dodatkowe do przekazania do drajwera, jeśli w wdrożeniu są potrzebne dane dodatkowe</summary>
    public string ExtendData {get;set;}
  }
  
  
  
	//ERRSOURCE: structure EcomPackageInfo { ... } in object ECOM.ECOMSTDMETHODS
  /// <summary>
  /// Klasa zawiera dane spedycyjne wykorzystywane przy wysyłaniu komunikatów o zmiane statusu zamówienia na  podstawie tabeli LISTYWYSDROZ_OPK
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("5c4cacd35d084d30a4140246111f7d7d")]
  public class EcomPackageInfo 
  {
    ///<summary>ref opakowania z tabeli LISTYWYSDROZ_OPK</summary>
    public int? ListywsydrozOpkRef {get;set;}    
    ///<summary>Symbol paczki nadany przez spedytora. Brany z tabeli LISTYWYSDROZ_OPK</summary>
    public string ShipqingSymbol {get;set;}    
  }
  
  
  
	//ERRSOURCE: structure EcomAddressInfo { ... } in object ECOM.ECOMSTDMETHODS
  /// <summary>
  /// Uniwersalna struktura wykorzystywana w metodzie IAIOrderToEcomOrderInfo w ECOMADAPTERIAI. Stanowi część klasy EcomIOrderInfo przechowującej dane o zamówieniach
  /// Przechowuje ona informacje o danych adresowych, np. dane adresu odbioru, adresu do faktury itp.
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("5e417205ef45408984d6d1ed7ecee644")]
  public class EcomAddressInfo
  {
      string countryId;
      string nip;
      string firmName;
      string firstName;
      string lastName;
      string fullName;
      public string Phone {get; set;}
      public string Email {get; set;}
      public string PickupPointId {get; set;}
      public string AddressId {get; set;}
      public string AppartmentNo {get; set;}
      public string BuildingNo {get; set;}
      public string Street {get; set;}
      public string ZipCode {get; set;}
      public string City {get; set;}
      public string Province {get; set;}
      public string CountryId 
      {
          get
          {
              return countryId;
          } 
          set
          {
              countryId = value;
              countryId = value.ToUpper().Trim();
          }
      }
      public string CountryName {get; set;}
      public string FirmName
      {
          get
          {
              return firmName;
          }
          set
          {
              if(string.IsNullOrEmpty(value))
              {
                  firmName = "";
              }
              else
              {
                  firmName = value.Trim();
              }
          }
      }
      public string FirstName
      {
          get
          {
              return firstName;
          } 
          set
          {   
              if(string.IsNullOrEmpty(value))
              {
                  firstName = "";
              }
              else
              {
                  firstName = value.Trim();
              }
          }
      }
      
      public string LastName
      {
          get
          {
              return lastName;
          } 
          set
          {   
              if(string.IsNullOrEmpty(value))
              {
                  lastName = "";
              }
              else
              {
                  lastName = value.Trim();
              }
          }
      }
  
      public string FullName
      {
          get
          {
              return fullName;
          } 
          set
          {   
              if(string.IsNullOrEmpty(value))
              {
                  fullName = "";
              }
              else
              {
                  fullName = value.Trim();
              }
          }
      }
  
      public string Nip 
      {
          get
          {
              return nip;
          } 
          set
          {   
              if(string.IsNullOrEmpty(value))
              {
                  nip = "";
              }
              else
              {
                  nip = value.ToUpper().Trim();
              }
          }
      }
  }
  
  
	//ERRSOURCE: structure GetCheckSumData { ... } in object ECOM.ECOMSTDMETHODS
  /// <summary>
  /// Struktura danych wykorzystywana do zwracania danych z metody logiki GetCheckSum, obliczającej aktualną sumę kontrolną danych zamówienia
  ///  - OrderStateRef - ref statusu zamówienia z tabeli ECOMCHANNELSTATES
  ///  - OrderStateCheckSum - suma kontrolna statusu zamówienia
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("632512bbed404abdaa608e8c994e0790")]
  public class GetCheckSumData
  {
      public int OrderStateRef {get;set;} 
      public string OrderStateCheckSum {get;set;}
  }
  
  
	//ERRSOURCE: structure EcomInventoryPriceInfo { ... } in object ECOM.ECOMSTDMETHODS
  [CustomData("DataStructure=Y")]
  [UUID("91f41ad7535b46f695284e4756e39dda")]
  public class EcomInventoryPriceInfo 
  {
    ///<summary>REF towaru z tabeli ECOMINVENTORYPRICES</summary> 
    public Int64? EcomInventoryPriceRef {get; set;}
    ///<summary>unikalne id towaru w kanale sprzedaży, odpowiednik ECOMINVENTORYID</summary>
    public string InventoryId {get;set;}       
    //<summary>Cena detaliczna brutto</summary> 
    public decimal? RetailPrice {get;set;}
    ///<summary>Cena detaliczna Netto</summary>
    public decimal? RetailPriceNet {get;set;}
    ///<summary>Cena prmocyjna brutto</summary>
    public decimal? DiscountPrice {get;set;}
    ///<summary>Cena promocyjna netto</summary>
    public decimal? DiscountPriceNet {get;set;}
    ///<summary>Waluta</summary>
    public string Currency {get;set;}
    ///<summary>//dane dodatkowe do przekazania do drajwera, jeśli w wdrożeniu są potrzebne dane dodatkowe</summary>
    public string ExtendData {get;set;} 
  }
  
  
  
	//ERRSOURCE: structure GetVatForVerscountryIn { ... } in object ECOM.ECOMSTDMETHODS
  [CustomData("DataStructure=Y", "DataStructureType=IN", "FromProcedureName=GET_VAT_FOR_VERSCOUNTRY", "LogicMethodUUID=5e9f8db46a3c4c52bbf911186a08ee52")]
  [UUID("ac2b97e8fbe845e8954df19e8c2f9802")]
  public class GetVatForVerscountryIn 
  {
    /** 
     *  Klasa automatycznie wygenerowana na podstawie procedury: GET_VAT_FOR_VERSCOUNTRY
     **/
  
    // DOMAIN: COUNTRY_ID
    [Order(0)]
    public String Country {get; set;}
    // DOMAIN: WERSJE_ID
    [Order(1)]
    public Int32? Vers {get; set;}
    // DOMAIN: KTM_ID
    [Order(2)]
    public String Ktm {get; set;}
    // DOMAIN: DATE_ID
    [Order(3)]
    public DateTime? Fordate {get; set;}
  
  }
  
  
	//ERRSOURCE: structure EcomOrderStatusInfo { ... } in object ECOM.ECOMSTDMETHODS
  /// <summary>
  /// Struktura wykorzystywana w metodzie IAIOrderToEcomOrderInfo w ECOMADAPTERIAI
  /// Przechowuje dane na temat aktualnego statusu zamówienia (statusu realizacji, płatności)
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("acef075ea5404eb2b0a55cfb2976f95c")]
  public class EcomOrderStatusInfo 
  {   
      //struktura danych o statusie zamówienia, używana przy wysłąniu zamówienia, jak i uzupełnianiana przy wypełnianiu EcomOrderInfo przy odbiorze
      //dane redundantne przydatne przy  wysyłaniu statusu jako samodzielnej struktury
      public Int64? EcomOrderRef {get; set;}
      public Int64? EcomOrderStatusRef {get;set;}
      public string EcomOrderEDAID {get; set;} //identyfikator EDA
      public string OrderId {get; set;}//id zamowienia w kanale sprzedazy
      public string OrderSymbol {get; set;}//symbol zamówienia w kanale sprzedazy
      public string OrderStatusId {get; set;}//identyfikator statusu w kanale sprzedazy
      public string DropshippingOrderStatus {get;set;}
      public string PaymentStatus {get;set;}
      public EcomSpedInfo OrderSpedInfo {get;set;}
      public EcomAttachmentInfo InvoiceInfo {get; set;}
      public string ExtendData {get;set;}//dane dodatkowe do przekazania do drajwera, jeśli w wdrozęniu są potrzebne dane dodatkowe    
  }
  
  
	//ERRSOURCE: structure CalculateMode { ... } in object ECOM.ECOMSTDMETHODS
  /// <summary>
  /// Typ wyliczeniowy oznaczający w jakim trybie działają metody naliczania danych do eksportu stanó magzaynowych, cen itp.. 
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("b35631247ab7432fa4b349764e886e3a")]
  public enum CalculateMode 
  { 
      ///<summary>nalicza tylko ceny oznaczone jako nieważne w ECOMINVETORYPRICES</summary>
      Invalid = 0,
      ///<summary>nalicza tylko dla wskazanych towarów w ECOMINVENTORIES</summary>
      List,
      ///<summary>aktualizuje wszystkie ceny i nalicza nowe/ brakujące wpisy do cennika</summary>
      All
  }
  
  
  
	//ERRSOURCE: structure EcomInventoryUnitInfo { ... } in object ECOM.ECOMSTDMETHODS
  /// <summary>
  /// Uniwersalna struktura do przechowywania danych jednostek dla towaru (TOWJEDN)
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("cfcde48f4fe14f78b6aa27ddb55a7dc8")]
  public class EcomInventoryUnitInfo 
  {
      ///<summary>unikalny symbol jednostki w kanale sprzedaży</summary>
      public string UnitId {get; set;} 
      ///<summary>unikalny symbol podstawowej jednostki w kanale sprzedaży</summary>
      public string BaseUnitId {get; set;}
      ///<summary>flaga, czy podana jednostka jest jednostką podstawową</summary>
      public bool? IsBaseUnit {get; set;}
      ///<summary>Przelicznik jednostki na jednostkę podstawową</summary>
      public decimal? Factor {get; set;}   
      ///<summary>Waga opakowania</summary>
      public decimal? Weight {get;set;}
      ///<summary>Wysokość opakowania</summary>
      public decimal? Height {get;set;}
      ///<summary>Szerokość opakowania</summary>
      public decimal? Width {get;set;}
      ///<summary>Długość opakowania</summary>
      public decimal? Length {get;set;}
      ///<summary>Kod kreskowy</summary>
      public string EAN {get; set;}    
  }
  
  
	//ERRSOURCE: structure EcomPrepaidInfo { ... } in object ECOM.ECOMSTDMETHODS
  /// <summary>
  /// uniwersalna struktura przechowująca dane o wpłatach dla zamówienia
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("e04e180e142b4d31aeb3313f8532c85d")]
  public class EcomPrepaidInfo 
  {
      public string PrepaidId {get; set;}
      public string Currency {get;set;}
      public decimal? Value {get;set;}
      public DateTime? PaymentDate {get;set;}
      public string PaymentType {get;set;}    
  }
  
  
	//ERRSOURCE: structure EcomInventoryImageInfo { ... } in object ECOM.ECOMSTDMETHODS
  /// <summary>
  /// Uniwersalna struktura wykorzystywana w metodzie IAIOrderToEcomOrderInfo w ECOMADAPTERIAI
  /// Przechowuje dane na temat zdjęcia towaru
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("e0df3bd44fe145b4b5614e53c7307e60")]
  public class EcomInventoryImageInfo 
  {
      ///<summary>Identyfikator towaru w kanale sprzedaży, odpowiednik ECOMINVENTORYID</summary>    
      public string InventoryId {get; set;} 
      ///<summary>Ref wersji towaru</summary>
      public int? WersjaRef {get; set;}
      ///<summary>dane zdjęcia np. kod base64 albo adres url</summary>
      public string PictureSource {get; set;}  
      ///<summary>typ danych zdjęcia np: (base64, url)</summary>
      public string PictureSourceType {get; set;} 
      ///<summary>Numer zdjęcia towaru, liczba porządkowa na podstawie TWJEND.NUMER</summary>
      public int? PictureNumber {get; set;} 
      ///<summary></summary>
      public int? PicturePrio {get; set;} // Numer zdjęcia towaru po ustawieniu    
  }
  
  
	//ERRSOURCE: structure EcomOrderInfo { ... } in object ECOM.ECOMSTDMETHODS
  /// <summary>
  /// Uniwersalna struktura wykorzystywana np. w metodzie IAIOrderToEcomOrderInfo w ECOMADAPTERIAI
  /// Zawiera komplet danych o zamówieniu
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("efc6bf9e2335499fb1fc830a47bb6054")]
  public class EcomOrderInfo
  {
      public string OrderId {get;set;} //id unikalne zamówienia w kanale sprzedaży, odpowiednik ECOMORDER.ECOMORDERID
      public string OrderSymbol {get;set;}//sybol "human redable" zamówienia w kanale sprzedaży, odpowiednik ECOMORDER.ECOMORDERSYMBOL
      public int? NagzamRef {get; set;} // ref zamówienia z tabeli: NAGZAM.REF      
      public string OrderBridgeNote {get;set;}
      public string OrderType {get;set;}
      public string OrderConfirmation {get;set;}
      public DateTime? OrderAddDate {get;set;}
      public DateTime? OrderChangeDate {get;set;}
      public DateTime? OrderDispatchDate {get;set;}    
      public string NoteToCourier {get;set;}
      public string NoteToOrder {get;set;}
      public string OrderSourceType {get;set;}
      public string OrderSourceName  {get;set;}
      public string OrderSourceTypeId  {get;set;}    
      public EcomOrderStatusInfo OrderStatus {get;set;}    
      public EcomClientAccountInfo ClientAccount {get;set;}
      public List<EcomOrderLineInfo> OrderLines {get; set;}
      public EcomAddressInfo DeliveryAddress {get; set;}
      public EcomAddressInfo BillingAddress {get; set;}
      public EcomAddressInfo PickupPointAddress {get; set;}
      public EcomDispatchInfo Dispatch {get; set;}
      public EcomPaymentInfo Payment {get; set;}
      public List<EcomPrepaidInfo> Prepaids {get; set;}
      public bool IsOrderCancelled {get; set;}     
      public Int64? EcomOrderRef {get; set;}
      public string StockId {get;set;}      
  }
  
  
	//ERRSOURCE: structure EcomInventoryInfo { ... } in object ECOM.ECOMSTDMETHODS
  /// <summary>
  /// Uniwersalna struktura wykorzystywana np. w metodzie IAIOrderToEcomOrderInfo w ECOMADAPTERIAI
  /// Zawiera komplet danych o towarze
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("fff6585a50a944f4b782363a86480d4d")]
  public class EcomInventoryInfo 
  {
      ///<summary>KTM (w baselinkerze: SKU) dla towaru</summary>
      public string KTM {get;set;}
      ///<summary>unikalne id towaru w kanale sprzedaży, odpowiednik ECOMINVENTORYID</summary>
      public string InventoryId {get;set;}
      ///<summary>czy towar jest widoczny na witrynie</summary>   
      public int? IsVisible {get; set;}     
      [SynchronizeContent]
      ///<summary>Nazwy towaru w różnych jezykach</summary> 
      public List<EcomInventoryTextInfo> InventoryNames {get; set;} 
      [SynchronizeContent]
      ///<summary>Opisy towaru w różnych jezykach</summary>  
      public List<EcomInventoryTextInfo> InventoryDescriptions {get; set;}
      ///<summary>Ref wersji dla towaru</summary>     
      public int? WersjaRef {get;set;}
      ///<summary>Id grupy towaru odpowiednik TOWARY.GRUPA</summary>     
      public string CategoryId {get;set;} 
      public string CategoryName {get;set;}    
      public string CurrencyId {get;set;}
      ///<summary>Typ towaru, odpowiednik TOWARY.USLUGA</summary>
      public string Type {get;set;}  
      [SynchronizeContent]
      ///<summary>Lista jednostek towaru, na podstawie tablei TOWJEDN</summary> 
      public List<EcomInventoryUnitInfo> Units {get; set;}
      ///<summary>Wartość stawki VAT na podstawie VAT.STAWKA</summary>    
      public string Vat {get;set;}
      ///<summary>Flaga oznaczająca brak stawki VAT (nie to samo co stawka 0%)</summary>
      public bool? VatFree {get;set;}                
      [SynchronizeContent]
      ///<summary>Lista zdjęć dla towaru na podstaiwe TOWPLIKI</summary> 
      public List<EcomInventoryImageInfo> Images {get;set;}
      ///<summary>Cena detaliczna brutto</summary> 
      public decimal? RetailPrice {get;set;}
      ///<summary>Cena detaliczna Netto</summary>
      public decimal? RetailPriceNet {get;set;}
      ///<summary>Cena hurtowa brutto</summary>
      public decimal? WholesalePrice {get;set;}
      ///<summary>Cena hurtowa netto</summary>
      public decimal? WholesalePriceNet {get;set;}
      ///<summary>Cena hurtowa brutto</summary>
      public decimal? PosPrice {get;set;}
      ///<summary>Cena hurtowa netto</summary>
      public decimal? PosPriceNet {get;set;}
      ///<summary>REF towaru z tabeli ECOMINVENTORIES</summary>
      public Int64? EcomInventoryRef {get; set;}
      ///<summary>//dane dodatkowe do przekazania do drajwera, jeśli w wdrożeniu są potrzebne dane dodatkowe</summary>
      public string ExtendData {get;set;}
      ///<summary>Kod kreskowy dla wersji (a gdy nie jest przypisany do wersji, to towaru)</summary>
      public string EAN {get;set;}
  }
  [CustomData("DataStructure=Y")]
  [UUID("3120055f4b1648c9b9837d99d9863aff")]
  public class EcomOrderPackageInfo 
  {
      public int OrderId {get;set;}
      public int? InternalPackageId {get; set;}
      public int ExternalPackageId {get; set;}
      public DateTime? PackageDate {get; set;}
      public string PackageNumber {get; set;}
      public string CourierCode {get; set;}
      public string LabelPath {get; set;}
  }
  [CustomData("DataStructure=Y")]
  [UUID("9a925bfb240a4032bff5a099afce3a6c")]
  public class EcomPackageLabelInfo 
  {
      public int OrderId {get;set;}
      public int PackageId {get; set;}
      public string LabelPath {get; set;}
      public int LabelType {get; set;}
  }
}
