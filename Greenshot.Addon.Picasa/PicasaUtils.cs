﻿/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom,
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Dapplo.Config.Ini;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.OAuth;
using Greenshot.Addon.Core;
using Greenshot.Addon.Interfaces;
using Greenshot.Addon.Interfaces.Plugin;

namespace Greenshot.Addon.Picasa
{
	/// <summary>
	/// Description of PicasaUtils.
	/// </summary>
	public static class PicasaUtils
	{
		private static readonly Serilog.ILogger Log = Serilog.Log.Logger.ForContext(typeof(PicasaUtils));
		private static readonly IPicasaConfiguration _config = IniConfig.Current.Get<IPicasaConfiguration>();

		/// <summary>
		/// Do the actual upload to Picasa
		/// </summary>
		/// <param name="capture">ICapture</param>
		/// <param name="progress">IProgress</param>
		/// <param name="token">CancellationToken</param>
		/// <returns>url</returns>
		public static async Task<string> UploadToPicasa(ICapture capture, IProgress<int> progress, CancellationToken token = default(CancellationToken))
		{
			string filename = Path.GetFileName(FilenameHelper.GetFilename(_config.UploadFormat, capture.CaptureDetails));
			var outputSettings = new SurfaceOutputSettings(_config.UploadFormat, _config.UploadJpegQuality);
			// Fill the OAuth2Settings

			var oAuth2Settings = new OAuth2Settings
			{
				AuthorizationUri = new Uri("https://accounts.google.com").AppendSegments("o", "oauth2", "auth").
					ExtendQuery(new Dictionary<string, string>{
						{ "response_type", "code"},
						{ "client_id", "{ClientId}" },
						{ "redirect_uri", "{RedirectUrl}" },
						{ "state", "{State}"},
						{ "scope", "https://picasaweb.google.com/data/"}
				}),
				TokenUrl = new Uri("https://www.googleapis.com/oauth2/v3/token"),
				CloudServiceName = "Picasa",
				ClientId = _config.ClientId,
				ClientSecret = _config.ClientSecret,
				RedirectUrl = "http://getgreenshot.org",
				AuthorizeMode = AuthorizeModes.LocalhostServer,
				Token = _config
			};

			var oauthHttpBehaviour = new HttpBehaviour();
			oauthHttpBehaviour.OnHttpMessageHandlerCreated = httpMessageHandler => new OAuth2HttpMessageHandler(oAuth2Settings, oauthHttpBehaviour, httpMessageHandler);
			if (_config.AddFilename)
			{
				oauthHttpBehaviour.OnHttpClientCreated = httpClient => httpClient.AddDefaultRequestHeader("Slug", Uri.EscapeDataString(filename));
			}

			string response;
			var uploadUri = new Uri("https://picasaweb.google.com/data/feed/api/user").AppendSegments(_config.UploadUser, "albumid", _config.UploadAlbum);
			using (var stream = new MemoryStream())
			{
				ImageOutput.SaveToStream(capture, stream, outputSettings);
				stream.Position = 0;
				using (var uploadStream = new ProgressStream(stream, progress))
				{
					using (var content = new StreamContent(uploadStream))
					{
						content.Headers.Add("Content-Type", "image/" + outputSettings.Format);

						oauthHttpBehaviour.MakeCurrent();
						response = await uploadUri.PostAsync<string, HttpContent>(content, token);
					}
				}
			}

			return ParseResponse(response);
		}

		/// <summary>
		/// Parse the upload URL from the response
		/// </summary>
		/// <param name="response"></param>
		/// <returns></returns>
		public static string ParseResponse(string response)
		{
			if (response == null)
			{
				return null;
			}
			try
			{
				var doc = new XmlDocument();
				doc.LoadXml(response);
				var nodes = doc.GetElementsByTagName("link", "*");
				if (nodes.Count > 0)
				{
					string url = null;
					foreach (XmlNode node in nodes)
					{
						if (node.Attributes != null)
						{
							url = node.Attributes["href"].Value;
							string rel = node.Attributes["rel"].Value;
							// Pictures with rel="http://schemas.google.com/photos/2007#canonical" are the direct link
							if (rel != null && rel.EndsWith("canonical"))
							{
								break;
							}
						}
					}
					return url;
				}
			}
			catch (Exception e)
			{
				Log.Error("Could not parse Picasa response due to error {0}, response was: {1}", e.Message, response);
			}
			return null;
		}
	}
}