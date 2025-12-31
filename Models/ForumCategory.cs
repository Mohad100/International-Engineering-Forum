namespace Fourm.Models;

/// <summary>
/// Represents a forum category
/// </summary>
public class ForumCategory
{
    public static readonly List<ForumCategory> AllCategories = new()
    {
        new ForumCategory { Id = "discussion", Name = "General Discussion", Icon = "💬", Description = "General discussions and diverse engineering topics" },
        new ForumCategory { Id = "civil", Name = "Civil Engineering", Icon = "🏗️", Description = "Construction projects, structural design and infrastructure" },
        new ForumCategory { Id = "electrical", Name = "Electrical Engineering", Icon = "⚡", Description = "Power systems, electronics and electrical networks" },
        new ForumCategory { Id = "mechanical", Name = "Mechanical Engineering", Icon = "⚙️", Description = "Machinery, thermodynamics and manufacturing" },
        new ForumCategory { Id = "software", Name = "Software Engineering", Icon = "💻", Description = "Programming, app development and artificial intelligence" },
        new ForumCategory { Id = "chemical", Name = "Chemical Engineering", Icon = "🧪", Description = "Chemical processes and petrochemical industries" },
        new ForumCategory { Id = "architecture", Name = "Architecture Engineering", Icon = "🏛️", Description = "Architectural design, urbanism and interior design" }
    };
    
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public static ForumCategory? GetById(string id)
    {
        return AllCategories.FirstOrDefault(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }
}
