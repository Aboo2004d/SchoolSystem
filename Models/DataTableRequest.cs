namespace SchoolSystem.Models{
    public class DataTableRequest
    {
        public int draw { get; set; }
        public int start { get; set; }
        public int length { get; set; }
        public DataTableSearch search { get; set; }
    }
    
}