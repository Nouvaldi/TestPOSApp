namespace TestPOSApp.Models
{
    public class TransactionReportDt
    {
        public int TransactionId { get; set; }
        public DateTime Date { get; set; }
        public List<TransactionItemReportDt> Items { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
