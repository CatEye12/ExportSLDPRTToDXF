﻿using System.Collections.Generic;
using System.ComponentModel;

namespace ExportSLDPRTToDXF.Models
{
    /// <summary>
    /// The describes specification properties.
    /// </summary>
    public class Specification : ISpecificationView
    {

        public bool isDxf { get; set; }
        public string PartNumber { get; set; } // +
        public string Description { get; set; } // 
        public decimal WorkpieceX { get; set; }
        public decimal WorkpieceY { get; set; }
        public int Bend { get; set; }
        public decimal Thickness { get; set; }
        public string Configuration { get; set; }
        public int Version { get; set; }
        public int PaintX { get; set; }
        public int PaintY { get; set; }
        public int PaintZ { get; set; }
        public decimal SurfaceArea { get; set; }
        public string FileName { get; set; }
        public int IDPDM { get; set; }
        public string FilePath { get; set; }

        public string Partition { get; set; }
        public static IEnumerable <ISpecificationView> ToView (IEnumerable<Specification> specification)
        {
            List<ISpecificationView> resultList = new List<ISpecificationView>( );
            foreach (var item in specification)
            {
                
                resultList.Add(item);
            }
            return resultList;
        }
    }

    /// <summary>
    /// The describes properties which will view to user. 
    /// </summary>
    public interface ISpecificationView
    { 
        [DisplayName("Текущий DXF статус")]                
        bool isDxf { get; set; }      
        [DisplayName("Обозначение")]          
        string PartNumber { get; set; }
        [DisplayName("Наименование")]
        string Description { get; set; }
        [DisplayName("Конфигурация")]
        string Configuration { get; set; }
        [DisplayName("Версия")]
        int Version { get; set; }
    }
}
