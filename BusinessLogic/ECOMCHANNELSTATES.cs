
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
  
  public partial class ECOMCHANNELSTATES<TModel> where TModel : MODEL.ECOMCHANNELSTATES, new()
  {

    /// <summary>
    /// metoda serializuje dane stasusu zamówienia do pliku JSON i wylicza string z ich sumą kontrolną za pomocą algorytmu MD5
    /// </summary>
    /// <param name="channelRef"></param>
    /// <param name="ordStatData"></param>
    [UUID("662dc03bcaca4593991d92f9b9447548")]
    public static string GetOrderStateCheckSum(int channelRef, NagzamStatusChanged ordStatData)
    { 
      //metoda serializuje dane stasusu zamówienia do pliku JSON i wylicza string z ich sumą kontrolną za pomocą algorytmu MD5  
      string stringToHash = JsonConvert.SerializeObject(ordStatData);
      string hash;
      using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create()) 
      {
        hash = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(stringToHash))).Replace("-", String.Empty);
      }  
      return hash.Remove(0, 12);
    }
    
    


    /// <summary>
    /// Metoda wyliczająca status zamówienia. na podstawie otrzymanych danych zamówienia i
    /// danych z tabeli ECOMCHANNELSTATES.
    /// 
    /// parametry:
    /// channelRef - ref kanału sprzedaży, w którym wyliczany będzie status zamówienia
    /// orderStatusData - dane wejściowe zamówienia 
    /// zwraca ref nowego statusu z ECOMCHANNELSTATES lub 0
    /// 
    ///  
    /// </summary>
    /// <param name="channelRef"></param>
    /// <param name="orderStatusData"></param>
    [UUID("707245eb77d14f31902342da99c3e208")]
    public static int CalcOrderStateInChannel(int channelRef, NagzamStatusChanged orderStatusData)
    {
      int channelOrderStateRef = 0;
    
      //wyliczanie statusu zaówienia na podstawie ECOMCHANNELSTATES.NAGZAMQUERY,
      //brane od końca po kolejności obliczania statusów 
      //(jeżeli zamówienie spełnia warunki bycia w pakowaniu to nie sprawdzamy czy jest nowe)
      string selectStatuses = 
        "select ref, symbol, nagzamquery from ECOMCHANNELSTATES s " +
          $"where s.ecomchannelref = {channelRef} " +
            "and s.calcorder is not null " +
            "and coalesce(s.nagzamquery, '') != '' " +
            "order by calcorder desc ";
    
      var statusList = CORE.QuerySQL(selectStatuses);
      foreach(var stausRow in statusList)
      {
        try
        {
          //sprawdzamy, czy dane zawarte orderStatusData w spełniają warunki zawarte w wyrażeniu filtrującym
          //jezeli tak to zwracamy status odpowiadający filtrowi jako aktualny           
          if(CompareOrderStateDataWithFilter(orderStatusData, stausRow["NAGZAMQUERY"]))    
          {        
            channelOrderStateRef = stausRow["REF"].AsInteger;
            break;        
          } 
        }
        catch(Exception ex)
        {
          throw new Exception("Błąd określania statusu zamówienia na podstawie filtru " 
            + stausRow["NAGZAMQUERY"] + "\n" + ex.Message);
        }    
      }
      return channelOrderStateRef; 
    }
    
    


    /// <summary>
    /// Metoda sprawdza czy status zamówienia jest aktualny uruchamiając metodę wyliczania sumy kontrolnej statusu dla zamówienia.
    /// Jeśli jest nieaktualny to zaznacza, że status zamówienia w kanale sprzedaży jest nieaktualny i zwraca true
    /// 
    /// parametry: 
    /// orderRef - ref zamówienia z tabeli ECOMORDERS
    /// orderStatusData - dane zamówienia, na podstawie których wyliczany jest status
    /// 
    /// zwracane:
    /// CheckOrderStateUpdateResult
    ///   StateHasChanged - flaga oznaczająca, że status się zmienił
    ///   OrderStatusInfo - zawiera dane na temat nowego statusu zamówienia
    /// 
    /// </summary>
    /// <param name="orderRef"></param>
    /// <param name="orderStatusData"></param>
    [UUID("7a3fe3a03ee44207a2b601ea3fc1ac89")]
    public static CheckOrderStateUpdateResult CheckOrderStateUpdate(int orderRef, NagzamStatusChanged orderStatusData)
    {
      CheckOrderStateUpdateResult result = new CheckOrderStateUpdateResult()
      {
        StateHasChanged = false
      };
    
      ECOM.ECOMORDERS ecomOrder = new ECOM.ECOMORDERS(); 
      ecomOrder.FilterAndSort($"{nameof(ECOM.ECOMORDERS)}.{ecomOrder.REF.Symbol} = 0{orderRef}");
      if(!ecomOrder.FirstRecord())
      {
        throw new Exception($"Nie znaleziono zamówienia o ref {orderRef}");
      }
      
      //wyliczenie sumy kontrolnej dla parametrów satusu
      string orderStateCheckSum = GetOrderStateCheckSum(ecomOrder.ECOMCHANNELREF.AsInteger, orderStatusData);
      if(ecomOrder.OREDERSTATUSCHECKSUM != orderStateCheckSum)
      {
        //wyliczenie statusu w kanale sprzedaży na podstawie przekazanych danych metodą parametryzowaną dla danego kanału sprzedaży 
        var methodsObject = LOGIC.ECOMCHANNELS.FindMethodObject(ecomOrder.ECOMCHANNELREF.AsInteger);
        result.OrderStatusInfo = methodsObject.Logic.GetOrderStatusInfo(ecomOrder.REF.AsInteger,orderStatusData);
        ecomOrder.OREDERSTATUSCHECKSUM = orderStateCheckSum;
       
        //ustawiamy, że status nieaktualny w kanale sprzedaży jeżeli się zmienił 
        if(ecomOrder.ECOMCHANNELSTATE != result.OrderStatusInfo.EcomOrderStatusRef && result.OrderStatusInfo.EcomOrderStatusRef != 0)
        {
          ecomOrder.EditRecord();
          ecomOrder.SYNCSENDCHANELLSTATUS = (int)EcomSyncStatus.ExportPending;
          ecomOrder.ECOMCHANNELSTATE = result.OrderStatusInfo.EcomOrderStatusRef;
          result.StateHasChanged = true;
        }   
    
        if(!ecomOrder.PostRecord())
        {
          throw new Exception($"Błąd aktualizacji statusu zamówienia o ref {orderRef}");
        }  
      }
      ecomOrder.Close();
      return result;
    }
    
    


    /// <param name="orderData"></param>
    /// <param name="filterQuery"></param>
    [UUID("7a8849ea80ae4456ac3b3722c935d15b")]
    public static bool CompareOrderStateDataWithFilter(object orderData, string filterQuery)
    {
      //[ML] do przeniesienia do ECOMORDERS
      var p = System.Linq.Expressions.Expression.Parameter(orderData.GetType(), orderData.GetType().Name);
      var e = System.Linq.Dynamic.Core.DynamicExpressionParser.ParseLambda(new[] { p }, null, filterQuery);
      var result = e.Compile().DynamicInvoke(orderData);
      return (bool) result;
    }
    
    


  }
}
