using System;
using System.Collections.Generic;

namespace wayfair_order_picklist_dev.Models
{
    public class PickListsDTO
    {
        public string ObjectType { get; set; }
        public string PickDate { get; set; }
        public List<PickListsLineDTO> PickListsLines { get; set; }
    }
}