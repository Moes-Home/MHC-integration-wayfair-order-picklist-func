namespace wayfair_order_picklist_dev.Models
{
    public class DocumentLinesBinAllocation
    {
        public int BinAbsEntry {  get; set; }
        public int Quantity { get; set; }
        public string AllowNegativeQuantity { get; set; } = "tNO";
    }
}