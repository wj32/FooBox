﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace FooBox
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "FileBrowse",
                url: "Home/{*path}",
                defaults: new { controller = "File", action = "Browse", path = "" }
                );

            routes.MapRoute(
                name: "Client",
                url: "Client/{action}/{id}/{secret}",
                defaults: new { controller = "Client" }
                );

            routes.MapRoute(
                name: "DownloadKey",
                url: "k/{key}",
                defaults: new { controller = "File", action = "DownloadKey" }
                );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
                );
        }
    }
}
