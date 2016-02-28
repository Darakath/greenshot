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

using System.Collections.Generic;
using System.Net.Http;
using Dapplo.HttpExtensions.Support;

namespace Greenshot.Addon.Dropbox.Entities
{
	/// <summary>
	/// This defines the request "content" for the Dropbox upload
	/// </summary>
	[Http(HttpParts.Request)]
	public class Upload
	{
		/// <summary>
		/// Headers is needed for the "Dropbox-API-Arg" value
		/// </summary>
		[Http(HttpParts.RequestHeaders)]
		public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>();

		/// <summary>
		/// This is what dropbox expects, not what it really is...
		/// </summary>
		[Http(HttpParts.RequestContentType)]
		public string ContentType { get; set; } = "application/octet-stream";

		/// <summary>
		/// The actual image for the upload is stored here
		/// </summary>
		[Http(HttpParts.RequestContent)]
		public HttpContent Content { get; set; }
	}
}
