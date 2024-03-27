
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
//ERRSOURCE: ECOM.GETALLECOMCHANNELSSTATS

  public partial class GETALLECOMCHANNELSSTATS
  {
    [UUID("0918e8e361054257a8c8860e35f323f4")]
    virtual public bool IsOrdersErrorCountWarningVisible()
    {
      if(ORDSERRORCOUNT.AsInteger > 0)
      {
        return true;
      }
      return false;
    }
    
    

    [UUID("2759e81433de466fb40a21cf5bb75303")]
    virtual public bool IsInventoriesErrorCountVisible()
    {
      if(INVENTERRORCOUNT.AsInteger == 0)
      {
        return true;
      }
      return false;
    }
    
    

    /// <param name="html"></param>
    [UUID("313499552bbc4e5887d1bfcee53ff091")]
    virtual public string SetGETALLECOMCHANNELSSTATSRowTemplate(HtmlTemplate html)
    {
        var result = "";
        /*
        var result =@"
                
            <div class='blueStyle'; style='padding:8px; margin:5px!important; display: inline-block;'>
    			<div style='margin:2px; color:\\#fff;'>             
                    <div style='font-size:14px; font-weight:400; text-align: center'> Ilość kanałów sprzedaży:</div> 
                    <div style='font-size:32px; text-align: center; font-weight:600; line-height:52px '>" + html.Val(this.CHANNELSCOUNT) + @"</div>                
                </div>
    		</div>
    	    <div class='grayStyle'; style='padding:8px; margin:5px!important; display: inline-block;'>
    			<div style='margin:2px;'>
                    <div style='font-size:14px; font-weight:400; text-align: center'> Ilość zamówień z dzisiaj:</div> 
                    <div style='font-size:32px; text-align: center; font-weight:600; line-height:52px '>" + html.Val(this.DAILYORDCOUNT) + @"</div> 
                </div>
    		</div>
            <div class='orangeStyle'; style='padding:8px; margin:5px!important; display: inline-block;'>
    			<div style='margin:2px; color:\\#fff;'>
                    <div style='font-size:14px; font-weight:400; text-align: center'> Ilość towarów z błędami:</div> 
                    <div style='font-size:32px; text-align: center; font-weight:600; line-height:52px '>" + html.Val(this.INVENTERRORCOUNT) + @"</div> 
                </div>
    		</div>
            <div class='blueStyle'; style='padding:8px; margin:5px!important; display: inline-block;'>
    			<div style='margin:2px; color:\\#fff;'>             
                    <div style='font-size:14px; font-weight:400; text-align: center'> Ilość zamówień z błędami:</div> 
                    <div style='font-size:32px; text-align: center; font-weight:600; line-height:52px '>" + html.Val(this.ORDSERRORCOUNT) + @"</div>                
                </div>
    		</div>";
            */
      return result;
    }
    
    

    [UUID("3ff9c5516c4d4eaa8657b415d0049391")]
    virtual public bool IsDailyOrdsCountVisible()
    {
      if(DAILYORDCOUNT == 0)
      {
        return true;
      }
      return false;
    }
    
    

    [UUID("43bd30ba9b7741f3a630e81c6216dc07")]
    virtual public bool IsDailyOrdsCountWarningVisible()
    {
      if(DAILYORDCOUNT.AsInteger > 0)
      {
        return true;
      }
      return false;
    }
    
    

    [UUID("493183c25ed845b99bbf18de65972343")]
    virtual public bool IsInventoriesErrorCountWarningVisible()
    {
      if(INVENTERRORCOUNT.AsInteger > 0)
      {
        return true;
      }
      return false;
    }
    
    

    [UUID("80415cbe1ef94e5291ecb9bdd8f688e1")]
    virtual public string SetDBQuery()
    {
    	return "from GET_ALL_ECOMCHANNELS_STATS";
    }
    
    

    [UUID("8543d9ecc2e54057bb091dcdaa94462c")]
    virtual public bool IsOrdersErrorCountVisible()
    {
      if(ORDSERRORCOUNT.AsInteger == 0)
      {
        return true;
      }
      return false;
    }
  }

}
