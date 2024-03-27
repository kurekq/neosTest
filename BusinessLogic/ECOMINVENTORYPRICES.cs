
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
  
  public partial class ECOMINVENTORYPRICES<TModel> where TModel : MODEL.ECOMINVENTORYPRICES, new()
  {

    /// <param name="context"></param>
    [CustomData("MessageHandlerType=E")]
    [UUID("786eed24e669466790dfb929a53abf31")]
    virtual public HandlerResult PriceChangedHandler(ConsumeContext<ECOM.PriceChanged> context)
    {
      try
      { 
        OnPriceChangeOrDelete(context.Message.DefcennikRef, context.Message.WersjaRef, context.Message.TowjednRef);
        return HandlerResult.Handled;
      }
      catch(Exception ex)
      {
        context.Message.Message = ex.Message;
        throw new Exception(ex.Message);
      }
    }
    
    
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=E")]
    [UUID("99d323d88d3e40f899bb4f3cd3477eee")]
    virtual public HandlerResult PriceDeletedHandler(ConsumeContext<ECOM.PriceDeleted> context)
    {
      try
      { 
        OnPriceChangeOrDelete(context.Message.DefcennikRef, context.Message.WersjaRef, context.Message.TowjednRef);
        return HandlerResult.Handled;
      }
      catch(Exception ex)
      {
        context.Message.Message = ex.Message;
        throw new Exception(ex.Message);
      }
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("e2236a08f70c4b1d95a73b7541de751e")]
    [Wait(5000)]
    virtual public HandlerResult CheckInventoryPriceChangedCommandHandler(ConsumeContext<ECOM.CheckInventoryPriceChangedCommand> context)
    {
      if (context.ShouldIgnore())
      {
        return HandlerResult.Ignored;  
      }
      else
      { 
        //wyszukujemy aktualne wpisy o cenie dla towaru we wszystkich kanałach sprzedaży i ustawiamy, że są nieaktywne
        //resztę zrobi naliczenie cennika w schedulerze, albo naliczenie przez operatora
        string sqlString = 
          $@"select p.ref, i.ecomchannelref from ecominventories i
            join ecominventoryprices p on p.ecominventoryref = i.ref
              and p.isvalid = 1 
            where i.ACTIVE = 1 and i.wersjaref = {context.Message.WersjaRef}
            order by i.ecomchannelref";
    
        //sprawdzamy każdy kanal sprzedaży
        var inventoryPrice = new ECOM.ECOMINVENTORYPRICES();
        int ecomChannelRef = 0;
        int? defCennikForEcomChannel = null;
        Contexts channelParams = null;
        
        foreach(var dataRow in CORE.QuerySQL(sqlString))
        {
          inventoryPrice.FilterAndSort($"{nameof(ECOM.ECOMINVENTORYPRICES)}.{inventoryPrice.REF.Symbol} = 0{dataRow["REF"]}");
          if(inventoryPrice.FirstRecord())
          {
            if(dataRow["ECOMCHANNELREF"].AsInteger != ecomChannelRef)
            {
              ecomChannelRef = dataRow["ECOMCHANNELREF"].AsInteger; 
              channelParams = LOGIC.ECOMSTDMETHODS.LoadEcomChannelParams(ecomChannelRef); 
              if(string.IsNullOrEmpty(channelParams["_clientprice"]))
              {
                throw new Exception($"Nie znaleziono klienta do naliczania cen dla kanału sprzedaży: {ecomChannelRef}");
              }
              var klient = new LOGIC.KLIENCI().Get(channelParams["_clientprice"].AsInteger);
              defCennikForEcomChannel = klient?.CENNIK;
              if(defCennikForEcomChannel == null)
              {
                throw new Exception($"Nie znaleziono cennika na kliencie do naliczania cen dla kanału sprzedaży: {ecomChannelRef}");
              }
            }
    
            if (defCennikForEcomChannel == context.Message.DefcennikRef)
            {
              inventoryPrice.EditRecord();
              inventoryPrice.ISVALID = 0; //ustawiamy, że wartość nieaktualna        
              if(!inventoryPrice.PostRecord())
              {
                throw new Exception($"Błąd ustawienia ceny jako nieaktualnej o REF: {dataRow["REF"]}. Błąd zapisu do bazy danych");
              }	
            }        
          }
          else
          {
            //Jeżeli FilterAndSort nie znalazł wiersza, który wyszukujemy po ref to coś jest bardzo nie tak,
            //dlatego przerywamy całą pętle i rzucamy wyjątek
            throw new Exception($"Nie znaleziono wpisu dla o cenie dla towaru o wersji: {context.Message.WersjaRef}");
          }          
        }
        inventoryPrice.Close(); 
        return HandlerResult.Handled;   
      }
    }
    
    


    /// <param name="DefcennikRef"></param>
    /// <param name="WersjaRef"></param>
    /// <param name="TowjednRef"></param>
    [UUID("f191f6e2cd6c47fe8cd0c3a4a7525726")]
    virtual public void OnPriceChangeOrDelete(int DefcennikRef, int WersjaRef, int TowjednRef)
    {
      //tu usatwiamy, że trzeba unieważnić ceny w kanałach sprzedaży   
      var command = new ECOM.CheckInventoryPriceChangedCommand()     
      {      
        DefcennikRef = DefcennikRef,
        WersjaRef = WersjaRef,
        TowjednRef = TowjednRef
      };
      //uruchamiamy komendę zamiast obsługi w evencie, żeby obsłużyć deduplikację na wszelki wypadek
      command.SetDeduplicationIdentifier($"INVENTORYPRICE_{DefcennikRef.ToString()}" +
        $"_{WersjaRef.ToString()}_{TowjednRef.ToString()}");    
      EDA.SendCommand("ECOM.ECOMINVENTORYPRICES.CheckInventoryPriceChangedCommandHandler",command);
    }
    
    


  }
}
