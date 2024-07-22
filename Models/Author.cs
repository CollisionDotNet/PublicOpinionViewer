namespace PublicOpinionViewer.Models
{
    public enum Sex
    {
        Male,
        Female
    }
    public class Author
    {       
        public string Id { get; init; }
        public Sex? Sex { get; set; }
        public DateOnly? BirthDate { get; set; }
        public int? Age => BirthDate == null ? null : (new DateTime(1, 1, 1) + (DateTime.Now - BirthDate.Value.ToDateTime(TimeOnly.MinValue))).Year - 1;
        public Author (string id)
        {
            Id = id;
        }
    }
}
