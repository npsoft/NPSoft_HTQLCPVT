/**
 * Country.cs
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
    using System;

    /// <summary>
    /// Represents a country and it's ISO code.
    /// </summary>
    public class Country : IEquatable<Country>
    {
        /// <summary>
        /// Initializes a new instance of the MaxMind.GeoIP.Country class.
        /// </summary>
        /// <param name="code">The country code.</param>
        /// <param name="name">The country name.</param>
        public Country(string code, string name)
        {
            this.Code = code;
            this.Name = name;
        }

        /// <summary>
        /// Gets the ISO two-letter country code of this country.
        /// </summary>
        public string Code { get; private set; }

        /// <summary>
        /// Gets the name of this country.
        /// </summary>
        public string Name { get; private set; }

        public static bool operator ==(Country a, Country b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Country a, Country b)
        {
            return !a.Equals(b);
        }

        /// <summary>
        /// Formats this Country object as a human readable string.
        /// </summary>
        /// <returns>Returns a string that represents the current MaxMind.GeoIP.Country.</returns>
        public override string ToString()
        {
            return string.Format("{0} ({1})", this.Name, this.Code);
        }

        #region IEquatable<Country> Members

        /// <summary>
        /// Determines whether the specified MaxMind.GeoIP.Country is equal to the current MaxMind.GeoIP.Country.
        /// </summary>
        /// <param name="other">The MaxMind.GeoIP.Country to compare with the current MaxMind.GeoIP.Country.</param>
        /// <returns>Returns true if the specified MaxMind.GeoIP.Country is equal to the current MaxMind.GeoIP.Country;
        /// otherwise, false.</returns>
        public bool Equals(Country other)
        {
            return this.Code == other.Code && this.Name == other.Name;
        }

        #endregion

        /// <summary>
        /// Determines whether the specified System.object is equal to the current MaxMind.GeoIP.Country.
        /// </summary>
        /// <param name="obj">The System.object to compare with the current MaxMind.GeoIP.Country.</param>
        /// <returns>Returns true if the specified System.object is equal to the current MaxMind.GeoIP.Country;
        /// otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            Country other = obj as Country;
            if (other == null)
            {
                return false;
            }

            return this.Equals(other);
        }

        /// <summary>
        /// Returns the hash code for this MaxMind.GeoIP.Country.
        /// </summary>
        /// <returns>Returns a 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return this.Code.GetHashCode() ^ this.Name.GetHashCode();
        }
    }
}