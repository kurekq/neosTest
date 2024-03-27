
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
//ERRSOURCE: ECOM.ECOMADDINVENTORIES

  public partial class ECOMADDINVENTORIES
  {
    [UUID("CancelRecord")]
    override public void CancelRecord()
    {
      base.CancelRecord();
      CloseForm();
    }
    
    

    [UUID("388aa63fa55c4e0088917942c3a1361d")]
    virtual public string SetDBQuery()
    {
      return @"from WERSJE wer 
                   left join ECOMINVENTORIES inv on inv.WERSJAREF = wer.REF " + 
                   $"and inv.ECOMCHANNELREF = 0{_channelref.AsInteger} " +
              "where wer.WITRYNA = 1 and wer.AKT = 1 and inv.REF is null";
    }
    
    

    /// <summary>
    /// automatycznie wygenerowana metoda na inicjalizację pola REF
    /// </summary>
    [UUID("8158ee9983de46beb79874843e301f6b")]
    virtual public string InitializeREF()
    {
      return GenRef();
    }
    
    
    

    [UUID("e28910ec749942dc87f19ba9b941a6cb")]
    virtual public void OperatorAddInventories()
    {
        try
        {  
            var version = new ECOM.WERSJE();
            Contexts ecomChannelParams = LOGIC.ECOMSTDMETHODS.LoadEcomChannelParams(Int32.Parse(_channelref));
            foreach(var item in SelectedRowsOrCurrent)
            {
                var inventory = new ECOM.ECOMINVENTORIES();
                version.FilterAndSort($"{nameof(ECOM.WERSJE)}.{version.REF.Symbol} = 0{item["REF"].ToString()}");
                if(version.FirstRecord())
                {
                    inventory.NewRecord();
                    inventory.ECOMCHANNELREF = _channelref;
                    inventory.WERSJAREF = version.REF;
                    if(ecomChannelParams["_sendinventoryautomatically"].AsInteger == 1)
                    {
                        inventory.ACTIVE = 1;
                        inventory.SYNCSTATUS = (int)EcomSyncStatus.ExportPending;
                    }
                    else
                    {
                        inventory.ACTIVE = 0;
                        inventory.SYNCSTATUS = (int)EcomSyncStatus.Unsynchronizable;
                    }
                    if(!inventory.PostRecord())
                    {
                        throw new Exception($"Błąd zapisu towaru o REF: {item["REF"]}");
                    }	 
                }
            }
            ShowBalloonHint($"Dodano towar.", "Informacja", IconType.INFORMATION); 
            version.Close();
        }
        catch(Exception ex)
        {
            ShowBalloonHint($"Błąd dodania towaru: " + ex.Message, "Błąd", IconType.STOP); 
        }
        
        CloseForm();
    }
  }

}
