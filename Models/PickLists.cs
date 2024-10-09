using System;
using System.Collections.Generic;

namespace wayfair_order_picklist_dev.Models
{
    public class PickLists
    {
        public string ObjectType { get; set; }  
        public DateTime PickDate { get; set; }
        public List<PickListsLine> PickListsLines { get; set; }
    }
}