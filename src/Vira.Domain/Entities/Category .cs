using Vira.Shared.Base;

namespace Vira.Domain.Entities;
public class Category : AuditableEntity<Guid>
{
    public string Name { get; private set; }
    public string? Description { get; private set; }

    private Category()
    {
        Name = string.Empty;
    }
    public Category(string name, string? description)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        CreatedAt = DateTime.UtcNow;
    }

    public void Renama(string newName)
    {
        Name = newName;
        UpdatedAt = DateTime.UtcNow;
    }
}
