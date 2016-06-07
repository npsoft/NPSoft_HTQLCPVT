/**
 * Region.cs
 *
 * Copyright (C) 2008 MaxMind Inc.  All Rights Reserved.
 * Copyright (C) 2010 GeoIPSharp.   All Rights Reserved.
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */
namespace MaxMind.GeoIP
{
    /// <summary>
    /// Represents a region of a country.
    /// </summary>
    public class Region
    {
        /// <summary>
        /// Initializes a new instance of the MaxMind.GeoIP.Region class.
        /// </summary>
        public Region() { }

        /// <summary>
        /// Initializes a new instance of the MaxMind.GeoIP.Region class specifying the country code, 
        /// country name and region name
        /// </summary>
        /// <param name="countryCode"></param>
        /// <param name="countryName"></param>
        /// <param name="name"></param>
        public Region(string countryCode, string countryName, string name)
        {
            this.CountryCode = countryCode;
            this.CountryName = countryName;
            this.Name = name;
        }

        /// <summary>
        /// Gets the containing country.
        /// </summary>
        public string CountryCode { get; internal set; }

        /// <summary>
        /// Gets the name of the containing country.
        /// </summary>
        public string CountryName { get; internal set; }

        /// <summary>
        /// Gets the name of the region
        /// </summary>
        public string Name { get; internal set; }
    }
}
