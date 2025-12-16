namespace AppCore.Entities;

public class Blog : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string FeaturedImageUrl { get; set; } = string.Empty;
    public string Abstract { get; set; } = string.Empty;
}
