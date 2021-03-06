﻿using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using RemoteFork.Plugins;
using RemoteFork.Tools;

namespace RemoteFork.Requestes {
    public class PluginRequestHandler : BaseRequestHandler<string> {
        public const string URL_PATH = "plugin";
        public const string PARAM_PLUGIN_KEY = "plugin";

        internal static readonly Regex PluginParamRegex = new Regex($@"{PARAM_PLUGIN_KEY}(\w+)[\\]?", RegexOptions.Compiled);

        public override string Handle(HttpRequest request, HttpResponse response) {
            string pluginKey = ParsePluginKey(request);

            if (!string.IsNullOrEmpty(pluginKey)) {
                var plugin = PluginManager.Instance.GetPlugin(pluginKey);

                if (plugin != null) {
                    Log.LogDebug("Execute: {0}", plugin.Name);

                    try {
                        var query = request.Query.ConvertToNameValue();
                        var context = new PluginContext(pluginKey, request, query);
                        var pluginResponse = plugin.Instance.GetList(context);

                        if (pluginResponse != null) {
                            if (pluginResponse.source != null) {
                                Log.LogDebug(
                                    "Plugin Playlist.source not null! Write to response Playlist.source and ignore other methods. Plugin: {0}",
                                    pluginKey);
                                return pluginResponse.source;
                            } else {
                                return ResponseSerializer.ToXml(pluginResponse);
                            }
                        }
                    } catch (Exception exception) {
                        Log.LogError(exception, exception.Message);
                        response.StatusCode = (int) HttpStatusCode.BadRequest;
                        return $"Plugin: {pluginKey}";
                    }
                }
            }
            response.StatusCode = (int)HttpStatusCode.NotFound;
            return $"Plugin is not defined in request. Plugin: {pluginKey}";
        }

        private static string ParsePluginKey(HttpRequest request) {
            string pluginParam = string.Empty;
            if (request.Query.ContainsKey(string.Empty)) {
                pluginParam = request.Query[string.Empty].FirstOrDefault(s => PluginParamRegex.IsMatch(s ?? string.Empty));
            }

            var pluginParamMatch = PluginParamRegex.Match(pluginParam ?? string.Empty);

            return pluginParamMatch.Success ? pluginParamMatch.Groups[1].Value : string.Empty;
        }

        internal static string CreatePluginUrl(HttpRequest request, string pluginName, NameValueCollection parameters = null) {
            var query = new NameValueCollection {
                {string.Empty, string.Concat(PARAM_PLUGIN_KEY, pluginName, Path.DirectorySeparatorChar, ".xml")},
                {"host", request.Host.ToUriComponent()}
            };

            if (parameters != null) {
                foreach (var parameter in parameters.AllKeys) {
                    query.Add(parameter, parameters[parameter]);
                }
            }

            return CreateUrl(request, URL_PATH, query);
        }
    }
}