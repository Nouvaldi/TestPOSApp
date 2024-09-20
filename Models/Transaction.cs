namespace TestPOSApp.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public List<TransactionItem> Items { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
