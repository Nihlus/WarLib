﻿//
//  SplineKey.cs
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

using System.IO;
using Warcraft.Core.Extensions;

namespace Warcraft.Core.Structures
{
    public class SplineKey<T>
    {
        public T Value;
        public T InTangent;
        public T OutTangent;

        public SplineKey(BinaryReader br)
        {
            this.Value = br.Read<T>();
            this.InTangent = br.Read<T>();
            this.OutTangent = br.Read<T>();
        }
    }
}
