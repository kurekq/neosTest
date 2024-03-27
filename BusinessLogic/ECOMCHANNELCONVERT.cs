
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
  
  public partial class ECOMCHANNELCONVERT<TModel> where TModel : MODEL.ECOMCHANNELCONVERT, new()
  {

    /// <summary>
    /// Metoda tłumacząca wartości pól słownikowanych otrzymywanych z Teneum na wartości w w kanale sprzedaży na podstawie tabeli 
    /// ECOMCHANNELCONVERT, wykorzystywana np. przy eksporcie towarów
    /// 
    /// parametry:
    /// - channelRef - ref kanału sprzedaży
    /// - conversionType - typ konwersji na podstawie kolumny ECOMCHANNELCONVERT.CONVTYPE, zwykle podawana jest tu nazwa tabeli Teneum np. SPOSDOST, PLATNOSCI itp.
    /// - tdValue - wartość w Teneum
    /// zwraca:
    /// - symbol w kanale sprzedaży
    /// 
    /// 
    /// </summary>
    /// <param name="channelRef"></param>
    /// <param name="conversionType"></param>
    /// <param name="tdValue"></param>
    [UUID("51c8775e789d4585b5ce6609780844fa")]
    public static string ConvertToChannelValue(int channelRef, string conversionType, string tdValue)
    {
      string channelValue = CORE.GetField("CHVAL",$"ECOMCHANNELCONVERT where ECOMCHANNELREF = 0{channelRef} and CONVTYPE = '{conversionType}' " + 
        $"and TDVAL = '{tdValue}'"); 
      return string.IsNullOrEmpty(channelValue) ? "" : channelValue;
    }
    
    


    /// <summary>
    /// Metoda tłumacząca wartości pól słownikowanych otrzymywanych z kanału sprzedaży na wartości w Teneum na podstawie tabeli 
    /// ECOMCHANNELCONVERT, wykorzystywana np. przy pobieraniu zamówień i eksporcie towarów
    /// parametry:
    /// - channelRef - ref kanału sprzedaży, którego słownik jest przeszukiwany
    /// - conversionType - typ konwersji na podstawie kolumny ECOMCHANNELCONVERT.CONVTYPE, zwykle podawana jest tu nazwa tabeli Teneum np. SPOSDOST, PLATNOSCI itp.
    /// - channelValue - symbol w kanale sprzedaży
    /// zwraca:
    /// - wartość słownikowana w Teneum, czyli np. ref sposobu dostawy
    /// 
    /// </summary>
    /// <param name="channelRef"></param>
    /// <param name="conversionType"></param>
    /// <param name="channelValue"></param>
    [UUID("c669ce035f034bdda55809977e64ca19")]
    public static string ConvertFromChannelValue(int channelRef, string conversionType, string channelValue)
    {
      string tdValue = CORE.GetField("TDVAL",$"ECOMCHANNELCONVERT where ECOMCHANNELREF = 0{channelRef} and CONVTYPE = '{conversionType}' " + 
        $"and CHVAL = '{channelValue}'"); 
      return string.IsNullOrEmpty(tdValue) ? "" : tdValue;
    }
    
    


    /// <summary>
    /// Metoda tłumacząca wartości pól słownikowanych otrzymywanych z Teneum na wartości w w kanale sprzedaży na podstawie tabeli 
    /// ECOMCHANNELCONVERT, wykorzystywana np. przy eksporcie towarów
    /// parametry:
    /// - channelRef - ref kanału sprzedaży
    /// - conversionType - typ konwersji na podstawie kolumny ECOMCHANNELCONVERT.CONVTYPE, zwykle podawana jest tu nazwa tabeli Teneum np. SPOSDOST, PLATNOSCI itp.
    /// - tdField - pole używane w celu uściślenia dla jakiej danej w Teneum odbywa się konwersja, zwykle jest to kolumna z tabeli w Teneum
    /// - tdValue - wartość w Teneum
    /// zwraca:
    /// - symbol w kanale sprzedaży
    /// 
    /// 
    /// </summary>
    /// <param name="channelRef"></param>
    /// <param name="conversionType"></param>
    /// <param name="tdField"></param>
    /// <param name="tdValue"></param>
    [UUID("efe21d149f644e17b0152a67911213fc")]
    public static string ConvertToChannelValue(int channelRef, string conversionType, string tdField, string tdValue)
    {
      string channelValue = CORE.GetField("CHVAL",$"ECOMCHANNELCONVERT where ECOMCHANNELREF = 0{channelRef} and CONVTYPE = '{conversionType}' " + 
        $"and FIELD = '{tdField}' and TDVAL = '{tdValue}'"); 
      return string.IsNullOrEmpty(channelValue) ? "" : channelValue;
    }
    
    


  }
}
