namespace wayfair_order_picklist_dev.Models
{
    public class PickListsLine
    {
        public int BaseObjectType { get; set; }
        public int OrderEntry {  get; set; }
        public int OrderRowID { get; set; }
        public int ReleasedQuantity { get; set; }
    }
}