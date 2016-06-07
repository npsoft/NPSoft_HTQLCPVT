/**
 * DatabaseInfo.cs
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

    public class DatabaseInfo
    {
        public const int COUNTRY_EDITION = 1;
        public const int REGION_EDITION_REV0 = 7;
        public const int REGION_EDITION_REV1 = 3;
        public const int CITY_EDITION_REV0 = 6;
        public const int CITY_EDITION_REV1 = 2;
        public const int ORG_EDITION = 5;
        public const int ISP_EDITION = 4;
        public const int PROXY_EDITION = 8;
        public const int ASNUM_EDITION = 9;
        public const int NETSPEED_EDITION = 10;

        private string info;

        /// <summary>
        /// Initializes a new instance of MaxMind.GeoIP.DatabaseInfo class.
        /// </summary>
        /// <param name="info"></param>
        public DatabaseInfo(string info)
        {
            this.info = info;
        }

        public int DatabaseType
        {
            get { return this.GetDatabaseType(); }
        }

        /// <summary>
        /// Gets a value indicating whether the database is the premium version.
        /// </summary>
        public bool IsPremium
        {
            get
            {
                return this.info.IndexOf("FREE") < 0;
            }
        }

        /// <summary>
        /// Gets the date of the database file.
        /// </summary>
        public DateTime Date
        {
            get { return this.GetDate(); }
        }

        public override string ToString()
        {
            return this.info;
        }

        private DateTime GetDate()
        {
            for (int i = 0; i < this.info.Length - 9; i++)
            {
                if (Char.IsWhiteSpace(this.info[i]) == true)
                {
                    string dateString = this.info.Substring(i + 1, i + 9);
                    try
                    {
                        return DateTime.ParseExact(dateString, "yyyyMMdd", null);
                    }
                    catch (Exception e)
                    {
                        Console.Write(e.Message);
                    }

                    break;
                }
            }

            return DateTime.Now;
        }

        private int GetDatabaseType()
        {
            if (string.IsNullOrEmpty(this.info))
            {
                return COUNTRY_EDITION;
            }
            else
            {
                // Get the type code from the database info string and then
                // subtract 105 from the value to preserve compatability with
                // databases from April 2003 and earlier.
                return Convert.ToInt32(this.info.Substring(4, 7)) - 105;
            }
        }
    }
}