﻿//
//  AttributeTypes.cs
//
//  Copyright (c) 2018 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;

namespace Warcraft.MPQ.Attributes
{
    /// <summary>
    /// The types of attributes which a file might have.
    /// </summary>
    [Flags]
    public enum AttributeTypes : uint
    {
        /// <summary>
        /// The file has a CRC32 hash.
        /// </summary>
        CRC32         = 0x00000001,

        /// <summary>
        /// The file has a timestamp.
        /// </summary>
        Timestamp     = 0x00000002,

        /// <summary>
        /// This file has an MD5 hash.
        /// </summary>
        MD5         = 0x00000004
    }
}
