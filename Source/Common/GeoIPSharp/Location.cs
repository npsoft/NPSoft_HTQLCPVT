/**
 * Location.cs
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
    /// Represents a specific location on the Earths surface.
    /// </summary>
    public class Location : IEquatable<Location>
    {
        public string CountryCode { get; set; }
        public string CountryName { get; set; }
        public string Region { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int DmaCode { get; set; }
        public int AreaCode { get; set; }
        public string RegionName { get; set; }
        public int MetroCode { get; set; }

        /// <summary>
        /// The earths diameter in KM.
        /// </summary>
        private static double EARTH_DIAMETER = 2 * 6378.2;

        private static double RAD_CONVERT = Math.PI / 180;


        /// <summary>
        /// Gets the distance from the current location to the given location.
        /// </summary>
        /// <param name="loc">The end point which to measure to.</param>
        /// <returns></returns>
        public double DistanceFrom(Location loc)
        {
            double delta_lat, delta_lon;
            double temp;

            double lat1 = Latitude;
            double lon1 = Longitude;
            double lat2 = loc.Latitude;
            double lon2 = loc.Longitude;

            // convert degrees to radians
            lat1 *= RAD_CONVERT;
            lat2 *= RAD_CONVERT;

            // find the deltas
            delta_lat = lat2 - lat1;
            delta_lon = (lon2 - lon1) * RAD_CONVERT;

            // Find the great circle Distance
            temp = Math.Pow(Math.Sin(delta_lat / 2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(delta_lon / 2), 2);
            return EARTH_DIAMETER * Math.Atan2(Math.Sqrt(temp), Math.Sqrt(1 - temp));
        }

        #region IEquatable<Location> Members

        public bool Equals(Location other)
        {
            return this.Latitude == other.Latitude && this.Longitude == other.Longitude;
        }

        #endregion

        public override bool Equals(object obj)
        {
            Location other = obj as Location;
            if (other == null) return false;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return this.Latitude.GetHashCode() ^ this.Latitude.GetHashCode();
        }

        public static bool operator ==(Location a, Location b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Location a, Location b)
        {
            return !a.Equals(b);
        }
    }
}