namespace EnterpriseRag.Domain
{
    public class DocumentAccess
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public Document? Document { get; set; }
    }
}
