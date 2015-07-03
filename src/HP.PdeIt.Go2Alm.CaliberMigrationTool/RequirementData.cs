// go2ALM: migrate from various tools to QC/ALM
// Copyright (C) 2012 Hewlett Packard Company
// Authors: 
//      Gustavo Mejia
//        from Hewlett Packard Company
//      
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.

namespace HP.PdeIt.Go2Alm.CaliberMigrationTool
{
    /// <summary>
    ///  Data structure to map requirements between the old caliber ID and the new ALM ID.
    /// </summary>
    /// <remarks>Caliber and ALM names might be different when the Caliber name contains characters that are invalid for ALM names.</remarks>
    public class RequirementData
    {
        public string CaliberName { get; set; }

        public int CaliberID { get; set; }
        
        public string AlmName { get; set; }
        
        public int AlmID { get; set; }
        
        public string Description { get; set; }
    }
}
