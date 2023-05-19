using DatabaseBackupManager.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DatabaseBackupManager.Models;

public class AfterSaveChanges
{
    public BaseModel Entity { get; }
    public EntityState State { get; }
    
    private readonly List<PropertyChanged> _changedProperties;

    public AfterSaveChanges(EntityEntry entry)
    {
        Entity = entry.Entity as BaseModel;
        State = entry.State;
        
        _changedProperties = entry.Properties
            .Where(p => p.IsModified)
            .Select(p => new PropertyChanged
            {
                IsModified = p.IsModified,
                PropertyName = p.Metadata.PropertyInfo?.Name ?? p.Metadata.Name,
                OriginalValue = p.OriginalValue,
                CurrentValue = p.CurrentValue
            })
            .ToList();
    }

    public PropertyChanged Property(string name)
    {
        return _changedProperties.FirstOrDefault(p => p.PropertyName == name);
    }
}

public class PropertyChanged
{
    public bool IsModified { get; set; }
    public string PropertyName { get; set; }
    public object OriginalValue { get; set; }
    public object CurrentValue { get; set; }
}