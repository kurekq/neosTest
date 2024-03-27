
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
  
  public partial class ECOMINVENTORIESGROUPS<TModel> where TModel : MODEL.ECOMINVENTORIESGROUPS, new()
  {

    /// <param name="context"></param>
    [UUID("Fill")]
    override protected IEnumerable<TModel> Fill(Contexts context)
    {
    	List<TModel> inventories = new List<TModel>();
    
      string lastChangeDate = "null";
      if (!string.IsNullOrEmpty(context["_lastchangedate"].ToString()))
      {
        lastChangeDate = "'" + context["_lastchangedate"] + "'";
      }
    	string sqlString = $@"
      select c.channelgroup as CHANNELGROUP,
        c.ref as CHANNEL,
        cg.ref as CHANNELINGROUP,
        i.ref as INVENTORYREF,
        i.wersjaref as WERSJAREF
      from ecomchannels c
          left join ecomchannels cg on c.channelgroup = cg.channelgroup
        join ecominventories i on i.ecomchannelref = c.ref ";
    
      if (int.TryParse(context["_ecomchannelref"].ToString(), out int ecomChannelRef))
      {
        sqlString += $"where c.ref = {ecomChannelRef} or cg.ref = {ecomChannelRef}";
      }
    
    	foreach (var row in CORE.QuerySQL(sqlString))
    	{
        var inventoryInGroup = new TModel();
    		inventoryInGroup.CHANNEL = row["CHANNEL"].AsInteger;
        inventoryInGroup.CHANNELINGROUP = row["CHANNELINGROUP"].AsInteger;
        inventoryInGroup.CHANNELGROUP = row["CHANNELGROUP"].AsInteger;
    		inventoryInGroup.INVENTORYREF = row["INVENTORYREF"].AsInteger;
    		inventoryInGroup.WERSJAREF = row["WERSJAREF"].AsInteger;   
    		inventories.Add(inventoryInGroup);
    	}
    	return inventories;
    }
    
    


    /// <param name="wersjaRef"></param>
    [UUID("9c6a72dd77d44390a7164f16b0411db5")]
    virtual public int? GetInventoryRef(int wersjaRef)
    {
      int? ecomInventoryRef = 0;
      var inventoriesInGroup = this.Get().Where(ig => ig.WERSJAREF == wersjaRef).ToList();
      
      ecomInventoryRef = inventoriesInGroup
        .Where(i => i.CHANNEL == this.Parameters["_ecomchannelref"].AsInteger)
        .FirstOrDefault()?.INVENTORYREF;
    
      if ((ecomInventoryRef ?? 0) == 0)
      {
        ecomInventoryRef = inventoriesInGroup.FirstOrDefault()?.INVENTORYREF;
      }   
      return ecomInventoryRef;
    }
    
    
    


  }
}
