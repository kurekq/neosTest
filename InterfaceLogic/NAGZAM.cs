
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
//ERRSOURCE: ECOM.NAGZAM

  public partial class NAGZAM
  {
    /// <summary>
    /// automatycznie wygenerowana metoda na inicjalizację pola REF
    /// </summary>
    [UUID("8f039ff1df3b487099637bb68fd77d47")]
    virtual public string InitializeREF()
    {
      return GenRef();
    }
  }
	//ERRSOURCE: structure NagzamHeadChanged { ... } in object ECOM.NAGZAM
  [CustomData("DataStructure=Y")]
  [UUID("8693ed3f108c4503a88772b42a793e13")]
  public class NagzamHeadChanged : NeosEvent
  {
      public int NagzamRef {get; set;}
      public string Message {get; set;}
  }
  
  
	//ERRSOURCE: structure NagzamPosChanged { ... } in object ECOM.NAGZAM
  [CustomData("DataStructure=Y")]
  [UUID("9f5e030f4ae1403e80ec36e2da678363")]
  public class NagzamPosChanged : NeosEvent
  {
      public int NagzamRef {get; set;}
      public int PozzamRef {get; set;}
      public string Message {get; set;}
  }
  
  
	//ERRSOURCE: structure NagzamStatusChanged { ... } in object ECOM.NAGZAM
  /// <summary>
  /// Zdarzenie zawierające informacje nt. statusu zamówienia, na podstawie których wyliczany jest aktualny status zamówienia 
  /// </summary>
  [CustomData("DataStructure=Y")]
  [UUID("bdc45463532741728330d8f0ce6cd562")]
  public class NagzamStatusChanged : NeosEvent
  {
      public int NagzamRef {get; set;}
      public string NagzamRejestr {get; set;}
      public int? NagzamOplacone {get; set;}
      public int? NagzamAnulowano {get; set;}
      public string NagfakRef {get; set;} 
      public int? NagfakAkcept {get; set;}  
      public DateTime NagfakDataAkcept {get; set;}
      public int? NagfakWydrukowano {get; set;}  
      public string NagfakSymbol {get; set;}    
      public string TypfakSymbol {get; set;}
      public string AttachmentPath {get; set;}    
      public int? ListwysdRef {get;set;}
      public string ListwysdSymbolsped {get;set;}
      public int? ListywysdStatusSped {get; set;}
      public int? ListwysdAkcept {get;set;}      
      public decimal ListwysdPobranie  {get; set;}
      public string ListwysdPobraniewal {get; set;}
      public int? SposdostRef {get; set;}
      public List<ListywysdrozOpkInfo> ListywysdrozOpk {get; set;}
      ///<summary>pole rozszerzające dla przekazywania danych X'owych, w notacji JSON</summary>
      public string ExtendData {get; set;}
  
      ///<summary>Klasa zawierająca dane opakowań na potrzeby NagzamStatusChanged</summary>
      public class ListywysdrozOpkInfo
      {
          public int? ListywysdrozOpkRef {get; set;}
          public string ListywysdrozopkSymbolSped {get; set;}        
      }       
  }
}
