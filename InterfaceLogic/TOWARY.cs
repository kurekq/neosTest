
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
//ERRSOURCE: ECOM.TOWARY

  public partial class TOWARY
  {

  }
	//ERRSOURCE: structure VersionChanged { ... } in object ECOM.TOWARY
  [CustomData("DataStructure=Y")]
  [UUID("28aec6c0d47b4bd98361807309d75063")]
  public class VersionChanged  : NeosEvent
  {
      public int WersjaRef {get; set;}
      public string Ktm {get; set;}
      public string Message {get; set;}
  }
  
  
	//ERRSOURCE: structure InventoryChanged { ... } in object ECOM.TOWARY
  [CustomData("DataStructure=Y")]
  [UUID("2b8c848fc83e43158a55cbbe82ed1524")]
  public class InventoryChanged : NeosEvent
  {
      public string Ktm {get; set;}
      public string Message {get; set;}
  }
  
  
	//ERRSOURCE: structure UnitChanged { ... } in object ECOM.TOWARY
  [CustomData("DataStructure=Y")]
  [UUID("35f3c1530e6646d4acfce93a5a012e23")]
  public class UnitChanged : NeosEvent
  {
      public int TowjednRef {get; set;}
      public string Ktm {get; set;}
      public string Message {get; set;}    
  }
  
  
	//ERRSOURCE: structure FileChanged { ... } in object ECOM.TOWARY
  [CustomData("DataStructure=Y")]
  [UUID("8bf6483dd67a434899a5eaf379caa5c7")]
  public class FileChanged : NeosEvent
  {
      public int WersjaRef {get; set;}
      public int Numer {get; set;}
      public string Ktm {get; set;}
      public int TowplikiRef {get; set;}
      public string Message {get; set;}
      public string Type {get; set;}
  }
  
  
	//ERRSOURCE: structure AttributeChanged { ... } in object ECOM.TOWARY
  [CustomData("DataStructure=Y")]
  [UUID("cc5895ea00684b2892cd360a84c39683")]
  public class AttributeChanged : NeosEvent
  {
      public int WersjaRef {get; set;}
      public string Ktm {get; set;}
      public string Cecha {get; set;}
      public string Message {get; set;}
  }
  
  
	//ERRSOURCE: structure BarcodeChanged { ... } in object ECOM.TOWARY
  [CustomData("DataStructure=Y")]
  [UUID("f5e437afbec24fcdba718767e934ab39")]
  public class BarcodeChanged : NeosEvent
  {
      public int TowkodkreskRef {get; set;}    
      public string Ktm {get; set;}
      public int WersjaRef {get; set;}
      public string Message {get; set;}
  }
}
