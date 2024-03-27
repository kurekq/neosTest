
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
  
  public partial class ECOMINVENTORYSTOCKS<TModel> where TModel : MODEL.ECOMINVENTORYSTOCKS, new()
  {

    /// <param name="context"></param>
    [CustomData("MessageHandlerType=E")]
    [UUID("a1940664f6d44319b3deac9924cfa5a3")]
    virtual public HandlerResult StockChangedHandler(ConsumeContext<ECOM.StockChanged> context)
    {
      try
      { 
        //tu ustawiamy, że trzeba unieważnić ceny w kanałach sprzedaży   
        var command = new ECOM.CheckInventoryStockChangedCommand()  
        {      
          DefMagaz = context.Message.DefMagaz,
          WersjaRef = context.Message.WersjaRef      
        };
        //uruchamiamy komendę zamiast obsługi w evencie, żeby obsłużyć deduplikację
        string identifier = $"INVENTORYSTOCK_{context.Message.DefMagaz?.ToString()}" +
          $"_{context.Message.WersjaRef.ToString()}";
    
        command.SetDeduplicationIdentifier(identifier);
        command.SetIdentifier(identifier);     
        EDA.SendCommand("ECOM.ECOMINVENTORYSTOCKS.CheckInventoryStockChangedCommandHandler",command);
      }
      catch(Exception ex)
      {
        context.Message.Message = ex.Message;
        throw new Exception(ex.Message);
      }
      return HandlerResult.Handled;
    }
    
    


    /// <param name="context"></param>
    [CustomData("MessageHandlerType=C")]
    [UUID("bf2ba902d3d545f1bd04101ec94e3ed4")]
    [Wait(5000)]
    virtual public HandlerResult CheckInventoryStockChangedCommandHandler(ConsumeContext<ECOM.CheckInventoryStockChangedCommand> context)
    {
      if (context.ShouldIgnore())
      {
        return HandlerResult.Ignored;  
      }
      else
      {
        //wyszukujemy aktualne wpisy o stanach magazyniwych dla towaru we wszystkich kanałach sprzedaży i ustawiamy, że są nieaktywne
        //resztę zrobi naliczenie cennika w schedulerze, albo naliczenie przez operatora
        string sqlString = 
          @"select s.ref 
            from ecominventories i
              join ecominventorystocks s on s.ecominventoryref = i.ref
                and s.isvalid = 1 " +
            $"where i.ACTIVE = 1 and i.wersjaref = {context.Message.WersjaRef}";
    
        
        //jeżeli mamy podany magazyn teneum, w którym zmienił się stan, to wyszukujemy wszystkie magazyny wirtulane, które są
        //z nim powiązane
        if(!string.IsNullOrEmpty(context.Message.DefMagaz))
        {
          string ecomChannelStockStringList = "";      
          var ecomChannelStockList = new LOGIC.ECOMCHANNELSTOCK().Get().Where(s => s.DEFMAGAZLIST.Contains(context.Message.DefMagaz)).ToList();
          if(ecomChannelStockList != null)
          {
            foreach(var ecomChannelStock in ecomChannelStockList)
            {
              ecomChannelStockStringList += ecomChannelStock.REF + ",";
            }
            ecomChannelStockStringList = ecomChannelStockStringList.Trim(new char[]{','});
    
            //doklejamy filtrowanie stanów w kanale szprzedazy po liście mag. wirtualnych
            sqlString += $" and s.ecomchannelstockref in ({ecomChannelStockStringList})";
          }
          else
          {        
            //nie ma żadnego magazynu wirtualnego, który jest powiązany z magazynem Teneum, więc nie da się zdezaktualizować
            //żadnego stanu i wyłączamy
            return HandlerResult.Handled; 
          }
        }
        
        //sprawdzamy każdy kanal sprzedaży
        var inventoryStocksLogic = new LOGIC.ECOMINVENTORYSTOCKS();
        foreach(var dataRow in CORE.QuerySQL(sqlString))
        {
          var inventoryStock =  inventoryStocksLogic.Get(dataRow["REF"].AsInteger);
          if(inventoryStock != null)
          {        
            inventoryStock.ISVALID = 0; //ustawiamy, że wartość nieaktualna
            inventoryStocksLogic.Update(inventoryStock);        
          }
          else
          {
            //Jeżeli Get nie znalazł wiersza, który wyszukujemy po ref to coś jest bardzo nie tak,
            //dlatego przerywamy całą pętle i rzucamy wyjątek
            throw new Exception($"Nie znaleziono wpisu o stanie magazynowym dla towaru o wersji:{context.Message.WersjaRef}");
          }          
        }
        return HandlerResult.Handled;
      }
    }
    
    


  }
}
